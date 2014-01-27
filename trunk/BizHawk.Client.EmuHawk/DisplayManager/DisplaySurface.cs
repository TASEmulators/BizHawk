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
	/// encapsulates thread-safe concept of pending/current display surfaces, reusing buffers where matching 
	/// sizes are available and keeping them cleaned up when they dont seem like theyll need to be used anymore
	/// </summary>
	class SwappableDisplaySurfaceSet
	{
		DisplaySurface Pending, Current;
		Queue<DisplaySurface> ReleasedSurfaces = new Queue<DisplaySurface>();

		/// <summary>
		/// retrieves a surface with the specified size, reusing an old buffer if available and clearing if requested
		/// </summary>
		public DisplaySurface AllocateSurface(int width, int height, bool needsClear = true)
		{
			for (; ; )
			{
				DisplaySurface trial;
				lock (this)
				{
					if (ReleasedSurfaces.Count == 0) break;
					trial = ReleasedSurfaces.Dequeue();
				}
				if (trial.Width == width && trial.Height == height)
				{
					if (needsClear) trial.Clear();
					return trial;
				}
				trial.Dispose();
			}
			return new DisplaySurface(width, height);
		}

		/// <summary>
		/// sets the provided buffer as pending. takes control of the supplied buffer
		/// </summary>
		public void SetPending(DisplaySurface newPending)
		{
			lock (this)
			{
				if (Pending != null) ReleasedSurfaces.Enqueue(Pending);
				Pending = newPending;
			}
		}

		public void ReleaseSurface(DisplaySurface surface)
		{
			lock (this) ReleasedSurfaces.Enqueue(surface);
		}

		/// <summary>
		/// returns the current buffer, making the most recent pending buffer (if there is such) as the new current first.
		/// </summary>
		public DisplaySurface GetCurrent()
		{
			lock (this)
			{
				if (Pending != null)
				{
					if (Current != null) ReleasedSurfaces.Enqueue(Current);
					Current = Pending;
					Pending = null;
				}
			}
			return Current;
		}
	}

	
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
