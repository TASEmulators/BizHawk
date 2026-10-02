using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// Loader for the ZX Spectrum FDI ("Full Disk Image", UKV/Ramsoft) sector format. FDI is a
	/// sector-level container (per-track sector lists with CHRN, a flags byte, and a data pointer); we
	/// synthesize a clean IBM System-34 MFM track from each track's sectors. FDI is most often a TR-DOS /
	/// Beta Disk image read by a WD1793 controller rather than the uPD765 - but this conversion is
	/// controller-agnostic (it produces standard MFM flux), so the eventual WD1793 core would read the same
	/// FluxDisk this produces.
	/// </summary>
	public static class FdiConverter
	{
		public static bool IsFdi(byte[] d)
			=> d != null && d.Length >= 14 && d[0] == (byte)'F' && d[1] == (byte)'D' && d[2] == (byte)'I';

		public static FluxDisk ToFluxDisk(byte[] d)
		{
			if (!IsFdi(d)) throw new System.ArgumentException("not an FDI file (no FDI signature)", nameof(d));

			bool writeProtected = d[0x03] != 0;
			int cylinders = ReadLe16(d, 0x04);
			int heads = ReadLe16(d, 0x06);
			int dataArea = ReadLe16(d, 0x0A);
			int extraLen = ReadLe16(d, 0x0C);

			var disk = new FluxDisk { WriteProtected = writeProtected };
			int th = 0x0E + extraLen; // first track header

			for (int i = 0; i < cylinders * heads; i++)
			{
				if (th + 7 > d.Length) break;
				int trackOffset = ReadLe32(d, th);
				int sectorCount = d[th + 6];
				int cyl = i / heads;
				int head = i % heads;

				var secs = new List<TrackSector>(sectorCount);
				int sd = th + 7; // first sector descriptor
				for (int s = 0; s < sectorCount; s++)
				{
					if (sd + 7 > d.Length) break;
					byte c = d[sd], h = d[sd + 1], r = d[sd + 2], n = d[sd + 3], flags = d[sd + 4];
					int secOffset = ReadLe16(d, sd + 5);
					sd += 7;

					int size = 128 << (n & 7);
					var data = new byte[size];
					int abs = dataArea + trackOffset + secOffset;
					int copy = System.Math.Min(size, System.Math.Max(0, d.Length - abs));
					if (abs >= 0 && copy > 0) System.Array.Copy(d, abs, data, 0, copy);

					secs.Add(new TrackSector
					{
						C = c, H = h, R = r, N = n, Data = data,
						Deleted = (flags & 0x80) != 0,      // bit 7 = deleted data address mark
						DataCrcError = (flags & 0x3F) == 0, // no CRC-valid bits set => the sector had a CRC error
					});
				}

				disk.SetTrack(cyl, head, StandardMfmFormat.BuildStandardTrack(secs));
				th = sd; // next track header follows the last sector descriptor
			}
			return disk;
		}

		private static int ReadLe16(byte[] d, int o) => d[o] | (d[o + 1] << 8);
		private static int ReadLe32(byte[] d, int o) => d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24);
	}
}
