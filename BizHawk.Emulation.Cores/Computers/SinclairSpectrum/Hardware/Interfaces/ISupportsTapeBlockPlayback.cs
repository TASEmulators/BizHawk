using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// This interface represents that the implementing class supports
    /// emulating tape playback of a single tape block
    /// </summary>
    public interface ISupportsTapeBlockPlayback
    {
        /// <summary>
        /// The current playing phase
        /// </summary>
        PlayPhase PlayPhase { get; }

        /// <summary>
        /// The tact count of the CPU when playing starts
        /// </summary>
        long StartCycle { get; }

        /// <summary>
        /// Initializes the player
        /// </summary>
        void InitPlay(long startCycle);

        /// <summary>
        /// Gets the EAR bit value for the specified tact
        /// </summary>
        /// <param name="currentCycle">Tacts to retrieve the EAR bit</param>
        /// <returns>
        /// The EAR bit value to play back
        /// </returns>
        bool GetEarBit(long currentCycle);
    }
}
