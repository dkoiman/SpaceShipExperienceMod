using HarmonyLib;

using PavonisInteractive.TerraInvicta;

namespace SpaceShipExtras.Utility {

    // Special utility modules can only be mounted to a 'Utility Ship' hull.
    [HarmonyPatch(typeof(TISpaceShipTemplate), "ValidPartForDesign")]
    static class TISpaceShipTemplate_ValidPartForDesign_Patch {
        static bool Prefix(TISpaceShipTemplate __instance, TIShipPartTemplate part, ref bool __result) {
            bool isSpecialUtilityModule = (
                part as TIDeepSpaceRepairBay != null ||
                part as TIFuelPipe != null ||
                part as TIDebrisCleaner != null);

            if (isSpecialUtilityModule &&
                __instance.hullTemplate as TIUtilityShipHull == null) {
                __result = false;
                return false;
            }
            return true;
        }
    }

    // Operation's icon image is not virtual, thus we need to highjack the
    // function to route it into the mod's asset bundle.
    [HarmonyPatch(typeof(TIOperationTemplate), "get_operationIconImagePath")]
    static class TIOperationTemplate_get_operationIconImagePath_Patch {
        static bool Prefix(TIOperationTemplate __instance, ref string __result) {
            if (__instance as DeepSpaceRepairFleetOperation != null) {
                __result = "space_ship_extras/ICO_DeepSpaceRepairFleetOperation";
                return false;
            } else if (__instance as ClearDebrisOperation != null) {
                __result = "space_ship_extras/ICO_ClearDebrisOperation";
                return false;
            } else if (__instance as RefuelOperation != null) {
                __result = "space_ship_extras/ICO_RefuelOperation";
                return false;
            }

            return true;
        }
    }

    // Register the newly added fleet operations.
    [HarmonyPatch(typeof(OperationsManager), "Initalize")]
    static class OperationManager_Initalize_Patch {
        static void Postfix() {
            DeepSpaceRepairFleetOperation deepSpaceRepair = new DeepSpaceRepairFleetOperation();
            OperationsManager.fleetOperations.Add(deepSpaceRepair);
            OperationsManager.operationsLookup.Add(deepSpaceRepair.GetType(), deepSpaceRepair);

            ClearDebrisOperation clearDebris = new ClearDebrisOperation();
            OperationsManager.fleetOperations.Add(clearDebris);
            OperationsManager.operationsLookup.Add(clearDebris.GetType(), clearDebris);

            RefuelOperation refuel = new RefuelOperation();
            OperationsManager.fleetOperations.Add(refuel);
            OperationsManager.operationsLookup.Add(refuel.GetType(), refuel);
        }
    }

    // Register the newly added template. ValidateAllTemplates is run after all
    // templates are loaded by the game itself.
    [HarmonyPatch(typeof(TemplateManager), "ValidateAllTemplates")]
    static class TemplateManager_ValidateAllTemplates_Patch {
        // Intercept the method prior execution.
        static void Prefix() {
            TemplateManager.Add<TIShipHullTemplate>(new TIUtilityShipHull());

            TemplateManager.Add<TIFuelPipe>(new TIFuelPipe());
            TemplateManager.Add<TIDeepSpaceRepairBay>(new TIDeepSpaceRepairBay());
            TemplateManager.Add<TIProjectTemplate>(new TIDeepSpaceMaintenanceProject());

            TemplateManager.Add<TIDebrisCleaner>(new TIDebrisCleaner());
            TemplateManager.Add<TIProjectTemplate>(new TIOrbitalCaptureProject());
        }
    }

    // Pretend that 'Utility Ship' is a 'Gunship'. Otherwise the engine model resolution
    // code complains it can not find a proper asset for the engines. 'Utility Ship' is
    // a dimilitarized copy-cat of 'Gunship' in general, so no problem with models'
    // compatibility.
    [HarmonyPatch(typeof(TIDriveTemplate), "modelResource")]
    static class TIDriveTemplate_modelResource_Patch {
        static void Postfix(ref string __result) {
            if (__result.Contains("UtilityShip")) {
                __result = __result.Replace("UtilityShip", "Gunship");
            }
        }
    }
}
