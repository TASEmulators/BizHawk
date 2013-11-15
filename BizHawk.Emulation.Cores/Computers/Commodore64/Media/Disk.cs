using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public class Track
	{
		public int bits;
		public byte[] data;
		public int density;
		public int index;
	}

	public class Disk
	{

		public List<Track> tracks = new List<Track>();
		public bool valid;
	}
}
