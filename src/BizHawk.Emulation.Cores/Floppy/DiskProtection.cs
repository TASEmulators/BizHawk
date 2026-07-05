using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>A recognized +3/CPC disk copy-protection / special-format scheme (None if nothing matched).</summary>
	public enum DiskProtectionScheme
	{
		None,
		Speedlock,
		OperaSoft,
		LongTrack,
		Alkatraz,
		PaulOwens,
		Hexagon,
		ShadowOfTheBeast,
		RainbowArts,
		Kbi,
		Kbi19,
		Prehistorik,
		LogoProfessor,
		ElevenSector,
	}

	/// <summary>
	/// Identifies well-known ZX Spectrum +3 / Amstrad CPC disk copy-protection and special-format schemes
	/// from the flux, for logging. Detection is reported for everything recognized even when the flux model
	/// already reproduces the protection (deleted marks, non-standard ids/sizes, weak/fuzzy data) so no
	/// mitigation is needed; only Speedlock additionally needs weak-sector synthesis, and only for plain
	/// dumps that lack the weak data (see CpcDskConverter). Detection signatures follow the documented
	/// on-disk fingerprints (SAMdisk / CPCWiki / the original ZXHawk detection); this is an independent
	/// implementation of those facts.
	/// </summary>
	public static class DiskProtection
	{
		public static string DisplayName(DiskProtectionScheme scheme)
			=> scheme == DiskProtectionScheme.None ? "None (or unknown)" : scheme.ToString();

		/// <summary>Detect a protection / special-format scheme from the flux (best-effort, for reporting).</summary>
		public static DiskProtectionScheme Detect(FluxDisk disk)
		{
			if (disk == null) return DiskProtectionScheme.None;

			var t0 = Decode(disk, 0);

			// signature-string / distinctive-id schemes first (most reliable)
			if (IsSpeedlock(t0.Count, t0.Count > 0 ? t0[0].Data : null)) return DiskProtectionScheme.Speedlock;
			foreach (var s in t0)
			{
				if (ContainsAscii(s.Data, "PAUL OWENS")) return DiskProtectionScheme.PaulOwens;
				if (ContainsAscii(s.Data, "GON DISK PROT")) return DiskProtectionScheme.Hexagon;
			}
			if (IsRainbowArts(t0)) return DiskProtectionScheme.RainbowArts;
			if (IsKbi(t0)) return DiskProtectionScheme.Kbi;
			if (IsShadowOfTheBeast(t0, Decode(disk, 1))) return DiskProtectionScheme.ShadowOfTheBeast;
			if (IsOperaSoft(Decode(disk, 40)) || IsOperaSoft(t0)) return DiskProtectionScheme.OperaSoft;
			if (IsKbi19(t0)) return DiskProtectionScheme.Kbi19;
			if (IsLogoProfessor(t0)) return DiskProtectionScheme.LogoProfessor;
			if (IsElevenSector(t0)) return DiskProtectionScheme.ElevenSector;

			// structural schemes that may sit on a non-standard track near the disk start
			int scan = System.Math.Min(disk.Cylinders, 12);
			for (int c = 0; c < scan; c++)
			{
				var t = Decode(disk, c);
				if (IsPrehistorik(t)) return DiskProtectionScheme.Prehistorik;
				if (t.Count == 18 && t[0].C >= 0xE0) return DiskProtectionScheme.Alkatraz;
				foreach (var s in t) if (s.N >= 6) return DiskProtectionScheme.LongTrack; // 6K/8K "long" sector
			}

			return DiskProtectionScheme.None;
		}

		/// <summary>The Speedlock track-0 fingerprint: a 9-sector track whose first sector carries the signature.</summary>
		public static bool IsSpeedlock(int sectorCount, byte[] firstSectorData)
			=> sectorCount == 9 && ContainsAscii(firstSectorData, "SPEEDLOCK");

		/// <summary>
		/// The weak byte range within the Speedlock weak sector (id 2). If the sector starts with filler the
		/// weak run is 32 bytes at 0x150, otherwise the whole 512 bytes are treated as weak.
		/// </summary>
		public static (int Offset, int Length) SpeedlockWeakRegion(byte[] data)
		{
			bool startFiller = true;
			for (int i = 0; i < 250 && i + 1 < data.Length; i++)
			{
				if (data[i] != data[i + 1]) { startFiller = false; break; }
			}
			return startFiller ? (0x150, 0x20) : (0, System.Math.Min(0x200, data.Length));
		}

		// Rainbow Arts: 9-sector track with a non-standard sector id 198 carrying a data CRC error.
		private static bool IsRainbowArts(List<DecodedSector> t)
		{
			if (t.Count != 9) return false;
			foreach (var s in t) if (s.R == 198 && !s.DataCrcOk) return true;
			return false;
		}

		// KBI weak sector: a 10-sector track whose final 256-byte sector has a data CRC error and starts "Kxx".
		private static bool IsKbi(List<DecodedSector> t)
		{
			if (t.Count != 10) return false;
			var last = t[t.Count - 1];
			return last.N == 1 && !last.DataCrcOk && last.Data.Length >= 3
				&& last.Data[0] == (byte)'K' && IsAlpha(last.Data[1]) && IsAlpha(last.Data[2]);
		}

		// KBI-19: an unusual 19- or 20-sector track.
		private static bool IsKbi19(List<DecodedSector> t) => t.Count == 19 || t.Count == 20;

		// OperaSoft 32K: 9 sectors ids 0..8 where the last (id 8) is a 32K sector (size code 8).
		private static bool IsOperaSoft(List<DecodedSector> t)
		{
			if (t.Count != 9) return false;
			foreach (var s in t) if (s.R == 8 && s.N == 8) return true;
			return false;
		}

		// Prehistorik: a 4K sector (size code 5) with a data CRC error and a "Titus" signature.
		private static bool IsPrehistorik(List<DecodedSector> t)
		{
			foreach (var s in t) if (s.N == 5 && ContainsAscii(s.Data, "Titus")) return true;
			return false;
		}

		// Logo Professor: a 10/11-sector track whose sector ids start at 2 (non-standard).
		private static bool IsLogoProfessor(List<DecodedSector> t)
			=> (t.Count == 10 || t.Count == 11) && LowestId(t) == 2;

		// A full 11-sector track (ids from 1) - a non-standard tight format.
		private static bool IsElevenSector(List<DecodedSector> t)
			=> t.Count == 11 && LowestId(t) == 1;

		private static bool IsShadowOfTheBeast(List<DecodedSector> t0, List<DecodedSector> t1)
		{
			if (t0.Count != 9 || t1.Count != 8) return false;
			for (int i = 0; i < 9; i++) if (t0[i].R != 65 + i) return false;
			for (int i = 0; i < 8; i++) if (t1[i].R != 17 + i) return false;
			return true;
		}

		private static int LowestId(List<DecodedSector> t)
		{
			int min = 0xFF;
			foreach (var s in t) if (s.R < min) min = s.R;
			return min;
		}

		private static bool IsAlpha(byte b) => (b >= 'A' && b <= 'Z') || (b >= 'a' && b <= 'z');

		private static List<DecodedSector> Decode(FluxDisk disk, int cyl)
		{
			var t = disk.GetTrack(cyl, 0);
			return t == null ? new List<DecodedSector>() : StandardMfmFormat.DecodeSectors(t);
		}

		private static bool ContainsAscii(byte[] data, string s)
		{
			if (data == null || data.Length < s.Length) return false;
			for (int i = 0; i <= data.Length - s.Length; i++)
			{
				int j = 0;
				for (; j < s.Length; j++) if (data[i + j] != (byte)s[j]) break;
				if (j == s.Length) return true;
			}
			return false;
		}
	}
}
