using System;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// an IVideoProvider wrapping a Bitmap
	/// </summary>
	public class BmpVideoProvider : IVideoProvider, IDisposable
	{
		private Bitmap _bmp;

		public BmpVideoProvider(Bitmap bmp, int vsyncnum, int vsyncden)
		{
			_bmp = bmp;
			VsyncNumerator = vsyncnum;
			VsyncDenominator = vsyncden;
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
			var data = _bmp.LockBits(new Rectangle(0, 0, _bmp.Width, _bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			int[] ret = new int[_bmp.Width * _bmp.Height];

			// won't work if stride is messed up
			System.Runtime.InteropServices.Marshal.Copy(data.Scan0, ret, 0, _bmp.Width * _bmp.Height);
			_bmp.UnlockBits(data);
			return ret;
		}

		public int VirtualWidth => _bmp.Width;

		// todo: Bitmap actually has size metric data; use it
		public int VirtualHeight => _bmp.Height;

		public int BufferWidth => _bmp.Width;

		public int BufferHeight => _bmp.Height;

		public int BackgroundColor => 0;

		public int VsyncNumerator { get; private set; }

		public int VsyncDenominator { get; private set; }
	}
}
