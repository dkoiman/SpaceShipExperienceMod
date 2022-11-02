using System.Collections.Generic;
using PavonisInteractive.TerraInvicta;

namespace SpaceShipExtras.ShipExperience {
    // SpaceShipExperienceManager maintains the mapping between ships and
    // their experience state.
    public class SpaceShipExperienceManager {

        private Dictionary<GameStateID, TISpaceShipExperienceState>
        spaceShipExperienceMapping = new Dictionary<GameStateID, TISpaceShipExperienceState>();

        // Allow index-access to the objects.
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

        // Returns a Ship/Rank string for replacing ship names in UI.
        public string GetNameRankString(TISpaceShipState ship, bool rank_first = false) {
            if (!Main.enabled) {
                return ship.GetDisplayName(GameControl.control.activePlayer);
            }

            if (rank_first) {
                return (this[ship].GetRankString() + " " +
                        ship.GetDisplayName(GameControl.control.activePlayer));
            }

            return (ship.GetDisplayName(GameControl.control.activePlayer) + " " +
                    this[ship].GetRankString());
        }

        // Get ship's numerical rank.
        public int GetRank(TISpaceShipState ship) {
            if (!Main.enabled) {
                return 0;
            }

            return this[ship].GetRank();
        }

        // Get ship's raw experience points.
        public int GetExperience(TISpaceShipState ship) {
            if (!Main.enabled) {
                return 0;
            }

            return this[ship].experience;
        }

        // Add experience for a ship.
        public void AddExperience(TISpaceShipState ship, int exp) {
            if (!Main.enabled) {
                return;
            }

            this[ship].AddExperience(exp);
        }

        // Register a ship, either with a specified experience or as a new one.
        // New ships also get experience state object registerd for them.
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

        // Removes experince state for a ship.
        public void UnregisterShip(TISpaceShipState ship) {
            if (!spaceShipExperienceMapping.ContainsKey(ship.ID)) {
                TISpaceShipExperienceState shipExperience = this[ship];

                if (GameStateManager.RemoveGameState<TISpaceShipExperienceState>(shipExperience.ID, false)) {
                    spaceShipExperienceMapping.Remove(ship.ID);
                }
            }
        }

        // Clears the mapping for game re-loads.
        public void ResetState() {
            spaceShipExperienceMapping.Clear();
        }
    }
}
