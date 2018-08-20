using BizHawk.Emulation.Cores.Components.Z80A;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// 16K is idential to 48K, just without the top 32KB of RAM
    /// </summary>
    public class ZX16 : ZX48
    {
        #region Construction

        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="spectrum"></param>
        /// <param name="cpu"></param>
        public ZX16(ZXSpectrum spectrum, Z80A cpu, ZXSpectrum.BorderType borderType, List<byte[]> files, List<JoystickType> joysticks) 
            : base(spectrum, cpu, borderType, files, joysticks)
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
            var index = addr % 0x4000;

            // paging logic goes here

            switch (divisor)
            {
                case 0:
                    TestForTapeTraps(addr % 0x4000);
                    return ROM0[index];
                case 1: return RAM0[index];
                default:
                    // memory does not exist
                    return 0xff;
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
