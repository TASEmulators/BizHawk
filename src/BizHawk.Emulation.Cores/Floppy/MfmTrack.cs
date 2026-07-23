using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// A single track represented as a stream of MFM cells (bit-packed). This is the canonical
	/// "bitstream" the FDC decodes - one cell per bit-time, two cells (clock,data) per encoded data bit.
	/// Cell 0 is the first cell after the index. Uniform-density MFM for now; variable per-region cell
	/// timing (for IPF Copylock/Speedlock density) is a later layer on top of this.
	/// An optional per-cell weak mask marks fuzzy cells (copy protection) that read unpredictably.
	/// Part of the shared floppy disk subsystem.
	/// </summary>
	public sealed class MfmTrack
	{
		private readonly byte[] _cells;   // bit-packed, cell i at _cells[i>>3] bit (i&7)
		private readonly byte[] _weak;    // bit-packed weak-cell mask (null = no weak cells)
		public int CellCount { get; }

		public MfmTrack(byte[] packedCells, int cellCount, byte[] weakMask = null)
		{
			_cells = packedCells;
			_weak = weakMask;
			CellCount = cellCount;
		}

		public bool GetCell(int i) => (_cells[i >> 3] & (1 << (i & 7))) != 0;

		/// <summary>
		/// True if this cell is weak/fuzzy - it reads unpredictably (the mechanism behind weak-sector copy
		/// protection). The reader substitutes a random bit for a weak data cell.
		/// </summary>
		public bool IsWeak(int i) => _weak != null && (_weak[i >> 3] & (1 << (i & 7))) != 0;

		/// <summary>
		/// Read a 16-cell window starting at pos as a big-endian 16-bit value
		/// (cell at pos = bit 15). Used to match sync patterns. Wraps around the track (circular).
		/// </summary>
		public ushort Window16(int pos)
		{
			ushort v = 0;
			for (int i = 0; i < 16; i++)
			{
				v = (ushort)(v << 1);
				if (GetCell((pos + i) % CellCount)) v |= 1;
			}
			return v;
		}
	}

	/// <summary>
	/// Builds an MfmTrack by appending MFM-encoded bytes and the special missing-clock sync
	/// marks. Pure cell encoding - CRC is the format builder's concern (it feeds the same bytes to
	/// Crc16Ccitt and writes the resulting CRC bytes via WriteByte). Bytes
	/// written via WriteByteWeak have their cells flagged weak/fuzzy.
	/// </summary>
	public sealed class MfmTrackWriter
	{
		// The two IBM System-34 missing-clock sync patterns (16 cells each, MSB = first cell):
		public const ushort SyncA1 = 0x4489; // encodes data 0xA1 with a suppressed clock - the ID/DAM sync
		public const ushort SyncC2 = 0x5224; // encodes data 0xC2 with a suppressed clock - the IAM sync

		private readonly List<bool> _cells = new List<bool>(120_000);
		private readonly List<bool> _weak = new List<bool>(120_000);
		private int _prevDataBit; // last DATA bit emitted, for the MFM clock rule
		private bool _weakMode;   // when true, appended cells are flagged weak

		public int CellCount => _cells.Count;

		private void AddCell(bool cell)
		{
			_cells.Add(cell);
			_weak.Add(_weakMode);
		}

		private void EmitDataBit(int dataBit)
		{
			// MFM clock rule: a clock cell is set only between two zero data bits
			int clock = (_prevDataBit == 0 && dataBit == 0) ? 1 : 0;
			AddCell(clock != 0);
			AddCell(dataBit != 0);
			_prevDataBit = dataBit;
		}

		/// <summary>
		/// Append one MFM-encoded data byte (MSB first). Does NOT touch any CRC.
		/// </summary>
		public void WriteByte(byte b)
		{
			for (int i = 7; i >= 0; i--)
				EmitDataBit((b >> i) & 1);
		}

		/// <summary>
		/// Append an MFM-encoded byte whose cells are flagged weak/fuzzy (reads vary per pass).
		/// </summary>
		public void WriteByteWeak(byte b)
		{
			_weakMode = true;
			WriteByte(b);
			_weakMode = false;
		}

		public void WriteBytes(byte value, int count)
		{
			for (int i = 0; i < count; i++) WriteByte(value);
		}

		public void WriteBytes(byte[] data)
		{
			foreach (var b in data) WriteByte(b);
		}

		private void EmitFixed16(ushort pattern)
		{
			for (int i = 15; i >= 0; i--)
				AddCell(((pattern >> i) & 1) != 0);
		}

		/// <summary>
		/// Append an A1 (0x4489) sync mark. Data-bit continuity: A1's last data bit is 1.
		/// </summary>
		public void WriteSyncA1()
		{
			EmitFixed16(SyncA1);
			_prevDataBit = 1;
		}

		/// <summary>
		/// Append a C2 (0x5224) sync mark (IAM). C2's last data bit is 0.
		/// </summary>
		public void WriteSyncC2()
		{
			EmitFixed16(SyncC2);
			_prevDataBit = 0;
		}

		/// <summary>
		/// Append raw cells taken verbatim from sample (MSB first), for stream data that is
		/// already at the cell level (IPF Sync/Raw elements, which carry the recorded flux bits directly).
		/// </summary>
		public void WriteRawCells(byte[] sample, int cellCount, bool weak = false)
		{
			_weakMode = weak;
			for (int i = 0; i < cellCount; i++)
			{
				int idx = i >> 3;
				bool bit = idx < sample.Length && ((sample[idx] >> (7 - (i & 7))) & 1) != 0;
				AddCell(bit);
				_prevDataBit = bit ? 1 : 0;
			}
			_weakMode = false;
		}

		/// <summary>
		/// Append cellCount weak/fuzzy cells (IPF Fuzzy: consumer-generated bits).
		/// </summary>
		public void WriteWeakCells(int cellCount)
		{
			_weakMode = true;
			for (int i = 0; i < cellCount; i++) AddCell(false);
			_weakMode = false;
			_prevDataBit = 0;
		}

		public MfmTrack Build()
		{
			int n = _cells.Count;
			var packed = new byte[(n + 7) >> 3];
			bool anyWeak = false;
			for (int i = 0; i < n; i++)
			{
				if (_cells[i]) packed[i >> 3] |= (byte)(1 << (i & 7));
				if (_weak[i]) anyWeak = true;
			}

			byte[] weak = null;
			if (anyWeak)
			{
				weak = new byte[(n + 7) >> 3];
				for (int i = 0; i < n; i++)
					if (_weak[i]) weak[i >> 3] |= (byte)(1 << (i & 7));
			}

			return new MfmTrack(packed, n, weak);
		}
	}
}
