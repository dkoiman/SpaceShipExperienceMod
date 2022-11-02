using System.Linq;
using System.Collections.Generic;

using PavonisInteractive.TerraInvicta;

namespace SpaceShipExtras.ShipExperience {
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

            if (outcome == Outcome.WIN && !faction.IsAlienFaction) {
                BoostOpinion(faction, propaganda);
            }

            if (outcome == Outcome.WIN && faction.IsAlienFaction) {
                BoostOpinion(GameStateManager.AlienProxy(), propaganda);
                BoostOpinion(GameStateManager.AlienAppeaser(), propaganda);
            }
        }
    }
}
