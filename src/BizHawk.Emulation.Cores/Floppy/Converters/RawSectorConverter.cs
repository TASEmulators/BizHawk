using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// Physical geometry for a headerless raw sector image (which carries no metadata of its own).
	/// </summary>
	public sealed class DiskGeometry
	{
		public int Cylinders { get; set; }
		public int Heads { get; set; } = 1;
		public int SectorsPerTrack { get; set; }
		public int SectorSize { get; set; } = 512;
		public int FirstSectorId { get; set; } = 1;
		public int Gap3 { get; set; } = 78;

		/// <summary>
		/// The standard ZX Spectrum +3 / PCW format: 40 cylinders, 1 side, 9x512 sectors from id 1.
		/// </summary>
		public static DiskGeometry Plus3 => new() { Cylinders = 40, Heads = 1, SectorsPerTrack = 9, SectorSize = 512, FirstSectorId = 1 };

		public int TotalBytes => Cylinders * Heads * SectorsPerTrack * SectorSize;
	}

	/// <summary>
	/// Loads a headerless raw sector image (.img/.raw/.trd-style) into flux using an explicit geometry - the
	/// sectors are laid out sequentially cylinder 0 head 0, cylinder 0 head 1, ... each track's sectors in id
	/// order. Each track is synthesized as a clean IBM System-34 MFM track. Since the image carries no gap,
	/// weak or timing data the result is a clean disk (protected titles need EDSK/IPF/HFE/SCP instead).
	/// </summary>
	public static class RawSectorConverter
	{
		public static FluxDisk ToFluxDisk(byte[] data, DiskGeometry g)
		{
			if (data == null) throw new System.ArgumentNullException(nameof(data));
			if (g == null || g.Cylinders <= 0 || g.SectorsPerTrack <= 0 || g.SectorSize <= 0)
				throw new System.ArgumentException("invalid geometry", nameof(g));

			int n = SizeCode(g.SectorSize);
			var disk = new FluxDisk();
			int pos = 0;
			for (int cyl = 0; cyl < g.Cylinders; cyl++)
			{
				for (int head = 0; head < g.Heads; head++)
				{
					var secs = new List<TrackSector>(g.SectorsPerTrack);
					for (int s = 0; s < g.SectorsPerTrack; s++)
					{
						var sd = new byte[g.SectorSize];
						int copy = System.Math.Min(g.SectorSize, System.Math.Max(0, data.Length - pos));
						if (copy > 0) System.Array.Copy(data, pos, sd, 0, copy);
						pos += g.SectorSize;
						secs.Add(new TrackSector { C = (byte)cyl, H = (byte)head, R = (byte)(g.FirstSectorId + s), N = (byte)n, Data = sd });
					}
					disk.SetTrack(cyl, head, StandardMfmFormat.BuildStandardTrack(secs, g.Gap3));
				}
			}
			return disk;
		}

		private static int SizeCode(int sectorSize)
		{
			int n = 0;
			while ((128 << n) < sectorSize && n < 7) n++;
			return n;
		}
	}
}
