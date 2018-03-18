using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public partial class ZX128 : SpectrumBase
    {
        /// <summary>
        /// Reads a byte of data from a specified port address
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public override byte ReadPort(ushort port)
        {
            bool deviceAddressed = true;

            // process IO contention
            ContendPortAddress(port);

            int result = 0xFF;

            // check AY
            if (AYDevice.ReadPort(port, ref result))
                return (byte)result;

            // Kempston joystick input takes priority over all other input
            // if this is detected just return the kempston byte
            if ((port & 0xe0) == 0 || (port & 0x20) == 0)
            {
                if (LocateUniqueJoystick(JoystickType.Kempston) != null)
                    return (byte)((KempstonJoystick)LocateUniqueJoystick(JoystickType.Kempston) as KempstonJoystick).JoyLine;

                InputRead = true;
            }
            else
            {
                if (KeyboardDevice.ReadPort(port, ref result))
                {
                    // not a lagframe
                    InputRead = true;

                    // tape loading monitor cycle
                    TapeDevice.MonitorRead();

                    // process tape INs
                    TapeDevice.ReadPort(port, ref result);
                }
                else
                    deviceAddressed = false;
            }

            if (!deviceAddressed)
            {
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

            /*

            // Check whether the low bit is reset
            // Technically the ULA should respond to every even I/O address
            bool lowBitReset = (port & 0x0001) == 0;

            // Kempston joystick input takes priority over all other input
            // if this is detected just return the kempston byte
            if ((port & 0xe0) == 0 || (port & 0x20) == 0)
            {
                if (LocateUniqueJoystick(JoystickType.Kempston) != null)
                    return (byte)((KempstonJoystick)LocateUniqueJoystick(JoystickType.Kempston) as KempstonJoystick).JoyLine;

                InputRead = true;
            }
            else if (lowBitReset)
            {
                // Even I/O address so get input from keyboard
                KeyboardDevice.ReadPort(port, ref result);

                // not a lagframe
                InputRead = true;

                // tape loading monitor cycle
                TapeDevice.MonitorRead();

                // process tape INs
                TapeDevice.ReadPort(port, ref result);
            }
            else if ((port & 0xc002) == 0xc000)
            {
                // AY sound chip
                result = (int)AYDevice.PortRead();
            }
            else
            {
                // devices other than the ULA will respond here

                // Kempston Mouse (not implemented yet)


                // If this is an unused port the floating memory bus should be returned
                // Floating bus is read on the previous cycle
                int _tStates = CurrentFrameCycle - 1;

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

            */

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

            // get a BitArray of the port
            BitArray portBits = new BitArray(BitConverter.GetBytes(port));
            // get a BitArray of the value byte
            BitArray bits = new BitArray(new byte[] { value });

            long currT = CPU.TotalExecutedCycles;

            AYDevice.WritePort(port, value);

            // paging
            if (port == 0x7ffd)
            {
                //if (PagingDisabled)
                    //return;

                // Bits 0, 1, 2 select the RAM page
                var rp = value & 0x07;
                if (rp < 8)
                    RAMPaged = rp;

                // bit 3 controls shadow screen
                SHADOWPaged = bits[3];

                if (SHADOWPaged == false)
                {

                }
                else
                {

                }

                // ROM page
                if (bits[4])
                {
                    // 48k basic rom
                    ROMPaged = 1;
                }
                else
                {
                    // 128k editor and menu system
                    ROMPaged = 0;
                }

                // Bit 5 set signifies that paging is disabled until next reboot
                PagingDisabled = bits[5];
            }

            // Check whether the low bit is reset
            // Technically the ULA should respond to every even I/O address
            bool lowBitReset = !portBits[0]; // (port & 0x01) == 0;

            // Only even addresses address the ULA
            if (lowBitReset)
            {
                // store the last OUT byte
                LastULAOutByte = value;
                CPU.TotalExecutedCycles += ULADevice.contentionTable[CurrentFrameCycle];

                /*
                    Bit   7   6   5   4   3   2   1   0
                        +-------------------------------+
                        |   |   |   | E | M |   Border  |
                        +-------------------------------+
                */

                // Border - LSB 3 bits hold the border colour
                if (ULADevice.borderColour != (value & BORDER_BIT))
                    ULADevice.UpdateScreenBuffer(CurrentFrameCycle);

                ULADevice.borderColour = value & BORDER_BIT;

                // Buzzer
                BuzzerDevice.ProcessPulseValue(false, (value & EAR_BIT) != 0);

                // Tape
                //TapeDevice.ProcessMicBit((value & MIC_BIT) != 0);
                
            }
            /*
            // Active AY Register
            if ((port & 0xc002) == 0xc000)
            {
                var reg = value & 0x0f;
                AYDevice.SelectedRegister = reg;
                CPU.TotalExecutedCycles += 3;
            }

            // AY Write
            if ((port & 0xc002) == 0x8000)
            {
                AYDevice.PortWrite(value);
                CPU.TotalExecutedCycles += 3;
            }    
            */        
        }
    }
}
