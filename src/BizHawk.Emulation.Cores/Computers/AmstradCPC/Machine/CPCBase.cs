using BizHawk.Common;
//using BizHawk.Emulation.Cores.Components.Z80A;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// The abstract class that all emulated models will inherit from
	/// * Main properties / fields / contruction*
	/// </summary>
	public abstract partial class CPCBase
	{
		/// <summary>
		/// The calling AmstradCPC class (piped in via constructor)
		/// </summary>
		public AmstradCPC CPC { get; set; }

		/*
		/// <summary>
		/// Reference to the instantiated Z80 cpu (piped in via constructor)
		/// </summary>
		public Z80A<AmstradCPC.CpuLink> CPU { get; set; }
		*/

		public LibFz80Wrapper CPU { get; set; }

		/// <summary>
		/// ROM and extended info
		/// </summary>
		public RomData RomData { get; set; }

		/// <summary>
		/// The Amstrad datacorder device
		/// </summary>
		public virtual DatacorderDevice TapeDevice { get; set; }

		/// <summary>
		/// beeper output for the tape
		/// </summary>
		public IBeeperDevice TapeBuzzer { get; set; }

		/// <summary>
		/// Device representing the AY-3-8912 chip found in the CPC
		/// </summary>
		public IPSG AYDevice { get; set; }

		/// <summary>
		/// The keyboard device
		/// Technically, this is controlled by the PSG, but has been abstracted due to the port over from ZXHawk
		/// </summary>
		public IKeyboard KeyboardDevice { get; set; }

		/// <summary>
		/// The Amstrad disk drive
		/// </summary>
		public virtual NECUPD765 UPDDiskDevice { get; set; }

		/// <summary>
		/// The Cathode Ray Tube Controller chip
		/// </summary>
		public CRTC CRTC { get; set; }

		/// <summary>
		/// The Amstrad gate array
		/// </summary>
		public GateArray GateArray { get; set; }

		/// <summary>
		/// The CRT screen
		/// </summary>
		public CRTScreen CRTScreen { get; set; }

		/// <summary>
		/// The PPI contoller chip
		/// </summary>
		public PPI_8255 PPI { get; set; }

		/// <summary>
		/// PAL16L8 Programmable Logic Array Circuit
		/// </summary>
		public PAL16L8 PAL { get; set; }

		/// <summary>
		/// The length of a standard frame in CPU cycles
		/// </summary>
		public int FrameLength;

		/// <summary>
		/// Signs whether the frame has ended
		/// </summary>
		public bool FrameCompleted;

		/// <summary>
		/// The total number of frames rendered
		/// </summary>
		public int FrameCount;

		/// <summary>
		/// The current cycle (T-State) that we are at in the frame
		/// </summary>
		public long _frameCycles;

		/// <summary>
		/// Stores where we are in the frame after each CPU cycle
		/// </summary>
		public long LastFrameStartCPUTick;

		/// <summary>
		/// Gets the current frame cycle according to the CPU tick count
		/// </summary>
		public virtual long CurrentFrameCycle => GateArray.GAClockCounter; // GateArray.FrameClock; // CPU.TotalExecutedCycles - LastFrameStartCPUTick;

		/// <summary>
		/// Non-Deterministic bools
		/// </summary>
		public bool _render;
		public bool _renderSound;

		/// <summary>
		/// Mask constants &amp; misc
		/// </summary>
		protected const int BORDER_BIT = 0x07;
		protected const int EAR_BIT = 0x10;
		protected const int MIC_BIT = 0x08;
		protected const int TAPE_BIT = 0x40;
		protected const int AY_SAMPLE_RATE = 16;

		/// <summary>
		/// Executes a single frame
		/// </summary>
		public virtual void ExecuteFrame(bool render, bool renderSound)
		{
			InputRead = false;
			_render = render;
			_renderSound = renderSound;

			FrameCompleted = false;

			if (UPDDiskDevice == null || !UPDDiskDevice.FDD_IsDiskLoaded)
				TapeDevice.StartFrame();

			if (_renderSound)
			{
				AYDevice.StartFrame();
			}

			PollInput();

			GateArray.GAClockCounter = 0;
			GateArray.FrameEnd = false;

			while (!GateArray.FrameEnd)
			{
				GateArray.Clock();

				// cycle the tape device
				if (UPDDiskDevice == null || !UPDDiskDevice.FDD_IsDiskLoaded)
					TapeDevice.TapeCycle();
			}

			GateArray.FrameEnd = false;

			var ipf = GateArray.interruptsPerFrame;
			GateArray.interruptsPerFrame = 0;
			double nops = GateArray.LastGAFrameClocks / 16.0;

			// we have reached the end of a frame
			LastFrameStartCPUTick = CPU.TotalExecutedCycles; // - OverFlow;

			AYDevice?.EndFrame();

			FrameCount++;

			if (UPDDiskDevice == null || !UPDDiskDevice.FDD_IsDiskLoaded)
				TapeDevice.EndFrame();

			FrameCompleted = true;

			// is this a lag frame?
			CPC.IsLagFrame = !InputRead;

			// FDC debug
			/*
			if (UPDDiskDevice != null && UPDDiskDevice.writeDebug)
			{
				// only write UPD log every second
				if (FrameCount % 10 == 0)
				{
					System.IO.File.AppendAllLines(UPDDiskDevice.outputfile, UPDDiskDevice.dLog);
					UPDDiskDevice.dLog = new System.Collections.Generic.List<string>();
					//System.IO.File.WriteAllText(UPDDiskDevice.outputfile, UPDDiskDevice.outputString);
				}
			}
			*/

			// setup GA for next frame
			GateArray.GAClockCounter = 0;
		}

		/// <summary>
		/// Hard reset of the emulated machine
		/// </summary>
		public virtual void HardReset()
		{
			/*
            //ULADevice.ResetInterrupt();
            ROMPaged = 0;
            SpecialPagingMode = false;
            RAMPaged = 0;
            CPU.RegPC = 0;

            Spectrum.SetCpuRegister("SP", 0xFFFF);
            Spectrum.SetCpuRegister("IY", 0xFFFF);
            Spectrum.SetCpuRegister("IX", 0xFFFF);
            Spectrum.SetCpuRegister("AF", 0xFFFF);
            Spectrum.SetCpuRegister("BC", 0xFFFF);
            Spectrum.SetCpuRegister("DE", 0xFFFF);
            Spectrum.SetCpuRegister("HL", 0xFFFF);
            Spectrum.SetCpuRegister("SP", 0xFFFF);
            Spectrum.SetCpuRegister("Shadow AF", 0xFFFF);
            Spectrum.SetCpuRegister("Shadow BC", 0xFFFF);
            Spectrum.SetCpuRegister("Shadow DE", 0xFFFF);
            Spectrum.SetCpuRegister("Shadow HL", 0xFFFF);

            CPU.Regs[CPU.I] = 0;
            CPU.Regs[CPU.R] = 0;

            TapeDevice.Reset();
            if (AYDevice != null)
                AYDevice.Reset();

            byte[][] rams = new byte[][]
            {
                RAM0,
                RAM1,
                RAM2,
                RAM3,
                RAM4,
                RAM5,
                RAM6,
                RAM7
            };

            foreach (var r in rams)
            {
                for (int i = 0; i < r.Length; i++)
                {
                    r[i] = 0x00;
                }
            }
            */
		}

		/// <summary>
		/// Soft reset of the emulated machine
		/// </summary>
		public virtual void SoftReset()
		{
			/*
            //ULADevice.ResetInterrupt();
            ROMPaged = 0;
            SpecialPagingMode = false;
            RAMPaged = 0;
            CPU.RegPC = 0;

            Spectrum.SetCpuRegister("SP", 0xFFFF);
            Spectrum.SetCpuRegister("IY", 0xFFFF);
            Spectrum.SetCpuRegister("IX", 0xFFFF);
            Spectrum.SetCpuRegister("AF", 0xFFFF);
            Spectrum.SetCpuRegister("BC", 0xFFFF);
            Spectrum.SetCpuRegister("DE", 0xFFFF);
            Spectrum.SetCpuRegister("HL", 0xFFFF);
            Spectrum.SetCpuRegister("SP", 0xFFFF);
            Spectrum.SetCpuRegister("Shadow AF", 0xFFFF);
            Spectrum.SetCpuRegister("Shadow BC", 0xFFFF);
            Spectrum.SetCpuRegister("Shadow DE", 0xFFFF);
            Spectrum.SetCpuRegister("Shadow HL", 0xFFFF);

            CPU.Regs[CPU.I] = 0;
            CPU.Regs[CPU.R] = 0;

            TapeDevice.Reset();
            if (AYDevice != null)
                AYDevice.Reset();

            byte[][] rams = new byte[][]
            {
                RAM0,
                RAM1,
                RAM2,
                RAM3,
                RAM4,
                RAM5,
                RAM6,
                RAM7
            };

            foreach (var r in rams)
            {
                for (int i = 0; i < r.Length; i++)
                {
                    r[i] = 0x00;
                }
            }
            */
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("CPCMachine");
			ser.Sync(nameof(FrameCompleted), ref FrameCompleted);
			ser.Sync(nameof(FrameCount), ref FrameCount);
			ser.Sync(nameof(_frameCycles), ref _frameCycles);
			ser.Sync(nameof(inputRead), ref inputRead);
			ser.Sync(nameof(LastFrameStartCPUTick), ref LastFrameStartCPUTick);
			ser.Sync(nameof(ROMLower), ref ROMLower, false);
			ser.Sync(nameof(ROM0), ref ROM0, false);
			ser.Sync(nameof(ROM7), ref ROM7, false);
			ser.Sync(nameof(RAM0), ref RAM0, false);
			ser.Sync(nameof(RAM1), ref RAM1, false);
			ser.Sync(nameof(RAM2), ref RAM2, false);
			ser.Sync(nameof(RAM3), ref RAM3, false);
			ser.Sync(nameof(RAM4), ref RAM4, false);
			ser.Sync(nameof(RAM5), ref RAM5, false);
			ser.Sync(nameof(RAM6), ref RAM6, false);
			ser.Sync(nameof(RAM7), ref RAM7, false);

			ser.Sync(nameof(UpperROMPosition), ref UpperROMPosition);
			ser.Sync(nameof(UpperROMPaged), ref UpperROMPaged);
			ser.Sync(nameof(LowerROMPaged), ref LowerROMPaged);

			CRTC.SyncState(ser);
			GateArray.SyncState(ser);
			PPI.SyncState(ser);
			PAL.SyncState(ser);
			KeyboardDevice.SyncState(ser);
			TapeBuzzer.SyncState(ser);
			AYDevice.SyncState(ser);

			ser.Sync(nameof(tapeMediaIndex), ref tapeMediaIndex);
			if (ser.IsReader)
				TapeMediaIndex = tapeMediaIndex;

			TapeDevice.SyncState(ser);

			ser.Sync(nameof(diskMediaIndex), ref diskMediaIndex);
			if (ser.IsReader)
				DiskMediaIndex = diskMediaIndex;

			UPDDiskDevice?.SyncState(ser);

			ser.EndSection();
		}
	}
}
