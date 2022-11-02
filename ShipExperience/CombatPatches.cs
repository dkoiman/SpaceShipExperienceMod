using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HarmonyLib;
using UnityEngine;

using PavonisInteractive.TerraInvicta;
using PavonisInteractive.TerraInvicta.SpaceCombat.UI;

namespace SpaceShipExtras.ShipExperience {
    // Data, that needs to be carried over from the pre-combat state to the
    // post-combat evaluation.
    static class PrecombatDataStash {
        public static TIFactionState factionA = null;
        public static TIFactionState factionB = null;
        public static float savedPrecombatPowerFactionA;
        public static float savedPrecombatPowerFactionB;
        public static Dictionary<TISpaceShipState, int> ranksFactionA = new Dictionary<TISpaceShipState, int>();
        public static Dictionary<TISpaceShipState, int> ranksFactionB = new Dictionary<TISpaceShipState, int>();
    }

    public static class PostcombatReportStash {
        public static bool playerFactionWin = false;
        public static float propagandaGain = 0;
    }

    // During the combat ships get damaged and destroyed. That triggers changes
    // in the power evaluation of the fleet and ship destruction will result in
    // losing its rank information. So, we intercept a method that runs just
    // prior the combat to capture the state of the combatants just before the
    // battle commences. One of the combatants may be not a fleet, but a hub,
    // and that adds a bit of the complications in doing some evaluations.
    [HarmonyPatch(typeof(PrecombatController), "FillOutCombatData")]
    static class PrecombatController_FillOutCombatData_Patch {
        static void Prefix(PrecombatController __instance) {
            PrecombatDataStash.ranksFactionA.Clear();
            PrecombatDataStash.ranksFactionB.Clear();

            TISpaceCombatState combat =
                __instance
                .GetType()
                .GetField("combat", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(__instance) as TISpaceCombatState;

            float powerFactionA = 0;
            float powerFactionB = 0;
            TISpaceFleetState fleetFactionA = combat.fleets[0];
            TISpaceFleetState fleetFactionB = combat.fleets[1];
            TIFactionState factionA = fleetFactionA?.faction;
            TIFactionState factionB = fleetFactionB?.faction;
            TIHabState hab = combat.hab;

            // If there is no fleetA, we don't know factionA, but we know it
            // must have a hab in the battle. Alternatively, if there is a
            // hab and it belongs to factionA, we also have to account for its
            // combat power.
            if (factionA == null || hab?.faction == factionA) {
                factionA = hab.faction;
                powerFactionA += hab.SpaceCombatValue();
            }
            // If there is no fleetB, we don't know factionB, but we know it
            // must have a hab in the battle. Alternatively, if there is a
            // hab and it belongs to factionB, we also have to account for its
            // combat power.
            if (factionB == null || hab?.faction == factionB) {
                factionB = hab.faction;
                powerFactionB += hab.SpaceCombatValue();
            }

            // Account for fleetA power if present, and record all its ships'
            // ranks.
            if (fleetFactionA != null) {
                powerFactionA += fleetFactionA.SpaceCombatValue();
                foreach (var ship in fleetFactionA.ships) {
                    PrecombatDataStash.ranksFactionA.Add(ship, Main.experienceManager.GetRank(ship));
                }
            }
            // Account for fleetB power if present, and record all its ships'
            // ranks.
            if (fleetFactionB != null) {
                powerFactionB += fleetFactionB.SpaceCombatValue();
                foreach (var ship in fleetFactionB.ships) {
                    PrecombatDataStash.ranksFactionB.Add(ship, Main.experienceManager.GetRank(ship));
                }
            }

            // Preserve the faction and power info.
            PrecombatDataStash.factionA = factionA;
            PrecombatDataStash.factionB = factionB;
            PrecombatDataStash.savedPrecombatPowerFactionA = powerFactionA;
            PrecombatDataStash.savedPrecombatPowerFactionB = powerFactionB;
        }
    }

    // Add RankEffect outcomes to Post Combat UI
    [HarmonyPatch(typeof(PrecombatController), "OnCombatComplete")]
    [HarmonyPatch(new Type[] { typeof(TISpaceCombatState) })]
    static class PrecombatController_OnCombatComplete_Patch {
        static bool withinPostCombat = false;

        static void Prefix() { withinPostCombat = true; }
        static void Postfix() { withinPostCombat = false; }

