using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public partial class BsnesCore : IVideoProvider
	{
		// TODO: This should probably be different for PAL?
		public int VirtualWidth => (int) Math.Ceiling((double) BufferHeight * 64 / 49);

		public int VirtualHeight => BufferHeight;

		public int BufferWidth { get; private set; } = 256;

		public int BufferHeight { get; private set; } = 224;

		public int BackgroundColor => 0;

		public int[] GetVideoBuffer() => _videoBuffer;

		public int VsyncNumerator { get; }
		public int VsyncDenominator { get; }

		private int[] _videoBuffer = new int[256 * 224];
	}
}
