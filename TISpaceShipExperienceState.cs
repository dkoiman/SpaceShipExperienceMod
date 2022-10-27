using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using PavonisInteractive.TerraInvicta.Entities;
using UnityEngine;


namespace SpaceShipExperienceMod {
    public class TISpaceShipExperienceState : TIGameState {
        new public TISpaceShipState ref_ship;

        private static readonly int[] ranks = { 0, 100, 500, 2500, 10000 };

        [SerializeField]
        private int experience { get;  set; } = 0;

        public void AddExperience(int exp) {
            experience += exp;
        }

        public int GetRank() {
            int rank = 0;
            for (; rank < ranks.Length; rank++) {
                if (experience <= ranks[rank]) {
                    break;
                }
            }
            return rank;
        }

        public string GetRankString() {
            return "[" + Loc.T("TISpaceShipRank.Rank_" + GetRank().ToString()) + "]";
        }

        public void InitWithSpaceShipState(TISpaceShipState ship) {
            if (ship.template == null) {
                return;
            }

            this.ref_ship = ship;
        }

        public override void PostInitializationInit_4() {
            Main.experienceManager.RegisterShip(this.ref_ship, this);
        }
    }
}
