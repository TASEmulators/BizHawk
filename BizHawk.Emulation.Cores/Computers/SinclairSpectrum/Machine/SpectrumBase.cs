using BizHawk.Common;
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

        public bool ROMPaged { get; set; }
        public bool SHADOWPaged { get; set; }
        public int RAMPaged { get; set; }
        public bool PagingDisabled { get; set; }

        /// <summary>
        /// The calling ZXSpectrum class (piped in via constructor)
        /// </summary>
        public ZXSpectrum Spectrum { get; set; }

        /// <summary>
        /// Reference to the instantiated Z80 cpu (piped in via constructor)
        /// </summary>
        public Z80A CPU { get; set; }

        /// <summary>
        /// ROM and extended info
        /// </summary>
        public RomData RomData { get; set; }

        /// <summary>
        /// The spectrum buzzer/beeper
        /// </summary>
        public Buzzer BuzzerDevice { get; set; }

        /// <summary>
        /// Device representing the AY-3-8912 chip found in the 128k and up spectrums
        /// </summary>
        public AYSound AYDevice { get; set; }

        /// <summary>
        /// The spectrum keyboard
        /// </summary>
        public virtual IKeyboard KeyboardDevice { get; set; }

        /// <summary>
        /// The spectrum datacorder device
        /// </summary>
        public virtual Tape TapeDevice { get; set; }

        /// <summary>
        /// The tape provider
        /// </summary>
        public virtual ITapeProvider TapeProvider { get; set; }

        /// <summary>
        /// Kempston joystick
        /// </summary>
        public virtual KempstonJoystick KempstonDevice { get; set; }

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
      
            }

            // we have reached the end of a frame
            LastFrameStartCPUTick = CPU.TotalExecutedCycles - OverFlow;
            LastRenderedULACycle = OverFlow;

            BuzzerDevice.EndFrame();

            TapeDevice.CPUFrameCompleted();

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

        public void SyncState(Serializer ser)
        {
            ser.BeginSection("ZXMachine");
            ser.Sync("FrameCompleted", ref FrameCompleted);
            ser.Sync("OverFlow", ref OverFlow);
            ser.Sync("FrameCount", ref FrameCount);
            ser.Sync("_frameCycles", ref _frameCycles);
            ser.Sync("LastFrameStartCPUTick", ref LastFrameStartCPUTick);
            ser.Sync("LastULAOutByte", ref LastULAOutByte);
            ser.Sync("_flashPhase", ref _flashPhase);
            ser.Sync("_frameBuffer", ref _frameBuffer, false);
            ser.Sync("_flashOffColors", ref _flashOffColors, false);
            ser.Sync("_flashOnColors", ref _flashOnColors, false);
            ser.Sync("InterruptCycle", ref InterruptCycle);
            ser.Sync("InterruptRaised", ref InterruptRaised);
            ser.Sync("InterruptRevoked", ref InterruptRevoked);
            ser.Sync("UlaFrameCycleCount", ref UlaFrameCycleCount);
            ser.Sync("FirstScreenPixelCycle", ref FirstScreenPixelCycle);
            ser.Sync("FirstDisplayPixelCycle", ref FirstDisplayPixelCycle);
            ser.Sync("FirstPixelCycleInLine", ref FirstPixelCycleInLine);
            ser.Sync("AttributeDataPrefetchTime", ref AttributeDataPrefetchTime);
            ser.Sync("PixelDataPrefetchTime", ref PixelDataPrefetchTime);
            ser.Sync("ScreenLineTime", ref ScreenLineTime);
            ser.Sync("NonVisibleBorderRightTime", ref NonVisibleBorderRightTime);
            ser.Sync("BorderRightTime", ref BorderRightTime);
            ser.Sync("DisplayLineTime", ref DisplayLineTime);
            ser.Sync("BorderLeftTime", ref BorderLeftTime);
            ser.Sync("HorizontalBlankingTime", ref HorizontalBlankingTime);
            ser.Sync("ScreenWidth", ref ScreenWidth);
            ser.Sync("BorderRightPixels", ref BorderRightPixels);
            ser.Sync("BorderLeftPixels", ref BorderLeftPixels);
            ser.Sync("FirstDisplayLine", ref FirstDisplayLine);
            ser.Sync("ScreenLines", ref ScreenLines);
            ser.Sync("NonVisibleBorderBottomLines", ref NonVisibleBorderBottomLines);
            ser.Sync("BorderBottomLines", ref BorderBottomLines);
            ser.Sync("BorderTopLines", ref BorderTopLines);
            ser.Sync("NonVisibleBorderTopLines", ref NonVisibleBorderTopLines);
            ser.Sync("VerticalSyncLines", ref VerticalSyncLines);
            ser.Sync("FlashToggleFrames", ref FlashToggleFrames);
            ser.Sync("DisplayLines", ref DisplayLines);
            ser.Sync("DisplayWidth", ref DisplayWidth);
            ser.Sync("_pixelByte1", ref _pixelByte1);
            ser.Sync("_pixelByte2", ref _pixelByte2);
            ser.Sync("_attrByte1", ref _attrByte1);
            ser.Sync("_attrByte2", ref _attrByte2);
            ser.Sync("_xPos", ref _xPos);
            ser.Sync("_yPos", ref _yPos);
            ser.Sync("DisplayWidth", ref DisplayWidth);
            ser.Sync("DisplayWidth", ref DisplayWidth);
            ser.Sync("DisplayWidth", ref DisplayWidth);
            ser.Sync("DisplayWidth", ref DisplayWidth);
            ser.Sync("_borderColour", ref _borderColour);
            ser.Sync("ROM0", ref ROM0, false);
            ser.Sync("ROM1", ref ROM1, false);
            ser.Sync("ROM2", ref ROM2, false);
            ser.Sync("ROM3", ref ROM3, false);
            ser.Sync("RAM0", ref RAM0, false);
            ser.Sync("RAM1", ref RAM1, false);
            ser.Sync("RAM2", ref RAM2, false);
            ser.Sync("RAM3", ref RAM3, false);
            ser.Sync("RAM4", ref RAM4, false);
            ser.Sync("RAM5", ref RAM5, false);
            ser.Sync("RAM6", ref RAM6, false);
            ser.Sync("RAM7", ref RAM7, false);

            RomData.SyncState(ser);
            KeyboardDevice.SyncState(ser);
            BuzzerDevice.SyncState(ser);
            TapeDevice.SyncState(ser);

            if (AYDevice != null)
                AYDevice.SyncState(ser);

            ser.EndSection();

            //ReInitMemory();
        }
    }
}
