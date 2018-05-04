using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using System;

namespace CounterDeckBuilder
{
    /// <summary>
    /// Interface for heuristic, has to return double "score" from given game.
    /// </summary>
    public interface IGameStateHeuristic
    {
        /// <summary>
        /// Get score from current state of game.
        /// </summary>
        /// <param name="game"> Current game. </param>
        /// <returns> Score of current state. </returns>
        double GetScore(Game game);
    }

    /// <summary>
    /// Enum containing all implemented heuristics. 
    /// </summary>
    public enum Heuristic
    {
        FACE_HUNTER, HEARTH_AGENT, AGGRO_PALLY, SECRET_MAGE, CONTROL_PRIEST, BASIC, DEFAULT
    }

    /// <summary>
    /// Extension class containing auxiliary methods of Heuristics.
    /// </summary>
    public static class HeuristicExtension
    {
        /// <summary>
        /// Constructs actual IGameStateHeuristic, given Heuristic enum.
        /// </summary>
        /// <param name="heuristic"> Heuristic name enum. </param>
        /// <returns> Actual IGameStateHeuristic. </returns>
        public static IGameStateHeuristic GetHeuristic(this Heuristic heuristic)
        {
            switch (heuristic)
            {
                case Heuristic.AGGRO_PALLY:
                    return new AggroPallyHeuristic();
                case Heuristic.FACE_HUNTER:
                    return new FaceHunterHeuristic();
                case Heuristic.SECRET_MAGE:
                    return new SecretMageHeuristic();
                case Heuristic.BASIC:
                    return new BasicHeuristic();
                case Heuristic.DEFAULT:
                    return new DefaultHeuristic();
                case Heuristic.CONTROL_PRIEST:
                    return new ControlPriestHeuristic();
                default:
                    return new DefaultHeuristic();
            }
        }
    }

    /// <summary>
    /// My original basic heuristic.
    /// </summary>
    public class BasicHeuristic : IGameStateHeuristic
    {
        /// <summary>
        /// Cares about board mana advantage, card advantage, damage set up and health advantage.
        /// </summary>
        /// <param name="game"> Current game. </param>
        /// <returns> Score. </returns>
        public double GetScore(Game game)
        {
            CounterDeckBuilder.GameState x = GameStateObserver.GetGameState(game);

            double result = 0;

            result += x.BoardManaAdvantage.Value * 5;
            result += x.CardAdvantage.Value * 2;
            result += x.DamageSetUpAdvantage.Value * 3;
            result += x.HealthAdvantage.Value;

            return result;
        }
    }

    /// <summary>
    /// Heuristic used for comparison to MCTS in HearthAgent in Metastone.
    /// Original source code (in Java):
    /// ...\metastone\game\behaviour\heuristic\WeightedHeuristic.java
    /// 
    /// 
    /// Cannot use secrets! 
    /// Does not care about some static abilities - poisonous, lifesteal, charge, ...
    /// </summary>
    public class HearthAgentHeuristic : IGameStateHeuristic
    {
        /// <summary>
        /// Get score of a minion. 
        /// </summary>
        /// <param name="minion"> Minion to be rated. </param>
        /// <returns> Score of minion. </returns>
        private double GetMinionScore(Minion minion)
        {
            double minionScore = minion.AttackDamage + minion.Health;
            double baseScore = minionScore;

            if (minion.IsFrozen)
            {
                return minion.Health;
            }
            if (minion.HasTaunt)
            {
                minionScore += 2;
            }
            if (minion.HasWindfury)
            {
                minionScore += minion.AttackDamage * 0.5;
            }
            if (minion.HasDivineShield)
            {
                minionScore += 1.5f * baseScore;
            }
            if (minion.Card.Tags.ContainsKey(SabberStoneCore.Enums.GameTag.SPELLPOWER))
            {
                minionScore += minion.Card.Tags[SabberStoneCore.Enums.GameTag.SPELLPOWER];
            }
            if (minion.IsEnraged)
            {
                minionScore += 1;
            }
            if (minion.HasStealth)
            {
                minionScore += 1;
            }
            if (minion.CantBeTargetedBySpells)
            {
                minionScore += 1.5f * baseScore;
            }

            return minionScore;
        }

