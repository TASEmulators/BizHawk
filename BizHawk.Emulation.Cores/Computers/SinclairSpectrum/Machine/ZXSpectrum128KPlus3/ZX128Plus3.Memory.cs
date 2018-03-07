using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public partial class ZX128Plus3 : SpectrumBase
    {
        /*  http://www.worldofspectrum.org/faq/reference/128kreference.htm
         *  
         *  Port 0x7ffd behaves in the almost exactly the same way as on the 128K/+2, with two exceptions:

            Bit 4 is now the low bit of the ROM selection.
            The partial decoding used is now slightly different: the hardware will respond only to those port addresses with bit 1 reset, bit 14 set and bit 15 reset (as opposed to just bits 1 and 15 reset on the 128K/+2).
            The extra paging features of the +2A/+3 are controlled by port 0x1ffd (again, partial decoding applies here: the hardware will respond to all port addresses with bit 1 reset, bit 12 set and bits 13, 14 and 15 reset). This port is also write-only, and its last value should be saved at 0x5b67 (23399).

            Port 0x1ffd responds as follows:

              Bit 0: Paging mode. 0=normal, 1=special
              Bit 1: In normal mode, ignored.
              Bit 2: In normal mode, high bit of ROM selection. The four ROMs are:
                      ROM 0: 128k editor, menu system and self-test program
                      ROM 1: 128k syntax checker
                      ROM 2: +3DOS
                      ROM 3: 48 BASIC
              Bit 3: Disk motor; 1=on, 0=off
              Bit 4: Printer port strobe.
            When special mode is selected, the memory map changes to one of four configurations specified in bits 1 and 2 of port 0x1ffd:
                     Bit 2 =0    Bit 2 =0    Bit 2 =1    Bit 2 =1
                     Bit 1 =0    Bit 1 =1    Bit 1 =0    Bit 1 =1
             0xffff +--------+  +--------+  +--------+  +--------+
                    | Bank 3 |  | Bank 7 |  | Bank 3 |  | Bank 3 |
                    |        |  |        |  |        |  |        |
                    |        |  |        |  |        |  |        |
                    |        |  | screen |  |        |  |        |
             0xc000 +--------+  +--------+  +--------+  +--------+
                    | Bank 2 |  | Bank 6 |  | Bank 6 |  | Bank 6 |
                    |        |  |        |  |        |  |        |
                    |        |  |        |  |        |  |        |
                    |        |  |        |  |        |  |        |
             0x8000 +--------+  +--------+  +--------+  +--------+
                    | Bank 1 |  | Bank 5 |  | Bank 5 |  | Bank 7 |
                    |        |  |        |  |        |  |        |
                    |        |  |        |  |        |  |        |
                    |        |  | screen |  | screen |  | screen |
             0x4000 +--------+  +--------+  +--------+  +--------+
                    | Bank 0 |  | Bank 4 |  | Bank 4 |  | Bank 4 |
                    |        |  |        |  |        |  |        |
                    |        |  |        |  |        |  |        |
                    |        |  |        |  |        |  |        |
             0x0000 +--------+  +--------+  +--------+  +--------+
            RAM banks 1,3,4 and 6 are used for the disc cache and RAMdisc, while Bank 7 contains editor scratchpads and +3DOS workspace.
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

            // special paging
            if (SpecialPagingMode)
            {
                switch (divisor)
                {
                    case 0:
                        switch (PagingConfiguration)
                        {
                            case 0:
                                result = Memory[4][addr % 0x4000];
                                break;
                            case 1:
                            case 2:
                            case 3:
                                result = Memory[8][addr % 0x4000];
                                break;
                        }
                        break;
                    case 1:
                        switch (PagingConfiguration)
                        {
                            case 0:
                                result = Memory[5][addr % 0x4000];
                                break;
                            case 1:
                            case 2:                            
                                result = Memory[9][addr % 0x4000];
                                break;
                            case 3:
                                result = Memory[11][addr % 0x4000];
                                break;
                        }
                        break;
                    case 2:
                        switch (PagingConfiguration)
                        {
                            case 0:
                                result = Memory[6][addr % 0x4000];
                                break;
                            case 1:
                            case 2:
                            case 3:
                                result = Memory[10][addr % 0x4000];
                                break;
                        }
                        break;
                    case 3:
                        switch (PagingConfiguration)
                        {
                            case 0:
                            case 2:
                            case 3:
                                result = Memory[7][addr % 0x4000];
                                break;
                            case 1:
                                result = Memory[11][addr % 0x4000];
                                break;
                        }
                        break;
                }
            }
            else
            {
                switch (divisor)
                {
                    // ROM 0x000
                    case 0:
                        result = Memory[ROMPaged][addr % 0x4000];
                        break;

                    // RAM 0x4000 (RAM5 - Bank5 or shadow bank RAM7)
                    case 1:
                        result = Memory[9][addr % 0x4000];
                        break;

                    // RAM 0x8000 (RAM2 - Bank2)
                    case 2:
                        result = Memory[6][addr % 0x4000];
                        break;

                    // RAM 0xc000 (any ram bank 0 - 7 may be paged in - default bank0)
                    case 3:
                        switch (RAMPaged)
                        {
                            case 0:
                                result = Memory[4][addr % 0x4000];
                                break;
                            case 1:
                                result = Memory[5][addr % 0x4000];
                                break;
                            case 2:
                                result = Memory[6][addr % 0x4000];
                                break;
                            case 3:
                                result = Memory[7][addr % 0x4000];
                                break;
                            case 4:
                                result = Memory[8][addr % 0x4000];
                                break;
                            case 5:
                                result = Memory[9][addr % 0x4000];
                                break;
                            case 6:
                                result = Memory[10][addr % 0x4000];
                                break;
                            case 7:
                                result = Memory[11][addr % 0x4000];
                                break;
                        }
                        break;
                    default:
                        break;
                }
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

            // special paging
            if (SpecialPagingMode)
            {
                switch (divisor)
                {
                    case 0:
                        switch (PagingConfiguration)
                        {
                            case 0:
                                Memory[4][addr % 0x4000] = value;
                                break;
                            case 1:
                            case 2:
                            case 3:
                                Memory[8][addr % 0x4000] = value;
                                break;
                        }
                        break;
                    case 1:
                        switch (PagingConfiguration)
                        {
                            case 0:
                                Memory[5][addr % 0x4000] = value;
                                break;
                            case 1:
                            case 2:
                                Memory[9][addr % 0x4000] = value;
                                break;
                            case 3:
                               Memory[11][addr % 0x4000] = value;
                                break;
                        }
                        break;
                    case 2:
                        switch (PagingConfiguration)
                        {
                            case 0:
                                Memory[6][addr % 0x4000] = value;
                                break;
                            case 1:
                            case 2:
                            case 3:
                                Memory[10][addr % 0x4000] = value;
                                break;
                        }
                        break;
                    case 3:
                        switch (PagingConfiguration)
                        {
                            case 0:
                            case 2:
                            case 3:
                                Memory[7][addr % 0x4000] = value;
                                break;
                            case 1:
                                Memory[11][addr % 0x4000] = value;
                                break;
                        }
                        break;
                }
            }
            else
            {
                switch (divisor)
                {
                    // ROM 0x000
                    case 0:
                        Memory[ROMPaged][addr % 0x4000] = value;
                        break;

                    // RAM 0x4000 (RAM5 - Bank5 or shadow bank RAM7)
                    case 1:
                        Memory[9][addr % 0x4000] = value;
                        break;

                    // RAM 0x8000 (RAM2 - Bank2)
                    case 2:
                        Memory[6][addr % 0x4000] = value;
                        break;

                    // RAM 0xc000 (any ram bank 0 - 7 may be paged in - default bank0)
                    case 3:
                        switch (RAMPaged)
                        {
                            case 0:
                                Memory[4][addr % 0x4000] = value;
                                break;
                            case 1:
                                Memory[5][addr % 0x4000] = value;
                                break;
                            case 2:
                                Memory[6][addr % 0x4000] = value;
                                break;
                            case 3:
                                Memory[7][addr % 0x4000] = value;
                                break;
                            case 4:
                                Memory[8][addr % 0x4000] = value;
                                break;
                            case 5:
                                Memory[9][addr % 0x4000] = value;
                                break;
                            case 6:
                                Memory[10][addr % 0x4000] = value;
                                break;
                            case 7:
                                Memory[11][addr % 0x4000] = value;
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }

            // update ULA screen buffer if necessary
            if ((addr & 49152) == 16384 && _render)
                ULADevice.UpdateScreenBuffer(CurrentFrameCycle);
        }

        /// <summary>
        /// Reads a byte of data from a specified memory address
        /// (with memory contention if appropriate)
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public override byte ReadMemory(ushort addr)
        {
            if (ULADevice.IsContended(addr))
                CPU.TotalExecutedCycles += ULADevice.contentionTable[CurrentFrameCycle];
            
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
            // apply contention if necessary
            if (ULADevice.IsContended(addr))
                CPU.TotalExecutedCycles += ULADevice.contentionTable[CurrentFrameCycle];
            
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
                Memory[2] = ROM2;
            else
                Memory.Add(2, ROM2);

            if (Memory.ContainsKey(3))
                Memory[3] = ROM3;
            else
                Memory.Add(3, ROM3);

            if (Memory.ContainsKey(4))
                Memory[4] = RAM0;
            else
                Memory.Add(4, RAM0);

            if (Memory.ContainsKey(5))
                Memory[5] = RAM1;
            else
                Memory.Add(5, RAM1);

            if (Memory.ContainsKey(6))
                Memory[6] = RAM2;
            else
                Memory.Add(6, RAM2);

            if (Memory.ContainsKey(7))
                Memory[7] = RAM3;
            else
                Memory.Add(7, RAM3);

            if (Memory.ContainsKey(8))
                Memory[8] = RAM4;
            else
                Memory.Add(8, RAM4);

            if (Memory.ContainsKey(9))
                Memory[9] = RAM5;
            else
                Memory.Add(9, RAM5);

            if (Memory.ContainsKey(10))
                Memory[10] = RAM6;
            else
                Memory.Add(10, RAM6);

            if (Memory.ContainsKey(11))
                Memory[11] = RAM7;
            else
                Memory.Add(11, RAM7);
        }

        /// <summary>
        /// Sets up the ROM
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startAddress"></param>
        public override void InitROM(RomData romData)
        {
            RomData = romData;
            // +3 uses ROM0, ROM1, ROM2 & ROM3
            /*  ROM 0: 128k editor, menu system and self-test program
                ROM 1: 128k syntax checker
                ROM 2: +3DOS
                ROM 3: 48 BASIC
            */
            Stream stream = new MemoryStream(RomData.RomBytes);
            stream.Read(ROM0, 0, 16384);
            stream.Read(ROM1, 0, 16384);
            stream.Read(ROM2, 0, 16384);
            stream.Read(ROM3, 0, 16384);
            stream.Dispose();            
        }
    }
}
