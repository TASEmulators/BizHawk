using System.Collections.Generic;
using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>
	/// Tests for the WD1793 controller (Beta 128 / Pentagon) driving the shared flux/drive model through its
	/// register interface: Type I seeks, Read Sector, Write Sector (round-trip through the flux rebuild),
	/// Read Address rotation, and Write Track formatting. The disk is synthesized in code so the test carries
	/// no external media.
	/// </summary>
	[TestClass]
	public sealed class Wd1793FdcTests
	{
		private const byte ST_BUSY = 0x01, ST_NOTREADY = 0x80, ST_WRITEPROT = 0x40;
		private const byte ST2_LOSTDATA = 0x04, ST2_RNF = 0x10;

		private const int Cyls = 2, Heads = 2, SecPerTrk = 16, SecSize = 256;

		// deterministic per-byte pattern so any mis-addressed read/write is caught
		private static byte[] MakeRaw()
		{
			var raw = new byte[Cyls * Heads * SecPerTrk * SecSize];
			for (int i = 0; i < raw.Length; i++) raw[i] = (byte)((i * 31 + 7) & 0xFF);
			return raw;
		}

		private static int RawOffset(int cyl, int side, int sec)
			=> ((cyl * Heads + side) * SecPerTrk + (sec - 1)) * SecSize;

		private static (Wd1793Fdc fdc, FloppyDrive drive) MakeFdc(byte[] raw, bool writeProtect = false)
		{
			var disk = RawSectorConverter.ToFluxDisk(raw, new DiskGeometry
			{
				Cylinders = Cyls, Heads = Heads, SectorsPerTrack = SecPerTrk, SectorSize = SecSize, FirstSectorId = 1,
			});
			disk.WriteProtected = writeProtect;
			var drive = new FloppyDrive(new FloppyDriveProfile { Cylinders = Cyls, Sides = Heads, Rpm = 300 })
			{
				Disk = disk, MotorOn = true,
			};
			var fdc = new Wd1793Fdc();
			fdc.Drives[0] = drive;
			fdc.ConfigureTiming(3_546_900);
			fdc.Reset();
			return (fdc, drive);
		}

		private static void RunUntilIdle(Wd1793Fdc fdc, int guardCycles = 4_000_000)
		{
			for (int i = 0; i < guardCycles && (fdc.ReadStatus() & ST_BUSY) != 0; i += 16) fdc.Clock(16);
		}

		private static void Seek(Wd1793Fdc fdc, int side, int cyl)
		{
			fdc.SetSystem(0, side, true);
			fdc.WriteData((byte)cyl);
			fdc.WriteCommand(0x18); // Seek (no verify, 6ms)
			RunUntilIdle(fdc);
		}

		private static byte[] ReadSector(Wd1793Fdc fdc, int side, int cyl, int sec)
		{
			Seek(fdc, side, cyl);
			fdc.WriteTrack((byte)cyl);
			fdc.WriteSector((byte)sec);
			fdc.WriteCommand(0x80); // Read Sector, single
			var data = new List<byte>();
			for (int i = 0; i < 4_000_000; i++)
			{
				byte st = fdc.ReadStatus();
				if (fdc.DataRequest) data.Add(fdc.ReadData());
				if ((st & ST_BUSY) == 0) break;
				fdc.Clock(16);
			}
			if (fdc.DataRequest) data.Add(fdc.ReadData());
			return data.ToArray();
		}

		private static byte WriteSector(Wd1793Fdc fdc, int side, int cyl, int sec, byte[] payload)
		{
			Seek(fdc, side, cyl);
			fdc.WriteTrack((byte)cyl);
			fdc.WriteSector((byte)sec);
			fdc.WriteCommand(0xA0); // Write Sector, single
			int p = 0;
			for (int i = 0; i < 4_000_000; i++)
			{
				byte st = fdc.ReadStatus();
				if ((st & ST_BUSY) == 0) break;
				if (fdc.DataRequest && p < payload.Length) fdc.WriteData(payload[p++]);
				else fdc.Clock(16);
			}
			return fdc.ReadStatus();
		}

		[TestMethod]
		public void ReadSector_ReturnsFluxData()
		{
			var raw = MakeRaw();
			var (fdc, _) = MakeFdc(raw);
			foreach (var (cyl, side, sec) in new[] { (0, 0, 1), (0, 1, 5), (1, 0, 16), (1, 1, 9) })
			{
				var got = ReadSector(fdc, side, cyl, sec);
				Assert.AreEqual(SecSize, got.Length, $"cyl{cyl} side{side} sec{sec} length");
				int off = RawOffset(cyl, side, sec);
				for (int i = 0; i < SecSize; i++)
					Assert.AreEqual(raw[off + i], got[i], $"cyl{cyl} side{side} sec{sec} byte {i}");
			}
		}

		[TestMethod]
		public void WriteSector_RoundTripsThroughFlux()
		{
			var raw = MakeRaw();
			var (fdc, _) = MakeFdc(raw);
			var payload = new byte[SecSize];
			for (int i = 0; i < SecSize; i++) payload[i] = (byte)(0xC0 ^ i);

			byte st = WriteSector(fdc, 1, 1, 7, payload);
			Assert.AreEqual(0, st & ST_BUSY, "write completed");
			Assert.AreEqual(0, st & ST2_RNF, "sector found");

			var got = ReadSector(fdc, 1, 1, 7);
			CollectionAssert.AreEqual(payload, got, "written sector reads back byte-for-byte");

			// a different sector on the same track is untouched
			var other = ReadSector(fdc, 1, 1, 8);
			int off = RawOffset(1, 1, 8);
			for (int i = 0; i < SecSize; i++) Assert.AreEqual(raw[off + i], other[i], $"neighbour byte {i}");
		}

		[TestMethod]
		public void WriteSector_WriteProtected_Refused()
		{
			var (fdc, _) = MakeFdc(MakeRaw(), writeProtect: true);
			byte st = WriteSector(fdc, 0, 0, 1, new byte[SecSize]);
			Assert.AreNotEqual(0, st & ST_WRITEPROT, "write-protect reported");
		}

		[TestMethod]
		public void ReadAddress_RotatesThroughSectors()
		{
			var (fdc, _) = MakeFdc(MakeRaw());
			Seek(fdc, 0, 0);
			var seen = new HashSet<byte>();
			for (int call = 0; call < SecPerTrk; call++)
			{
				fdc.WriteCommand(0xC0); // Read Address
				var id = new List<byte>();
				for (int i = 0; i < 100000; i++)
				{
					byte st = fdc.ReadStatus();
					if (fdc.DataRequest) id.Add(fdc.ReadData());
					if ((st & ST_BUSY) == 0) break;
					fdc.Clock(16);
				}
				if (fdc.DataRequest) id.Add(fdc.ReadData());
				Assert.AreEqual(6, id.Count, "Read Address returns 6 ID bytes");
				seen.Add(id[2]); // R (sector number)
			}
			Assert.AreEqual(SecPerTrk, seen.Count, "successive Read Address calls cover every sector id");
		}

		[TestMethod]
		public void ReadAddress_ReturnsRealIdCrc()
		{
			var (fdc, _) = MakeFdc(MakeRaw());
			Seek(fdc, 0, 0);
			fdc.WriteCommand(0xC0); // Read Address
			var id = new List<byte>();
			for (int i = 0; i < 100000; i++)
			{
				byte st = fdc.ReadStatus();
				if (fdc.DataRequest) id.Add(fdc.ReadData());
				if ((st & ST_BUSY) == 0) break;
				fdc.Clock(16);
			}
			if (fdc.DataRequest) id.Add(fdc.ReadData());
			Assert.AreEqual(6, id.Count);
			ushort expected = StandardMfmFormat.IdFieldCrc(id[0], id[1], id[2], id[3]);
			Assert.AreEqual((byte)(expected >> 8), id[4], "ID CRC high byte");
			Assert.AreEqual((byte)(expected & 0xFF), id[5], "ID CRC low byte");
		}

		[TestMethod]
		public void ReadSector_MissingSector_ReportsRnfAfterDelay()
		{
			var (fdc, _) = MakeFdc(MakeRaw());
			Seek(fdc, 0, 0);
			fdc.WriteTrack(0);
			fdc.WriteSector(99); // no such sector on the track
			fdc.WriteCommand(0x80); // Read Sector

			int cycles = 0;
			byte st = 0;
			for (int i = 0; i < 16_000_000; i += 16)
			{
				st = fdc.ReadStatus();
				if ((st & ST_BUSY) == 0) break;
				fdc.Clock(16);
				cycles += 16;
			}
			Assert.AreEqual(0, st & ST_BUSY, "command terminated");
			Assert.AreNotEqual(0, st & ST2_RNF, "Record Not Found reported");
			Assert.IsTrue(cycles > 1_000_000, $"RNF should follow a multi-revolution search, took only {cycles} cycles");
		}

		[TestMethod]
		public void SyncState_RoundTripsRegistersAndHeadPosition()
		{
			var raw = MakeRaw();
			var (fdc1, drive1) = MakeFdc(raw);
			Seek(fdc1, 1, 1);            // move the head to cylinder 1, side 1
			fdc1.WriteTrack(1);
			fdc1.WriteSector(5);

			var ms = new MemoryStream();
			var bw = new BinaryWriter(ms);
			var sw = Serializer.CreateBinaryWriter(bw);
			fdc1.SyncState(sw);
			drive1.SyncState(sw);
			bw.Flush();

			var (fdc2, drive2) = MakeFdc(raw);
			ms.Position = 0;
			var br = new BinaryReader(ms);
			var sr = Serializer.CreateBinaryReader(br);
			fdc2.SyncState(sr);
			drive2.SyncState(sr);

			Assert.AreEqual(fdc1.ReadTrack(), fdc2.ReadTrack(), "track register restored");
			Assert.AreEqual(fdc1.ReadSector(), fdc2.ReadSector(), "sector register restored");
			Assert.AreEqual(drive1.CurrentCylinder, drive2.CurrentCylinder, "head position restored");

			// the restored controller reads the right data WITHOUT re-seeking (proves side + head position)
			fdc2.SetSystem(0, 1, true);
			fdc2.WriteCommand(0x80); // Read Sector at the restored track/sector regs (1, 5)
			var got = new List<byte>();
			for (int i = 0; i < 4_000_000; i++)
			{
				byte st = fdc2.ReadStatus();
				if (fdc2.DataRequest) got.Add(fdc2.ReadData());
				if ((st & ST_BUSY) == 0) break;
				fdc2.Clock(16);
			}
			if (fdc2.DataRequest) got.Add(fdc2.ReadData());
			int off = RawOffset(1, 1, 5);
			Assert.AreEqual(SecSize, got.Count, "restored read length");
			for (int i = 0; i < SecSize; i++) Assert.AreEqual(raw[off + i], got[i], $"restored read byte {i}");
		}

		[TestMethod]
		public void WriteTrack_FormatsThenReadsBack()
		{
			var (fdc, _) = MakeFdc(MakeRaw());
			Seek(fdc, 0, 1);

			// build a standard IBM format byte stream for 16 sectors of filler 0xE5, padded to a track
			var stream = new List<byte>();
			for (int s = 1; s <= SecPerTrk; s++)
			{
				for (int g = 0; g < 12; g++) stream.Add(0x4E);
				stream.Add(0x00); stream.Add(0x00); stream.Add(0x00);
				stream.Add(0xF5); stream.Add(0xF5); stream.Add(0xF5);
				stream.Add(0xFE); stream.Add(1); stream.Add(0); stream.Add((byte)s); stream.Add(1); // C H R N(=256)
				stream.Add(0xF7);
				for (int g = 0; g < 22; g++) stream.Add(0x4E);
				stream.Add(0x00); stream.Add(0x00); stream.Add(0x00);
				stream.Add(0xF5); stream.Add(0xF5); stream.Add(0xF5);
				stream.Add(0xFB);
				for (int b = 0; b < SecSize; b++) stream.Add(0xE5);
				stream.Add(0xF7);
			}
			while (stream.Count < 6250) stream.Add(0x4E);

			fdc.WriteTrack(1);
			fdc.WriteCommand(0xF0); // Write Track (format)
			int p = 0;
			for (int i = 0; i < 8_000_000; i++)
			{
				byte st = fdc.ReadStatus();
				if ((st & ST_BUSY) == 0) break;
				if (fdc.DataRequest && p < stream.Count) fdc.WriteData(stream[p++]);
				else fdc.Clock(16);
			}
			Assert.IsTrue(p >= 6250, "format stream consumed");

			var got = ReadSector(fdc, 0, 1, 10);
			Assert.AreEqual(SecSize, got.Length, "formatted sector readable");
			foreach (var b in got) Assert.AreEqual(0xE5, b, "formatted sector is filler");
		}
	}
}
