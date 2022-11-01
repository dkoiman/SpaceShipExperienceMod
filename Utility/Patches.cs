using HarmonyLib;

using PavonisInteractive.TerraInvicta;

namespace SpaceShipExtras.Utility {

    [HarmonyPatch(typeof(TISpaceShipTemplate), "ValidPartForDesign")]
    static class TISpaceShipTemplate_ValidPartForDesign_Patch {
        static bool Prefix(TISpaceShipTemplate __instance, TIShipPartTemplate part, ref bool __result) {
            bool isSpecialUtilityModule = (
                part as TIDeepSpaceRepairBay != null ||
                part as TIDebrisCleaner != null);

            if (isSpecialUtilityModule &&
                __instance.hullTemplate as TIUtilityShipHull == null) {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(TIOperationTemplate), "get_operationIconImagePath")]
    static class TIOperationTemplate_get_operationIconImagePath_Patch {
        static bool Prefix(TIOperationTemplate __instance, ref string __result) {
            if (__instance as DeepSpaceRepairFleetOperation != null) {
                __result = "space_ship_extras/ICO_DeepSpaceRepairFleetOperation";
                return false;
            } else if (__instance as ClearDebrisOperation != null) {
                __result = "space_ship_extras/ICO_ClearDebrisOperation";
                return false;
            }
            
            return true;
        }
    }

    [HarmonyPatch(typeof(OperationsManager), "Initalize")]
    static class OperationManager_Initalize_Patch {
        static void Postfix() {
            DeepSpaceRepairFleetOperation deepSpaceReapir = new DeepSpaceRepairFleetOperation();
            OperationsManager.fleetOperations.Add(deepSpaceReapir);
            OperationsManager.operationsLookup.Add(deepSpaceReapir.GetType(), deepSpaceReapir);

            ClearDebrisOperation clearDebris = new ClearDebrisOperation();
            OperationsManager.fleetOperations.Add(clearDebris);
            OperationsManager.operationsLookup.Add(clearDebris.GetType(), clearDebris);
        }
    }

    [HarmonyPatch(typeof(TemplateManager), "ValidateAllTemplates")]
    static class TemplateManager_ValidateAllTemplates_Patch {
        // Intercept the method prior execution.
        static void Prefix() {
            TemplateManager.Add<TIShipHullTemplate>(new TIUtilityShipHull());

            TemplateManager.Add<TIDeepSpaceRepairBay>(new TIDeepSpaceRepairBay());
            TemplateManager.Add<TIProjectTemplate>(new TIDeepSpaceMaintenanceProject());

            TemplateManager.Add<TIDebrisCleaner>(new TIDebrisCleaner());
            TemplateManager.Add<TIProjectTemplate>(new TIOrbitalCaptureProject());
        }
    }

    [HarmonyPatch(typeof(TIDriveTemplate), "modelResource")]
    static class TIDriveTemplate_modelResource_Patch {
        static void Postfix(ref string __result) {
            if (__result.Contains("UtilityShip")) {
                __result = __result.Replace("UtilityShip", "Gunship");
            }
        }
    }
}
