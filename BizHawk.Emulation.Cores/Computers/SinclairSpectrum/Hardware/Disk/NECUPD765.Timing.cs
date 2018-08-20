
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Timimng
    /// </summary>
    #region Attribution
    /*
        Implementation based on the information contained here:
        http://www.cpcwiki.eu/index.php/765_FDC
        and here:
        http://www.cpcwiki.eu/imgs/f/f3/UPD765_Datasheet_OCRed.pdf
    */
    #endregion
    public partial class NECUPD765
    {
        /// <summary>
        /// The current Z80 cycle
        /// </summary>
        private long CurrentCPUCycle
        {
            get
            {
                if (_machine == null)
                    return 0;
                else
                    return _machine.CPU.TotalExecutedCycles;
            }
        }

        /// <summary>
        /// The last CPU cycle when the FDC accepted an IO read/write
        /// </summary>
        private long LastCPUCycle;

        /// <summary>
        /// The current delay figure (in Z80 t-states)
        /// This implementation only introduces delay upon main status register reads
        /// All timing calculations should be done during the other read/write operations
        /// </summary>
        private long StatusDelay;

        /// <summary>
        /// Defines the numbers of Z80 cycles per MS
        /// </summary>
        private long CPUCyclesPerMs;

        /// <summary>
        /// The floppy drive emulated clock speed
        /// </summary>
        public const double DriveClock = 31250;

        /// <summary>
        /// The number of floppy drive cycles per MS
        /// </summary>
        public long DriveCyclesPerMs;

        /// <summary>
        /// The number of T-States in one floppy drive clock tick
        /// </summary>
        public long StatesPerDriveTick;

        /// <summary>
        /// Responsible for measuring when the floppy drive is ready to run a cycle
        /// </summary>
        private long TickCounter;

        /// <summary>
        /// Internal drive cycle counter
        /// </summary>
        private int DriveCycleCounter = 1;

        /// <summary>
        /// Initializes the timing routines
        /// </summary>
        private void TimingInit()
        {
            // z80 timing
            double frameSize = _machine.ULADevice.FrameLength;
            double rRate = _machine.ULADevice.ClockSpeed / frameSize;
            long tPerSecond = (long)(frameSize * rRate);
            CPUCyclesPerMs = tPerSecond / 1000;

            // drive timing
            double dRate = DriveClock / frameSize;
            long dPerSecond = (long)(frameSize * dRate);
            DriveCyclesPerMs = dPerSecond / 1000;

            long TStatesPerDriveCycle = (long)((double)_machine.ULADevice.ClockSpeed / DriveClock);
            StatesPerDriveTick = TStatesPerDriveCycle;

        }

        /// <summary>
        /// Called by reads to the main status register
        /// Returns true if there is no delay
        /// Returns false if read is to be deferred
        /// </summary>
        /// <returns></returns>
        private bool CheckTiming()
        {
            // get delta
            long delta = CurrentCPUCycle - LastCPUCycle;

            if (StatusDelay >= delta)
            {
                // there is still delay remaining
                StatusDelay -= delta;
                LastCPUCycle = CurrentCPUCycle;
                return false;
            }
            else
            {
                // no delay remaining
                StatusDelay = 0;
                LastCPUCycle = CurrentCPUCycle;
                return true;
            }
        }
    }
}
