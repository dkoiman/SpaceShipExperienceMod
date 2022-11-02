using System;

using PavonisInteractive.TerraInvicta;
using PavonisInteractive.TerraInvicta.Debugging;

namespace SpaceShipExtras.ShipExperience {
    // A console command to give experience to a ship. Can only deal with
    // single-word ship names.
    public class SpaceShipExperienceConsoleCommand {
        public SpaceShipExperienceConsoleCommand(TerminalController terminalController) {
            this.terminalController = terminalController;
            this.terminalController.RegisterCommand(
                "GiveShipExp",
                new CommandHandler(this.GiveShipExp),
                "Gives experience to a ship in the selected fleet `GiveShipExp <ship_name> <exp>`");
        }

        // Our command takes two arguments, but the in-game parser bundles it
        // as one, so we will have to manually decode.
        public void GiveShipExp(string[] args) {
            TIGameState asset = GeneralControlsController.UISelectedAssetState;

            // A fleet needs to be selected.
            if (!asset.isSpaceFleetState || asset.ref_fleet == null) {
                terminalController.OutputError("Selected object is not a fleet.");
                return;
            }

            // Expect an argument
            if (args.Length < 1) {
                terminalController.OutputError("No arguments provided");
                return;
            }

            // Manually split the argument and expect two components.
            string[] realArgs = args[0].Split(' ');

            if (realArgs.Length != 2) {
                terminalController.OutputError("Requires 2 arguments");
                return;
            }

            string shipStr = realArgs[0];
            string expStr = realArgs[1];

            // Find target ship in the fleet by name.
            TISpaceShipState targetShip = null;
            foreach (var ship in asset.ref_fleet.ships) {
                if (ship.displayName == shipStr) {
                    targetShip = ship;
                }
            }

            if (targetShip == null) {
                terminalController.OutputError($"Couldn't find '{shipStr}' in the selected fleet.");
                return;
            }

            // Parse the experience value and add to the target ship if valid.
            try {
                int exp = Int32.Parse(expStr);
                Main.experienceManager.AddExperience(targetShip, exp);
            } catch (FormatException) {
                terminalController.OutputError($"Unable to parse '{expStr}' to integer");
            }
        }

        private TerminalController terminalController;
    }
}
