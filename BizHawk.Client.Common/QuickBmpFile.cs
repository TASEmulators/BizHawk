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
				int in_w = v.BufferWidth;
				int in_h = v.BufferHeight;

				int* sp = srcp;
				int* dp = (int*)dstp;

				// vflip along the way
				for (int j = h - 1; j >= 0; j--)
				{
					sp = srcp + in_w * (j * in_h / h);
					for (int i = 0; i < w; i++)
					{
						dp[i] = sp[i * in_w / w];
					}
					dp += w;
				}
			}

			s.Write(dst, 0, dst.Length);
			s.Close();
		}
	}
}
