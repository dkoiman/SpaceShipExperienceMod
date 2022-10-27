using System.Collections.Generic;

using PavonisInteractive.TerraInvicta;


namespace SpaceShipExtras {
    public class TIDeepSpaceRepairBay : TIUtilityModuleTemplate {
        public TIDeepSpaceRepairBay() {
            var resource = new ResourceCostBuilder();

            resource.water = 0f;
            resource.volatiles = 0.22f;
            resource.metals = 0.68f;
            resource.nobleMetals = 0.1f;

            base.dataName = "DeepSpaceRepairBay";
            base.friendlyName = "Deep Space Repair Bay";
            base.crew = 100;
            base.mass_tons = 400;
            base.grouping = -1;
            base.requiredProjectName = "Project_Shipyard";
            base.powerRequirement_MW = 40000;
            base.laserPowerBonus_MW = 0;
            base.thrustMultiplier = 1;
            base.EVMultiplier = 1;
            base.requiresHydrogenPropellant = false;
            base.requiresNuclearDrive = false;
            base.requiresFusionDrive = false;
            base.weightedBuildMaterials = resource;
            base.specialModuleRules = new List<SpecialModuleRule> { };
            base.iconResource = "space_ship_extras/ICO_DeepSpaceRepairBay";
            base.modelResource = "";
        }
    }
}