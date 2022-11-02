using System.Collections.Generic;
using HarmonyLib;

using PavonisInteractive.TerraInvicta;
using PavonisInteractive.TerraInvicta.Systems.Bootstrap;

namespace SpaceShipExtras.ShipExperience {

    // Clear the mapping on loading the game.
    [HarmonyPatch(typeof(SolarSystemBootstrap), "LoadGame")]
    static class SolarSystemBootstrap_LoadGame_Patch {
        static void Prefix() {
            Main.experienceManager.ResetState();
        }
    }

    // Clear the mapping when game clears its state.
    [HarmonyPatch(typeof(ViewControl), "ClearGameData")]
    static class ViewControl_ClearGameData_Patch {
        static void Prefix() {
            Main.experienceManager.ResetState();
        }
    }

    // A global structurure to preserve the experience over refits.
    static class RefitExperienceState {
        public static Dictionary<string, int> expMap = new Dictionary<string, int>();
    }

    // Before we start processing the refit event, we need to preserve the
    // experience level of the original ship, for it will be destroyed within
    // the body of the method before the new ship is created. We track the state
    // by display name. While the method would not be reliable in general, it
    // works fine here because all the refit events are processed sequentially.
    // We could be ok with just a global int, but keeping the name in the
    // mapping ensures we are accounting for the right ship - even if the game
    // will start processing the refit events in parallel, there is a very small
    // chance that two ships with the equivalent display name will get done at
    // the same time unless it is manually orchestrated.
    //
    // As a secondary effect, we are refunding the cost of fuel of the original
    // ship, since vanilla game just counts it as lost.
    [HarmonyPatch(typeof(TIFactionState), "CompleteShipConstruction")]
    static class TIFactionState_CompleteShipConstruction_Patch {
        static void Prefix(TIFactionState __instance, TIHabModuleState shipyardIdx, ShipConstructionQueueItem item) {
            if (__instance.nShipyardQueues[shipyardIdx].Count == 0) {
                return;
            }

            if (item == null) {
                item = __instance.nShipyardQueues[shipyardIdx][0];
            }

            if (!item.isRefit) {
                return;
            }

            TISpaceShipState ship = item.originalSpaceShipState;

            // Preserve experience.
            RefitExperienceState.expMap.Add(
                ship.displayName,
                Main.experienceManager.GetExperience(item.originalSpaceShipState));

            // Refund fuel.
            ship.GetPreferredPropellantTankCost(ship.faction, ship.propellant_tons)
                .RefundCost(ship.faction);
        }
    }

    // On SetDisplayName, if the name is equal to one we preserved in the
    // mapping - transfer the experience to the ship and clear the mapping.
    [HarmonyPatch(typeof(TISpaceShipState), "SetDisplayName")]
    static class TISpaceShipState_SetDisplayName_Patch {
        static void Postfix(TISpaceShipState __instance, string newName) {
            if (RefitExperienceState.expMap.ContainsKey(newName)) {
                Main.experienceManager.AddExperience(__instance, RefitExperienceState.expMap[newName]);
                RefitExperienceState.expMap.Remove(newName);
            }
        }
    }

    // When a new ship instance is created, register it with the manager.
    [HarmonyPatch(typeof(TISpaceShipState), "InitWithTemplate")]
    static class TISpaceShipState_InitWithTemplate_Patch {
        static void Postfix(TIDataTemplate rawTemplate, TISpaceShipState __instance) {
            if (rawTemplate as TISpaceShipTemplate != null) {
                Main.experienceManager.RegisterShip(__instance);
            }
        }
    }

    // When a ship is destroyed - unregister it from the manager.
    [HarmonyPatch(typeof(TISpaceShipState), "DestroyShip")]
    static class TISpaceShipState_DestroyShip_Patch {
        static void Postfix(ref TISpaceShipState __instance) {
            Main.experienceManager.UnregisterShip(__instance);
        }
    }
}
