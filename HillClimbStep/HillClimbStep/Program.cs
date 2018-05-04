using CounterDeckBuilder;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HillClimbStep
{  
    class Program
    {
        /// <summary>
        /// Get last generation from ./out.txt
        /// </summary>
        /// <returns> List of decks from last generation. </returns>
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
                    decks.Add(new MutatingDeck() { thisDeck = d});
                }
            }
            return decks;
        }

        static void Main(string[] args)
        {
            List<IEvolvable> lastgen = GetLastGeneration();
            lastgen.Sort();      

            HillClimbing evolve = new HillClimbing(lastgen, null, 0);
            var d1 = evolve.HillClimbLastGeneration();
            
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
