using System.Reflection;

using HarmonyLib;
using UnityModManagerNet;

using PavonisInteractive.TerraInvicta.Debugging;
using PavonisInteractive.TerraInvicta.Systems.Bootstrap;


namespace SpaceShipExperienceMod
{
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
