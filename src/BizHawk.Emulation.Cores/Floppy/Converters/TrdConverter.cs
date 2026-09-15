namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// Loads a TR-DOS .TRD disk image (the Beta 128 / Pentagon native format) into the shared flux model.
	/// A .TRD is a headerless raw sector dump: 16 sectors of 256 bytes per track, stored in TR-DOS logical
	/// track order (cylinder-major, side-minor: cyl0/side0, cyl0/side1, cyl1/side0, ...), which is exactly
	/// the layout RawSectorConverter emits. The geometry (40/80 cylinders, single/double sided) is taken
	/// from the disk-type byte in the disk-info sector, falling back to 80-track double-sided (640K).
	/// </summary>
	public static class TrdConverter
	{
		// disk-info sector = track 0, sector 9 (zero-based sector index 8) -> file offset 8 * 256 = 2048
		private const int InfoSectorOffset = 8 * 256;
		private const int DiskTypeOffset = InfoSectorOffset + 0xE3; // disk type byte
		private const int TrDosIdOffset = InfoSectorOffset + 0xE7;  // TR-DOS marker byte (0x10)

		/// <summary>
		/// A .TRD carries no signature, so validate structurally: the length must be a whole number of
		/// 256-byte sectors (TOSEC and other tools commonly store TRIMMED images, omitting the trailing
		/// unused sectors - so the length is NOT necessarily a whole number of 16-sector tracks), it must be
		/// large enough to hold track 0, and the disk-info sector must carry the TR-DOS marker (0x10) and a
		/// known disk-type byte (0x16-0x19). The trailing sectors omitted by a trim are zero-padded on load.
		/// </summary>
		public static bool IsTrd(byte[] d)
		{
			if (d == null || d.Length < 9 * 256) return false;
			if (d.Length % 256 != 0) return false;
			byte type = d[DiskTypeOffset];
			return d[TrDosIdOffset] == 0x10 && type >= 0x16 && type <= 0x19;
		}

		public static FluxDisk ToFluxDisk(byte[] d)
		{
			var g = DiskGeometry.TrDos;
			if (d != null && d.Length > DiskTypeOffset)
			{
				switch (d[DiskTypeOffset])
				{
					case 0x16: g.Cylinders = 80; g.Heads = 2; break;
					case 0x17: g.Cylinders = 40; g.Heads = 2; break;
					case 0x18: g.Cylinders = 80; g.Heads = 1; break;
					case 0x19: g.Cylinders = 40; g.Heads = 1; break;
				}
			}
			return RawSectorConverter.ToFluxDisk(d, g);
		}
	}
}
