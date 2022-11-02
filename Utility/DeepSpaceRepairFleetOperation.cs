using System.Collections.Generic;
using System.Reflection;

using PavonisInteractive.TerraInvicta;

namespace SpaceShipExtras.Utility {
    class DeepSpaceRepairFleetOperation : TISpaceFleetOperationTemplate {

        private static int CountFunctionalDeepSpaceRescueBays(TISpaceFleetState fleet) {
            int count = 0;

            foreach (var ship in fleet.ships) {
                foreach (var module in ship.GetFunctionalUtilitySlotModules(1f)) {
                    if (module.moduleTemplate as TIDeepSpaceRepairBay != null) {
                        count++; ;
                    }
                }
            }

            return count;
        }

        private static bool HasFunctionalDeepSpaceRescueBay(TISpaceFleetState fleet) {
            return CountFunctionalDeepSpaceRescueBays(fleet) > 0;
        }

        private static Dictionary<TISpaceShipState, TIResourcesCost> EssentialRepairsCost(TISpaceFleetState fleet) {
            Dictionary<TISpaceShipState, TIResourcesCost> result =
                new Dictionary<TISpaceShipState, TIResourcesCost>();

            foreach (var ship in fleet.ships) {
                TIResourcesCost essentialRepairs = new TIResourcesCost();

                var damagedSystems =
                    ship
                    .GetType()
                    .GetField("damagedSystems", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(ship) as Dictionary<ShipSystem, float>;

                if (damagedSystems.ContainsKey(ShipSystem.DriveCoupling)) {
                    essentialRepairs.SumCosts(ship.SystemRepairCost(ShipSystem.DriveCoupling));
                    essentialRepairs.SetCompletionTime_Days(essentialRepairs.completionTime_days + 6f);
                }

                if (damagedSystems.ContainsKey(ShipSystem.VectorThrusters)) {
                    essentialRepairs.SumCosts(ship.SystemRepairCost(ShipSystem.VectorThrusters));
                    essentialRepairs.SetCompletionTime_Days(essentialRepairs.completionTime_days + 6f);
                }

                foreach (var part in ship.damagedParts) {
                    if (part.module.moduleTemplate.isDrive ||
                        part.module.moduleTemplate.isPowerPlant) {
                        essentialRepairs.SumCosts(ship.PartRepairCost(part.module));
                        essentialRepairs.SetCompletionTime_Days(essentialRepairs.completionTime_days + 3f);
                    }
                }

                if (essentialRepairs.completionTime_days > 0) {
                    result.Add(ship, essentialRepairs);
                }
            }
            return result;
        }

        private static void ScheduleEssentialRepairs(TISpaceShipState ship) {
            var damagedSystems =
                ship
                .GetType()
                .GetField("damagedSystems", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(ship) as Dictionary<ShipSystem, float>;

            if (damagedSystems.ContainsKey(ShipSystem.DriveCoupling)) {
                ship.plannedResupplyAndRepair.AddSystemToRepair(ShipSystem.DriveCoupling);
            }

            if (damagedSystems.ContainsKey(ShipSystem.VectorThrusters)) {
                ship.plannedResupplyAndRepair.AddSystemToRepair(ShipSystem.VectorThrusters);
            }

            foreach (var part in ship.damagedParts) {
                if (part.module.moduleTemplate.isDrive ||
                    part.module.moduleTemplate.isPowerPlant) {
                    ship.plannedResupplyAndRepair.AddModuleToRepair(part);
                }
            }
        }

        private static bool HasEssentialRepairs(TISpaceFleetState fleet) {
            return EssentialRepairsCost(fleet).Count > 0;
        }

        public static void LogOurFleetRepaired(TISpaceFleetState fleet) {
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
            notificationQueueItem.itemHeadline = Loc.T("UI.Notifications.FleetRepairedHed", new object[]
            {
                fleet.GetDisplayName(TINotificationQueueState.activePlayer)
            });
            notificationQueueItem.itemSummary = Loc.T("UI.Notifications.FleetRepairedSummary", new object[]
            {
                fleet.GetDisplayName(TINotificationQueueState.activePlayer),
                fleet.location.GetDisplayName(TINotificationQueueState.activePlayer)
            });
            notificationQueueItem.itemDetail = Loc.T("UI.Notifications.FleetRepairedSummary", new object[]
            {
                fleet.GetDisplayName(TINotificationQueueState.activePlayer),
                fleet.location.GetDisplayName(TINotificationQueueState.activePlayer)
            });
            notificationQueueItem.soundToPlay = "event:/SFX/UI_SFX/trig_SFX_RepairsComplete";

            typeof(TINotificationQueueState)
                .GetMethod("AddItem", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { notificationQueueItem, false, null });
        }

        public static TIResourcesCost ExpectedCost(TISpaceFleetState fleetState) {
            TIResourcesCost tiresourcesCost = new TIResourcesCost();
            var essentialRepairs = EssentialRepairsCost(fleetState);
            foreach (var cost in essentialRepairs) {
                tiresourcesCost.SumCosts(cost.Value);
                tiresourcesCost.SetCompletionTime_Days(tiresourcesCost.completionTime_days + cost.Value.completionTime_days);
            }
            tiresourcesCost.SetCompletionTime_Days(tiresourcesCost.completionTime_days * 4 / CountFunctionalDeepSpaceRescueBays(fleetState));
            return tiresourcesCost;
        }

        // Overrides

        public override OperationTiming GetOperationTiming() {
            return OperationTiming.DelayedExecutionOfInstantEffect;
        }

        public override bool IsBlockingOperation() {
            return true;
        }

        public override bool UseResourceCostDuration() {
            return true;
        }

        public override bool CancelUponCombat() {
            return true;
        }

        public override float GetDuration_days(TIGameState actor, TIGameState target, Trajectory trajectory = null) {
            return -1f;
        }

        public override System.Type GetTargetingMethod() {
            return typeof(TIOperationTargeting_Self);
        }

        public override int SortOrder() {
            return 7;
        }

        public override List<TIGameState> GetPossibleTargets(TIGameState actor, TIGameState defaultTarget = null) {
            return new List<TIGameState> {
                actor
            };
        }

        public override bool OpVisibleToActor(TIGameState actor, TIGameState target = null) {
            return HasFunctionalDeepSpaceRescueBay(actor.ref_fleet);
        }

        public override bool ActorCanPerformOperation(TIGameState actor, TIGameState target) {
            if (!base.ActorCanPerformOperation(actor, target)) {
                return false;
            }
            TISpaceFleetState fleet = actor.ref_fleet;
            return
                HasEssentialRepairs(fleet) &&
                HasFunctionalDeepSpaceRescueBay(fleet) &&
                fleet.CanAffordAnyRepairs() &&
                !fleet.transferAssigned &&
                !fleet.inCombat &&
                !fleet.dockedAtHab;
        }

        public override List<TIResourcesCost> ResourceCostOptions(TIFactionState faction,
                                                                  TIGameState target,
                                                                  TIGameState actor,
                                                                  bool checkCanAfford = true) {
            if (actor.isSpaceFleetState) {
                return new List<TIResourcesCost>(1) {
                    DeepSpaceRepairFleetOperation.ExpectedCost(actor.ref_fleet)
                };
            }
            return null;
        }

        public override bool HasResourceCost() {
            return true;
        }

        public override bool OnOperationConfirm(TIGameState actor,
                                                TIGameState target,
                                                TIResourcesCost resourcesCost = null,
                                                Trajectory trajectory = null) {
            TISpaceFleetState fleet = actor.ref_fleet;
            TIResourcesCost tiresourcesCost = new TIResourcesCost();
            var essentialRepairs = EssentialRepairsCost(fleet);

            foreach (var cost in essentialRepairs) {
                if (!cost.Value.CanAfford(cost.Key.faction, 1f, null, float.PositiveInfinity)) {
                    continue;
                }

                ScheduleEssentialRepairs(cost.Key);
                tiresourcesCost.SumCosts(cost.Value);
                tiresourcesCost.SetCompletionTime_Days(
                    tiresourcesCost.completionTime_days + cost.Value.completionTime_days); 
            }

            tiresourcesCost.SetCompletionTime_Days(
                tiresourcesCost.completionTime_days * 4 / CountFunctionalDeepSpaceRescueBays(fleet));

            return base.OnOperationConfirm(actor, target, tiresourcesCost, trajectory);
        }

        public override void ExecuteOperation(TIGameState actor, TIGameState target) {
            TISpaceFleetState fleet = actor.ref_fleet;

            if (HasFunctionalDeepSpaceRescueBay(fleet) &&
                fleet.CanAffordAnyRepairs() &&
                !fleet.inCombat &&
                !fleet.transferAssigned &&
                !fleet.dockedAtHab) {
                foreach (var ship in fleet.ships) {
                    ship.plannedResupplyAndRepair.ProcessResupplyAndRepair(ship);
                }
                LogOurFleetRepaired(fleet);
                return;
            }

            foreach (var ship in fleet.ships) {
                ship.plannedResupplyAndRepair.CancelRepair(fleet.faction);
            }
        }

        public override void OnOperationCancel(TIGameState actor,
                                               TIGameState target,
                                               TIDateTime opCompleteDate) {
            base.OnOperationCancel(actor, target, opCompleteDate);

            foreach (var ship in actor.ref_fleet.ships) {
                ship.plannedResupplyAndRepair.CancelRepair(actor.ref_faction);
            };
        }
    }
}
