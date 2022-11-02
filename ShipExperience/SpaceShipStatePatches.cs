using System.Collections.Generic;
using HarmonyLib;

using PavonisInteractive.TerraInvicta;
using PavonisInteractive.TerraInvicta.Systems.Bootstrap;

using SpaceShipExtras;


namespace SpaceShipExtras.ShipExperience {
    [HarmonyPatch(typeof(SolarSystemBootstrap), "LoadGame")]
    static class SolarSystemBootstrap_LoadGame_Patch {
        static void Prefix() {
            Main.experienceManager.ResetState();
        }
    }

    [HarmonyPatch(typeof(ViewControl), "ClearGameData")]
    static class ViewControl_ClearGameData_Patch {
        static void Prefix() {
            Main.experienceManager.ResetState();
        }
    }

    static class RefitExperienceState {
        public static Dictionary<string, int> expMap = new Dictionary<string, int>();
    }

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

            RefitExperienceState.expMap.Add(
                item.originalSpaceShipState.displayName,
                Main.experienceManager.GetExperience(item.originalSpaceShipState));
        }
    }

    [HarmonyPatch(typeof(TISpaceShipState), "SetDisplayName")]
    static class TISpaceShipState_SetDisplayName_Patch {
        static void Postfix(TISpaceShipState __instance, string newName) {
            if (RefitExperienceState.expMap.ContainsKey(newName)) {
                Main.experienceManager.AddExperience(__instance, RefitExperienceState.expMap[newName]);
                RefitExperienceState.expMap.Remove(newName);
            }
        }
    }

    [HarmonyPatch(typeof(TISpaceShipState), "InitWithTemplate")]
    static class TISpaceShipState_InitWithTemplate_Patch {
        static void Postfix(TIDataTemplate rawTemplate, TISpaceShipState __instance) {
            if (rawTemplate as TISpaceShipTemplate != null) {
                Main.experienceManager.RegisterShip(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(TISpaceShipState), "DestroyShip")]
    static class TISpaceShipState_DestroyShip_Patch {
        static void Postfix(ref TISpaceShipState __instance) {
            Main.experienceManager.UnregisterShip(__instance);
        }
    }
}
