// TODO - introduce Trim for ArtManager
// TODO - add a small buffer reuse manager.. small images can be stored in larger buffers which we happen to have held. use a timer to wait to free it until some time has passed

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BizHawk.Common.CollectionExtensions;

using SDGraphics = System.Drawing.Graphics;

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// a software-based bitmap, way easier (and faster) to use than .net's built-in bitmap.
	/// Only supports a fixed rgba format
	/// Even though this is IDisposable, you don't have to worry about disposing it normally (that's only for the Bitmap-mimicking)
	/// But you know you can't resist.
	/// </summary>
	public unsafe class BitmapBuffer : IDisposable
	{
		public int Width, Height;
		public int[] Pixels;

		/// <summary>
		/// Whether this instance should be considered as having alpha (ARGB) or not (XRBG)
		/// </summary>
		public bool HasAlpha = true;

		public Size Size => new(Width, Height);

		private readonly Bitmap WrappedBitmap;
		private GCHandle CurrLockHandle;
		private BitmapData CurrLock;

		/// <summary>same as <see cref="Pixels"/> (<see cref="PixelFormat.Format32bppArgb">A8R8G8B8</see>)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<int> AsSpan()
			=> Pixels;

		/// <exception cref="InvalidOperationException">already locked</exception>
		/// <remarks>TODO add read/write semantic, for wraps</remarks>
		public BitmapData LockBits()
		{
			if(CurrLock != null)
				throw new InvalidOperationException($"{nameof(BitmapBuffer)} can only be locked once!");

			if (WrappedBitmap != null)
			{
				CurrLock = WrappedBitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
				return CurrLock;
			}

			CurrLockHandle = GCHandle.Alloc(Pixels, GCHandleType.Pinned);
			CurrLock = new()
			{
				Height = Height,
				Width = Width,
				Stride = Width * 4,
				Scan0 = CurrLockHandle.AddrOfPinnedObject()
			};

			return CurrLock;
		}

		public void UnlockBits(BitmapData bmpd)
		{
			Debug.Assert(CurrLock == bmpd, "must pass in the same object obtained from " + nameof(LockBits));

			if (WrappedBitmap != null)
			{
				WrappedBitmap.UnlockBits(CurrLock);
				CurrLock = null;
				return;
			}

			CurrLockHandle.Free();
			CurrLock = null;
		}

		public void Dispose()
		{
			if (CurrLock == null) return;
			UnlockBits(CurrLock);
		}

		public void YFlip()
		{
			// TODO - could be faster
			var bmpdata = LockBits();
			var newPixels = new int[Width * Height];
			var s = (int*)bmpdata.Scan0.ToPointer();
			fixed (int* d = newPixels)
			{
				for (int y = 0, si = 0, di = (Height - 1) * Width; y < Height; y++)
				{
					for (var x = 0; x < Width; x++, si++, di++)
					{
						d[di] = s[si];
					}
					di -= Width * 2;
				}
			}

			UnlockBits(bmpdata);

			Pixels = newPixels;
		}

		/// <summary>
		/// Makes sure the alpha channel is clean and optionally y-flips
		/// </summary>
		public void Normalize(bool yflip)
		{
			var bmpdata = LockBits();
			var newPixels = new int[Width * Height];
			var s = (int*)bmpdata.Scan0.ToPointer();
			fixed (int* d = newPixels)
			{
				if (yflip)
				{
					for (int y = 0, si = 0, di = (Height - 1) * Width; y < Height; y++)
					{
						for (var x = 0; x < Width; x++, si++, di++)
						{
							d[di] = s[si] | unchecked((int)0xFF000000);
						}
						di -= Width * 2;
					}
				}
				else
				{
					for (int y = 0, i=0; y < Height; y++)
					{
						for (var x = 0; x < Width; x++, i++)
						{
							d[i] = s[i] | unchecked((int)0xFF000000);
						}
					}
				}
			}

			UnlockBits(bmpdata);
		
			Pixels = newPixels;
		}

		public int GetPixel(int x, int y)
		{
			return Pixels[Width * y + x];
		}

		public void SetPixel(int x, int y, int value)
		{
			Pixels[Width * y + x] = value;
		}

		public Color GetPixelAsColor(int x, int y)
		{
			var c = Pixels[Width * y + x];
			return Color.FromArgb(c);
		}

		/// <summary>
		/// transforms tcol to 0,0,0,0
		/// </summary>
		public void Alphafy(int tcol)
		{
			for (int y = 0, idx = 0; y < Height; y++)
			{
				for (var x = 0; x < Width; x++, idx++)
				{
					if (Pixels[idx] == tcol)
					{
						Pixels[idx] = 0;
					}
				}
			}
		}

		/// <summary>
		/// copies this bitmap and trims out transparent pixels, returning the offset to the topleft pixel
		/// </summary>
		public BitmapBuffer Trim()
		{
			return Trim(out _, out _);
		}

		/// <summary>
		/// copies this bitmap and trims out transparent pixels, returning the offset to the topleft pixel
		/// </summary>
		public BitmapBuffer Trim(out int xofs, out int yofs)
		{
			var minx = int.MaxValue;
			var maxx = int.MinValue;
			var miny = int.MaxValue;
			var maxy = int.MinValue;
			for (var y = 0; y < Height; y++)
			{
				for (var x = 0; x < Width; x++)
				{
					var pixel = GetPixel(x, y);
					var a = (pixel >> 24) & 0xFF;
					if (a != 0)
					{
						minx = Math.Min(minx, x);
						maxx = Math.Max(maxx, x);
						miny = Math.Min(miny, y);
						maxy = Math.Max(maxy, y);
					}
				}
			}

			if (minx == int.MaxValue || maxx == int.MinValue || miny == int.MaxValue || minx == int.MinValue)
			{
				xofs = yofs = 0;
				return new(0, 0);
			}

			xofs = minx;
			yofs = miny;
			return Copy(region: new(x: minx, y: miny, width: maxx - minx + 1, height: maxy - miny + 1));
		}

		public BitmapBuffer Copy()
			=> new(width: Width, height: Height, pixels: AsSpan().ToArray());

		/// <remarks>TODO surely there's a better implementation --yoshi</remarks>
		public BitmapBuffer Copy(Rectangle region)
		{
			BitmapBuffer bbRet = new(region.Size);
			var miny = region.Top;
			var minx = region.Left;
			for (int y = 0, h = region.Height; y < h; y++) for (int x = 0, w = region.Width; x < w; x++)
			{
				bbRet.SetPixel(x, y, GetPixel(x + minx, y + miny));
			}
			return bbRet;
		}

		/// <summary>
		/// increases dimensions of this bitmap to the next higher power of 2
		/// </summary>
		public void Pad()
		{
			var widthRound = NextHigher(Width);
			var heightRound = NextHigher(Height);
			if (widthRound == Width && heightRound == Height) return;
			var NewPixels = new int[heightRound * widthRound];

			for (int y = 0, sptr = 0, dptr = 0; y < Height; y++)
			{
				for (var x = 0; x < Width; x++)
				{
					NewPixels[dptr++] = Pixels[sptr++];
				}

				dptr += (widthRound - Width);
			}

			Pixels = NewPixels;
			Width = widthRound;
			Height = heightRound;
		}
		
		/// <summary>
		/// Creates a BitmapBuffer image from the specified filename
		/// </summary>
		public BitmapBuffer(string fname, BitmapLoadOptions options)
		{
			using var fs = new FileStream(fname, FileMode.Open, FileAccess.Read, FileShare.Read);
			LoadInternal(fs, null, options);
		}

		/// <summary>
		/// loads an image (png,bmp,etc) from the specified stream
		/// </summary>
		public BitmapBuffer(Stream stream, BitmapLoadOptions options)
		{
			LoadInternal(stream, null, options);
		}

		/// <summary>
		/// Initializes the BitmapBuffer from a System.Drawing.Bitmap
		/// </summary>
		public BitmapBuffer(Bitmap bitmap, BitmapLoadOptions options)
		{
			if (options.AllowWrap && bitmap.PixelFormat == PixelFormat.Format32bppArgb)
			{
				Width = bitmap.Width;
				Height = bitmap.Height;
				WrappedBitmap = bitmap;
			}
			else
			{
				LoadInternal(null, bitmap, options);
			}
		}

		/// <summary>
		/// Initializes a BitmapBuffer --WRAPPED-- from the supplied parameters, which should definitely have a stride==width and be in the standard color format
		/// </summary>
		public BitmapBuffer(int width, int height, int[] pixels)
		{
			Pixels = pixels;
			Width = width;
			Height = height;
		}

		/// <summary>
		/// Suggests that this BitmapBuffer is now XRGB instead of ARGB but doesn't actually change any of the pixels data.
		/// Should affect how things get exported from here, though, I think
		/// </summary>
		public void DiscardAlpha()
		{
			HasAlpha = false;
		}

		private void LoadInternal(Stream stream, Bitmap bitmap, BitmapLoadOptions options)
		{
			var cleanup = options.CleanupAlpha0;
			var needsPad = true;

			var colorKey24bpp = options.ColorKey24bpp;
			using (var loadedBmp = bitmap == null ? new Bitmap(stream) : null) // sneaky!
			{
				var bmp = loadedBmp ?? bitmap;

				// if we have a 24bpp image and a colorkey callback, the callback can choose a colorkey color and we'll use that
				if (bmp.PixelFormat == PixelFormat.Format24bppRgb && colorKey24bpp != null)
				{
					var colorKey = colorKey24bpp(bmp);
					var w = bmp.Width;
					var h = bmp.Height;
					InitSize(w, h);
					var bmpdata = bmp.LockBits(new(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
					var ptr = (int*)bmpdata.Scan0.ToPointer();
					fixed (int* pPtr = &Pixels[0])
					{
						for (int idx = 0, y = 0; y < h; y++)
						{
							for (var x = 0; x < w; x++)
							{
								var srcPixel = ptr[idx];
								if (srcPixel == colorKey)
								{
									srcPixel = 0;
								}

								pPtr[idx++] = srcPixel;
							}
						}
					}

					bmp.UnlockBits(bmpdata);
				}
				if (bmp.PixelFormat is PixelFormat.Format8bppIndexed or PixelFormat.Format4bppIndexed)
				{
					var w = bmp.Width;
					var h = bmp.Height;
					InitSize(w, h);
					var bmpdata = bmp.LockBits(new(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
					var palette = bmp.Palette.Entries;
					var ptr = (byte*)bmpdata.Scan0.ToPointer();
					fixed (int* pPtr = &Pixels[0])
					{
						for (int idx = 0, y = 0; y < h; y++)
						{
							for (var x = 0; x < w; x++)
							{
								int srcPixel = ptr[idx];
								if (srcPixel != 0)
								{
									var color = palette[srcPixel].ToArgb();
									
									// make transparent pixels turn into black to avoid filtering issues and other annoying issues with stray junk in transparent pixels.
									// (yes, we can have palette entries with transparency in them (PNGs support this, annoyingly))
									if (cleanup)
									{
										if ((color & 0xFF000000) == 0)
										{
											color = 0;
										}

										pPtr[idx] = color;
									}
								}
								idx++;
							}
						}
					}

					bmp.UnlockBits(bmpdata);
				}
				else
				{
					// dump the supplied bitmap into our pixels array
					var width = bmp.Width;
					var height = bmp.Height;
					InitSize(width, height);
					var bmpdata = bmp.LockBits(new(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
					var ptr = (int*)bmpdata.Scan0;
					var stride = bmpdata.Stride / 4;
					LoadFrom(width, stride, height, (byte*)ptr, options);
					bmp.UnlockBits(bmpdata);
					needsPad = false;
				}
			}

			if (needsPad && options.Pad)
			{
				Pad();
			}
		}


		/// <summary>
		/// Loads the BitmapBuffer from a source buffer, which is expected to be the right pixel format
		/// </summary>
		public void LoadFrom(int width, int stride, int height, byte* data, BitmapLoadOptions options)
		{
			var cleanup = options.CleanupAlpha0;
			Width = width;
			Height = height;
			Pixels = new int[width * height];
			fixed (int* pPtr = &Pixels[0])
			{
				for (int idx = 0, y = 0; y < Height; y++)
				{
					for (var x = 0; x < Width; x++)
					{
						var src = y * stride + x;
						var srcVal = ((int*)data)[src];

						// make transparent pixels turn into black to avoid filtering issues and other annoying issues with stray junk in transparent pixels
						if (cleanup)
						{
							if ((srcVal & 0xFF000000) == 0)
							{
								srcVal = 0;
							}

							pPtr[idx++] = srcVal;
						}
					}
				}
			}

			if (options.Pad)
			{
				Pad();
			}
		}

		/// <summary>
		/// premultiplies a color
		/// </summary>
		public static int PremultiplyColor(int srcVal)
		{
			var b = (srcVal >> 0) & 0xFF;
			var g = (srcVal >> 8) & 0xFF;
			var r = (srcVal >> 16) & 0xFF;
			var a = (srcVal >> 24) & 0xFF;
			r = (r * a) >> 8;
			g = (g * a) >> 8;
			b = (b * a) >> 8;
			srcVal = b | (g << 8) | (r << 16) | (a << 24);
			return srcVal;
		}

		/// <summary>
		/// initializes an empty BitmapBuffer, cleared to all 0
		/// </summary>
		public BitmapBuffer(int width, int height)
		{
			InitSize(width, height);
		}

		/// <summary>
		/// Makes a new bitmap buffer, in ??? state
		/// </summary>
		public BitmapBuffer()
		{
		}

		/// <summary>
		/// initializes an empty BitmapBuffer, cleared to all 0
		/// </summary>
		public BitmapBuffer(Size size)
		{
			InitSize(size.Width, size.Height);
		}

		/// <summary>
		/// clears this instance to (0,0,0,0) -- without allocating a new array (to avoid GC churn)
		/// </summary>
		public void ClearWithoutAlloc()
		{
			// http://techmikael.blogspot.com/2009/12/filling-array-with-default-value.html
			// this guy says its faster

			var size = Width * Height;
			const byte fillValue = 0;
			const ulong fillValueLong = 0;

			fixed (int* ptr = &Pixels[0])
			{
				var dest = (ulong*)ptr;
				var length = size;
				while (length >= 8)
				{
					*dest = fillValueLong;
					dest++;
					length -= 8;
				}
				var bDest = (byte*)dest;
				for (byte i = 0; i < length; i++)
				{
					*bDest = fillValue;
					bDest++;
				}
			}
		}

		private void InitSize(int width, int height)
		{
			Pixels = new int[width * height];
			Width = width;
			Height = height;
		}

		/// <summary>
		/// returns the next higher power of 2 than the provided value, for rounding up POW2 textures.
		/// </summary>
		private static int NextHigher(int k)
		{
			k--;
			for (var i = 1; i < 32; i <<= 1)
			{
				k |= k >> i;
			}

			var candidate = k + 1;
			return candidate;
		}

		public bool SequenceEqual(BitmapBuffer other)
			=> Width == other.Width/* && Height == other.Height*/ && AsSpan().SequenceEqual(other.AsSpan());

		/// <summary>
		/// Dumps this BitmapBuffer to a new System.Drawing.Bitmap
		/// </summary>
		public Bitmap ToSysdrawingBitmap()
		{
			if (WrappedBitmap != null)
			{
				return (Bitmap)WrappedBitmap.Clone();
			}

			var pf = HasAlpha ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb;
			var bmp = new Bitmap(Width, Height, pf);
			ToSysdrawingBitmap(bmp);
			return bmp;
		}

		/// <summary>
		/// Dumps this BitmapBuffer to an existing System.Drawing.Bitmap.
		/// Some features of this may not be super fast (in particular, 32bpp to 24bpp conversion; we might fix that later with a customized loop)
		/// </summary>
		public void ToSysdrawingBitmap(Bitmap bmp)
		{
			if (WrappedBitmap != null)
			{
				using var g = SDGraphics.FromImage(bmp);
				g.CompositingMode = CompositingMode.SourceCopy;
				g.CompositingQuality = CompositingQuality.HighSpeed;
				g.DrawImageUnscaled(WrappedBitmap, 0, 0);
				return;
			}

			//note: we lock it as 32bpp even if the bitmap is 24bpp so we can write to it more conveniently.
			var bmpdata = bmp.LockBits(new(0, 0, Width, Height), ImageLockMode.WriteOnly, HasAlpha ? PixelFormat.Format32bppArgb : PixelFormat.Format32bppRgb);

			if (bmpdata.Stride == bmpdata.Width * 4)
			{
				Marshal.Copy(Pixels, 0, bmpdata.Scan0, Width * Height);
			}
			else if (bmp.Width != 0 && bmp.Height != 0)
			{
				var ptr = (int*)bmpdata.Scan0.ToPointer();
				fixed (int* pPtr = &Pixels[0])
				{
					for (int idx = 0, y = 0; y < Height; y++)
					{
						for (var x = 0; x < Width; x++)
						{
							var srcPixel = pPtr[idx];
							ptr[idx] = srcPixel;
							idx++;
						}
					}
				}
			}

			bmp.UnlockBits(bmpdata);
		}

	}

}
