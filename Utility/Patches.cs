using HarmonyLib;

using PavonisInteractive.TerraInvicta;

namespace SpaceShipExtras.Utility {

    [HarmonyPatch(typeof(TISpaceShipTemplate), "ValidPartForDesign")]
    static class TISpaceShipTemplate_ValidPartForDesign_Patch {
        static bool Prefix(TISpaceShipTemplate __instance, TIShipPartTemplate part, ref bool __result) {
            if (part as TIDeepSpaceRepairBay != null &&
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
            if (__instance as DeepSpaceRepairFleetOperation == null) {
                return true;
            }
            __result = "space_ship_extras/ICO_DeepSpaceRepairFleetOperation";
            return false;
        }
    }

    [HarmonyPatch(typeof(OperationsManager), "Initalize")]
    static class OperationManager_Initalize_Patch {
        static void Postfix() {
            DeepSpaceRepairFleetOperation operation = new DeepSpaceRepairFleetOperation();
            OperationsManager.fleetOperations.Add(operation);
            OperationsManager.operationsLookup.Add(operation.GetType(), operation);
        }
    }

    [HarmonyPatch(typeof(TemplateManager), "ValidateAllTemplates")]
    static class TemplateManager_ValidateAllTemplates_Patch {
        // Intercept the method prior execution.
        static void Prefix() {
            TemplateManager.Add<TIDeepSpaceRepairBay>(new TIDeepSpaceRepairBay());
            TemplateManager.Add<TIShipHullTemplate>(new TIUtilityShipHull());
        }
    }
}
