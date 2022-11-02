using System.Collections.Generic;
using System.Reflection;

using PavonisInteractive.TerraInvicta;

namespace SpaceShipExtras.Utility {
    // Operation for clearing debris in the current orbit of the fleet.
    class ClearDebrisOperation : TISpaceFleetOperationTemplate {

        // Scan all ships in the fleet to retrieve the number of functional
        // DebrisCleaner modules.
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

        // Returns true if at least one ship in the fleet fits a functional
        // DebrisCleaner module.
        private static bool HasFunctionalDebrisCleaner(TISpaceFleetState fleet) {
            return CountFunctionalDebrisCleaner(fleet) > 0;
        }


        // Returns true if the fleet is in the orbit with present debris.
        private static bool HasDebrisInCurrentOrbit(TISpaceFleetState fleet) {
            var orbit = fleet.ref_orbit;

            if (orbit == null) {
                return false;
            }

            return orbit.destroyedAssets > 0;
        }

        // Clear a single debris cloud, if the orbit has any.
        private static void ClearDebrisInCurrrentOrbit(TISpaceFleetState fleet) {
            var orbit = fleet.ref_orbit;

            if (orbit == null) {
                return;
            }

            if (orbit.destroyedAssets > 0) {
                orbit.destroyedAssets--;
            }
        }

        // Delta-V cost of the operation is 1/1000th of the orbits circumference
        // divided by the number of ships with the functional module.
        private static float NeedDeltaVToExecute(TISpaceFleetState fleet) {
            var orbit = fleet.ref_orbit;

            if (orbit == null) {
                return 0;
            }

            return orbit.circumference_km / 1000 / CountFunctionalDebrisCleaner(fleet);
        }

        // Returns true if the fleets delta-V is sufficient for the operation.
        private static bool FleetHasDeltaVToExecute(TISpaceFleetState fleet) {
            return fleet.currentDeltaV_kps > NeedDeltaVToExecute(fleet);
        }

        // Duration of the operation is 1/1000th of the orbits circumference
        // divided by the number of ships with the functional module.
        private static float TimeToExecute(TISpaceFleetState fleet) {
            var orbit = fleet.ref_orbit;

            if (orbit == null) {
                return 0;
            }

            return orbit.circumference_km / 1000 / CountFunctionalDebrisCleaner(fleet);
        }

        // Show a notification screen upon operation completion.
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

        // Sort it near the end of the list
        public override int SortOrder() {
            return 8;
        }

        // Operation blocks other operations while waiting for resolution.
        public override bool IsBlockingOperation() {
            return true;
        }

        // Operation is visible if there are any ships with the necessary
        // module.
        public override bool OpVisibleToActor(TIGameState actor, TIGameState target = null) {
            return HasFunctionalDebrisCleaner(actor.ref_fleet);
        }

        // Operation is active if there are any ships with the necessary
        // module, and a number of conditions take place.
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

        // Since the targeting is 'self', the actor itself is the only target.
        public override List<TIGameState> GetPossibleTargets(TIGameState actor,
                                                             TIGameState defaultTarget = null) {
            return new List<TIGameState> {
                actor
            };
        }

        // We allow targeting only the current orbit of the ship, which is
        // easily proxied by the fleet itself
        public override System.Type GetTargetingMethod() {
            return typeof(TIOperationTargeting_Self);
        }

        // Duration of the operation.
        public override float GetDuration_days(TIGameState actor,
                                               TIGameState target,
                                               Trajectory trajectory = null) {
            return TimeToExecute(actor.ref_fleet);
        }

        // When operation duration passes, check we are still in the valid state
        // and then process removing debisand display completeion notification.
        public override void ExecuteOperation(TIGameState actor, TIGameState target) {
            TISpaceFleetState fleet = actor.ref_fleet;
            foreach (var ship in fleet.ships) {
                ship.ConsumeDeltaV(NeedDeltaVToExecute(fleet), true, true);
            }
            ClearDebrisInCurrrentOrbit(fleet);
            LogClearDebrisComplete(fleet);
        }

        // Operation execution triggers after its "duration" cost passes.
        public override OperationTiming GetOperationTiming() {
            return OperationTiming.DelayedExecutionOfInstantEffect;
        }

        // Combat interferes with the operation.
        public override bool CancelUponCombat() {
            return true;
        }
    }
}
