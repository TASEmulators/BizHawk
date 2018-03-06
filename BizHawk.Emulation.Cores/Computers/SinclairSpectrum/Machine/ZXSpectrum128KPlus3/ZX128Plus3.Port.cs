using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public partial class ZX128Plus3 : SpectrumBase
    {
        /// <summary>
        /// Reads a byte of data from a specified port address
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public override byte ReadPort(ushort port)
        {
            int result = 0xFF;

            // Check whether the low bit is reset
            // Technically the ULA should respond to every even I/O address
            bool lowBitReset = (port & 0x0001) == 0;

            ULADevice.Contend(port);

            // Kempston Joystick
            if ((port & 0xe0) == 0 || (port & 0x20) == 0)
            {
                if (LocateUniqueJoystick(JoystickType.Kempston) != null)
                    return (byte)((KempstonJoystick)LocateUniqueJoystick(JoystickType.Kempston) as KempstonJoystick).JoyLine;
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

                if ((port & 0x8000) == 0)
                    result &= KeyboardDevice.KeyLine[7];

                if ((port & 0x4000) == 0)
                    result &= KeyboardDevice.KeyLine[6];

                if ((port & 0x2000) == 0)
                    result &= KeyboardDevice.KeyLine[5];

                if ((port & 0x1000) == 0)
                    result &= KeyboardDevice.KeyLine[4];

                if ((port & 0x800) == 0)
                    result &= KeyboardDevice.KeyLine[3];

                if ((port & 0x400) == 0)
                    result &= KeyboardDevice.KeyLine[2];

                if ((port & 0x200) == 0)
                    result &= KeyboardDevice.KeyLine[1];

                if ((port & 0x100) == 0)
                    result &= KeyboardDevice.KeyLine[0];

                result = result & 0x1f; //mask out lower 4 bits
                result = result | 0xa0; //set bit 5 & 7 to 1


                if (TapeDevice.TapeIsPlaying)//.CurrentMode == TapeOperationMode.Load)
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
                else if ((LastULAOutByte & 0x10) == 0)
                {
                    result &= ~(0x40);                   
                }
                else
                {
                    result |= 0x40;
                }

            }
            else
            {
                // devices other than the ULA will respond here
                // (e.g. the AY sound chip in a 128k spectrum

                // AY register activate - on +3/2a both FFFD and BFFD active AY
                if ((port & 0xc002) == 0xc000)
                {
                    result = (int)AYDevice.PortRead();
                }
                else if ((port & 0xc002) == 0x8000)
                {
                    result = (int)AYDevice.PortRead();
                }

                // Kempston Mouse


                else if ((port & 0xF002) == 0x2000) //Is bit 12 set and bits 13,14,15 and 1 reset?
                {
                    //result = udpDrive.DiskStatusRead();
                }
                else if ((port & 0xF002) == 0x3000)
                {
                    //result = udpDrive.DiskReadByte();
                }

                else if ((port & 0xF002) == 0x0)
                {
                    if (PagingDisabled)
                        result = 0x1;
                    else
                        result = 0xff;
                }

                // if unused port the floating memory bus should be returned (still todo)
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
            // Check whether the low bit is reset
            // Technically the ULA should respond to every even I/O address
            bool lowBitReset = (port & 0x01) == 0;

            ULADevice.Contend(port);

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
                if (ULADevice.borderColour != (value & BORDER_BIT))
                    ULADevice.UpdateScreenBuffer(CurrentFrameCycle);

                ULADevice.borderColour = value & BORDER_BIT;

                // Buzzer
                BuzzerDevice.ProcessPulseValue(false, (value & EAR_BIT) != 0);

                // Tape
                //TapeDevice.ProcessMicBit((value & MIC_BIT) != 0);
            }

            else
            {
                // AY Register activation
                if ((port & 0xc002) == 0xc000)
                {
                    var reg = value & 0x0f;
                    AYDevice.SelectedRegister = reg;
                    CPU.TotalExecutedCycles += 3;
                }
                else
                {
                    if ((port & 0xC002) == 0x8000)
                    {
                        AYDevice.PortWrite(value);
                        CPU.TotalExecutedCycles += 3;
                    }
                    else
                    {
                        if ((port & 0xC002) == 0x4000) //Are bits 1 and 15 reset and bit 14 set?
                        {
                            // memory paging activate
                            if (PagingDisabled)
                                return;

                            // bit 5 handles paging disable (48k mode, persistent until next reboot)
                            if ((value & 0x20) != 0)
                            {
                                PagingDisabled = true;
                            }

                            // shadow screen
                            if ((value & 0x08) != 0)
                            {
                                SHADOWPaged = true;
                            }
                            else
                            {
                                SHADOWPaged = false;
                            }
                        }
                        else
                        {
                            //Extra Memory Paging feature activate
                            if ((port & 0xF002) == 0x1000) //Is bit 12 set and bits 13,14,15 and 1 reset?
                            {
                                if (PagingDisabled)
                                    return;

                                // set disk motor state
                                //todo

                                if ((value & 0x08) != 0)
                                {
                                    //diskDriveState |= (1 << 4);
                                }
                                else
                                {
                                    //diskDriveState &= ~(1 << 4);
                                }

                                if ((value & 0x1) != 0)
                                {
                                    // activate special paging mode
                                    SpecialPagingMode = true;
                                    PagingConfiguration = (value & 0x6 >> 1);
                                }
                                else
                                {
                                    // normal paging mode
                                    SpecialPagingMode = false;
                                }
                            }
                            else
                            {
                                // disk write port
                                if ((port & 0xF002) == 0x3000) //Is bit 12 set and bits 13,14,15 and 1 reset?
                                {
                                    //udpDrive.DiskWriteByte((byte)(val & 0xff));
                                }
                            }
                        }
                    }
                }
            }


            // paging
            if (port == 0x7ffd)
            {
                if (PagingDisabled)
                    return;

                LastULAOutByte = value;

                
                    



                    // Bits 0, 1, 2 select the RAM page
                    var rp = value & 0x07;
                if (rp < 8)
                    RAMPaged = rp;

                // ROM page
                if ((value & 0x10) != 0)
                {
                    // 48k ROM
                    ROMPaged = 1;
                }
                else
                {
                    ROMPaged = 0;
                }

                // Bit 5 signifies that paging is disabled until next reboot
                if ((value & 0x20) != 0)
                    PagingDisabled = true;


                return;
            }

            
        }
    }
}
