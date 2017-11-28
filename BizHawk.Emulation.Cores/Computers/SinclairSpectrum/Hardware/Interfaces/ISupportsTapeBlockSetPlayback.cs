using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// This interface represents that the implementing class supports
    /// emulating tape playback of a set of subsequent tape blocks
    /// </summary>
    public interface ISupportsTapeBlockSetPlayback : ISupportsTapeBlockPlayback
    {
        /// <summary>
        /// Moves the player to the next playable block
        /// </summary>
        /// <param name="currentCycle">Tacts time to start the next block</param>
        void NextBlock(long currentCycle);
    }
}
