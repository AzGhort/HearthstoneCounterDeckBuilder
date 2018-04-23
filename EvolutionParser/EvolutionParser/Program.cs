using CounterDeckBuilder;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolutionParser
{
    class DeckParser
    {
        private StreamReader _reader;

        public List<Deck> GetAllDecks()
        {
            List<Deck> decks = new List<Deck>();
            try
            {
                string filename = "." + Path.DirectorySeparatorChar + "decks" + Path.DirectorySeparatorChar + "decks.txt";
                _reader = new StreamReader(filename);
            }
            catch
            {
                Console.WriteLine("No decks were found.");
                return decks;
            }
            string line = "";
            //first line is empty...
            line = _reader.ReadLine();
            Random randomCards = new Random();
            CardClass cl = CardClass.INVALID;
            while (_reader.Peek() >= 0)
            {
                line = _reader.ReadLine();
                //header line of the deck found
                List<Card> deck = new List<Card>();
                string deckname = line;
                Deck d = new Deck();
                d.myCurve = new List<int>();
                while ((line = _reader.ReadLine()) != "")
                {
                    if (line is null) break;
                    string[] tokens = line.Split('_');
                    Card card = Cards.FromName(tokens[1]);
                    if (card == null)
                    {
                        //get random card from standard neutral
                        var allNeutralCards = Cards.FormatTypeClassCards(FormatType.FT_STANDARD)[CardClass.NEUTRAL];
                        card = allNeutralCards.ToList()[randomCards.Next(allNeutralCards.Count())];
                        while (deck.Contains(card))
                        {
                            card = allNeutralCards.ToList()[randomCards.Next(allNeutralCards.Count())];
                        }
                    }
                    deck.Add(card);
                    if (tokens[0] == "x2" || tokens[0] == "2x")
                    {
                        d.myCurve.Add(card.Cost);
                        deck.Add(card);
                    }
                    if (card.Class != CardClass.NEUTRAL)
                    {
                        cl = card.Class;
                    }
                    d.myCurve.Add(card.Cost);
                }
                d.Deckname = deckname;
                d.deck = deck;
                d.heroClass = cl;
                decks.Add(d);
            }
            return decks;
        }
    }

    class Program
    {
        public static List<IEvolvable> GetGeneration(string subdirectory)
        {
            string filename = "." + Path.DirectorySeparatorChar + subdirectory + Path.DirectorySeparatorChar + "out.txt";
            StreamReader _reader = new StreamReader(filename);
            List<IEvolvable> decks = new List<IEvolvable>();
            using (_reader)
            {
                string line = "";
                while (_reader.Peek() >= 0)
                {
                    line = _reader.ReadLine();
                    string[] tokens = line.Split('-');
                    tokens[1] = tokens[1].Replace('.', ',');
                    tokens[2] = tokens[2].Replace('.', ',');

                    double variance = Double.Parse(tokens[2]);
                    double winrate = Double.Parse(tokens[1]);
                    SimulationResult sim = new SimulationResult();

                    Deck d = new Deck();
                    d.myCurve = new List<int>();
                    CardClass cl = CardClass.INVALID;
                    List<Card> deck = new List<Card>();

                    while ((line = _reader.ReadLine()) != "---------------------------------------")
                    {
                        Card card = Cards.FromName(line);
                        deck.Add(card);
                        if (card.Class != CardClass.NEUTRAL)
                        {
                            cl = card.Class;
                        }
                        d.myCurve.Add(card.Cost);
                    }
                    d.Deckname = "";
                    d.deck = deck;
                    d.heroClass = cl;
                    d.Winrate = winrate;
                    d.Variance = variance;
                    sim.Winrate = winrate;
                    d.simulationResult = sim;
                    decks.Add(new DumbEvolvingDeck() { thisDeck = d });
                }
            }
            return decks;
        }

        public static IEvolvable GetBestDeckOfGeneration(string subdirectory)
        {
            Deck d = new Deck();
            string filename = "." + Path.DirectorySeparatorChar + subdirectory + Path.DirectorySeparatorChar + "newdeck.txt";
            using (var _reader = new StreamReader(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                string line = "";
                CardClass cl = CardClass.INVALID;
                List<Card> deck = new List<Card>();
                d.myCurve = new List<int>();
                while ((line = _reader.ReadLine()) != "")
                {
                    Card card = Cards.FromName(line);
                    if (card == null) break;
                    //Console.WriteLine(line);
                    deck.Add(card);
                    if (card.Class != CardClass.NEUTRAL)
                    {
                        cl = card.Class;
                    }
                }
                d.deck = deck;
                d.heroClass = cl;
            }
            return new DumbEvolvingDeck() { thisDeck = d };
        }

        static void Main(string[] args)
        {
            var directories = Directory.GetDirectories(".");
            string[] allgen_fitness = new string[directories.Count() - 2];
            int counter = 0;

            DeckParser parser = new DeckParser();
            var decks = parser.GetAllDecks();
            List<IEvolvable> initialwinrates = new List<IEvolvable>();

            foreach (Deck d in decks)
            {
                DraftSeeker seeker = new DraftSeeker(d.heroClass, d.deck, decks);
                Console.WriteLine("Looking for drafts in {0}", d.Deckname);

                List<Draft> drafts = Draft.XmlDeserializeDrafts(d.Deckname);

                d.myHands = drafts;
            }
            
            foreach (var dir in directories)
            {
                if (!Char.IsDigit(dir, 2)) continue; // every directory starts with .\\
                
                var tokens = dir.Split('.');
                string rem = tokens[1].Replace("\\", "");
                int num = int.Parse(rem);
                
                var gen = GetGeneration(dir);
                gen.Sort();

                double win1 = gen[0].GetThisDeck().Winrate;
                double var1 = gen[0].GetThisDeck().Variance;
                double win2 = gen[gen.Count / 2].GetThisDeck().Winrate;
                double var2 = gen[gen.Count / 2].GetThisDeck().Variance;
                double win3 = gen[gen.Count - 1].GetThisDeck().Winrate;
                double var3 = gen[gen.Count - 1].GetThisDeck().Variance;
                
                double fitness1 = win1;
                double fitness2 = win2;
                double fitness3 = win3;
                
                if (num == 1)
                {
                   /*
                    var best = GetBestDeckOfGeneration(dir);

                    EvolutionConfiguration config = new EvolutionConfiguration();
                    config.player1 = Player.RANDOM_DUMB_GREEDY;
                    config.player2 = Player.RANDOM_DUMB_GREEDY;
                    config.heuristic1 = Heuristic.BASIC;
                    config.heuristic2 = Heuristic.BASIC;
                    config.numGames = 5;
                    config.refdecks = decks;
                    config.population = new List<IEvolvable>() { best };

                    initialwinrates = EvolutionTester.TestDeckWinrate(config, 1000, false, 4);
                    
                    for (int i = 0; i < 1000; i++)
                    {
                        while (initialwinrates[i].GetThisDeck().Winrate == 0)
                        {
                            var r = EvolutionTester.TestDeckWinrate(config, 1, true);
                            initialwinrates[i] = r[0];
                        }
                    }


                    using (StreamWriter sw = new StreamWriter("outcurve8.txt"))
                    {
                        foreach (var deck in initialwinrates)
                        {
                            sw.Write(deck.GetThisDeck().Winrate + " ");
                        }
                    }*/
                }

                string res = fitness1 + "-" + fitness2 + "-" + fitness3; 
                allgen_fitness[num - 1] = res;
                Console.WriteLine("Generation " + num + " parsed.");
                counter++;
            }
            
            /*
            using (StreamReader sr = new StreamReader("outcurve7.txt"))
            {
                string s = sr.ReadLine();
                var tokens = s.Split(' ');
                List<double> d = new List<double>();
                foreach (string s1 in tokens)
                {
                    if (s1 == "") continue;
                    d.Add(Double.Parse(s1));
                }
                double res = d.GetVariance();
            }
            using (StreamReader sr = new StreamReader("outcurve8.txt"))
            {
                string s = sr.ReadLine();
                var tokens = s.Split(' ');
                List<double> d = new List<double>();
                foreach (string s1 in tokens)
                {
                    if (s1 == "") continue;
                    d.Add(Double.Parse(s1));
                }
                double res = d.GetVariance();
            }*/
            using (StreamWriter sw = new StreamWriter("outcurve.txt"))
            {
                foreach (string fit in allgen_fitness)
                {
                    sw.WriteLine(fit);
                }
            }
        }
    }
}
