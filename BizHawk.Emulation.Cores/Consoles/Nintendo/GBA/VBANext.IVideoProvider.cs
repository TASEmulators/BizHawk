using System;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class VBANext : IVideoProvider
	{
		public int VirtualWidth => 240;
		public int VirtualHeight => 160;
		public int BufferWidth => 240;
		public int BufferHeight => 160;

		public int BackgroundColor => unchecked((int)0xff000000);

		public int[] GetVideoBuffer()
		{
			return _videobuff;
		}

		public int VsyncNumerator => 262144;

		public int VsyncDenominator => 4389;

		private readonly int[] _videobuff = new int[240 * 160];
		private readonly int[] _videopalette = new int[65536];

		private void SetupColors()
		{
			int[] tmp = GBColors.GetLut(GBColors.ColorType.vivid);

			// reorder
			for (int i = 0; i < 32768; i++)
			{
				int j = i & 0x3e0 | (i & 0x1f) << 10 | i >> 10 & 0x1f;
				_videopalette[i] = tmp[j];
			}

			// duplicate
			Array.Copy(_videopalette, 0, _videopalette, 32768, 32768);
		}
	}
}
