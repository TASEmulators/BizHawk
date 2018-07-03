using System;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// The abstract class that all emulated models will inherit from
    /// * Memory *
    /// </summary>
    public abstract partial class CPCBase
    {
        #region Memory Fields & Properties

        /// <summary>
        /// ROM Banks
        /// </summary>        
        public byte[] ROM0 = new byte[0x4000];
        public byte[] ROM1 = new byte[0x4000];

        /// <summary>
        /// RAM Banks
        /// </summary>
        public byte[] RAM0 = new byte[0x4000];  // Bank 0
        public byte[] RAM1 = new byte[0x4000];  // Bank 1
        public byte[] RAM2 = new byte[0x4000];  // Bank 2
        public byte[] RAM3 = new byte[0x4000];  // Bank 3

        /// <summary>
        /// Signs whether Upper ROM is paged in
        /// </summary>
        public bool UpperROMPaged;

        /// <summary>
        /// Signs whether Lower ROM is paged in
        /// </summary>
        public bool LowerROMPaged;

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
        public abstract void InitROM(RomData romData);

        /// <summary>
        /// ULA reads the memory at the specified address
        /// (No memory contention)
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public virtual byte FetchScreenMemory(ushort addr)
        {
            var value = ReadBus((ushort)((addr & 0x3FFF) + 0x4000));
            //var value = ReadBus(addr);
            return value;
        }

        #endregion
    }
}
