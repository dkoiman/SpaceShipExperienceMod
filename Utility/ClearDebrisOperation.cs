using System.Collections.Generic;
using System.Reflection;

using PavonisInteractive.TerraInvicta;

namespace SpaceShipExtras.Utility {
    class ClearDebrisOperation : TISpaceFleetOperationTemplate {
        private static int CountFunctionalDebrisCleaner(TISpaceFleetState fleet) {
            int count = 0;

            foreach (var ship in fleet.ships) {
                foreach (var module in ship.GetFunctionalUtilitySlotModules(1f)) {
                    if (module.moduleTemplate as TIDebrisCleaner != null) {
                        count++; ;
                    }
                }
            }

            return count;
        }

        private static bool HasFunctionalDebrisCleaner(TISpaceFleetState fleet) {
            return CountFunctionalDebrisCleaner(fleet) > 0;
        }

        private static bool HasDebrisInCurrentOrbit(TISpaceFleetState fleet) {
            var orbit = fleet.ref_orbit;

            if (orbit == null) {
                return false;
            }

            return orbit.destroyedAssets > 0;
        }

        private static void ClearDebrisInCurrrentOrbit(TISpaceFleetState fleet) {
            var orbit = fleet.ref_orbit;

            if (orbit == null) {
                return;
            }

            if (orbit.destroyedAssets > 0) {
                orbit.destroyedAssets--;
            }
        }

        private static float NeedDeltaVToExecute(TISpaceFleetState fleet) {
            var orbit = fleet.ref_orbit;

            if (orbit == null) {
                return 0;
            }

            return orbit.circumference_km / 1000 / CountFunctionalDebrisCleaner(fleet);
        }

        private static bool FleetHasDeltaVToExecute(TISpaceFleetState fleet) {
            return fleet.currentDeltaV_kps > NeedDeltaVToExecute(fleet);
        }

        private static float TimeToExecute(TISpaceFleetState fleet) {
            var orbit = fleet.ref_orbit;

            if (orbit == null) {
                return 0;
            }

            return orbit.circumference_km / 1000 / CountFunctionalDebrisCleaner(fleet);
        }

        private static void LogClearDebrisComplete(TISpaceFleetState fleet) {
            NotificationQueueItem notificationQueueItem =
                typeof(TINotificationQueueState)
                .GetMethod("InitItem", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { }) as NotificationQueueItem;

            notificationQueueItem.alertFactions.Add(fleet.faction);
            notificationQueueItem.logFactions = notificationQueueItem.alertFactions;
            notificationQueueItem.icon = fleet.iconResource;
            notificationQueueItem.itemHammer = Loc.T("UI.Notifications.MissionControl");
            notificationQueueItem.popupResource1 = fleet.iconResource;
            notificationQueueItem.gotoGameState = fleet;
            notificationQueueItem.itemHeadline = Loc.T("UI.Notifications.DebrisCleared.Headline", new object[]
            {
                fleet.faction.adjective,
                fleet.GetDisplayName(TINotificationQueueState.activePlayer)
            });
            notificationQueueItem.itemSummary = Loc.T("UI.Notifications.DebrisCleared.Summary", new object[]
            {
                fleet.faction.adjectiveWithColor,
                fleet.GetDisplayName(TINotificationQueueState.activePlayer),
                TIUtilities.GetLocationString(fleet.location, true, true)
            });
            notificationQueueItem.itemDetail = Loc.T("UI.Notifications.DebrisCleared.Detail", new object[]
            {
                fleet.faction.adjectiveWithColor,
                fleet.GetDisplayName(TINotificationQueueState.activePlayer),
                TIUtilities.GetLocationString(fleet.location, true, true)
            });

            typeof(TINotificationQueueState)
                .GetMethod("AddItem", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { notificationQueueItem, false, null });
        }

        // Overrides

        public override int SortOrder() {
            return 8;
        }

        public override bool IsBlockingOperation() {
            return true;
        }

        public override bool OpVisibleToActor(TIGameState actor, TIGameState target = null) {
            return HasFunctionalDebrisCleaner(actor.ref_fleet);
        }

        public override bool ActorCanPerformOperation(TIGameState actor, TIGameState target = null) {
            TISpaceFleetState fleet = actor.ref_fleet;

            if (!base.ActorCanPerformOperation(actor, target)) {
                return false;
            }
            
            return (
                HasFunctionalDebrisCleaner(fleet) &&
                HasDebrisInCurrentOrbit(fleet) &&
                FleetHasDeltaVToExecute(fleet) &&
                !fleet.transferAssigned &&
                !fleet.inCombat);
        }

        public override List<TIGameState> GetPossibleTargets(TIGameState actor,
                                                             TIGameState defaultTarget = null) {
            return new List<TIGameState> {
                actor
            };
        }

        public override System.Type GetTargetingMethod() {
            return typeof(TIOperationTargeting_Self);
        }


        public override float GetDuration_days(TIGameState actor,
                                               TIGameState target,
                                               Trajectory trajectory = null) {
            return TimeToExecute(actor.ref_fleet);
        }

        public override void ExecuteOperation(TIGameState actor, TIGameState target) {
            TISpaceFleetState fleet = actor.ref_fleet;
            foreach (var ship in fleet.ships) {
                ship.ConsumeDeltaV(NeedDeltaVToExecute(fleet), true, true);
            }
            ClearDebrisInCurrrentOrbit(fleet);
            LogClearDebrisComplete(fleet);
        }

        public override OperationTiming GetOperationTiming() {
            return OperationTiming.DelayedExecutionOfInstantEffect;
        }

        public override bool CancelUponCombat() {
            return true;
        }
    }
}
