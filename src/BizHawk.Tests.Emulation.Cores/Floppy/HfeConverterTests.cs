using System.Collections.Generic;

using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>
	/// Tests for the HFE (HxC) loader: a synthetic HFE with known bytes proves the LSB-first cell
	/// order and the 256-byte side interleave against the spec, and a full round-trip (a synthesized 9-sector
	/// track written into HFE and loaded back) proves sectors decode from the loaded flux.
	/// </summary>
	[TestClass]
	public sealed class HfeConverterTests
	{
		[TestMethod]
		public void Load_SyntheticHfe_UnpacksLsbFirstAndDeinterleavesSides()
		{
			// two tracks, two sides. Side 0's first cell byte = 0x01 (LSB first => cell 0 set, 1..7 clear);
			// side 1's first byte = 0x80 (cell 0 clear ... cell 7 set). Track data is one 512-byte block:
			// [0..255] side 0, [256..511] side 1.
			var hfe = new byte[512 + 512 + 512];
			WriteSig(hfe, "HXCPICFE");
			hfe[0x009] = 1;           // number_of_track
			hfe[0x00A] = 2;           // number_of_side
			hfe[0x00B] = 0x00;        // ISOIBM_MFM
			hfe[0x00C] = 250; hfe[0x00D] = 0; // bitRate 250
			hfe[0x012] = 1; hfe[0x013] = 0;   // track_list_offset = 1 block => 0x200
			hfe[0x014] = 0x00;        // write protected

			// LUT entry 0: offset = 2 blocks (0x400), track_len = 512
			int lut = 0x200;
			hfe[lut + 0] = 2; hfe[lut + 1] = 0;      // offset (blocks)
			hfe[lut + 2] = 0x00; hfe[lut + 3] = 0x02; // len = 512

			int data = 0x400;
			hfe[data + 0] = 0x01;     // side 0, first cell byte
			hfe[data + 256] = 0x80;   // side 1, first cell byte

			var (tracks, wp) = HfeConverter.Parse(hfe);
			Assert.IsTrue(wp, "write protected flag decoded");
			Assert.AreEqual(2, tracks.Count, "one cylinder, two sides");

			var s0 = tracks.Find(t => t.Side == 0).Track;
			Assert.IsTrue(s0.GetCell(0), "side0 cell 0 set (0x01 LSB first)");
			Assert.IsFalse(s0.GetCell(1), "side0 cell 1 clear");

			var s1 = tracks.Find(t => t.Side == 1).Track;
			Assert.IsFalse(s1.GetCell(0), "side1 cell 0 clear (0x80 LSB first)");
			Assert.IsTrue(s1.GetCell(7), "side1 cell 7 set");
		}

		[TestMethod]
		public void RoundTrip_NineSectorTrack_ThroughHfe_DecodesAllSectors()
		{
			var secs = new List<TrackSector>();
			for (int r = 1; r <= 9; r++)
				secs.Add(new TrackSector { C = 3, H = 0, R = (byte)r, N = 2, Data = Fill(512, (byte)(0x40 + r)) });
			var source = StandardMfmFormat.BuildStandardTrack(secs);

			byte[] hfe = BuildHfeSingleSided(source, cylinder: 3, cylinders: 4);
			var disk = HfeConverter.ToFluxDisk(hfe);
			var track = disk.GetTrack(3, 0);
			Assert.IsNotNull(track, "cylinder 3 loaded from HFE");

			var decoded = StandardMfmFormat.DecodeSectors(track);
			int good = 0;
			foreach (var s in decoded)
				if (s.IdCrcOk && s.DataCrcOk && s.C == 3 && s.SizeBytes == 512) good++;
			Assert.AreEqual(9, good, "all nine sectors decode from the HFE-loaded flux");

			var s5 = decoded.Find(s => s.R == 5);
			Assert.IsNotNull(s5);
			Assert.AreEqual((byte)(0x40 + 5), s5.Data[0], "sector data survived the HFE round-trip");
		}

		// Pack an MfmTrack's cells into a single-sided HFE (side 0 only; side-1 halves left blank).
		private static byte[] BuildHfeSingleSided(MfmTrack track, int cylinder, int cylinders)
		{
			int cells = track.CellCount;
			int sideBytes = (cells + 7) / 8;
			var side0 = new byte[sideBytes];
			for (int i = 0; i < cells; i++)
				if (track.GetCell(i)) side0[i >> 3] |= (byte)(1 << (i & 7)); // LSB first, same as HFE

			// interleave into 512-byte blocks: [256 side0][256 side1(blank)]
			var blocks = new List<byte>();
			for (int p = 0; p < sideBytes; p += 256)
			{
				int chunk = System.Math.Min(256, sideBytes - p);
				var block = new byte[512];
				System.Array.Copy(side0, p, block, 0, chunk);
				blocks.AddRange(block);
			}
			int trackLen = blocks.Count; // combined length (side1 halves are present but blank)

			int lutOffset = 512;
			int dataOffset = 1024;
			var hfe = new byte[dataOffset + blocks.Count];
			WriteSig(hfe, "HXCPICFE");
			hfe[0x009] = (byte)cylinders;
			hfe[0x00A] = 1;         // single sided
			hfe[0x00B] = 0x00;      // MFM
			hfe[0x00C] = 250;
			hfe[0x012] = 1;         // LUT at block 1
			hfe[0x014] = 0xFF;      // unprotected

			int lut = lutOffset + cylinder * 4;
			hfe[lut + 0] = (byte)(dataOffset / 512);
			hfe[lut + 1] = (byte)((dataOffset / 512) >> 8);
			hfe[lut + 2] = (byte)trackLen;
			hfe[lut + 3] = (byte)(trackLen >> 8);
			for (int i = 0; i < blocks.Count; i++) hfe[dataOffset + i] = blocks[i];
			return hfe;
		}

		private static void WriteSig(byte[] d, string sig)
		{
			for (int i = 0; i < 8; i++) d[i] = (byte)sig[i];
		}

		private static byte[] Fill(int n, byte v)
		{
			var a = new byte[n];
			for (int i = 0; i < n; i++) a[i] = v;
			return a;
		}
	}
}
