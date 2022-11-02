﻿using UnityEngine;

using PavonisInteractive.TerraInvicta;


namespace SpaceShipExtras.ShipExperience {
    public class TISpaceShipExperienceState : TIGameState {
        new public TISpaceShipState ref_ship;

        private static readonly int[] ranks = { 0, 100, 500, 2500, 10000 };

        public static int ExpToRank(int exp) {
            int rank = 0;
            for (; rank < ranks.Length; rank++) {
                if (exp <= ranks[rank]) {
                    break;
                }
            }
            return rank;
        }

        [SerializeField]
        public int experience { get;  private set; } = 0;

        public void AddExperience(int exp) {
            this.experience += exp;
        }

        public int GetRank() {

            return ExpToRank(experience);
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
