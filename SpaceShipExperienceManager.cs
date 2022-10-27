using System.Collections.Generic;
using PavonisInteractive.TerraInvicta;

namespace SpaceShipExperienceMod {
    public class SpaceShipExperienceManager {

        private Dictionary<GameStateID, TISpaceShipExperienceState> 
            spaceShipExperienceMapping =
            new Dictionary<GameStateID, TISpaceShipExperienceState>();

        private TISpaceShipExperienceState this[TISpaceShipState ship] {
            get { 
                if (ship == null) {
                    return null;
                }

                if (!spaceShipExperienceMapping.ContainsKey(ship.ID)) {
                    this.RegisterShip(ship);
                }

                return spaceShipExperienceMapping[ship.ID];
            }
        }

        public string GetNameRankString(TISpaceShipState ship, bool rank_first = false) {
            if (!Main.enabled) {
                return ship.GetDisplayName(GameControl.control.activePlayer);
            }
            if (rank_first) {
                return this[ship].GetRankString() + " " + ship.GetDisplayName(GameControl.control.activePlayer);
            }
            return ship.GetDisplayName(GameControl.control.activePlayer) + " " + this[ship].GetRankString();
        }

        public void RegisterShip(TISpaceShipState ship, TISpaceShipExperienceState shipExperience = null) {
            if (!Main.enabled) {
                return;
            }

            if (spaceShipExperienceMapping.ContainsKey(ship.ID)) {
                return;
            }

            if (shipExperience == null) {
                shipExperience = GameStateManager.CreateNewGameState<TISpaceShipExperienceState>();
                shipExperience.InitWithSpaceShipState(ship);
            }

            spaceShipExperienceMapping.Add(ship.ID, shipExperience);
        }

        public void AddExperience(TISpaceShipState ship, int exp) {
            if (!Main.enabled) {
                return;
            }

            this[ship].AddExperience(exp);
        }

        public void UnregisterShip(TISpaceShipState ship) {
            if (!spaceShipExperienceMapping.ContainsKey(ship.ID)) {
                TISpaceShipExperienceState shipExperience = this[ship];
                if (GameStateManager.RemoveGameState<TISpaceShipExperienceState>(shipExperience.ID, false)) {
                    spaceShipExperienceMapping.Remove(ship.ID);
                }
            }
        }

        public void ResetState() {
            spaceShipExperienceMapping.Clear();
        }
    }
}
