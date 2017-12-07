using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public partial class ZX128 : SpectrumBase
    {
        /* 128k paging controlled by writes to port 0x7ffd
         * 
         * 
         
            #7FFD (32765) - decoded as A15=0, A1=0 and /IORQ=0. Bits 0..5 are latched. Bits 0..2 select RAM bank in secton D. Bit 3 selects RAM bank to dispay screen (0 - RAM5, 1 - RAM7). Bit 4 selects ROM bank (0 - ROM0, 1 - ROM1). Bit 5, when set locks future writing to #7FFD port until reset. Reading #7FFD port is the same as writing #FF into it.
            #BFFD (49149) - write data byte into AY-3-8912 chip.
            #FFFD (65533) - select AY-3-8912 addres (D4..D7 ignored) and reading data byte.

         *  0xffff +--------+--------+--------+--------+--------+--------+--------+--------+
                   | Bank 0 | Bank 1 | Bank 2 | Bank 3 | Bank 4 | Bank 5 | Bank 6 | Bank 7 |
                   |        |        |(also at|        |        |(also at|        |        |
                   |        |        | 0x8000)|        |        | 0x4000)|        |        |
                   |        |        |        |        |        | screen |        | screen |
            0xc000 +--------+--------+--------+--------+--------+--------+--------+--------+
                   | Bank 2 |        Any one of these pages may be switched in.
                   |        |
                   |        |
                   |        |
            0x8000 +--------+
                   | Bank 5 |
                   |        |
                   |        |
                   | screen |
            0x4000 +--------+--------+
                   | ROM 0  | ROM 1  | Either ROM may be switched in.
                   |        |        |
                   |        |        |
                   |        |        |
            0x0000 +--------+--------+
        */

        /// <summary>
        /// Simulates reading from the bus (no contention)
        /// Paging should be handled here
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public override byte ReadBus(ushort addr)
        {
            int divisor = addr / 0x4000;
            byte result = 0xff;
            switch (divisor)
            {
                // ROM 0x000
                case 0:
                    if (ROMPaged == 0)
                        result = Memory[0][addr % 0x4000];
                    else
                        result = Memory[1][addr % 0x4000];
                    break;

                // RAM 0x4000 (RAM5 - Bank5 or shadow bank RAM7)
                case 1:
                    result = Memory[7][addr % 0x4000];
                    break;

                // RAM 0x8000 (RAM2 - Bank2)
                case 2:
                    result = Memory[4][addr % 0x4000];
                    break;

                // RAM 0xc000 (any ram bank 0 - 7 may be paged in - default bank0)
                case 3:
                    switch (RAMPaged)
                    {
                        case 0:
                            result = Memory[2][addr % 0x4000];
                            break;
                        case 1:
                            result = Memory[3][addr % 0x4000];
                            break;
                        case 2:
                            result = Memory[4][addr % 0x4000];
                            break;
                        case 3:
                            result = Memory[5][addr % 0x4000];
                            break;
                        case 4:
                            result = Memory[6][addr % 0x4000];
                            break;
                        case 5:
                            result = Memory[7][addr % 0x4000];
                            break;
                        case 6:
                            result = Memory[8][addr % 0x4000];
                            break;
                        case 7:
                            result = Memory[9][addr % 0x4000];
                            break;
                    }
                    break;
                default:
                    break;
            }

            return result;
        }

        /// <summary>
        /// Simulates writing to the bus (no contention)
        /// Paging should be handled here
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        public override void WriteBus(ushort addr, byte value)
        {
            int divisor = addr / 0x4000;
            switch (divisor)
            {
                // ROM 0x000
                case 0:
                    if (ROMPaged == 0)
                        Memory[0][addr % 0x4000] = value;
                    else
                        Memory[1][addr % 0x4000] = value;
                    break;

                // RAM 0x4000 (RAM5 - Bank5 or shadow bank RAM7)
                case 1:
                    Memory[7][addr % 0x4000] = value;
                    break;

                // RAM 0x8000 (RAM2 - Bank2)
                case 2:
                    Memory[4][addr % 0x4000] = value;
                    break;

                // RAM 0xc000 (any ram bank 0 - 7 may be paged in - default bank0)
                case 3:
                    switch (RAMPaged)
                    {
                        case 0:
                            Memory[2][addr % 0x4000] = value;
                            break;
                        case 1:
                            Memory[3][addr % 0x4000] = value;
                            break;
                        case 2:
                            Memory[4][addr % 0x4000] = value;
                            break;
                        case 3:
                            Memory[5][addr % 0x4000] = value;
                            break;
                        case 4:
                            Memory[6][addr % 0x4000] = value;
                            break;
                        case 5:
                            Memory[7][addr % 0x4000] = value;
                            break;
                        case 6:
                            Memory[8][addr % 0x4000] = value;
                            break;
                        case 7:
                            Memory[9][addr % 0x4000] = value;
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Reads a byte of data from a specified memory address
        /// (with memory contention if appropriate)
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public override byte ReadMemory(ushort addr)
        {
            var data = ReadBus(addr);
            if ((addr & 0xC000) == 0x4000)
            {
                // addr is in RAM not ROM - apply memory contention if neccessary
                var delay = GetContentionValue(CurrentFrameCycle);
                CPU.TotalExecutedCycles += delay;
            }
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
            if (addr < 0x4000)
            {
                // Do nothing - we cannot write to ROM
                return;
            }
            else if (addr < 0xC000)
            {
                // possible contended RAM
                var delay = GetContentionValue(CurrentFrameCycle);
                CPU.TotalExecutedCycles += delay;
            }

            WriteBus(addr, value);
        }

        public override void ReInitMemory()
        {
            if (Memory.ContainsKey(0))
                Memory[0] = ROM0;
            else
                Memory.Add(0, ROM0);

            if (Memory.ContainsKey(1))
                Memory[1] = ROM1;
            else
                Memory.Add(1, ROM1);

            if (Memory.ContainsKey(2))
                Memory[2] = RAM0;
            else
                Memory.Add(2, RAM0);

            if (Memory.ContainsKey(3))
                Memory[3] = RAM1;
            else
                Memory.Add(3, RAM1);

            if (Memory.ContainsKey(4))
                Memory[4] = RAM2;
            else
                Memory.Add(4, RAM2);

            if (Memory.ContainsKey(5))
                Memory[5] = RAM3;
            else
                Memory.Add(5, RAM3);

            if (Memory.ContainsKey(6))
                Memory[6] = RAM4;
            else
                Memory.Add(6, RAM4);

            if (Memory.ContainsKey(7))
                Memory[7] = RAM5;
            else
                Memory.Add(7, RAM5);

            if (Memory.ContainsKey(8))
                Memory[8] = RAM6;
            else
                Memory.Add(8, RAM6);

            if (Memory.ContainsKey(9))
                Memory[9] = RAM7;
            else
                Memory.Add(9, RAM7);
        }

        /// <summary>
        /// Sets up the ROM
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startAddress"></param>
        public override void InitROM(RomData romData)
        {
            RomData = romData;
            // 128k uses ROM0 and ROM1
            // 128k loader is in ROM0, and fallback 48k rom is in ROM1
            for (int i = 0; i < 0x4000; i++)
            {
                ROM0[i] = RomData.RomBytes[i];
                ROM1[i] = RomData.RomBytes[i + 0x4000];
            }
        }
    }
}
