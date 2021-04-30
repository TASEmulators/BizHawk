using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public partial class BsnesCore : IVideoProvider
	{
		public int VirtualWidth { get; private set; } = 293;

		public int VirtualHeight { get; private set; } = 224;

		public int BufferWidth => _videoWidth;

		public int BufferHeight => _videoHeight;

		public int BackgroundColor => 0;

		public int[] GetVideoBuffer() => _videoBuffer;

		public int VsyncNumerator { get; }
		public int VsyncDenominator { get; }

		private int[] _videoBuffer = new int[256 * 224];
		private int _videoWidth = 256;
		private int _videoHeight = 224;
	}
}
