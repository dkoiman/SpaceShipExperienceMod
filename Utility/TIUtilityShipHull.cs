using System.Collections.Generic;

using PavonisInteractive.TerraInvicta;

namespace SpaceShipExtras.Utility {
    public class TIUtilityShipHull : TIShipHullTemplate {
        public TIUtilityShipHull() {
            var resource = new ResourceCostBuilder();

            resource.volatiles = 0.1f;
            resource.metals = 0.7f;
            resource.nobleMetals = 0.2f;

            ShipModuleSlot propelantSlot = new ShipModuleSlot();
            propelantSlot.moduleSlotType = ShipModuleSlotType.Propellant;
            propelantSlot.x = 2;
            propelantSlot.y = 0;

            ShipModuleSlot driveSlot = new ShipModuleSlot();
            driveSlot.moduleSlotType = ShipModuleSlotType.Drive;
            driveSlot.x = 2;
            driveSlot.y = 3;

            ShipModuleSlot powerSlot = new ShipModuleSlot();
            powerSlot.moduleSlotType = ShipModuleSlotType.PowerPlant;
            powerSlot.x = 3;
            powerSlot.y = 3;

            ShipModuleSlot batterySlot = new ShipModuleSlot();
            batterySlot.moduleSlotType = ShipModuleSlotType.Battery;
            batterySlot.x = 4;
            batterySlot.y = 3;

            ShipModuleSlot radiatorSlot = new ShipModuleSlot();
            radiatorSlot.moduleSlotType = ShipModuleSlotType.Radiator;
            radiatorSlot.x = 5;
            radiatorSlot.y = 3;

            ShipModuleSlot utilitySlot = new ShipModuleSlot();
            utilitySlot.moduleSlotType = ShipModuleSlotType.Utility;
            utilitySlot.x = 6;
            utilitySlot.y = 3;

            ShipModuleSlot tailArmor = new ShipModuleSlot();
            tailArmor.moduleSlotType = ShipModuleSlotType.TailArmor;
            tailArmor.x = 2;
            tailArmor.y = 7;

            ShipModuleSlot sideArmor = new ShipModuleSlot();
            sideArmor.moduleSlotType = ShipModuleSlotType.LateralArmor;
            sideArmor.x = 4;
            sideArmor.y = 7;

            ShipModuleSlot noseArmor = new ShipModuleSlot();
            noseArmor.moduleSlotType = ShipModuleSlotType.NoseArmor;
            noseArmor.x = 7;
            noseArmor.y = 7;


            base.dataName = "UtilityShip";
            base.friendlyName = "Utility Ship";
            base.noseHardpoints = 0;
            base.hullHardpoints = 0;
            base.internalModules = 1;
            base.length_m = 50;
            base.width_m = 10;
            base.thrusterMultiplier = 1;
            base.structuralIntegrity = 3;
            base.mass_tons = 200;
            base.crew = 3;
            base.alien = false;
            base.monthlyIncome_Money = -1;
            base.missionControl = 1;
            base.shipyardyOffset = new float[] { 0, 0, 0 };
            base.modelResource = new string[] { "ships/Gunship", "ships/Gunship_1" };
            base.combatUIpath = new string[] {
                "ui_spacecombat/OBJ_battle_earth_GS",
                "ui_spacecombat/OBJ_battle_earth_GS_ALT",
            };
            base.path1 = new string[] {
                "earth_GS/",
                "earth_GS_ALT/",
            };
            base.path2 = new string[] {
                "OBJ_battle_earth_GS",
                "OBJ_battle_earth_GS_ALT",
            };
            base.requiredProjectName = "Project_SpaceDock";
            base.weightedBuildMaterials = resource;
            base.baseConstructionTime_days = 60;
            base.shipModuleSlots = new List<ShipModuleSlot> {
                propelantSlot, driveSlot, powerSlot,
                batterySlot, radiatorSlot, utilitySlot,
                tailArmor, sideArmor, noseArmor
            };
        }
    }
}
