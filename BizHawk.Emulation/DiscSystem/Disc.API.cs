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
			Sectors[aba].Sector.Read(buffer, offset);
		}

		internal void ReadABA_2048(int aba, byte[] buffer, int offset)
		{
			byte[] temp = new byte[2352];
			Sectors[aba].Sector.Read(temp, offset);
			Array.Copy(temp, 16, buffer, offset, 2048);
		}

		/// <summary>
		/// reads logical data from a flat disc address space
		/// useful for plucking data from a known location on the disc
		/// </summary>
		public void ReadLBA_2352_Flat(long disc_offset, byte[] buffer, int offset, int length)
		{
			int secsize = 2352;
			byte[] lba_buf = new byte[secsize];
			while(length > 0)
			{
				int lba = (int)(disc_offset / secsize);
				int lba_within = (int)(disc_offset % secsize);
				int todo = length;
				int remains_in_lba = secsize - lba_within;
				if (remains_in_lba < todo)
					todo = remains_in_lba;
				ReadLBA_2352(lba, lba_buf, 0);
				Array.Copy(lba_buf, lba_within, buffer, offset, todo);
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

		/// <summary>
		/// this isn't quite right...
		/// </summary>
		/// <returns></returns>
		public bool DetectSegaSaturn()
		{
			byte[] data = new byte[2048];
			ReadLBA_2048(0, data, 0);
			byte[] cmp = System.Text.Encoding.ASCII.GetBytes("SEGA SEGASATURN");
			byte[] cmp2 = new byte[15];
			Buffer.BlockCopy(data, 0, cmp2, 0, 15);
			return System.Linq.Enumerable.SequenceEqual(cmp, cmp2);
		}
	}
}