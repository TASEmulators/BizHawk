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

	//TODO - rename these APIs to ReadSector
	public partial class Disc
	{
		/// <summary>
		/// Main API to read a 2352-byte sector from a disc.
		/// This starts at the beginning of the "userdata" area of the disc (track 1, index 0)
		/// However, located here is a mandatory pregap of 2 seconds (or more?).
		/// so you may need to add 150 depending on how your system addresses things to get to the "start" of the first track. (track 1, index 1)
		/// </summary>
		public void ReadLBA_2352(int lba, byte[] buffer, int offset)
		{
			Sectors[lba].Sector.Read(buffer, offset);
		}

		/// <summary>
		/// Returns a SectorEntry from which you can retrieve various interesting pieces of information about the sector.
		/// The SectorEntry's interface is not likely to be stable, though, but it may be more convenient.
		/// </summary>
		public SectorEntry ReadSectorEntry(int lba)
		{
			return Sectors[lba];
		}

		/// <summary>
		/// Reads the specified sector's subcode (96 bytes) deinterleaved into the provided buffer.
		/// P is first 12 bytes, followed by 12 Q bytes, etc.
		/// I'm not sure what format scsi commands generally return it in. 
		/// It could be this, or RAW (interleaved) which I could also supply when we need it
		/// </summary>
		public void ReadSector_Subcode_Deinterleaved(int lba, byte[] buffer, int offset)
		{
			Array.Clear(buffer, offset, 96);
			Sectors[lba].Read_SubchannelQ(buffer, offset + 12);
		}

		/// <summary>
		/// Reads the specified sector's subchannel Q (12 bytes) into the provided buffer
		/// </summary>
		public void ReadSector_Subchannel_Q(int lba, byte[] buffer, int offset)
		{
			Sectors[lba].Read_SubchannelQ(buffer, offset);
		}

		/// <summary>
		/// Main API to read a 2048-byte sector from a disc.
		/// This starts at the beginning of the "userdata" area of the disc (track 1, index 0)
		/// However, located here is a mandatory pregap of 2 seconds (or more?).
		/// so you may need to add 150 depending on how your system addresses things to get to the "start" of the first track. (track 1, index 1)
		/// </summary>
		public void ReadLBA_2048(int lba, byte[] buffer, int offset)
		{
			byte[] temp = new byte[2352];
			Sectors[lba].Sector.Read(temp, offset);
			Array.Copy(temp, 16, buffer, offset, 2048);
		}

		/// <summary>
		/// Main API to determine how many sectors are available on the disc.
		/// This counts from absolute sector 0 to the final sector available.
		/// </summary>
		public int LBACount { get { return Sectors.Count; } }

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
            m = (byte) (lba / 75 / 60);
            s = (byte) ((lba - (m * 75 * 60)) / 75);
            f = (byte) (lba - (m * 75 * 60) - (s * 75));
        }

        // converts MSF to LBA offset
        public static int ConvertMSFtoLBA(byte m, byte s, byte f)
        {
            return f + (s*75) + (m*75*60);
        }

        // gets an identifying hash. hashes the first 512 sectors of 
        // the first data track on the disc.
        public string GetHash()
        {
            byte[] buffer = new byte[512*2352];
            foreach (var track in TOC.Sessions[0].Tracks)
            {
                if (track.TrackType == ETrackType.Audio)
                    continue;

                int lba_len = Math.Min(track.length_lba, 512);
                for (int s=0; s<512 && s<track.length_lba; s++)
                    ReadLBA_2352(track.Indexes[1].lba + s, buffer, s*2352);

                return Util.Hash_MD5(buffer, 0, lba_len*2352);
            }
            return "no data track found";
        }
	}
}