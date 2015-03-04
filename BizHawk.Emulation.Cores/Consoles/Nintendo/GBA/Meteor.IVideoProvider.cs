using System;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class GBA : IVideoProvider
	{
		public int VirtualWidth { get { return 240; } }
		public int VirtualHeight { get { return 160; } }
		public int BufferWidth { get { return 240; } }
		public int BufferHeight { get { return 160; } }
		public int BackgroundColor
		{
			get { return unchecked((int)0xff000000); }
		}

		public int[] GetVideoBuffer()
		{
			return videobuffer;
		}

		private int[] videobuffer;
		private GCHandle videohandle;
	}
}
