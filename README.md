# Space Ship Extras

Collection of extra features for space ships

## Installation

* Install [Unity Mod Manager](https://www.nexusmods.com/site/mods/21)
* Build the project or Download as an archive.
* Copy `$(project_location)/Result/ModEnsemble` or archive content into
  `$(game_directory)/Mods/Enabled`
* Existing saves are functional with the mod. After mod installation, existing
  ships will start with 0 experience.

## Removal

While the mod is compatible with vanilla saves, saves made with the mod are NOT
compatible with the vanilla game. If you want to remove the mod, but continue
from the save you made with the mod, you have to modify your save file to remove
`SpaceShipExperienceMod.TISpaceShipExperienceState` entry entirely.

## Features

### Fuel Refund on Refit

Well, the title speaks for itself :)

### Space ship experience

#### Experience accumulation and ranks

Ships accumulate experince in battles. The experience is granted to surviving
ships regardless of whether the fight was won or lost. The gained experience
by a ship is calculated by using the following formula:

* BONUS_FACTOR = (FOE_POWER / ALLY_POWER) ^ 0.3
* SHIP_EXP_GAINED = FOE_POWER * BONUS_FACTOR / NUMBER_OF_SURVIVING_ALLIED_SHIPS

Total accumulated ship's experience is represented as its rank, which are the
following:

* Rookie - 0 EXP
* Soldier - <=100 EXP
* Veteran - <=500 EXP
* Elite - <= 2500 EXP
* Hero - <= 10000 EXP
* Legen - >= 10000 EXP

#### UI
Ranks are prefixed or suffixed to the ship name in most UI. There is a number of
UIs where the rank is omitted due to technical reasons:
* Reinforcements UI (because the specific methods are hard to patch)
* Combat UI primary target tooltip in the ship list (because who cares lol)

If you find the rank omitted in any other UI, let me know on Discord.

#### Rank bonuses

So far rank serves two purposes:
1) Cosmetic
2) Winning space battle provides a boost to faction support depending on
   the highest ship rank in the fleet and power balance of the fleets.
   Current formula is:

   RANK_IDX - Numerical representation of rank, Rookie - 0, Soldier - 1 etc.
              The highest rank in the fleet is used for calculations.
   RANK_FACTOR = RANK_IDX * RANK_IDX / 5
   FLEET_FACTOR = FOE_POWER / ALLY_POWER
   POPULARITY_GAIN = RANK_FACTOR * FLEET_FACTOR

There are plans to add more effects based on rank of ships in fleets, including
negative effects for loses.

### Utility Ship Hull

The mod adds a new hull type that can carry a single utility module. The ship
hull is unlocked with `SpaceDock` faction project. This ship type is the only
design that is allowed to carry the special utility modules added with this mod.

### Deep Space Repair

`Deep Space Repair Bay` - a utility module that allows to perform
limitted repairs in the deep space. 

#### Use

* Unlocks with `Deep Space Maintenance` faction project which depends on
  `Shipyards` project.
* Can ONLY be fitted to `Utility ship` design.
* The module is capable of fixing Engine and Power plant subsystem and parts.
  `Deep Space Emergency Repair` operation becomes available in the fleet actions
  bar when the fleet has at least one ship with an operational 
  `Deep Space Repair Bay` and any of the ships in the fleet sustained damage to
  engines or power plant.
* The cost of the repairs are the same as if they were done on a station, but
  the repair time is 4 times longer.
* Having multiple ships with the module speeds up the process.

#### Known issues

* There is a slight but that may allow initiating deep space repairs when the
  faction has insufficient resources to do so. The behaviour is consistent with
  the repair behaviour in the vanilla game and thus left as is.

### Debris Cleaner

`Debris Cleaner` - a utility module that allows to perform `Clear Debris`
operation to remove a cloud of debris.

#### Use

* Unlocks with `Orbital Capture` faction project, which depends on SpaceDock 
  project and Applied Artificial Intelegence global tech.
* Can ONLY be fitted to `Utility ship` design.
* Allows executing "Clear Debris" operation in orbits with debris.
* The operation required delta-V equal to 1/1000th of the orbit's circumference
  and takes as many days to complete.
* Multiple ships with the module in the fleet speed up the process and reduce
  the delta-V cost.
