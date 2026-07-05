using System.Collections.Generic;

using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>
	/// Tests for the IPF container parser: builds a synthetic IPF file (CAPS/INFO/IMGE/DATA with
	/// two block descriptors and tokenized data-stream elements, including a Fuzzy element) using our own
	/// CRC32 routine, then parses it back and checks every layer.
	/// </summary>
	[TestClass]
	public sealed class IpfConverterTests
	{
		[TestMethod]
		public void Parse_SyntheticIpf_ReadsAllRecordsBlocksAndStreamElements()
		{
			byte[] ipf = BuildSyntheticIpf();

			Assert.IsTrue(IpfConverter.IsIpf(ipf), "recognized as IPF");
			var disk = IpfConverter.Parse(ipf);

			Assert.IsTrue(disk.AllCrcOk, "all record and data-block CRC32s validate");

			Assert.IsNotNull(disk.Info);
			Assert.AreEqual(2, disk.Info.EncoderType, "SPS encoder");
			Assert.AreEqual(5, disk.Info.Platforms[0], "Spectrum platform");

			Assert.AreEqual(1, disk.Images.Count);
			var img = disk.Images[0];
			Assert.AreEqual(0, img.Track);
			Assert.AreEqual(100, img.DataKey);
			Assert.AreEqual(2, img.BlockCount);
			Assert.IsTrue(img.Fuzzy, "track flagged fuzzy");

			Assert.IsTrue(disk.Data.ContainsKey(100), "DATA record matched by dataKey");
			var data = disk.Data[100];
			Assert.IsTrue(data.ExtraCrcOk, "extra data block CRC32 validates");
			Assert.AreEqual(2, data.Blocks.Count);

			var b0 = data.Blocks[0];
			Assert.AreEqual(1, b0.EncoderType, "MFM block");
			Assert.IsTrue(b0.DataInBit, "sample sizes are in bits");
			Assert.AreEqual(3, b0.DataElements.Count, "sync + data + fuzzy");

			Assert.AreEqual(IpfDataType.Sync, b0.DataElements[0].Type);
			Assert.AreEqual(16, b0.DataElements[0].Size);
			CollectionAssert.AreEqual(new byte[] { 0x44, 0x89 }, b0.DataElements[0].Sample);

			Assert.AreEqual(IpfDataType.Data, b0.DataElements[1].Type);
			CollectionAssert.AreEqual(new byte[] { 0xAA, 0x55 }, b0.DataElements[1].Sample);

			Assert.AreEqual(IpfDataType.Fuzzy, b0.DataElements[2].Type);
			Assert.AreEqual(8, b0.DataElements[2].Size, "fuzzy carries a size");
			Assert.AreEqual(0, b0.DataElements[2].Sample.Length, "fuzzy carries no sample");

			var b1 = data.Blocks[1];
			Assert.AreEqual(1, b1.DataElements.Count);
			Assert.AreEqual(IpfDataType.Data, b1.DataElements[0].Type);
			CollectionAssert.AreEqual(new byte[] { 0xFF }, b1.DataElements[0].Sample);
		}

		[TestMethod]
		public void RoboCop2_RealIpf_ParsesAndDecodesSectorsFromFlux()
		{
			string path = System.IO.Path.Combine(
				System.IO.Path.GetDirectoryName(typeof(IpfConverterTests).Assembly.Location)!,
				"Resources", "disk", "RoboCop2.ipf");
			if (!System.IO.File.Exists(path))
			{
				Assert.Inconclusive($"test IPF not present (copyrighted, kept local): {path}");
				return;
			}

			var bytes = System.IO.File.ReadAllBytes(path);
			var ipf = IpfConverter.Parse(bytes);
			Assert.IsTrue(ipf.AllCrcOk, "every record and data-block CRC32 validates against the real file");
			Assert.AreEqual(2, ipf.Info.EncoderType, "SPS encoder");
			Assert.AreEqual(5, ipf.Info.Platforms[0], "Spectrum");

			// track 0 side 0 is a standard 9-sector track; roll it to flux and read the sectors back
			var img0 = ipf.Images.Find(i => i.Track == 0 && i.Side == 0);
			Assert.IsNotNull(img0);
			Assert.AreEqual(9, img0.BlockCount);

			var disk = FluxDisk.FromIpf(bytes);
			var track0 = disk.GetTrack(0, 0);
			Assert.IsNotNull(track0, "track 0 rolled into flux");

			var sectors = StandardMfmFormat.DecodeSectors(track0);
			int good = 0;
			foreach (var s in sectors)
				if (s.IdCrcOk && s.HasData && s.DataCrcOk && s.SizeBytes == 512) good++;
			Assert.AreEqual(9, good, "all nine 512-byte sectors decode with valid ID and data CRCs");

			// over-formatted: the disk uses tracks past the nominal 40 (a drive must be able to seek there)
			Assert.IsTrue(ipf.Images.Exists(i => i.Track >= 40 && i.BlockCount > 0)
				|| ipf.Info.MaxTrack >= 40, "image extends to/over cylinder 40");
		}

		[TestMethod]
		public void MidnightResistance_RealIpf_ReproducesSpeedlockNonStandardTrack()
		{
			string path = System.IO.Path.Combine(
				System.IO.Path.GetDirectoryName(typeof(IpfConverterTests).Assembly.Location)!,
				"Resources", "disk", "MidnightResistance.ipf");
			if (!System.IO.File.Exists(path))
			{
				Assert.Inconclusive($"test IPF not present (copyrighted, kept local): {path}");
				return;
			}

			var bytes = System.IO.File.ReadAllBytes(path);
			var ipf = IpfConverter.Parse(bytes);
			Assert.IsTrue(ipf.AllCrcOk, "all CRC32s validate");

			var disk = FluxDisk.FromIpf(bytes);

			// track 0 is a normal 9x512 track and must decode cleanly
			var t0 = StandardMfmFormat.DecodeSectors(disk.GetTrack(0, 0));
			int good0 = 0;
			foreach (var s in t0) if (s.IdCrcOk && s.DataCrcOk && s.SizeBytes == 512) good0++;
			Assert.AreEqual(9, good0, "standard track decodes 9 good sectors");

			// track 1 is a Speedlock protection track: one oversized sector with a non-standard id
			// (R=0xC1, N=6 => declared 8192 bytes) recorded with a deleted address mark. The flux model must
			// reproduce it faithfully - the old core needed a hardcoded per-title hack for this kind of thing.
			var t1 = StandardMfmFormat.DecodeSectors(disk.GetTrack(1, 0));
			var prot = t1.Find(s => s.R == 0xC1);
			Assert.IsNotNull(prot, "the non-standard sector id (R=0xC1) is present on the flux");
			Assert.AreEqual(1, prot.C);
			Assert.AreEqual(6, prot.N, "declared size N=6 (8192 bytes)");
			Assert.AreEqual(8192, prot.SizeBytes);
			Assert.IsTrue(prot.IdCrcOk, "the id field itself has a valid CRC");
			Assert.IsTrue(prot.Deleted, "recorded with a deleted data address mark (F8)");
			// the recorded data begins 09 8C D4 84 20 (verbatim from the IPF data element)
			CollectionAssert.AreEqual(new byte[] { 0x09, 0x8C, 0xD4, 0x84, 0x20 },
				new[] { prot.Data[0], prot.Data[1], prot.Data[2], prot.Data[3], prot.Data[4] },
				"the sector data is reproduced verbatim from the recorded flux");
		}

		[TestMethod]
		public void IsIpf_RejectsNonCaps()
		{
			Assert.IsFalse(IpfConverter.IsIpf(new byte[] { (byte)'M', (byte)'V', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }));
			Assert.IsFalse(IpfConverter.IsIpf(new byte[3]));
		}

		// ---- synthetic IPF builder (uses the same CRC32 routine the parser validates against) ----

		private static byte[] BuildSyntheticIpf()
		{
			var f = new List<byte>();
			AddRecord(f, "CAPS", System.Array.Empty<byte>());

			var info = new byte[84];
			PutBe(info, 0, 1);   // mediaType = floppy
			PutBe(info, 4, 2);   // encoderType = SPS
			PutBe(info, 48, 5);  // platform[0] = Spectrum
			AddRecord(f, "INFO", info);

			var imge = new byte[68];
			PutBe(imge, 8, 2);    // density = auto
			PutBe(imge, 12, 1);   // signalType = 2us
			PutBe(imge, 40, 2);   // blockCount
			PutBe(imge, 48, 1);   // trackFlags: fuzzy
			PutBe(imge, 52, 100); // dataKey
			AddRecord(f, "IMGE", imge);

			byte[] stream0 = { 0x21, 0x10, 0x44, 0x89, 0x22, 0x10, 0xAA, 0x55, 0x25, 0x08, 0x00 };
			byte[] stream1 = { 0x22, 0x08, 0xFF, 0x00 };
			int off0 = 64, off1 = 64 + stream0.Length;
			var extra = new byte[64 + stream0.Length + stream1.Length];
			WriteDesc(extra, 0, dataBits: 160, gapBits: 0, f8: 0, f12: 1, enc: 1, flags: 0x04, gapDefault: 0x4E, dataOffset: off0);
			WriteDesc(extra, 32, dataBits: 8, gapBits: 0, f8: 0, f12: 1, enc: 1, flags: 0x04, gapDefault: 0x4E, dataOffset: off1);
			System.Array.Copy(stream0, 0, extra, off0, stream0.Length);
			System.Array.Copy(stream1, 0, extra, off1, stream1.Length);

			var datablk = new byte[16];
			PutBe(datablk, 0, extra.Length);                             // length of extra data block
			PutBe(datablk, 4, extra.Length * 8);                          // bitSize
			PutBe(datablk, 8, (int)Crc32Iso.Compute(extra, 0, extra.Length)); // extra block CRC
			PutBe(datablk, 12, 100);                                      // dataKey
			AddDataRecord(f, datablk, extra);

			return f.ToArray();
		}

		private static void AddRecord(List<byte> f, string name, byte[] block)
		{
			int length = 12 + block.Length;
			var rec = new byte[length];
			for (int i = 0; i < 4; i++) rec[i] = (byte)name[i];
			PutBe(rec, 4, length);
			System.Array.Copy(block, 0, rec, 12, block.Length);
			PutBe(rec, 8, (int)Crc32Iso.ComputeWithZeroedField(rec, 0, length, 8));
			f.AddRange(rec);
		}

		private static void AddDataRecord(List<byte> f, byte[] dataBlock, byte[] extra)
		{
			var rec = new byte[28];
			rec[0] = (byte)'D'; rec[1] = (byte)'A'; rec[2] = (byte)'T'; rec[3] = (byte)'A';
			PutBe(rec, 4, 28);
			System.Array.Copy(dataBlock, 0, rec, 12, 16);
			PutBe(rec, 8, (int)Crc32Iso.ComputeWithZeroedField(rec, 0, 28, 8));
			f.AddRange(rec);
			f.AddRange(extra);
		}

		private static void WriteDesc(byte[] buf, int o, int dataBits, int gapBits, int f8, int f12, int enc, int flags, int gapDefault, int dataOffset)
		{
			PutBe(buf, o, dataBits);
			PutBe(buf, o + 4, gapBits);
			PutBe(buf, o + 8, f8);
			PutBe(buf, o + 12, f12);
			PutBe(buf, o + 16, enc);
			PutBe(buf, o + 20, flags);
			PutBe(buf, o + 24, gapDefault);
			PutBe(buf, o + 28, dataOffset);
		}

		private static void PutBe(byte[] b, int o, int v)
		{
			b[o] = (byte)(v >> 24);
			b[o + 1] = (byte)(v >> 16);
			b[o + 2] = (byte)(v >> 8);
			b[o + 3] = (byte)v;
		}
	}
}
