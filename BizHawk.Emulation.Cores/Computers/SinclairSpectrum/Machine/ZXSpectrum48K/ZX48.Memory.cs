using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// 48K Memory
    /// </summary>
    public partial class ZX48 : SpectrumBase
    {
        /* 48K Spectrum has NO memory paging
         * 
         *  0xffff +--------+
                   | Bank 2 |
                   |        |
                   |        |
                   |        |
            0xc000 +--------+
                   | Bank 1 |
                   |        |
                   |        |
                   |        |
            0x8000 +--------+
                   | Bank 0 |
                   |        |
                   |        |
                   | screen |
            0x4000 +--------+
                   | ROM 0  |
                   |        |
                   |        |
                   |        |
            0x0000 +--------+
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
            var index = addr % 0x4000;

            // paging logic goes here

            switch (divisor)
            {
                case 0:
                    TestForTapeTraps(addr % 0x4000);
                    return ROM0[index];
                case 1: return RAM0[index];
                case 2: return RAM1[index];
                case 3: return RAM2[index];
                default: return 0;
            }
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
            var index = addr % 0x4000;

            // paging logic goes here

            switch (divisor)
            {
                case 0:
                    // cannot write to ROM
                    break;
                case 1:
                    //ULADevice.RenderScreen((int)CurrentFrameCycle);
                    RAM0[index] = value;
                    break;
                case 2:
                    RAM1[index] = value;                    
                    break;
                case 3:
                    RAM2[index] = value;
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
            return data;
        }

        /// <summary>
        /// Returns the ROM/RAM enum that relates to this particular memory read operation
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public override ZXSpectrum.CDLResult ReadCDL(ushort addr)
        {
            var res = new ZXSpectrum.CDLResult();

            int divisor = addr / 0x4000;
            res.Address = addr % 0x4000;

            // paging logic goes here
            switch (divisor)
            {
                case 0: res.Type = ZXSpectrum.CDLType.ROM0; break;
                case 1: res.Type = ZXSpectrum.CDLType.RAM0; break;
                case 2: res.Type = ZXSpectrum.CDLType.RAM1; break;
                case 3: res.Type = ZXSpectrum.CDLType.RAM2; break;
            }

            return res;
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
        /// Checks whether supplied address is in a potentially contended bank
        /// </summary>
        /// <param name="addr"></param>
        public override bool IsContended(ushort addr)
        {
            if ((addr & 49152) == 16384)
                return true;
            return false;
        }

        /// <summary>
        /// Returns TRUE if there is a contended bank paged in
        /// </summary>
        /// <returns></returns>
        public override bool ContendedBankPaged()
        {
            return false;
        }

        /// <summary>
        /// Sets up the ROM
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startAddress"></param>
        public override void InitROM(RomData romData)
        {
            RomData = romData;
            // for 16/48k machines only ROM0 is used (no paging)
            RomData.RomBytes?.CopyTo(ROM0, 0);
        }
    }
}
