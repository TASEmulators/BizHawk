using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// an IVideoProvider wrapping a Bitmap
	/// </summary>
	public class BmpVideoProvider : IVideoProvider, IDisposable
	{
		private Bitmap _bmp;

		public BmpVideoProvider(Bitmap bmp, int vsyncNum, int vsyncDen)
		{
			_bmp = bmp;
			VsyncNumerator = vsyncNum;
			VsyncDenominator = vsyncDen;
		}

		public void Dispose()
		{
			if (_bmp != null)
			{
				_bmp.Dispose();
				_bmp = null;
			}
		}

		public int[] GetVideoBuffer()
		{
			// is there a faster way to do this?
			var data = _bmp.LockBits(new Rectangle(0, 0, _bmp.Width, _bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

			int[] ret = new int[_bmp.Width * _bmp.Height];

			// won't work if stride is messed up
			Marshal.Copy(data.Scan0, ret, 0, _bmp.Width * _bmp.Height);
			_bmp.UnlockBits(data);
			return ret;
		}

		public int VirtualWidth => _bmp.Width;

		// todo: Bitmap actually has size metric data; use it
		public int VirtualHeight => _bmp.Height;

		public int BufferWidth => _bmp.Width;

		public int BufferHeight => _bmp.Height;

		public int BackgroundColor => 0;

		public int VsyncNumerator { get; }

		public int VsyncDenominator { get; }
	}
}
