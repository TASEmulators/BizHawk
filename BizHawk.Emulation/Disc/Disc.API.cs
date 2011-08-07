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

	public partial class Disc
	{
		//main API to read a 2352-byte LBA from a disc.
		//this starts at the beginning of the disc (at the lead-in)
		//so add 150 to get to get an address in the user data area
		public void ReadLBA_2352(int lba, byte[] buffer, int offset)
		{
			Sectors[lba].Sector.Read(buffer, offset);
		}

		//main API to read a 2048-byte LBA from a disc.
		//this starts at the beginning of the disc (at the lead-in)
		//so add 150 to get to get an address in the user data area
		public void ReadLBA_2048(int lba, byte[] buffer, int offset)
		{
			byte[] temp = new byte[2352];
			Sectors[lba].Sector.Read(temp, offset);
			Array.Copy(temp, 16, buffer, offset, 2048);
		}

		//main API to determine how many LBA sectors are available
		public int LBACount { get { return Sectors.Count; } }

		//main api for reading the TOC from a disc
		public DiscTOC ReadTOC()
		{
			return TOC;
		}

        // converts LBA to minute:second:frame format.
        public static void ConvertLBAtoMSF(int lba, out byte m, out byte s, out byte f)
        {
            m = (byte) (lba / 75 / 60);
            s = (byte) ((lba - (m * 75 * 60)) / 75);
            f = (byte) (lba - (m * 75 * 60) - (s * 75));
        }

        // gets an identifying hash. hashes the first 512 sectors of 
        // the first data track on the disc.
        public string GetHash()
        {
            byte[] buffer = new byte[512*2353];
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