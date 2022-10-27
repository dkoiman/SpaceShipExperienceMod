using System.Reflection;

using HarmonyLib;
using UnityModManagerNet;

namespace SpaceShipExperienceMod
{
    public class Main
    {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;
        public static SpaceShipExperienceManager experienceManager = new SpaceShipExperienceManager();

        static bool Load(UnityModManager.ModEntry modEntry) {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            mod = modEntry;
            modEntry.OnToggle = OnToggle;

            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {
            enabled = value;
            return true;
        }
    }
}
