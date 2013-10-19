using System.Collections.Generic;

namespace BizHawk.Emulation.Computers.Commodore64.Media
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