        [HarmonyPatch(typeof(TIResourcesCost), "ToString")]
        static class TIResourcesCost_ToString_Patch {
            static void Postfix(ref string __result) {
                if (!withinPostCombat ||
                    !PostcombatReportStash.playerFactionWin ||
                    PostcombatReportStash.propagandaGain < 0.001) {
                    return;
                }

                __result =
                    new StringBuilder()
                    .Append(__result)
                    .AppendLine()
                    .AppendLine(
                        Loc.T(
                            "UI.SpaceCombat.Outcome.Propaganda",
                            new object [] {
                                PostcombatReportStash.propagandaGain.ToString("N3")
                            }))
                    .ToString();

                PostcombatReportStash.playerFactionWin = false;
            }
        }
    }

    // Once the battle finishes, winner and loser are decided. Once that happens
    // we execute our post-combat logic.
    [HarmonyPatch(typeof(TISpaceCombatState), "SetWinnerAndLoser")]
    static class TISpaceCombatState_SetWinnerAndLoser_Patch {
        static void Postfix(TISpaceCombatState __instance) {
            Process(__instance);
            PrecombatDataStash.ranksFactionA.Clear();
            PrecombatDataStash.ranksFactionB.Clear();
        }

        // Returns the winning fleet.
        static TISpaceFleetState WinnerFleet(TISpaceCombatState combat) {
            if (combat.fleets[0] != null) {
                return combat.winner == combat.fleets[0].faction ? combat.fleets[0] : combat.fleets[1];
            } else {
                return combat.winner == combat.fleets[1].faction ? combat.fleets[1] : combat.fleets[0];
            }
        }

        // Returns the loosing fleet.
        static TISpaceFleetState LooserFleet(TISpaceCombatState combat) {
            if (combat.fleets[0] != null) {
                return combat.loser == combat.fleets[0].faction ? combat.fleets[0] : combat.fleets[1];
            } else {
                return combat.loser == combat.fleets[1].faction ? combat.fleets[1] : combat.fleets[0];
            }
        }

        // Evaluate the effects of the battle on the specifi fleet.
        static void EvalResult(TISpaceFleetState thisFleet,
                               RankEffects.Outcome outcome) {
            if (thisFleet == null) {
                return;
            }

            // Determine if the fleet was A or B side in the battle.
            bool evalFactionA = thisFleet.faction == PrecombatDataStash.factionA;
            float thisPower =
                evalFactionA
                ? PrecombatDataStash.savedPrecombatPowerFactionA
                : PrecombatDataStash.savedPrecombatPowerFactionB;
            float otherPower =
                evalFactionA
                ? PrecombatDataStash.savedPrecombatPowerFactionB
                : PrecombatDataStash.savedPrecombatPowerFactionA;

            // Trigger rank effects for the fleet.
            RankEffects.DispatchResults(
                evalFactionA ? PrecombatDataStash.factionA : PrecombatDataStash.factionB,
                evalFactionA ? PrecombatDataStash.ranksFactionA : PrecombatDataStash.ranksFactionB,
                otherPower / thisPower,
                outcome);

            // Grant experience for each surviving ship.
            var survivingShips =
                from ship in thisFleet.ships
                where !ship.ShipDestroyed()
                select ship;

            if (survivingShips.Count() == 0) {
                return;
            }

            double difficultyFactor = Math.Pow(otherPower / thisPower, 0.3);
            double expGainedRaw = otherPower * difficultyFactor / survivingShips.Count();
            int expGained = (int)Math.Ceiling(expGainedRaw);

            foreach (var ship in survivingShips) {
                Main.experienceManager.AddExperience(ship, expGained);
            }
        }

        // Ignore evasions and mutual destructions. Evaluate effects on both
        // combatants in other cases.
        static void Process(TISpaceCombatState combat) {
            PostcombatReportStash.playerFactionWin = false;
            if (combat.bothSidesDestroyed) {
                return;
            }
            
            if (!combat.combatOccurs) {
                return;
            }
            
            if (combat.draw) {
                EvalResult(combat.fleets[0], RankEffects.Outcome.DRAW);
                EvalResult(combat.fleets[1], RankEffects.Outcome.DRAW);
                return;
            } 

            EvalResult(WinnerFleet(combat), RankEffects.Outcome.WIN);
            EvalResult(LooserFleet(combat), RankEffects.Outcome.LOSS);
        }
    }
}
