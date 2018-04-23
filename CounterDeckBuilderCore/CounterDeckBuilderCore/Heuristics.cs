using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using System;

namespace CounterDeckBuilder
{
    public interface IGameStateHeuristic
    {
        double GetScore(Game game);
    }

    public enum Heuristic
    {
        FACE_HUNTER, HEARTH_AGENT, AGGRO_PALLY, SECRET_MAGE, CONTROL_PRIEST, BASIC, DEFAULT
    }

    public static class HeuristicExtension
    {
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
    /// What to do with battlecries and deathrattles?
    /// </summary>
    public class HearthAgentHeuristic : IGameStateHeuristic
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

    public class DefaultHeuristic : IGameStateHeuristic
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
