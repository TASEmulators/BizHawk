using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// CPC464
    /// * Memory *
    /// </summary>
    public partial class CPC464 : CPCBase
    {
        /// <summary>
        /// Simulates reading from the bus
        /// ROM paging should be handled here
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public override byte ReadBus(ushort addr)
        {
            int divisor = addr / 0x4000;
            byte result = 0xff;

            switch (divisor)
            {
                // 0x000 or LowerROM
                case 0:
                    if (LowerROMPaged)
                        result = ROMLower[addr % 0x4000];
                    else
                        result = RAM0[addr % 0x4000];
                    break;

                // 0x4000
                case 1:
                    result = RAM1[addr % 0x4000];
                    break;

                // 0x8000
                case 2:
                    result = RAM2[addr % 0x4000];
                    break;

                // 0xc000 or UpperROM
                case 3:
                    if (UpperROMPaged)
                        result = ROM0[addr % 0x4000];
                    else
                        result = RAM3[addr % 0x4000];
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
                    RAM0[addr % 0x4000] = value;
                    break;

                // RAM 0x4000
                case 1:
                    RAM1[addr % 0x4000] = value;
                    break;

                // RAM 0x8000
                case 2:
                    RAM2[addr % 0x4000] = value;
                    break;

                // RAM 0xc000
                case 3:
                    RAM3[addr % 0x4000] = value;
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
