using BizHawk.Emulation.Cores.Components.Z80A;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public class ZX16 : ZX48
    {
        #region Construction

        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="spectrum"></param>
        /// <param name="cpu"></param>
        public ZX16(ZXSpectrum spectrum, Z80A cpu, ZXSpectrum.BorderType borderType, byte[] file) 
            : base(spectrum, cpu, borderType, file)
        {

        }

        #endregion


        #region Memory

        /* 48K Spectrum has NO memory paging
         * 
         *  
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
            // paging logic goes here

            if (divisor > 1)
            {
                // memory does not exist
                return 0xff;
            }

            var bank = Memory[divisor];
            var index = addr % 0x4000;
            return bank[index];
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
            // paging logic goes here

            if (divisor > 1)
            {
                // memory does not exist
                return;
            }

            var bank = Memory[divisor];
            var index = addr % 0x4000;
            bank[index] = value;
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

            CPU.TotalExecutedCycles += 3;

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

            CPU.TotalExecutedCycles += 3;

            WriteBus(addr, value);
        }

        public override void ReInitMemory()
        {
            if (Memory.ContainsKey(0))
                Memory[0] = ROM0;
            else
                Memory.Add(0, ROM0);

            if (Memory.ContainsKey(1))
                Memory[1] = RAM1;
            else
                Memory.Add(1, RAM1);            
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

        #endregion
    }
}
