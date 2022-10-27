using System.Collections.Generic;
using System.Reflection;

using PavonisInteractive.TerraInvicta;

namespace SpaceShipExtras.Utility {
    class DeepSpaceRepairFleetOperation : TISpaceFleetOperationTemplate {

        private static int CountFunctionalDeepSpaceRescueBays(TISpaceFleetState fleetState) {
            int count = 0;

            foreach (var ship in fleetState.ref_fleet.ships) {
                foreach (var module in ship.GetFunctionalUtilitySlotModules(1f)) {
                    if (module.moduleTemplate as TIDeepSpaceRepairBay != null) {
                        count++; ;
                    }
                }
            }

            return count;
        }

        private static bool HasFunctionalDeepSpaceRescueBay(TISpaceFleetState fleetState) {
            return CountFunctionalDeepSpaceRescueBays(fleetState) > 0;
        }

        private static Dictionary<TISpaceShipState, TIResourcesCost> EssentialRepairsCost(TISpaceFleetState fleetState) {
            Dictionary<TISpaceShipState, TIResourcesCost> result =
                new Dictionary<TISpaceShipState, TIResourcesCost>();

            foreach (var ship in fleetState.ships) {
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

        private static bool HasEssentialRepairs(TISpaceFleetState fleetState) {
            return EssentialRepairsCost(fleetState).Count > 0;
        }

        public override OperationTiming GetOperationTiming() {
            return OperationTiming.DelayedExecutionOfInstantEffect;
        }

        public override bool IsBlockingOperation() {
            return true;
        }

        public override bool UseResourceCostDuration() {
            return true;
        }

        public override bool CancelUponDepartHab() {
            return false;
        }

        public override float GetDuration_days(TIGameState actorState, TIGameState target, Trajectory trajectory = null) {
            return -1f;
        }

        public override System.Type GetTargetingMethod() {
            return typeof(TIOperationTargeting_Self);
        }

        public override int SortOrder() {
            return 7;
        }

        public override List<TIGameState> GetPossibleTargets(TIGameState actorState, TIGameState defaultTarget = null) {
            return new List<TIGameState> {
                actorState
            };
        }

        public override bool OpVisibleToActor(TIGameState actorState, TIGameState targetState = null) {
            TISpaceFleetState ref_fleet = actorState.ref_fleet;
            return HasEssentialRepairs(ref_fleet) && HasFunctionalDeepSpaceRescueBay(ref_fleet) && !ref_fleet.dockedAtHab;
        }

        public override bool ActorCanPerformOperation(TIGameState actorState, TIGameState target) {
            if (!base.ActorCanPerformOperation(actorState, target)) {
                return false;
            }
            TISpaceFleetState ref_fleet = actorState.ref_fleet;
            return
                HasEssentialRepairs(ref_fleet) && HasFunctionalDeepSpaceRescueBay(ref_fleet)
                && !ref_fleet.inCombat && ref_fleet.CanAffordAnyRepairs() && !ref_fleet.dockedAtHab;
        }

        public override List<TIResourcesCost> ResourceCostOptions(TIFactionState faction, TIGameState target, TIGameState actor, bool checkCanAfford = true) {
            if (actor.isSpaceFleetState) {
                return new List<TIResourcesCost>(1) {
                    DeepSpaceRepairFleetOperation.ExpectedCost(actor.ref_fleet, actor.ref_fleet.ref_hab)
                };
            }
            return null;
        }

        public override bool HasResourceCost() {
            return true;
        }

        public static TIResourcesCost ExpectedCost(TISpaceFleetState fleetState, TIHabState hab) {
            TIResourcesCost tiresourcesCost = new TIResourcesCost();
            var essentialRepairs = EssentialRepairsCost(fleetState);
            foreach (var cost in essentialRepairs) {
                tiresourcesCost.SumCosts(cost.Value);
                tiresourcesCost.SetCompletionTime_Days(tiresourcesCost.completionTime_days + cost.Value.completionTime_days);
            }
            tiresourcesCost.SetCompletionTime_Days(tiresourcesCost.completionTime_days * 4 / CountFunctionalDeepSpaceRescueBays(fleetState));
            return tiresourcesCost;
        }

        public override bool OnOperationConfirm(TIGameState actorState, TIGameState target, TIResourcesCost resourcesCost = null, Trajectory trajectory = null) {
            TISpaceFleetState fleetState = actorState.ref_fleet;
            TIResourcesCost tiresourcesCost = new TIResourcesCost();
            var essentialRepairs = EssentialRepairsCost(fleetState);
            foreach (var cost in essentialRepairs) {
                if (!cost.Value.CanAfford(cost.Key.faction, 1f, null, float.PositiveInfinity)) {
                    continue;
                }
                ScheduleEssentialRepairs(cost.Key);
                tiresourcesCost.SumCosts(cost.Value);
                tiresourcesCost.SetCompletionTime_Days(tiresourcesCost.completionTime_days + cost.Value.completionTime_days); 
            }
            tiresourcesCost.SetCompletionTime_Days(tiresourcesCost.completionTime_days * 4 / CountFunctionalDeepSpaceRescueBays(fleetState));
            return base.OnOperationConfirm(actorState, target, tiresourcesCost, trajectory);
        }

        public override void ExecuteOperation(TIGameState actorState, TIGameState target) {
            TISpaceFleetState ref_fleet = actorState.ref_fleet;
            if (HasFunctionalDeepSpaceRescueBay(ref_fleet) &&
                !ref_fleet.inCombat &&
                ref_fleet.CanAffordAnyRepairs() &&
                !ref_fleet.transferAssigned &&
                !ref_fleet.dockedAtHab) {
                foreach (TISpaceShipState tispaceShipState in ref_fleet.ships) {
                    tispaceShipState.plannedResupplyAndRepair.ProcessResupplyAndRepair(tispaceShipState);
                }
                TINotificationQueueState.LogOurFleetRepaired(ref_fleet);
                return;
            }

            foreach (TISpaceShipState tispaceShipState2 in ref_fleet.ships) {
                tispaceShipState2.plannedResupplyAndRepair.CancelRepair(ref_fleet.faction);
            }
        }

        public override void OnOperationCancel(TIGameState actorState, TIGameState target, TIDateTime opCompleteDate) {
            base.OnOperationCancel(actorState, target, opCompleteDate);
            actorState.ref_fleet.ships.ForEach(delegate (TISpaceShipState x)
            {
                x.plannedResupplyAndRepair.CancelRepair(actorState.ref_faction);
            });
        }

        public override List<System.Type> BreakthroughOps() {
            return new List<System.Type> { };
        }
    }
}
