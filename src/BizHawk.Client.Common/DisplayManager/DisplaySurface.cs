using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using BizHawk.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// This is a wrapper for a Bitmap, basically, which can also be a int[].
	/// It should be phased out, in favor of BitmapBuffer and Texture2d's
	/// </summary>
	public unsafe class DisplaySurface : IDisplaySurface
	{
		private Bitmap _bmp;
		private BitmapData _bmpData;
		private readonly int[] _pixels;

		public void Clear()
		{
			FromBitmap(false);
			Util.Memset(PixelPtr, 0, Stride * Height);
		}

		public Bitmap PeekBitmap()
		{
			ToBitmap();
			return _bmp;
		}

		public Graphics GetGraphics()
		{
			ToBitmap();
			return Graphics.FromImage(_bmp);
		}

		public void ToBitmap(bool copy = true)
		{
			if (_isBitmap) return;
			_isBitmap = true;

			if (_bmp == null)
			{
				_bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
			}

			if (copy)
			{
				_bmpData = _bmp.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

				int w = Width;
				int h = Height;
				int stride = _bmpData.Stride / 4;
				int* bmpBuf = (int*)_bmpData.Scan0.ToPointer();
				for (int y = 0, i = 0; y < h; y++)
				{
					for (int x = 0; x < w; x++)
					{
						bmpBuf[y * stride + x] = _pixels[i++];
					}
				}

				_bmp.UnlockBits(_bmpData);
			}

		}

		private bool _isBitmap;

		public void FromBitmap(bool copy = true)
		{
			if (!_isBitmap)
			{
				return;
			}

			_isBitmap = false;

			if (copy)
			{
				_bmpData = _bmp.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

				int w = Width;
				int h = Height;
				int stride = _bmpData.Stride / 4;
				int* bmpBuf = (int*)_bmpData.Scan0.ToPointer();
				for (int y = 0, i = 0; y < h; y++)
				{
					for (int x = 0; x < w; x++)
					{
						_pixels[i++] = bmpBuf[y * stride + x];
					}
				}

				_bmp.UnlockBits(_bmpData);
			}
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

			_pixels = new int[width * height];
			LockPixels();
		}

		public int* PixelPtr => (int*)_ptr;
		public int Stride => Width * 4;

		private void* _ptr;
		private GCHandle _handle;
		private void LockPixels()
		{
			UnlockPixels();
			_handle = GCHandle.Alloc(_pixels, GCHandleType.Pinned);
			_ptr = _handle.AddrOfPinnedObject().ToPointer();
		}

		private void UnlockPixels()
		{
			if (_handle.IsAllocated)
			{
				_handle.Free();
			}
		}

		public int Width { get; }
		public int Height { get; }

		public void Dispose()
		{
			_bmp?.Dispose();
			_bmp = null;
			UnlockPixels();
		}
	}

}
