using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// Determines which emulated System a container-agnostic flux disk image (.scp, .hfe) belongs to by
	/// decoding it and inspecting the disk contents - the equivalent of DskIdentifier/IpfIdentifier for the
	/// flux formats. Those two live in BizHawk.Emulation.Common (which cannot reference this assembly), and
	/// SCP/HFE headers do not reliably encode the target machine (a ZX Spectrum and an Amstrad CPC dump both
	/// look like "generic MFM"), so the only dependable way to tell them apart is to decode the flux to
	/// sectors and apply the same content heuristics DskIdentifier uses. That decode is only available here,
	/// so RomLoader (which can reach both assemblies) calls this when the extension leaves the system open.
	/// Returns a VSystemID.Raw value, or null when the contents are inconclusive (leave it to the chooser).
	/// </summary>
	public static class FluxDiskFormatIdentifier
	{
		public static string IdentifySystem(byte[] image)
		{
			FluxDisk disk;
			try
			{
				if (ScpConverter.IsScp(image)) disk = ScpConverter.ToFluxDisk(image);
				else if (HfeConverter.IsHfe(image) || HfeConverter.IsHfeV3(image)) disk = HfeConverter.ToFluxDisk(image);
				else return null;
			}
			catch
			{
				return null;
			}

			return IdentifySystem(disk);
		}

		/// <summary>
		/// Identify the target System from an already-decoded flux disk (exposed for testing and for callers
		/// that have a FluxDisk in hand). Returns a VSystemID.Raw value, or null when inconclusive.
		/// </summary>
		public static string IdentifySystem(FluxDisk disk)
		{
			var t0 = disk?.GetTrack(0, 0);
			if (t0 == null) return null;
			var secs = StandardMfmFormat.DecodeSectors(t0);
			if (secs.Count == 0) return null; // not an IBM/System-34 MFM disk (e.g. Amiga/C64 GCR) - defer

			secs.Sort((a, b) => a.R.CompareTo(b.R));

			// TR-DOS (Beta 128 / Pentagon): the disk-info sector is track 0 physical sector 9, carrying the
			// TR-DOS marker 0x10 at 0xE7 and a disk-type byte 0x16-0x19 at 0xE3.
			foreach (var s in secs)
			{
				if (s.R == 9 && s.Data != null && s.Data.Length > 0xE7
					&& s.Data[0xE7] == 0x10 && s.Data[0xE3] >= 0x16 && s.Data[0xE3] <= 0x19)
				{
					return VSystemID.Raw.ZXSpectrum;
				}
			}

			// +3DOS: every +3DOS file starts with a 128-byte "PLUS3DOS" header, but the reserved/boot track 0
			// is often blank (the directory and files live on track 1+), so scan the first few cylinders on
			// both sides - the same whole-disk search DskIdentifier does for the .dsk container.
			int scanCyls = System.Math.Min(disk.Cylinders, 4);
			for (int cyl = 0; cyl < scanCyls; cyl++)
			{
				for (int side = 0; side < disk.Sides; side++)
				{
					var t = disk.GetTrack(cyl, side);
					if (t == null) continue;
					foreach (var s in StandardMfmFormat.DecodeSectors(t))
						if (s.Data != null && ContainsAscii(s.Data, "PLUS3DOS"))
							return VSystemID.Raw.ZXSpectrum;
				}
			}

			// Amstrad CPC formats number their sectors from 0x41 (or 0xC1 on side 1 / higher tracks)
			byte lowestId = 0xFF;
			foreach (var s in secs) if (s.R < lowestId) lowestId = s.R;
			if (lowestId is 0x41 or 0xC1) return VSystemID.Raw.AmstradCPC;

			// bootable-record checksum on the first sector: a +3 boot record sums to 3 (mod 256),
			// the Amstrad PCW/CPC boot records to 1 or 255 (same test DskIdentifier uses)
			var boot = secs[0];
			if (boot.Data != null && boot.Data.Length >= 512)
			{
				int sum = 0;
				for (int i = 0; i < 512; i++) sum += boot.Data[i];
				switch (sum & 0xFF)
				{
					case 3: return VSystemID.Raw.ZXSpectrum;
					case 1:
					case 255: return VSystemID.Raw.AmstradCPC;
				}
			}

			return null; // inconclusive - let the platform chooser decide
		}

		private static bool ContainsAscii(byte[] data, string needle)
		{
			int n = needle.Length;
			for (int i = 0; i + n <= data.Length; i++)
			{
				int j = 0;
				while (j < n && data[i + j] == (byte)needle[j]) j++;
				if (j == n) return true;
			}
			return false;
		}
	}
}
