using System.Collections.Generic;
using PavonisInteractive.TerraInvicta;
using UnityEngine;

namespace SpaceShipExperienceMod {
    public class SpaceShipExperienceManager {

        private Dictionary<GameStateID, TISpaceShipExperienceState> 
            SpaceShipVeterancyStateMapping =
            new Dictionary<GameStateID, TISpaceShipExperienceState>();

        public TISpaceShipExperienceState this[TISpaceShipState ship] {
            get { 
                if (ship == null) {
                    return null;
                }

                if (!SpaceShipVeterancyStateMapping.ContainsKey(ship.ID)) {
                    this.RegisterShip(ship);
                }

                return SpaceShipVeterancyStateMapping[ship.ID];
            }
        }

        public string GetNameRankString(TISpaceShipState ship, bool rank_first = false) {
            if (rank_first) {
                return this[ship].GetRankString() + " " + ship.GetDisplayName(GameControl.control.activePlayer);
            }
            return ship.GetDisplayName(GameControl.control.activePlayer) + " " + this[ship].GetRankString();
        }

        public void RegisterShip(TISpaceShipState ship, TISpaceShipExperienceState veterancy = null) {
            if (SpaceShipVeterancyStateMapping.ContainsKey(ship.ID)) {
                return;
            }

            if (veterancy == null) {
                veterancy = GameStateManager.CreateNewGameState<TISpaceShipExperienceState>();
                veterancy.InitWithSpaceShipState(ship);
            }

            SpaceShipVeterancyStateMapping.Add(ship.ID, veterancy);
        }

        public void UnregisterShip(TISpaceShipState ship) {
            if (!SpaceShipVeterancyStateMapping.ContainsKey(ship.ID)) {
                TISpaceShipExperienceState shipVeterancyState = this[ship];
                if (GameStateManager.RemoveGameState<TISpaceShipExperienceState>(shipVeterancyState.ID, false)) {
                    SpaceShipVeterancyStateMapping.Remove(ship.ID);
                }
            }
        }

        public void ResetState() {
            SpaceShipVeterancyStateMapping.Clear();
        }
    }
}
