using System;

using PavonisInteractive.TerraInvicta;
using PavonisInteractive.TerraInvicta.Debugging;

namespace SpaceShipExtras.ShipExperience {
    public class SpaceShipExperienceConsoleCommand {
        public SpaceShipExperienceConsoleCommand(TerminalController terminalController) {
            this.terminalController = terminalController;
            this.terminalController.RegisterCommand(
                "GiveShipExp",
                new CommandHandler(this.GiveShipExp),
                "Gives experience to a ship in the selected fleet `GiveShipExp <ship_name> <exp>`");
        }

        public void GiveShipExp(string[] args) {
            TIGameState asset = GeneralControlsController.UISelectedAssetState;
            if (!asset.isSpaceFleetState || asset.ref_fleet == null) {
                terminalController.OutputError("Selected object is not a fleet.");
                return;
            }

            if (args.Length < 1) {
                terminalController.OutputError("No arguments provided");
                return;
            }

            string[] realArgs = args[0].Split(' ');

            if (realArgs.Length != 2) {
                terminalController.OutputError("Requires 2 arguments");
                return;
            }

            string shipStr = realArgs[0];
            string expStr = realArgs[1];

            TISpaceShipState ship = null;
            foreach (var s in asset.ref_fleet.ships) {
                if (s.displayName == shipStr) {
                    ship = s;
                }
            }

            if (ship == null) {
                terminalController.OutputError($"Couldn't find '{shipStr}' in the selected fleet.");
                return;
            }

            try {
                int exp = Int32.Parse(expStr);
                Main.experienceManager.AddExperience(ship, exp);
            } catch (FormatException) {
                terminalController.OutputError($"Unable to parse '{expStr}' to integer");
            }
        }

        private TerminalController terminalController;
    }
}
