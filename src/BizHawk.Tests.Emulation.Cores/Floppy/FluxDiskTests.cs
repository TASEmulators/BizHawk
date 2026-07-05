using System.Collections.Generic;
using System.IO;

using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>
	/// Phase-2 (drive + flux read engine) tests: a self-contained FluxDisk/FloppyDrive read-by-ID case,
	/// plus loading the real RoboCop EDSK into a flux disk and reading a clean sector back through the
	/// drive (skipped if the local copyrighted image is absent).
	/// </summary>
	[TestClass]
	public sealed class FluxDiskTests
	{
		[TestMethod]
		public void Drive_ReadsSectorById_FromFluxDisk()
		{
			var secs = new List<TrackSector>
			{
				new() { C = 5, H = 0, R = 1, N = 2, Data = Fill(512, 0xAB) },
				new() { C = 5, H = 0, R = 2, N = 2, Data = Fill(512, 0xCD) },
			};
			var disk = new FluxDisk();
			disk.SetTrack(5, 0, StandardMfmFormat.BuildStandardTrack(secs));

			var drive = new FloppyDrive { Disk = disk };
			Assert.IsFalse(drive.Ready, "motor off -> not ready");
			drive.MotorOn = true;
			Assert.IsTrue(drive.Ready, "motor on + disk -> ready");
			Assert.IsTrue(drive.Track0, "starts at cylinder 0");

			drive.SeekTo(5);
			Assert.AreEqual(5, drive.CurrentCylinder);
			Assert.IsFalse(drive.Track0);

			var s2 = StandardMfmFormat.ReadSectorById(drive.CurrentTrack(0), 5, 0, 2, 2);
			Assert.IsNotNull(s2, "sector R=2 found");
			Assert.IsTrue(s2.IdCrcOk && s2.DataCrcOk, "CRCs ok");
			Assert.AreEqual((byte)0xCD, s2.Data[0], "correct sector data");

			Assert.IsNull(StandardMfmFormat.ReadSectorById(drive.CurrentTrack(0), 5, 0, 9, 2), "missing sector -> null");

			for (int i = 0; i < 5; i++) drive.Step(towardHigherCylinder: false);
			Assert.IsTrue(drive.Track0, "stepped back to track 0");
		}

		[TestMethod]
		public void RoboCop_Edsk_LoadsIntoFluxDisk_AndReadsBackThroughDrive()
		{
			string path = Path.Combine(
				Path.GetDirectoryName(typeof(FluxDiskTests).Assembly.Location)!, "Resources", "disk", "RoboCop(Fixed).dsk");
			if (!File.Exists(path))
			{
				Assert.Inconclusive($"test disk not present (copyrighted, kept local): {path}");
				return;
			}

			var bytes = File.ReadAllBytes(path);
			var parsed = CpcDskConverter.Parse(bytes);

			// find a clean (non-weak, no error) sector so we can check an exact read-back
			CpcDskConverter.ParsedTrack targetTrack = null;
			TrackSector target = null;
			foreach (var pt in parsed.Tracks)
			{
				var t = pt.Sectors.Find(s => s.WeakCopies == null && !s.DataCrcError && !s.IdCrcError && !s.Deleted);
				if (t != null) { targetTrack = pt; target = t; break; }
			}
			if (target == null) { Assert.Inconclusive("no clean sector found on RoboCop"); return; }

			var disk = FluxDisk.FromCpcDsk(bytes);
			Assert.IsTrue(disk.Cylinders > 0, "flux disk has tracks");

			var drive = new FloppyDrive { Disk = disk };
			Assert.IsFalse(drive.Ready, "not ready with motor off");
			drive.MotorOn = true;
			Assert.IsTrue(drive.Ready, "ready with motor on + disk");

			drive.SeekTo(targetTrack.Cylinder);
			Assert.AreEqual(targetTrack.Cylinder, drive.CurrentCylinder);

			var dec = StandardMfmFormat.ReadSectorById(
				drive.CurrentTrack(targetTrack.Side), target.C, target.H, target.R, target.N);
			Assert.IsNotNull(dec, "clean sector located on the flux via the drive");
			Assert.IsTrue(dec.IdCrcOk && dec.DataCrcOk, "clean sector CRCs ok");
			CollectionAssert.AreEqual(target.Data, dec.Data, "read-back data matches the source image");
		}

		private static byte[] Fill(int n, byte v)
		{
			var a = new byte[n];
			for (int i = 0; i < n; i++) a[i] = v;
			return a;
		}
	}
}
