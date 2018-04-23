using SabberStoneCore.Model;
using SabberStoneCore.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace CounterDeckBuilder
{
    class CommandsParser
    {
        public EvolutionConfiguration GetCommands(string filename)
        {
            EvolutionConfiguration config = new EvolutionConfiguration();
            try
            {
                using (StreamReader reader = new StreamReader(filename))
                {
                    string line = "";
                    while (reader.Peek() >= 0)
                    {
                        line = reader.ReadLine();
                        string[] tokens = line.Split(new char[] { '-' });

                        //numgames
                        config.numGames = int.Parse(tokens[0]);
                        //heuristic 1
                        config.heuristic1 = GetHeuristicFromName(tokens[1]);
                        //heuristic 2
                        config.heuristic2 = GetHeuristicFromName(tokens[2]);
                        //player 1
                        config.player1 = GetPlayerFromName(tokens[3]);
                        //player 2
                        config.player2 = GetPlayerFromName(tokens[4]);
                    }
                }
                return config;
            }
            catch
            {
                return config;
            }
        }

        private Heuristic GetHeuristicFromName(string name)
        {
            switch (name)
            {
                case "DEFAULT":
                    return Heuristic.DEFAULT;
                case "BASIC":
                    return Heuristic.BASIC;
                case "HEARTHAGENT":
                    return Heuristic.HEARTH_AGENT;
                case "FACEHUNTER":
                    return Heuristic.FACE_HUNTER;
                case "AGGROPALLY":
                    return Heuristic.AGGRO_PALLY;
                case "SECRETMAGE":
                    return Heuristic.SECRET_MAGE;
                case "CONTROLPRIEST":
                    return Heuristic.CONTROL_PRIEST;
                default:
                    return Heuristic.DEFAULT;
            }
        }

        private Player GetPlayerFromName(string name)
        {
            switch (name)
            {
                case "RANDOMDUMB":
                    return Player.RANDOM_DUMB_GREEDY;
                case "RANDOM":
                    return Player.RANDOM_NON_GREEDY;
                case "HEURISTICSTEP":
                    return Player.HEURISTIC_STEP;
                case "HEURISTICTURN":
                    return Player.HEURISTIC_TURN;
                default:
                    return Player.RANDOM_DUMB_GREEDY;
            }
        }
    }

    class DeckParser
    {
        private StreamReader _reader;

        public Deck GetRandomDeck(CardClass Class)
        {
            Random random = new Random();
            Deck d = new Deck();
            d.heroClass = Class;
            List<Card> deck = new List<Card>();
            var totCards = Cards.AllStandard.ToList();

            while (deck.Count() < 30)
            {
                Card card = totCards[random.Next(totCards.Count())];
                if (card.Class != Class && card.Class != CardClass.NEUTRAL) continue;
                int countInDeck = 0;
                foreach (Card c in deck)
                {
                    if (c == card) countInDeck++;
                }
                if (countInDeck > 1) continue;
                deck.Add(card);
            }
            d.deck = deck;
            return d;
        }

        public Deck GetDeckByName(string deckfile, string deckname, CardClass cl)
        {
            Deck Deck = new Deck();
            Deck.heroClass = cl;
            Deck.Deckname = deckname;
            var deck = new List<Card>();
            try
            {
                string filename = "." + Path.DirectorySeparatorChar + "decks" + Path.DirectorySeparatorChar + deckfile;
                _reader = new StreamReader(filename);
            }
            catch
            {
                return (GetRandomDeck(cl));
            }
            string line = "";
            bool deckFound = false;
            Random randomCards = new Random();
            List<int> curve = new List<int>();
            while (_reader.Peek() >= 0)
            {
                line = _reader.ReadLine();
                //header line of the deck found
                if (line == deckname)
                {
                    deckFound = true;
                    continue;
                }
                if (deckFound)
                {
                    //end of deck
                    if (line == "") break;

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
                    curve.Add(card.Cost);
                    if (tokens[0] == "x2" || tokens[0] == "2x")
                    {
                        deck.Add(card);
                        curve.Add(card.Cost);
                    }
                    deckFound = true;
                }
            }
            _reader.DiscardBufferedData();
            _reader.BaseStream.Seek(0, SeekOrigin.Begin);
            Deck.deck = deck;
            Deck.myCurve = curve;
            if (!deckFound) Deck = GetRandomDeck(cl);
            return Deck;
        }

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

        public Deck GetBestDeckOfLastGeneration()
        {
            Deck d = new Deck();
            using (var _reader = new StreamReader(File.Open("newdeck.txt", FileMode.Open, FileAccess.Read, FileShare.Read)))
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
            return d;
        }
    }

    class Program
    {
        private static Random random = new Random();

        static void Main(string[] args)
        {
            DeckParser parser = new DeckParser();
            CommandsParser comm = new CommandsParser();
            var decks = parser.GetAllDecks();

            int threads = 1;
            int parts = 1;
            int index = 0;

            if (args.Length > 0)
            {
                int.TryParse(args[0], out parts);
                int.TryParse(args[1], out index);
            }
            
            Deck Deck1 = parser.GetDeckByName("basicdecks.txt","BasicPriest", CardClass.PRIEST);
            EvolutionConfiguration config = comm.GetCommands("commands.txt");

            Stopwatch s = new Stopwatch();
            s.Start();
            foreach (Deck d in decks)
            {
                DraftSeeker seeker = new DraftSeeker(d.heroClass, d.deck, decks);
                Console.WriteLine("Looking for drafts in {0}", d.Deckname);

                //var drafts = seeker.GetAllDrafts(4);
                //List<Draft> res = new List<Draft>();
                //res.Add(drafts[0]);
                //res.Add(drafts[drafts.Count / 2]);
                //res.Add(drafts[drafts.Count - 1]);

                //Draft.XmlSerializeDrafts(res, d.Deckname);

                List<Draft> drafts = Draft.XmlDeserializeDrafts(d.Deckname);
                
                d.myHands = drafts;              
            }
            s.Stop();

            Console.WriteLine("Drafts were found, total time elapsed (in seconds): " + (s.ElapsedMilliseconds / 1000).ToString());
            Console.WriteLine("-------------------------------------------");
            
            if (args.Length > 2)
            {
                Console.WriteLine("Not the first generation, getting best deck of last generation...");

                Deck1 = parser.GetBestDeckOfLastGeneration();
            }

            Console.WriteLine("Hill climbing with deck: " + Deck1.Deckname);
            DumbEvolvingDeck dumb = new DumbEvolvingDeck() { thisDeck = Deck1 };
            config.population = new List<IEvolvable>(new IEvolvable[] { dumb });
            config.refdecks = decks;

            Evolution evolution = new Evolution(config);
            var result2 = evolution.TestGenerationPartForBash(parts, index, threads);
            
            using (StreamWriter file = new StreamWriter("deck" + index.ToString() + ".txt", true))
            {
                foreach (var ievolv in result2)
                {
                    double variance = ievolv.GetThisDeck().Variance();
                    file.WriteLine("Winrate-" + ievolv.GetThisDeck().Winrate + "-" + variance.ToString());
                    foreach (var card in ievolv.GetThisDeck().deck)
                    {
                        file.WriteLine(card.Name);
                    }
                    file.WriteLine("---------------------------------------");
                }
            }

        }
    }
}