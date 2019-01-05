using System;
using System.Collections;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Pentagon 128K Port
    /// </summary>
    public partial class Pentagon128 : SpectrumBase
    {
        /// <summary>
        /// Reads a byte of data from a specified port address
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public override byte ReadPort(ushort port)
        {
            bool deviceAddressed = true;

            int result = 0xFF;

            // ports 0x3ffd & 0x7ffd
            // traditionally thought to be write-only
            if (port == 0x3ffd || port == 0x7ffd)
            {
                // https://faqwiki.zxnet.co.uk/wiki/ZX_Spectrum_128
                // HAL bugs
                // Reads from port 0x7ffd cause a crash, as the 128's HAL10H8 chip does not distinguish between reads and writes to this port, 
                // resulting in a floating data bus being used to set the paging registers.

                // -asni (2018-06-08) - need this to pass the final portread tests from fusetest.tap

                // get the floating bus value
                ULADevice.ReadFloatingBus((int)CurrentFrameCycle, ref result, port);
                // use this to set the paging registers
                WritePort(port, (byte)result);
                // return the floating bus value
                return (byte)result;
            }

            // check AY
            if (AYDevice.ReadPort(port, ref result))
                return (byte)result;

            byte lowByte = (byte)(port & 0xff);

            // Kempston joystick input takes priority over keyboard input
            // if this is detected just return the kempston byte
            if (lowByte == 0x1f)
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

                    // process tape INs
                    TapeDevice.ReadPort(port, ref result);
                }
                else
                    deviceAddressed = false;
            }

            if (!deviceAddressed)
            {
                // If this is an unused port the floating memory bus should be returned
                ULADevice.ReadFloatingBus((int)CurrentFrameCycle, ref result, port);
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
            // get a BitArray of the port
            BitArray portBits = new BitArray(BitConverter.GetBytes(port));
            // get a BitArray of the value byte
            BitArray bits = new BitArray(new byte[] { value });

            // handle AY port writes
            AYDevice.WritePort(port, value);

            // memory paging
            // this is controlled by writes to port 0x7ffd
            // but it is only partially decoded so it actually responds to any port with bits 1 and 15 reset
            if (portBits[1] == false && portBits[15] == false)
            {
                Last7ffd = value;

                // if paging is disabled then all writes to this port are ignored until the next reboot
                if (!PagingDisabled)
                {
                    // Bits 0, 1, 2 select the RAM page
                    var rp = value & 0x07;
                    if (RAMPaged != rp && rp < 8)
                        RAMPaged = rp;

                    // bit 3 controls shadow screen
                    if (SHADOWPaged != bits[3])
                        SHADOWPaged = bits[3];

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
                else
                {
                    // no changes to paging
                }
            }

			if (port == 0x1ffd)
			{

			}

            // Check whether the low bit is reset
            // Technically the ULA should respond to every even I/O address
            bool lowBitReset = !portBits[0]; // (port & 0x01) == 0;

            // Only even addresses address the ULA
            if (lowBitReset)
            {
                LastFe = value;

                // store the last OUT byte
                LastULAOutByte = value;

                /*
                    Bit   7   6   5   4   3   2   1   0
                        +-------------------------------+
                        |   |   |   | E | M |   Border  |
                        +-------------------------------+
                */

                // Border - LSB 3 bits hold the border colour
                if (ULADevice.BorderColor != (value & BORDER_BIT))
                {
                    //ULADevice.RenderScreen((int)CurrentFrameCycle);
                    ULADevice.BorderColor = value & BORDER_BIT;
                }

                // Buzzer
                BuzzerDevice.ProcessPulseValue((value & EAR_BIT) != 0);
                TapeDevice.WritePort(port, value);

                // Tape
                //TapeDevice.ProcessMicBit((value & MIC_BIT) != 0);                
            }    
        }
    }
}
