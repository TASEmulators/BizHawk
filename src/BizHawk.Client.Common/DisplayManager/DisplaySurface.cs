using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// This is a wrapper for a Bitmap.
	/// It should be phased out, in favor of BitmapBuffer and Texture2d's
	/// </summary>
	public unsafe class DisplaySurface : IDisplaySurface
	{
		private const PixelFormat Format = PixelFormat.Format32bppArgb;

		private Bitmap _bmp;

		public void Clear()
		{
			var bmpData = _bmp.LockBits(new(0, 0, Width, Height), ImageLockMode.WriteOnly, Format);
			new Span<byte>((void*)bmpData.Scan0, bmpData.Stride * bmpData.Height).Clear();
			_bmp.UnlockBits(bmpData);
		}

		public Bitmap PeekBitmap()
		{
			return _bmp;
		}

		public Graphics GetGraphics()
		{
			return Graphics.FromImage(_bmp);
		}

		public DisplaySurface(int width, int height)
		{
			// can't create a bitmap with zero dimensions, so for now, just bump it up to one
			if (width == 0)
			{
				width = 1;
			}

			if (height == 0)
			{
				height = 1;
			}

			Width = width;
			Height = height;

			_bmp = new Bitmap(Width, Height, Format);
		}

		public int Width { get; }
		public int Height { get; }

		public void Dispose()
		{
			_bmp?.Dispose();
			_bmp = null;
		}
	}

}
