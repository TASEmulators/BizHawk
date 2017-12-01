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
            int result = 0xFF;

            // Check whether the low bit is reset
            // Technically the ULA should respond to every even I/O address
            bool lowBitReset = (port & 0x0001) == 0;

            ContendPort((ushort)port);
                      

            // Kempston Joystick
            if (port == 0x1f)
            {

            }  
            else if (lowBitReset)
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

                result &= KeyboardDevice.GetLineStatus((byte)(port >> 8));
                /*
                if (high == 0xfe)
                    result &= KeyboardDevice.KeyLine[0];
                if (high == 0xfd)
                    result &= KeyboardDevice.KeyLine[1];
                if (high == 0xfb)
                    result &= KeyboardDevice.KeyLine[2];
                if (high == 0xf7)
                    result &= KeyboardDevice.KeyLine[3];
                if (high == 0xef)
                    result &= KeyboardDevice.KeyLine[4];
                if (high == 0xdf)
                    result &= KeyboardDevice.KeyLine[5];
                if (high == 0xbf)
                    result &= KeyboardDevice.KeyLine[6];
                if (high == 0x7f)
                    result &= KeyboardDevice.KeyLine[7];
*/

                result = result & 0x1f; //mask out lower 4 bits
                result = result | 0xa0; //set bit 5 & 7 to 1

                
                if (TapeDevice.CurrentMode == TapeOperationMode.Load)
                {
                    if (!TapeDevice.GetEarBit(CPU.TotalExecutedCycles))
                    {
                        result &= ~(TAPE_BIT);      // reset is EAR ON
                    }
                    else
                    {
                        result |= (TAPE_BIT);       // set is EAR Off
                    }
                }
                else
                {
                    if (KeyboardDevice.IsIssue2Keyboard)
                    {
                        if ((LastULAOutByte & (EAR_BIT + MIC_BIT)) == 0)
                        {
                            result &= ~(TAPE_BIT);
                        }
                        else
                        {
                            result |= TAPE_BIT;
                        }
                    }
                    else
                    {
                        if ((LastULAOutByte & EAR_BIT) == 0)
                        {
                            result &= ~(TAPE_BIT);
                        }
                        else
                        {
                            result |= TAPE_BIT;
                        }
                    }
                }
                /*
                // read keyboard input
                if (high != 0)
                    result &= KeyboardDevice.GetLineStatus((byte)high);
                
                var ear = TapeDevice.GetEarBit(CPU.TotalExecutedCycles);
                if (!ear)
                {
                    result = (byte)(result & Convert.ToInt32("10111111", 2));
                }
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
            
            return (byte)result;
        }

        /// <summary>
        /// Writes a byte of data to a specified port address
        /// </summary>
        /// <param name="port"></param>
        /// <param name="value"></param>
        public virtual void WritePort(ushort port, byte value)
        {
            // Check whether the low bit is reset
            // Technically the ULA should respond to every even I/O address
            bool lowBitReset = (port & 0x01) == 0;

            ContendPort(port);

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
                BuzzerDevice.ProcessPulseValue(false, (value & EAR_BIT) != 0);

                // Tape
                TapeDevice.ProcessMicBit((value & MIC_BIT) != 0);
            }
        }

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
