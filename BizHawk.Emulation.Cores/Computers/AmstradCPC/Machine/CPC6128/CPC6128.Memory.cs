using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// CPC6128
    /// * Memory *
    /// </summary>
    public partial class CPC6128 : CPCBase
    {
        /// <summary>
        /// Simulates reading from the bus
        /// ROM and RAM paging should be handled here
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public override byte ReadBus(ushort addr)
        {
            int divisor = addr / 0x4000;
            byte result = 0xff;

            switch (divisor)
            {
                // RAM 0x000
                case 0:
                    if (LowerROMPaged)
                    {
                        result = ROMLower[addr % 0x4000];
                    }
                    else
                    {
                        switch (RAMConfig)
                        {
                            case 2:
                                result = RAM4[addr % 0x4000];
                                break;
                            default:
                                result = RAM0[addr % 0x4000];
                                break;
                        }
                    }
                    break;

                // RAM 0x4000
                case 1:
                    switch (RAMConfig)
                    {
                        case 0:
                        case 1:
                            result = RAM1[addr % 0x4000];
                            break;
                        case 2:
                        case 5:
                            result = RAM5[addr % 0x4000];
                            break;
                        case 3:
                            result = RAM3[addr % 0x4000];
                            break;
                        case 4:
                            result = RAM4[addr % 0x4000];
                            break;
                        case 6:
                            result = RAM6[addr % 0x4000];
                            break;
                        case 7:
                            result = RAM7[addr % 0x4000];
                            break;
                    }

                    break;

                // RAM 0x8000
                case 2:
                    switch (RAMConfig)
                    {
                        case 2:
                            result = RAM6[addr % 0x4000];
                            break;
                        default:
                            result = RAM2[addr % 0x4000];
                            break;
                    }
                    break;

                // RAM 0xc000
                case 3:
                    if (UpperROMPaged)
                    {
                        switch (UpperROMPosition)
                        {
                            case 7:
                                result = ROM7[addr % 0x4000];
                                break;
                            case 0:
                            default:
                                result = ROM0[addr % 0x4000];
                                break;
                        }
                    }
                    else
                    {
                        switch (RAMConfig)
                        {
                            case 1:
                            case 2:
                            case 3:
                                result = RAM7[addr % 0x4000];
                                break;
                            default:
                                result = RAM3[addr % 0x4000];
                                break;
                        }
                    }                    
                    break;
                default:
                    break;
            }

            return result;
        }

        /// <summary>
        /// Simulates writing to the bus
        /// Writes to the bus ALWAYS go to RAM, regardless of what upper and lower ROMs are paged in
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        public override void WriteBus(ushort addr, byte value)
        {
            int divisor = addr / 0x4000;

            switch (divisor)
            {
                // RAM 0x000
                case 0:
                    switch (RAMConfig)
                    {
                        case 2:
                            RAM4[addr % 0x4000] = value;
                            break;
                        default:
                            RAM0[addr % 0x4000] = value;
                            break;
                    }                    
                    break;

                // RAM 0x4000
                case 1:
                    switch (RAMConfig)
                    {
                        case 0:
                        case 1:
                            RAM1[addr % 0x4000] = value;
                            break;
                        case 2:
                        case 5:
                            RAM5[addr % 0x4000] = value;
                            break;
                        case 3:
                            RAM3[addr % 0x4000] = value;
                            break;
                        case 4:
                            RAM4[addr % 0x4000] = value;
                            break;
                        case 6:
                            RAM6[addr % 0x4000] = value;
                            break;
                        case 7:
                            RAM7[addr % 0x4000] = value;
                            break;
                    }
                    
                    break;

                // RAM 0x8000
                case 2:
                    switch (RAMConfig)
                    {
                        case 2:
                            RAM6[addr % 0x4000] = value;
                            break;
                        default:
                            RAM2[addr % 0x4000] = value;
                            break;
                    }
                    break;

                // RAM 0xc000
                case 3:
                    switch (RAMConfig)
                    {
                        case 1:
                        case 2:
                        case 3:
                            RAM7[addr % 0x4000] = value;
                            break;
                        default:
                            RAM3[addr % 0x4000] = value;
                            break;
                    }                    
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Reads a byte of data from a specified memory address
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public override byte ReadMemory(ushort addr)
        {
            var data = ReadBus(addr);
            return data;
        }

        /// <summary>
        /// Writes a byte of data to a specified memory address
        /// (with memory contention if appropriate)
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        public override void WriteMemory(ushort addr, byte value)
        {
            WriteBus(addr, value);
        }


        /// <summary>
        /// Sets up the ROM
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startAddress"></param>
        public override void InitROM(RomData[] romData)
        {
            foreach (var r in romData)
            {
                if (r.ROMType == RomData.ROMChipType.Lower)
                {
                    for (int i = 0; i < 0x4000; i++)
                    {
                        ROMLower[i] = r.RomBytes[i];

                    }
                }
                else
                {
                    for (int i = 0; i < 0x4000; i++)
                    {
                        switch (r.ROMPosition)
                        {
                            case 0:
                                ROM0[i] = r.RomBytes[i];
                                break;
                            case 7:
                                ROM7[i] = r.RomBytes[i];
                                break;
                        }
                    }
                }
            }

            LowerROMPaged = true;
            UpperROMPaged = true;
        }
    }
}
