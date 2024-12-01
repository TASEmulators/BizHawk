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
        /// Signs that the shadow screen is now displaying
        /// Note: normal screen memory in RAM5 is not altered, the ULA just outputs Screen1 instead (RAM7)
        /// </summary>
        public bool SHADOWPaged;

        /// <summary>
        /// Index of the current RAM page
        /// /// 128k, +2/2a and +3 only
        /// </summary>
        public int RAMPaged;

        /// <summary>
        /// Signs that all paging is disabled
        /// If this is TRUE, then 128k and above machines need a hard reset before paging is allowed again
        /// </summary>
        public bool PagingDisabled;

        /// <summary>
        /// Index of the currently paged ROM
        /// 128k, +2/2a and +3 only
        /// </summary>
        protected int ROMPaged;
        public virtual int _ROMpaged
        {
            get => ROMPaged;
            set => ROMPaged = value;
        }

        /* 
         *  +3/+2A only 
         */

        /// <summary>
        /// High bit of the ROM selection (in normal paging mode)
        /// </summary>
        public bool ROMhigh = false;

        /// <summary>
        /// Low bit of the ROM selection (in normal paging mode)
        /// </summary>
       public bool ROMlow = false;

        /// <summary>
        /// Signs that the +2a/+3 special paging mode is activated
        /// </summary>
        public bool SpecialPagingMode;

        /// <summary>
        /// Index of the current special paging mode (0-3)
        /// </summary>
        public int PagingConfiguration;

        /// <summary>
        /// The last byte that was read after contended cycles
        /// </summary>
        public byte LastContendedReadByte;

        /// <summary>
        /// Simulates reading from the bus
        /// Paging should be handled here
        /// </summary>
        public abstract byte ReadBus(ushort addr);

        /// <summary>
        ///  Pushes a value onto the data bus that should be valid as long as the interrupt is true
        /// </summary>
        public virtual byte PushBus()
        {
            return 0xFF;
        }

        /// <summary>
        /// Simulates writing to the bus
        /// Paging should be handled here
        /// </summary>
        public virtual void WriteBus(ushort addr, byte value)
        {
            throw new NotImplementedException("Must be overriden");
        }

        /// <summary>
        /// Reads a byte of data from a specified memory address
        /// (with memory contention if appropriate)
        /// </summary>
        public abstract byte ReadMemory(ushort addr);

        /// <summary>
        /// Returns the ROM/RAM enum that relates to this particular memory read operation
        /// </summary>
        public abstract ZXSpectrum.CDLResult ReadCDL(ushort addr);

        /// <summary>
        /// Writes a byte of data to a specified memory address
        /// (with memory contention if appropriate)
        /// </summary>
        public abstract void WriteMemory(ushort addr, byte value);

        /// <summary>
        /// Sets up the ROM
        /// </summary>
        public abstract void InitROM(RomData romData);

        /// <summary>
        /// ULA reads the memory at the specified address
        /// (No memory contention)
        /// </summary>
        public virtual byte FetchScreenMemory(ushort addr)
        {
            var value = ReadBus((ushort)((addr & 0x3FFF) + 0x4000));
            //var value = ReadBus(addr);
            return value;
        }

        /// <summary>
        /// Checks whether supplied address is in a potentially contended bank
        /// </summary>
        public abstract bool IsContended(ushort addr);


        /// <summary>
        /// Returns TRUE if there is a contended bank paged in
        /// </summary>
        public abstract bool ContendedBankPaged();

        /// <summary>
        /// Detects whether this is a 48k machine (or a 128k in 48k mode)
        /// </summary>
        public virtual bool IsIn48kMode()
            => PagingDisabled || this is ZX48 or ZX16;

        /// <summary>
        /// Monitors ROM access
        /// Used to auto start/stop the tape device when appropriate
		/// * This isnt really used anymore for tape trap detection *
        /// </summary>
        public virtual void TestForTapeTraps(int addr)
        {
            if (TapeDevice.TapeIsPlaying)
            {
                // THE 'ERROR' RESTART
                if (addr == 8)
                {
                    //TapeDevice?.AutoStopTape();
                    return;
                }

                // THE 'ED-ERROR' SUBROUTINE
                if (addr == 4223)
                {
                    //TapeDevice?.AutoStopTape();
                    return;
                }

                // THE 'ERROR-2' ROUTINE
                if (addr == 83)
                {
                    //TapeDevice?.AutoStopTape();
                    return;
                }

                // THE 'MASKABLE INTERRUPT' ROUTINE
                // This is sometimes used when the tape is to be stopped
                // 
                if (addr == 56)
                {
                    //TapeDevice.MaskableInterruptCount++;

                    //if (TapeDevice.MaskableInterruptCount > 50)
                    //{
                        //TapeDevice.MaskableInterruptCount = 0;
                        //TapeDevice.AutoStopTape();
                    //}

                    //TapeDevice?.AutoStopTape();
                    return;
                }
            }
            else
            {
                // THE 'LD-BYTES' SUBROUTINE
                if (addr == 1366)
                {
                    //TapeDevice?.AutoStartTape();
                    return;
                }

                // THE 'LD-EDGE-2' AND 'LD-EDGE-1' SUBROUTINES
                if (addr == 1507)
                {
                    //TapeDevice?.AutoStartTape();
                    return;
                }
            }
        }
    }
}
