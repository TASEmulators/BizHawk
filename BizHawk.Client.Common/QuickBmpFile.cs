using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Emulation.Common;
using System.Runtime.InteropServices;
using System.IO;

namespace BizHawk.Client.Common
{
	public class QuickBmpFile
	{
		#region structs
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		class BITMAPFILEHEADER
		{
			public ushort bfType;
			public uint bfSize;
			public ushort bfReserved1;
			public ushort bfReserved2;
			public uint bfOffBits;

			public BITMAPFILEHEADER()
			{
				bfSize = (uint)Marshal.SizeOf(this);
			}

			public static BITMAPFILEHEADER FromStream(Stream s)
			{
				var ret = GetObject<BITMAPFILEHEADER>(s);
				if (ret.bfSize != Marshal.SizeOf(typeof(BITMAPFILEHEADER)))
					throw new InvalidOperationException();
				return ret;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		class BITMAPINFOHEADER
		{
			public uint biSize;
			public int biWidth;
			public int biHeight;
			public ushort biPlanes;
			public ushort biBitCount;
			public BitmapCompressionMode biCompression;
			public uint biSizeImage;
			public int biXPelsPerMeter;
			public int biYPelsPerMeter;
			public uint biClrUsed;
			public uint biClrImportant;

			public BITMAPINFOHEADER()
			{
				biSize = (uint)Marshal.SizeOf(this);
			}

			public static BITMAPINFOHEADER FromStream(Stream s)
			{
				var ret = GetObject<BITMAPINFOHEADER>(s);
				if (ret.biSize != Marshal.SizeOf(typeof(BITMAPINFOHEADER)))
					throw new InvalidOperationException();
				return ret;
			}
		}

		enum BitmapCompressionMode : uint
		{
			BI_RGB = 0,
			BI_RLE8 = 1,
			BI_RLE4 = 2,
			BI_BITFIELDS = 3,
			BI_JPEG = 4,
			BI_PNG = 5
		}
		#endregion

		private unsafe static byte[] GetBytes(object o)
		{
			byte[] ret = new byte[Marshal.SizeOf(o)];
			fixed (byte* p = ret)
			{
				Marshal.StructureToPtr(o, (IntPtr)p, false);
			}
			return ret;
		}

		private unsafe static T GetObject<T>(Stream s)
		{
			byte[] tmp = new byte[Marshal.SizeOf(typeof(T))];
			s.Read(tmp, 0, tmp.Length);
			fixed (byte* p = tmp)
			{
				return (T)Marshal.PtrToStructure((IntPtr)p, typeof(T));
			}
		}

		unsafe struct BMP
		{
			public int* Data;
			public int Width;
			public int Height;
		}

		static void Blit(BMP src, BMP dst)
		{
			if (src.Width == dst.Width && src.Height == dst.Height)
				Blit_Same(src, dst);
			else
				Blit_Any(src, dst);
		}

		unsafe static void Blit_Same(BMP src, BMP dst)
		{
			int* sp = src.Data + src.Width * (src.Height - 1);
			int* dp = dst.Data;
			for (int j = 0; j < src.Height; j++)
			{
				for (int i = 0; i < src.Width; i++)
					dp[i] = sp[i];
				sp -= src.Width;
				dp += src.Width;
			}
		}

		unsafe static void Blit_Any(BMP src, BMP dst)
		{
			int w = dst.Width;
			int h = dst.Height;
			int in_w = src.Width;
			int in_h = src.Height;
			int* sp = src.Data;
			int* dp = dst.Data;

			// vflip along the way
			for (int j = h - 1; j >= 0; j--)
			{
				sp = src.Data + in_w * (j * in_h / h);
				for (int i = 0; i < w; i++)
				{
					dp[i] = sp[i * in_w / w];
				}
				dp += w;
			}
		}

		unsafe static void Blit_Any_NoFlip(BMP src, BMP dst)
		{
			int w = dst.Width;
			int h = dst.Height;
			int in_w = src.Width;
			int in_h = src.Height;
			int* sp = src.Data;
			int* dp = dst.Data;

			for (int j = 0; j < h; j++)
			{
				sp = src.Data + in_w * (j * in_h / h);
				for (int i = 0; i < w; i++)
				{
					dp[i] = sp[i * in_w / w];
				}
				dp += w;
			}
		}

		public unsafe static void Copy(IVideoProvider src, IVideoProvider dst)
		{
			if (src.BufferWidth == dst.BufferWidth && src.BufferHeight == dst.BufferHeight)
			{
				Array.Copy(src.GetVideoBuffer(), dst.GetVideoBuffer(), src.GetVideoBuffer().Length);
			}
			else
			{
				fixed (int* srcp = src.GetVideoBuffer(), dstp = dst.GetVideoBuffer())
				{
					Blit_Any_NoFlip(new BMP
					{
						Data = srcp,
						Width = src.BufferWidth,
						Height = src.BufferHeight
					},
					new BMP
					{
						Data = dstp,
						Width = dst.BufferWidth,
						Height = dst.BufferHeight
					});
				}
			}
		}

		/// <summary>
		/// if passed to QuickBMPFile.Load(), will size itself to match the incoming bmp
		/// </summary>
		public class LoadedBMP : IVideoProvider
		{
			public int[] VideoBuffer { get; set; }
			public int[] GetVideoBuffer() { return VideoBuffer; }
			public int VirtualWidth { get { return BufferWidth; } }
			public int VirtualHeight { get { return BufferHeight; } }
			public int BufferWidth { get; set; }
			public int BufferHeight { get; set; }
			public int BackgroundColor { get { return unchecked((int)0xff000000); } }
		}

		public unsafe static bool Load(IVideoProvider v, Stream s)
		{
			var bf = BITMAPFILEHEADER.FromStream(s);
			var bi = BITMAPINFOHEADER.FromStream(s);
			if (bf.bfType != 0x4d42
				|| bf.bfOffBits != bf.bfSize + bi.biSize
				|| bi.biPlanes != 1
				|| bi.biBitCount != 32
				|| bi.biCompression != BitmapCompressionMode.BI_RGB)
				return false;
			int in_w = bi.biWidth;
			int in_h = bi.biHeight;

			byte[] src = new byte[in_w * in_h * 4];
			s.Read(src, 0, src.Length);
			if (v is LoadedBMP)
			{
				var l = v as LoadedBMP;
				l.BufferWidth = in_w;
				l.BufferHeight = in_h;
				l.VideoBuffer = new int[in_w * in_h];
			}
			int[] dst = v.GetVideoBuffer();

			fixed (byte *srcp = src)
			fixed (int* dstp = dst)
			{
				using (new BizHawk.Common.SimpleTime("Blit"))
				Blit(new BMP
				{
					Data = (int*)srcp,
					Width = in_w,
					Height = in_h
				},
				new BMP
				{
					Data = dstp,
					Width = v.BufferWidth,
					Height = v.BufferHeight,
				});
			}

			return true;
		}

		public unsafe static void Save(IVideoProvider v, Stream s, int w, int h)
		{
			var bf = new BITMAPFILEHEADER();
			var bi = new BITMAPINFOHEADER();
			bf.bfType = 0x4d42;
			bf.bfOffBits = bf.bfSize + bi.biSize;

			bi.biPlanes = 1;
			bi.biBitCount = 32; // xrgb
			bi.biCompression = BitmapCompressionMode.BI_RGB;
			bi.biSizeImage = (uint)(w * h * 4);
			bi.biWidth = w;
			bi.biHeight = h;

			byte[] bfb = GetBytes(bf);
			byte[] bib = GetBytes(bi);

			s.Write(bfb, 0, bfb.Length);
			s.Write(bib, 0, bib.Length);

			int[] src = v.GetVideoBuffer();
			byte[] dst = new byte[4 * w * h];

			fixed (int* srcp = src)
			fixed (byte* dstp = dst)
			{
				using (new BizHawk.Common.SimpleTime("Blit"))
				Blit(new BMP
				{
					Data = srcp,
					Width = v.BufferWidth,
					Height = v.BufferHeight
				},
				new BMP
				{
					Data = (int*)dstp,
					Width = w,
					Height = h,
				});
			}

			s.Write(dst, 0, dst.Length);
		}
	}
}
