using System.Collections.Generic;

using PavonisInteractive.TerraInvicta;

namespace SpaceShipExtras.Utility {
    public class TIOrbitalCaptureProject : TIProjectTemplate {
        public TIOrbitalCaptureProject() {
            base.dataName = "Project_OrbitalCapture";
            base.friendlyName = "Orbital Capture";
            base.techCategory = TechCategory.SpaceScience;
            base.AI_techRole = TechRole.None;
            base.AI_criticalTech = false;
            base.AI_projectRole = ProjectRole.None;
            base.researchCost = 5000;
            base.prereqs = new string[] {
                "Project_SpaceDock",
                "AppliedArtificialIntelligence",
            };
            base.altPrereq0 = "";
            base.requiredObjectiveNames = new List<string> { "" };
            base.altRequiredObjective0Name = "";
            base.requiredMilestone = CampaignMilestone.None;
            base.effects = new string[] { };
            base.oneTimeGlobally = false;
            base.repeatable = false;
            base.requiresNation = "";
            base.requiredControlPoint = new List<string> { };
            base.factionPrereq = new List<string> { };
            base.factionAvailableChance = 100;
            base.factionAlways = "";
            base.initialUnlockChance = 0;
            base.deltaUnlockChance = 20;
            base.maxUnlockChance = 100;
            base.orgGranted = "";
            base.resourcesGranted = new ResourceValue[] { };
            base.iconResource = "";
            base.completedIllustrationPath = "";
        }
    }
}
