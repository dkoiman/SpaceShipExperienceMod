using System.Collections.Generic;
using System.Reflection;

using Unity.Entities;
using PavonisInteractive.TerraInvicta;
using PavonisInteractive.TerraInvicta.Systems.Camera;

namespace SpaceShipExtras.Utility {
    class RefuelOperation : TISpaceFleetOperationTemplate {
        private const float kRefuelTimeMultiplier = 4f;

        // Returns dictionary of ships that need fuel and amount of the fuel
        // they miss.
        private static Dictionary<TISpaceShipState, float> GetShipsToRefuel(TISpaceFleetState fleet) {
            Dictionary<TISpaceShipState, float>  ships = new Dictionary<TISpaceShipState, float>();

            foreach (var ship in fleet.ships) {
                if (ship.NeedsRefuel()) {
                    ships.Add(ship, ship.PropellantShortage_tons);
                }
            }

            return ships;
        }

        // Scan all ships in the fleet to retrieve the number of functional
        // FuelPipes modules.
        private static int CountFunctionalFuelPipes(TISpaceFleetState fleet) {
            int count = 0;

            foreach (var ship in fleet.ships) {
                foreach (var module in ship.GetFunctionalUtilitySlotModules(1f)) {
                    if (module.moduleTemplate as TIFuelPipe != null) {
                        count++;
                    }
                }
            }

            return count;
        }

        // Scan all ships in the fleet to retrieve the total mass of fuel
        // functional FuelPipes can supply. Each fuel pipe can resupply
        // kFuelPipeTankSizeTons of fuel, and gets progressively damaged
        // while doing so to indicate its depletion.
        private static float GetTotalRefuelLimit(TISpaceFleetState fleet) {
            float tons = 0;

            foreach (var ship in fleet.ships) {
                foreach (var module in ship.GetFunctionalUtilitySlotModules(1f)) {
                    if (module.moduleTemplate as TIFuelPipe != null) {
                        tons += TIFuelPipe.kTankSizeTons * ship.GetPartFunction(module);
                    }
                }
            }

            return tons;
        }

        // Returns a list of ships that are providing refueling operation.
        private static List<TISpaceShipState> GetRefuelingShips(TISpaceFleetState fleet) {
            List<TISpaceShipState> ships = new List<TISpaceShipState>();
            foreach (var ship in fleet.ships) {
                foreach (var module in ship.GetFunctionalUtilitySlotModules(1f)) {
                    if (module.moduleTemplate as TIFuelPipe != null) {
                        ships.Add(ship);
                        break;
                    }
                }
            }

            return ships;
        }

        // Reduce the available capacity of FuelPipe tanks.
        private static void ConsumeFuelPipeCapacity(TISpaceFleetState fleet, float tons) {
            foreach (var ship in GetRefuelingShips(fleet)) {
                foreach (var module in ship.GetFunctionalUtilitySlotModules(1f)) {
                    if (module.moduleTemplate as TIFuelPipe != null) {
                        float moduleTonsLeft = TIFuelPipe.kTankSizeTons * ship.GetPartFunction(module);
                        if (moduleTonsLeft < tons) {
                            tons -= moduleTonsLeft;
                            moduleTonsLeft = 0;
                        } else {
                            moduleTonsLeft -= tons;
                            tons = 0;
                        }

                        float capacityLeft = 1 - moduleTonsLeft / TIFuelPipe.kTankSizeTons;

                        ship
                            .GetType()
                            .GetMethod("SetPartDamage", BindingFlags.Instance | BindingFlags.NonPublic)
                            .Invoke(ship, new object[] { module, capacityLeft, false });
                    }
                }
            }
        }

        // Returns true if at least one ship in the fleet fits a functional
        // FuelPipe module.
        private static bool HasFunctionalFuelPipe(TISpaceFleetState fleet) {
            return GetTotalRefuelLimit(fleet) > 0;
        }

