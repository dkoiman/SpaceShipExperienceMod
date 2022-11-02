using System.Text;
using System.Reflection;

using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using PavonisInteractive.TerraInvicta.SpaceCombat.UI;
using PavonisInteractive.TerraInvicta.Ship;

namespace SpaceShipExtras.ShipExperience {
    // Hovering over ship pictogram upon selecting friendly or enemy fleet.
    [HarmonyPatch(typeof(FleetShipGridItemController), "BuildShipTooltip")]
    static class FleetShipGridItemController_BuildShipTooltip_Patch {
        static bool Prefix(TISpaceShipState ship, ref string __result) {
            __result = Main.experienceManager.GetNameRankString(ship);
            return false;
        }
    }

    // A list of ships upon selecting friendly or enemy fleet.
    [HarmonyPatch(typeof(ShipsInFleetListItemController), "SetListItem")]
    static class ShipsInFleetListItemController_SetListItem_Patch {
        static void Postfix(ref ShipsInFleetListItemController __instance, TISpaceShipState shipState) {
            __instance.shipName.SetText(Main.experienceManager.GetNameRankString(shipState), true);
        }
    }

    // Split fleet canvas
    [HarmonyPatch(typeof(SplitFleetShipListItemController), "SetListItem")]
    static class SplitFleetShipListItemController_SetListItem_Patch {
        static void Postfix(ref SplitFleetShipListItemController __instance, TISpaceShipState ship) {
            __instance.shipName.SetText(Loc.T("UI.Operations.ShipItem", new object[]
            {
                Main.experienceManager.GetNameRankString(ship, true),
                ship.template.fullDisplayName
            }), true);
        }
    }

    // `Fleets and ship construction` canvas.
    [HarmonyPatch(typeof(FleetsSceenFleetListItemController), "UpdateListItem")]
    static class FleetsSceenFleetListItemController_UpdateListItem_Patch {
        static void Postfix(ref FleetsSceenFleetListItemController __instance, TIGameState gameState) {
            if (!gameState.isSpaceShipState) {
                return;
            }

            __instance.shipName.SetText(Main.experienceManager.GetNameRankString(__instance.ship), true);
        }
    }

    // Left panel in detailed ship info.
    [HarmonyPatch(typeof(ShipScreenShipListItemController), "SetListItem")]
    static class ShipScreenShipListItemController_SetListItem_Patch {
        static void Postfix(ref ShipScreenShipListItemController __instance, TISpaceShipState ship) {
            __instance.shipName.SetText(Main.experienceManager.GetNameRankString(ship), true);
        }
    }

    // Left panel in detailed ship info.
    [HarmonyPatch(typeof(ShipScreenShipListItemController), "UpdateNames")]
    static class ShipScreenShipListItemController_UpdateNames_Patch {
        static void Postfix(ref ShipScreenShipListItemController __instance, TISpaceShipState ship) {
            __instance.shipName.SetText(Main.experienceManager.GetNameRankString(ship), true);
        }
    }

    // Docked ships list
    [HarmonyPatch(typeof(DockedShipListItemController), "UpdateListItem")]
    static class DockedShipListItemController_UpdateListItem_Patch {
        static void Postfix(ref DockedShipListItemController __instance) {
            TISpaceShipState ship =
                __instance
                .GetType()
                .GetField("shipState", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(__instance) as TISpaceShipState;

            __instance.shipName.SetText(Main.experienceManager.GetNameRankString(ship), true);
        }
    }

    // Current ship name in selected ship detailed info.
    [HarmonyPatch(typeof(FleetsScreenController), "UpdateIndividualDataScreen")]
    static class FleetsScreenController_UpdateIndividualDataScreen_Patch {
        static void Postfix(ref FleetsScreenController __instance) {
            __instance.indiv_ShipName.SetText(Main.experienceManager.GetNameRankString(__instance.selectedShip), true);
        }
    }

    // Current ship name in selected ship detailed info.
    [HarmonyPatch(typeof(FleetsScreenController), "OnClickSaveName")]
    static class FleetsScreenController_OnClickSaveName_Patch {
        static void Postfix(ref FleetsScreenController __instance) {
            __instance.indiv_ShipName.SetText(Main.experienceManager.GetNameRankString(__instance.selectedShip), true);
        }
    }

    // In-space Ship model highlight tooltip.
    [HarmonyPatch(typeof(ShipUIController), "Update")]
    static class ShipUIController_Update_Patch {
        static void Postfix(ShipUIController __instance) {
            TISpaceShipState ship =
                __instance
                .GetType()
                .GetProperty("ship", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(__instance) as TISpaceShipState;

            __instance.shipName.SetText(Main.experienceManager.GetNameRankString(ship), true);
        }
    }


    // Precombat panel
    [HarmonyPatch(typeof(PrecombatShipListItemController), "SetListItem")]
    static class PrecombatShipListItemController_SetListItem_Patch {
        static void Postfix(ref PrecombatShipListItemController __instance, TIGameState item) {
            TISpaceShipState ship = item as TISpaceShipState;
            if (ship == null) {
                return;
            }

            __instance.shipName.SetText(Main.experienceManager.GetNameRankString(ship), true);
        }
    }

    // Postcombat panel
    [HarmonyPatch(typeof(PostcombatShipListItemController), "SetListItem")]
    static class PostcombatShipListItemController_SetListItem_Patch {
        static void Postfix(ref PostcombatShipListItemController __instance, CombatRecord.SingleAssetCombatRecord item) {
            if (item.asset == null || !item.asset.isSpaceShipState) {
                return;
            }

            __instance.itemName.SetText(Main.experienceManager.GetNameRankString(item.asset.ref_ship), true);
        }
    }

    // Combat ship list
    [HarmonyPatch(typeof(CombatantListItemController), "Init")]
    static class CombatantListItemController_Init_Patch {
        static void Postfix(ref CombatantListItemController __instance) {
            if (__instance.combatantType != IDamageableType.Ship) {
                return;
            }

            TISpaceShipState ship =
                __instance
                .GetType()
                .GetField("shipState", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(__instance) as TISpaceShipState;

            __instance.shipName.SetText(
                new StringBuilder(ship.hull.displayName)
                .Append(" ")
                .Append(Main.experienceManager.GetNameRankString(ship)));
        }
    }

    // Combat selected ship
    [HarmonyPatch(typeof(SpaceCombatCanvasController), "SetSelectedShipPanel")]
    static class SpaceCombatCanvasController_SetSelectedShipPanel_Patch {
        static void Postfix(ref SpaceCombatCanvasController __instance) {
            TISpaceShipState ship = __instance.selectedFriendlyShipState;

            __instance.selectedShipName.SetText(
                new StringBuilder(ship.hull.displayName)
                .Append(" ")
                .Append(Main.experienceManager.GetNameRankString(ship)));
        }
    }

    // SpaceCombatCanvasController reinforcement
    // FriendlyShipListItemController targeting
    // CombatRecord ???
}
