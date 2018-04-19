using System;
using SabberStoneCore.Model;
using SabberStoneCore.Config;
using SabberStoneCore.Enums;
using SabberStoneCore.Tasks;
using SabberStoneCore.Tasks.PlayerTasks;
using System.Collections.Generic;

namespace CounterDeckBuilder
{
    public class SimulationResult
    {
        public Tuple<int, int> Total;

        public int DeckGames = 0;

        public List<int> Results = new List<int>();

        public double Winrate;
    }

    public class SimulatorConfiguration
    {
        public IPlayer ai1;
        public IPlayer ai2;
        public int numGames;
        public Deck deck1;
        public Deck deck2;
        public string name1;
        public string name2;
        public List<Card> hand1;
        public List<Card> hand2;

        public SimulatorConfiguration(IPlayer ai1, IPlayer ai2, int totalGames, Deck deck_1, Deck deck_2, string deckn1, string deckn2, List<Card> hand1, List<Card> hand2)
        {
            this.ai1 = ai1;
            this.ai2 = ai2;
            numGames = totalGames;
            deck1 = deck_1;
            deck2 = deck_2;
            name1 = deckn1;
            name2 = deckn2;
            this.hand1 = hand1;
            this.hand2 = hand2;
        }
    }

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

        public Tuple<int, int> SimulateGames(SimulatorConfiguration conf)
        {
            return this.SimulateGames(conf.ai1, conf.ai2, conf.numGames, conf.deck1, conf.deck2, conf.name1, conf.name2, conf.hand1, conf.hand2);
        }

        public Tuple<int, int> SimulateGames(IPlayer ai1, IPlayer ai2, int totalGames, Deck deck_1, Deck deck_2, string deckn1, string deckn2, List<Card> hand1, List<Card> hand2)
        {
            SimulationResult sim = new SimulationResult();
            AI1 = ai1;
            AI2 = ai2;
            _numberOfGamesToSimulate = totalGames;
            Deck1 = deck_1;
            Deck2 = deck_2;
            deckname1 = deckn1;
            deckname2 = deckn2;

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