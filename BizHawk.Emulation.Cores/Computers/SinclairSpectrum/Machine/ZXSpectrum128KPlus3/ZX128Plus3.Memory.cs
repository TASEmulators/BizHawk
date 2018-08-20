using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// +3 Memory
    /// </summary>
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
                                result = RAM0[addr % 0x4000];
                                break;
                            case 1:
                            case 2:
                            case 3:
                                result = RAM4[addr % 0x4000];
                                break;
                        }
                        break;
                    case 1:
                        switch (PagingConfiguration)
                        {
                            case 0:
                                result = RAM1[addr % 0x4000];
                                break;
                            case 1:
                            case 2:                            
                                result = RAM5[addr % 0x4000];
                                break;
                            case 3:
                                result = RAM7[addr % 0x4000];
                                break;
                        }
                        break;
                    case 2:
                        switch (PagingConfiguration)
                        {
                            case 0:
                                result = RAM0[addr % 0x4000];
                                break;
                            case 1:
                            case 2:
                            case 3:
                                result = RAM6[addr % 0x4000];
                                break;
                        }
                        break;
                    case 3:
                        switch (PagingConfiguration)
                        {
                            case 0:
                            case 2:
                            case 3:
                                result = RAM3[addr % 0x4000];
                                break;
                            case 1:
                                result = RAM7[addr % 0x4000];
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
                        switch (_ROMpaged)
                        {
                            case 0:
                                result = ROM0[addr % 0x4000];
                                TestForTapeTraps(addr % 0x4000);
                                break;
                            case 1:
                                result = ROM1[addr % 0x4000];
                                TestForTapeTraps(addr % 0x4000);
                                break;
                            case 2:
                                result = ROM2[addr % 0x4000];
                                break;
                            case 3:
                                result = ROM3[addr % 0x4000];
                                break;
                        }
                        break;

                    // RAM 0x4000 (RAM5 - Bank5 always)
                    case 1:
                        result = RAM5[addr % 0x4000];
                        break;

                    // RAM 0x8000 (RAM2 - Bank2)
                    case 2:
                        result = RAM2[addr % 0x4000];
                        break;

                    // RAM 0xc000 (any ram bank 0 - 7 may be paged in - default bank0)
                    case 3:
                        switch (RAMPaged)
                        {
                            case 0:
                                result = RAM0[addr % 0x4000];
                                break;
                            case 1:
                                result = RAM1[addr % 0x4000];
                                break;
                            case 2:
                                result = RAM2[addr % 0x4000];
                                break;
                            case 3:
                                result = RAM3[addr % 0x4000];
                                break;
                            case 4:
                                result = RAM4[addr % 0x4000];
                                break;
                            case 5:
                                result = RAM5[addr % 0x4000];
                                break;
                            case 6:
                                result = RAM6[addr % 0x4000];
                                break;
                            case 7:
                                result = RAM7[addr % 0x4000];
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
                                RAM0[addr % 0x4000] = value;
                                break;
                            case 1:
                            case 2:
                            case 3:
                                RAM4[addr % 0x4000] = value;
                                break;
                        }
                        break;
                    case 1:
                        switch (PagingConfiguration)
                        {
                            case 0:
                                RAM1[addr % 0x4000] = value;
                                break;
                            case 1:
                            case 2:
                                //ULADevice.RenderScreen((int)CurrentFrameCycle);
                                RAM5[addr % 0x4000] = value;
                                break;
                            case 3:
                                //ULADevice.RenderScreen((int)CurrentFrameCycle);
                                RAM7[addr % 0x4000] = value;
                                break;
                        }
                        break;
                    case 2:
                        switch (PagingConfiguration)
                        {
                            case 0:
                                RAM2[addr % 0x4000] = value;
                                break;
                            case 1:
                            case 2:
                            case 3:
                                RAM6[addr % 0x4000] = value;
                                break;
                        }
                        break;
                    case 3:
                        switch (PagingConfiguration)
                        {
                            case 0:
                            case 2:
                            case 3:
                                RAM3[addr % 0x4000] = value;
                                break;
                            case 1:
                                //ULADevice.RenderScreen((int)CurrentFrameCycle);
                                RAM7[addr % 0x4000] = value;
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
                        /*
                        switch (_ROMpaged)
                        {
                            // cannot write to ROMs
                            case 0:
                                ROM0[addr % 0x4000] = value;
                                break;
                            case 1:
                                ROM1[addr % 0x4000] = value;
                                break;
                            case 2:
                                ROM2[addr % 0x4000] = value;
                                break;
                            case 3:
                                ROM3[addr % 0x4000] = value;
                                break;
                        }
                        */
                        break;

                    // RAM 0x4000 (RAM5 - Bank5 only)
                    case 1:
                        //ULADevice.RenderScreen((int)CurrentFrameCycle);
                        RAM5[addr % 0x4000] = value;
                        break;

                    // RAM 0x8000 (RAM2 - Bank2)
                    case 2:
                        RAM2[addr % 0x4000] = value;
                        break;

                    // RAM 0xc000 (any ram bank 0 - 7 may be paged in - default bank0)
                    case 3:
                        switch (RAMPaged)
                        {
                            case 0:
                                RAM0[addr % 0x4000] = value;
                                break;
                            case 1:
                                RAM1[addr % 0x4000] = value;
                                break;
                            case 2:
                                RAM2[addr % 0x4000] = value;
                                break;
                            case 3:
                                RAM3[addr % 0x4000] = value;
                                break;
                            case 4:
                                RAM4[addr % 0x4000] = value;
                                break;
                            case 5:
                                //ULADevice.RenderScreen((int)CurrentFrameCycle);
                                RAM5[addr % 0x4000] = value;
                                break;
                            case 6:
                                RAM6[addr % 0x4000] = value;
                                break;
                            case 7:
                                //ULADevice.RenderScreen((int)CurrentFrameCycle);
                                RAM7[addr % 0x4000] = value;
                                break;
                        }
                        break;
                    default:
                        break;
                }
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
            if (CPUMon.NextMemReadContended)
            {
                LastContendedReadByte = data;
                CPUMon.NextMemReadContended = false;
            }
                
            return data;
        }

        /// <summary>
        /// Returns the ROM/RAM enum that relates to this particular memory read operation
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public override ZXSpectrum.CDLResult ReadCDL(ushort addr)
        {
            var result = new ZXSpectrum.CDLResult();

            int divisor = addr / 0x4000;
            result.Address = addr % 0x4000;

            // special paging
            if (SpecialPagingMode)
            {
                switch (divisor)
                {
                    case 0:
                        switch (PagingConfiguration)
                        {
                            case 0:
                                result.Type = ZXSpectrum.CDLType.RAM0;
                                break;
                            case 1:
                            case 2:
                            case 3:
                                result.Type = ZXSpectrum.CDLType.RAM4;
                                break;
                        }
                        break;
                    case 1:
                        switch (PagingConfiguration)
                        {
                            case 0:
                                result.Type = ZXSpectrum.CDLType.RAM1;
                                break;
                            case 1:
                            case 2:
                                result.Type = ZXSpectrum.CDLType.RAM5;
                                break;
                            case 3:
                                result.Type = ZXSpectrum.CDLType.RAM7;
                                break;
                        }
                        break;
                    case 2:
                        switch (PagingConfiguration)
                        {
                            case 0:
                                result.Type = ZXSpectrum.CDLType.RAM0;
                                break;
                            case 1:
                            case 2:
                            case 3:
                                result.Type = ZXSpectrum.CDLType.RAM6;
                                break;
                        }
                        break;
                    case 3:
                        switch (PagingConfiguration)
                        {
                            case 0:
                            case 2:
                            case 3:
                                result.Type = ZXSpectrum.CDLType.RAM3;
                                break;
                            case 1:
                                result.Type = ZXSpectrum.CDLType.RAM7;
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
                        switch (_ROMpaged)
                        {
                            case 0:
                                result.Type = ZXSpectrum.CDLType.ROM0;
                                break;
                            case 1:
                                result.Type = ZXSpectrum.CDLType.ROM1;
                                break;
                            case 2:
                                result.Type = ZXSpectrum.CDLType.ROM2;
                                break;
                            case 3:
                                result.Type = ZXSpectrum.CDLType.ROM3;
                                break;
                        }
                        break;

                    // RAM 0x4000 (RAM5 - Bank5 always)
                    case 1:
                        result.Type = ZXSpectrum.CDLType.RAM5;
                        break;

                    // RAM 0x8000 (RAM2 - Bank2)
                    case 2:
                        result.Type = ZXSpectrum.CDLType.RAM2;
                        break;

                    // RAM 0xc000 (any ram bank 0 - 7 may be paged in - default bank0)
                    case 3:
                        switch (RAMPaged)
                        {
                            case 0:
                                result.Type = ZXSpectrum.CDLType.RAM0;
                                break;
                            case 1:
                                result.Type = ZXSpectrum.CDLType.RAM1;
                                break;
                            case 2:
                                result.Type = ZXSpectrum.CDLType.RAM2;
                                break;
                            case 3:
                                result.Type = ZXSpectrum.CDLType.RAM3;
                                break;
                            case 4:
                                result.Type = ZXSpectrum.CDLType.RAM4;
                                break;
                            case 5:
                                result.Type = ZXSpectrum.CDLType.RAM5;
                                break;
                            case 6:
                                result.Type = ZXSpectrum.CDLType.RAM6;
                                break;
                            case 7:
                                result.Type = ZXSpectrum.CDLType.RAM7;
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
        /// Writes a byte of data to a specified memory address
        /// (with memory contention if appropriate)
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        public override void WriteMemory(ushort addr, byte value)
        {
            /*
            // update ULA screen buffer if necessary BEFORE T1 write
            if (!SpecialPagingMode)
            {
                if (((addr & 49152) == 16384 || ((addr & 0xc000) == 0xc000) && (RAMPaged == 5 || RAMPaged == 7)) && _render)
                    ULADevice.RenderScreen((int)CurrentFrameCycle);
            }
            else
            {
                switch (PagingConfiguration)
                {
                    case 2:
                    case 3:
                        if ((addr & 49152) == 16384)
                            ULADevice.RenderScreen((int)CurrentFrameCycle);
                        break;
                    case 1:
                        if ((addr & 49152) == 16384 || addr >= 0xc000)
                            ULADevice.RenderScreen((int)CurrentFrameCycle);
                        break;
                }
            }
            */
            
            WriteBus(addr, value);
        }

        /// <summary>
        /// Checks whether supplied address is in a potentially contended bank
        /// </summary>
        /// <param name="addr"></param>
        public override bool IsContended(ushort addr)
        {
            var a = addr & 0xc000;

            if (a == 0x4000)
            {
                // low port contention
                return true;
            }

            if (a == 0xc000)
            {
                // high port contention - check for contended bank paged in
                switch (RAMPaged)
                {
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns TRUE if there is a contended bank paged in
        /// </summary>
        /// <returns></returns>
        public override bool ContendedBankPaged()
        {
            switch (RAMPaged)
            {
                case 4:
                case 5:
                case 6:
                case 7:
                    return true;
            }

            return false;
        }

        /// <summary>
        /// ULA reads the memory at the specified address
        /// (No memory contention)
        /// Will read RAM5 (screen0) by default, unless RAM7 (screen1) is selected as output
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public override byte FetchScreenMemory(ushort addr)
        {
            byte value = new byte();

            if (SHADOWPaged && !PagingDisabled)
            {
                // shadow screen should be outputted
                // this lives in RAM7
                value = RAM7[addr & 0x3FFF];
            }
            else
            {
                // shadow screen is not set to display or paging is disabled (probably in 48k mode) 
                // (use screen0 at RAM5)
                value = RAM5[addr & 0x3FFF];
            }

            return value;
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
