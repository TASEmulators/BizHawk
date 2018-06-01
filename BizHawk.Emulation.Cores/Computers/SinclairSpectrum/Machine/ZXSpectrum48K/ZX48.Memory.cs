using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
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
            ContendMemory(addr);
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
            // update ULA screen buffer if necessary BEFORE T1 write
            if ((addr & 49152) == 16384 && _render)
                ULADevice.RenderScreen((int)CurrentFrameCycle);

            ContendMemory(addr);                       
            WriteBus(addr, value);
        }

        /// <summary>
        /// Contends memory if necessary
        /// </summary>
        public override void ContendMemory(ushort addr)
        {
            if (IsContended(addr))
            {
                var off = 1;
                var offset = CurrentFrameCycle + off;
                if (offset < 0)
                    offset += ULADevice.FrameCycleLength;
                if (offset >= ULADevice.FrameCycleLength)
                    offset -= ULADevice.FrameCycleLength;

                var delay = ULADevice.GetContentionValue((int)offset);
                if (delay > 0)
                {

                }
                CPU.TotalExecutedCycles += delay;
            }
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
