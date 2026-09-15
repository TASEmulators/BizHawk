namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// Loader for the ZX Spectrum UDI v1.0 ("UDI!") disk image format. UDI is a track-level format: each
	/// track stores the decoded byte stream as read off the track (gaps, sync, address marks, CHRN, CRC and
	/// data) plus a clock bitmap flagging which bytes are anomalous-clock markers (the A1/C2 sync bytes). We
	/// map that straight onto MFM cells - a flagged 0xA1/0xC2 becomes the missing-clock sync, every other
	/// byte is MFM-encoded normally - which the FDC reader then decodes as usual. The compressed variant
	/// ("udi!") is not supported.
	/// </summary>
	public static class UdiConverter
	{
		public static bool IsUdi(byte[] d)
			=> d != null && d.Length >= 16
				&& (d[0] == 'U' || d[0] == 'u') && (d[1] == 'D' || d[1] == 'd') && (d[2] == 'I' || d[2] == 'i') && d[3] == '!'
				&& d[0x08] == 0; // uppercase "UDI!" = uncompressed, lowercase "udi!" = compressed (rejected on convert)

		public static FluxDisk ToFluxDisk(byte[] d)
		{
			if (!IsUdi(d)) throw new System.ArgumentException("not a UDI v1.0 file", nameof(d));
			if (d[0] == 'u') throw new System.ArgumentException("compressed UDI ('udi!') is not supported", nameof(d));

			int numTracks = d[0x09] + 1;
			int numSides = d[0x0A] + 1;
			int extHeader = ReadLe32(d, 0x0C);
			int pos = 0x10 + extHeader;

			var disk = new FluxDisk();
			int total = numTracks * numSides;
			for (int t = 0; t < total; t++)
			{
				if (pos + 3 > d.Length) break;
				pos++; // track type (0 = MFM); ZX/Beta tracks are MFM
				int tlen = d[pos] | (d[pos + 1] << 8);
				pos += 2;
				int clen = (tlen + 7) / 8; // one clock bit per track byte
				if (tlen <= 0 || pos + tlen + clen > d.Length) { pos += tlen + clen; continue; }

				// tracks are stored interleaved by side within a cylinder
				disk.SetTrack(t / numSides, t % numSides, BuildTrack(d, pos, tlen, pos + tlen));
				pos += tlen + clen;
			}
			return disk;
		}

		private static MfmTrack BuildTrack(byte[] d, int dataStart, int tlen, int clockStart)
		{
			var w = new MfmTrackWriter();
			for (int i = 0; i < tlen; i++)
			{
				byte b = d[dataStart + i];
				bool marker = (d[clockStart + (i >> 3)] & (1 << (i & 7))) != 0; // clock bitmap: LSB first
				if (marker && b == 0xA1) w.WriteSyncA1();
				else if (marker && b == 0xC2) w.WriteSyncC2();
				else w.WriteByte(b);
			}
			return w.Build();
		}

		private static int ReadLe32(byte[] d, int o) => d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24);
	}
}
