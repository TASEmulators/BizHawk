using BizHawk.Bizware.Graphics;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class BitmapBufferVideoProvider : IVideoProvider, IDisposable
	{
		private BitmapBuffer _bb;

		public BitmapBufferVideoProvider(BitmapBuffer bb)
		{
			_bb = bb;
		}

		public void Dispose()
		{
			_bb?.Dispose();
			_bb = null;
		}

		public int[] GetVideoBuffer() => _bb.Pixels!;

		public int VirtualWidth => _bb.Width;

		public int VirtualHeight => _bb.Height;

		public int BufferWidth => _bb.Width;

		public int BufferHeight => _bb.Height;

		public int BackgroundColor => 0;

		public int VsyncNumerator => 0;

		public int VsyncDenominator => 0;
	}
}
