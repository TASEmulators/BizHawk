using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public partial class ZX48 : SpectrumBase
    {
        /// <summary>
        /// Reads a byte of data from a specified port address
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public override byte ReadPort(ushort port)
        {
            // process IO contention
            ContendPortAddress(port);

            int result = 0xFF;

            // Check whether the low bit is reset
            // Technically the ULA should respond to every even I/O address
            bool lowBitReset = (port & 0x0001) == 0;

            // Kempston joystick input takes priority over all other input
            // if this is detected just return the kempston byte
            if ((port & 0xe0) == 0 || (port & 0x20) == 0)
            {
                if (LocateUniqueJoystick(JoystickType.Kempston) != null)
                    return (byte)((KempstonJoystick)LocateUniqueJoystick(JoystickType.Kempston) as KempstonJoystick).JoyLine;

                // not a lag frame
                InputRead = true;
            }
            else if (lowBitReset)
            {
                // Even I/O address so get input from keyboard
                KeyboardDevice.ReadPort(port, ref result);

                // not a lagframe
                InputRead = true;           

                // process tape INs
                TapeDevice.ReadPort(port, ref result);
            }
            else
            {
                // devices other than the ULA will respond here
                // (e.g. the AY sound chip in a 128k spectrum

                // AY register activate - no AY chip in a 48k spectrum

                // Kemptson Mouse (not implemented yet)


                // If this is an unused port the floating memory bus should be returned
                // Floating bus is read on the previous cycle
                long _tStates = CurrentFrameCycle - 1;

                // if we are on the top or bottom border return 0xff
                if ((_tStates < ULADevice.contentionStartPeriod) || (_tStates > ULADevice.contentionEndPeriod))
                {
                    result = 0xff;
                }
                else
                {
                    if (ULADevice.floatingBusTable[_tStates] < 0)
                    {
                        result = 0xff;
                    }
                    else
                    {
                        result = ReadBus((ushort)ULADevice.floatingBusTable[_tStates]);
                    }
                }
            }

            return (byte)result;
        }

        /// <summary>
        /// Writes a byte of data to a specified port address
        /// </summary>
        /// <param name="port"></param>
        /// <param name="value"></param>
        public override void WritePort(ushort port, byte value)
        {
            // process IO contention
            ContendPortAddress(port);

            // Check whether the low bit is reset
            // Technically the ULA should respond to every even I/O address
            if ((port & 0x0001) != 0)
                return;

            // store the last OUT byte
            LastULAOutByte = value;

            /*
                Bit   7   6   5   4   3   2   1   0
                    +-------------------------------+
                    |   |   |   | E | M |   Border  |
                    +-------------------------------+
            */

            // Border - LSB 3 bits hold the border colour
            if (ULADevice.borderColour != (value & BORDER_BIT))
            {
                // border value has changed - update the screen buffer
                ULADevice.UpdateScreenBuffer(CurrentFrameCycle);
            }

            ULADevice.borderColour = value & BORDER_BIT;

            // Buzzer
            BuzzerDevice.ProcessPulseValue((value & EAR_BIT) != 0);

            // Tape
            TapeDevice.WritePort(port, value);

            // Tape mic processing (not implemented yet)
            //TapeDevice.ProcessMicBit((value & MIC_BIT) != 0);

        }
       
    }
}
