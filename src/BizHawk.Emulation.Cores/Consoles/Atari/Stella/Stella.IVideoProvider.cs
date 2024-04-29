using System;
using BizHawk.Emulation.Common;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Atari.Stella
{
	public partial class Stella : IVideoProvider
	{
		public int[] GetVideoBuffer() => _vidBuff;

		public int VirtualWidth => 320;

		public int VirtualHeight => 224;

		public int BufferWidth => _vwidth;

		public int BufferHeight => _vheight;

		public int BackgroundColor => unchecked((int)0xff000000);

		public int VsyncNumerator { get; }

		public int VsyncDenominator { get; }

		private int[] _vidBuff = new int[0];
		private int _vwidth;
		private int _vheight;

		private void UpdateVideoInitial()
		{
		}

		private unsafe void UpdateVideo()
		{
		}

	}
}
