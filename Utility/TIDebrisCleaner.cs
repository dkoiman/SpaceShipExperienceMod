using System.Collections.Generic;

using PavonisInteractive.TerraInvicta;

namespace SpaceShipExtras.Utility {
    public class TIDebrisCleaner : TIUtilityModuleTemplate {
        public TIDebrisCleaner() {
            var resource = new ResourceCostBuilder();

            resource.water = 0f;
            resource.volatiles = 0.22f;
            resource.metals = 0.68f;
            resource.nobleMetals = 0.1f;

            base.dataName = "DebrisCleaner";
            base.friendlyName = "Debris Cleaner";
            base.crew = 10;
            base.mass_tons = 40;
            base.grouping = -1;
            base.requiredProjectName = "Project_HardenedHabShelters";
            base.powerRequirement_MW = 400;
            base.laserPowerBonus_MW = 0;
            base.thrustMultiplier = 1;
            base.EVMultiplier = 1;
            base.requiresHydrogenPropellant = false;
            base.requiresNuclearDrive = false;
            base.requiresFusionDrive = false;
            base.weightedBuildMaterials = resource;
            base.specialModuleRules = new List<SpecialModuleRule> { };
            base.iconResource = "space_ship_extras/ICO_DebrisCleaner";
            base.modelResource = "";
        }
    }
}