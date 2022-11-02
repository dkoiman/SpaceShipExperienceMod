using System.Collections.Generic;
using System.Reflection;

using PavonisInteractive.TerraInvicta;

namespace SpaceShipExtras.Utility {
    // Operation for performing essential repairs in deep space.
    class DeepSpaceRepairFleetOperation : TISpaceFleetOperationTemplate {

        // Deep space repair take longer base time than repairs at habs.
        private const float kRepairTimeMultipleier = 4;

        // Scan all ships in the fleet to retrieve the number of functional
        // DeepSpaceRepaireBay modules.
        private static int CountFunctionalDeepSpaceRepairBays(TISpaceFleetState fleet) {
            int count = 0;

            foreach (var ship in fleet.ships) {
                foreach (var module in ship.GetFunctionalUtilitySlotModules(1f)) {
                    if (module.moduleTemplate as TIDeepSpaceRepairBay != null) {
                        count++;
                    }
                }
            }

            return count;
        }

        // Returns true if at least one ship in the fleet fits a functional
        // DeepSpaceRepaireBay module.
        private static bool HasFunctionalDeepSpaceRepairBay(TISpaceFleetState fleet) {
            return CountFunctionalDeepSpaceRepairBays(fleet) > 0;
        }

        // Calculate cost of essentials repairs for each ship in the fleet.
        // Essential are the repairs for the internal systems and modules that
        // are necessary for initiating orbital transfer of ships. Currently
        // that includes
        // * Drive Coupling subsystem
        // * Drive module
        // * Power Plant
        // * Vector Thrusters subsystem - techically not required, but that is
        //   likely a bug.
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

                // Each subsystem's base time-to-repair is 6 days in the vanilla game.
                if (damagedSystems.ContainsKey(ShipSystem.DriveCoupling)) {
                    essentialRepairs.SumCosts(ship.SystemRepairCost(ShipSystem.DriveCoupling));
                    essentialRepairs.SetCompletionTime_Days(essentialRepairs.completionTime_days + 6f);
                }

                if (damagedSystems.ContainsKey(ShipSystem.VectorThrusters)) {
                    essentialRepairs.SumCosts(ship.SystemRepairCost(ShipSystem.VectorThrusters));
                    essentialRepairs.SetCompletionTime_Days(essentialRepairs.completionTime_days + 6f);
                }

                // Each module's base time-to-repair is 3 days in the vanilla game.
                foreach (var part in ship.damagedParts) {
                    if (part.module.moduleTemplate.isDrive ||
                        part.module.moduleTemplate.isPowerPlant) {
                        essentialRepairs.SumCosts(ship.PartRepairCost(part.module));
                        essentialRepairs.SetCompletionTime_Days(essentialRepairs.completionTime_days + 3f);
                    }
                }

