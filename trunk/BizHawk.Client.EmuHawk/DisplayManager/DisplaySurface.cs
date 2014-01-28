using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// This is a wrapper for a Bitmap, basically, which can also be a int[].
	/// It should be phased out, in favor of BitmapBuffer and Texture2d's
	/// </summary>
	public unsafe class DisplaySurface : IDisposable
	{
		Bitmap bmp;
		BitmapData bmpdata;
		int[] pixels;

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
			if (isBitmap) return;
			isBitmap = true;

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

		public bool IsBitmap { get { return isBitmap; } }
		bool isBitmap = false;

		public unsafe void FromBitmap(bool copy=true)
		{
			if (!isBitmap) return;
			isBitmap = false;

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


		public static DisplaySurface DisplaySurfaceWrappingBitmap(Bitmap bmp)
		{
			DisplaySurface ret = new DisplaySurface();
			ret.Width = bmp.Width;
			ret.Height = bmp.Height;
			ret.bmp = bmp;
			ret.isBitmap = true;
			return ret;
		}

		private DisplaySurface() 
		{
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

		public int* PixelPtr { get { return (int*)ptr; } }
		public IntPtr PixelIntPtr { get { return new IntPtr(ptr); } }
		public int Stride { get { return Width*4; } }
		public int OffsetOf(int x, int y) { return y * Stride + x*4; }

		void* ptr;
		GCHandle handle;
		void LockPixels()
		{
			UnlockPixels();
			handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
			ptr = handle.AddrOfPinnedObject().ToPointer();
		}

		void UnlockPixels()
		{
			if(handle.IsAllocated) handle.Free();
		}

		/// <summary>
		/// returns a new surface 
		/// </summary>
		/// <param name="xpad"></param>
		/// <param name="ypad"></param>
		/// <returns></returns>
		public DisplaySurface ToPaddedSurface(int xpad0, int ypad0, int xpad1, int ypad1)
		{
			int new_width = Width + xpad0 + xpad1;
			int new_height = Height + ypad0 + ypad1;
			DisplaySurface ret = new DisplaySurface(new_width, new_height);
			int* dptr = ret.PixelPtr;
			int* sptr = PixelPtr;
			int dstride = ret.Stride / 4;
			int sstride = Stride / 4;
			for (int y = 0; y < Height; y++)
				for (int x = 0; x < Width; x++)
				{
					dptr[(y + ypad0) * dstride + x + xpad0] = sptr[y * sstride + x];
				}
			return ret;

		}

		public int Width { get; private set; }
		public int Height { get; private set; }

		public void Dispose()
		{
			if (bmp != null)
				bmp.Dispose();
			bmp = null;
			UnlockPixels();
		}

		//public unsafe int[] ToIntArray() { }

		public void AcceptIntArray(int[] newpixels)
		{
			FromBitmap(false);
			UnlockPixels();
			pixels = newpixels;
			LockPixels();
		}
	}

}
