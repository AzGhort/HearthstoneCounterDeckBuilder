using CounterDeckBuilder;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HillClimbStep
{
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
                    if (tokens[0] == "x2" || tokens[0] == "2x") deck.Add(card);
                    deckFound = true;
                }
            }
            _reader.DiscardBufferedData();
            _reader.BaseStream.Seek(0, SeekOrigin.Begin);
            Deck.deck = deck;
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
    }

    class Program
    {
        public static List<IEvolvable> GetLastGeneration()
        {
            StreamReader _reader = new StreamReader("out.txt");
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
                    decks.Add(new DumbEvolvingDeck() { thisDeck = d});
                }
            }
            return decks;
        }

        static void Main(string[] args)
        {
            List<IEvolvable> lastgen = GetLastGeneration();
            lastgen.Sort();      

            Evolution evolve = new Evolution(lastgen, null, 0);
            var d1 = evolve.HillClimbLastGeneration(20);
            
            using (StreamWriter file = new StreamWriter("newdeck.txt", true))
            {
                foreach (var card in d1.GetThisDeck().deck)
                {
                    file.WriteLine(card.Name);
                }
            }
        }
    }
}
