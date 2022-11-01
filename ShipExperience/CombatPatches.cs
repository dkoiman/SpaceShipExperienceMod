using System;
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
    }

    [HarmonyPatch(typeof(PrecombatController), "FillOutCombatData")]
    static class PrecombatController_FillOutCombatData_Patch {
        static void Prefix(PrecombatController __instance) {
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
            }

            if (fleetFactionB != null) {
                powerFactionB += fleetFactionB.SpaceCombatValue();
            }

            PrecombatDataStash.factionA = factionA;
            PrecombatDataStash.factionB = factionB;
            PrecombatDataStash.savedPrecombatPowerFactionA = powerFactionA;
            PrecombatDataStash.savedPrecombatPowerFactionB = powerFactionB;
        }
    }

    [HarmonyPatch(typeof(TISpaceCombatState), "SetWinnerAndLoser")]
    static class TISpaceCombatState_SetWinnerAndLoser_Patch {
        enum Outcome {
            WIN,
            LOSS,
            DRAW,
            TOTAL_ANIHILATION
        }

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

        static void EvalResult(TISpaceFleetState thisFleet, TISpaceFleetState otherFleet, Outcome outcome) {
            if (thisFleet == null) {
                return;
            }

            var survivingShips =
                from ship in thisFleet.ships
                where !ship.ShipDestroyed()
                select ship;

            if (survivingShips.Count() == 0) {
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

            double difficultyFactor = Math.Pow(otherPower / thisPower, 0.3);
            double expGainedRaw = otherPower * difficultyFactor / survivingShips.Count();
            int expGained = (int)Math.Ceiling(expGainedRaw);

            foreach (var ship in survivingShips) {
                Main.experienceManager.AddExperience(ship, expGained);
            }
        }

        static void Postfix(TISpaceCombatState __instance) {
            if (__instance.bothSidesDestroyed) {
                return;
            }

            if (!__instance.combatOccurs) {
                return;
            }

            if (__instance.draw ) {
                EvalResult(__instance.fleets[0], __instance.fleets[1], Outcome.DRAW);
                EvalResult(__instance.fleets[1], __instance.fleets[0], Outcome.DRAW);
                return;
            }

            EvalResult(WinnerFleet(__instance), LooserFleet(__instance), Outcome.WIN);
            EvalResult(LooserFleet(__instance), WinnerFleet(__instance), Outcome.LOSS);
        }
    }
}
