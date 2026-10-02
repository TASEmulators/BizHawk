namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// The Panasonic/Matsushita EME-150: the single-head (single-sided) 3-inch compact floppy disk drive
	/// fitted to the early Amstrad CPC (664/6128). The ZX Spectrum +3 and later CPCs use the EME-156, a minor
	/// cost-reduced revision whose electrical, timing and track specifications are identical (see
	/// Eme156Drive) - only mechanical details (the write-protect plunger and PCB/worm-drive layout) differ.
	/// The double-head variant is the EME-250; single-head machines handle double-sided disks by splitting
	/// them into two single-sided images at load time.
	/// Datasheet figures below come from the EME-150 service manual (Table 1-1, Figure 6):
	/// https://www.cpcwiki.eu/imgs/0/0e/EME150_ServiceManual.pdf
	///   - 300 rpm rotation (200 ms per revolution; 100 ms average latency), one index pulse per revolution
	///   - 1.0 s max motor spin-up before valid read data
	///   - 12 ms track-to-track access (a floor on the controller's programmed step rate)
	///   - 15 ms head settling time after the final step of a seek
	///   - 40 cylinders, single side, 250 kbit/s MFM (double density)
	/// </summary>
	public sealed class Eme150Drive : FloppyDrive
	{
		public Eme150Drive() : base(Profile) { }

		/// <summary>
		/// The shared EME-150/156 family profile. No standalone EME-156 datasheet exists; the EME-150 manual
		/// is authoritative for both, as the electrical/timing/geometry figures are identical.
		/// </summary>
		public static FloppyDriveProfile Profile { get; } = new()
		{
			Cylinders = 40,
			Sides = 1,
			Rpm = 300,
			SpinUpMs = 1000,
			TrackToTrackMs = 12,
			SettleMs = 15,
		};
	}
}
