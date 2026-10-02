using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// Reader for the CPCEMU DSK and Extended DSK (EDSK) disk-image formats (used by +3 and CPC
	/// software), producing per-track sector lists that synthesize into MFM flux tracks. Written from the
	/// documented on-disk layout, not ported from any existing implementation.
	/// Standard DSK stores uniform 128*2^N sector data. EDSK adds a per-track size table and per-sector
	/// actual data lengths, which is how weak sectors are recorded: a data length that is an exact multiple
	/// of the sector size means several copies are stored, and the byte positions that disagree across
	/// copies are the weak (fuzzy) bits, mapped here to weak cells in the flux.
	/// </summary>
	public static class CpcDskConverter
	{
		public sealed class ParsedTrack
		{
			public int Cylinder;
			public int Side;
			public byte Gap3;
			public readonly List<TrackSector> Sectors = new List<TrackSector>();

			public MfmTrack BuildFlux() => StandardMfmFormat.BuildStandardTrack(Sectors, Gap3 == 0 ? 78 : Gap3);
		}

		public sealed class ParsedDisk
		{
			public bool Extended;
			public int TrackCount;
			public int SideCount;
			public readonly List<ParsedTrack> Tracks = new List<ParsedTrack>();
		}

		/// <summary>
		/// A plain DSK dump of a Speedlock-protected title carries no weak-sector data, so the protection
		/// check (which reads the weak sector several times expecting differing bytes and a data CRC error)
		/// would fail. When Speedlock is detected and the weak sector has no recorded copies, synthesize a
		/// few differing copies over its weak byte range - BuildStandardTrack then flags those cells weak and
		/// the FDC reads them unpredictably, reproducing the protection. Images that already carry weak data
		/// (EDSK multi-copy) are left untouched.
		/// </summary>
		public static void ApplySpeedlockWeakSynthesis(ParsedDisk disk)
		{
			var t0 = disk?.Tracks.Find(t => t.Cylinder == 0 && t.Side == 0);
			if (t0 == null || t0.Sectors.Count != 9) return;
			if (!DiskProtection.IsSpeedlock(t0.Sectors.Count, t0.Sectors[0].Data)) return;

			var weak = t0.Sectors[1]; // Speedlock's weak sector is the second one (id 2)
			// The genuine Speedlock weak sector always carries a data CRC error in the dump; synthesize only
			// then. A plain deleted-DAM data sector (valid CRC) must NOT be corrupted - some titles that carry
			// the Speedlock loader signature still store real data in sector 2.
			if (weak.WeakCopies != null || !weak.DataCrcError) return; // already weak, or not the weak sector

			int size = weak.SizeBytes;
			var copy0 = new byte[size];
			System.Array.Copy(weak.Data, 0, copy0, 0, System.Math.Min(size, weak.Data.Length));
			var copy1 = (byte[])copy0.Clone();
			var copy2 = (byte[])copy0.Clone();

			var (offset, length) = DiskProtection.SpeedlockWeakRegion(copy0);
			for (int i = offset; i < offset + length && i < size; i++)
			{
				copy1[i] = (byte)(copy0[i] ^ 0xFF);
				copy2[i] = (byte)(copy0[i] + 0x55);
			}
			weak.WeakCopies = new[] { copy0, copy1, copy2 };
		}

		private const string StdIdent = "MV - CPC";
		private const string ExtIdent = "EXTENDED";
		private const string TrackIdent = "Track-Info";

		public static bool IsCpcDsk(byte[] d)
			=> d != null && d.Length >= 0x100 && (Match(d, 0, StdIdent) || Match(d, 0, ExtIdent));

		public static ParsedDisk Parse(byte[] d)
		{
			if (!IsCpcDsk(d)) throw new ArgumentException("not a CPC DSK/EDSK image", nameof(d));

			var disk = new ParsedDisk
			{
				Extended = Match(d, 0, ExtIdent),
				TrackCount = d[0x30],
				SideCount = d[0x31] == 0 ? 1 : d[0x31],
			};

			int stdTrackSize = d[0x32] | (d[0x33] << 8); // standard DSK: one uniform track size
			int entries = disk.TrackCount * disk.SideCount;
			int offset = 0x100; // track data begins after the 256-byte disk header

			for (int i = 0; i < entries; i++)
			{
				int trackSize = disk.Extended ? d[0x34 + i] * 256 : stdTrackSize;
				if (disk.Extended && trackSize == 0)
					continue; // unformatted track: no Track-Info block, no data

				if (offset + 0x100 > d.Length) break;
				if (!Match(d, offset, TrackIdent)) { offset += trackSize; continue; }

				var pt = new ParsedTrack
				{
					Cylinder = d[offset + 0x10],
					// A single-sided image keeps all tracks on side 0. This matters for the +3 double-sided
					// workflow: a DS disk is split into two single-sided images, but the split leaves the
					// second image's track headers still marked side 1 - honouring that would hide the disk
					// from the single-headed drive (which only reads side 0).
					Side = disk.SideCount <= 1 ? 0 : d[offset + 0x11],
					Gap3 = d[offset + 0x16],
				};

				int numSectors = d[offset + 0x15];
				int siPos = offset + 0x18;   // sector information list (8 bytes each)
				int dataPos = offset + 0x100; // sector data area

				for (int s = 0; s < numSectors; s++)
				{
					int p = siPos + s * 8;
					if (p + 8 > d.Length) break;

					byte c = d[p], h = d[p + 1], r = d[p + 2], n = d[p + 3];
					byte st1 = d[p + 4], st2 = d[p + 5];
					int declared = 128 << (n & 7);
					int actual = disk.Extended ? (d[p + 6] | (d[p + 7] << 8)) : declared;
					if (actual == 0) actual = declared;

					int avail = Math.Max(0, Math.Min(actual, d.Length - dataPos));
					var raw = new byte[actual];
					if (avail > 0) Array.Copy(d, dataPos, raw, 0, avail);
					dataPos += actual;

					var ts = new TrackSector
					{
						C = c, H = h, R = r, N = n,
						// image status bytes -> synthesized flux conditions
						Deleted = (st2 & 0x40) != 0,               // ST2 CM: deleted data address mark
						DataCrcError = (st2 & 0x20) != 0,          // ST2 DD: data-field CRC error
						IdCrcError = (st1 & 0x20) != 0 && (st2 & 0x20) == 0, // ST1 DE without DD: ID-field CRC error
					};

					if (actual > declared && declared > 0 && actual % declared == 0)
					{
						// several stored copies -> weak sector; keep them so differing bytes become weak cells
						int copies = actual / declared;
						var arr = new byte[copies][];
						for (int cpy = 0; cpy < copies; cpy++)
						{
							arr[cpy] = new byte[declared];
							Array.Copy(raw, cpy * declared, arr[cpy], 0, declared);
						}
						ts.WeakCopies = arr;
						ts.Data = arr[0];
					}
					else
					{
						var data = new byte[declared];
						Array.Copy(raw, 0, data, 0, Math.Min(declared, raw.Length));
						ts.Data = data;
					}

					pt.Sectors.Add(ts);
				}

				disk.Tracks.Add(pt);
				offset += trackSize;
			}

			return disk;
		}

		private static bool Match(byte[] d, int at, string ascii)
		{
			if (at + ascii.Length > d.Length) return false;
			for (int i = 0; i < ascii.Length; i++)
				if (d[at + i] != (byte)ascii[i]) return false;
			return true;
		}
	}
}
