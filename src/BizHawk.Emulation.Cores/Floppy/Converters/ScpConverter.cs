using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// Loader for the SuperCard Pro (.scp) flux image format. SCP records raw flux-reversal timings
	/// (16-bit big-endian, in 25 ns ticks) per revolution, not decoded cells - so we recover cells by
	/// quantizing each flux interval to the nearest whole number of bit-cells at the given cell time (2 us
	/// for 250 kbit/s DD), emitting that many cells with a flux transition ('1') on the last one. This is a
	/// simple quantizer, not a full PLL; it decodes clean dumps. The FDC reader locks phase on the A1 syncs.
	/// </summary>
	public static class ScpConverter
	{
		private const long DefaultCellTimeNs = 2000; // 2 us bit-cell = 250 kbit/s double density

		public static bool IsScp(byte[] d)
			=> d != null && d.Length >= 16 && d[0] == (byte)'S' && d[1] == (byte)'C' && d[2] == (byte)'P';

		public static FluxDisk ToFluxDisk(byte[] d, long cellTimeNs = DefaultCellTimeNs)
		{
			if (!IsScp(d)) throw new System.ArgumentException("not an SCP file (no SCP signature)", nameof(d));

			byte flags = d[0x08];
			if ((flags & 0x40) != 0) throw new System.ArgumentException("SCP extended mode not supported", nameof(d));
			int startTrack = d[0x06];
			int endTrack = d[0x07];
			int headMode = d[0x0A];        // 0 = both, 1 = side 0 only, 2 = side 1 only
			int resolution = d[0x0B];
			long tickNs = 25L * (resolution + 1);

			var disk = new FluxDisk();
			for (int t = startTrack; t <= endTrack; t++)
			{
				int tableEntry = 0x10 + t * 4;
				if (tableEntry + 4 > d.Length) break;
				int tdh = ReadLe32(d, tableEntry);
				if (tdh == 0 || tdh + 16 > d.Length) continue;
				if (d[tdh] != 'T' || d[tdh + 1] != 'R' || d[tdh + 2] != 'K') continue;

				// use the first revolution's triplet
				int trackLen = ReadLe32(d, tdh + 8);     // flux transition count
				int dataOffset = ReadLe32(d, tdh + 12);  // relative to the TDH start
				byte[] cells = FluxToCells(d, tdh + dataOffset, trackLen, tickNs, cellTimeNs, out int cellCount);
				if (cellCount == 0) continue;

				var (cyl, head) = MapTrack(t, headMode);
				disk.SetTrack(cyl, head, new MfmTrack(cells, cellCount));
			}
			return disk;
		}

		private static (int cyl, int head) MapTrack(int track, int headMode)
			=> headMode switch
			{
				1 => (track, 0),      // side 0 only
				2 => (track, 1),      // side 1 only
				_ => (track >> 1, track & 1), // both heads interleaved: even = side 0, odd = side 1
			};

		// Quantize flux intervals into MFM cells (LSB-first packed, matching MfmTrack).
		private static byte[] FluxToCells(byte[] d, int start, int fluxCount, long tickNs, long cellTimeNs, out int cellCount)
		{
			var cells = new List<bool>(fluxCount * 3);
			long carry = 0;
			int p = start;
			for (int i = 0; i < fluxCount; i++)
			{
				if (p + 2 > d.Length) break;
				int v = (d[p] << 8) | d[p + 1]; // 16-bit big-endian
				p += 2;
				if (v == 0) { carry += 65536; continue; } // overflow marker: fold into the next interval

				long total = carry + v;
				carry = 0;
				long ns = total * tickNs;
				int n = (int)((ns + cellTimeNs / 2) / cellTimeNs); // round to nearest cell count
				if (n < 1) n = 1;
				for (int c = 0; c < n - 1; c++) cells.Add(false);
				cells.Add(true); // flux transition on the final cell
			}

			cellCount = cells.Count;
			var packed = new byte[(cellCount + 7) >> 3];
			for (int i = 0; i < cellCount; i++)
				if (cells[i]) packed[i >> 3] |= (byte)(1 << (i & 7)); // LSB first, matching MfmTrack
			return packed;
		}

		private static int ReadLe32(byte[] d, int o) => d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24);
	}
}
