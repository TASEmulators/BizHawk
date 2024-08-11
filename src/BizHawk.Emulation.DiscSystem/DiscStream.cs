namespace BizHawk.Emulation.DiscSystem
{
	public enum EDiscStreamView
	{
		/// <summary>
		/// views the disc as Mode 0 (aka Audio)
		/// </summary>
		DiscStreamView_Mode0_2352,

		/// <summary>
		/// views the disc as audio (aka Mode 0)
		/// </summary>
		DiscStreamView_Audio_2352 = DiscStreamView_Mode0_2352,

		/// <summary>
		/// views the disc as Mode 1
		/// </summary>
		DiscStreamView_Mode1_2048,

		/// <summary>
		/// views the disc as Mode 2
		/// </summary>
		DiscStreamView_Mode2_2336,

		/// <summary>
		/// views the disc as Mode 2 Form 1
		/// </summary>
		DiscStreamView_Mode2_Form1_2048,

		/// <summary>
		/// views the disc as Mode 2 Form 2
		/// </summary>
		DiscStreamView_Mode2_Form2_2324,
	}

	/// <summary>
	/// Allows you to stream data off a disc.
	/// For future work: depending on the View you select, it may not be seekable (in other words, it would need to read sequentially)
	///
	/// OLD COMMENTS:
	/// Allows you to stream data off a disc
	/// NOTE - it's probably been commented elsewhere, but this is possibly a bad idea! Turn it into views instead,
	/// and the exact behaviour depends on the requested access level (raw or logical) and then what type of sectors are getting used
	/// NOTE - actually even THAT is probably a bad idea. sector types can change on the fly.
	/// this class promises something it can't deliver. (it's only being used to scan an ISO disc)
	/// Well, we could make code that is full of red flags and warnings like "if this ISNT a 2048 byte sector ISO disc, then this wont work"
	///
	/// TODO - Receive some information about the track that this stream is modeling, and have the stream return EOF at the end of the track?
	/// </summary>
	public class DiscStream : System.IO.Stream
	{
		private readonly int SectorSize;
		private readonly int NumSectors;
		private Disc Disc;

		private long currPosition;
		private readonly byte[] cachedSectorBuffer;
		private int cachedSector;
		private readonly DiscSectorReader dsr;

		/// <exception cref="NotSupportedException"><paramref name="view"/> is not <see cref="DiscSectorReaderPolicy.EUserData2048Mode.AssumeMode1"/> or <see cref="DiscSectorReaderPolicy.EUserData2048Mode.AssumeMode2_Form1"/></exception>
		public DiscStream(Disc disc, EDiscStreamView view, int from_lba)
		{
			SectorSize = 2048;
			Disc = disc;
			NumSectors = disc.Session1.LeadoutLBA;
			dsr = new(disc);

			//following the provided view
			switch (view)
			{
				case EDiscStreamView.DiscStreamView_Mode1_2048:
					dsr.Policy.UserData2048Mode = DiscSectorReaderPolicy.EUserData2048Mode.AssumeMode1;
					break;
				case EDiscStreamView.DiscStreamView_Mode2_Form1_2048:
					dsr.Policy.UserData2048Mode = DiscSectorReaderPolicy.EUserData2048Mode.AssumeMode2_Form1;
					break;
				default:
					throw new NotSupportedException($"Unsupported {nameof(EDiscStreamView)}");
			}


			currPosition = from_lba * SectorSize;
			cachedSector = -1;
			cachedSectorBuffer = new byte[SectorSize];
		}

		public override bool CanRead => true;
		public override bool CanSeek => true;
		public override bool CanWrite => false;
		public override void Flush() { throw new NotImplementedException(); }
		public override long Length => NumSectors * SectorSize;

		public override long Position
		{
			get => currPosition;
			set
			{
				currPosition = value;
				//invalidate the cached sector..
				//as a later optimization, we could actually intelligently decide if this is necessary
				cachedSector = -1;
			}
		}

		internal void READLBA_Flat_Implementation(long disc_offset, byte[] buffer, int offset, int length, Action<int, byte[], int> sectorReader, int sectorSize, byte[] sectorBuf, ref int sectorBufferHint)
		{
			//hint is the sector number which is already read. to avoid repeatedly reading the sector from the disc in case of several small reads, so that sectorBuf can be used as a sector cache

		}

		//TODO - I'm not sure everything in here makes sense right now..
		public override int Read(byte[] buffer, int offset, int count)
		{
			var remainInDisc = Length - currPosition;
			if (count > remainInDisc)
				count = (int)Math.Min(remainInDisc, int.MaxValue);

			var remain = count;
			var readed = 0;
			while (remain > 0)
			{
				var lba = (int)(currPosition / SectorSize);
				var lba_within = (int)(currPosition % SectorSize);
				var todo = remain;
				var remains_in_lba = SectorSize - lba_within;
				if (remains_in_lba < todo)
					todo = remains_in_lba;
				if (cachedSector != lba)
				{
					dsr.ReadLBA_2048(lba, cachedSectorBuffer, 0);
					cachedSector = lba;
				}
				Array.Copy(cachedSectorBuffer, lba_within, buffer, offset, todo);
				offset += todo;
				remain -= todo;
				currPosition += todo;
				readed += todo;
			}

			return readed;
		}

		public override long Seek(long offset, System.IO.SeekOrigin origin)
		{
			switch (origin)
			{
				case System.IO.SeekOrigin.Begin: Position = offset; break;
				case System.IO.SeekOrigin.Current: Position += offset; break;
				case System.IO.SeekOrigin.End: Position = Length - offset; break;
			}
			return Position;
		}

		public override void SetLength(long value) { throw new NotImplementedException(); }
		public override void Write(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }
	}
}