        public double GetScore(Game game)
        {
            double score = 0;

            if (game.CurrentPlayer.ToBeDestroyed)
            {
                return Double.MinValue;
            }
            if (game.CurrentOpponent.ToBeDestroyed)
            {
                return Double.MaxValue;
            }

            int ownHp = game.CurrentPlayer.Hero.Health + game.CurrentPlayer.Hero.Armor;
            int opponentHp = game.CurrentOpponent.Hero.Health + game.CurrentOpponent.Hero.Armor;
            score += ownHp - opponentHp;

            score += game.CurrentPlayer.HandZone.Count * 3;
            score -= game.CurrentOpponent.HandZone.Count * 3;

            score += game.CurrentPlayer.BoardZone.Count * 2;
            score -= game.CurrentOpponent.BoardZone.Count * 2;
            
            foreach (Minion minion in game.CurrentPlayer.BoardZone)
            {
                score += GetMinionScore(minion);
            }
            foreach (Minion minion in game.CurrentOpponent.BoardZone)
            {
                score -= GetMinionScore(minion);
            }

            return score;
        }
    }

    /// <summary>
    /// Default heuristic, used for most of meta-decks during our experiments. Highly inspired by Metastone's heuristic.
    /// </summary>
    public class DefaultHeuristic : IGameStateHeuristic
    {
        /// <summary>
        /// Get score of a minion. 
        /// </summary>
        /// <param name="minion"> Minion to be rated. </param>
        /// <returns> Score of the minion. </returns>
        private double GetMinionScore(Minion minion)
        {
            double minionScore = minion.AttackDamage + minion.Health;
            double baseScore = minionScore;

            if (minion.IsFrozen)
            {
                return minion.Health;
            }
            if (minion.HasTaunt)
            {
                minionScore += 2;
            }
            if (minion.HasWindfury)
            {
                minionScore += minion.AttackDamage * 0.5;
            }
            if (minion.HasDivineShield)
            {
                minionScore += 1.5f * baseScore;
            }
            if (minion.Card.Tags.ContainsKey(SabberStoneCore.Enums.GameTag.SPELLPOWER))
            {
                minionScore += minion.Card.Tags[SabberStoneCore.Enums.GameTag.SPELLPOWER];
            }
            if (minion.IsEnraged)
            {
                minionScore += 1;
            }
            if (minion.HasStealth)
            {
                minionScore += 1;
            }
            if (minion.CantBeTargetedBySpells)
            {
                minionScore += 1.5f * baseScore;
            }

            if (minion.AttackDamage == 0) minionScore /= 2;

            return minionScore;
        }

        public double GetScore(Game game)
        {
            double score = 0;

            if (game.CurrentPlayer.ToBeDestroyed)
            {
                return Double.MinValue;
            }
            if (game.CurrentOpponent.ToBeDestroyed)
            {
                return Double.MaxValue;
            }

            int ownHp = game.CurrentPlayer.Hero.Health + game.CurrentPlayer.Hero.Armor;
            int opponentHp = game.CurrentOpponent.Hero.Health + game.CurrentOpponent.Hero.Armor;
            score += 0.5*(ownHp - opponentHp);

            score += game.CurrentPlayer.BoardZone.Count * 2;
            score -= game.CurrentOpponent.BoardZone.Count * 2;
            
            score += 1.5 * game.CurrentPlayer.NumCardsPlayedThisTurn;

            foreach (Minion minion in game.CurrentPlayer.BoardZone)
            {
                score += GetMinionScore(minion);
            }
            foreach (Minion minion in game.CurrentOpponent.BoardZone)
            {
                score -= GetMinionScore(minion);
            }

            return score;
        }
    }

    /// <summary>
    /// Secret mage heuristic, keeps playing secrets in mind.
    /// </summary>
    public class SecretMageHeuristic : IGameStateHeuristic
    {
        private double GetMinionScore(Minion minion)
        {
            double minionScore = minion.AttackDamage + minion.Health;
            double baseScore = minionScore;

            if (minion.IsFrozen)
            {
                return minion.Health;
            }
            if (minion.HasTaunt)
            {
                minionScore += 2;
            }
            if (minion.HasWindfury)
            {
                minionScore += minion.AttackDamage * 0.5;
            }
            if (minion.HasDivineShield)
            {
                minionScore += 1.5f * baseScore;
            }
            if (minion.Card.Tags.ContainsKey(SabberStoneCore.Enums.GameTag.SPELLPOWER))
            {
                minionScore += minion.Card.Tags[SabberStoneCore.Enums.GameTag.SPELLPOWER];
            }
            if (minion.IsEnraged)
            {
                minionScore += 1;
            }
            if (minion.HasStealth)
            {
                minionScore += 1;
            }
            if (minion.CantBeTargetedBySpells)
            {
                minionScore += 1.5f * baseScore;
            }
            if (minion.HasLifeSteal)
            {
                minionScore += 1;
            }
            if (minion.Poisonous)
            {
                minionScore += 2;
            }

            if (minion.AttackDamage == 0) minionScore /= 2;

            return minionScore;
        }

