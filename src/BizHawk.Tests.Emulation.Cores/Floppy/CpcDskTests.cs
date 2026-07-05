using System.IO;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>
	/// Tests for the DSK/EDSK reader and its conversion into MFM flux: a self-contained synthetic EDSK
	/// (always runs) plus a real EDSK game image with weak sectors (RoboCop, skipped if the local file is
	/// absent - it is a copyrighted image kept out of the repo).
	/// </summary>
	[TestClass]
	public sealed class CpcDskTests
	{
		[TestMethod]
		public void SyntheticEdsk_Parses_Converts_And_RoundTrips()
		{
			// sector 1: a normal 256-byte sector
			var normal = new byte[256];
			for (int i = 0; i < 256; i++) normal[i] = (byte)(i ^ 0x5A);

			// sector 2: a weak sector stored as 3 copies that disagree only at bytes 10..14
			byte[] Copy(int variant)
			{
				var a = new byte[256];
				for (int i = 0; i < 256; i++) a[i] = (byte)i;
				if (variant > 0) for (int p = 10; p < 15; p++) a[p] = (byte)(variant * 40 + p);
				return a;
			}
			var weak = Copy(0).Concat(Copy(1)).Concat(Copy(2)).ToArray(); // 768 bytes = 3 x 256

			byte[] edsk = BuildEdsk(
				cyl: 0, side: 0, trackN: 1,
				sectors: new[]
				{
					(C: (byte)0, H: (byte)0, R: (byte)1, N: (byte)1, st1: (byte)0, st2: (byte)0, data: normal),
					(C: (byte)0, H: (byte)0, R: (byte)2, N: (byte)1, st1: (byte)0, st2: (byte)0, data: weak),
				});

			Assert.IsTrue(CpcDskConverter.IsCpcDsk(edsk));
			var disk = CpcDskConverter.Parse(edsk);
			Assert.IsTrue(disk.Extended);
			Assert.AreEqual(1, disk.Tracks.Count);

			var track = disk.Tracks[0];
			Assert.AreEqual(2, track.Sectors.Count);
			Assert.IsNull(track.Sectors[0].WeakCopies, "sector 1 is not weak");
			Assert.IsNotNull(track.Sectors[1].WeakCopies, "sector 2 is weak");
			Assert.AreEqual(3, track.Sectors[1].WeakCopies.Length);

			// build the flux track, decode it back
			var flux = track.BuildFlux();
			var rng = new Random(12345);
			var dec = StandardMfmFormat.DecodeSectors(flux, rng);
			Assert.AreEqual(2, dec.Count);

			// normal sector: exact round-trip incl. CRCs
			var s1 = dec.Single(x => x.R == 1);
			Assert.IsTrue(s1.IdCrcOk && s1.DataCrcOk, "normal sector CRCs");
			CollectionAssert.AreEqual(normal, s1.Data, "normal sector data");

			// weak sector: stable outside the weak window, varies inside it across repeated reads
			var seenAt12 = new System.Collections.Generic.HashSet<byte>();
			for (int pass = 0; pass < 30; pass++)
			{
				var s2 = StandardMfmFormat.DecodeSectors(flux, rng).Single(x => x.R == 2);
				Assert.AreEqual((byte)0, s2.Data[0], "weak sector: byte 0 is stable");
				Assert.AreEqual((byte)5, s2.Data[5], "weak sector: byte 5 is stable");
				seenAt12.Add(s2.Data[12]);
			}
			Assert.IsTrue(seenAt12.Count > 1, "weak sector: byte 12 must vary across reads");
		}

		[TestMethod]
		public void RoboCop_Edsk_Loads_RoundTrips_AndHasWeakSectors()
		{
			string path = Path.Combine(
				Path.GetDirectoryName(typeof(CpcDskTests).Assembly.Location)!, "Resources", "disk", "RoboCop(Fixed).dsk");
			if (!File.Exists(path))
			{
				Assert.Inconclusive($"test disk not present (copyrighted, kept local): {path}");
				return;
			}

			var bytes = File.ReadAllBytes(path);
			Assert.IsTrue(CpcDskConverter.IsCpcDsk(bytes), "recognised as CPC DSK/EDSK");
			var disk = CpcDskConverter.Parse(bytes);
			Assert.IsTrue(disk.Extended, "RoboCop(Fixed) is EDSK");
			Assert.IsTrue(disk.Tracks.Count > 0, "tracks parsed");

			int totalSectors = 0, decodedBack = 0, weakSectors = 0;
			var rng = new Random(1);
			foreach (var t in disk.Tracks)
			{
				totalSectors += t.Sectors.Count;
				weakSectors += t.Sectors.Count(s => s.WeakCopies is { Length: > 1 });

				var dec = StandardMfmFormat.DecodeSectors(t.BuildFlux(), rng);
				foreach (var ps in t.Sectors)
					if (dec.Exists(x => x.C == ps.C && x.H == ps.H && x.R == ps.R && x.N == ps.N))
						decodedBack++;
			}

			Assert.IsTrue(totalSectors > 0, "sectors parsed");
			Assert.AreEqual(totalSectors, decodedBack, "every parsed sector decodes back from its flux track");
			Assert.IsTrue(weakSectors > 0, "RoboCop(Fixed) is expected to contain weak sectors");

			// a weak sector must read differently across passes
			var weakTrack = disk.Tracks.First(t => t.Sectors.Exists(s => s.WeakCopies is { Length: > 1 }));
			var wflux = weakTrack.BuildFlux();
			byte weakR = weakTrack.Sectors.First(s => s.WeakCopies is { Length: > 1 }).R;
			var datas = new System.Collections.Generic.List<byte[]>();
			for (int pass = 0; pass < 20; pass++)
				datas.Add(StandardMfmFormat.DecodeSectors(wflux, rng).First(x => x.R == weakR).Data);
			bool anyDiffer = datas.Skip(1).Any(d => !d.SequenceEqual(datas[0]));
			Assert.IsTrue(anyDiffer, "weak sector must vary across reads");
		}

		// Build a minimal single-sided EDSK byte image from a sector list.
		private static byte[] BuildEdsk(int cyl, int side, byte trackN,
			(byte C, byte H, byte R, byte N, byte st1, byte st2, byte[] data)[] sectors)
		{
			int dataArea = sectors.Sum(s => s.data.Length);
			int trackTotal = (256 + dataArea + 255) & ~255; // TIB + data, rounded up to 256
			var buf = new byte[256 + trackTotal];

			var ident = Encoding.ASCII.GetBytes("EXTENDED CPC DSK File\r\nDisk-Info\r\n");
			Array.Copy(ident, buf, ident.Length);
			buf[0x30] = 1;                          // track count
			buf[0x31] = 1;                          // side count
			buf[0x34] = (byte)(trackTotal / 256);   // track-size-table entry 0

			int to = 0x100;
			var tident = Encoding.ASCII.GetBytes("Track-Info\r\n");
			Array.Copy(tident, 0, buf, to, tident.Length);
			buf[to + 0x10] = (byte)cyl;
			buf[to + 0x11] = (byte)side;
			buf[to + 0x14] = trackN;
			buf[to + 0x15] = (byte)sectors.Length;
			buf[to + 0x16] = 0x4E;                  // GAP3
			buf[to + 0x17] = 0xE5;                  // filler

			int si = to + 0x18;
			int dp = to + 0x100;
			foreach (var s in sectors)
			{
				buf[si] = s.C; buf[si + 1] = s.H; buf[si + 2] = s.R; buf[si + 3] = s.N;
				buf[si + 4] = s.st1; buf[si + 5] = s.st2;
				buf[si + 6] = (byte)(s.data.Length & 0xFF);
				buf[si + 7] = (byte)(s.data.Length >> 8);
				si += 8;
				Array.Copy(s.data, 0, buf, dp, s.data.Length);
				dp += s.data.Length;
			}
			return buf;
		}
	}
}
