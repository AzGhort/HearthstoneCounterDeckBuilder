using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;

namespace CounterDeckBuilder
{
    /// <summary>
    /// OBSOLETE - Metric used in GameState class.
    /// </summary>
    public struct Metric
    {
        public string Player { get; set; }
        public int Value { get; set; }
    }

    /// <summary>
    /// OBSOLETE - GameState class, representing current state of game by metrics.
    /// </summary>
    public struct GameState
    {
        public string Name;

        #region One-sided metrics
        //this is not very "useful" metric for an immediate state of the game
        public Metric ManaSpent;
        //total number of cards drawn this turn
        public Metric CardsDrawn;
        //sum of all mana the player "has on board" - minion's + weapon's manacosts
        public Metric BoardMana;
        //number of minions
        public Metric BoardSize;
        //number of cards in hand
        public Metric HandSize;
        //sum of damage the player has set up for next turn
        public Metric DamageSetUp;
        //boolean metric ;)
        public Metric SetUpLethal;
        #endregion

        #region Memory of last task

        public int Taunt;
        public int DivineShield;
        public int Charge;

        public int ManaCost;
        public int CreatedOptions;
        #endregion
        
        #region Metrics respecting opponent
        //number of all cards on board + in hand
        public Metric CardAdvantage;
        //health advantage
        public Metric HealthAdvantage;
        //sum of all mana the player "has on board" - minions
        public Metric BoardManaAdvantage;
        //number of minions
        public Metric BoardSizeAdvantage;
        //sum of damage the player has set up for next turn
        public Metric DamageSetUpAdvantage;
        //for each minion, count of all opponent's minions that can be traded with it
        //this one is weird, not sure if it's any useful
        public Metric TradeAdvantage;
        #endregion

        public GameState(string name)
        {
            Name = name;
            ManaSpent = new Metric();
            CardsDrawn = new Metric();
            BoardMana = new Metric();
            BoardSize = new Metric();
            HandSize = new Metric();
            DamageSetUp = new Metric();
            CardAdvantage = new Metric();
            HealthAdvantage = new Metric();
            SetUpLethal = new Metric();
            TradeAdvantage = new Metric();

            Taunt = 0;
            DivineShield = 0;
            Charge = 0;
            ManaCost = 0;
            CreatedOptions = 0;

            BoardManaAdvantage = new Metric();
            BoardSizeAdvantage = new Metric();
            DamageSetUpAdvantage = new Metric();
        }
    }

    /// <summary>
    /// OBSOLETE - Observer returning state of game.
    /// </summary>
    public class GameStateObserver
    {
        /// <summary>
        /// Get current game state (defined by metrics).
        /// </summary>
        /// <param name="game"> Game to create the state from. </param>
        /// <returns> GameState class with current metrics. </returns>
        public static GameState GetGameState(Game game)
        {
            GameState state = new GameState();

            state.ManaSpent.Value = game.CurrentPlayer.UsedMana;
            state.HandSize.Value = game.CurrentPlayer.HandZone.Count;
            state.CardsDrawn.Value = game.CurrentPlayer.NumCardsDrawnThisTurn;
            state.BoardSize.Value = game.CurrentPlayer.BoardZone.Count;
            state.HealthAdvantage.Value = (game.CurrentPlayer.Hero.Health + game.CurrentPlayer.Hero.Armor) - (game.CurrentOpponent.Hero.Health + game.CurrentOpponent.Hero.Armor);
            int opponentMana = 0;
            int opponentDamage = 0;
            foreach (Minion m in game.CurrentPlayer.BoardZone)
            {
                state.BoardMana.Value += m.Cost;
                if (!m.Freeze) state.DamageSetUp.Value += m.AttackDamage;
                /*foreach (Minion min in game.CurrentOpponent.BoardZone)
                {
                    if (min.AttackDamage < m.Health && min.Health <= m.AttackDamage)
                    {
                        state.TradeAdvantage.Value++;
                    }
                }*/
            }
            foreach (Minion min in game.CurrentOpponent.BoardZone)
            {
                opponentMana += min.Cost;
                if (!min.Freeze) opponentDamage += min.AttackDamage;
            }
            if (!(game.CurrentPlayer.Hero.Weapon == null)) state.DamageSetUp.Value += game.CurrentPlayer.Hero.Weapon.AttackDamage;
            if (!(game.CurrentOpponent.Hero.Weapon == null)) opponentDamage += game.CurrentOpponent.Hero.Weapon.AttackDamage;

            state.SetUpLethal.Value = (state.DamageSetUp.Value >= game.CurrentOpponent.Hero.Health + game.CurrentOpponent.Hero.Armor) ? 1 : 0;
            state.CardAdvantage.Value = (state.BoardSize.Value + state.HandSize.Value) - (game.CurrentOpponent.BoardZone.Count + game.CurrentOpponent.HandZone.Count);
            state.BoardManaAdvantage.Value = state.BoardMana.Value - opponentMana;
            state.BoardSizeAdvantage.Value = state.BoardSize.Value - game.CurrentOpponent.BoardZone.Count;
            state.DamageSetUpAdvantage.Value = state.DamageSetUp.Value - opponentDamage;

            return state;
        }
    }
}
