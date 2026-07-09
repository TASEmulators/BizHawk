using System.Collections.Generic;

using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>
	/// Tests for the TR-DOS .SCL loader: the packed SINCLAIR catalogue-plus-data form is expanded into a
	/// TR-DOS layout (directory on track 0, files from track 1 with assigned start track/sector) and loaded
	/// as flux, with the file data readable back byte-for-byte. The image is built in code (no external media).
	/// </summary>
	[TestClass]
	public sealed class SclConverterTests
	{
		// build an SCL with the given files (name8, type, data). sectorCount = ceil(data/256).
		private static byte[] MakeScl(params (string name, char type, byte[] data)[] files)
		{
			var cat = new List<byte>();
			var payload = new List<byte>();
			foreach (var (name, type, data) in files)
			{
				int secs = (data.Length + 255) / 256;
				var padded = new byte[secs * 256];
				System.Array.Copy(data, padded, data.Length);
				for (int i = 0; i < 8; i++) cat.Add((byte)(i < name.Length ? name[i] : ' '));
				cat.Add((byte)type);
				cat.Add(0x00); cat.Add(0x80);       // start/param (arbitrary)
				cat.Add((byte)(data.Length & 0xFF)); cat.Add((byte)(data.Length >> 8)); // length
				cat.Add((byte)secs);                 // sector count
				payload.AddRange(padded);
			}

			var scl = new List<byte>();
			scl.AddRange(System.Text.Encoding.ASCII.GetBytes("SINCLAIR"));
			scl.Add((byte)files.Length);
			scl.AddRange(cat);
			scl.AddRange(payload);
			uint sum = 0; foreach (var b in scl) sum += b;
			scl.Add((byte)sum); scl.Add((byte)(sum >> 8)); scl.Add((byte)(sum >> 16)); scl.Add((byte)(sum >> 24));
			return scl.ToArray();
		}

		[TestMethod]
		public void IsScl_MatchesSignature()
		{
			Assert.IsTrue(SclConverter.IsScl(MakeScl(("A", 'B', new byte[256]))));
			Assert.IsFalse(SclConverter.IsScl(new byte[] { (byte)'S', (byte)'I', (byte)'N', 0, 0, 0, 0, 0, 0 }));
			Assert.IsFalse(SclConverter.IsScl(new byte[4]));
		}

		[TestMethod]
		public void ToTrd_AssignsSequentialStartSectors()
		{
			var f1 = Fill(5 * 256, 0xA1);
			var f2 = Fill(3 * 256, 0xB2);
			var trd = SclConverter.ToTrd(MakeScl(("BOOT", 'B', f1), ("DATA", 'C', f2)));

			// file 0 starts at track 1 sector 0 (logical sector 16); file 1 immediately after (16 + 5 = 21)
			Assert.AreEqual(1, trd[15], "file0 start track");
			Assert.AreEqual(0, trd[14], "file0 start sector");
			Assert.AreEqual(21 / 16, trd[16 + 15], "file1 start track");
			Assert.AreEqual(21 % 16, trd[16 + 14], "file1 start sector");

			// disk-info sector reflects the format
			Assert.AreEqual(0x16, trd[8 * 256 + 0xE3], "disk type 80DS");
			Assert.AreEqual(0x10, trd[8 * 256 + 0xE7], "TR-DOS marker");
			Assert.AreEqual(2, trd[8 * 256 + 0xE4], "file count");
		}

		[TestMethod]
		public void ToFluxDisk_FileDataReadsBack()
		{
			var f1 = Fill(5 * 256, 0xA1);
			var f2 = Fill(3 * 256, 0xB2);
			var disk = SclConverter.ToFluxDisk(MakeScl(("BOOT", 'B', f1), ("DATA", 'C', f2)));
			Assert.AreEqual(80, disk.Cylinders);
			Assert.AreEqual(2, disk.Sides);

			// file 0 occupies logical sectors 16..20, file 1 21..23 - read them back and compare
			CheckFile(disk, 16, f1);
			CheckFile(disk, 21, f2);
		}

		private static void CheckFile(FluxDisk disk, int startLogicalSector, byte[] expected)
		{
			for (int s = 0; s * 256 < expected.Length; s++)
			{
				int ls = startLogicalSector + s;
				int cyl = (ls / 16) / 2, side = (ls / 16) % 2, r = (ls % 16) + 1;
				var sec = StandardMfmFormat.DecodeSectors(disk.GetTrack(cyl, side)).Find(x => x.R == r);
				Assert.IsNotNull(sec, $"logical sector {ls} present");
				for (int b = 0; b < 256; b++)
					Assert.AreEqual(expected[s * 256 + b], sec.Data[b], $"file byte {s * 256 + b}");
			}
		}

		private static byte[] Fill(int n, byte seed)
		{
			var d = new byte[n];
			for (int i = 0; i < n; i++) d[i] = (byte)(seed ^ (i & 0xFF));
			return d;
		}
	}
}
