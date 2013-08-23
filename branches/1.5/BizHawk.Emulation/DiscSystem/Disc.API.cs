using System;
using System.Collections.Generic;

//main apis for emulator core routine use

namespace BizHawk.DiscSystem
{
	public class DiscReferenceException : Exception
	{
		public DiscReferenceException(string fname, Exception inner)
			: base(string.Format("A disc attempted to reference a file which could not be accessed or loaded: {0}", fname),inner)
		{
		}
		public DiscReferenceException(string fname, string extrainfo)
			: base(string.Format("A disc attempted to reference a file which could not be accessed or loaded:\n\n{0}\n\n{1}", fname, extrainfo))
		{
		}
	}

	public class ProgressReport
	{
		public string Message;
		public bool InfoPresent;
		public double ProgressEstimate;
		public double ProgressCurrent;
		public int TaskCount;
		public int TaskCurrent;
		public bool CancelSignal;
	}

	public class DiscHopper
	{
		public Disc CurrentDisc;

		public Queue<Disc> Queue = new Queue<Disc>();

		public void Enqueue(Disc disc)
		{
			Queue.Enqueue(disc);
		}

		public void Next()
		{
			if (Queue.Count != 0) Queue.Dequeue();
		}
		public void Eject()
		{
			CurrentDisc = null;
		}
		public void Insert()
		{
			if (Queue.Count > 0)
				CurrentDisc = Queue.Peek();
		}

		public void Clear()
		{
			CurrentDisc = null;
			Queue.Clear();
		}
	}

	/// <summary>
	/// Allows you to stream data off a disc
	/// </summary>
	public class DiscStream : System.IO.Stream
	{
		int SectorSize;
		int NumSectors;
		Disc Disc;

		long currPosition;
		int cachedSector;
		byte[] cachedSectorBuffer;

		public static DiscStream Open_LBA_2048(Disc disc)
		{
			var ret = new DiscStream();
			ret._Open_LBA_2048(disc);
			return ret;
		}

		void _Open_LBA_2048(Disc disc)
		{
			SectorSize = 2048;
			this.Disc = disc;
			NumSectors = disc.LBACount;

			currPosition = 0;
			cachedSector = -1;
			cachedSectorBuffer = new byte[SectorSize];
		}

		public override bool CanRead { get { return true; } }
		public override bool CanSeek { get { return true; } }
		public override bool CanWrite { get { return false; } }
		public override void Flush() { throw new NotImplementedException(); }
		public override long Length { get { return NumSectors * SectorSize; } }

