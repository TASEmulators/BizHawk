using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// This is a wrapper for a Bitmap, basically, which can also be a int[].
	/// It should be phased out, in favor of BitmapBuffer and Texture2d's
	/// </summary>
	public unsafe class DisplaySurface : IDisposable
	{
		private Bitmap bmp;
		private BitmapData bmpdata;
		private int[] pixels;

		public unsafe void Clear()
		{
			FromBitmap(false);
			Util.Memset(PixelPtr, 0, Stride * Height);
		}

		public Bitmap PeekBitmap()
		{
			ToBitmap();
			return bmp;
		}

		/// <summary>
		/// returns a Graphics object used to render to this surface. be sure to dispose it!
		/// </summary>
		public Graphics GetGraphics()
		{
			ToBitmap();
			return Graphics.FromImage(bmp);
		}

		public unsafe void ToBitmap(bool copy=true)
		{
			if (_isBitmap) return;
			_isBitmap = true;

			if (bmp == null)
			{
				bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
			}

			if (copy)
			{
				bmpdata = bmp.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

				int w = Width;
				int h = Height;
				int stride = bmpdata.Stride / 4;
				int* bmpbuf = (int*)bmpdata.Scan0.ToPointer();
				for (int y = 0, i = 0; y < h; y++)
					for (int x = 0; x < w; x++)
						bmpbuf[y * stride + x] = pixels[i++];

				bmp.UnlockBits(bmpdata);
			}

		}

		bool _isBitmap;

		public unsafe void FromBitmap(bool copy=true)
		{
			if (!_isBitmap) return;
			_isBitmap = false;

			if (copy)
			{
				bmpdata = bmp.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

				int w = Width;
				int h = Height;
				int stride = bmpdata.Stride / 4;
				int* bmpbuf = (int*)bmpdata.Scan0.ToPointer();
				for (int y = 0, i = 0; y < h; y++)
					for (int x = 0; x < w; x++)
						pixels[i++] = bmpbuf[y * stride + x];

				bmp.UnlockBits(bmpdata);
			}
		}

		public DisplaySurface(int width, int height)
		{
			//can't create a bitmap with zero dimensions, so for now, just bump it up to one
			if (width == 0) width = 1;
			if (height == 0) height = 1; 
			
			Width = width;
			Height = height;

			pixels = new int[width * height];
			LockPixels();
		}

		public int* PixelPtr => (int*)ptr;
		public int Stride => Width * 4;

		void* ptr;
		GCHandle handle;
		void LockPixels()
		{
			UnlockPixels();
			handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
			ptr = handle.AddrOfPinnedObject().ToPointer();
		}

		private void UnlockPixels()
		{
			if(handle.IsAllocated) handle.Free();
		}

		public int Width { get; }
		public int Height { get; }

		public void Dispose()
		{
			bmp?.Dispose();
			bmp = null;
			UnlockPixels();
		}
	}

}