        // Returns dictionary of ships that need fuel and amount of the fuel
        // they can get refueled. All ships get tanked up by a simillar percent
        // of the missing fuel.
        private static Dictionary<TISpaceShipState, float> WillRefuelPerShip(TISpaceFleetState fleet) {
            Dictionary<TISpaceShipState, float> ships = GetShipsToRefuel(fleet);
            float refuelingLimitTons = GetTotalRefuelLimit(fleet);
            float refuelingNeeded = 0;
            float refuelingFactor = 1;

            foreach (var ship in ships) {
                refuelingNeeded += ship.Value;
            }

            if (refuelingNeeded > refuelingLimitTons) {
                refuelingFactor = refuelingLimitTons / refuelingNeeded;
            }

            foreach (var ship in new List<TISpaceShipState>(ships.Keys)) {
                ships[ship] *= refuelingFactor;
            }

            return ships;
        }

        // Evaluate the cost of refueling. To abstract the complexity of keeping
        // track of what is loaded to the fuel tank, we assume it carries
        // kFuelPipeTankSizeTons of materials, but they are to be paid at the
        // operation time. Refueling takes 4 times longer with the modules.
        private static TIResourcesCost ExpectedCost(TISpaceFleetState fleet) {
            TIResourcesCost totalCost = new TIResourcesCost();

            foreach (var ship in WillRefuelPerShip(fleet)) {
                TIResourcesCost cost =
                    ship.Key.GetPreferredPropellantTankCost(ship.Key.faction, ship.Value);

                totalCost.SumCosts(cost);
                totalCost.SetCompletionTime_Days(
                    UnityEngine.Mathf.Max(totalCost.completionTime_days,
                    UnityEngine.Mathf.Max(ship.Key.PropellantShortage_tons / 1000f, 10f)));
            }

            totalCost.SetCompletionTime_Days(
                totalCost.completionTime_days * kRefuelTimeMultiplier
                / CountFunctionalFuelPipes(fleet));

            return totalCost;
        }

        // Show a notification screen upon operation completion.
        public static void LogOurFleetRefueled(TISpaceFleetState fleet) {
            NotificationQueueItem notificationQueueItem =
                typeof(TINotificationQueueState)
                .GetMethod("InitItem", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { }) as NotificationQueueItem;

            notificationQueueItem.alertFactions.Add(fleet.faction);
            notificationQueueItem.timerFactions.Add(fleet.faction);
            notificationQueueItem.icon = fleet.iconResource;
            notificationQueueItem.itemHammer = Loc.T("UI.Notifications.MissionControl");
            notificationQueueItem.itemHeadline = Loc.T("UI.Notifications.FleetRefueledHed", new object[]
            {
                fleet.GetDisplayName(TINotificationQueueState.activePlayer)
            });
            if (!fleet.landed) {
                notificationQueueItem.itemSummary = Loc.T("UI.Notifications.FleetRefueledSummaryOrbit", new object[]
                {
                    fleet.GetDisplayName(TINotificationQueueState.activePlayer),
                    fleet.location.GetDisplayName(TINotificationQueueState.activePlayer)
                });
                notificationQueueItem.itemDetail = Loc.T("UI.Notifications.FleetRefueledSummaryOrbit", new object[]
                {
                    fleet.GetDisplayName(TINotificationQueueState.activePlayer),
                    fleet.location.GetDisplayName(TINotificationQueueState.activePlayer)
                });
                notificationQueueItem.illustrationResource = World.Active.GetExistingManager<CameraManager>().skyboxBackdropPath;
            } else {
                notificationQueueItem.itemSummary = Loc.T("UI.Notifications.FleetRefueledSummary", new object[]
                {
                    fleet.GetDisplayName(TINotificationQueueState.activePlayer),
                    fleet.location.GetDisplayName(TINotificationQueueState.activePlayer)
                });
                notificationQueueItem.itemDetail = Loc.T("UI.Notifications.FleetRefueledSummary", new object[]
                {
                    fleet.GetDisplayName(TINotificationQueueState.activePlayer),
                    fleet.location.GetDisplayName(TINotificationQueueState.activePlayer)
                });
                notificationQueueItem.illustrationResource = fleet.dockedLocation.ref_habSite.template.backgroundPath;
            }
            notificationQueueItem.gotoGameState = fleet;
            notificationQueueItem.soundToPlay = "event:/SFX/UI_SFX/trig_SFX_RefuelingComplete";

            typeof(TINotificationQueueState)
                .GetMethod("AddItem", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { notificationQueueItem, false, null });
        }

