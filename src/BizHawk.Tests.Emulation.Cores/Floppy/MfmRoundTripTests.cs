using System.Collections.Generic;
using System.Text;

using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Tests.Emulation.Cores.Floppy
{
	/// <summary>
	/// Phase-1 foundation tests for the flux/MFM disk subsystem: CRC engine, low-level MFM
	/// cell encode/decode, and a full DSK-style sector-list -> synthesized MFM track -> decode round-trip
	/// (the core de-risk for the redesign).
	/// </summary>
	[TestClass]
	public sealed class MfmRoundTripTests
	{
		[TestMethod]
		public void Crc16Ccitt_MatchesStandardVector()
		{
			// The canonical CRC-16/CCITT-FALSE check value for "123456789" is 0x29B1.
			var data = Encoding.ASCII.GetBytes("123456789");
			Assert.AreEqual((ushort)0x29B1, Crc16Ccitt.Compute(data));
		}

		[TestMethod]
		public void MfmBytes_EncodeDecode_RoundTrip()
		{
			byte[] vals = { 0x00, 0xFF, 0xA1, 0x4E, 0xFE, 0x5A, 0xC3, 0x01, 0x80 };
			var w = new MfmTrackWriter();
			foreach (var v in vals) w.WriteByte(v);
			var track = w.Build();
			var r = new MfmTrackReader(track);
			for (int i = 0; i < vals.Length; i++)
				Assert.AreEqual(vals[i], r.ReadByteAt(i * 16), $"byte {i} (0x{vals[i]:X2})");
		}

		[TestMethod]
		public void A1Sync_HasKnownPattern_AndDecodesToA1()
		{
			Assert.AreEqual((ushort)0x4489, MfmTrackWriter.SyncA1);
			Assert.AreEqual((ushort)0x5224, MfmTrackWriter.SyncC2);

			var w = new MfmTrackWriter();
			w.WriteSyncA1();
			var track = w.Build();
			Assert.AreEqual((ushort)0x4489, track.Window16(0), "A1 cell pattern");
			Assert.AreEqual((byte)0xA1, new MfmTrackReader(track).ReadByteAt(0), "A1 data decode");
		}

		[TestMethod]
		public void StandardTrack_RoundTrips_AllSectors()
		{
			// A typical +3 track: 9 sectors, 512 bytes (N=2), distinct data per sector.
			var secs = new List<TrackSector>();
			for (int i = 1; i <= 9; i++)
			{
				var data = new byte[512];
				for (int j = 0; j < data.Length; j++) data[j] = (byte)((i * 31 + j) & 0xFF);
				secs.Add(new TrackSector { C = 0, H = 0, R = (byte)i, N = 2, Data = data });
			}

			var track = StandardMfmFormat.BuildStandardTrack(secs);
			var decoded = StandardMfmFormat.DecodeSectors(track);

			Assert.AreEqual(9, decoded.Count, "sector count");
			for (int i = 0; i < 9; i++)
			{
				var d = decoded[i];
				var s = secs[i];
				Assert.AreEqual(s.C, d.C, $"sec {i} C");
				Assert.AreEqual(s.H, d.H, $"sec {i} H");
				Assert.AreEqual(s.R, d.R, $"sec {i} R");
				Assert.AreEqual(s.N, d.N, $"sec {i} N");
				Assert.IsTrue(d.IdCrcOk, $"sec {i} ID CRC");
				Assert.IsTrue(d.DataCrcOk, $"sec {i} data CRC");
				Assert.IsFalse(d.Deleted, $"sec {i} deleted");
				CollectionAssert.AreEqual(s.Data, d.Data, $"sec {i} data");
			}
		}

		[TestMethod]
		public void DeletedDam_And_CorruptCrc_AreDetected()
		{
			var secs = new List<TrackSector>
			{
				new() { C = 0, H = 0, R = 1, N = 2, Data = new byte[512], Deleted = true },
				new() { C = 0, H = 0, R = 2, N = 2, Data = new byte[512], IdCrcError = true },
				new() { C = 0, H = 0, R = 3, N = 2, Data = new byte[512], DataCrcError = true },
			};

			var decoded = StandardMfmFormat.DecodeSectors(StandardMfmFormat.BuildStandardTrack(secs));

			Assert.AreEqual(3, decoded.Count);

			// Deleted DAM: flagged deleted, both CRCs still good.
			Assert.IsTrue(decoded[0].Deleted);
			Assert.IsTrue(decoded[0].IdCrcOk);
			Assert.IsTrue(decoded[0].DataCrcOk);

			// Corrupt ID CRC: ID CRC fails, but the field is still readable (CHRN correct, data still read).
			Assert.IsFalse(decoded[1].IdCrcOk);
			Assert.AreEqual((byte)2, decoded[1].R);

			// Corrupt data CRC: ID CRC good, data CRC fails.
			Assert.IsTrue(decoded[2].IdCrcOk);
			Assert.IsFalse(decoded[2].DataCrcOk);
		}

		[TestMethod]
		public void VariableSectorSizes_RoundTrip()
		{
			// N = 0 (128), 1 (256), 3 (1024) on one track.
			var secs = new List<TrackSector>();
			byte[] ns = { 0, 1, 3 };
			for (int i = 0; i < ns.Length; i++)
			{
				int size = 128 << ns[i];
				var data = new byte[size];
				for (int j = 0; j < size; j++) data[j] = (byte)((i * 7 + j) & 0xFF);
				secs.Add(new TrackSector { C = 1, H = 0, R = (byte)(i + 1), N = ns[i], Data = data });
			}

			var decoded = StandardMfmFormat.DecodeSectors(StandardMfmFormat.BuildStandardTrack(secs));
			Assert.AreEqual(3, decoded.Count);
			for (int i = 0; i < 3; i++)
			{
				Assert.AreEqual(ns[i], decoded[i].N, $"sec {i} N");
				Assert.AreEqual(128 << ns[i], decoded[i].Data.Length, $"sec {i} size");
				Assert.IsTrue(decoded[i].IdCrcOk && decoded[i].DataCrcOk, $"sec {i} CRCs");
				CollectionAssert.AreEqual(secs[i].Data, decoded[i].Data, $"sec {i} data");
			}
		}
	}
}
