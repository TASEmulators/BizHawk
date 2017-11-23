using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.Z80A;
using System;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// The abstract class that all emulated models will inherit from
    /// * Main properties / fields / contruction*
    /// </summary>
    public abstract partial class SpectrumBase
    {    
        /// <summary>
        /// The calling ZXSpectrum class (piped in via constructor)
        /// </summary>
        protected ZXSpectrum Spectrum { get; set; }

        /// <summary>
        /// Reference to the instantiated Z80 cpu (piped in via constructor)
        /// </summary>
        protected Z80A CPU { get; set; }

        /// <summary>
        /// The spectrum buzzer/beeper
        /// </summary>
        public Buzzer BuzzerDevice { get; set; }

        /// <summary>
        /// The spectrum keyboard
        /// </summary>
        public virtual IKeyboard KeyboardDevice { get; set; }

        /// <summary>
        /// The spectrum datacorder device
        /// </summary>
        public virtual Tape TapeDevice { get; set; }

        /// <summary>
        /// Signs whether the frame has ended
        /// </summary>
        public bool FrameCompleted;

        /// <summary>
        /// Overflow from the previous frame (in Z80 cycles)
        /// </summary>
        public int OverFlow;

        /// <summary>
        /// The total number of frames rendered
        /// </summary>
        public int FrameCount;

        /// <summary>
        /// The current cycle (T-State) that we are at in the frame
        /// </summary>
        public int _frameCycles;

        /// <summary>
        /// Stores where we are in the frame after each CPU cycle
        /// </summary>
        public int LastFrameStartCPUTick;

        /// <summary>
        /// Gets the current frame cycle according to the CPU tick count
        /// </summary>
        public virtual int CurrentFrameCycle => CPU.TotalExecutedCycles - LastFrameStartCPUTick;

        /// <summary>
        /// Mask constants
        /// </summary>
        protected const int BORDER_BIT = 0x07;
        protected const int EAR_BIT = 0x10;
        protected const int MIC_BIT = 0x08;
        protected const int TAPE_BIT = 0x40;

        /// <summary>
        /// Executes a single frame
        /// </summary>
        public virtual void ExecuteFrame()
        {
            FrameCompleted = false;
            BuzzerDevice.StartFrame();

            PollInput();

            while (CurrentFrameCycle <= UlaFrameCycleCount)
            {
                // check for interrupt
                CheckForInterrupt(CurrentFrameCycle);

                // run a single CPU instruction
                CPU.ExecuteOne();

                // run a rendering cycle according to the current CPU cycle count
                var lastCycle = CurrentFrameCycle;
                RenderScreen(LastRenderedULACycle + 1, lastCycle);
                LastRenderedULACycle = lastCycle;

                // test
                if (CPU.IFF1)
                {
                    //Random rnd = new Random();
                    //ushort rU = (ushort)rnd.Next(0x4000, 0x8000);
                    //PokeMemory(rU, (byte)rnd.Next(7));
                }                
            }

            // we have reached the end of a frame
            LastFrameStartCPUTick = CPU.TotalExecutedCycles - OverFlow;
            LastRenderedULACycle = OverFlow;

            BuzzerDevice.EndFrame();

            

            FrameCount++;

            // setup for next frame
            OverFlow = CurrentFrameCycle % UlaFrameCycleCount;
            ResetInterrupt();
            FrameCompleted = true;

            if (FrameCount % FlashToggleFrames == 0)
            {
                _flashPhase = !_flashPhase;
            }

            RenderScreen(0, OverFlow);
        }

        /// <summary>
        /// Executes one cycle of the emulated machine
        /// </summary>
        public virtual void ExecuteCycle()
        {  
            // check for interrupt
            CheckForInterrupt(CurrentFrameCycle);

            // run a single CPU instruction
            CPU.ExecuteOne();

            // run a rendering cycle according to the current CPU cycle count
            var lastCycle = CurrentFrameCycle;
            RenderScreen(LastRenderedULACycle + 1, lastCycle);

            // has the frame completed?
            FrameCompleted = CurrentFrameCycle >= UlaFrameCycleCount;

            if (CurrentFrameCycle > 50000)
            {

            }
        }
        
        /// <summary>
        /// Hard reset of the emulated machine
        /// </summary>
        public virtual void HardReset()
        {            
            ResetBorder();
            ResetInterrupt();
            
        }

        /// <summary>
        /// Soft reset of the emulated machine
        /// </summary>
        public virtual void SoftReset()
        {
            ResetBorder();
            ResetInterrupt();
        }
    }
}