        public double GetScore(Game game)
        {
            double score = 0;

            if (game.CurrentPlayer.ToBeDestroyed)
            {
                return Double.MinValue;
            }
            if (game.CurrentOpponent.ToBeDestroyed)
            {
                return Double.MaxValue;
            }

            int ownHp = game.CurrentPlayer.Hero.Health + game.CurrentPlayer.Hero.Armor;
            int opponentHp = game.CurrentOpponent.Hero.Health + game.CurrentOpponent.Hero.Armor;
            score += ownHp - opponentHp;

            score += game.CurrentPlayer.HandZone.Count * 1.5;
            score -= game.CurrentOpponent.HandZone.Count * 1.5;

            score += game.CurrentPlayer.SecretZone.Count * 2;

            score += game.CurrentPlayer.BoardZone.Count * 2;
            score -= game.CurrentOpponent.BoardZone.Count * 2;

            foreach (Minion minion in game.CurrentPlayer.BoardZone)
            {
                score += GetMinionScore(minion);
            }
            foreach (Minion minion in game.CurrentOpponent.BoardZone)
            {
                score -= GetMinionScore(minion);
            }

            return score;
        }
    }

    /// <summary>
    /// Intended aggro palladin heuristic (board count of minions, ...)
    /// </summary>
    public class AggroPallyHeuristic : IGameStateHeuristic
    {
        private double GetMinionScore(Minion minion)
        {
            double minionScore = minion.AttackDamage + minion.Health;
            double baseScore = minionScore;

            if (minion.IsFrozen)
            {
                return minion.Health;
            }
            if (minion.HasTaunt)
            {
                minionScore += 2;
            }
            if (minion.HasWindfury)
            {
                minionScore += minion.AttackDamage * 0.5;
            }
            if (minion.HasDivineShield)
            {
                minionScore += 1.5f * baseScore;
            }
            if (minion.Card.Tags.ContainsKey(SabberStoneCore.Enums.GameTag.SPELLPOWER))
            {
                minionScore += minion.Card.Tags[SabberStoneCore.Enums.GameTag.SPELLPOWER];
            }
            if (minion.IsEnraged)
            {
                minionScore += 1;
            }
            if (minion.HasStealth)
            {
                minionScore += 1;
            }
            if (minion.CantBeTargetedBySpells)
            {
                minionScore += 1.5f * baseScore;
            }
            if (minion.HasLifeSteal)
            {
                minionScore += 1;
            }
            if (minion.Poisonous)
            {
                minionScore += 2;
            }

            if (minion.AttackDamage == 0) minionScore /= 2;

            return minionScore;
        }

        public double GetScore(Game game)
        {
            double score = 0;

            if (game.CurrentPlayer.ToBeDestroyed)
            {
                return Double.MinValue;
            }
            if (game.CurrentOpponent.ToBeDestroyed)
            {
                return Double.MaxValue;
            }

            int ownHp = game.CurrentPlayer.Hero.Health + game.CurrentPlayer.Hero.Armor;
            int opponentHp = game.CurrentOpponent.Hero.Health + game.CurrentOpponent.Hero.Armor;
            score += ownHp - opponentHp;
            
            score += game.CurrentPlayer.BoardZone.Count * 3;
            score -= game.CurrentOpponent.BoardZone.Count * 3;

            foreach (Minion minion in game.CurrentPlayer.BoardZone)
            {
                score += GetMinionScore(minion);
            }
            foreach (Minion minion in game.CurrentOpponent.BoardZone)
            {
                score -= GetMinionScore(minion);
            }

            return score;
        }
    }

