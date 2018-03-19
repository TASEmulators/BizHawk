using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// The MIC and EAR pins in the spectrum deal in on/off pulses of varying lengths
    /// This struct therefore represents 1 of these pulses
    /// </summary>
    public struct Pulse
    {
        /// <summary>
        /// True:   High State
        /// False:  Low State
        /// </summary>
        public bool State { get; set; }

        /// <summary>
        /// Pulse length in Z80 T-States (cycles)
        /// </summary>
        public long Length { get; set; }
    }
}
