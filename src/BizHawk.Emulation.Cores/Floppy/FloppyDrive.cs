using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// The signals a floppy drive presents to the controller. Different drive types (single-sided 3" for
	/// the +3/CPC, others later) implement this so the controller stays hardware-agnostic.
	/// </summary>
	public interface IFloppyDrive
	{
		bool MotorOn { get; set; }
		bool Ready { get; }
		bool WriteProtected { get; }
		bool Track0 { get; }
		int CurrentCylinder { get; }
		int SideCount { get; }

		/// <summary>True while the motor is spun up to operating speed (rotation/index only run then).</summary>
		bool AtSpeed { get; }

		/// <summary>True during the index-hole pulse window once per revolution (only while AtSpeed).</summary>
		bool Index { get; }

		/// <summary>Mechanical track-to-track step time; a floor on how fast the controller can step. 0 = none.</summary>
		int TrackToTrackMs { get; }

		/// <summary>Head settling time added after the final step of a seek before it completes. 0 = none.</summary>
		int SettleMs { get; }

		void Step(bool towardHigherCylinder);
		void SeekTo(int cylinder);
		MfmTrack CurrentTrack(int side);

		/// <summary>Replace the flux at the current cylinder / given side (Write Data, Format).</summary>
		void WriteTrack(int side, MfmTrack track);

		/// <summary>Precompute rotation/spin-up thresholds for the host CPU clock (T-states per second).</summary>
		void ConfigureTiming(long cpuClockHz);

		/// <summary>Advance the mechanical timing by the given number of host CPU cycles.</summary>
		void Clock(int cpuCycles);
	}

	/// <summary>
	/// The fixed mechanical characteristics of a physical drive model: geometry (cylinders, heads),
	/// rotational speed, motor spin-up and the track-to-track / settling access times. A concrete drive
	/// type (e.g. the EME-150) supplies one of these so its real datasheet figures drive the timing.
	/// </summary>
	public sealed class FloppyDriveProfile
	{
		public int Cylinders { get; set; }       // recorded reference geometry; 0 = unspecified
		public int Sides { get; set; } = 2;       // physical head count
		public int Rpm { get; set; } = 300;       // rotational speed
		public int SpinUpMs { get; set; } = 1000; // motor start to at-speed
		public int TrackToTrackMs { get; set; }   // step-to-step access time (0 = no floor)
		public int SettleMs { get; set; }         // head settle after the final step (0 = none)

		/// <summary>A permissive generic profile (no cylinder limit, no step floor or settle).</summary>
		public static FloppyDriveProfile Generic { get; } = new();
	}

	/// <summary>
	/// A single mechanical floppy drive holding a FluxDisk. Models head position (current cylinder); the
	/// motor, ready, write-protect and track-0 signals; and mechanical timing driven by a FloppyDriveProfile
	/// - motor spin-up, disk rotation and the once-per-revolution index pulse. Seek-step timing is driven by
	/// the controller (it issues the step pulses at the programmed step rate, bounded by the drive's
	/// track-to-track figure); the drive just moves the head. Concrete hardware drives subclass this and pass
	/// their datasheet profile.
	/// </summary>
	public class FloppyDrive : IFloppyDrive
	{
		private readonly FloppyDriveProfile _profile;

		public FloppyDrive() : this(FloppyDriveProfile.Generic) { }

		public FloppyDrive(FloppyDriveProfile profile) => _profile = profile ?? FloppyDriveProfile.Generic;

		public FluxDisk Disk { get; set; }
		public bool MotorOn { get; set; }
		public int CurrentCylinder { get; private set; }

		public bool WriteProtected => Disk != null && Disk.WriteProtected;
		public bool Track0 => CurrentCylinder == 0;
		public bool Ready => Disk != null && MotorOn && Disk.Cylinders > 0;
		public int SideCount => Disk?.Sides ?? 0;

		public int TrackToTrackMs => _profile.TrackToTrackMs;
		public int SettleMs => _profile.SettleMs;

		/// <summary>Recorded cylinder count for this drive model (0 if unspecified).</summary>
		public int CylinderCount => _profile.Cylinders;

		private long _cpuHz = 3_546_900;
		private int _cyclesPerRev;
		private int _cyclesSpinUp;
		private int _indexWindow;
		private int _rotation;   // position within a revolution, in CPU cycles
		private int _spinUp;     // elapsed spin-up, in CPU cycles

		public bool AtSpeed => _cyclesSpinUp > 0 && _spinUp >= _cyclesSpinUp;
		public bool Index => AtSpeed && _rotation < _indexWindow;

		public void ConfigureTiming(long cpuClockHz)
		{
			_cpuHz = cpuClockHz > 0 ? cpuClockHz : 3_546_900;
			int rpm = _profile.Rpm > 0 ? _profile.Rpm : 300;
			_cyclesPerRev = (int)(_cpuHz * 60 / rpm); // e.g. 300 rpm -> 200 ms per revolution
			if (_cyclesPerRev < 1) _cyclesPerRev = 1;
			_indexWindow = System.Math.Max(1, _cyclesPerRev / 100); // roughly a 2 ms index pulse
			_cyclesSpinUp = (int)(_cpuHz * (_profile.SpinUpMs > 0 ? _profile.SpinUpMs : 1000) / 1000);
		}

		public void Clock(int cpuCycles)
		{
			if (_cyclesPerRev == 0) ConfigureTiming(_cpuHz);
			if (!MotorOn)
			{
				_spinUp = 0; // motor off: spins down (simplified as immediate)
				return;
			}
			if (_spinUp < _cyclesSpinUp)
			{
				_spinUp += cpuCycles;
				return;
			}
			_rotation += cpuCycles;
			while (_rotation >= _cyclesPerRev) _rotation -= _cyclesPerRev;
		}

		public void Step(bool towardHigherCylinder)
		{
			// The physical head can travel a little past the nominal cylinder count - disks are routinely
			// over-formatted (e.g. 42 tracks on a nominally-40 3" drive) - so we do not clamp at CylinderCount.
			if (towardHigherCylinder) CurrentCylinder++;
			else if (CurrentCylinder > 0) CurrentCylinder--;
		}

		public void SeekTo(int cylinder) => CurrentCylinder = cylinder < 0 ? 0 : cylinder;

		public MfmTrack CurrentTrack(int side) => Disk?.GetTrack(CurrentCylinder, side);

		public void WriteTrack(int side, MfmTrack track) => Disk?.SetTrack(CurrentCylinder, side, track);

		/// <summary>Serialize the mechanical state (head position, motor, rotation). The disk is restored separately.</summary>
		public void SyncState(Serializer ser)
		{
			ser.BeginSection("FloppyDrive");
			bool motor = MotorOn;
			ser.Sync(nameof(MotorOn), ref motor);
			MotorOn = motor;
			int cyl = CurrentCylinder;
			ser.Sync(nameof(CurrentCylinder), ref cyl);
			CurrentCylinder = cyl;
			ser.Sync(nameof(_rotation), ref _rotation);
			ser.Sync(nameof(_spinUp), ref _spinUp);
			ser.EndSection();
		}
	}
}
