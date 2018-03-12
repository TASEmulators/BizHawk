using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// The abstract class that all emulated models will inherit from
    /// * Memory *
    /// </summary>
    public abstract partial class SpectrumBase
    {
        /// <summary>
        /// ROM Banks
        /// </summary>        
        public byte[] ROM0 = new byte[0x4000];
        public byte[] ROM1 = new byte[0x4000];
        public byte[] ROM2 = new byte[0x4000];
        public byte[] ROM3 = new byte[0x4000];
        
        /// <summary>
        /// RAM Banks
        /// </summary>
        public byte[] RAM0 = new byte[0x4000];  // Bank 0
        public byte[] RAM1 = new byte[0x4000];  // Bank 1
        public byte[] RAM2 = new byte[0x4000];  // Bank 2
        public byte[] RAM3 = new byte[0x4000];  // Bank 3
        public byte[] RAM4 = new byte[0x4000];  // Bank 4
        public byte[] RAM5 = new byte[0x4000];  // Bank 5
        public byte[] RAM6 = new byte[0x4000];  // Bank 6
        public byte[] RAM7 = new byte[0x4000];  // Bank 7

        /// <summary>
        /// Represents the addressable memory space of the spectrum
        /// All banks for the emulated system should be added during initialisation
        /// </summary>
        public Dictionary<int, byte[]> Memory = new Dictionary<int, byte[]>();

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
        public virtual void WriteBus(ushort addr, byte value)
        {
            throw new NotImplementedException("Must be overriden");
        }

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
            return value;
        }

        /// <summary>
        /// Detects whether this is a 48k machine (or a 128k in 48k mode)
        /// </summary>
        /// <returns></returns>
        public virtual bool IsIn48kMode()
        {
            if (this.GetType() == typeof(ZX48) ||
                this.GetType() == typeof(ZX16) ||
                PagingDisabled)
            {
                return true;
            }
            else
                return false;
        }
        
    }
}
