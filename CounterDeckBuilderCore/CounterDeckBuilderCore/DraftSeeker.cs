using System;
using System.Collections.Generic;
using System.Linq;
using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using SabberStoneCore.Enums;
using SabberStoneCore.Config;
using SabberStoneCore.Tasks;
using System.Reflection;
using SabberStoneCore.Actions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Xml.Serialization;

namespace CounterDeckBuilder
{
    /// <summary>
    /// Utility extensions to IEnumerable
    /// </summary>
    public static class MathsExtension
    {
        public static IEnumerable<IEnumerable<T>> Combinations<T>(this IEnumerable<T> elements, int k)
        {
            return k == 0 ? new[] { new T[0] } : elements.SelectMany((e, i) =>   elements.Skip(i + 1).Combinations(k - 1).Select(c => (new[] { e }).Concat(c)));
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> list, int size)
        {
            var r = new Random();
            var shuffledList = list.Select(x => new { Number = r.Next(), Item = x }).OrderBy(x => x.Number).Select(x => x.Item).Take(size); 

            return shuffledList.ToList();
        }

        public static double NextGaussian(this Random random)
        {
            double u1 = 1.0 - random.NextDouble(); 
            double u2 = 1.0 - random.NextDouble();

            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

        public static double Variance(this Deck deck)
        {
            int numgames = deck.simulationResult.DeckGames;
            int numdecks = deck.simulationResult.Results.Count;
            double EX = 0;
            double EsquaredX = 0;
            foreach (int result in deck.simulationResult.Results)
            {
                EX += result / numdecks;
                EsquaredX += (result * result) / numdecks;
            }
            double squaredEX = EX * EX;

            return Math.Sqrt(EsquaredX - squaredEX);
        }

        public static double GetVariance(this IEnumerable<double> list)
        {
            double EX = 0;
            double EsquaredX = 0;
            var count = list.Count();

            foreach (var dou in list)
            {
                EX += dou / count;
                EsquaredX += (dou * dou) / count;
            }
            double squaredEX = EX * EX;

            return Math.Sqrt(EsquaredX - squaredEX);
        }
    }

    /// <summary>
    /// Hacks into SabberStone...
    /// </summary>
    public static class GameExtension
    {
        /// <summary>
        /// Hack into Sabberstone, fake cards in starting hand, do not shuffle, player1 always starts.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="hand"></param>
        /// <param name="deck"></param>
        public static void FairStartGame(this Game game, List<Card> hand, List<Card> deck, List<Card> refHand, List<Card> refdeck)
        {
            GameConfig _gameConfig = game.GetPrivateField<GameConfig>("_gameConfig");
            if (refHand is null) refdeck = refdeck.Shuffle(deck.Count).ToList();

            // setting up the decks ...
            deck?.ForEach(p =>
            {
                game.Player1.DeckCards.Add(p);
                Entity.FromCard(game.Player1, p, null, game.Player1.DeckZone);
            });
            refdeck?.ForEach(p =>
            {
                game.Player2.DeckCards.Add(p);
                Entity.FromCard(game.Player2, p, null, game.Player2.DeckZone);
            });

            if (_gameConfig.FillDecks)
            {
                game.Player1.DeckZone.Fill(_gameConfig.FillDecksPredictably ? _gameConfig.UnPredictableCardIDs : null);
                game.Player2.DeckZone.Fill(_gameConfig.FillDecksPredictably ? _gameConfig.UnPredictableCardIDs : null);
            }

            // set gamestats
            game.State = State.RUNNING;
            game.Player1.PlayState = PlayState.PLAYING;
            game.Player2.PlayState = PlayState.PLAYING;

            // tested player always starts
            game.FirstPlayer = game.Player1;
            var prop = game.GetType().GetField("_currentPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
            prop.SetValue(game, game.FirstPlayer);

            // first turn
            game.Turn = 1;

            //the first player
            hand?.ForEach(p =>
            {
                Entity.FromCard(game.Player1, p, null, game.Player1.HandZone);
            });

            if (refHand == null)
            {
                //take care of second player
                //game.Player2.DeckZone.Shuffle();
                Generic.Draw(game.Player2);
                Generic.Draw(game.Player2);
                Generic.Draw(game.Player2);
                Generic.Draw(game.Player2);
                IPlayable coin = Entity.FromCard(game.FirstPlayer.Opponent, Cards.FromId("GAME_005"), new EntityData.Data
                {
                    [GameTag.ZONE] = (int)Zone.HAND,
                    [GameTag.CARDTYPE] = (int)CardType.SPELL,
                    [GameTag.CREATOR] = game.FirstPlayer.Opponent.PlayerId
                });
                Generic.AddHandPhase(game.FirstPlayer.Opponent, coin);
            }
            else
            //fake also opponents hand
            {
                //game.Player2.DeckZone.Shuffle();
                refHand?.ForEach(p =>
                {
                    Entity.FromCard(game.Player2, p, null, game.Player2.HandZone);
                });

                Generic.Draw(game.Player2);
                IPlayable coin = Entity.FromCard(game.FirstPlayer.Opponent, Cards.FromId("GAME_005"), new EntityData.Data
                {
                    [GameTag.ZONE] = (int)Zone.HAND,
                    [GameTag.CARDTYPE] = (int)CardType.SPELL,
                    [GameTag.CREATOR] = game.FirstPlayer.Opponent.PlayerId
                });
                Generic.AddHandPhase(game.FirstPlayer.Opponent, coin);
            }

            // set next step
            // no shuffling for the first player ;)
            game.NextStep = Step.MAIN_BEGIN;
        }

        /// <summary>
        /// No power history and other useless stuff...
        /// </summary>
        /// <param name="game"></param>
        /// <param name="gameTask"></param>
        public static void LazyProcess(this Game game, PlayerTask gameTask)
        {
            gameTask.Game = game;
            gameTask.Process();
        }

        public static Game LazyClone(this Game game)
        {
            GameConfig _gameConfig = game.GetPrivateField<GameConfig>("_gameConfig");
            GameConfig gameConfig = _gameConfig.Clone();

            var newgame = new Game(gameConfig, false)
            {
                CloneIndex = $"{game.CloneIndex}[{game.NextCloneIndex++}]"
            };
            newgame.Player1.Stamp(game.Player1);
            newgame.Player2.Stamp(game.Player2);
            newgame.Stamp(game);

            newgame.SetIndexer(game.GetPrivateField<int>("_idIndex"), game.GetPrivateField<int>("_oopIndex"));

            return newgame;
        }

        //extension methods cannot access private fields, so we have to "hack" into Sabberstone
        public static T GetPrivateField<T>(this object obj, string name)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            System.Type type = obj.GetType();
            FieldInfo field = type.GetField(name, flags);
            return (T)field.GetValue(obj);
        }
    }

