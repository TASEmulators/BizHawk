using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// The floppy disk controller surface the +3 machine drives. Implemented by the shared flux-based uPD765
	/// subsystem adapter. This is the seam that lets the machine stay independent of the controller internals.
	/// </summary>
	public interface IFloppyDiskController : IPortIODevice
	{
		/// <summary>
		/// Wire the controller to its host machine (for the CPU cycle clock).
		/// </summary>
		void Init(SpectrumBase machine);

		/// <summary>
		/// Load a disk image (any supported format) into drive 0. side selects one side of
		/// a double-sided image (0 or 1) to present as a single-sided disk for the single-headed drive; -1
		/// loads the image as-is.
		/// </summary>
		void FDD_LoadDisk(byte[] diskData, int side);

		/// <summary>
		/// Eject the disk in drive 0.
		/// </summary>
		void FDD_EjectDisk();

		/// <summary>
		/// True if drive 0 has a disk inserted.
		/// </summary>
		bool FDD_IsDiskLoaded { get; }

		/// <summary>
		/// The spindle-motor line (port 0x1ffd bit 3), also read back for snapshots.
		/// </summary>
		bool FDD_FLAG_MOTOR { get; set; }

		/// <summary>
		/// Disk activity indicator for the UI light.
		/// </summary>
		bool DriveLight { get; }

		/// <summary>
		/// True if a disk is present (for status messages).
		/// </summary>
		bool DiskInserted { get; }

		/// <summary>
		/// Human-readable protection description for status messages.
		/// </summary>
		string ProtectionName { get; }

		void SyncState(Serializer ser);
	}
}
