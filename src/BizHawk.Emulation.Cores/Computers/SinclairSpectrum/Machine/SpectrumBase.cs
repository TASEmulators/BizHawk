using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.Z80A;
using BizHawk.Emulation.Cores.Sound;

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
		public ZXSpectrum Spectrum { get; set; }

		/// <summary>
		/// Reference to the instantiated Z80 cpu (piped in via constructor)
		/// </summary>
		public Z80A<ZXSpectrum.CpuLink> CPU { get; set; }

		/// <summary>
		/// ROM and extended info
		/// </summary>
		public RomData RomData { get; set; }

		/// <summary>
		/// The emulated ULA device
		/// </summary>
		//public ULABase ULADevice { get; set; }
		public ULA ULADevice { get; set; }

		/// <summary>
		/// Monitors CPU cycles
		/// </summary>
		public CPUMonitor CPUMon { get; set; }

		/// <summary>
		/// The spectrum buzzer/beeper
		/// </summary>
		public OneBitBeeper BuzzerDevice { get; set; }

		/// <summary>
		/// A second beeper for the tape
		/// </summary>
		public OneBitBeeper TapeBuzzer { get; set; }

		/// <summary>
		/// Device representing the AY-3-8912 chip found in the 128k and up spectrums
		/// </summary>
		public IPSG AYDevice { get; set; }

		/// <summary>
		/// The spectrum keyboard
		/// </summary>
		public virtual IKeyboard KeyboardDevice { get; set; }

		/// <summary>
		/// The spectrum datacorder device
		/// </summary>
		public virtual DatacorderDevice TapeDevice { get; set; }

		/// <summary>
		/// The +3 built-in disk drive
		/// </summary>
		public virtual NECUPD765 UPDDiskDevice { get; set; }

		/// <summary>
		/// Holds the currently selected joysticks
		/// </summary>
		public virtual IJoystick[] JoystickCollection { get; set; }

		/// <summary>
		/// +3/2a printer port strobe
		/// </summary>
		protected bool PrinterPortStrobe;

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
		public long _frameCycles;

		/// <summary>
		/// Stores where we are in the frame after each CPU cycle
		/// </summary>
		public long LastFrameStartCPUTick;

		/// <summary>
		/// Gets the current frame cycle according to the CPU tick count
		/// </summary>
		public virtual long CurrentFrameCycle => CPU.TotalExecutedCycles - LastFrameStartCPUTick;

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
			ULADevice.FrameEnd = false;
			ULADevice.ULACycleCounter = CurrentFrameCycle;

			InputRead = false;
			_render = render;
			_renderSound = renderSound;

			FrameCompleted = false;

			//if (UPDDiskDevice == null || !UPDDiskDevice.FDD_IsDiskLoaded)
			//TapeDevice.StartFrame();

			if (_renderSound)
			{
				AYDevice?.StartFrame();
			}

			PollInput();

			for (; ; )
			{
				// run the CPU Monitor cycle
				CPUMon.ExecuteCycle();

				// clock the beepers
				TapeBuzzer.SetClock((int)CurrentFrameCycle);
				BuzzerDevice.SetClock((int)CurrentFrameCycle);

				// cycle the tape device
				if (UPDDiskDevice == null || !UPDDiskDevice.FDD_IsDiskLoaded)
					TapeDevice.TapeCycle();

				// has frame end been reached?
				if (ULADevice.FrameEnd)
					break;
			}

			OverFlow = (int)CurrentFrameCycle - ULADevice.FrameLength;

			// we have reached the end of a frame
			LastFrameStartCPUTick = CPU.TotalExecutedCycles - OverFlow;

			ULADevice.LastTState = 0;

			AYDevice?.EndFrame();

			FrameCount++;

			if (UPDDiskDevice == null || !UPDDiskDevice.FDD_IsDiskLoaded)
				TapeDevice.EndFrame();

			FrameCompleted = true;

			// is this a lag frame?
			Spectrum.IsLagFrame = !InputRead;

			// FDC debug            
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
		}

		/// <summary>
		/// Hard reset of the emulated machine
		/// </summary>
		public virtual void HardReset()
		{
			//ULADevice.ResetInterrupt();
			ROMPaged = 0;
			SpecialPagingMode = false;
			PagingDisabled = false;
			RAMPaged = 0;
			CPU.RegPC = 0;
			
			Spectrum.SetCpuRegister("SP", 0xFFFF);
			Spectrum.SetCpuRegister("IY", 0);
			Spectrum.SetCpuRegister("IX", 0);
			Spectrum.SetCpuRegister("AF", 0xFFFF);
			Spectrum.SetCpuRegister("BC", 0);
			Spectrum.SetCpuRegister("DE", 0);
			Spectrum.SetCpuRegister("HL", 0);
			Spectrum.SetCpuRegister("SP", 0xFFFF);
			Spectrum.SetCpuRegister("Shadow AF", 0xFFFF);
			Spectrum.SetCpuRegister("Shadow BC", 0);
			Spectrum.SetCpuRegister("Shadow DE", 0);
			Spectrum.SetCpuRegister("Shadow HL", 0);

			CPU.Regs[CPU.I] = 0;
			CPU.Regs[CPU.R] = 0;

			TapeDevice.Reset();
			AYDevice?.Reset();

			byte[][] rams = 
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
		}

		/// <summary>
		/// Soft reset of the emulated machine
		/// </summary>
		public virtual void SoftReset()
		{
			//ULADevice.ResetInterrupt();
			ROMPaged = 0;
			SpecialPagingMode = false;
			PagingDisabled = false;
			RAMPaged = 0;
			CPU.RegPC = 0;

			Spectrum.SetCpuRegister("SP", 0xFFFF);
			//Spectrum.SetCpuRegister("IY", 0xFFFF);
			//Spectrum.SetCpuRegister("IX", 0xFFFF);
			Spectrum.SetCpuRegister("AF", 0xFFFF);
			//Spectrum.SetCpuRegister("BC", 0xFFFF);
			//Spectrum.SetCpuRegister("DE", 0xFFFF);
			//Spectrum.SetCpuRegister("HL", 0xFFFF);
			Spectrum.SetCpuRegister("Shadow AF", 0xFFFF);
			//Spectrum.SetCpuRegister("Shadow BC", 0xFFFF);
			//Spectrum.SetCpuRegister("Shadow DE", 0xFFFF);
			//Spectrum.SetCpuRegister("Shadow HL", 0xFFFF);

			CPU.Regs[CPU.I] = 0;
			CPU.Regs[CPU.R] = 0;

			TapeDevice.Reset();
			AYDevice?.Reset();

			byte[][] rams =
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
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("ZXMachine");
			ser.Sync(nameof(FrameCompleted), ref FrameCompleted);
			ser.Sync(nameof(OverFlow), ref OverFlow);
			ser.Sync(nameof(FrameCount), ref FrameCount);
			ser.Sync(nameof(_frameCycles), ref _frameCycles);
			ser.Sync(nameof(inputRead), ref inputRead);
			ser.Sync(nameof(LastFrameStartCPUTick), ref LastFrameStartCPUTick);
			ser.Sync(nameof(LastULAOutByte), ref LastULAOutByte);
			ser.Sync(nameof(ROM0), ref ROM0, false);
			ser.Sync(nameof(ROM1), ref ROM1, false);
			ser.Sync(nameof(ROM2), ref ROM2, false);
			ser.Sync(nameof(ROM3), ref ROM3, false);
			ser.Sync(nameof(RAM0), ref RAM0, false);
			ser.Sync(nameof(RAM1), ref RAM1, false);
			ser.Sync(nameof(RAM2), ref RAM2, false);
			ser.Sync(nameof(RAM3), ref RAM3, false);
			ser.Sync(nameof(RAM4), ref RAM4, false);
			ser.Sync(nameof(RAM5), ref RAM5, false);
			ser.Sync(nameof(RAM6), ref RAM6, false);
			ser.Sync(nameof(RAM7), ref RAM7, false);
			ser.Sync(nameof(ROMPaged), ref ROMPaged);
			ser.Sync(nameof(SHADOWPaged), ref SHADOWPaged);
			ser.Sync(nameof(RAMPaged), ref RAMPaged);
			ser.Sync(nameof(PagingDisabled), ref PagingDisabled);
			ser.Sync(nameof(SpecialPagingMode), ref SpecialPagingMode);
			ser.Sync(nameof(PagingConfiguration), ref PagingConfiguration);
			ser.Sync(nameof(ROMhigh), ref ROMhigh);
			ser.Sync(nameof(ROMlow), ref ROMlow);
			ser.Sync(nameof(LastContendedReadByte), ref LastContendedReadByte);

			KeyboardDevice.SyncState(ser);
			BuzzerDevice.SyncState(ser);
			TapeBuzzer.SyncState(ser);
			ULADevice.SyncState(ser);
			CPUMon.SyncState(ser);

			if (AYDevice != null)
			{
				AYDevice.SyncState(ser);
				((AY38912)AYDevice).PanningConfiguration = Spectrum.Settings.AYPanConfig;
			}

			ser.Sync(nameof(tapeMediaIndex), ref tapeMediaIndex);
			if (ser.IsReader)
			{
				IsLoadState = true;
				TapeMediaIndex = tapeMediaIndex;
				IsLoadState = false;
			}

			TapeDevice.SyncState(ser);

			ser.Sync(nameof(diskMediaIndex), ref diskMediaIndex);
			if (ser.IsReader)
			{
				IsLoadState = true;
				DiskMediaIndex = diskMediaIndex;
				IsLoadState = false;
			}

			UPDDiskDevice?.SyncState(ser);

			ser.EndSection();
		}
	}
}
