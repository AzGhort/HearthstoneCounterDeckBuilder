using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CounterDeckBuilder;
using SabberStoneCore.Tasks;

namespace CounterDeckBuilder
{
    public interface IGameObserver
    {
        void NotifyForNextState(CounterDeckBuilder.GameState state);

        void NotifyForMove(PlayerTask task);

        void NotifyForEndGame();
    }
}
