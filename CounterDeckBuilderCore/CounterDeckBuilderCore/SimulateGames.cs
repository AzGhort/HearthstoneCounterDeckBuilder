using System;
using SabberStoneCore.Model;
using SabberStoneCore.Config;
using SabberStoneCore.Enums;
using SabberStoneCore.Tasks;
using SabberStoneCore.Tasks.PlayerTasks;
using System.Collections.Generic;

namespace CounterDeckBuilder
{
    /// <summary>
    /// Detail info about simulation result.
    /// </summary>
    public class SimulationResult
    {
        /// <summary>
        /// Number of games won/lost.
        /// </summary>
        public Tuple<int, int> Total;

        /// <summary>
        /// Number of games for every opponent deck.
        /// </summary>
        public int DeckGames = 0;

        /// <summary>
        /// Number of games WON against every opponent deck.
        /// </summary>
        public List<int> Results = new List<int>();

        /// <summary>
        /// Winrate of deck in percents.
        /// </summary>
        public double Winrate;
    }

    /// <summary>
    /// Configuration for Simulation class (eg. IPlayers, decks, num of games, ...)
    /// </summary>
    public class SimulatorConfiguration
    {
        public IPlayer ai1;
        public IPlayer ai2;
        public int numGames;
        public Deck deck1;
        public Deck deck2;
        public List<Card> hand1;
        public List<Card> hand2;

        public SimulatorConfiguration(IPlayer ai1, IPlayer ai2, int totalGames, Deck deck_1, Deck deck_2, List<Card> hand1, List<Card> hand2)
        {
            this.ai1 = ai1;
            this.ai2 = ai2;
            numGames = totalGames;
            deck1 = deck_1;
            deck2 = deck_2;
            this.hand1 = hand1;
            this.hand2 = hand2;
        }
    }

    /// <summary>
    /// Simulator class, used for executing games with given configuration in SabberStone.
    /// </summary>
    public class Simulator
    {
        private int _numberOfGamesToSimulate;

        IPlayer AI1;
        IPlayer AI2;

        Deck Deck1;
        Deck Deck2;

        string deckname1;
        string deckname2;
        
        Game currentGame;

        /// <summary>
        /// Calls SimulateGames with given configuration.
        /// </summary>
        /// <param name="conf"> Configuration of simulations. </param>
        /// <returns> Just number of games won/lost (memory issues, and we do not need more info in our algorithm). </returns>
        public Tuple<int, int> SimulateGames(SimulatorConfiguration conf)
        {
            return this.SimulateGames(conf.ai1, conf.ai2, conf.numGames, conf.deck1, conf.deck2, conf.hand1, conf.hand2);
        }

        /// <summary>
        /// Main method executing games with given config.
        /// </summary>
        /// <param name="ai1"> IPlayer for first player. </param>
        /// <param name="ai2"> IPlayer for second player. </param>
        /// <param name="totalGames"> Number of games to play. </param>
        /// <param name="deck_1"> Deck of first player. </param>
        /// <param name="deck_2"> Deck of second player. </param>
        /// <param name="hand1"> Can be null. If not null, then second hand is also expected to not be null. Hand to use for first player. </param>
        /// <param name="hand2"> Hand to use for second player. </param>
        /// <returns></returns>
        public Tuple<int, int> SimulateGames(IPlayer ai1, IPlayer ai2, int totalGames, Deck deck_1, Deck deck_2, List<Card> hand1, List<Card> hand2)
        {
            SimulationResult sim = new SimulationResult();
            AI1 = ai1;
            AI2 = ai2;
            _numberOfGamesToSimulate = totalGames;
            Deck1 = deck_1;
            Deck2 = deck_2;

            int total = _numberOfGamesToSimulate;
            int Player1Wins = 0;
            int Player2Wins = 0;
            int i = 0;

            //to prevent infinite loops
            int error_counter = 0;
            
            while (i < total)
            {
                error_counter = 0;
                try
                {
                    var game = new Game(new GameConfig
                    {
                        StartPlayer = 1,
                        Player1HeroClass = deck_1.heroClass,
                        Player2HeroClass = deck_2.heroClass,
                        FillDecks = false,
                        Logging = false,
                        History = false
                    });

                    currentGame = game;

                    //shuffling
                    if (deck_1.positions.Count != 0)
                    {
                        deck_1.deck = new List<Card>(deck_1.deck.Shuffle(30));
                        deck_2.deck = new List<Card>(deck_2.deck.Shuffle(30));
                        deck_1.deck.Reverse();
                        foreach (KeyValuePair<Card, int> pair in deck_1.positions)
                        {
                            int index = deck_1.deck.FindIndex(c => c.Name == pair.Key.Name);
                            int realpos = (pair.Value > 0) ? pair.Value - 1 : 0;
                            Card replaced = deck_1.deck[realpos];
                            deck_1.deck[realpos] = pair.Key;
                            deck_1.deck[index] = replaced;
                        }
                        deck_1.deck.Reverse();
                    }
                    else
                    {
                        deck_1.deck = new List<Card>(deck_1.deck.Shuffle(30));
                        deck_2.deck = new List<Card>(deck_2.deck.Shuffle(30));
                    }


                    if (hand1 != null)
                    {
                        List<Card> d1 = new List<Card>(deck_1.deck);
                        List<Card> d2 = new List<Card>(deck_2.deck);
                        foreach (Card c in hand1)
                        {
                            d1.Remove(c);
                        }
                        foreach (Card c in hand2)
                        {
                            d2.Remove(c);
                        }
                        game.FairStartGame(hand1, d1, hand2, d2);
                    }
                    else game.StartGame();
                    AI1.SetGame(game);
                    AI2.SetGame(game);

                    while (game.State != State.COMPLETE)
                    {
                        PlayerTask option = null;

                        if (game.CurrentPlayer.Name == "Player1") option = AI1.GetNextMove();
                        else option = AI2.GetNextMove();

                        if (error_counter > 10000)
                        {
                            error_counter = 0;
                            throw new Exception();
                        }

                        // if you want to know whats happening in the game...
                        //Console.WriteLine(option.FullPrint());
                        //Console.ReadLine();

                        if (option is EndTurnTask)
                        {
                            error_counter = 0;
                            game.Process(option);
                        }
                        else
                        {
                            error_counter++;
                            game.Process(option);
                        }
                    }
                    if (game.Player1.PlayState == PlayState.WON) Player1Wins++;
                    else Player2Wins++;
                    //Console.WriteLine("Game " + i + " ends!");
                    i++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Game " + i + " failed!");
                }            
            }
            //Console.WriteLine($"Player1 won {Player1Wins}, Player2 won {Player2Wins}");
            
            return new Tuple<int, int>(Player1Wins, Player2Wins);
        }
    }
}