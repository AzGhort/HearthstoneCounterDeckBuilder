using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CounterDeckBuilder
{
    public interface IEvolvable : IComparable<IEvolvable>
    {
        Deck GetThisDeck();

        List<Tuple<string, string>> Mutations();
        
        IEvolvable Crossover(IEvolvable deck);

        IEvolvable Mutate(int num);
    }

    /// <summary>
    /// General deck class, used everywhere in CounterDeckBuilder.
    /// </summary>
    public class Deck
    {
        #region Static stuff
        static Random r = new Random();

        public static List<Card> allAvailableCards = Cards.AllStandard.ToList();

        public static Card GetRandomCardOfManaCost(CardClass heroClass, int manacost)
        {
            int loop = 0;
            List<Card> class_ok = new List<Card>();
            class_ok.AddRange(allAvailableCards.Where(card => (card.Class == CardClass.NEUTRAL) || (card.Class == heroClass)));
            Card c = class_ok[r.Next(class_ok.Count)];
            while (c.Cost != manacost)
            {
                c = class_ok[r.Next(class_ok.Count)];
                loop++;
                if (loop == 10000)
                {
                    manacost += 1;
                    loop = 0;
                }
            }
            return c;
        }

        public static List<List<int>> GetManaCurves(IEnumerable<Deck> decks)
        {
            List<List<int>> manacurves = new List<List<int>>();
            foreach (var deck in decks)
            {
                List<int> manacurve = new List<int>();
                foreach (var card in deck.deck)
                {
                    manacurve.Add(card.Cost);
                }
                manacurves.Add(manacurve);
            }
            return manacurves;
        }
        #endregion
        public string Deckname;
        public Double Winrate = 0;
        public Double Variance = 0;
        public CardClass heroClass;
        public List<Card> deck = new List<Card>();
        public List<Draft> myHands = new List<Draft>();
        public SimulationResult simulationResult = new SimulationResult();
        public List<int> myCurve = new List<int>();

        //used to prevent certain cards from ever being on starting hand
        public List<Card> forbiddenOnHand = new List<Card>();
        //used to put some cards at certain positions in every game...
        public Dictionary<Card, int> positions = new Dictionary<Card, int>();

        /// <summary>
        /// Generates a random standard deck that follows the given mana curve.
        /// </summary>
        /// <param name="referenceCurve"></param>
        /// <param name="classindex"></param>
        public Deck(List<int> referenceCurve, List<Card> referenceDeck, int classindex)
        {
            myCurve = new List<int>();

            //numbers 2 - 10
            classindex %= 9;
            classindex += 2;
            var a = CardClass.DRUID;
            heroClass = (CardClass) Enum.GetValues(a.GetType()).GetValue(classindex);

            foreach (int cost in referenceCurve)
            {
                Card c = GetRandomCardOfManaCost(heroClass, cost);
                while ((c.Rarity == Rarity.LEGENDARY && deck.Contains(c)) || (deck.FindAll(card => card.Name == c.Name).Count == 2))
                {
                    c = GetRandomCardOfManaCost(heroClass, cost);
                }
                deck.Add(c);
            }

            Draft myhand = new Draft(3);
            //only one hand for now...
            myhand.cards = new Card[] { deck[r.Next(0, 4)], deck[r.Next(5, 9)], deck[r.Next(10, 14)] };
            myHands.Add(myhand);

            /*
            foreach (var hand in refHands)
            {
                Draft myhand = new Draft(3);
                int counter = 0;
                List<int> usedIndices = new List<int>();
                foreach (var card in hand.cards)
                {
                    int indexToCurve = referenceDeck.FindIndex(c => c == card);
                    if (usedIndices.Contains(indexToCurve)) indexToCurve++;
                    Card newCard = deck[indexToCurve];
                    myhand.cards[counter] = newCard;
                    counter++;
                    usedIndices.Add(indexToCurve);
                }
                myHands.Add(myhand);
            }*/

        }    

        public Deck()
        {

        }

        public void FindHands(int[][] costs)
        {
            foreach (int[] handCost in costs)
            {
               Draft hand = new Draft(3);
               List<Card> cards = new List<Card>();
               foreach (int cost in handCost)
               {
                   Card c = deck.Find(card => (!forbiddenOnHand.Contains(card)) && (card.Cost == cost));
                   int a = cost;
                   while (c == null)
                   {
                       a--;
                       c = deck.Find(card => (!forbiddenOnHand.Contains(card)) && (card.Cost == a));
                       if (a < 0) a = 4;
                   }
                   cards.Add(c);
               }
               hand.cards = cards.ToArray();
               this.myHands.Add(hand);
            }          
        }

        public bool CardAvailable(Card c)
        {
            int count = deck.FindAll(card => card.Name == c.Name).Count;
            if (c.Rarity == Rarity.LEGENDARY) count++;
            return (count < 2);
        }

        public Deck Clone()
        {
            return (Deck) MemberwiseClone();
        }

        public Deck DeepCopy()
        {
            Deck d = new Deck();
            d.Deckname = this.Deckname;
            d.Winrate = this.Winrate;
            d.heroClass = this.heroClass;
            d.deck = new List<Card>(this.deck);
            d.myHands = new List<Draft>(this.myHands);
            d.forbiddenOnHand = new List<Card>(this.forbiddenOnHand);
            d.positions = new Dictionary<Card, int>(this.positions);

            return d;
        }
    }

    /// <summary>
    /// Dumb ievolvable deck, doesn't care about combos.
    /// </summary>
    public class DumbEvolvingDeck : IEvolvable
    {
        public static List<IEvolvable> GetFirstGeneration(List<Deck> referenceDecks, int generationCount)
        {
            Random r = new Random();
            List<IEvolvable> result = new List<IEvolvable>();
            for (int i = 0; i < generationCount; i++)
            {
                Deck refd = referenceDecks[i % referenceDecks.Count];
                int hero = r.Next(2, 11);
                Deck deck = new Deck(refd.myCurve, refd.deck, 4); //MAGE NOW
                result.Add(new DumbEvolvingDeck() { thisDeck = deck });
            }
            return result;
        }

        public Deck thisDeck;
        public List<Tuple<string, string>> mutations;

        public int CompareTo(IEvolvable other)
        {
            return thisDeck.Winrate.CompareTo(other.GetThisDeck().Winrate);
        }

        public IEvolvable Crossover(IEvolvable partner)
        {
            return this;
        }

        public Deck GetThisDeck()
        {
            return thisDeck;
        }

        public IEvolvable Mutate(int mutations)
        {
            Random r = new Random();
            Deck toMutate = thisDeck.Clone();
            for (int i = 0; i < mutations; i++)
            {
                int index = r.Next(30);
                Card c = Deck.GetRandomCardOfManaCost(thisDeck.heroClass, thisDeck.deck[index].Cost);
                toMutate.deck[index] = c;
            }
            toMutate.myHands = new List<Draft>()
            {
                new Draft(3) { cards = new Card[3] { toMutate.deck[0], toMutate.deck[5], toMutate.deck[9] } }
            };
            return new DumbEvolvingDeck()
            {
                thisDeck = toMutate
            };
        }

        public List<Tuple<string, string>> Mutations()
        {
            return mutations;
        }
    }

    public class EvolutionConfiguration
    {
        public Heuristic heuristic1;
        public Heuristic heuristic2;

        public Player player1;
        public Player player2;

        public int numGames;
        public List<Deck> refdecks;
        public List<IEvolvable> population;
    }

    public class EvolutionTester
    {
        public static void TestDeckWinrate(IEvolvable deck, List<Deck> referenceDecks, int numTries, int numberOfGames, int numThreads = 0)
        {
            List<IEvolvable> pop = new List<IEvolvable>();
            for (int i = 0; i < numTries; i++)
            {
                pop.Add(new DumbEvolvingDeck() { thisDeck = deck.GetThisDeck().DeepCopy() });
                pop[i].GetThisDeck().FindHands(new int[][] { new int[] { 1, 2, 3 }, new int[] { 2, 4, 5 }, new int[] { 4, 5, 6 } });
            }
            Evolution evol = new Evolution(pop, referenceDecks, numberOfGames);
            evol.Evolve(1, numThreads);
        }

        public static void TestMutationStability(IEvolvable parent, List<Deck> referenceDecks, int numberOfGames, int numThreads = 0)
        {
            var population = EvolutionTester.GetAllMutations(parent, true);

            Evolution evol = new Evolution(population, referenceDecks, numberOfGames);
            var lastgen = evol.Evolve(1, numThreads);

            StringBuilder buffer = new StringBuilder();
            foreach (IEvolvable deck in lastgen)
            {
                buffer.Append(deck.Mutations()[0].Item1);
                buffer.Append(" => ");
                buffer.Append(deck.Mutations()[0].Item2);
                buffer.Append(" \n ");
            }
            string str = buffer.ToString();

            using (System.IO.StreamWriter file = new System.IO.StreamWriter("mutations.txt", true))
            {
                file.WriteLine(str);
            }
        }

        public static List<IEvolvable> GetAllMutations(IEvolvable parent, bool costBound)
        {
            List<IEvolvable> population = new List<IEvolvable>();

            CardClass heroClass = parent.GetThisDeck().heroClass;
            List<Card> class_ok = new List<Card>();
            class_ok.AddRange(Cards.AllStandard.ToList().Where(card => (card.Class == CardClass.NEUTRAL) || (card.Class == heroClass)));
            //need to initialize this with some real card, 10 mana ultrasaur cannot be the first card of the deck hopefully
            Card lastcard = Cards.FromName("Ultrasaur");

            for (int i = 0; i < parent.GetThisDeck().deck.Count(); i++)
            {
                //unnecessary to mutate same cards... 
                if (parent.GetThisDeck().deck[i].Name == lastcard.Name)
                {
                    continue;
                }
                lastcard = parent.GetThisDeck().deck[i];

                foreach (Card mutation in class_ok)
                {
                    //mutate only cards with similar mana cost or the ones that are not already in the deck (3 copies...)
                    if (parent.GetThisDeck().deck.FindAll(card => card.Name == mutation.Name).Count == 2)
                    {
                        continue;
                    }
                    if (costBound && Math.Abs(mutation.Cost - parent.GetThisDeck().deck[i].Cost) > 1)
                    {
                        continue;
                    }
                    // these cards fuck up the heuristics
                    if (mutation.Name == "The Darkness")
                    {
                        continue;
                    }
                    DumbEvolvingDeck deck = new DumbEvolvingDeck()
                    {
                        thisDeck = parent.GetThisDeck().DeepCopy(),
                        mutations = new List<Tuple<string, string>>(new Tuple<string, string>[] { new Tuple<string, string>(parent.GetThisDeck().deck[i].Name, mutation.Name) })
                    };
                    if (deck.GetThisDeck().CardAvailable(mutation))
                    {
                        deck.GetThisDeck().deck[i] = mutation;
                        deck.GetThisDeck().forbiddenOnHand.Add(mutation);
                        deck.GetThisDeck().positions[mutation] = mutation.Cost;
                        population.Add(deck);
                    }
                }
            }

            return population;
        }
    }

    public class Evolution
    {
        private List<IEvolvable> currentGeneration;
        private List<Deck> testingDecks;
        private int numGames;
        private static Random r = new Random();

        private List<List<double>> results = new List<List<double>>();
        private Heuristic heuristic1;
        private Heuristic heuristic2;

        private Player player1;
        private Player player2;

        public Evolution(EvolutionConfiguration config)
        {
            testingDecks = config.refdecks;
            numGames = config.numGames;
            currentGeneration = config.population;

            heuristic1 = config.heuristic1;
            heuristic2 = config.heuristic2;

            player1 = config.player1;
            player2 = config.player2;
        }

        public Evolution(List<IEvolvable> population, List<Deck> referenceDecks, int numberOfGames)
        {
            currentGeneration = population;
            testingDecks = referenceDecks;
            numGames = numberOfGames;
        }

        public List<IEvolvable> Evolve(int generations, int threads = 0)
        {
            for (int i = 0; i < generations; i++)
            {
                currentGeneration = EvaluateGeneration(currentGeneration, threads);

                List<double> res = new List<double>();
                foreach (IEvolvable deck in currentGeneration)
                {
                    res.Add(deck.GetThisDeck().Winrate);
                }
                res.Sort();
                results.Add(res);

                //Console.WriteLine(String.Format("Getting generation number {0}...", i + 2));
                //not the last iteration...
                if (i < generations - 1) currentGeneration = GetNextGeneration();
            }
            currentGeneration.Sort();

            using (System.IO.StreamWriter file = new System.IO.StreamWriter("outcurve.txt", true))
            {
                foreach (var list in results)
                {
                    string tog = String.Join(" ", list);
                    file.WriteLine(tog);
                }
            }

            return currentGeneration;
        }

        /// <summary>
        /// Executes the hill climbing algorithm with given number of steps. Expects the number of decks in initial population is 1.
        /// </summary>
        /// <param name="iterations"></param>
        /// <param name="threads"></param>
        /// <returns></returns>
        public IEvolvable HillClimb(int iterations, int threads = 0)
        {
            IEvolvable parent = currentGeneration[0];
            for (int i = 0; i < iterations; i++)
            {
                var population = EvolutionTester.GetAllMutations(parent, true);
                Console.WriteLine("Generation number " + (i+1).ToString() + " in progress.");
                Console.WriteLine("Testing " + population.Count() + " decks...");
                //Console.WriteLine("-------------------------------------------");
                var evaluated = EvaluateGeneration(population, threads);

                currentGeneration.Sort();

                parent = HillClimbStep(20);
            }
            return parent;
        }

        public IEvolvable HillClimbLastGeneration(int num_top)
        {
            return HillClimbStep(num_top);
        }

        public List<IEvolvable> TestGenerationPartForBash(int parts, int index, int threads = 0)
        {
            IEvolvable parent = currentGeneration[0];
            var population = EvolutionTester.GetAllMutations(parent, true);

            int part = population.Count / parts;
            int mod = population.Count % parts;
            var tested = new List<IEvolvable>();

            for (int i = 0; i < parts; i++)
            {
                if (i == parts - 1) part += mod;
                tested = population.Take(part).ToList();
                if (i == index) break;
                population = population.Skip(part).ToList();
            }

            Console.WriteLine("Testing " + tested.Count() + " decks...");
            //Console.WriteLine("-------------------------------------------");
            var evaluated = EvaluateGeneration(tested, threads);

            evaluated.Sort();

            return evaluated;
        }

        /// <summary>
        /// Tests efficiency of mutation, eg. what is the winrate difference of two decks that differ in exactly one card, given that they draw that card always on time.  
        /// </summary>
        /// <param name="deck1"></param>
        /// <param name="deck2"></param>
        /// <returns></returns>
        public Tuple<double, double> TestMutationEfficiency(Deck deck1, Deck deck2, int numgames)
        {
            var cop1 = new List<Card>(deck1.deck);
            var cop2 = new List<Card>(deck2.deck);

            foreach (Card c in deck2.deck)
            {
                cop1.Remove(c);
            }
            foreach (Card c in deck1.deck)
            {
                cop2.Remove(c);
            }

            var card1 = cop1[0];
            var card2 = cop2[0];

            deck1.FindHands(new int[][] { new int[] { 1, 2, 3 }, new int[] { 2, 4, 5 }, new int[] { 4, 5, 6 } });
            deck2.FindHands(new int[][] { new int[] { 1, 2, 3 }, new int[] { 2, 4, 5 }, new int[] { 4, 5, 6 } });

            deck1.positions.Add(card1, card1.Cost - 1);
            deck2.positions.Add(card2, card2.Cost - 1);

            numGames = numgames;

            TestDeck(new DumbEvolvingDeck() { thisDeck = deck1 });
            TestDeck(new DumbEvolvingDeck() { thisDeck = deck2 });
            var result = new Tuple<double, double>(deck1.Winrate, deck2.Winrate);

            return result;
        }
       
        #region Private stuff
        /// <summary>
        /// Stochasticly chooses between top 20 steps. 
        /// </summary>
        /// <returns></returns>
        private IEvolvable HillClimbStep(int top)
        {
            if (top > 100) top = 100;

            //normal (0,1) random number
            double rand = r.NextGaussian();
            //fit into 0-4..
            if (rand < 0.0) rand *= -1.0;
            if (rand >= 4.0) rand = 3.99999;
            //fit into 0-100
            rand *= 100.0;
            rand /= 4.0;

            List<IEvolvable> candidates = new List<IEvolvable>();
            for (int i = 0; i < top; i++)
            {
                candidates.Add(currentGeneration[currentGeneration.Count - 1 - i]);
            }

            int index = (int) Math.Floor(rand) / (100 / top);

            return candidates[index];
        }

        private List<IEvolvable> GetNextGeneration()
        {
            List<IEvolvable> newGeneration = new List<IEvolvable>();
            currentGeneration.Sort();
            for (int i = currentGeneration.Count() - 10; i < currentGeneration.Count(); i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    newGeneration.Add(currentGeneration[i].Mutate(3));
                }
                newGeneration.Add(currentGeneration[i]);
            }
            return newGeneration;
        }

        /// <summary>
        /// Last (and only) method that knows anything about threads.
        /// </summary>
        /// <param name="population"></param>
        /// <param name="threads"></param>
        /// <returns></returns>
        private List<IEvolvable> EvaluateGeneration(List<IEvolvable> population, int threads)
        {
            List<IEvolvable> retval = new List<IEvolvable>();
            if (threads == 0)
            {
                retval = TestPopulation(population);
            }
            else
            {
                Stopwatch s = new Stopwatch();
                s.Start();
                //int test = threads;

                foreach (var deck in population)
                {
                    //deck.GetThisDeck().FindHands(new int[][] { new int[] { 1, 2, 3 }, new int[] { 2, 4, 5 }, new int[] { 4, 5, 6 } });
                }

                var copyGen = new List<IEvolvable>(population);
                int part = copyGen.Count / threads;
                int mod = copyGen.Count % threads;

                Console.WriteLine("Evaluating in " + threads.ToString() + " threads.");
                Console.WriteLine("Number of decks for every thread: " + part.ToString());
                Console.WriteLine("-------------------------------------------");

                Task<List<IEvolvable>>[] tasks = new Task<List<IEvolvable>>[threads];
                List<IEvolvable>[] results = new List<IEvolvable>[threads];

                for (int j = 0; j < threads; j++)
                {
                    int other = j;
                    if (j == threads - 1) part += mod;
                    List<IEvolvable> partGen = copyGen.Take(part).ToList();
                    copyGen = copyGen.Skip(part).ToList();
                    tasks[other] = Task<List<IEvolvable>>.Factory.StartNew(() => TestPopulation(partGen));
                    //results[j] = TestPopulation(partGen);
                }

                Task.WaitAll(tasks);

                for (int i = 0; i < tasks.Count(); i++)
                {
                    retval.AddRange(tasks[i].Result);
                }

                //retval = tasks.ToList().SelectMany(x => x).ToList();

                //retval = TestPopulation(testpart);
                s.Stop();

                Console.WriteLine("{0} decks were evaluated in " + (s.ElapsedMilliseconds / 1000).ToString() + " seconds.", population.Count());
                //Console.ReadLine();
            }

            return retval;
        }

        /// <summary>
        /// Or maybe do the parallelization here..?
        /// </summary>
        /// <param name="population"></param>
        /// <returns></returns>
        private List<IEvolvable> TestPopulation(List<IEvolvable> population)
        {
            for (int i = 0; i < population.Count(); i++)
            {
                Console.WriteLine("Testing deck number {0}.", i);
                TestDeck(population[i]);
            }
            return population;
        }

        private void TestDeck(IEvolvable deck)
        {
            Simulator simulator = new Simulator();
            int gamesCount = 0;
            int gamesWon = 0;
            IPlayer AI1 = player1.GetPlayer(heuristic1);
            IPlayer AI2 = player2.GetPlayer(heuristic2);

            deck.GetThisDeck().FindHands(new int[][] { new int[] { r.Next(5), r.Next(5), r.Next(5) }});

            foreach (Deck d in testingDecks)
            {
                int deckGamesWon = 0;
                int deckGamesCount = 0;
                foreach (Draft draft in d.myHands)
                {
                    foreach (Draft draft2 in deck.GetThisDeck().myHands)
                    {
                        deckGamesCount += numGames;
                        var result = simulator.SimulateGames(AI1, AI2, numGames, deck.GetThisDeck(), d, "", "", draft2.cards.ToList(), draft.cards.ToList());
                        deckGamesWon += result.Item1;
                    }
                }
                deck.GetThisDeck().simulationResult.Results.Add(deckGamesWon);
                gamesWon += deckGamesWon;
                gamesCount += deckGamesCount;
                deck.GetThisDeck().simulationResult.DeckGames = deckGamesCount;
            }
            deck.GetThisDeck().Winrate = gamesWon / (gamesCount / 100.0);
            deck.GetThisDeck().simulationResult.Winrate = gamesWon / (gamesCount / 100.0);
        }
        #endregion
    }
}
