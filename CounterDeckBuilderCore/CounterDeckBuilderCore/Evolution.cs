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
    /// <summary>
    /// Interface for any evolvable deck.
    /// </summary>
    public interface IEvolvable : IComparable<IEvolvable>
    {
        /// <summary>
        /// Returns deck wrapped in interface.
        /// </summary>
        /// <returns></returns>
        Deck GetThisDeck();

        /// <summary>
        /// List of all mutations (card for card).
        /// </summary>
        /// <returns></returns>
        List<Tuple<string, string>> Mutations();
        
        /// <summary>
        /// Crossover this deck with another.
        /// </summary>
        /// <param name="deck"> Another deck to crossover. </param>
        /// <returns></returns>
        IEvolvable Crossover(IEvolvable deck);

        /// <summary>
        /// Mutates given number of times.
        /// </summary>
        /// <param name="num"> Number of times to mutate. </param>
        /// <returns></returns>
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
        public Deck(List<int> referenceCurve, CardClass heroClass)
        {
            this.heroClass = heroClass;
            foreach (int cost in referenceCurve)
            {
                Card c = GetRandomCardOfManaCost(heroClass, cost);
                while (!CardAvailable(c) || (c.Class != CardClass.NEUTRAL && c.Class != heroClass))
                {
                    c = GetRandomCardOfManaCost(heroClass, cost);
                }
                deck.Add(c);
            }   
        }    

        public Deck()
        {

        }

        /// <summary>
        /// Finds "hands" for a deck, given the hand's cards mana costs.
        /// </summary>
        /// <param name="costs"> Expected format: array of 3-integer arrays. Mana costs of cards in hands. </param>
        public void FindHands(int[][] costs)
        {
            this.myHands = new List<Draft>();
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

        /// <summary>
        /// Checks whether the card is available for the deck. (regarding Hearthstone rules)
        /// </summary>
        /// <param name="c">Card to be checked. </param>
        /// <returns> Availability of a card for deck. </returns>
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

        /// <summary>
        /// Creates a deep copy of this deck.
        /// </summary>
        /// <returns> Deep copy of this deck. </returns>
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
    /// Deck that only knows mutations.
    /// </summary>
    public class MutatingDeck : IEvolvable
    {
        public Deck thisDeck;
        public List<Tuple<string, string>> mutations;

        /// <summary>
        /// Compares this deck to another lexicographically (thisDeck's winrate and variance)
        /// </summary>
        /// <param name="other"> Deck to compare to.</param>
        /// <returns></returns>
        public int CompareTo(IEvolvable other)
        {
            return (thisDeck.Winrate == other.GetThisDeck().Winrate) ? -(thisDeck.Variance.CompareTo(other.GetThisDeck().Variance)) : (thisDeck.Winrate.CompareTo(other.GetThisDeck().Winrate));
        }

        /// <summary>
        /// This deck cannot crossover with other...
        /// </summary>
        /// <param name="partner"> Other deck to crossover.</param>
        /// <returns></returns>
        public IEvolvable Crossover(IEvolvable partner)
        {
            return this;
        }

        public Deck GetThisDeck()
        {
            return thisDeck;
        }

        /// <summary>
        /// Changes given number of cards in deck.
        /// </summary>
        /// <param name="mutations"> Number of cards to change.</param>
        /// <returns></returns>
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
            return new MutatingDeck()
            {
                thisDeck = toMutate
            };
        }

        public List<Tuple<string, string>> Mutations()
        {
            return mutations;
        }
    }

    /// <summary>
    /// Configuration of the hill-climbing algo. 
    /// </summary>
    public class HillClimbingConfiguration
    {
        public Heuristic heuristic1;
        public Heuristic heuristic2;

        public Player player1;
        public Player player2;

        public int numGames;
        public List<Deck> refdecks;
        public List<IEvolvable> population;
    }

    /// <summary>
    /// Few auxiliary methods for Hill-climbing.
    /// </summary>
    public class HillClimbingTester
    {
        /// <summary>
        /// Tests input deck (expects the population of config to be one) given number of tries.
        /// </summary>
        /// <param name="config"> Configuration of hill-climbing algorithm. </param>
        /// <param name="numTries"> Number of times to test the input deck. </param>
        /// <param name="hands"> Should it use random hands for ref decks? </param>
        /// <param name="numThreads"> Number of threads to use. </param>
        /// <returns></returns>
        public static List<IEvolvable> TestDeckWinrate(HillClimbingConfiguration config, int numTries, bool hands, int numThreads = 0)
        {
            List<IEvolvable> pop = new List<IEvolvable>();
            for (int i = 0; i < numTries; i++)
            {
                pop.Add(new MutatingDeck() { thisDeck = config.population[0].GetThisDeck().DeepCopy() });                
            }
            config.population = pop;
            HillClimbing evol = new HillClimbing(config);
            if (hands) evol.hands = false;
            return evol.EvaluateGeneration(pop, numThreads);
        }
      
        /// <summary>
        /// Generates all mutations of a deck (that differ in exactly one card).
        /// </summary>
        /// <param name="parent"> Parent to deck to generate mutations. </param>
        /// <param name="costBound"> Should it only use mutations that have similar mana costs? </param>
        /// <returns></returns>
        public static List<IEvolvable> GetAllMutations(IEvolvable parent, bool costBound)
        {
            List<IEvolvable> population = new List<IEvolvable>();

            CardClass heroClass = parent.GetThisDeck().heroClass;
            List<Card> class_ok = new List<Card>();
            class_ok.AddRange(Cards.AllStandard.ToList().Where(card => (card.Class == CardClass.NEUTRAL) || (card.Class == heroClass)));
            //need to initialize this with some real card, 10 mana ultrasaur cannot be the first card of the deck hopefully
            Card lastcard = Cards.FromName("Ultrasaur");

            parent.GetThisDeck().deck.Sort((c1, c2) => c1.Name.CompareTo(c2.Name));

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
                    if (mutation.Name == "The Darkness" || mutation.Name == "Devilsaur Egg")
                    {
                        continue;
                    }
                    MutatingDeck deck = new MutatingDeck()
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

    /// <summary>
    /// Represents hill-climbing algorithm.
    /// </summary>
    public class HillClimbing
    {
        private List<IEvolvable> currentGeneration;
        private List<Deck> testingDecks;
        private int numGames;
        private static Random r = new Random();
        public bool hands = true;

        private List<List<double>> results = new List<List<double>>();
        private Heuristic heuristic1;
        private Heuristic heuristic2;

        private Player player1;
        private Player player2;

        /// <summary>
        /// Initializas hill-climbing.
        /// </summary>
        /// <param name="config"> Initial data/configuration of hill-climbing. </param>
        public HillClimbing(HillClimbingConfiguration config)
        {
            testingDecks = config.refdecks;
            numGames = config.numGames;
            currentGeneration = config.population;

            heuristic1 = config.heuristic1;
            heuristic2 = config.heuristic2;

            player1 = config.player1;
            player2 = config.player2;
        }

        /// <summary>
        /// Initializes hill-climbing. 
        /// </summary>
        /// <param name="population"> Initial population. </param>
        /// <param name="referenceDecks"> Reference decks. </param>
        /// <param name="numberOfGames"> Number of games to simulate for every ref deck. </param>
        public HillClimbing(List<IEvolvable> population, List<Deck> referenceDecks, int numberOfGames)
        {
            currentGeneration = population;
            testingDecks = referenceDecks;
            numGames = numberOfGames;
        }

        /// <summary>
        /// Executes the hill climbing algorithm with given number of steps. Expects the number of decks in initial population is 1.
        /// </summary>
        /// <param name="iterations"> Number of iterations for hill-climbing. </param>
        /// <param name="threads">Number of threads to use. </param>
        /// <returns></returns>
        public IEvolvable HillClimb(int iterations, int threads = 0)
        {
            IEvolvable parent = currentGeneration[0];
            for (int i = 0; i < iterations; i++)
            {
                var population = HillClimbingTester.GetAllMutations(parent, true);
                Console.WriteLine("Generation number " + (i+1).ToString() + " in progress.");
                Console.WriteLine("Testing " + population.Count() + " decks...");
                //Console.WriteLine("-------------------------------------------");
                currentGeneration = EvaluateGeneration(population, threads);

                currentGeneration.Sort();

                parent = HillClimbStep();
            }
            return parent;
        }

        /// <summary>
        /// Public method to access hill-climb step.
        /// </summary>
        /// <returns> Best deck of last generation. </returns>
        public IEvolvable HillClimbLastGeneration()
        {
            currentGeneration.Sort();

            return HillClimbStep();
        }

        /// <summary>
        /// Tests part of current generation (chosen by parts and index). Used on servers, because parallelization.
        /// </summary>
        /// <param name="parts"> Number of parts to split current generation. </param>
        /// <param name="index"> Zero-based index of part to be evaluated. </param>
        /// <param name="threads"> Number of threads to use. </param>
        /// <returns></returns>
        public List<IEvolvable> TestGenerationPartForBash(int parts, int index, int threads = 0)
        {
            IEvolvable parent = currentGeneration[0];
            var population = HillClimbingTester.GetAllMutations(parent, true);

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
       
        #region Private stuff
        /// <summary>
        /// Returns the best deck of current generation. 
        /// </summary>
        /// <returns> Best deck of current generation. </returns>
        private IEvolvable HillClimbStep()
        { 
            return currentGeneration[currentGeneration.Count - 1];
        }

        /// <summary>
        /// Handles threading - eg. split population into parts, test them in separate threads.
        /// </summary>
        /// <param name="population"> Population to be tested. </param>
        /// <param name="threads"> Number of threads to evaluate in. </param>
        /// <returns> Evaluated generation. </returns>
        public List<IEvolvable> EvaluateGeneration(List<IEvolvable> population, int threads)
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
                }

                Task.WaitAll(tasks);

                for (int i = 0; i < tasks.Count(); i++)
                {
                    retval.AddRange(tasks[i].Result);
                }
                
                s.Stop();

                Console.WriteLine("{0} decks were evaluated in " + (s.ElapsedMilliseconds / 1000).ToString() + " seconds.", population.Count());
            }

            return retval;
        }
        
        /// <summary>
        /// Tests all decks in given population.
        /// </summary>
        /// <param name="population"> Population to be tested. </param>
        /// <returns>Evaluated population. </returns>
        private List<IEvolvable> TestPopulation(List<IEvolvable> population)
        {
            for (int i = 0; i < population.Count(); i++)
            {
                Console.WriteLine("Testing deck number {0}.", i);
                if (hands) TestDeck(population[i]);
                else TestDeckNoHands(population[i]);
            }
            return population;
        }

        /// <summary>
        /// Tests deck with random hands for tested deck, and given hands for ref decks.
        /// </summary>
        /// <param name="deck"> Deck to be tested. </param>
        private void TestDeck(IEvolvable deck)
        {
            Simulator simulator = new Simulator();
            int gamesCount = 0;
            int gamesWon = 0;
            IPlayer AI1 = player1.GetPlayer(heuristic1);
            IPlayer AI2 = player2.GetPlayer(heuristic2);
            
            foreach (Deck d in testingDecks)
            {
                int deckGamesWon = 0;
                int deckGamesCount = 0;

                foreach (Draft draft in d.myHands)
                {
                    deck.GetThisDeck().FindHands(new int[][] { new int[] { r.Next(5), r.Next(5), r.Next(5) } });
                    foreach (Draft draft2 in deck.GetThisDeck().myHands)
                    {
                        deckGamesCount += numGames;
                        var result = simulator.SimulateGames(AI1, AI2, numGames, deck.GetThisDeck(), d, draft2.cards.ToList(), draft.cards.ToList());
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
            deck.GetThisDeck().Variance = deck.GetThisDeck().Variance();
        }

        /// <summary>
        /// Tests deck with random hands for both ref decks and tested deck.
        /// </summary>
        /// <param name="deck"> Deck to be tested. </param>
        private void TestDeckNoHands(IEvolvable deck)
        {
            Simulator simulator = new Simulator();
            int gamesCount = 0;
            int gamesWon = 0;
            IPlayer AI1 = player1.GetPlayer(heuristic1);
            IPlayer AI2 = player2.GetPlayer(heuristic2);

            try
            {
                foreach (Deck d in testingDecks)
                {
                    int deckGamesWon = 0;
                    int deckGamesCount = 0;

                    for (int i = 0; i < numGames; i++)
                    {
                        deck.GetThisDeck().FindHands(new int[][] { new int[] { r.Next(5), r.Next(5), r.Next(5) } });
                        d.FindHands(new int[][] { new int[] { r.Next(5), r.Next(5), r.Next(5) } });
                        deckGamesCount += 1;
                        var result = simulator.SimulateGames(AI1, AI2, 1, deck.GetThisDeck(), d, deck.GetThisDeck().myHands[0].cards.ToList(), d.myHands[0].cards.ToList());
                        deckGamesWon += result.Item1;
                    }
                    deck.GetThisDeck().simulationResult.Results.Add(deckGamesWon);
                    gamesWon += deckGamesWon;
                    gamesCount += deckGamesCount;
                    deck.GetThisDeck().simulationResult.DeckGames = deckGamesCount;
                }
                deck.GetThisDeck().Winrate = gamesWon / (gamesCount / 100.0);
                deck.GetThisDeck().simulationResult.Winrate = gamesWon / (gamesCount / 100.0);
                deck.GetThisDeck().Variance = deck.GetThisDeck().Variance();
            }
            catch (Exception e)
            {
                deck.GetThisDeck().Winrate = 0;
                deck.GetThisDeck().simulationResult.Winrate = 0;
                deck.GetThisDeck().Variance = 0;
            }
        }
        #endregion
    }
}
