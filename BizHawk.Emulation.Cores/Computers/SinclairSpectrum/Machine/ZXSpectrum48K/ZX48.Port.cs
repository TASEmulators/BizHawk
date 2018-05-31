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
            ContendPort(port);

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
                ULADevice.ReadFloatingBus((int)CurrentFrameCycle, ref result);

                /*
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
                */
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
            ContendPort(port);

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
            ULADevice.RenderScreen((int)CurrentFrameCycle);
            ULADevice.BorderColor = value & BORDER_BIT;

            // Buzzer
            BuzzerDevice.ProcessPulseValue((value & EAR_BIT) != 0);

            // Tape
            TapeDevice.WritePort(port, value);

            // Tape mic processing (not implemented yet)
            //TapeDevice.ProcessMicBit((value & MIC_BIT) != 0);

        }

        /// <summary>
        /// Simulates IO port contention based on the supplied address
        /// This method is for 48k and 128k/+2 machines only and should be overridden for other models
        /// </summary>
        /// <param name="addr"></param>
        public override void ContendPort(ushort addr)
        {
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

            CPUMon.ContendPort(addr);
            return;
        }

    }
}
