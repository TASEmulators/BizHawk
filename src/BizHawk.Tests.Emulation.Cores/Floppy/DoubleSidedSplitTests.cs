using System.Collections.Generic;

using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>
	/// Tests the format-agnostic double-sided split: FluxDisk.ExtractSide turns one side of a two-sided disk
	/// into a standalone single-sided disk (that side's tracks at side 0), which is how the single-headed +3
	/// exposes both sides of any supported format.
	/// </summary>
	[TestClass]
	public sealed class DoubleSidedSplitTests
	{
		[TestMethod]
		public void ExtractSide_SplitsTwoSidedFluxIntoSingleSidedDisks()
		{
			var ds = new FluxDisk();
			for (int cyl = 0; cyl < 3; cyl++)
			{
				ds.SetTrack(cyl, 0, StandardMfmFormat.BuildStandardTrack(OneSector(cyl, 0, 0xA0)));
				ds.SetTrack(cyl, 1, StandardMfmFormat.BuildStandardTrack(OneSector(cyl, 1, 0xB0)));
			}
			Assert.AreEqual(2, ds.Sides, "starts double-sided");

			var side0 = ds.ExtractSide(0);
			var side1 = ds.ExtractSide(1);
			Assert.AreEqual(1, side0.Sides, "side 0 image is single-sided");
			Assert.AreEqual(1, side1.Sides, "side 1 image is single-sided");
			Assert.AreEqual(3, side0.Cylinders);

			// each split disk holds its own side's data, now readable at side 0
			var s0 = StandardMfmFormat.ReadSectorById(side0.GetTrack(1, 0), 1, 0, 1, 2);
			var s1 = StandardMfmFormat.ReadSectorById(side1.GetTrack(1, 0), 1, 1, 1, 2);
			Assert.IsNotNull(s0); Assert.IsNotNull(s1);
			Assert.AreEqual((byte)0xA0, s0.Data[0], "side 0 data");
			Assert.AreEqual((byte)0xB0, s1.Data[0], "side 1 data");
		}

		[TestMethod]
		public void RealDoubleSidedIpf_SplitsIntoTwoUsableSides()
		{
			string path = System.IO.Path.Combine(
				System.IO.Path.GetDirectoryName(typeof(DoubleSidedSplitTests).Assembly.Location)!, "Resources", "disk", "MagicKnightTrilogy.ipf");
			if (!System.IO.File.Exists(path)) { Assert.Inconclusive($"test IPF not present: {path}"); return; }

			var disk = FluxDisk.FromIpf(System.IO.File.ReadAllBytes(path));
			Assert.AreEqual(2, disk.Sides, "the compilation is double-sided");

			foreach (int side in new[] { 0, 1 })
			{
				var single = disk.ExtractSide(side);
				Assert.AreEqual(1, single.Sides, $"side {side} extracts to a single-sided disk");
				int good = 0;
				for (int cyl = 0; cyl < single.Cylinders; cyl++)
				{
					var t = single.GetTrack(cyl, 0);
					if (t == null) continue;
					foreach (var s in StandardMfmFormat.DecodeSectors(t))
						if (s.IdCrcOk && s.DataCrcOk) good++;
				}
				Assert.IsTrue(good > 100, $"side {side} still decodes its sectors at side 0 (got {good})");
			}
		}

		private static List<TrackSector> OneSector(int cyl, int head, byte fill)
		{
			var d = new byte[512];
			for (int i = 0; i < 512; i++) d[i] = fill;
			return new List<TrackSector> { new() { C = (byte)cyl, H = (byte)head, R = 1, N = 2, Data = d } };
		}
	}
}
