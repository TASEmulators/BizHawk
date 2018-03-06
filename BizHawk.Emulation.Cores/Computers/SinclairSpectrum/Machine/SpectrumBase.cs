using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.Z80A;
using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// The abstract class that all emulated models will inherit from
    /// * Main properties / fields / contruction*
    /// </summary>
    public abstract partial class SpectrumBase
    {
        // 128 and up only
        //protected int ROMPaged = 0;

        protected int ROMPaged;

        public int _ROMpaged
        {
            get { return ROMPaged; }
            set { ROMPaged = value; }
        }


        protected bool SHADOWPaged;
        public int RAMPaged;
        protected bool PagingDisabled;

        // +3/+2A only
        protected bool SpecialPagingMode;
        protected int PagingConfiguration;

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
        /// The emulated ULA device
        /// </summary>
        public ULABase ULADevice { get; set; }

        /// <summary>
        /// The spectrum buzzer/beeper
        /// </summary>
        public Buzzer BuzzerDevice { get; set; }

        /// <summary>
        /// Device representing the AY-3-8912 chip found in the 128k and up spectrums
        /// </summary>
        public AY38912 AYDevice { get; set; }

        /// <summary>
        /// The spectrum keyboard
        /// </summary>
        public virtual IKeyboard KeyboardDevice { get; set; }

        /// <summary>
        /// The spectrum datacorder device
        /// </summary>
        public virtual DatacorderDevice TapeDevice { get; set; }

        /// <summary>
        /// Holds the currently selected joysticks
        /// </summary>
        public virtual IJoystick[] JoystickCollection { get; set; }

        /*
        /// <summary>
        /// Joystick device 1
        /// </summary>
        public virtual IJoystick Joystick1 { get; set; }

        /// <summary>
        /// Joystick device 2
        /// </summary>
        public virtual IJoystick Joystick2 { get; set; }

        /// <summary>
        /// Joystick device 3
        /// </summary>
        public virtual IJoystick Joystick3 { get; set; }

    */

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

        protected const int AY_SAMPLE_RATE = 16;

        /// <summary>
        /// Executes a single frame
        /// </summary>
        public virtual void ExecuteFrame()
        {
            InputRead = false;

            FrameCompleted = false;
            BuzzerDevice.StartFrame();
            if (AYDevice != null)
                AYDevice.StartFrame();

            PollInput();
            
            while (CurrentFrameCycle < ULADevice.FrameLength) // UlaFrameCycleCount)
            {
                // check for interrupt
                ULADevice.CheckForInterrupt(CurrentFrameCycle);

                // run a single CPU instruction
                CPU.ExecuteOne();

                // update AY
                if (AYDevice != null)
                    AYDevice.UpdateSound(CurrentFrameCycle);
            }
            
            // we have reached the end of a frame
            LastFrameStartCPUTick = CPU.TotalExecutedCycles - OverFlow;

            // paint the buffer if needed
            if (ULADevice.needsPaint)
                ULADevice.UpdateScreenBuffer(ULADevice.FrameLength);
            
            BuzzerDevice.EndFrame();            

            //TapeDevice.CPUFrameCompleted();

            FrameCount++;

            // setup for next frame
            ULADevice.ResetInterrupt();

            TapeDevice.EndFrame();

            FrameCompleted = true;

            // is this a lag frame?
            Spectrum.IsLagFrame = !InputRead;
        }
        
        /// <summary>
        /// Hard reset of the emulated machine
        /// </summary>
        public virtual void HardReset()
        {            
            //ResetBorder();
            ULADevice.ResetInterrupt();            
        }

        /// <summary>
        /// Soft reset of the emulated machine
        /// </summary>
        public virtual void SoftReset()
        {
            //ResetBorder();
            ULADevice.ResetInterrupt();
        }

        public void SyncState(Serializer ser)
        {
            ser.BeginSection("ZXMachine");
            ser.Sync("FrameCompleted", ref FrameCompleted);
            ser.Sync("OverFlow", ref OverFlow);
            ser.Sync("FrameCount", ref FrameCount);
            ser.Sync("_frameCycles", ref _frameCycles);
            ser.Sync("inputRead", ref inputRead);
            ser.Sync("LastFrameStartCPUTick", ref LastFrameStartCPUTick);
            ser.Sync("LastULAOutByte", ref LastULAOutByte);            
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
            ser.Sync("ROMPaged", ref ROMPaged);
            ser.Sync("SHADOWPaged", ref SHADOWPaged);
            ser.Sync("RAMPaged", ref RAMPaged);
            ser.Sync("PagingDisabled", ref PagingDisabled);
            ser.Sync("SpecialPagingMode", ref SpecialPagingMode);
            ser.Sync("PagingConfiguration", ref PagingConfiguration);

            RomData.SyncState(ser);
            KeyboardDevice.SyncState(ser);
            BuzzerDevice.SyncState(ser);
            ULADevice.SyncState(ser);

            if (AYDevice != null)
                AYDevice.SyncState(ser);

            ser.Sync("tapeMediaIndex", ref tapeMediaIndex);
            TapeMediaIndex = tapeMediaIndex;

            ser.Sync("diskMediaIndex", ref diskMediaIndex);
            DiskMediaIndex = diskMediaIndex;

            TapeDevice.SyncState(ser);

            ser.EndSection();

            ReInitMemory();
        }
    }
}
