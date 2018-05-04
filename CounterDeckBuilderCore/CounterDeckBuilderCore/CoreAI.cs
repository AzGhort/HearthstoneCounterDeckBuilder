using System;
using System.Linq;
using SabberStoneCore.Model;
using SabberStoneCore.Tasks.PlayerTasks;
using SabberStoneCore.Tasks;
using System.Collections.Generic;

namespace CounterDeckBuilder
{
    /// <summary>
    /// General interface for any agent.
    /// </summary>
    public interface IPlayer
    {
        /// <summary>
        /// Current game that agent plays.
        /// </summary>
        Game CurrentGame { get; set; }
        /// <summary>
        /// Does the agent want to do any more actions?
        /// </summary>
        /// <returns> Whether the agent want to do more actions. </returns>
        bool HasNextMove();
        /// <summary>
        /// Get next action of agent.
        /// </summary>
        /// <returns> Next action of the agent. </returns>
        PlayerTask GetNextMove();
        /// <summary>
        /// Sets the current game for agent to play.
        /// </summary>
        /// <param name="game"> Game to be set. </param>
        void SetGame(Game game);
    }

    /// <summary>
    /// Enum containing all implemented agents.
    /// </summary>
    public enum Player
    {
        RANDOM_NON_GREEDY, HEURISTIC_STEP, HEURISTIC_TURN, RANDOM_DUMB_GREEDY
    }

    /// <summary>
    /// Extension class containg auxiliary methods of agents.
    /// </summary>
    public static class PlayerExtension
    {
        /// <summary>
        /// Returns actual IPlayer class, given the input Player enum.
        /// </summary>
        /// <param name="pl"> Player from enum. </param>
        /// <param name="heuristic"> Heuristic that the player can use. </param>
        /// <returns> Actual IPlayer class. </returns>
        public static IPlayer GetPlayer(this Player pl, Heuristic heuristic)
        {
            switch (pl)
            {
                case Player.HEURISTIC_STEP:
                    var heur = heuristic.GetHeuristic();
                    return new HeuristicStepGreedyPlayer(heur);
                case Player.HEURISTIC_TURN:
                    var heur2 = heuristic.GetHeuristic();
                    return new HeuristicTurnGreedyPlayer(heur2);
                case Player.RANDOM_DUMB_GREEDY:
                    return new RandomDumbGreedyPlayer();
                case Player.RANDOM_NON_GREEDY:
                    return new RandomNonGreedyPlayer();
                default:
                    return new RandomDumbGreedyPlayer();                   
            }
        }
    }

    /// <summary>
    /// Random non-greedy player, may end the turn earlier than necessary... 
    /// </summary>
    public class RandomNonGreedyPlayer : IPlayer
    {
        Random Brain = new Random();
        public Game CurrentGame { get; set; }

        public void SetGame(Game game)
        {
            CurrentGame = game;
        }

        /// <summary>
        /// Get random action.
        /// </summary>
        /// <returns>Random action of player. </returns>
        public PlayerTask GetNextMove()
        {
            if (!HasNextMove()) return null;

            var options = CurrentGame.CurrentPlayer.Options();
            var option = options[Brain.Next(options.Count())];
            return option;
        }

        public bool HasNextMove()
        {
            return (CurrentGame.CurrentPlayer.Options().Count() > 0);
        }

        public override string ToString()
        {
            return "Random Non-greedy AI";
        }
    }

    /// <summary>
    /// Heuristic greedy player.
    /// Looks for the "best option" on one level of the decision tree.
    /// </summary>
    public class HeuristicStepGreedyPlayer : IPlayer
    {
        public Game CurrentGame { get; set; }
        private IGameStateHeuristic heuristic;

        public HeuristicStepGreedyPlayer(IGameStateHeuristic gameStateHeuristic)
        {
            this.heuristic = gameStateHeuristic;
        }

        public void SetGame(Game game)
        {
            CurrentGame = game;
        }

        public PlayerTask GetNextMove()
        {
            try
            {
                if (!HasNextMove()) return null;

                var options = CurrentGame.CurrentPlayer.Options();

                //only end turn task
                if (options.Count() == 1)
                {
                    return options[0];
                }

                int bestOptionIndex = 0;

                double bestScore = heuristic.GetScore(CurrentGame); 
                int index = 0;

                //just for debugging...
                //var a = CurrentGame.CurrentPlayer.HandZone;
                //Console.WriteLine(String.Join("-", a));
                //Console.WriteLine(CurrentGame.CurrentPlayer.RemainingMana);

                foreach (PlayerTask task in options)
                {
                    if (task is EndTurnTask)
                    {
                        index++;
                        continue;
                    }

                    Game clone = CurrentGame.Clone();
                    clone.Process(task);
                    double score = heuristic.GetScore(clone);                   

                    if (score >= bestScore)
                    {
                        bestOptionIndex = index;
                        bestScore = score;
                    }
                    index++;
                }

                return options[bestOptionIndex];
            }
           
            //return end turn task..
            catch (Exception e)
            {
                return CurrentGame.CurrentPlayer.Options()[0];
            }
        }

