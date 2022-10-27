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

### Space ship experience

#### Experience accumulation and ranks

Ships accumulate experince in battles. The experience is granted to surviving
ships regardless of whether the fight was won or lost. The gained experience
by a ship is calculated by using the following formula:

* BONUS_FACTOR = (FOE_POWER / ALLY_POWER) ^ 0.3
* SHIP_EXP_GAINED = FOE_POEWR * BONUS_FACTOR / NUMBER_OF_SURVIVING_ALLIED_SHIPS

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
* Docked ship UI (because I have no idea what canvas is that lol)
* Reinforcements UI (because the specific methods are hard to patch)
* Combat UI primary target tooltip in the ship list (because who cares lol)

If you find the rank omitted in any other UI, let me know on Discord.

#### Rank bonuses

At this point in time the rank is purely cosmetic and doesn't add any bonuses.
There are plans to add various effects based on rank of ships in fleets.

### Deep Space Repair

The mod adds `Deep Space Repair Bay` - a utility module that allows to perform
limitted repairs in the deep space. 

#### Use

* The mod introduces new hull type - `Utility ship`
* `Deep Space Repair Bay` can ONLY be fitted to `Utility ship` design.
* The module is capable of fixing Engine and Power plant subsystem and parts.
  `Deep Space Emergency Repair` operation becomes available in the fleet actions
  bar when the fleet has at least one ship with an operational 
  `Deep Space Repair Bay` and any of the ships in the fleet sustained damage to
  engines or power plant.
* The cost of the repairs are the same as if they were done on a station, but
  the repair time is 4 times longer. Having multiple ships with the module
  speeds up the process.

#### Known issues

* There is a slight but that may allow initiating deep space repairs when the
  faction has insufficient resources to do so. The behaviour is consistent with
  the repair behaviour in the vanilla game and thus left as is.
