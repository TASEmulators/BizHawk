using System.Collections.Generic;

using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>
	/// Tests for the uPD765 controller driving the flux/drive model through its register
	/// interface, including the timing model: RQM-gated execution byte cadence, overrun, timed overlapped
	/// seeks reported via Sense Interrupt Status, the interrupt line through IFdcHost, and drive rotation.
	/// </summary>
	[TestClass]
	public sealed class Upd765FdcTests
	{
		private const byte MSR_CB = 0x10, MSR_EXM = 0x20, MSR_DIO = 0x40, MSR_RQM = 0x80;
		private const byte ST0_IC_ABTERM = 0x40;
		private const byte ST0_SE = 0x20;
		private const byte ST1_NW = 0x02, ST1_ND = 0x04, ST1_OR = 0x10, ST1_EN = 0x80;
		private const byte ST3_T0 = 0x10, ST3_RY = 0x20;

		private sealed class TestHost : IFdcHost
		{
			public bool Int;
			public int IntEdges;
			public void OnFdcInterrupt(bool asserted) { Int = asserted; IntEdges++; }
			public void OnFdcDataRequest(bool asserted) { }
		}

		private static Upd765Fdc MakeFdc()
		{
			var secs = new List<TrackSector>
			{
				new() { C = 0, H = 0, R = 1, N = 2, Data = Fill(512, 0x11) },
				new() { C = 0, H = 0, R = 2, N = 2, Data = Fill(512, 0x22) },
			};
			var disk = new FluxDisk();
			disk.SetTrack(0, 0, StandardMfmFormat.BuildStandardTrack(secs));
			var drive = new FloppyDrive { Disk = disk, MotorOn = true };
			var fdc = new Upd765Fdc();
			fdc.Drives[0] = drive;
			fdc.ConfigureTiming(3_546_900);
			fdc.Reset();
			return fdc;
		}

		private static void Send(Upd765Fdc fdc, byte cmd, params byte[] ps)
		{
			fdc.WriteData(cmd);
			foreach (var p in ps) fdc.WriteData(p);
		}

		// Read `count` execution-phase bytes, advancing the clock in sub-byte chunks and reading as soon as
		// RQM is raised (so we never trip the overrun detector).
		private static byte[] ReadExecBytes(Upd765Fdc fdc, int count)
		{
			var outp = new byte[count];
			for (int i = 0; i < count; i++)
			{
				int guard = 0;
				while ((fdc.ReadStatus() & MSR_RQM) == 0)
				{
					fdc.Clock(64);
					Assert.IsTrue(++guard < 1_000_000, "timed out waiting for RQM");
				}
				outp[i] = fdc.ReadData();
			}
			return outp;
		}

		[TestMethod]
		public void ReadData_StreamsSectorBytes_OnDemand_AndReturnsResult()
		{
			var fdc = MakeFdc();
			Send(fdc, 0x46, 0x00, 0, 0, 1, 2, 1, 0x2A, 0xFF);

			byte st = fdc.ReadStatus();
			Assert.IsTrue((st & MSR_EXM) != 0 && (st & MSR_DIO) != 0 && (st & MSR_CB) != 0,
				"execution phase reports EXM+DIO+CB");
			Assert.AreEqual(MSR_RQM, (byte)(st & MSR_RQM), "the first byte is available immediately (on-demand, no cadence)");

			var data = ReadExecBytes(fdc, 512);
			for (int i = 0; i < 512; i++) Assert.AreEqual((byte)0x11, data[i], $"data byte {i}");

			var res = new byte[7];
			for (int i = 0; i < 7; i++) res[i] = fdc.ReadData();
			Assert.AreEqual(ST1_EN, (byte)(res[1] & ST1_EN), "ST1 EN set at end of requested range");
			Assert.AreEqual((byte)1, res[5], "result R = last sector read");
			Assert.AreEqual(0, fdc.ReadStatus() & MSR_CB, "controller returns to idle");
		}

		[TestMethod]
		public void ReadData_MissingSector_ReportsNoData()
		{
			var fdc = MakeFdc();
			Send(fdc, 0x46, 0x00, 0, 0, 9, 2, 9, 0x2A, 0xFF); // R=9 does not exist

			byte st = fdc.ReadStatus();
			Assert.AreEqual(0, st & MSR_EXM, "no execution phase (no data)");

			var res = new byte[7];
			for (int i = 0; i < 7; i++) res[i] = fdc.ReadData();
			Assert.AreEqual(ST1_ND, (byte)(res[1] & ST1_ND), "ST1 ND (no data) set");
		}

		[TestMethod]
		public void ReadId_ReturnsASectorChrn()
		{
			var fdc = MakeFdc();
			Send(fdc, 0x4A, 0x00); // Read ID (0x0A) + MFM

			var res = new byte[7];
			for (int i = 0; i < 7; i++) res[i] = fdc.ReadData();
			Assert.IsTrue(res[5] == 1 || res[5] == 2, "Read ID returns a real sector id");
			Assert.AreEqual((byte)2, res[6], "N reported");
		}

		[TestMethod]
		public void Seek_IsTimed_AndReportedViaSenseInterrupt_WithInterruptLine()
		{
			var fdc = MakeFdc();
			var host = new TestHost();
			fdc.Host = host;

			Send(fdc, 0x0F, 0x00, 0x05); // Seek unit 0 to cylinder 5
			Assert.IsFalse(fdc.IntPending, "seek not complete immediately");
			Assert.IsTrue((fdc.ReadStatus() & 0x01) != 0, "drive 0 busy bit set while seeking");

			// advance well past 5 steps at the programmed step rate
			fdc.Clock(2_000_000);
			Assert.IsTrue(fdc.IntPending, "seek completion raised the interrupt");
			Assert.IsTrue(host.Int, "host saw INT asserted");
			Assert.AreEqual(0, fdc.ReadStatus() & 0x0F, "drive busy bit cleared after seek");

			Send(fdc, 0x08); // Sense Interrupt Status
			byte st0 = fdc.ReadData();
			byte pcn = fdc.ReadData();
			Assert.AreEqual(ST0_SE, (byte)(st0 & ST0_SE), "ST0 Seek End set");
			Assert.AreEqual((byte)5, pcn, "present cylinder number = 5");
			Assert.IsFalse(fdc.IntPending, "sensing the interrupt cleared it");
			Assert.IsFalse(host.Int, "host saw INT deasserted");

			Send(fdc, 0x08); // nothing pending now
			Assert.AreEqual((byte)0x80, fdc.ReadData(), "no interrupt pending -> IC invalid");
		}

		[TestMethod]
		public void SenseDrive_ReportsReadyAndTrack0()
		{
			var fdc = MakeFdc();
			Send(fdc, 0x04, 0x00); // Sense Drive Status, unit 0
			byte st3 = fdc.ReadData();
			Assert.AreEqual(ST3_RY, (byte)(st3 & ST3_RY), "ready");
			Assert.AreEqual(ST3_T0, (byte)(st3 & ST3_T0), "track 0");
		}

		[TestMethod]
		public void Drive_SpinsUp_AndPulsesIndexOncePerRevolution()
		{
			var drive = new FloppyDrive { MotorOn = true };
			drive.ConfigureTiming(3_546_900);
			Assert.IsFalse(drive.AtSpeed, "not at speed before spin-up");

			drive.Clock(3_600_000); // just over one second
			Assert.IsTrue(drive.AtSpeed, "at speed after spin-up");
			Assert.IsTrue(drive.Index, "index pulse present at rotation start");

			int halfRev = (int)(3_546_900L * 200 / 1000 / 2);
			drive.Clock(halfRev);
			Assert.IsFalse(drive.Index, "no index pulse mid-revolution");
			drive.Clock(halfRev);
			Assert.IsTrue(drive.Index, "index pulse again after a full revolution");
		}

		[TestMethod]
		public void ReadDeletedData_ReadsDeletedSector_And_ReadData_FlagsControlMark()
		{
			// a track with a normal sector (R=1) and a deleted-address-mark sector (R=2)
			var secs = new List<TrackSector>
			{
				new() { C = 0, H = 0, R = 1, N = 2, Data = Fill(512, 0x11) },
				new() { C = 0, H = 0, R = 2, N = 2, Data = Fill(512, 0x22), Deleted = true },
			};
			var disk = new FluxDisk();
			disk.SetTrack(0, 0, StandardMfmFormat.BuildStandardTrack(secs));
			var fdc = new Upd765Fdc { Drives = { [0] = new FloppyDrive { Disk = disk, MotorOn = true } } };
			fdc.ConfigureTiming(3_546_900);
			fdc.Reset();

			// Read Deleted Data (0x0C + MFM = 0x4C) for the deleted sector: reads its data, no control mark
			Send(fdc, 0x4C, 0x00, 0, 0, 2, 2, 2, 0x2A, 0xFF);
			var data = ReadExecBytes(fdc, 512);
			for (int i = 0; i < 512; i++) Assert.AreEqual((byte)0x22, data[i], $"deleted sector data byte {i}");
			var res = new byte[7];
			for (int i = 0; i < 7; i++) res[i] = fdc.ReadData();
			Assert.AreEqual(0, res[2] & 0x40, "no Control Mark: a deleted sector read by Read Deleted Data is a match");

			// Read Data (0x46) for the same deleted sector: transfers it but flags ST2 Control Mark (bit 6)
			Send(fdc, 0x46, 0x00, 0, 0, 2, 2, 2, 0x2A, 0xFF);
			var d2 = ReadExecBytes(fdc, 512);
			Assert.AreEqual((byte)0x22, d2[0], "data still transferred");
			var r2 = new byte[7];
			for (int i = 0; i < 7; i++) r2[i] = fdc.ReadData();
			Assert.AreEqual(0x40, r2[2] & 0x40, "Read Data on a deleted-DAM sector sets ST2 Control Mark");
		}

		// Supply execution-phase bytes to the FDC (Write/Format), advancing the clock in sub-byte chunks and
		// writing as soon as the FDC raises RQM to request one.
		private static void WriteExecBytes(Upd765Fdc fdc, byte[] data)
		{
			foreach (var b in data)
			{
				int guard = 0;
				while ((fdc.ReadStatus() & MSR_RQM) == 0)
				{
					fdc.Clock(64);
					Assert.IsTrue(++guard < 1_000_000, "timed out waiting for write RQM");
				}
				fdc.WriteData(b);
			}
		}

		[TestMethod]
		public void WriteData_RoundTripsThroughFlux()
		{
			var fdc = MakeFdc();

			// Write Data (0x05) + MFM = 0x45 to sector R=1
			Send(fdc, 0x45, 0x00, 0, 0, 1, 2, 1, 0x2A, 0xFF);
			byte st = fdc.ReadStatus();
			Assert.AreEqual(0, st & MSR_DIO, "write execution direction is host -> FDC (DIO=0)");
			WriteExecBytes(fdc, Fill(512, 0x55));

			var res = new byte[7];
			for (int i = 0; i < 7; i++) res[i] = fdc.ReadData();
			Assert.AreEqual(0, res[0] & 0xC0, "normal termination");
			Assert.AreEqual(ST1_EN, (byte)(res[1] & ST1_EN), "ST1 EN set");

			// read the same sector back and confirm the new contents persisted into the flux
			Send(fdc, 0x46, 0x00, 0, 0, 1, 2, 1, 0x2A, 0xFF);
			var back = ReadExecBytes(fdc, 512);
			for (int i = 0; i < 512; i++) Assert.AreEqual((byte)0x55, back[i], $"read-back byte {i}");
			for (int i = 0; i < 7; i++) fdc.ReadData(); // drain result
		}

		[TestMethod]
		public void WriteData_WriteProtected_SetsNotWritable()
		{
			var secs = new List<TrackSector> { new() { C = 0, H = 0, R = 1, N = 2, Data = Fill(512, 0x11) } };
			var disk = new FluxDisk { WriteProtected = true };
			disk.SetTrack(0, 0, StandardMfmFormat.BuildStandardTrack(secs));
			var fdc = new Upd765Fdc();
			fdc.Drives[0] = new FloppyDrive { Disk = disk, MotorOn = true };
			fdc.ConfigureTiming(3_546_900);
			fdc.Reset();

			Send(fdc, 0x45, 0x00, 0, 0, 1, 2, 1, 0x2A, 0xFF);
			Assert.AreEqual(0, fdc.ReadStatus() & MSR_EXM, "no execution phase on a protected disk");
			var res = new byte[7];
			for (int i = 0; i < 7; i++) res[i] = fdc.ReadData();
			Assert.AreEqual(ST1_NW, (byte)(res[1] & ST1_NW), "ST1 NW (not writable) set");
		}

		[TestMethod]
		public void Format_LaysDownSectorsFilledWithGapByte()
		{
			var fdc = MakeFdc();

			// Format Track (0x0D) + MFM = 0x4D; params: HD/US, N, SC, GPL, filler
			Send(fdc, 0x4D, 0x00, 2, 3, 0x2A, 0xE5);
			WriteExecBytes(fdc, new byte[] { 0, 0, 1, 2, 0, 0, 2, 2, 0, 0, 3, 2 }); // C H R N per sector
			for (int i = 0; i < 7; i++) fdc.ReadData(); // drain result

			// the freshly formatted sector 3 should read back as the filler byte
			Send(fdc, 0x46, 0x00, 0, 0, 3, 2, 3, 0x2A, 0xFF);
			var back = ReadExecBytes(fdc, 512);
			for (int i = 0; i < 512; i++) Assert.AreEqual((byte)0xE5, back[i], $"formatted byte {i}");
			for (int i = 0; i < 7; i++) fdc.ReadData();
		}

		[TestMethod]
		public void Eme150Drive_HasDatasheetGeometryAndSeekRespectsStepFloorAndSettle()
		{
			var disk = new FluxDisk();
			disk.SetTrack(0, 0, StandardMfmFormat.BuildStandardTrack(
				new List<TrackSector> { new() { C = 0, H = 0, R = 1, N = 2, Data = Fill(512, 1) } }));
			var drive = new Eme150Drive { Disk = disk, MotorOn = true };
			var fdc = new Upd765Fdc();
			fdc.Drives[0] = drive;
			fdc.ConfigureTiming(3_546_900);
			fdc.Reset();

			Assert.AreEqual(40, drive.CylinderCount, "40 cylinders");
			Assert.AreEqual(12, drive.TrackToTrackMs);
			Assert.AreEqual(15, drive.SettleMs);

			// seek 0 -> 5: five 12 ms steps + 15 ms settle = 75 ms. It must NOT be done before ~74 ms
			// (SRT alone would be far faster), and must complete by ~80 ms.
			Send(fdc, 0x0F, 0x00, 0x05);
			long cyclesPerMs = 3_546_900 / 1000;
			fdc.Clock((int)(70 * cyclesPerMs));
			Assert.IsFalse(fdc.IntPending, "step floor + settle keep the seek pending at 70 ms");
			fdc.Clock((int)(12 * cyclesPerMs));
			Assert.IsTrue(fdc.IntPending, "seek complete by ~82 ms");

			Send(fdc, 0x08);
			byte st0 = fdc.ReadData();
			byte pcn = fdc.ReadData();
			Assert.AreEqual(0x20, (byte)(st0 & 0x20), "seek end");
			Assert.AreEqual((byte)5, pcn);

			// 40 is the nominal track count, but the head can travel past it (disks are over-formatted),
			// so a seek to an over-format cylinder is honoured rather than clamped.
			Assert.AreEqual(40, drive.CylinderCount, "nominal cylinder count");
			drive.SeekTo(41);
			Assert.AreEqual(41, drive.CurrentCylinder, "over-format seek honoured");
		}

		private static byte[] Fill(int n, byte v)
		{
			var a = new byte[n];
			for (int i = 0; i < n; i++) a[i] = v;
			return a;
		}
	}
}
