using System;
using System.Collections.Generic;
using System.Text;

using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>
	/// Tests for copy-protection detection and the Speedlock weak-sector synthesis. Synthesis must ONLY
	/// apply to a plain dump that lacks weak data - images that already carry it (EDSK multi-copy, IPF/UDI
	/// fuzzy) are left alone.
	/// </summary>
	[TestClass]
	public sealed class DiskProtectionTests
	{
		private static CpcDskConverter.ParsedDisk MakeSpeedlockDisk(bool withExistingWeak)
		{
			var t0 = new CpcDskConverter.ParsedTrack { Cylinder = 0, Side = 0 };

			var s0 = new byte[512];
			var sig = Encoding.ASCII.GetBytes("SPEEDLOCK");
			Array.Copy(sig, 0, s0, 304, sig.Length); // signature offset per SamDisk
			t0.Sectors.Add(new TrackSector { C = 0, H = 0, R = 1, N = 2, Data = s0 });

			var s1 = new byte[512];
			for (int i = 0; i < 512; i++) s1[i] = (byte)(i * 7); // non-filler -> whole sector weak
			// the genuine Speedlock weak sector always carries a data CRC error in the dump
			var weak = new TrackSector { C = 0, H = 0, R = 2, N = 2, Data = s1, DataCrcError = true };
			if (withExistingWeak)
			{
				var alt = (byte[])s1.Clone();
				for (int i = 0; i < 512; i++) alt[i] ^= 0x5A;
				weak.WeakCopies = new[] { s1, alt }; // image already carries weak data
			}
			t0.Sectors.Add(weak);

			for (byte r = 3; r <= 9; r++)
				t0.Sectors.Add(new TrackSector { C = 0, H = 0, R = r, N = 2, Data = new byte[512] });

			var disk = new CpcDskConverter.ParsedDisk();
			disk.Tracks.Add(t0);
			return disk;
		}

		[TestMethod]
		public void Speedlock_PlainDsk_SynthesizesWeakSector()
		{
			var disk = MakeSpeedlockDisk(withExistingWeak: false);
			Assert.IsNull(disk.Tracks[0].Sectors[1].WeakCopies, "no weak data before synthesis");

			CpcDskConverter.ApplySpeedlockWeakSynthesis(disk);

			var weak = disk.Tracks[0].Sectors[1];
			Assert.IsNotNull(weak.WeakCopies, "weak copies synthesized for the plain dump");
			Assert.AreEqual(3, weak.WeakCopies.Length);

			// the weak sector now reads differently across passes and fails its data CRC (as Speedlock expects)
			var flux = disk.Tracks[0].BuildFlux();
			var readA = StandardMfmFormat.ReadSectorById(flux, 0, 0, 2, 2, new WeakBitRng(1));
			var readB = StandardMfmFormat.ReadSectorById(flux, 0, 0, 2, 2, new WeakBitRng(99));
			Assert.IsNotNull(readA);
			Assert.IsFalse(readA.DataCrcOk, "weak sector reads with a data CRC error");
			CollectionAssert.AreNotEqual(readA.Data, readB.Data, "weak sector varies between reads");
		}

		[TestMethod]
		public void Speedlock_ImageWithBakedInWeak_IsNotResynthesized()
		{
			var disk = MakeSpeedlockDisk(withExistingWeak: true);
			var before = disk.Tracks[0].Sectors[1].WeakCopies;

			CpcDskConverter.ApplySpeedlockWeakSynthesis(disk);

			Assert.AreSame(before, disk.Tracks[0].Sectors[1].WeakCopies,
				"existing weak data must be left untouched - synthesis only fills a gap");
		}

		[TestMethod]
		public void Detect_ReportsSpeedlockOnRealDumps_AndNoneOnUtilityDisk()
		{
			// EDSK Speedlock game: detected as Speedlock, weak already present so synthesis is a no-op
			CheckReal("RoboCop(Fixed).dsk", FluxDisk.FromCpcDsk, DiskProtectionScheme.Speedlock);
			// IPF Speedlock game (fuzzy/geometry baked in): detected as Speedlock
			CheckReal("MidnightResistance.ipf", FluxDisk.FromIpf, DiskProtectionScheme.Speedlock);
			// CP/M utility disk: no protection
			CheckReal("ProfiCPM.udi", FluxDisk.FromUdi, DiskProtectionScheme.None);
		}

		[TestMethod]
		public void RoboCopPlainDsk_UndumpedWeak_SynthesisMakesItVary()
		{
			string path = System.IO.Path.Combine(
				System.IO.Path.GetDirectoryName(typeof(DiskProtectionTests).Assembly.Location)!, "Resources", "disk", "RoboCopPlain.dsk");
			if (!System.IO.File.Exists(path)) { Assert.Inconclusive($"test disk not present: {path}"); return; }
			var bytes = System.IO.File.ReadAllBytes(path);

			Assert.AreEqual(DiskProtectionScheme.Speedlock, DiskProtection.Detect(FluxDisk.FromCpcDsk(bytes)));

			// WITHOUT synthesis: the weak sector (R=2) was dumped as a single copy, so it reads identically
			// every pass - Speedlock's "must differ between reads" check fails.
			var parsed = CpcDskConverter.Parse(bytes);
			var rawTrack0 = parsed.Tracks.Find(t => t.Cylinder == 0 && t.Side == 0).BuildFlux();
			var raw1 = StandardMfmFormat.ReadSectorById(rawTrack0, 0, 0, 2, 2, new WeakBitRng(1));
			var raw2 = StandardMfmFormat.ReadSectorById(rawTrack0, 0, 0, 2, 2, new WeakBitRng(2));
			Assert.IsNotNull(raw1);
			CollectionAssert.AreEqual(raw1.Data, raw2.Data, "un-synthesized weak sector reads identically (protection would fail)");

			// WITH synthesis (FluxDisk.FromCpcDsk applies it): the sector now varies between reads and errors,
			// so the Speedlock check passes.
			var track0 = FluxDisk.FromCpcDsk(bytes).GetTrack(0, 0);
			var s1 = StandardMfmFormat.ReadSectorById(track0, 0, 0, 2, 2, new WeakBitRng(1));
			var s2 = StandardMfmFormat.ReadSectorById(track0, 0, 0, 2, 2, new WeakBitRng(2));
			Assert.IsNotNull(s1);
			CollectionAssert.AreNotEqual(s1.Data, s2.Data, "synthesized weak sector varies between reads");
			Assert.IsFalse(s1.DataCrcOk, "and reads with a data CRC error, as Speedlock expects");
		}

		[TestMethod]
		public void Detect_RecognizesTheDocumentedSchemesFromStructure()
		{
			// Rainbow Arts: 9 sectors, one with the non-standard id 198 + data CRC error
			var ra = new List<TrackSector>();
			for (int i = 0; i < 9; i++)
				ra.Add(new TrackSector { C = 0, H = 0, R = (byte)(i == 1 ? 198 : i + 1), N = 2, Data = new byte[512], DataCrcError = i == 1 });
			Assert.AreEqual(DiskProtectionScheme.RainbowArts, DiskProtection.Detect(OneTrack(ra)));

			// KBI: 10 sectors, final 256-byte sector with a data CRC error and "Kxx" signature
			var kbi = new List<TrackSector>();
			for (int i = 0; i < 9; i++) kbi.Add(new TrackSector { C = 0, H = 0, R = (byte)(i + 1), N = 2, Data = new byte[512] });
			kbi.Add(new TrackSector { C = 0, H = 0, R = 10, N = 1, Data = Ascii("KBI stuff", 256), DataCrcError = true });
			Assert.AreEqual(DiskProtectionScheme.Kbi, DiskProtection.Detect(OneTrack(kbi)));

			// Prehistorik: a 4K sector (size code 5) with a "Titus" signature at 0x1b
			var pre = new List<TrackSector>();
			for (int i = 0; i < 9; i++) pre.Add(new TrackSector { C = 0, H = 0, R = (byte)(i + 1), N = 2, Data = new byte[512] });
			var big = new byte[4096];
			var titus = Encoding.ASCII.GetBytes("Titus");
			Array.Copy(titus, 0, big, 0x1b, titus.Length);
			pre.Add(new TrackSector { C = 0, H = 0, R = 12, N = 5, Data = big, DataCrcError = true });
			Assert.AreEqual(DiskProtectionScheme.Prehistorik, DiskProtection.Detect(OneTrack(pre)));

			// Logo Professor: 10 sectors whose ids start at 2
			var logo = new List<TrackSector>();
			for (int i = 0; i < 10; i++) logo.Add(new TrackSector { C = 0, H = 0, R = (byte)(i + 2), N = 2, Data = new byte[512] });
			Assert.AreEqual(DiskProtectionScheme.LogoProfessor, DiskProtection.Detect(OneTrack(logo)));

			// A plain 9-sector data disk is not flagged
			var plain = new List<TrackSector>();
			for (int i = 0; i < 9; i++) plain.Add(new TrackSector { C = 0, H = 0, R = (byte)(i + 1), N = 2, Data = new byte[512] });
			Assert.AreEqual(DiskProtectionScheme.None, DiskProtection.Detect(OneTrack(plain)));
		}

		private static FluxDisk OneTrack(List<TrackSector> sectors)
		{
			var disk = new FluxDisk();
			disk.SetTrack(0, 0, StandardMfmFormat.BuildStandardTrack(sectors));
			return disk;
		}

		private static byte[] Ascii(string s, int size)
		{
			var a = new byte[size];
			var b = Encoding.ASCII.GetBytes(s);
			Array.Copy(b, 0, a, 0, Math.Min(b.Length, size));
			return a;
		}

		[TestMethod]
		public void BestOfElite_SpeedlockSignatureButRealDataSector_NotCorrupted()
		{
			string path = System.IO.Path.Combine(
				System.IO.Path.GetDirectoryName(typeof(DiskProtectionTests).Assembly.Location)!, "Resources", "disk", "BestOfElite.dsk");
			if (!System.IO.File.Exists(path)) { Assert.Inconclusive($"test disk not present: {path}"); return; }
			var bytes = System.IO.File.ReadAllBytes(path);

			// This title carries the SPEEDLOCK loader signature but stores REAL data (a deleted-DAM sector
			// with a valid CRC) in sector 2 - synthesis must NOT touch it (only a sector with a data CRC error
			// is the genuine weak sector). Regression for the black-screen bug.
			var track0 = FluxDisk.FromCpcDsk(bytes).GetTrack(0, 0);
			var a = StandardMfmFormat.ReadSectorById(track0, 0, 0, 2, 2, new WeakBitRng(1));
			var b = StandardMfmFormat.ReadSectorById(track0, 0, 0, 2, 2, new WeakBitRng(2));
			Assert.IsNotNull(a);
			Assert.IsTrue(a.Deleted, "sector 2 keeps its deleted address mark");
			Assert.IsTrue(a.DataCrcOk, "sector 2 reads with a valid CRC (not corrupted by weak synthesis)");
			CollectionAssert.AreEqual(a.Data, b.Data, "sector 2 is stable across reads (not made weak)");
		}

		private static void CheckReal(string file, Func<byte[], FluxDisk> load, DiskProtectionScheme expected)
		{
			string path = System.IO.Path.Combine(
				System.IO.Path.GetDirectoryName(typeof(DiskProtectionTests).Assembly.Location)!, "Resources", "disk", file);
			if (!System.IO.File.Exists(path)) return; // copyrighted, kept local

			var disk = load(System.IO.File.ReadAllBytes(path));
			Assert.AreEqual(expected, DiskProtection.Detect(disk), $"protection detection for {file}");
		}
	}
}
