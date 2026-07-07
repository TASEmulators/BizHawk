using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// <para>Loader for the SuperCard Pro (.scp) flux image format. SCP records raw flux-reversal timings
	/// (16-bit big-endian, in 25 ns ticks) per revolution, not decoded cells - so we recover cells with a
	/// per-track software PLL (auto-estimate the real bit-cell time, then track it) and emit one flux
	/// transition per interval.</para>
	/// <para>SCP stores several revolutions of each track (header byte 5). We decode every revolution and let
	/// the FDC read whichever recovered the most valid-CRC sectors (a marginal read on one pass is often clean
	/// on another), then use the remaining revolutions to recover WEAK/FUZZY bits: a copy-protection weak sector
	/// deliberately reads unstably, which shows up as the same sector's data differing from revolution to
	/// revolution. Cells whose byte differs across revolutions (in a sector that does not already read cleanly)
	/// are flagged weak so the FDC returns unpredictable data there, reproducing the protection - the whole
	/// point of a flux-level dump. To avoid corrupting genuinely-stable sectors with our own decode jitter, we
	/// only weaken sectors that fail their data CRC (a solid read is left solid).</para>
	/// </summary>
	public static class ScpConverter
	{
		private const long DefaultCellTimeNs = 2000; // 2 us bit-cell = 250 kbit/s double density

		public static bool IsScp(byte[] d)
			=> d != null && d.Length >= 16 && d[0] == (byte)'S' && d[1] == (byte)'C' && d[2] == (byte)'P';

		/// <summary>Number of revolutions captured per track (SCP header byte 5). Multi-rev dumps let a
		/// marginal read on one revolution be recovered from another, and weak bits to be detected.</summary>
		public static int RevolutionCount(byte[] d) => d != null && d.Length > 5 ? d[5] : 0;

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
			int revs = d[0x05];
			if (revs < 1) revs = 1;

			var disk = new FluxDisk();
			for (int t = startTrack; t <= endTrack; t++)
			{
				int tableEntry = 0x10 + t * 4;
				if (tableEntry + 4 > d.Length) break;
				int tdh = ReadLe32(d, tableEntry);
				if (tdh == 0 || tdh + 16 > d.Length) continue;
				if (d[tdh] != 'T' || d[tdh + 1] != 'R' || d[tdh + 2] != 'K') continue;

				// decode every revolution once, keeping its cells and its decoded sectors
				var revCells = new List<(byte[] packed, int count)>();
				var revSectors = new List<List<StandardMfmFormat.SectorLocation>>();
				for (int rev = 0; rev < revs; rev++)
				{
					byte[] p = DecodeRevolution(d, tdh, rev, revs, tickNs, cellTimeNs, out int cc);
					if (p == null || cc == 0) { revCells.Add((null, 0)); revSectors.Add(new List<StandardMfmFormat.SectorLocation>()); continue; }
					revCells.Add((p, cc));
					revSectors.Add(StandardMfmFormat.DecodeSectorLocations(new MfmTrack(p, cc)));
				}

				// the FDC reads whichever revolution recovered the most sectors cleanly (a marginal read on one
				// pass is often clean on another); rev 0 wins ties.
				int baseRev = -1, bestGood = -1;
				for (int rev = 0; rev < revs; rev++)
				{
					if (revCells[rev].packed == null) continue;
					int good = 0;
					foreach (var loc in revSectors[rev]) if (loc.Sector.IdCrcOk && loc.Sector.DataCrcOk) good++;
					if (good > bestGood) { bestGood = good; baseRev = rev; }
				}
				if (baseRev < 0) continue; // no revolution decoded

				var (basePacked, baseCells) = revCells[baseRev];
				// weak-cell mask: bytes of a not-cleanly-read sector that differ across revolutions
				byte[] weak = revs > 1 ? ComputeWeakMask(revSectors, baseRev, baseCells) : null;

				var (cyl, head) = MapTrack(t, headMode);
				disk.SetTrack(cyl, head, new MfmTrack(basePacked, baseCells, weak));
			}
			return disk;
		}

		/// <summary>Decode one revolution's flux into packed MFM cells. Returns null if the revolution is
		/// missing/out of range.</summary>
		private static byte[] DecodeRevolution(byte[] d, int tdh, int rev, int revs, long tickNs, long cellTimeNs, out int cellCount)
		{
			cellCount = 0;
			if (rev < 0 || rev >= revs) return null;
			// per-rev triplet after the 4-byte "TRK"+trackNo header: [index-time(4)][flux-count(4)][data-offset(4)]
			int tripletBase = tdh + 4 + rev * 12;
			if (tripletBase + 12 > d.Length) return null;
			int trackLen = ReadLe32(d, tripletBase + 4);    // flux transition count
			int dataOffset = ReadLe32(d, tripletBase + 8);  // relative to the TDH start
			if (trackLen <= 0 || tdh + dataOffset >= d.Length) return null;
			return FluxToCells(d, tdh + dataOffset, trackLen, tickNs, cellTimeNs, out cellCount);
		}

		/// <summary>
		/// Build a weak-cell mask for the chosen base revolution's track: for each base sector that does NOT
		/// read cleanly (its data CRC fails), compare its bytes against the same sector decoded on the other
		/// revolutions; bytes that differ across revolutions are weak (the flux there is unstable = the disk's
		/// deliberate weak-sector protection). Solid (CRC-OK) sectors are left alone so our own decode jitter
		/// cannot corrupt stable data. Returns null if nothing is weak.
		/// </summary>
		private static byte[] ComputeWeakMask(List<List<StandardMfmFormat.SectorLocation>> revSectors, int baseRev, int baseCells)
		{
			var baseLocs = revSectors[baseRev];
			if (baseLocs.Count == 0) return null;

			// gather each sector R's data copies across all revolutions
			var copies = new Dictionary<byte, List<byte[]>>();
			for (int rev = 0; rev < revSectors.Count; rev++)
				foreach (var loc in revSectors[rev])
				{
					if (!copies.TryGetValue(loc.Sector.R, out var l)) { l = new List<byte[]>(); copies[loc.Sector.R] = l; }
					l.Add(loc.Sector.Data);
				}

			var weakBits = new bool[baseCells];
			bool any = false;
			foreach (var loc in baseLocs)
			{
				// leave solid reads alone; only a sector that already fails CRC is a weak-sector candidate
				if (loc.Sector.DataCrcOk) continue;
				if (!copies.TryGetValue(loc.Sector.R, out var cps) || cps.Count < 2) continue;

				int size = loc.Sector.SizeBytes;
				var b0 = loc.Sector.Data;
				for (int j = 0; j < size; j++)
				{
					// flag this byte's 16 cells weak (data start + j bytes, each byte is 16 cells). Stop at the
					// end of the track: an OVERSIZED/overrun sector (e.g. a protection track's single N=6 = 8192
					// -byte sector on a ~6250-byte track) declares far more data than physically fits, so j*16
					// runs past the track. It must NOT wrap - wrapping would flag the start-of-track sync/IDAM
					// cells weak and make the sector impossible to find (its own address mark becomes fuzzy).
					int cellBase = loc.DataStartCell + j * 16;
					if (cellBase + 16 > baseCells) break;

					byte v0 = j < b0.Length ? b0[j] : (byte)0;
					bool differs = false;
					for (int c = 0; c < cps.Count && !differs; c++)
					{
						byte vc = j < cps[c].Length ? cps[c][j] : (byte)0;
						if (vc != v0) differs = true;
					}
					if (!differs) continue;
					for (int k = 0; k < 16; k++) weakBits[cellBase + k] = true;
					any = true;
				}
			}
			if (!any) return null;

			var weak = new byte[(baseCells + 7) >> 3];
			for (int i = 0; i < baseCells; i++)
				if (weakBits[i]) weak[i >> 3] |= (byte)(1 << (i & 7));
			return weak;
		}

		private static (int cyl, int head) MapTrack(int track, int headMode)
			// SCP track slots are ALWAYS physical (track = cylinder*2 + head): even slots = side 0, odd = side 1,
			// even for a single-sided image (which just leaves the other side's slots empty). So the cylinder is
			// always track>>1; headMode only fixes which head a single-sided image's slots belong to.
			=> headMode switch
			{
				1 => (track >> 1, 0),         // side 0 only: data lives in the even slots (0,2,4,…)
				2 => (track >> 1, 1),         // side 1 only: data lives in the odd slots (1,3,5,…)
				_ => (track >> 1, track & 1), // both heads interleaved
			};

		// Decode a track's flux into MFM cells (LSB-first packed, matching MfmTrack) with a software PLL data
		// separator. A real drive dump does not run at exactly the nominal cell time (these +3 dumps sit ~2.5%
		// slow, ~2050 ns instead of 2000 ns) AND has per-transition jitter. A rigid grid mis-rounds intervals
		// near the 2/3-cell boundary; a single slipped cell desyncs the rest of the field, so short ID fields
		// survive but long 512-byte data fields fail CRC. The separator therefore (1) seeds a per-track cell
		// estimate from the flux itself, then (2) runs a slow INTEGRAL loop that tracks the true bit-cell rate
		// - correcting slow spindle/rate drift while AVERAGING OUT the jitter, so a noisy interval does not slip
		// a cell. A proportional/high-gain loop instead chases the jitter and slips more (measured on real +3
		// dumps: this slow integral loop recovers markedly more standard sectors than the old proportional nudge).
		private static byte[] FluxToCells(byte[] d, int start, int fluxCount, long tickNs, long cellTimeNs, out int cellCount)
		{
			// gather the flux intervals (ns), folding the SCP 0x0000 overflow markers into the next interval
			var flux = new List<long>(fluxCount);
			long carry = 0;
			int p = start;
			for (int i = 0; i < fluxCount; i++)
			{
				if (p + 2 > d.Length) break;
				int v = (d[p] << 8) | d[p + 1]; // 16-bit big-endian
				p += 2;
				if (v == 0) { carry += 65536; continue; }
				flux.Add((carry + v) * tickNs);
				carry = 0;
			}

			// seed the cell estimate from this track's own flux: total time / total cell count, iterated so the
			// classification settles (ignoring long gap/index intervals so they don't skew the average).
			double cell = cellTimeNs;
			for (int iter = 0; iter < 4; iter++)
			{
				long sumNs = 0, sumCells = 0;
				foreach (var f in flux)
				{
					int n = (int)(f / cell + 0.5);
					if (n < 1) n = 1;
					if (n > 5) continue; // skip gaps/index for the rate estimate
					sumNs += f;
					sumCells += n;
				}
				if (sumCells == 0) break;
				double est = (double)sumNs / sumCells;
				// keep the estimate sane (a dump won't be off by more than ~15%)
				if (est < cellTimeNs * 0.85) est = cellTimeNs * 0.85;
				else if (est > cellTimeNs * 1.15) est = cellTimeNs * 1.15;
				cell = est;
			}

			// PLL pass: an integral loop tracks the bit-cell period. `centre` (the seed estimate) stays fixed;
			// `freq` accumulates the per-cell rate error so the working `period` follows slow rate drift with
			// zero steady-state error, while the tiny gain averages out per-transition jitter (a large gain
			// would chase the jitter and slip cells). Period clamped +/-20% of the estimate so noise/gaps can't
			// drag it away. ki tuned on real +3 dumps (0.01 = best recovery; 0 = fixed grid, higher = jitter-led).
			const double ki = 0.01;
			double centre = cell, freq = 0.0;
			double lo = centre * 0.80, hi = centre * 1.20;
			var cells = new List<bool>(fluxCount * 3);
			foreach (var f in flux)
			{
				double period = centre + freq;
				if (period < lo) period = lo; else if (period > hi) period = hi;
				int n = (int)(f / period + 0.5);
				if (n < 1) n = 1;
				if (n <= 5) // only track the loop on real data intervals, not long gaps/index
					freq += ((double)f / n - period) * ki;
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
