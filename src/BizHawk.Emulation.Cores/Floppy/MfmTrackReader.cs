namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// Decodes MFM cells back into bytes and locates address marks - the read side of the flux model,
	/// mirroring what the FDC's data separator + shift register do. Operates circularly over the track.
	/// </summary>
	public sealed class MfmTrackReader
	{
		private readonly MfmTrack _t;

		/// <summary>
		/// RNG used to resolve weak/fuzzy data cells so repeated reads of a weak sector vary. Seeded and
		/// resettable so a savestate/TAS replays identically (the seed lives in serialized state).
		/// </summary>
		public WeakBitRng WeakRng { get; set; } = new WeakBitRng(0);

		public MfmTrackReader(MfmTrack track) => _t = track;

		public int CellCount => _t.CellCount;

		/// <summary>
		/// Decode the 16-cell window at pos as one data byte (data cells are
		/// the odd cells; first data bit is the MSB). A weak data cell yields a random bit. Wraps around.
		/// </summary>
		public byte ReadByteAt(int pos)
		{
			int b = 0;
			for (int k = 0; k < 8; k++)
			{
				int cell = (pos + 1 + 2 * k) % CellCount;
				b <<= 1;
				bool bit = _t.IsWeak(cell) ? WeakRng.Next(2) != 0 : _t.GetCell(cell);
				if (bit) b |= 1;
			}
			return (byte)b;
		}

		/// <summary>
		/// From pos, scan forward for an A1 (0x4489) sync, consume consecutive A1 sync
		/// windows, and decode the following byte as the address mark. On success pos is
		/// left immediately after the mark byte, ready to read the field. Returns false if no A1 is found
		/// within one revolution.
		/// </summary>
		public bool TryFindAddressMark(ref int pos, out byte mark, out int a1Count, out int syncStart)
		{
			mark = 0;
			a1Count = 0;
			syncStart = -1;

			int scanned = 0;
			while (scanned < CellCount && _t.Window16(pos) != MfmTrackWriter.SyncA1)
			{
				pos = (pos + 1) % CellCount;
				scanned++;
			}
			if (scanned >= CellCount) return false; // no address mark on this track

			syncStart = pos; // cell position of the first A1 sync (identifies this mark uniquely)

			// consume the run of A1 sync marks
			while (_t.Window16(pos) == MfmTrackWriter.SyncA1)
			{
				a1Count++;
				pos = (pos + 16) % CellCount;
			}

			// the byte after the sync run is the address mark
			mark = ReadByteAt(pos);
			pos = (pos + 16) % CellCount;
			return true;
		}

		/// <summary>
		/// Read count MFM bytes starting at pos, advancing it.
		/// </summary>
		public byte[] ReadBytes(ref int pos, int count)
		{
			var buf = new byte[count];
			for (int i = 0; i < count; i++)
			{
				buf[i] = ReadByteAt(pos);
				pos = (pos + 16) % CellCount;
			}
			return buf;
		}

		public byte ReadByte(ref int pos)
		{
			byte b = ReadByteAt(pos);
			pos = (pos + 16) % CellCount;
			return b;
		}
	}
}
