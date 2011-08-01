using System;

//main apis for emulator core routine use

namespace BizHawk.Disc
{

	public partial class Disc
	{
		//main API to read a 2352-byte LBA from a disc.
		//this starts at the beginning of the disc (at the lead-in)
		//so add 150 to get to a FAD-address in the user data area
		public void ReadLBA_2352(int lba, byte[] buffer, int offset)
		{
			if (lba < 150)
			{
				//lead-in area not supported yet
				//in the future it will return something to mate with the 
				//subchannel data which we will load or calculate from the TOC
				return;
			}

			Sectors[lba - 150].Sector.Read(buffer, offset);
		}

		//main API to read a 2048-byte LBA from a disc.
		//this starts at the beginning of the disc (at the lead-in)
		//so add 150 to get to a FAD-address in the user data area
		public void ReadLBA_2048(int lba, byte[] buffer, int offset)
		{
			if (lba < 150)
			{
				//lead-in area not supported yet
				//in the future it will return something to mate with the 
				//subchannel data which we will load or calculate from the TOC
				return;
			}

			byte[] temp = new byte[2352];
			Sectors[lba - 150].Sector.Read(temp, offset);
			Array.Copy(temp, 16, buffer, offset, 2048);
		}

		//main API to determine how many LBA sectors are available
		public int LBACount { get { return Sectors.Count + 150; } }

		//main api for reading the TOC from a disc
		public DiscTOC ReadTOC()
		{
			return TOC;
		}

        public static void ConvertLBAtoMSF(int lba, out byte m, out byte s, out byte f)
        {
            m = (byte) (lba / 75 / 60);
            s = (byte) ((lba - (m * 75 * 60)) / 75);
            f = (byte) (lba - (m * 75 * 60) - (s * 75));
        }
	}
}