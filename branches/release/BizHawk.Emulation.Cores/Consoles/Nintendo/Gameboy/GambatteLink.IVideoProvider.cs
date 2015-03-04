using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : IVideoProvider
	{
		public int VirtualWidth { get { return 320; } }
		public int VirtualHeight { get { return 144; } }
		public int BufferWidth { get { return 320; } }
		public int BufferHeight { get { return 144; } }

		public int BackgroundColor
		{
			get { return unchecked((int)0xff000000); }
		}

		public int[] GetVideoBuffer()
		{
			return VideoBuffer;
		}

		private int[] VideoBuffer = new int[160 * 2 * 144];
	}
}
