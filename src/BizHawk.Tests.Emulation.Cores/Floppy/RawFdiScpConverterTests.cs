using System.Collections.Generic;

using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>
	/// Tests for the raw-sector, FDI and SCP loaders. Each is validated by constructing a synthetic image
	/// (per the format spec) and confirming sectors decode from the resulting flux; SCP additionally checks
	/// the flux-quantizer via a full cell round-trip.
	/// </summary>
	[TestClass]
	public sealed class RawFdiScpConverterTests
	{
		[TestMethod]
		public void Raw_Plus3Geometry_LaysOutSectorsSequentially()
		{
			var g = DiskGeometry.Plus3;
			var data = new byte[g.TotalBytes];
			// give each sector a recognizable fill: byte value = cylinder for the whole sector
			int pos = 0;
			for (int cyl = 0; cyl < g.Cylinders; cyl++)
				for (int s = 0; s < g.SectorsPerTrack; s++)
				{
					for (int i = 0; i < g.SectorSize; i++) data[pos + i] = (byte)cyl;
					pos += g.SectorSize;
				}

			var disk = RawSectorConverter.ToFluxDisk(data, g);
			Assert.AreEqual(40, disk.Cylinders);

			var t7 = StandardMfmFormat.DecodeSectors(disk.GetTrack(7, 0));
			int good = 0;
			foreach (var s in t7) if (s.IdCrcOk && s.DataCrcOk && s.SizeBytes == 512 && s.Data[0] == 7) good++;
			Assert.AreEqual(9, good, "track 7 has nine good 512-byte sectors filled with 0x07");
			Assert.IsNotNull(t7.Find(s => s.R == 1), "sector ids start at 1");
			Assert.IsNotNull(t7.Find(s => s.R == 9));
		}

		[TestMethod]
		public void Fdi_SectorImage_DecodesWithFlagsAndData()
		{
			// one cylinder, one head, two sectors (R=1 normal, R=2 deleted). Layout: header, one track
			// header (offset 0, 2 sectors), then the data area with the two sectors.
			const int dataArea = 0x0E + 0 /*extra*/ + 7 + 2 * 7; // header + track header + 2 sector descs
			var f = new List<byte>();
			// main header (14 bytes)
			f.AddRange(new byte[] { (byte)'F', (byte)'D', (byte)'I', 0x00 }); // sig + write enabled
			AddLe16(f, 1);           // cylinders
			AddLe16(f, 1);           // heads
			AddLe16(f, 0);           // description offset (unused)
			AddLe16(f, dataArea);    // data offset
			AddLe16(f, 0);           // extra header length
			// track header: trackOffset=0 (4), reserved (2), sectorCount=2 (1)
			AddLe32(f, 0);
			AddLe16(f, 0);
			f.Add(2);
			// sector descriptors (C,H,R,N,flags,offsetLo,offsetHi)
			f.AddRange(new byte[] { 0, 0, 1, 2, 0x04, 0x00, 0x00 });     // R=1, N=2, flags bit2 set (crc ok), offset 0
			f.AddRange(new byte[] { 0, 0, 2, 2, 0x84, 0x00, 0x02 });     // R=2, deleted (0x80)+crc-ok, offset 512
			// data area: sector 1 (0xAA x512), sector 2 (0xBB x512)
			for (int i = 0; i < 512; i++) f.Add(0xAA);
			for (int i = 0; i < 512; i++) f.Add(0xBB);

			var disk = FdiConverter.ToFluxDisk(f.ToArray());
			var t = StandardMfmFormat.DecodeSectors(disk.GetTrack(0, 0));

			var s1 = t.Find(s => s.R == 1);
			Assert.IsNotNull(s1);
			Assert.IsTrue(s1.IdCrcOk && s1.DataCrcOk);
			Assert.IsFalse(s1.Deleted);
			Assert.AreEqual((byte)0xAA, s1.Data[0]);

			var s2 = t.Find(s => s.R == 2);
			Assert.IsNotNull(s2);
			Assert.IsTrue(s2.Deleted, "sector 2 recorded with a deleted address mark");
			Assert.AreEqual((byte)0xBB, s2.Data[0]);
		}

		[TestMethod]
		public void Scp_FluxRoundTrip_RecoversSectors()
		{
			// synthesize a 9-sector track, express it as SCP flux, then load it back and decode
			var secs = new List<TrackSector>();
			for (int r = 1; r <= 9; r++)
				secs.Add(new TrackSector { C = 2, H = 0, R = (byte)r, N = 2, Data = Fill(512, (byte)(0x10 + r)) });
			var track = StandardMfmFormat.BuildStandardTrack(secs);

			byte[] scp = BuildScpSingleTrack(track, trackIndex: 4);
			var disk = ScpConverter.ToFluxDisk(scp);
			// SCP track slots are physical (cyl*2+head), so slot 4 (side 0) maps to cylinder 2 — which matches
			// the C=2 recorded in these sectors' ID headers.
			var loaded = disk.GetTrack(2, 0);
			Assert.IsNotNull(loaded, "SCP track slot 4 (side 0) maps to cylinder 2");

			var decoded = StandardMfmFormat.DecodeSectors(loaded);
			int good = 0;
			foreach (var s in decoded) if (s.IdCrcOk && s.DataCrcOk && s.C == 2 && s.SizeBytes == 512) good++;
			Assert.AreEqual(9, good, "all nine sectors recovered from SCP flux");
			Assert.AreEqual((byte)0x15, decoded.Find(s => s.R == 5).Data[0], "sector data survived the flux round-trip");
		}

		// Emit an MfmTrack's cells as one revolution of SCP flux (side 0). Resolution 0 (25ns), 2us cells:
		// a transition every n cells => n*2000ns => n*80 ticks.
		private static byte[] BuildScpSingleTrack(MfmTrack track, int trackIndex)
		{
			const long cellTimeNs = 2000, tickNs = 25;
			var flux = new List<byte>();
			int fluxCount = 0, prev = -1;
			for (int i = 0; i < track.CellCount; i++)
			{
				if (!track.GetCell(i)) continue;
				int n = i - prev;
				prev = i;
				long ticks = n * cellTimeNs / tickNs;
				flux.Add((byte)(ticks >> 8));
				flux.Add((byte)ticks);
				fluxCount++;
			}

			int tdh = 0x10 + 168 * 4; // header + full offset table
			var f = new byte[tdh + 16 + flux.Count];
			f[0] = (byte)'S'; f[1] = (byte)'C'; f[2] = (byte)'P';
			f[0x05] = 1;                 // 1 revolution
			f[0x06] = (byte)trackIndex;  // start track
			f[0x07] = (byte)trackIndex;  // end track
			f[0x0A] = 1;                 // side 0 only
			f[0x0B] = 0;                 // 25ns resolution

			int entry = 0x10 + trackIndex * 4;
			f[entry] = (byte)tdh; f[entry + 1] = (byte)(tdh >> 8); f[entry + 2] = (byte)(tdh >> 16); f[entry + 3] = (byte)(tdh >> 24);

			f[tdh] = (byte)'T'; f[tdh + 1] = (byte)'R'; f[tdh + 2] = (byte)'K'; f[tdh + 3] = (byte)trackIndex;
			WriteLe32(f, tdh + 4, 0);              // index duration (unused here)
			WriteLe32(f, tdh + 8, fluxCount);      // track length in flux transitions
			WriteLe32(f, tdh + 12, 16);            // data offset from TDH start
			for (int i = 0; i < flux.Count; i++) f[tdh + 16 + i] = flux[i];
			return f;
		}

		private static void AddLe16(List<byte> f, int v) { f.Add((byte)v); f.Add((byte)(v >> 8)); }
		private static void AddLe32(List<byte> f, int v) { f.Add((byte)v); f.Add((byte)(v >> 8)); f.Add((byte)(v >> 16)); f.Add((byte)(v >> 24)); }
		private static void WriteLe32(byte[] b, int o, int v) { b[o] = (byte)v; b[o + 1] = (byte)(v >> 8); b[o + 2] = (byte)(v >> 16); b[o + 3] = (byte)(v >> 24); }

		private static byte[] Fill(int n, byte v)
		{
			var a = new byte[n];
			for (int i = 0; i < n; i++) a[i] = v;
			return a;
		}
	}
}