        public bool HasNextMove()
        {
            return (CurrentGame.CurrentPlayer.Options().Count() > 0);
        }

        public override string ToString()
        {
            return "Heuristic Step Greedy AI";
        }
    }

    /// <summary>
    /// Heuristic greedy player. Tries to look for best sequence of actions in one turn (can fail sometimes, SabberStone issues..)
    /// </summary>
    public class HeuristicTurnGreedyPlayer : IPlayer
    {
        public Game CurrentGame { get; set; }
        private IGameStateHeuristic heuristic;

        private List<int> tasksIndices = new List<int>();

        public HeuristicTurnGreedyPlayer(IGameStateHeuristic gameStateHeuristic)
        {
            heuristic = gameStateHeuristic;
        }

        public void SetGame(Game game)
        {
            CurrentGame = game;
        }

        /// <summary>
        /// Returns next action from the "best sequence", or finds the sequence if it was not found yet.
        /// </summary>
        /// <returns></returns>
        public PlayerTask GetNextMove()
        {
            try
            {
                if (!HasNextMove()) return null;

                /*
                //just for debugging...
                var a = CurrentGame.CurrentPlayer.HandZone;
                Console.WriteLine(String.Join("-", a));
                Console.WriteLine(CurrentGame.CurrentPlayer.RemainingMana);*/

                var options = CurrentGame.CurrentPlayer.Options();
                //only end turn task
                if (options.Count() == 1)
                {
                    tasksIndices = new List<int>();
                    return options[0];
                }

                //we have already found our best turn
                if (tasksIndices.Count > 0)
                {
                    var ind = tasksIndices[tasksIndices.Count - 1];
                    tasksIndices.RemoveAt(tasksIndices.Count - 1);
                    return options[ind];
                }

                double score = 0;
                tasksIndices = GetTaskIndices(CurrentGame, ref score);

                var index = tasksIndices[tasksIndices.Count - 1];
                tasksIndices.RemoveAt(tasksIndices.Count - 1);
                return options[index];
            }

            //return end turn task..
            catch (Exception e)
            {
                return CurrentGame.CurrentPlayer.Options()[0];
            }
        }

        /// <summary>
        /// Recursive method to find a best turn.
        /// </summary>
        private List<int> GetTaskIndices(Game game, ref double score)
        {
            var options = game.CurrentPlayer.Options();

            if (options.Count == 1)
            {
                score = heuristic.GetScore(game);
                return new List<int>(new int[] { 0 });
            }

            int index = 0;
            double bestScore = double.MinValue;
            List<int> retval = null;
            string res = "";

            for (int i = 1; i < options.Count; i++)
            {
                Game clone = game.Clone();
                clone.Process(options[i]);
                
                double newscore = 0;
                var que = GetTaskIndices(clone, ref newscore);
                

                if (newscore >= bestScore)
                {
                    index = i;
                    bestScore = newscore;
                    retval = que;
                }
            }
            
            retval.Add(index);
            return retval;
        }

        public bool HasNextMove()
        {
            return (CurrentGame.CurrentPlayer.Options().Count() > 0);
        }

        public override string ToString()
        {
            return "Heuristic Turn Greedy AI";
        }
    }

    /// <summary>
    /// Random greedy player, always takes as much actions as possible.
    /// </summary>
    public class RandomDumbGreedyPlayer : IPlayer
    {
        Random Brain = new Random();

        public Game CurrentGame { get; set; }

        public void SetGame(Game game)
        {
            CurrentGame = game;
        }

        /// <summary>
        /// Get random action that is not "EndTurn".
        /// </summary>
        /// <returns> Random action of agent. </returns>
        public PlayerTask GetNextMove()
        {
            if (!HasNextMove()) return null;

            var options = CurrentGame.CurrentPlayer.Options();
            var option = options[Brain.Next(options.Count())];

            while (options.Count() > 1 && option is EndTurnTask)
            {
                option = options[Brain.Next(options.Count())];
            }

            return option;
        }

        public bool HasNextMove()
        {
            return (CurrentGame.CurrentPlayer.Options().Count() > 0);
        }

        public override string ToString()
        {
            return "Random Dumb Greedy AI";
        }

    }
}