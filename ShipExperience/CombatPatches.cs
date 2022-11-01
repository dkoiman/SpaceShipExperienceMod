using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using PavonisInteractive.TerraInvicta;
using PavonisInteractive.TerraInvicta.SpaceCombat.UI;

namespace SpaceShipExtras.ShipExperience {
    static class PrecombatDataStash {
        public static TIFactionState factionA = null;
        public static TIFactionState factionB = null;
        public static float savedPrecombatPowerFactionA;
        public static float savedPrecombatPowerFactionB;
        public static Dictionary<TISpaceShipState, int> ranksFactionA = new Dictionary<TISpaceShipState, int>();
        public static Dictionary<TISpaceShipState, int> ranksFactionB = new Dictionary<TISpaceShipState, int>();
    }

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

            if (factionA == null || hab?.faction == factionA) {
                factionA = hab.faction;
                powerFactionA += hab.SpaceCombatValue();
            }

            if (factionB == null || hab?.faction == factionB) {
                factionB = hab.faction;
                powerFactionB += hab.SpaceCombatValue();
            }

            if (fleetFactionA != null) {
                powerFactionA += fleetFactionA.SpaceCombatValue();
                foreach (var ship in fleetFactionA.ships) {
                    PrecombatDataStash.ranksFactionA.Add(ship, Main.experienceManager.GetRank(ship));
                }
            }

            if (fleetFactionB != null) {
                powerFactionB += fleetFactionB.SpaceCombatValue();
                foreach (var ship in fleetFactionB.ships) {
                    PrecombatDataStash.ranksFactionB.Add(ship, Main.experienceManager.GetRank(ship));
                }
            }

            PrecombatDataStash.factionA = factionA;
            PrecombatDataStash.factionB = factionB;
            PrecombatDataStash.savedPrecombatPowerFactionA = powerFactionA;
            PrecombatDataStash.savedPrecombatPowerFactionB = powerFactionB;
        }
    }

    [HarmonyPatch(typeof(TISpaceCombatState), "SetWinnerAndLoser")]
    static class TISpaceCombatState_SetWinnerAndLoser_Patch {
        static TISpaceFleetState WinnerFleet(TISpaceCombatState combat) {
            if (combat.fleets[0] != null) {
                return combat.winner == combat.fleets[0].faction ? combat.fleets[0] : combat.fleets[1];
            } else {
                return combat.winner == combat.fleets[1].faction ? combat.fleets[1] : combat.fleets[0];
            }
        }

        static TISpaceFleetState LooserFleet(TISpaceCombatState combat) {
            if (combat.fleets[0] != null) {
                return combat.loser == combat.fleets[0].faction ? combat.fleets[0] : combat.fleets[1];
            } else {
                return combat.loser == combat.fleets[1].faction ? combat.fleets[1] : combat.fleets[0];
            }
        }

        static void EvalResult(TISpaceFleetState thisFleet, TISpaceFleetState otherFleet, RankEffects.Outcome outcome) {
            if (thisFleet == null) {
                return;
            }

            bool evalFactionA = thisFleet.faction == PrecombatDataStash.factionA;
            float thisPower =
                evalFactionA
                ? PrecombatDataStash.savedPrecombatPowerFactionA
                : PrecombatDataStash.savedPrecombatPowerFactionB;
            float otherPower =
                evalFactionA
                ? PrecombatDataStash.savedPrecombatPowerFactionB
                : PrecombatDataStash.savedPrecombatPowerFactionA;

            RankEffects.DispatchResults(
                evalFactionA ? PrecombatDataStash.factionA : PrecombatDataStash.factionB,
                evalFactionA ? PrecombatDataStash.ranksFactionA : PrecombatDataStash.ranksFactionB,
                otherPower / thisPower,
                outcome);

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

        static void Process(TISpaceCombatState combat) {
            if (combat.bothSidesDestroyed) {
                return;
            }
            
            if (!combat.combatOccurs) {
                return;
            }
            
            if (combat.draw) {
                EvalResult(combat.fleets[0], combat.fleets[1], RankEffects.Outcome.DRAW);
                EvalResult(combat.fleets[1], combat.fleets[0], RankEffects.Outcome.DRAW);
                return;
            } 

            EvalResult(WinnerFleet(combat), LooserFleet(combat), RankEffects.Outcome.WIN);
            EvalResult(LooserFleet(combat), WinnerFleet(combat), RankEffects.Outcome.LOSS);
        }

        static void Postfix(TISpaceCombatState __instance) {
            Process(__instance);
            PrecombatDataStash.ranksFactionA.Clear();
            PrecombatDataStash.ranksFactionB.Clear();
        }
    }
}
