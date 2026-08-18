using BizHawk.Common;
using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// The +3's floppy controller, backed by the shared flux subsystem: a timed uPD765 controller
	/// driving an EME-156 single-head 3" drive over a flux/cell disk model. Disk images (DSK/EDSK/IPF/HFE/
	/// SCP/FDI/raw) are converted to flux on load. The controller is timed, but the +3's INT pin is not wired
	/// to the Z80 (software polls the main status register), so it is advanced lazily on each register access
	/// using the CPU cycle count - which naturally keeps the byte cadence in step with the +3DOS poll loop.
	/// </summary>
	public sealed class Upd765DiskController : IFloppyDiskController
	{
		private SpectrumBase _machine;
		private Upd765Fdc _fdc = new();
		private Eme156Drive _drive = new();
		private long _lastCycle;
		private bool _diskLoaded;
		private DiskProtectionScheme _protection = DiskProtectionScheme.None;

		public bool FDD_FLAG_MOTOR { get; set; }

		public void Init(SpectrumBase machine)
		{
			_machine = machine;
			_fdc.Drives[0] = _drive;
			_fdc.ConfigureTiming(machine.ULADevice.ClockSpeed);
			_fdc.Reset();
			_lastCycle = Cycles;
		}

		private long Cycles => _machine?.CPU.TotalExecutedCycles ?? 0;

		// Advance the controller to "now" (the +3 polls, so this runs on every FDC register access).
		private void CatchUp()
		{
			long delta = Cycles - _lastCycle;
			_lastCycle = Cycles;
			while (delta > 0)
			{
				int step = (int)System.Math.Min(delta, 100_000);
				_fdc.Clock(step);
				delta -= step;
			}
		}

		public bool ReadPort(ushort port, ref int data)
		{
			if (port == 0x3ffd) { CatchUp(); data = _fdc.ReadData(); return true; }
			if (port == 0x2ffd) { CatchUp(); data = _fdc.ReadStatus(); return true; }
			return false;
		}

		public bool WritePort(ushort port, int data)
		{
			if (port == 0x3ffd) { CatchUp(); _fdc.WriteData((byte)data); return true; }
			if (port == 0x1ffd) { CatchUp(); FDD_FLAG_MOTOR = (data & 0x08) != 0; _drive.MotorOn = FDD_FLAG_MOTOR; return true; }
			return false;
		}

		public void FDD_LoadDisk(byte[] diskData, int side)
		{
			var flux = DiskImageLoader.ToFluxDisk(diskData);
			if (side >= 0 && flux.Sides > 1) flux = flux.ExtractSide(side); // present one side of a DS image
			_drive.Disk = flux;
			_diskLoaded = _drive.Disk != null;
			_protection = DiskProtection.Detect(_drive.Disk);
		}

		public void FDD_EjectDisk()
		{
			_drive.Disk = null;
			_diskLoaded = false;
			_protection = DiskProtectionScheme.None;
		}

		public bool FDD_IsDiskLoaded => _drive.Disk != null;
		public bool DiskInserted => _drive.Disk != null;
		public bool DriveLight => FDD_FLAG_MOTOR && _fdc.Active;

		/// <summary>
		/// The detected copy-protection scheme, for the disk status display.
		/// </summary>
		public string ProtectionName => DiskProtection.DisplayName(_protection);

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("Upd765DiskController");
			bool motor = FDD_FLAG_MOTOR;
			ser.Sync(nameof(FDD_FLAG_MOTOR), ref motor);
			FDD_FLAG_MOTOR = motor;
			ser.Sync(nameof(_lastCycle), ref _lastCycle);
			ser.Sync(nameof(_diskLoaded), ref _diskLoaded);
			_fdc.SyncState(ser);
			_drive.SyncState(ser);
			ser.EndSection();
		}
	}
}
