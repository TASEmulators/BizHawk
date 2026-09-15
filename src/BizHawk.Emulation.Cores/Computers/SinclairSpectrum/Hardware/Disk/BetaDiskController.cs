using BizHawk.Common;
using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// The Beta 128 disk interface built into the Pentagon (and Scorpion), backed by the shared flux
	/// subsystem: a timed WD1793 controller driving a double-sided 80-track DD drive over a flux/cell disk
	/// model. TR-DOS .TRD images are converted to flux on load. Like the +3's uPD765 the WD1793's INTRQ is
	/// not wired to the Z80 (TR-DOS polls the system port for INTRQ/DRQ), so the controller is advanced
	/// lazily on each port access from the CPU cycle count, keeping the byte cadence in step with the poll.
	/// Port decode (partial): the interface only responds when A0-A4 are all high (low five bits = 0x1F),
	/// which is what separates it from the ULA at 0xFE. A7 then selects the system latch (0xFF) from the
	/// WD register file, and A6/A5 select the register: 0x1F command/status, 0x3F track, 0x5F sector,
	/// 0x7F data. The whole interface is gated by the caller on TR-DOS being paged in.
	/// </summary>
	public sealed class BetaDiskController : IFloppyDiskController
	{
		/// <summary>
		/// The physical drive the Beta 128 is fitted with. The Pentagon shipped with both 5.25" and 3.5"
		/// double-density drives. Both spin at 300 RPM (so the index period and 250 kbit/s data rate - the
		/// timings the WD1793 and copy protection actually depend on - are identical), and seek timing is
		/// dominated by the WD1793's programmed step rate rather than the drive mechanics; the drive type
		/// therefore only tweaks the secondary spin-up and track-to-track/settle floors. Both are modelled as
		/// 80-cylinder double-sided so they can read any TR-DOS geometry (40-track disks included).
		/// </summary>
		public enum DriveKind { ThreeHalfInch, FiveQuarterInch }

		public DriveKind DriveType { get; set; } = DriveKind.ThreeHalfInch;

		private static FloppyDriveProfile Profile(DriveKind kind) => kind == DriveKind.FiveQuarterInch
			? new FloppyDriveProfile { Cylinders = 80, Sides = 2, Rpm = 300, SpinUpMs = 750, TrackToTrackMs = 6, SettleMs = 15 }
			: new FloppyDriveProfile { Cylinders = 80, Sides = 2, Rpm = 300, SpinUpMs = 400, TrackToTrackMs = 3, SettleMs = 15 };

		private SpectrumBase _machine;
		private readonly Wd1793Fdc _fdc = new();
		private FloppyDrive _drive;
		private long _lastCycle;
		private bool _diskLoaded;
		private byte _system; // last value written to the 0xFF system latch

		public bool FDD_FLAG_MOTOR { get; set; }

		public void Init(SpectrumBase machine)
		{
			_machine = machine;
			_drive = new FloppyDrive(Profile(DriveType));
			_fdc.Drives[0] = _drive;
			_fdc.ConfigureTiming(machine.ULADevice.ClockSpeed);
			_fdc.Reset();
			_lastCycle = Cycles;
		}

		private long Cycles => _machine?.CPU.TotalExecutedCycles ?? 0;

		// Advance the controller to "now" (TR-DOS polls, so this runs on every Beta port access).
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

		// Returns true and sets 'system'/'reg' when the port addresses the Beta interface.
		private static bool Decode(ushort port, out bool system, out int reg)
		{
			system = false; reg = 0;
			byte lb = (byte)(port & 0xFF);
			if ((lb & 0x1F) != 0x1F) return false; // A0-A4 must all be high
			if ((lb & 0x80) != 0) { system = true; return true; } // A7 = system latch (0xFF)
			reg = (lb >> 5) & 0x03; // A6,A5 = register select
			return true;
		}

		public bool ReadPort(ushort port, ref int result)
		{
			if (!Decode(port, out bool system, out int reg)) return false;
			CatchUp();
			if (system)
			{
				// system-port read: INTRQ on bit 7, DRQ on bit 6 (what TR-DOS polls); other bits pulled high
				result = (_fdc.IntRequest ? 0x80 : 0x00) | (_fdc.DataRequest ? 0x40 : 0x00) | 0x3F;
				return true;
			}
			result = reg switch
			{
				0 => _fdc.ReadStatus(),
				1 => _fdc.ReadTrack(),
				2 => _fdc.ReadSector(),
				_ => _fdc.ReadData(),
			};
			return true;
		}

		public bool WritePort(ushort port, int value)
		{
			if (!Decode(port, out bool system, out int reg)) return false;
			CatchUp();
			byte v = (byte)value;
			if (system)
			{
				_system = v;
				// bits 0-1 drive select, bit 2 reset (0 = reset), bit 4 side, bit 6 density (0 = double).
				// The side line is active-low: bit 4 = 1 selects side 0, bit 4 = 0 selects side 1 (confirmed
				// by tracing TR-DOS - it writes bit 4 = 1 to read the disk-info/catalogue on physical side 0).
				int drive = v & 0x03;
				int side = ((v >> 4) & 0x01) ^ 1;
				bool doubleDensity = (v & 0x40) == 0;
				_fdc.SetSystem(drive, side, doubleDensity);
				FDD_FLAG_MOTOR = true;
				_drive.MotorOn = true;
				if ((v & 0x04) == 0) _fdc.Reset(); // MR asserted low
				return true;
			}
			switch (reg)
			{
				case 0: _fdc.WriteCommand(v); break;
				case 1: _fdc.WriteTrack(v); break;
				case 2: _fdc.WriteSector(v); break;
				default: _fdc.WriteData(v); break;
			}
			return true;
		}

		public void FDD_LoadDisk(byte[] diskData, int side)
		{
			FluxDisk flux;
			if (SclConverter.IsScl(diskData)) flux = SclConverter.ToFluxDisk(diskData);
			else if (TrdConverter.IsTrd(diskData)) flux = TrdConverter.ToFluxDisk(diskData);
			else flux = DiskImageLoader.ToFluxDisk(diskData); // SCP/HFE/FDI/etc
			if (side >= 0 && flux.Sides > 1) flux = flux.ExtractSide(side);
			_drive.Disk = flux;
			_diskLoaded = _drive.Disk != null;
		}

		public void FDD_EjectDisk()
		{
			_drive.Disk = null;
			_diskLoaded = false;
		}

		public bool FDD_IsDiskLoaded => _drive.Disk != null;
		public bool DiskInserted => _drive.Disk != null;
		public bool DriveLight => FDD_FLAG_MOTOR && _fdc.Active;

		/// <summary>
		/// TR-DOS disks do not carry the +3-style flux copy-protection the detector recognises.
		/// </summary>
		public string ProtectionName => "None";

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("BetaDiskController");
			bool motor = FDD_FLAG_MOTOR;
			ser.Sync(nameof(FDD_FLAG_MOTOR), ref motor);
			FDD_FLAG_MOTOR = motor;
			ser.Sync(nameof(_lastCycle), ref _lastCycle);
			ser.Sync(nameof(_diskLoaded), ref _diskLoaded);
			ser.Sync(nameof(_system), ref _system);
			_fdc.SyncState(ser);
			_drive.SyncState(ser);
			ser.EndSection();
		}
	}
}