        // Overrides

        // Sort it next ot vanilla resupply button.
        public override int SortOrder() {
            return 6;
        }

        // Operation blocks other operations while waiting for resolution.
        public override bool IsBlockingOperation() {
            return true;
        }

        // The operation has resource cost.
        public override bool HasResourceCost() {
            return true;
        }

        // Operation is visible if there are any ships with the fuel pipe not
        // depleated.
        public override bool OpVisibleToActor(TIGameState actor, TIGameState target = null) {
            return HasFunctionalFuelPipe(actor.ref_fleet);
        }

        // Operation is active if there are any ships with the fuel pipe not
        // depleated, and there are ships that need refueling in the fleet,
        // excluding the tanker ships.
        public override bool ActorCanPerformOperation(TIGameState actor, TIGameState target) {
            TISpaceFleetState fleet = actor.ref_fleet;

            if (!base.ActorCanPerformOperation(actor, target)) {
                return false;
            }

            return (
                HasFunctionalFuelPipe(fleet) &&
                GetShipsToRefuel(fleet).Count > 0 &&
                !fleet.inCombat &&
                !fleet.transferAssigned &&
                (!fleet.dockedAtHab ||
                 !fleet.ref_hab.AllowsResupply(fleet.faction, true, false)));
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
                    RefuelOperation.ExpectedCost(actor.ref_fleet)
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

            foreach (var ship in WillRefuelPerShip(fleet)) {
                TIResourcesCost cost =
                    ship.Key.GetPreferredPropellantTankCost(ship.Key.faction, ship.Value);
                if (!cost.CanAfford(ship.Key.faction, 1f, null, float.PositiveInfinity)) {
                    continue;
                }

                ship.Key.plannedResupplyAndRepair.AddPropellantToReload(ship.Value);
                totalCost.SumCosts(cost);
                totalCost.SetCompletionTime_Days(
                    UnityEngine.Mathf.Max(totalCost.completionTime_days,
                    UnityEngine.Mathf.Max(ship.Key.PropellantShortage_tons / 1000f, 10f)));
            }

            totalCost.SetCompletionTime_Days(
                totalCost.completionTime_days * kRefuelTimeMultiplier
                / CountFunctionalFuelPipes(fleet));

            return base.OnOperationConfirm(actor, target, totalCost, trajectory);
        }

        // When operation duration passes, check we are still in the valid state
        // and then process repairs and display completeion notification.
        public override void ExecuteOperation(TIGameState actor, TIGameState target) {
            TISpaceFleetState fleet = actor.ref_fleet;
            float propelantTons = 0;

            if (HasFunctionalFuelPipe(fleet) &&
                GetShipsToRefuel(fleet).Count > 0 &&
                !fleet.inCombat &&
                !fleet.transferAssigned &&
                (!fleet.dockedAtHab ||
                 !fleet.ref_hab.AllowsResupply(fleet.faction, true, false))) {
                foreach (var ship in fleet.ships) {
                    propelantTons += ship.plannedResupplyAndRepair.propellantToReload;
                    ship.plannedResupplyAndRepair.ProcessResupplyAndRepair(ship);
                }
                ConsumeFuelPipeCapacity(fleet, propelantTons);
                LogOurFleetRefueled(fleet);
                return;
            }

            // If state is no longer valid, undo all repairs.
            foreach (var ship in fleet.ships) {
                ship.plannedResupplyAndRepair.CancelRepair(fleet.faction);
            }
        }

        // If operation is canceled, undo all refueling.
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
