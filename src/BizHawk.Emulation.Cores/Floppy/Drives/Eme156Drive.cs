namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// The Panasonic/Matsushita EME-156: the single-head 3-inch drive fitted to the ZX Spectrum +3 (as the
	/// "FD-1") and later Amstrad CPCs. It is a cost-reduced revision of the EME-150 (see Eme150Drive); the
	/// two are electrically and timing-wise identical (40 tracks, single sided, 300 rpm, 250 kbit/s MFM), so
	/// this reuses the EME-150 family profile. The differences are purely mechanical - the EME-156 senses
	/// write-protect via a loose plunger pin rather than a leaf switch, and relocates the stepper worm drive
	/// / PCB traces - none of which affect emulation.
	/// No standalone EME-156 service manual was published; the EME-150 manual is the authoritative timing
	/// source (https://www.cpcwiki.eu/imgs/0/0e/EME150_ServiceManual.pdf), and the EME-156 board schematic
	/// appears as a supplement in the ZX Spectrum +3 service manual (FD-1 EME-156 Disk Drive Circuit Diagram).
	/// </summary>
	public sealed class Eme156Drive : FloppyDrive
	{
		public Eme156Drive() : base(Eme150Drive.Profile) { }
	}
}
