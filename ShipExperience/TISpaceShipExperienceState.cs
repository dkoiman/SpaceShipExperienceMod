using PavonisInteractive.TerraInvicta;


namespace SpaceShipExtras.ShipExperience {
    // Stores an experience state for a specific ship.
    public class TISpaceShipExperienceState : TIGameState {
        // Thresholds for ranks.
        private static readonly int[] ranks = { 0, 100, 500, 2500, 10000 };

        // Get the numerical rank for the given experience.
        public static int ExpToRank(int exp) {
            int rank = 0;
            for (; rank < ranks.Length; rank++) {
                if (exp <= ranks[rank]) {
                    break;
                }
            }
            return rank;
        }
        // We have to redefine the field to be able to assign to it.
        new public TISpaceShipState ref_ship;

        // Current experinece of the referenced ship.
        public int experience { get;  private set; } = 0;

        // Returns raw experience.
        public void AddExperience(int exp) {
            this.experience += exp;
        }

        // Returns numerical rank.
        public int GetRank() {
            return ExpToRank(experience);
        }

        // Returns localized textual rank.
        public string GetRankString() {
            return "[" + Loc.T("TISpaceShipRank.Rank_" + GetRank().ToString()) + "]";
        }

        // Called upon creation of a new state.
        public void InitWithSpaceShipState(TISpaceShipState ship) {
            if (ship.template == null) {
                return;
            }

            this.ref_ship = ship;
        }

        // Called upon retrieving the state from the save file.
        // The experience state is deserealized (including the ship reference)
        // prior calling to the method.
        public override void PostInitializationInit_4() {
            Main.experienceManager.RegisterShip(this.ref_ship, this);
        }
    }
}