    /// <summary>
    /// Aggro hunter heuristic, used during experiment 1.
    /// </summary>
    public class FaceHunterHeuristic : IGameStateHeuristic
    {
        private double GetMinionScore(Minion minion)
        {
            double minionScore = minion.AttackDamage + minion.Health;
            double baseScore = minionScore;

            if (minion.IsFrozen)
            {
                return minion.Health;
            }
            if (minion.HasTaunt)
            {
                minionScore += 2;
            }
            if (minion.HasWindfury)
            {
                minionScore += minion.AttackDamage * 0.5;
            }
            if (minion.HasDivineShield)
            {
                minionScore += 1.5f * baseScore;
            }
            if (minion.HasStealth)
            {
                minionScore += 1;
            }
            if (minion.CantBeTargetedBySpells)
            {
                minionScore += 1.5f * baseScore;
            }
            if (minion.HasLifeSteal)
            {
                minionScore += 1;
            }
            if (minion.Poisonous)
            {
                minionScore += 2;
            }
            if (minion.Race == SabberStoneCore.Enums.Race.BEAST)
            {
                minionScore += 1;
            }

            if (minion.AttackDamage == 0) minionScore = 0;

            return minionScore;
        }

        public double GetScore(Game game)
        {
            double score = 0;

            if (game.CurrentPlayer.ToBeDestroyed)
            {
                return Double.MinValue;
            }
            if (game.CurrentOpponent.ToBeDestroyed)
            {
                return Double.MaxValue;
            }

            int ownHp = game.CurrentPlayer.Hero.Health + game.CurrentPlayer.Hero.Armor;
            int opponentHp = game.CurrentOpponent.Hero.Health + game.CurrentOpponent.Hero.Armor;
            score += 2*(ownHp - opponentHp);
            
            //NOT ORIGINAL
            score += 1.5 * game.CurrentPlayer.NumCardsPlayedThisTurn;
            
            score += game.CurrentPlayer.BoardZone.Count * 2;
            score -= game.CurrentOpponent.BoardZone.Count * 2;
            
            foreach (Minion minion in game.CurrentPlayer.BoardZone)
            {
                score += GetMinionScore(minion);
            }
            foreach (Minion minion in game.CurrentOpponent.BoardZone)
            {
                score -= GetMinionScore(minion);
            }

            return score;
        }
    }

    /// <summary>
    /// Control priest heuristic, used during experiment 2.
    /// </summary>
    public class ControlPriestHeuristic : IGameStateHeuristic
    {
        private double GetMinionScore(Minion minion)
        {
            double minionScore = minion.AttackDamage + minion.Health;
            double baseScore = minionScore;

            if (minion.IsFrozen)
            {
                return minion.Health;
            }
            if (minion.HasTaunt)
            {
                minionScore += 3;
            }
            if (minion.HasWindfury)
            {
                minionScore += minion.AttackDamage * 0.5;
            }
            if (minion.HasDivineShield)
            {
                minionScore += 1.5f * baseScore;
            }
            if (minion.Card.Tags.ContainsKey(SabberStoneCore.Enums.GameTag.SPELLPOWER))
            {
                minionScore += minion.Card.Tags[SabberStoneCore.Enums.GameTag.SPELLPOWER];
            }
            if (minion.IsEnraged)
            {
                minionScore += 1;
            }
            if (minion.HasStealth)
            {
                minionScore += 1;
            }
            if (minion.CantBeTargetedBySpells)
            {
                minionScore += 1.5f * baseScore;
            }

            if (minion.AttackDamage == 0) minionScore /= 2;

            return minionScore;
        }

        public double GetScore(Game game)
        {
            double score = 0;

            if (game.CurrentPlayer.ToBeDestroyed)
            {
                return Double.MinValue;
            }
            if (game.CurrentOpponent.ToBeDestroyed)
            {
                return Double.MaxValue;
            }

            int ownHp = game.CurrentPlayer.Hero.Health + game.CurrentPlayer.Hero.Armor;
            int opponentHp = game.CurrentOpponent.Hero.Health + game.CurrentOpponent.Hero.Armor;
            score += 0.8*ownHp;
            score -= 0.2 * opponentHp;

            score += game.CurrentPlayer.BoardZone.Count * 3;
            score -= game.CurrentOpponent.BoardZone.Count * 3;

            score += 1.5*(game.CurrentPlayer.HandZone.Count - game.CurrentOpponent.HandZone.Count);

            foreach (Minion minion in game.CurrentPlayer.BoardZone)
            {
                score += GetMinionScore(minion);
            }
            foreach (Minion minion in game.CurrentOpponent.BoardZone)
            {
                score -= GetMinionScore(minion);
            }

            return score;
        }
    }
}
