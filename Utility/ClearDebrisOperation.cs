using System.Collections.Generic;
using System.Reflection;

using PavonisInteractive.TerraInvicta;

namespace SpaceShipExtras.Utility {
    class ClearDebrisOperation : TISpaceFleetOperationTemplate {
        private static int CountFunctionalDebrisCleaner(TISpaceFleetState fleetState) {
            int count = 0;

            foreach (var ship in fleetState.ref_fleet.ships) {
                foreach (var module in ship.GetFunctionalUtilitySlotModules(1f)) {
                    if (module.moduleTemplate as TIDebrisCleaner != null) {
                        count++; ;
                    }
                }
            }

            return count;
        }

        private static bool HasFunctionalDebrisCleaner(TISpaceFleetState fleetState) {
            return CountFunctionalDebrisCleaner(fleetState) > 0;
        }

        private static bool HasDebrisInCurrentOrbit(TISpaceFleetState fleetState) {
            var orbit = fleetState.ref_orbit;
            if (orbit == null) {
                return false;
            }

            return orbit.destroyedAssets > 0;
        }

        private static void ClearDebrisInCurrrentOrbit(TISpaceFleetState fleetState) {
            var orbit = fleetState.ref_orbit;
            if (orbit == null) {
                return;
            }

            if (orbit.destroyedAssets > 0) {
                orbit.destroyedAssets--;
            }
        }

        private static float NeedDeltaVToExecute(TISpaceFleetState fleetState) {
            var orbit = fleetState.ref_orbit;
            if (orbit == null) {
                return 0;
            }

            return orbit.circumference_km / 1000 / CountFunctionalDebrisCleaner(fleetState);
        }

        private static bool FleetHasDeltaVToExecute(TISpaceFleetState fleetState) {
            return fleetState.currentDeltaV_kps > NeedDeltaVToExecute(fleetState);
        }

        private static float TimeToExecute(TISpaceFleetState fleetState) {
            var orbit = fleetState.ref_orbit;
            if (orbit == null) {
                return 0;
            }

            return orbit.circumference_km / 1000 / CountFunctionalDebrisCleaner(fleetState);
        }

        private static void LogClearDebrisCOmplete(TISpaceFleetState fleet) {
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

        public override bool RequiresThrustProfile() {
            return false;
        }

        public override bool OpVisibleToActor(TIGameState actorState, TIGameState targetState = null) {
            TISpaceFleetState ref_fleet = actorState.ref_fleet;
            return HasFunctionalDebrisCleaner(ref_fleet);
        }

        public override bool ActorCanPerformOperation(TIGameState actorState, TIGameState targetState = null) {
            if (!base.ActorCanPerformOperation(actorState, targetState)) {
                return false;
            }
            TISpaceFleetState ref_fleet = actorState.ref_fleet;
            return (
                HasFunctionalDebrisCleaner(ref_fleet) &&
                HasDebrisInCurrentOrbit(ref_fleet) &&
                FleetHasDeltaVToExecute(ref_fleet) &&
                !ref_fleet.transferAssigned &&
                !ref_fleet.inCombat);
        }

        public override List<TIGameState> GetPossibleTargets(TIGameState actorState, TIGameState defaultTarget = null) {
            return new List<TIGameState> {
                actorState
            };
        }

        public override System.Type GetTargetingMethod() {
            return typeof(TIOperationTargeting_Self);
        }


        public override float GetDuration_days(TIGameState actorState, TIGameState target, Trajectory trajectory = null) {
            TISpaceFleetState ref_fleet = actorState.ref_fleet;
            return TimeToExecute(ref_fleet);
        }

        public override void ExecuteOperation(TIGameState actorState, TIGameState target) {
            TISpaceFleetState ref_fleet = actorState.ref_fleet;
            ref_fleet.ships.ForEach(delegate (TISpaceShipState x) {
                x.ConsumeDeltaV(NeedDeltaVToExecute(ref_fleet), true, true);
            });
            ClearDebrisInCurrrentOrbit(ref_fleet);
            LogClearDebrisCOmplete(ref_fleet);
        }

        public override OperationTiming GetOperationTiming() {
            return OperationTiming.DelayedExecutionOfInstantEffect;
        }

        public override bool CancelUponCombat() {
            return true;
        }
    }
}
