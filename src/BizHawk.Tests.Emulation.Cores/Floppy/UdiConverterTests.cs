using System.Collections.Generic;

using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>
	/// Tests for the UDI v1.0 loader: builds a synthetic UDI whose single track holds one IBM System-34
	/// sector (sync + A1 markers + IDAM/CHRN/CRC + DAM/data/CRC, with the clock bitmap flagging the A1 bytes),
	/// then converts it to flux and reads the sector back with valid CRCs.
	/// </summary>
	[TestClass]
	public sealed class UdiConverterTests
	{
		[TestMethod]
		public void Udi_SingleSector_DecodesFromFlux()
		{
			byte c = 0, h = 0, r = 1, n = 1; // N=1 -> 256-byte sector (typical TR-DOS)
			var data = Fill(256, 0x5A);

			var bytes = new List<byte>();
			var markers = new List<int>();
			void Add(byte b) => bytes.Add(b);
			void AddMark(byte b) { markers.Add(bytes.Count); bytes.Add(b); }
			void AddN(byte b, int count) { for (int i = 0; i < count; i++) bytes.Add(b); }

			AddN(0x4E, 16);                                  // lead-in gap
			AddN(0x00, 12);                                  // sync
			AddMark(0xA1); AddMark(0xA1); AddMark(0xA1);     // ID sync marks
			Add(0xFE); Add(c); Add(h); Add(r); Add(n);       // IDAM + CHRN
			ushort idCrc = Crc16Ccitt.Compute(new byte[] { 0xA1, 0xA1, 0xA1, 0xFE, c, h, r, n });
			Add((byte)(idCrc >> 8)); Add((byte)idCrc);
			AddN(0x4E, 22);                                  // gap 2
			AddN(0x00, 12);                                  // sync
			AddMark(0xA1); AddMark(0xA1); AddMark(0xA1);     // data sync marks
			Add(0xFB);                                       // DAM
			foreach (var b in data) Add(b);
			var dataField = new List<byte> { 0xA1, 0xA1, 0xA1, 0xFB };
			dataField.AddRange(data);
			ushort dCrc = Crc16Ccitt.Compute(dataField.ToArray());
			Add((byte)(dCrc >> 8)); Add((byte)dCrc);
			AddN(0x4E, 24);                                  // gap 3

			byte[] udi = BuildUdi(bytes, markers);
			Assert.IsTrue(UdiConverter.IsUdi(udi));

			var disk = FluxDisk.FromUdi(udi);
			var track = disk.GetTrack(0, 0);
			Assert.IsNotNull(track, "cylinder 0 built from UDI");

			var decoded = StandardMfmFormat.DecodeSectors(track);
			var s = decoded.Find(x => x.R == 1);
			Assert.IsNotNull(s, "sector located on the UDI flux");
			Assert.IsTrue(s.IdCrcOk, "ID CRC valid");
			Assert.IsTrue(s.DataCrcOk, "data CRC valid");
			Assert.AreEqual(256, s.SizeBytes);
			Assert.AreEqual((byte)0x5A, s.Data[0], "sector data reproduced");
		}

		[TestMethod]
		public void ProfiCpm_RealUdi_ParsesAndDecodesSectors()
		{
			string path = System.IO.Path.Combine(
				System.IO.Path.GetDirectoryName(typeof(UdiConverterTests).Assembly.Location)!,
				"Resources", "disk", "ProfiCPM.udi");
			if (!System.IO.File.Exists(path))
			{
				Assert.Inconclusive($"test UDI not present (kept local): {path}");
				return;
			}

			var bytes = System.IO.File.ReadAllBytes(path);
			Assert.IsTrue(UdiConverter.IsUdi(bytes));

			var disk = FluxDisk.FromUdi(bytes);
			Assert.AreEqual(80, disk.Cylinders, "80 cylinders");
			Assert.AreEqual(2, disk.Sides, "double sided");

			// decode a few tracks and confirm real sectors come back with valid CRCs
			int totalGood = 0, tracksChecked = 0;
			for (int cyl = 0; cyl < 5; cyl++)
			{
				var t = StandardMfmFormat.DecodeSectors(disk.GetTrack(cyl, 0));
				tracksChecked++;
				foreach (var s in t)
					if (s.IdCrcOk && s.HasData && s.DataCrcOk) totalGood++;
			}
			Assert.IsTrue(totalGood > 0, $"decoded {totalGood} good sectors over {tracksChecked} tracks - expected the standard MFM sectors to read back");
		}

		[TestMethod]
		public void IsUdi_RejectsNonUdiAndCompressed()
		{
			Assert.IsFalse(UdiConverter.IsUdi(new byte[16])); // no signature
			var compressed = new byte[16];
			compressed[0] = (byte)'u'; compressed[1] = (byte)'d'; compressed[2] = (byte)'i'; compressed[3] = (byte)'!';
			Assert.IsTrue(UdiConverter.IsUdi(compressed), "lowercase udi! is still recognized as UDI");
			Assert.ThrowsException<System.ArgumentException>(() => UdiConverter.ToFluxDisk(compressed), "but compressed UDI is rejected on convert");
		}

		private static byte[] BuildUdi(List<byte> trackBytes, List<int> markerIndices)
		{
			int tlen = trackBytes.Count;
			int clen = (tlen + 7) / 8;
			var clock = new byte[clen];
			foreach (int i in markerIndices) clock[i >> 3] |= (byte)(1 << (i & 7)); // LSB-first clock bitmap

			var f = new List<byte> { (byte)'U', (byte)'D', (byte)'I', (byte)'!' };
			AddLe32(f, 0);   // file size (ignored by the loader)
			f.Add(0);        // version 0
			f.Add(0);        // cylinders - 1 = 0 (one cylinder)
			f.Add(0);        // sides = 0 (single sided)
			f.Add(0);        // unused
			AddLe32(f, 0);   // extended header length
			f.Add(0);        // track type = MFM
			f.Add((byte)tlen); f.Add((byte)(tlen >> 8));
			f.AddRange(trackBytes);
			f.AddRange(clock);
			AddLe32(f, 0);   // trailing CRC32 placeholder (not validated)
			return f.ToArray();
		}

		private static void AddLe32(List<byte> f, int v)
		{
			f.Add((byte)v); f.Add((byte)(v >> 8)); f.Add((byte)(v >> 16)); f.Add((byte)(v >> 24));
		}

		private static byte[] Fill(int n, byte v)
		{
			var a = new byte[n];
			for (int i = 0; i < n; i++) a[i] = v;
			return a;
		}
	}
}
