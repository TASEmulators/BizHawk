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
        public virtual byte ReadPort(ushort port)
        {
            CPU.TotalExecutedCycles += 4;

            byte result = 0xFF;

            // get the high byte from Regs[6]
            ushort high = CPU.Regs[6];

            // combine the low byte (passed in as port) and the high byte (maybe not needed)
            ushort word = Convert.ToUInt16(((byte)port << 8 | (byte)high));

            // Check whether the low bit is reset
            // Technically the ULA should respond to every even I/O address
            bool lowBitReset = (port & 0x0001) == 0;   
            
            // Kempston Joystick
            //not implemented yet        

            if (lowBitReset)
            {
                // Even I/O address so get input
                // The high byte indicates which half-row of keys is being polled
                /*
                  IN:    Reads keys (bit 0 to bit 4 inclusive)
                  0xfefe  SHIFT, Z, X, C, V            0xeffe  0, 9, 8, 7, 6
                  0xfdfe  A, S, D, F, G                0xdffe  P, O, I, U, Y
                  0xfbfe  Q, W, E, R, T                0xbffe  ENTER, L, K, J, H
                  0xf7fe  1, 2, 3, 4, 5                0x7ffe  SPACE, SYM SHFT, M, N, B
                */

                // read keyboard input
                if (high != 0)
                    result = KeyboardDevice.GetLineStatus((byte)high);

                var ear = TapeDevice.GetEarBit(CurrentFrameCycle);
                if (!ear)
                {
                    result = (byte)(result & Convert.ToInt32("10111111", 2));
                }

                /*

                bool tapeIsPlaying = false;
                int tapeBit = 0;

                if (tapeIsPlaying)
                {
                    if (tapeBit == 0)
                    {
                        // reset is EAR ON
                        result = (byte)(result & ~(TAPE_BIT));
                    }
                    else
                    {
                        // set is EAR OFF
                        result |= TAPE_BIT;
                    }
                }
                else
                {
                    if ((LastULAOutByte & (EAR_BIT + MIC_BIT)) == 0)
                    {
                        result = (byte)(result & ~(TAPE_BIT));
                    }
                    else
                    {
                        result |= TAPE_BIT;
                    }
                }
                /*
                // read EAR pulse from tape device
                //todo
                
                bool earBit = false;

                if (earBit)
                    tapeBit = Convert.ToInt32("11111111", 2);
                else
                    tapeBit = Convert.ToInt32("10111111", 2);

                //var earBit = _tapeDevice.GetEarBit(_cpu.Tacts);

                if (!earBit)
                    result = (byte)(result & tapeBit);
                */
            }
            else
            {
                // devices other than the ULA will respond here
                // (e.g. the AY sound chip in a 128k spectrum

                // AY register activate
                // Kemptson Mouse


                // if unused port the floating memory bus should be returned (still todo)
            }

            return result;
        }

        /// <summary>
        /// Writes a byte of data to a specified port address
        /// </summary>
        /// <param name="port"></param>
        /// <param name="value"></param>
        public virtual void WritePort(ushort port, byte value)
        {
            CPU.TotalExecutedCycles += 4;

            // Check whether the low bit is reset
            // Technically the ULA should respond to every even I/O address
            bool lowBitReset = (port & 0x0001) == 0;

            // Only even addresses address the ULA
            if (lowBitReset)
            {
                // store the last OUT byte
                LastULAOutByte = value;

                /*
                    Bit   7   6   5   4   3   2   1   0
                        +-------------------------------+
                        |   |   |   | E | M |   Border  |
                        +-------------------------------+
                */
                
                // Border - LSB 3 bits hold the border colour
                BorderColour = value & BORDER_BIT;

                // Buzzer
                var beepVal = (int)value & (EAR_BIT);

                if (((int)value & MIC_BIT) == 0)
                    beepVal = (int)value & (MIC_BIT);

                //var micval = (int)value & (MIC_BIT);


                // if tape is not playing
                BuzzerDevice.ProcessPulseValue(false, (beepVal) != 0);

                // tape
                //_tapeDevice.ProcessMicBit((data & 0x08) != 0);
            }
        }
    }
}
