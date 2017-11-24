using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Represetns the data in the tape
    /// </summary>
    public interface ITapeData
    {
        /// <summary>
        /// Block Data
        /// </summary>
        byte[] Data { get; }

        /// <summary>
        /// Pause after this block (given in milliseconds)
        /// </summary>
        ushort PauseAfter { get; }
    }
}
