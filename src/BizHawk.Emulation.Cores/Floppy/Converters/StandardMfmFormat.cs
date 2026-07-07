using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// Input to track synthesis: one sector's ID + data (what a DSK/EDSK loader produces).
	/// </summary>
	public sealed class TrackSector
	{
		public byte C, H, R, N;
		public byte[] Data = System.Array.Empty<byte>();
		public bool Deleted;        // write a Deleted DAM (0xF8) instead of a normal DAM (0xFB)
		public bool IdCrcError;     // corrupt the ID-field CRC (for error synthesis / tests)
		public bool DataCrcError;   // corrupt the data-field CRC

		/// <summary>
		/// Multiple recorded data copies for a weak sector (EDSK). When set (length greater than 1), the
		/// synthesized track writes copy 0 but flags the byte positions that differ across copies as weak,
		/// so the FDC reads them unpredictably - the native mechanism behind weak-sector copy protection.
		/// </summary>
		public byte[][] WeakCopies;

		public int SizeBytes => 128 << (N & 7);
	}

	/// <summary>
	/// Decoded result of reading a sector back off a track.
	/// </summary>
	public sealed class DecodedSector
	{
		public byte C, H, R, N;
		public byte[] Data = System.Array.Empty<byte>();
		public bool HasData;
		public bool IdCrcOk;
		public bool DataCrcOk;
		public bool Deleted;

		public int SizeBytes => 128 << (N & 7);
	}

	/// <summary>
	/// Synthesises a clean IBM System-34 MFM track from a sector list (the DSK/EDSK -> flux path) and reads
	/// sectors back off a track (the FDC-side decode). This is the Phase-1 de-risk: a DSK sector list ->
	/// real MFM track (proper A1 sync marks, IDAM/DAM, computed CRCs) -> decode -> identical CHRN/data/CRC.
	/// Note (design): a plain DSK carries no weak/gap/timing data, so this yields a *clean* track. Weak/
	/// fuzzy/protection data comes from richer formats (EDSK multi-copy, IPF) at a higher layer, not here.
	/// </summary>
	public static class StandardMfmFormat
	{
		// IBM System-34 MFM gap/sync sizes (bytes)
		private const int Gap4a = 80, Sync = 12, Gap1 = 50, Gap2 = 22, Gap4b = 40;
		private const byte GapByte = 0x4E, SyncByte = 0x00;
		private const byte IamMark = 0xFC, IdamMark = 0xFE, DamMark = 0xFB, DeletedDamMark = 0xF8;

		public static MfmTrack BuildStandardTrack(IReadOnlyList<TrackSector> sectors, int gap3 = 78)
		{
			var w = new MfmTrackWriter();

			// Gap 4a, then sync + Index Address Mark (C2 C2 C2 FC), then Gap 1
			w.WriteBytes(GapByte, Gap4a);
			w.WriteBytes(SyncByte, Sync);
			w.WriteSyncC2(); w.WriteSyncC2(); w.WriteSyncC2();
			w.WriteByte(IamMark);
			w.WriteBytes(GapByte, Gap1);

			foreach (var s in sectors)
			{
				// --- ID field: sync + (A1 A1 A1 FE C H R N CRC) ---
				w.WriteBytes(SyncByte, Sync);
				ushort crc = Crc16Ccitt.Init;
				crc = Feed(crc, 0xA1); crc = Feed(crc, 0xA1); crc = Feed(crc, 0xA1);
				w.WriteSyncA1(); w.WriteSyncA1(); w.WriteSyncA1();
				crc = Feed(crc, IdamMark); w.WriteByte(IdamMark);
				crc = Feed(crc, s.C); w.WriteByte(s.C);
				crc = Feed(crc, s.H); w.WriteByte(s.H);
				crc = Feed(crc, s.R); w.WriteByte(s.R);
				crc = Feed(crc, s.N); w.WriteByte(s.N);
				WriteCrc(w, crc, s.IdCrcError);
				w.WriteBytes(GapByte, Gap2);

				// --- Data field: sync + (A1 A1 A1 DAM data CRC) ---
				w.WriteBytes(SyncByte, Sync);
				crc = Crc16Ccitt.Init;
				crc = Feed(crc, 0xA1); crc = Feed(crc, 0xA1); crc = Feed(crc, 0xA1);
				w.WriteSyncA1(); w.WriteSyncA1(); w.WriteSyncA1();
				byte dam = s.Deleted ? DeletedDamMark : DamMark;
				crc = Feed(crc, dam); w.WriteByte(dam);

				int size = s.SizeBytes;
				// A byte position is weak if the recorded copies disagree there.
				bool[] weakByte = WeakMask(s.WeakCopies, size);
				byte[] src = s.WeakCopies is { Length: > 0 } ? s.WeakCopies[0] : s.Data;
				for (int i = 0; i < size; i++)
				{
					byte d = i < src.Length ? src[i] : (byte)0x00;
					crc = Feed(crc, d);
					if (weakByte != null && weakByte[i]) w.WriteByteWeak(d);
					else w.WriteByte(d);
				}
				WriteCrc(w, crc, s.DataCrcError);
				w.WriteBytes(GapByte, gap3);
			}

			w.WriteBytes(GapByte, Gap4b);
			return w.Build();
		}

		/// <summary>
		/// Decode a track back into a TrackSector list (the input form for re-synthesis). Used by the write
		/// path: read the current track, modify the target sector(s), then rebuild. Preserves each sector's
		/// deleted mark and CRC-error state; weak data is not reconstructed (a rewrite makes the track clean).
		/// </summary>
		public static List<TrackSector> ToTrackSectors(MfmTrack track, WeakBitRng weakRng = null)
		{
			var list = new List<TrackSector>();
			if (track == null) return list;
			foreach (var d in DecodeSectors(track, weakRng))
			{
				list.Add(new TrackSector
				{
					C = d.C, H = d.H, R = d.R, N = d.N,
					Data = d.Data,
					Deleted = d.Deleted,
					IdCrcError = !d.IdCrcOk,
					DataCrcError = !d.DataCrcOk,
				});
			}
			return list;
		}

		public static List<DecodedSector> DecodeSectors(MfmTrack track, WeakBitRng weakRng = null)
		{
			var list = new List<DecodedSector>();
			foreach (var loc in DecodeSectorLocations(track, weakRng)) list.Add(loc.Sector);
			return list;
		}

		/// <summary>
		/// A decoded sector plus where its data field sits on the track, in cells (the first data byte
		/// starts at DataStartCell, each subsequent byte 16 cells later). Used to map a sector's
		/// bytes back to track cells - e.g. to flag weak/fuzzy cells derived from cross-revolution comparison.
		/// </summary>
		public sealed class SectorLocation
		{
			public DecodedSector Sector;
			public int DataStartCell;
		}

		/// <summary>
		/// Like DecodeSectors but also reports each sector's data-field start cell.
		/// </summary>
		public static List<SectorLocation> DecodeSectorLocations(MfmTrack track, WeakBitRng weakRng = null)
		{
			var r = new MfmTrackReader(track);
			if (weakRng != null) r.WeakRng = weakRng; // shared RNG so repeated weak reads vary
			var list = new List<SectorLocation>();
			var seen = new HashSet<int>();
			int pos = 0;
			DecodedSector pending = null;

			for (; ; )
			{
				if (!r.TryFindAddressMark(ref pos, out byte mark, out _, out int syncStart))
					break;
				// one full revolution done once we return to a mark we've already decoded
				if (!seen.Add(syncStart))
					break;

				if (mark == IdamMark)
				{
					var chrn = r.ReadBytes(ref pos, 4);
					var crcBytes = r.ReadBytes(ref pos, 2);

					ushort read = (ushort)((crcBytes[0] << 8) | crcBytes[1]);
					ushort calc = IdFieldCrc(chrn[0], chrn[1], chrn[2], chrn[3]);
					pending = new DecodedSector
					{
						C = chrn[0], H = chrn[1], R = chrn[2], N = chrn[3],
						IdCrcOk = read == calc,
					};
				}
				else if (mark == DamMark || mark == DeletedDamMark)
				{
					int n = pending?.N ?? 2;
					int size = 128 << (n & 7);
					int dataStart = pos; // cell position of the first data byte (before ReadBytes advances pos)
					var data = r.ReadBytes(ref pos, size);
					var crcBytes = r.ReadBytes(ref pos, 2);

					ushort read = (ushort)((crcBytes[0] << 8) | crcBytes[1]);
					ushort calc = DataFieldCrc(mark, data);
					if (pending != null)
					{
						pending.Data = data;
						pending.HasData = true;
						pending.DataCrcOk = read == calc;
						pending.Deleted = mark == DeletedDamMark;
						list.Add(new SectorLocation { Sector = pending, DataStartCell = dataStart });
						pending = null;
					}
				}
			}
			return list;
		}

		/// <summary>
		/// Read a specific sector (matched on full CHRN) off a track, the way the FDC Read Data command
		/// locates it. Returns null if no matching sector ID is present on the track.
		/// </summary>
		public static DecodedSector ReadSectorById(MfmTrack track, byte c, byte h, byte r, byte n, WeakBitRng weakRng = null)
		{
			if (track == null) return null;
			foreach (var s in DecodeSectors(track, weakRng))
				if (s.C == c && s.H == h && s.R == r && s.N == n)
					return s;
			return null;
		}

		private static ushort Feed(ushort crc, byte b) => Crc16Ccitt.Update(crc, b);

		private static void WriteCrc(MfmTrackWriter w, ushort crc, bool corrupt)
		{
			if (corrupt) crc ^= 0xFFFF;
			w.WriteByte((byte)(crc >> 8));
			w.WriteByte((byte)(crc & 0xFF));
		}

		private static ushort IdFieldCrc(byte c, byte h, byte r, byte n)
		{
			ushort crc = Crc16Ccitt.Init;
			crc = Feed(crc, 0xA1); crc = Feed(crc, 0xA1); crc = Feed(crc, 0xA1);
			crc = Feed(crc, IdamMark);
			crc = Feed(crc, c); crc = Feed(crc, h); crc = Feed(crc, r); crc = Feed(crc, n);
			return crc;
		}

		private static ushort DataFieldCrc(byte mark, byte[] data)
		{
			ushort crc = Crc16Ccitt.Init;
			crc = Feed(crc, 0xA1); crc = Feed(crc, 0xA1); crc = Feed(crc, 0xA1);
			crc = Feed(crc, mark);
			foreach (var b in data) crc = Feed(crc, b);
			return crc;
		}

		// A byte position is weak where the recorded copies disagree.
		private static bool[] WeakMask(byte[][] copies, int size)
		{
			if (copies == null || copies.Length < 2) return null;
			var mask = new bool[size];
			for (int i = 0; i < size; i++)
			{
				byte first = i < copies[0].Length ? copies[0][i] : (byte)0;
				for (int c = 1; c < copies.Length; c++)
				{
					byte v = i < copies[c].Length ? copies[c][i] : (byte)0;
					if (v != first) { mask[i] = true; break; }
				}
			}
			return mask;
		}
	}
}
