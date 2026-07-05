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

        // ---- Devirtualised memory access (opt-in per model) ----
        // When a model builds these maps, ReadMemoryMapped/WriteMemoryMapped index them directly — a
        // non-virtual, inlinable path — instead of the virtual ReadMemory/WriteMemory (which the net48
        // JIT cannot devirtualise through a SpectrumBase reference). Indexed by 16K region (addr >> 14);
        // a null _writeMap entry marks a read-only (ROM) region.
        // These are NOT serialized — they are derived from the (serialized) banks + paging state and
        // rebuilt via RebuildMemoryMap() at init AND after savestate load (loading may reallocate the
        // bank arrays the maps point at, which would otherwise leave the maps stale).
        protected byte[][] _readMap;
        protected byte[][] _writeMap;

        /// <summary>
        /// Per-16K-page (addr >> 14) precomputed copy of IsContended, so the per-memory-cycle contention
        /// decode in CPUMonitor reads a non-virtual array instead of calling the virtual IsContended
        /// (+ its paging switch on 128K/+2a) every cycle. Allocated once and mutated IN PLACE by
        /// RebuildPageContention() (so a cached reference stays valid) — NOT serialized, rebuilt from the
        /// (serialized) paging state exactly like _readMap/_writeMap.
        /// </summary>
        public readonly bool[] PageContended = new bool[4];

        /// <summary>
        /// Rebuilds the memory maps from the current paging state. Base default: no map, so models that
        /// don't override fall back to the virtual ReadMemory/WriteMemory (unchanged behaviour).
        /// </summary>
        public virtual void RebuildMemoryMap()
        {
            _readMap = null;
            _writeMap = null;
            RebuildPageContention();
        }

        /// <summary>
        /// Recomputes PageContended[] from the current paging state. Universal across models: it evaluates
        /// the (virtual) IsContended at each 16K page boundary, so it matches IsContended exactly for
        /// every model. Called from every RebuildMemoryMap() path (base + each override), which the
        /// existing plumbing already invokes on paging changes, resets, and savestate load.
        /// </summary>
        protected void RebuildPageContention()
        {
            PageContended[0] = IsContended(0x0000);
            PageContended[1] = IsContended(0x4000);
            PageContended[2] = IsContended(0x8000);
            PageContended[3] = IsContended(0xC000);
        }

        /// <summary>
        /// Devirtualised memory read; falls back to the virtual ReadMemory when no map is built.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public byte ReadMemoryMapped(ushort addr)
        {
            var m = _readMap;
            return m == null ? ReadMemory(addr) : m[addr >> 14][addr & 0x3FFF];
        }

        /// <summary>
        /// Devirtualised memory write; writes to read-only (null) regions are ignored; falls back when
        /// no map is built.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void WriteMemoryMapped(ushort addr, byte value)
        {
            if (EventDrivenDisplay) ScreenWriteCatchUp(addr);
            var m = _writeMap;
            if (m == null) { WriteMemory(addr, value); return; }
            var bank = m[addr >> 14];
            if (bank != null) bank[addr & 0x3FFF] = value;
        }

        /// <summary>
        /// When true, the ULA display is rendered event-driven — a catch-up render up to the current frame
        /// cycle before each screen/border change and at frame end — instead of per-T-state from
        /// CycleClock. Default false = exactly the current per-cycle behaviour (the hooks below are dormant
        /// and add only a predicted-not-taken bool test). This is both the revert switch and the A/B toggle
        /// for the event-driven-display experiment; NOT serialized (transient rendering strategy).
        /// </summary>
        public bool EventDrivenDisplay;

        /// <summary>
        /// Event-driven display hook: if addr is in the currently-displayed screen region, render up to the
        /// current frame cycle BEFORE the write lands, so the write only affects not-yet-drawn cycles (this
        /// is what preserves per-cycle display accuracy for beam-racing effects). Base impl covers the fixed
        /// lower screen 0x4000-0x5AFF (48K/16K, and 128K's normal screen mapped at 0x4000). 128K shadow
        /// screen (RAM7) and 0xC000-mapped screen writes need a model override — TODO before enabling 128K.
        /// </summary>
        protected virtual void ScreenWriteCatchUp(ushort addr)
        {
            if (_render && (uint)(addr - 0x4000) < 0x1B00)
                ULADevice.RenderScreen((int)CurrentFrameCycle);
        }

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