    public class Draft : IComparable<Draft>
    {
        public Card[] cards;
        public double Winrate = 0.0;

        public Draft(int draftCount)
        {
            cards = new Card[draftCount];
        }

        public int CompareTo(Draft other)
        {
            return Winrate.CompareTo(other.Winrate);
        }

        public static void SerializeDrafts(List<Draft> drafts, string deckname)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(String.Format(".\\drafts\\{0}.drafts", deckname), FileMode.Create, FileAccess.Write, FileShare.None);

            List<List<string>> cardNames = new List<List<string>>();
            foreach (Draft d in drafts)
            {
                List<string> lst = new List<string>();
                foreach (Card c in d.cards)
                {
                    lst.Add(c.Name);
                }
                cardNames.Add(lst);
            }

            formatter.Serialize(stream, cardNames);
            stream.Close();
        }

        public static void XmlSerializeDrafts(List<Draft> drafts, string deckname)
        {
            XmlSerializer formatter = new XmlSerializer(typeof(List<List<string>>));
            Stream stream = new FileStream(String.Format(".\\drafts\\XML{0}.xml", deckname), FileMode.Create, FileAccess.Write, FileShare.None);

            List<List<string>> cardNames = new List<List<string>>();
            foreach (Draft d in drafts)
            {
                List<string> lst = new List<string>();
                foreach (Card c in d.cards)
                {
                    lst.Add(c.Name);
                }
                cardNames.Add(lst);
            }

            formatter.Serialize(stream, cardNames);
            stream.Close();
        }

        public static List<Draft> DeserializeDrafts(string deckname)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(String.Format("./drafts/{0}.drafts", deckname), FileMode.Open, FileAccess.Read, FileShare.Read);
            List<List<string>> data = (List<List<string>>) formatter.Deserialize(stream);
            stream.Close();

            List<Draft> drafts = new List<Draft>();
            foreach (List<string> strings in data)
            {
                Draft d = new Draft(3);
                Card[] cards = new Card[3];
                for (int i = 0; i < 3; i++)
                {
                    Card c = Cards.FromName(strings[i]);
                    cards[i] = c;
                }
                d.cards = cards;
                drafts.Add(d);
            }

            List<Draft> result = new List<Draft>();
            result.Add(drafts[0]);
            result.Add(drafts[drafts.Count / 2]);
            result.Add(drafts[drafts.Count - 1]);

            return result;
        }

