using System.Reflection;

using HarmonyLib;
using UnityModManagerNet;

using PavonisInteractive.TerraInvicta;
using PavonisInteractive.TerraInvicta.Debugging;
using PavonisInteractive.TerraInvicta.Systems.Bootstrap;

using SpaceShipExtras.ShipExperience;

// Hack for save compatibility.
namespace SpaceShipExperienceMod {
    public class TISpaceShipExperienceState : SpaceShipExtras.ShipExperience.TISpaceShipExperienceState {
        public override void PostInitializationInit_4() {
            SpaceShipExtras.ShipExperience.TISpaceShipExperienceState exp =
                GameStateManager.CreateNewGameState<SpaceShipExtras.ShipExperience.TISpaceShipExperienceState>();
            GameStateManager.RemoveGameState<TISpaceShipExperienceState>(this.ID);
            exp.AddExperience(this.experience);
            exp.ref_ship = this.ref_ship;
            SpaceShipExtras.Main.experienceManager.RegisterShip(this.ref_ship, exp);
        }
    }
}

namespace SpaceShipExtras {
    public class Main
    {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;
        public static SpaceShipExperienceManager experienceManager = new SpaceShipExperienceManager();
        public static SpaceShipExperienceConsoleCommand terminalBindingHolder;

        static bool Load(UnityModManager.ModEntry modEntry) {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            mod = modEntry;
            modEntry.OnToggle = OnToggle;

            var container = GlobalInstaller.container;
            var terminalController = container.Resolve<Terminal>().controller;
            terminalBindingHolder = new SpaceShipExperienceConsoleCommand(terminalController);

            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {
            enabled = value;
            return true;
        }
    }
}
