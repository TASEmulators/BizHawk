using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// The abstract class that all emulated models will inherit from
    /// * Memory *
    /// </summary>
    public abstract partial class CPCBase
    {
        #region Memory Fields & Properties

        /* ROM Banks */
        /// <summary>
        /// Lower: OS ROM
        /// </summary>
        public byte[] ROMLower = new byte[0x4000];
        /// <summary>
        /// Upper: POS 0 (usually BASIC)
        /// </summary>
        public byte[] ROM0 = new byte[0x4000];
        /// <summary>
        /// Upper: POS 7 (usually AMSDOS)
        /// </summary>
        public byte[] ROM7 = new byte[0x4000];

        /* RAM Banks - Lower 64K */
        public byte[] RAM0 = new byte[0x4000];
        public byte[] RAM1 = new byte[0x4000];
        public byte[] RAM2 = new byte[0x4000];
        public byte[] RAM3 = new byte[0x4000];

        /* RAM Banks - Upper 64K */
        public byte[] RAM4 = new byte[0x4000];
        public byte[] RAM5 = new byte[0x4000];
        public byte[] RAM6 = new byte[0x4000];
        public byte[] RAM7 = new byte[0x4000];

        /// <summary>
        /// Signs whether Upper ROM is paged in
        /// </summary>
        public bool UpperROMPaged;

        /// <summary>
        /// The position of the currently paged upper ROM
        /// </summary>
        public int UpperROMPosition;

        /// <summary>
        /// Signs whether Lower ROM is paged in
        /// </summary>
        public bool LowerROMPaged;

        /// <summary>
        /// The currently selected RAM config
        /// </summary>
        public int RAMConfig;

        /// <summary>
        /// Always 0 on a CPC6128
        /// On a machine with more than 128K RAM (standard memory expansion) this selects each additional 64K above the first upper 64K
        /// </summary>
        public int RAM64KBank;

        #endregion

        #region Memory Related Methods

        /// <summary>
        /// Simulates reading from the bus
        /// Paging should be handled here
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public abstract byte ReadBus(ushort addr);

        /// <summary>
        ///  Pushes a value onto the data bus that should be valid as long as the interrupt is true
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public virtual byte PushBus()
        {
            return 0xFF;
        }

        /// <summary>
        /// Simulates writing to the bus
        /// Paging should be handled here
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        public abstract void WriteBus(ushort addr, byte value);

        /// <summary>
        /// Reads a byte of data from a specified memory address
        /// (with memory contention if appropriate)
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public abstract byte ReadMemory(ushort addr);

        /// <summary>
        /// Writes a byte of data to a specified memory address
        /// (with memory contention if appropriate)
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        public abstract void WriteMemory(ushort addr, byte value);

        /// <summary>
        /// Sets up the ROM
        /// </summary>
        /// <param name="buffer"></param>
        public abstract void InitROM(RomData[] romData);

        /// <summary>
        /// ULA reads the memory at the specified address
        /// (No memory contention)
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public virtual byte FetchScreenMemory(ushort addr)
        {
            int divisor = addr / 0x4000;
            byte result = 0xff;

            switch (divisor)
            {
                // 0x000
                case 0:
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
                    result = RAM3[addr % 0x4000];
                    break;
                default:
                    break;
            }

            return result;
        }

        #endregion
    }
}