        public static List<Draft> XmlDeserializeDrafts(string deckname)
        {
            XmlSerializer formatter = new XmlSerializer(typeof(List<List<string>>));
            //                      .\drafts\XMLdeck.xml or ./drafts/XMLdeck.xml
            string filename = "." + Path.DirectorySeparatorChar +"drafts" + Path.DirectorySeparatorChar + "XML{0}.xml";
            Stream stream = new FileStream(String.Format(filename, deckname), FileMode.Open, FileAccess.Read, FileShare.Read);
            List<List<string>> data = (List<List<string>>)formatter.Deserialize(stream);
            stream.Close();

            List<Draft> drafts = new List<Draft>();
            foreach (List<string> strings in data)
            {
                Draft d = new Draft(3);
                Card[] cards = new Card[3];
                for (int i = 0; i < 3; i++)
                {
                    Card c = Cards.FromName(strings[i]);
                    cards[i] = c;
                }
                d.cards = cards;
                drafts.Add(d);
            }

            List<Draft> result = new List<Draft>();
            result.Add(drafts[0]);
            result.Add(drafts[drafts.Count / 2]);
            result.Add(drafts[drafts.Count - 1]);

            return result;
        }
    }

    public class DraftSeeker
    {
        private List<Card> _draftDeck = null;
        private List<Deck> _refDecks = null;
        private static Random r = new Random();
        private CardClass class1;
        private IPlayer player1 = new HeuristicStepGreedyPlayer(new DefaultHeuristic());
        //private IPlayer player2 = new RandomGreedyPlayer();

        public DraftSeeker(CardClass c1, List<Card> deck, List<Deck> refdecks) 
        {
            _draftDeck = deck;
            _refDecks = refdecks;
            class1 = c1;
        }

        public List<Draft> GetAllDrafts(int sequenceLength)
        {
            //does not make sense for less than four cards
            if (sequenceLength < 4) return null;

            List<Draft> retval = new List<Draft>();

            //create all possible hands
            Dictionary<Draft, Tuple<int, int>> draftsWinrates = new Dictionary<Draft, Tuple<int, int>>();
            var allHands = _draftDeck.Combinations(3).ToList();
            int counter = 0;
            List<string> handnames = new List<string>();

            //3 cards in hand
            sequenceLength -= 3;

            //for each hand
            foreach (var hand in allHands)
            {
                //heuristic part, do not try same hands multiple times
                string handname = String.Join("-", hand.ToList().OrderBy(x => x.Name));
                if (handnames.Contains(handname)) continue;
                else handnames.Add(handname);

                Draft d = new Draft(3);
                d.cards = hand.ToArray();
                draftsWinrates[d] = new Tuple<int, int>(0, 0);

                List<Card> restDeck = new List<Card>(_draftDeck);
                foreach (Card c in hand)
                {
                    restDeck.Remove(c);
                }           

                //find all drafts
                var restDrafts = restDeck.Combinations(sequenceLength);

                //test one draft!
                foreach (var restDraft in restDrafts)
                {
                    List<Card> newDeck = new List<Card>(restDeck);
                    foreach (Card c in restDraft)
                    {
                        newDeck.Remove(c);
                    }

                    newDeck = newDeck.Shuffle(newDeck.Count).ToList();

                    //add the selected cards to the top deck
                    foreach (Card c in restDraft)
                    {
                        newDeck.Add(c);
                    }
                    newDeck.Reverse();

                    //now everything should be finally ready
                    try
                    {
                        Deck rand = _refDecks[r.Next(_refDecks.Count)];
                        var game = new Game(new GameConfig
                        {
                            StartPlayer = 1,
                            Player1HeroClass = class1,
                            Player2HeroClass = rand.heroClass,
                            FillDecks = false,
                            Logging = false,
                            History = false
                        });
                        game.FairStartGame(hand.ToList(), newDeck, null, rand.deck);

                        player1.SetGame(game);
                        //player2.SetGame(game);

                        while (game.State != State.COMPLETE)
                        {
                            PlayerTask option = player1.GetNextMove();
                            //if (game.CurrentPlayer.Name == "Player1") option = player1.GetNextMove();
                            //else option = player2.GetNextMove();

                            //Console.WriteLine(option.FullPrint());
                            //Console.ReadLine();

                            game.LazyProcess(option);
                        }

                        //save the result
                        if (game.Player1.PlayState == PlayState.WON)
                        {
                            Tuple<int, int> t = draftsWinrates[d];
                            draftsWinrates[d] = new Tuple<int, int>(t.Item1 + 1, t.Item2);
                        }
                        else
                        {
                            Tuple<int, int> t = draftsWinrates[d];
                            draftsWinrates[d] = new Tuple<int, int>(t.Item1, t.Item2 + 1);
                        }
                        counter++;
                        Console.WriteLine("Game " + counter.ToString() + " ends!");
                    }
                    catch
                    {
                        string b = "";
                    }

                }
                int totalgames = draftsWinrates[d].Item1 + draftsWinrates[d].Item2;
                d.Winrate = draftsWinrates[d].Item1 * 100 / totalgames;
                retval.Add(d);
            }

            return retval;
        }
    }
}
