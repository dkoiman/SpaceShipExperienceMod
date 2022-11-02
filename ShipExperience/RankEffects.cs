using System.Linq;
using System.Collections.Generic;

using PavonisInteractive.TerraInvicta;

namespace SpaceShipExtras.ShipExperience {
    // Stores the code for processing effects based on ships ranks and battles
    // outcomes.
    class RankEffects {
        public enum Outcome {
            WIN,
            LOSS,
            DRAW,
            TOTAL_ANIHILATION
        }

        public static void BoostOpinion(TIFactionState faction, float propaganda) {
            TIEffectsState.ProcessInstantEffect(
                faction,
                EffectTargetType.AllNations,
                EffectSecondaryStateType.none,
                InstantEffect.Propaganda_Faction,
                propaganda, 0, "");
        }

        public static void DispatchResults(TIFactionState faction,
                                           Dictionary<TISpaceShipState, int> ranks,
                                           float powerBalance,
                                           Outcome outcome) {
            if (!Main.enabled || faction == null) {
                return;
            }

            int maxRank = ranks.Values.Max();
            float propaganda = maxRank * maxRank / 5 * powerBalance;

            // If a human faction wins a battle - give it opinion boost.
            if (outcome == Outcome.WIN && !faction.IsAlienFaction) {
                BoostOpinion(faction, propaganda);
            }

            // If the aliens win a fight - boost its proxy and appeasers.
            if (outcome == Outcome.WIN && faction.IsAlienFaction) {
                BoostOpinion(GameStateManager.AlienProxy(), propaganda);
                BoostOpinion(GameStateManager.AlienAppeaser(), propaganda);
            }
        }
    }
}
