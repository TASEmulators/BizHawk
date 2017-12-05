using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// The abstract class that all emulated models will inherit from
    /// * Port Access *
    /// </summary>
    public abstract partial class SpectrumBase
    {
        /// <summary>
        /// The last OUT data that was sent to the ULA
        /// </summary>
        protected byte LastULAOutByte;

        /// <summary>
        /// Reads a byte of data from a specified port address
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public abstract byte ReadPort(ushort port);

        /// <summary>
        /// Writes a byte of data to a specified port address
        /// </summary>
        /// <param name="port"></param>
        /// <param name="value"></param>
        public abstract void WritePort(ushort port, byte value);

        /// <summary>
        /// Apply I/O contention if necessary
        /// </summary>
        /// <param name="port"></param>
        public virtual void ContendPort(ushort port)
        {
            var lowBit = (port & 0x0001) != 0;
            var ulaHigh = (port & 0xc000) == 0x4000;
            var cfc = CurrentFrameCycle;
            if (cfc < 1)
                cfc = 1;
            
            if (ulaHigh)
            {
                CPU.TotalExecutedCycles += GetContentionValue(cfc - 1);
            }                
            else
            {
                if (!lowBit)
                    CPU.TotalExecutedCycles += GetContentionValue(cfc);
            }
        }
    }
}
