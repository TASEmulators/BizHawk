using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public partial class LibsnesCore : IVideoProvider
	{
		public int VirtualWidth => (int)(_videoWidth * 1.146);

		public int VirtualHeight => _videoHeight;

		public int BufferWidth => _videoWidth;

		public int BufferHeight => _videoHeight;

		public int BackgroundColor => 0;

		public int[] GetVideoBuffer()
		{
			return _videoBuffer;
		}

		private int[] _videoBuffer = new int[256 * 224];
		private int _videoWidth = 256;
		private int _videoHeight = 224;
	}
}
