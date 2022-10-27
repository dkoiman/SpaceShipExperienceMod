using HarmonyLib;

using PavonisInteractive.TerraInvicta;
using PavonisInteractive.TerraInvicta.Systems.Bootstrap;


namespace SpaceShipExperienceMod {
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

    static class PrecombatDataStash {
        public static float savedPrecombatAllyPower;
        public static float savedPrecombatFoePower;
    }

 
}
