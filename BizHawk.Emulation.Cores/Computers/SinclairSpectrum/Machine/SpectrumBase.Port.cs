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
        public byte LASTULAOutByte
        {
            get { return LastULAOutByte; }
            set { LastULAOutByte = value; }
        }

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
        /// Increments the CPU totalCycles counter by the tStates value specified
        /// </summary>
        /// <param name="tStates"></param>
        public virtual void PortContention(int tStates)
        {
            CPU.TotalExecutedCycles += tStates;
        }

        /// <summary>
        /// Simulates IO port contention based on the supplied address
        /// This method is for 48k and 128k/+2 machines only and should be overridden for other models
        /// </summary>
        /// <param name="addr"></param>
        public virtual void ContendPortAddress(ushort addr)
        {
            return;

            /*
            It takes four T states for the Z80 to read a value from an I/O port, or write a value to a port. As is the case with memory access, 
            this can be lengthened by the ULA. There are two effects which occur here:

            If the port address being accessed has its low bit reset, the ULA is required to supply the result, which leads to a delay if it is 
            currently busy handling the screen.
            The address of the port being accessed is placed on the data bus. If this is in the range 0x4000 to 0x7fff, the ULA treats this as an 
            attempted access to contended memory and therefore introduces a delay. If the port being accessed is between 0xc000 and 0xffff, 
            this effect does not apply, even on a 128K machine if a contended memory bank is paged into the range 0xc000 to 0xffff.

            These two effects combine to lead to the following contention patterns:

                High byte   |         | 
                in 40 - 7F? | Low bit | Contention pattern  
                ------------+---------+-------------------
                     No     |  Reset  | N:1, C:3
                     No     |   Set   | N:4
                    Yes     |  Reset  | C:1, C:3
                    Yes     |   Set   | C:1, C:1, C:1, C:1
            
            The 'Contention pattern' column should be interpreted from left to right. An "N:n" entry means that no delay is applied at this cycle, and the Z80 continues uninterrupted for 'n' T states. A "C:n" entry means that the ULA halts the Z80; the delay is exactly the same as would occur for a contended memory access at this cycle (eg 6 T states at cycle 14335, 5 at 14336, etc on the 48K machine). After this delay, the Z80 then continues for 'n' cycles.
            */
            
            // is the low bit reset (i.e. is this addressing the ULA)?
            bool lowBit = (addr & 0x0001) != 0;

            if ((addr & 0xc000) == 0x4000 || (addr & 0xc000) == 0xC000)
            {
                // high byte is in 40 - 7F
                if (lowBit)
                {
                    // lowbit is set
                    // C:1, C:1, C:1, C:1
                    for (int i = 0; i < 4; i++)
                    {
                        CPU.TotalExecutedCycles += ULADevice.contentionTable[CurrentFrameCycle];
                        CPU.TotalExecutedCycles++;
                    }
                }
                else
                {
                    // low bit is reset
                    // C:1, C:3
                    CPU.TotalExecutedCycles += ULADevice.contentionTable[CurrentFrameCycle];
                    CPU.TotalExecutedCycles++;
                    CPU.TotalExecutedCycles += ULADevice.contentionTable[CurrentFrameCycle];
                    CPU.TotalExecutedCycles += 3;
                }
            }
            else
            {
                // high byte is NOT in 40 - 7F
                if (lowBit)
                {
                    // lowbit is set
                    // C:1, C:1, C:1, C:1
                    CPU.TotalExecutedCycles += 4;
                }
                else
                {
                    // lowbit is reset
                    // N:1, C:3
                    CPU.TotalExecutedCycles++;
                    CPU.TotalExecutedCycles += ULADevice.contentionTable[CurrentFrameCycle];
                    CPU.TotalExecutedCycles += 3;
                }
            }
        }
        
    }
}
