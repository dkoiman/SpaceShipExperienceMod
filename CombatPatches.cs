using System;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using PavonisInteractive.TerraInvicta;
using PavonisInteractive.TerraInvicta.SpaceCombat.UI;

namespace SpaceShipExperienceMod {
    [HarmonyPatch(typeof(PrecombatController), "FillOutCombatData")]
    static class PrecombatController_FillOutCombatData_Patch {
        static void Prefix(PrecombatController __instance) {
            TISpaceCombatState combat =
                __instance
                .GetType()
                .GetField("combat", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(__instance) as TISpaceCombatState;
            TISpaceFleetState x = combat.fleets[1];
            TIFactionState xFaction = (x != null) ? x.faction : null;
            bool xAlly = xFaction == GameControl.control.activePlayer;

            TISpaceFleetState allyFleet = xAlly ? combat.fleets[1] : combat.fleets[0];
            TISpaceFleetState foeFleet = xAlly ? combat.fleets[0] : combat.fleets[1];
            TIHabState hab = combat.hab;

            bool allyHasHab = (hab != null) && (hab.faction == xFaction);
            bool foeHasHab = (hab != null) && (hab.faction != xFaction);

            float allyPower = allyHasHab ? hab.SpaceCombatValue() : 0;
            float foePower = foeHasHab ? hab.SpaceCombatValue() : 0;

            if (allyFleet != null) {
                allyPower += allyFleet.SpaceCombatValue();
            }

            if (foeFleet != null) {
                foePower += foeFleet.SpaceCombatValue();
            }

            PrecombatDataStash.savedPrecombatAllyPower = allyPower;
            PrecombatDataStash.savedPrecombatFoePower = foePower;
        }
    }

    [HarmonyPatch(typeof(TISpaceFleetState), "PostCombat")]
    static class TISpaceFleetState_PostCombat_Patch {
        static void Postfix(ref TISpaceFleetState __instance) {
            var survivingShips =
                from ship in __instance.ships
                where !ship.ShipDestroyed()
                select ship;

            if (survivingShips.Count() == 0) {
                return;
            }

            bool xAlly = __instance.faction == GameControl.control.activePlayer;
            float thisPower =
                xAlly
                ? PrecombatDataStash.savedPrecombatAllyPower
                : PrecombatDataStash.savedPrecombatFoePower;
            float otherPower =
                xAlly
                ? PrecombatDataStash.savedPrecombatFoePower
                : PrecombatDataStash.savedPrecombatAllyPower;

            float difficultyFactor = otherPower / thisPower;
            float expGainedRaw = otherPower * difficultyFactor / survivingShips.Count();
            int expGained = (int)Math.Ceiling(expGainedRaw);

            foreach (var ship in survivingShips) {
                Main.experienceManager.AddExperience(ship, expGained);
            }
        }
    }
}