                // Include the ship into the resulting list if the ship has
                // damage to any essential subsystems or modules.
                if (essentialRepairs.completionTime_days > 0 &&
                    essentialRepairs.CanAfford(ship.faction, 1f, null, float.PositiveInfinity)) {
                    result.Add(ship, essentialRepairs);
                }
            }
            return result;
        }


        // Mark all damaged essential systems as scheduled for repair.
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

        // Returns true if at least one ship in the fleet has damage to
        // essential systems.
        private static bool HasEssentialRepairs(TISpaceFleetState fleet) {
            return EssentialRepairsCost(fleet).Count > 0;
        }

        // Show a notification screen upon operation completion.
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

        // Evaluate the cost of essential repairs. Note, the vanilla game has
        // some strange behaviour, where affordability of repairs is calculated
        // in disconnect with total accumulated cost so far. We are pretty much
        // doing the same.
        public static TIResourcesCost ExpectedCost(TISpaceFleetState fleetState) {
            TIResourcesCost totalCost = new TIResourcesCost();
            var essentialRepairs = EssentialRepairsCost(fleetState);

            foreach (var cost in essentialRepairs) {
                totalCost.SumCosts(cost.Value);
                totalCost.SetCompletionTime_Days(
                    totalCost.completionTime_days + cost.Value.completionTime_days);
            }

            totalCost.SetCompletionTime_Days(
                totalCost.completionTime_days * kRepairTimeMultipleier /
                CountFunctionalDeepSpaceRepairBays(fleetState));

            return totalCost;
        }

        // Overrides

        // Sort it next ot vanilla repair button.
        public override int SortOrder() {
            return 7;
        }

        // Operation blocks other operations while waiting for resolution.
        public override bool IsBlockingOperation() {
            return true;
        }

        // The operation has resource cost.
        public override bool HasResourceCost() {
            return true;
        }

        // Operation is visible if there are any ships with the necessary
        // module.
        public override bool OpVisibleToActor(TIGameState actor, TIGameState target = null) {
            return HasFunctionalDeepSpaceRepairBay(actor.ref_fleet);
        }

        // Operation is active if there are any ships with the necessary
        // module, and a number of conditions take place. Repairs at hab
        // supercede module's reapirs.
        public override bool ActorCanPerformOperation(TIGameState actor, TIGameState target) {
            TISpaceFleetState fleet = actor.ref_fleet;

            if (!base.ActorCanPerformOperation(actor, target)) {
                return false;
            }

            return
                HasFunctionalDeepSpaceRepairBay(fleet) &&
                HasEssentialRepairs(fleet) &&
                fleet.CanAffordAnyRepairs() &&
                !fleet.transferAssigned &&
                !fleet.inCombat &&
                (!fleet.dockedAtHab ||
                 fleet.ref_hab.AllowsShipConstruction(fleet.faction, false, false));
        }

        // Since the targeting is 'self', the actor itself is the only target.
        public override List<TIGameState> GetPossibleTargets(TIGameState actor,
                                                             TIGameState defaultTarget = null) {
            return new List<TIGameState> {
                actor
            };
        }

        // The fleet itself is a target of the operation.
        public override System.Type GetTargetingMethod() {
            return typeof(TIOperationTargeting_Self);
        }

        // This one is ignored when duration is taken from cost, but has to be
        // overriden, because it is an abstract method.
        public override float GetDuration_days(TIGameState actor,
                                               TIGameState target,
                                               Trajectory trajectory = null) {
            return -1f;
        }

        // Operation's duration should be retrieved from the resource cost.
        public override bool UseResourceCostDuration() {
            return true;
        }

        // The only cost option for the operation is calculated based on the
        // required repairs.
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

        // Schedule available essential repairs. Note, the vanilla game has
        // some strange behaviour, where affordability of repairs is calculated
        // in disconnect with total accumulated cost so far. We are pretty much
        // doing the same.
        public override bool OnOperationConfirm(TIGameState actor,
                                                TIGameState target,
                                                TIResourcesCost resourcesCost = null,
                                                Trajectory trajectory = null) {
            TISpaceFleetState fleet = actor.ref_fleet;
            TIResourcesCost totalCost = new TIResourcesCost();
            var essentialRepairs = EssentialRepairsCost(fleet);

            foreach (var cost in essentialRepairs) {
                if (!cost.Value.CanAfford(cost.Key.faction, 1f, null, float.PositiveInfinity)) {
                    continue;
                }

                ScheduleEssentialRepairs(cost.Key);
                totalCost.SumCosts(cost.Value);
                totalCost.SetCompletionTime_Days(
                    totalCost.completionTime_days + cost.Value.completionTime_days); 
            }

            totalCost.SetCompletionTime_Days(
                totalCost.completionTime_days * kRepairTimeMultipleier /
                CountFunctionalDeepSpaceRepairBays(fleet));

            return base.OnOperationConfirm(actor, target, totalCost, trajectory);
        }

        // When operation duration passes, check we are still in the valid state
        // and then process repairs and display completeion notification.
        public override void ExecuteOperation(TIGameState actor, TIGameState target) {
            TISpaceFleetState fleet = actor.ref_fleet;

            if (HasFunctionalDeepSpaceRepairBay(fleet) &&
                !fleet.inCombat &&
                !fleet.transferAssigned &&
                !fleet.dockedAtHab) {
                foreach (var ship in fleet.ships) {
                    ship.plannedResupplyAndRepair.ProcessResupplyAndRepair(ship);
                }
                LogOurFleetRepaired(fleet);
                return;
            }

            // If state is no longer valid, undo all repairs.
            foreach (var ship in fleet.ships) {
                ship.plannedResupplyAndRepair.CancelRepair(fleet.faction);
            }
        }

        // If operation is canceled, undo all repairs.
        public override void OnOperationCancel(TIGameState actor,
                                               TIGameState target,
                                               TIDateTime opCompleteDate) {
            base.OnOperationCancel(actor, target, opCompleteDate);

            foreach (var ship in actor.ref_fleet.ships) {
                ship.plannedResupplyAndRepair.CancelRepair(actor.ref_faction);
            };
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
