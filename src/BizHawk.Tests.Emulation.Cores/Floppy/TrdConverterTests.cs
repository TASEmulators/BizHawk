using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>
	/// Tests for the TR-DOS .TRD loader, in particular that TRIMMED images (as TOSEC and other tools store
	/// them - trailing unused sectors omitted, so the length is a whole number of sectors but not of tracks)
	/// are recognised and loaded, with the omitted tail zero-padded up to the declared geometry.
	/// </summary>
	[TestClass]
	public sealed class TrdConverterTests
	{
		// a minimal valid TR-DOS image: `sectors` x 256 bytes, with a plausible disk-info sector at track 0
		// sector 9 (offset 2048): disk type 0x16 (80DS) at 0xE3, TR-DOS marker 0x10 at 0xE7.
		private static byte[] MakeTrimmed(int sectors)
		{
			var d = new byte[sectors * 256];
			for (int i = 0; i < d.Length; i++) d[i] = (byte)((i * 13 + 5) & 0xFF);
			d[2048 + 0xE3] = 0x16;
			d[2048 + 0xE7] = 0x10;
			return d;
		}

		[TestMethod]
		public void IsTrd_AcceptsTrimmedWholeSectorImage()
		{
			// 40 sectors = 2.5 tracks: a whole number of sectors but NOT of 16-sector tracks
			var d = MakeTrimmed(40);
			Assert.AreNotEqual(0, d.Length % (16 * 256), "test image is deliberately not a whole-track multiple");
			Assert.IsTrue(TrdConverter.IsTrd(d), "a trimmed TRD (whole sectors) must be recognised");
		}

		[TestMethod]
		public void IsTrd_RejectsNonSectorMultipleOrForeign()
		{
			var oddLength = new byte[40 * 256 - 1];
			System.Array.Copy(MakeTrimmed(40), oddLength, oddLength.Length);
			Assert.IsFalse(TrdConverter.IsTrd(oddLength), "not a whole number of sectors");
			var noMarker = MakeTrimmed(40);
			noMarker[2048 + 0xE7] = 0x00; // missing the TR-DOS marker
			Assert.IsFalse(TrdConverter.IsTrd(noMarker), "missing TR-DOS marker");
			Assert.IsFalse(TrdConverter.IsTrd(new byte[8 * 256]), "too small to hold track 0's info sector");
		}

		[TestMethod]
		public void ToFluxDisk_ExpandsTrimmedImageToFullGeometryAndPreservesData()
		{
			var d = MakeTrimmed(40); // only 40 of the disk's 80x2x16 sectors are present
			var disk = TrdConverter.ToFluxDisk(d);
			Assert.AreEqual(80, disk.Cylinders, "0x16 = 80 cylinders");
			Assert.AreEqual(2, disk.Sides, "0x16 = double sided");

			// the stored sectors read back byte-for-byte (track 0 side 0 sector 1 = first 256 bytes)
			var t0 = disk.GetTrack(0, 0);
			var secs = StandardMfmFormat.DecodeSectors(t0);
			var s1 = secs.Find(s => s.R == 1);
			Assert.IsNotNull(s1);
			for (int i = 0; i < 256; i++) Assert.AreEqual(d[i], s1.Data[i], $"stored byte {i}");

			// a track beyond the trim exists (padded) and is readable as zero-filled sectors
			var tHigh = disk.GetTrack(40, 0);
			Assert.IsNotNull(tHigh, "cylinder past the trim is present (zero-padded)");
			var high = StandardMfmFormat.DecodeSectors(tHigh).Find(s => s.R == 1);
			Assert.IsNotNull(high);
			foreach (var b in high.Data) Assert.AreEqual(0, b, "padded sector is zero");
		}
	}
}
