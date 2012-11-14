using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public class Disk
	{
		public class Track
		{
			public int bits;
			public byte[] data;
			public int density;
			public int index;
		}

		public List<Track> tracks = new List<Track>();
		public bool valid;
	}
}
