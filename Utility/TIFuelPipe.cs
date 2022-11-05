using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;

using PavonisInteractive.TerraInvicta;

namespace SpaceShipExtras.Utility {
    public class TIFuelPipe : TIUtilityModuleTemplate {
        public const float kTankSizeTons = 5000;

        public TIFuelPipe() {
            var resource = new ResourceCostBuilder();

            resource.water = 0f;
            resource.volatiles = 0.22f;
            resource.metals = 0.68f;
            resource.nobleMetals = 0.1f;

            base.dataName = "FuelPipe";
            base.friendlyName = "Fuel Pipe";
            base.crew = 10;
            base.mass_tons = 5000;
            base.grouping = -1;
            base.requiredProjectName = "Project_DeepSpaceMaintenance";
            base.powerRequirement_MW = 400;
            base.laserPowerBonus_MW = 0;
            base.thrustMultiplier = 1;
            base.EVMultiplier = 1;
            base.requiresHydrogenPropellant = false;
            base.requiresNuclearDrive = false;
            base.requiresFusionDrive = false;
            base.weightedBuildMaterials = resource;
            base.specialModuleRules = new List<SpecialModuleRule> { };
            base.iconResource = "space_ship_extras/ICO_FuelPipe";
            base.modelResource = "";
        }
    }

    //// The refuel capacity is accounted as wet mass.
    //[HarmonyPatch(typeof(TISpaceShipTemplate), "get_wetMass_tons")]
    //static class TISpaceShipTemplate_get_wetMass_tons_Patch {
    //    static void Postfix(TISpaceShipTemplate __instance, ref float __result) {
    //        foreach (var module in __instance.utilityModules) {
    //            if (module.moduleTemplate as TIFuelPipe != null) {
    //                __result += TIFuelPipe.kTankSizeTons;
    //            }
    //        }
    //    }
    //}

    //// Add the refueling tank mass when initializing ship.
    //[HarmonyPatch(typeof(TISpaceShipState), "InstantFullRepair")]
    //static class TISpaceShipState_InstantFullRepair_Patch {
    //    static void Postfix(ref TISpaceShipState __instance) {
    //        foreach (var module in __instance.utilityModules) {
    //            if (module.moduleTemplate as TIFuelPipe != null) {
    //                __instance
    //                    .GetType()
    //                    .GetProperty("currentMass_kg", BindingFlags.Instance | BindingFlags.NonPublic)
    //                    .SetValue(__instance, __instance.currentMass_kg + TIFuelPipe.kTankSizeTons);
    //                __instance
    //                    .GetType()
    //                    .GetMethod("SetCurrentDeltaVFromPropellantMass", BindingFlags.Instance | BindingFlags.NonPublic)
    //                    .Invoke(__instance, new object[] { });
    //            }
    //        }
    //    }
    //}

    //// Correctly set dV avvounting for Refuel module.
    //[HarmonyPatch(typeof(TISpaceShipState), "SetCurrentDeltaVFromPropellantMass")]
    //static class TISpaceShipState_SetCurrentDeltaVFromPropellantMass_Patch {
    //    static bool Prefix(ref TISpaceShipState __instance) {
    //        foreach (var module in __instance.utilityModules) {
    //            if (module.moduleTemplate as TIFuelPipe != null) {
    //                float currentFunc = __instance.GetPartFunction(module);
    //                double dryMass = __instance.dryMass_tons + currentFunc * TIFuelPipe.kTankSizeTons;
    //                float dv =
    //                    (float)((double)__instance.currentEV_kps *
    //                    UnityEngine.Mathd.Log(((double)__instance.currentMass_kg) / dryMass));
    //                __instance
    //                        .GetType()
    //                        .GetProperty("currentDeltaV_kps", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
    //                        .SetValue(__instance, dv);

    //                return false;
    //            }
    //        }

    //        return true;
    //    }
    //}

    //// We track tank capacity as damage level. Adjust the current mass based on
    //// the change in the damage.
    //[HarmonyPatch(typeof(TISpaceShipState), "SetPartDamage")]
    //static class TISpaceShipState_SetPartDamage_Patch {
    //    static void Prefix(ref TISpaceShipState __instance, ModuleDataEntry module) {
    //        if (module.moduleTemplate as TIFuelPipe != null) {
    //            float currentFunc = __instance.GetPartFunction(module);
    //            float origmass = __instance.currentMass_kg - currentFunc * TIFuelPipe.kTankSizeTons * 1000;
    //            __instance
    //                .GetType()
    //                .GetProperty("currentMass_kg", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
    //                .SetValue(__instance, origmass);
    //        }
    //    }
    //    static void Postfix(ref TISpaceShipState __instance, ModuleDataEntry module) {
    //        if (module.moduleTemplate as TIFuelPipe != null) {
    //            float currentFunc = __instance.GetPartFunction(module);
    //            float newmass = __instance.currentMass_kg + currentFunc * TIFuelPipe.kTankSizeTons * 1000;
    //            __instance
    //                .GetType()
    //                .GetProperty("currentMass_kg", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
    //                .SetValue(__instance, newmass);
    //            __instance
    //                .GetType()
    //                .GetMethod("SetCurrentDeltaVFromPropellantMass", BindingFlags.Instance | BindingFlags.NonPublic)
    //                .Invoke(__instance, new object[] { });
    //        }
    //    }
    //}
}