		public override long Position
		{
			get { return currPosition; }
			set
			{
				currPosition = value;
				//invalidate the cached sector..
				//as a later optimization, we could actually intelligently decide if this is necessary
				cachedSector = -1;
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			long remain = Length - currPosition;
			if (count > remain)
				count = (int)Math.Min(remain,int.MaxValue);
			Disc.READLBA_Flat_Implementation(currPosition, buffer, offset, count, (a, b, c) => Disc.ReadLBA_2048(a, b, c), SectorSize, cachedSectorBuffer, ref cachedSector);
			currPosition += count;
			return count;
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

	public partial class Disc
	{
		/// <summary>
		/// Main API to read a 2352-byte sector from a disc.
		/// This starts after the mandatory pregap of 2 seconds (but what happens if there is more more?).
		/// </summary>
		public void ReadLBA_2352(int lba, byte[] buffer, int offset)
		{
			ReadABA_2352(lba + 150, buffer, offset);
		}

		/// <summary>
		/// Main API to read a 2048-byte sector from a disc.
		/// This starts after the mandatory pregap of 2 seconds (but what happens if there is more more?).
		/// </summary>
		public void ReadLBA_2048(int lba, byte[] buffer, int offset)
		{
			ReadABA_2048(lba + 150, buffer, offset);
		}

		internal void ReadABA_2352(int aba, byte[] buffer, int offset)
		{
			Sectors[aba].Sector.Read_2352(buffer, offset);
		}

		internal void ReadABA_2048(int aba, byte[] buffer, int offset)
		{
			Sectors[aba].Sector.Read_2048(buffer, offset);
		}

		/// <summary>
		/// reads logical data from a flat disc address space
		/// useful for plucking data from a known location on the disc
		/// </summary>
		public void ReadLBA_2352_Flat(long disc_offset, byte[] buffer, int offset, int length)
		{
			int secsize = 2352;
			byte[] lba_buf = new byte[secsize];
			int sectorHint = -1;
			READLBA_Flat_Implementation(disc_offset, buffer, offset, length, (a, b, c) => ReadLBA_2352(a, b, c), secsize, lba_buf, ref sectorHint);
		}

		/// <summary>
		/// reads logical data from a flat disc address space
		/// useful for plucking data from a known location on the disc
		/// </summary>
		public void ReadLBA_2048_Flat(long disc_offset, byte[] buffer, int offset, int length)
		{
			int secsize = 2048;
			byte[] lba_buf = new byte[secsize];
			int sectorHint = -1;
			READLBA_Flat_Implementation(disc_offset, buffer, offset, length, (a, b, c) => ReadLBA_2048(a, b, c), secsize, lba_buf, ref sectorHint);
		}

		internal void READLBA_Flat_Implementation(long disc_offset, byte[] buffer, int offset, int length, Action<int, byte[], int> sectorReader, int sectorSize, byte[] sectorBuf, ref int sectorBufferHint)
		{
			//hint is the sector number which is already read. to avoid repeatedly reading the sector from the disc in case of several small reads, so that sectorBuf can be used as a sector cache
			while (length > 0)
			{
				int lba = (int)(disc_offset / sectorSize);
				int lba_within = (int)(disc_offset % sectorSize);
				int todo = length;
				int remains_in_lba = sectorSize - lba_within;
				if (remains_in_lba < todo)
					todo = remains_in_lba;
				if(sectorBufferHint != lba)
					sectorReader(lba, sectorBuf, 0);
				sectorBufferHint = lba;
				Array.Copy(sectorBuf, lba_within, buffer, offset, todo);
				offset += todo;
				length -= todo;
				disc_offset += todo;
			}
		}

		/// <summary>
		/// Returns a SectorEntry from which you can retrieve various interesting pieces of information about the sector.
		/// The SectorEntry's interface is not likely to be stable, though, but it may be more convenient.
		/// </summary>
		public SectorEntry ReadLBA_SectorEntry(int lba)
		{
			return Sectors[lba + 150];
		}

		/// <summary>
		/// Reads the specified LBA's subcode (96 bytes) deinterleaved into the provided buffer.
		/// P is first 12 bytes, followed by 12 Q bytes, etc.
		/// I'm not sure what format scsi commands generally return it in. 
		/// It could be this, or RAW (interleaved) which I could also supply when we need it
		/// </summary>
		public void ReadLBA_Subcode_Deinterleaved(int lba, byte[] buffer, int offset)
		{
			Array.Clear(buffer, offset, 96);
			Sectors[lba + 150].Read_SubchannelQ(buffer, offset + 12);
		}

		/// <summary>
		/// Reads the specified LBA's subchannel Q (12 bytes) into the provided buffer
		/// </summary>
		public void ReadLBA_Subchannel_Q(int lba, byte[] buffer, int offset)
		{
			Sectors[lba + 150].Read_SubchannelQ(buffer, offset);
		}

		/// <summary>
		/// Main API to determine how many LBAs are available on the disc.
		/// This counts from LBA 0 to the final sector available.
		/// </summary>
		public int LBACount { get { return ABACount - 150; } }

		/// <summary>
		/// Main API to determine how many ABAs (sectors) are available on the disc.
		/// This counts from ABA 0 to the final sector available.
		/// </summary>
		public int ABACount { get { return Sectors.Count; } }

		/// <summary>
		/// indicates whether this disc took significant work to load from the hard drive (i.e. decoding of ECM or audio data)
		/// In this case, the user may appreciate a prompt to export the disc so that it won't take so long next time.
		/// </summary>
		public bool WasSlowLoad { get; private set; }

		/// <summary>
		/// main api for reading the TOC from a disc
		/// </summary>
		public DiscTOC ReadTOC()
		{
			return TOC;
		}

		// converts LBA to minute:second:frame format.
		//TODO - somewhat redundant with Timestamp, which is due for refactoring into something not cue-related
		public static void ConvertLBAtoMSF(int lba, out byte m, out byte s, out byte f)
		{
			lba += 150;
			m = (byte)(lba / 75 / 60);
			s = (byte)((lba - (m * 75 * 60)) / 75);
			f = (byte)(lba - (m * 75 * 60) - (s * 75));
		}

		// converts MSF to LBA offset
		public static int ConvertMSFtoLBA(byte m, byte s, byte f)
		{
			return f + (s * 75) + (m * 75 * 60) - 150;
		}

		// gets an identifying hash. hashes the first 512 sectors of 
		// the first data track on the disc.
		public string GetHash()
		{
			byte[] buffer = new byte[512 * 2352];
			foreach (var track in TOC.Sessions[0].Tracks)
			{
				if (track.TrackType == ETrackType.Audio)
					continue;

				int lba_len = Math.Min(track.length_aba, 512);
				for (int s = 0; s < 512 && s < track.length_aba; s++)
					ReadABA_2352(track.Indexes[1].aba + s, buffer, s * 2352);

				return Util.Hash_MD5(buffer, 0, lba_len * 2352);
			}
			return "no data track found";
		}
	}
}