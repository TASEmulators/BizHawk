using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Media
{
	public class Disk
	{

		public readonly List<Track> Tracks = new List<Track>();
		public bool Valid;

	    public class Track
        {
            public int Bits;
            public byte[] Data;
            public int Density;
            public int Index;
        }
    }
}
