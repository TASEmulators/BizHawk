using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class VicII : IVideoProvider
	{
		public int[] GetVideoBuffer()
		{
			return buffer;
		}

		public int VirtualWidth
		{
			get { return visibleWidth; }
		}

		public int BufferWidth
		{
			get { return visibleWidth; }
		}

		public int BufferHeight
		{
			get { return visibleHeight; }
		}

		public int BackgroundColor
		{
			get { return Colors.ARGB(0, 0, 0); }
		}
	}
}
