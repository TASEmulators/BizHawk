using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class VBANext : IVideoProvider
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
			return videobuff;
		}

		private int[] videobuff = new int[240 * 160];
		private int[] videopalette = new int[65536];

		private void SetupColors()
		{
			int[] tmp = BizHawk.Emulation.Cores.Nintendo.Gameboy.GBColors.GetLut(Gameboy.GBColors.ColorType.vivid);
			// reorder
			for (int i = 0; i < 32768; i++)
			{
				int j = i & 0x3e0 | (i & 0x1f) << 10 | i >> 10 & 0x1f;
				videopalette[i] = tmp[j];
			}
			// duplicate
			Array.Copy(videopalette, 0, videopalette, 32768, 32768);
		}
	}
}
