//http://wiki.superfamicom.org/snes/show/Backgrounds

using System;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Emulation.Consoles.Nintendo.SNES
{

	public unsafe class SNESGraphicsDecoder
	{
		public struct Dimensions
		{
			public Dimensions(int w, int h) { Width = (short)w; Height = (short)h; }
			public short Width, Height;
			public override string ToString()
			{
				return string.Format("{0}x{1}", Width, Height);
			}
		}

		public enum ScreenSize
		{
			AAAA_32x32 = 0, ABAB_64x32 = 1, AABB_32x64 = 2, ABCD_64x64 = 3
		}

		public static Dimensions SizeInTilesForBGSize(ScreenSize size)
		{
			switch (size)
			{
				case ScreenSize.AAAA_32x32: return new Dimensions(32, 32);
				case ScreenSize.ABAB_64x32: return new Dimensions(64, 32);
				case ScreenSize.AABB_32x64: return new Dimensions(32, 64);
				case ScreenSize.ABCD_64x64: return new Dimensions(64, 64);
				default: throw new Exception();
			}
		}

		public class BGInfo
		{
			/// <summary>
			/// screen and tiledata register values
			/// </summary>
			public int SCADDR, TDADDR;

			/// <summary>
			/// SCSIZE register
			/// </summary>
			public int SCSIZE;

			/// <summary>
			/// the address of the screen data
			/// </summary>
			public int ScreenAddr { get { return SCADDR << 9; } }

			/// <summary>
			/// the address of the tile data
			/// </summary>
			public int TiledataAddr { get { return SCADDR << 13; } }

			/// <summary>
			/// Screen size (shape, really.)
			/// </summary>
			public ScreenSize ScreenSize { get { return (ScreenSize)SCSIZE; } }

			/// <summary>
			/// the BPP of the BG, as derived from the current mode
			/// </summary>
			public int Bpp;

			/// <summary>
			/// The size of the layer, in tiles
			/// </summary>
			public Dimensions ScreenSizeInTiles { get { return SizeInTilesForBGSize(ScreenSize); } }
		}

		public class BGInfos
		{
			BGInfo[] bgs = new BGInfo[4] { new BGInfo(), new BGInfo(), new BGInfo(), new BGInfo() };
			public BGInfo BG1 { get { return bgs[0]; } }
			public BGInfo BG2 { get { return bgs[1]; } }
			public BGInfo BG3 { get { return bgs[2]; } }
			public BGInfo BG4 { get { return bgs[3]; } }
			public BGInfo this[int index] { get { return bgs[index - 1]; } }
		}

		public class ModeInfo
		{
			/// <summary>
			/// the mode number, i.e. Mode 7
			/// </summary>
			public int MODE;
		}

		public class ScreenInfo
		{
			public BGInfos BG = new BGInfos();

			public ModeInfo Mode = new ModeInfo();
		}

		static int[,] ModeBpps = new[,] {
				{2,2,2,2},
				{4,4,2,0},
				{4,4,0,0},
				{8,4,0,0},
				{8,2,0,0},
				{4,2,0,0},
				{4,0,0,0},
				{8,0,0,0},
				{8,7,0,0}
			};


		public ScreenInfo ScanScreenInfo()
		{
			var si = new ScreenInfo();

			si.Mode.MODE = LibsnesDll.snes_peek_logical_register(LibsnesDll.SNES_REG.BG_MODE);
			si.BG.BG1.Bpp = ModeBpps[si.Mode.MODE, 0];
			si.BG.BG2.Bpp = ModeBpps[si.Mode.MODE, 1];
			si.BG.BG3.Bpp = ModeBpps[si.Mode.MODE, 2];
			si.BG.BG4.Bpp = ModeBpps[si.Mode.MODE, 3];

			si.BG.BG1.SCSIZE = LibsnesDll.snes_peek_logical_register(LibsnesDll.SNES_REG.BG1_SCSIZE);
			si.BG.BG2.SCSIZE = LibsnesDll.snes_peek_logical_register(LibsnesDll.SNES_REG.BG2_SCSIZE);
			si.BG.BG3.SCSIZE = LibsnesDll.snes_peek_logical_register(LibsnesDll.SNES_REG.BG3_SCSIZE);
			si.BG.BG4.SCSIZE = LibsnesDll.snes_peek_logical_register(LibsnesDll.SNES_REG.BG4_SCSIZE);
			si.BG.BG1.SCADDR = LibsnesDll.snes_peek_logical_register(LibsnesDll.SNES_REG.BG1_SCADDR);
			si.BG.BG2.SCADDR = LibsnesDll.snes_peek_logical_register(LibsnesDll.SNES_REG.BG2_SCADDR);
			si.BG.BG3.SCADDR = LibsnesDll.snes_peek_logical_register(LibsnesDll.SNES_REG.BG3_SCADDR);
			si.BG.BG4.SCADDR = LibsnesDll.snes_peek_logical_register(LibsnesDll.SNES_REG.BG4_SCADDR);
			return si;
		}



		static int[] colortable = new int[16 * 32768];

		static SNESGraphicsDecoder()
		{
			for (int l = 0; l < 16; l++)
			{
				for (int r = 0; r < 32; r++)
				{
					for (int g = 0; g < 32; g++)
					{
						for (int b = 0; b < 32; b++)
						{
							//zero 04-sep-2012 - go ahead and turn this into a pixel format we'll want
							double luma = (double)l / 15.0;
							int ar = (int)(luma * r + 0.5);
							int ag = (int)(luma * g + 0.5);
							int ab = (int)(luma * b + 0.5);
							ar = ar * 255 / 31;
							ag = ag * 255 / 31;
							ab = ab * 255 / 31;
							int color = (ab << 16) + (ag << 8) + (ar << 0) | unchecked((int)0xFF000000);
							colortable[(l << 15) + (r << 10) + (g << 5) + (b << 0)] = color;
						}
					}
				}
			}
		}

		byte* vram;
		ushort* cgram, vram16;
		public SNESGraphicsDecoder()
		{
			IntPtr block = LibsnesDll.snes_get_memory_data(LibsnesDll.SNES_MEMORY.VRAM);
			vram = (byte*)block.ToPointer();
			vram16 = (ushort*)vram;
			block = LibsnesDll.snes_get_memory_data(LibsnesDll.SNES_MEMORY.CGRAM);
			cgram = (ushort*)block.ToPointer();
		}

		public struct TileEnty
		{
			public ushort tilenum;
			public byte palette;
			public TileEntryFlags flags;
		}

		public enum TileEntryFlags : byte
		{
			Priority = 1, Horz = 2, Vert = 4,
		}

		/// <summary>
		/// fetches a normal BG 32x32 tilemap block into the supplied buffer and offset
		/// </summary>
		public void FetchTilemapBlock(TileEnty[] buf, int offset, int addr)
		{
			for (int y = 0, i = offset; y < 32; y++)
				for (int x = 0; x < 32; x++, i++)
				{
					ushort entry = *(ushort*)(vram + addr);
					buf[i].tilenum = (ushort)(entry & 0x3FF);
					buf[i].palette = (byte)((entry >> 10) & 7);
					buf[i].flags = (TileEntryFlags)((entry >> 13) & 7);
					addr += 2;
				}
		}

		public void Paletteize(int* buf, int offset, int startcolor, int numpixels)
		{
			for (int i = 0; i < numpixels; i++)
			{
				buf[offset + i] = cgram[startcolor + buf[offset + i]];
			}
		}

		public void Colorize(int* buf, int offset, int numpixels)
		{
			for (int i = 0; i < numpixels; i++)
			{
				buf[offset + i] = colortable[491520 + buf[offset + i]];
			}
		}

		public void Decode8x8x2bpp(int[] buf, int offset, int addr)
		{
			for (int y = 0; y < 8; y++)
			{
				byte val = vram[addr + 0];
				for (int x = 0; x < 8; x++) buf[offset + y * 8 + x] = val >> (7 - x) & 1;
				val = vram[addr + 1];
				for (int x = 0; x < 8; x++) buf[offset + y * 8 + x] = (buf[offset + y * 8 + x] << 1) | (val >> (7 - x) & 1);
				addr += 2;
			}
		}

		public void Decode8x8x4bpp(int[] buf, int offset, int addr)
		{
			for (int y = 0; y < 8; y++)
			{
				byte val = vram[addr + 0];
				for (int x = 0; x < 8; x++) buf[offset + y * 8 + x] = val >> (7 - x) & 1;
				val = vram[addr + 1];
				for (int x = 0; x < 8; x++) buf[offset + y * 8 + x] = (buf[offset + y * 8 + x] << 1) | (val >> (7 - x) & 1);
				val = vram[addr + 16];
				for (int x = 0; x < 8; x++) buf[offset + y * 8 + x] = (buf[offset + y * 8 + x] << 1) | (val >> (7 - x) & 1);
				val = vram[addr + 17];
				for (int x = 0; x < 8; x++) buf[offset + y * 8 + x] = (buf[offset + y * 8 + x] << 1) | (val >> (7 - x) & 1);
				addr += 2;
			}
		}

		public void Decode8x8x8bpp(int[] buf, int offset, int addr)
		{
			for (int y = 0; y < 8; y++)
			{
				byte val = vram[addr + 0];
				for (int x = 0; x < 8; x++) buf[offset + y * 8 + x] = val >> (7 - x) & 1;
				val = vram[addr + 1];
				for (int x = 0; x < 8; x++) buf[offset + y * 8 + x] = (buf[offset + y * 8 + x] << 1) | (val >> (7 - x) & 1);
				val = vram[addr + 16];
				for (int x = 0; x < 8; x++) buf[offset + y * 8 + x] = (buf[offset + y * 8 + x] << 1) | (val >> (7 - x) & 1);
				val = vram[addr + 17];
				for (int x = 0; x < 8; x++) buf[offset + y * 8 + x] = (buf[offset + y * 8 + x] << 1) | (val >> (7 - x) & 1);
				val = vram[addr + 32];
				for (int x = 0; x < 8; x++) buf[offset + y * 8 + x] = (buf[offset + y * 8 + x] << 1) | (val >> (7 - x) & 1);
				val = vram[addr + 33];
				for (int x = 0; x < 8; x++) buf[offset + y * 8 + x] = (buf[offset + y * 8 + x] << 1) | (val >> (7 - x) & 1);
				val = vram[addr + 48];
				for (int x = 0; x < 8; x++) buf[offset + y * 8 + x] = (buf[offset + y * 8 + x] << 1) | (val >> (7 - x) & 1);
				val = vram[addr + 49];
				for (int x = 0; x < 8; x++) buf[offset + y * 8 + x] = (buf[offset + y * 8 + x] << 1) | (val >> (7 - x) & 1);
				addr += 2;
			}
		}

		/// <summary>
		/// decodes all the tiles in vram as if they were 2bpp tiles to a 64x64 tile (512x512 pixel) screen
		/// </summary>
		public void DecodeTiles2bpp(int* screen, int stride, int startcolor)
		{
			//cant handle this with our speed optimized routines
			Debug.Assert(stride == 512);
			
			int[] tilebuf = new int[8 * 8];
			for (int i = 0; i < 64 * 64; i++)
			{
				Decode8x8x2bpp(tilebuf, 0, 16 * i);
				int ty = i / 64;
				int tx = i % 64;
				ty *= 8;
				tx *= 8;
				for (int y = 0; y < 8; y++)
					for (int x = 0; x < 8; x++)
					{
						screen[(ty + y) * stride + tx + x] = tilebuf[y * 8 + x];
					}
			}

			Paletteize(screen, 0, startcolor, 64 * 64 * 8 * 8);
			Colorize(screen, 0, 64 * 64 * 8 * 8);
		}

		/// <summary>
		/// decodes all the tiles in vram as if they were 4bpp tiles to a 64x32 tile (512x256 pixel) screen
		/// </summary>
		public void DecodeTiles4bpp(int* screen, int stride, int startcolor)
		{
			//cant handle this with our speed optimized routines
			Debug.Assert(stride == 512);

			int[] tilebuf = new int[8 * 8];
			for (int i = 0; i < 64 * 32; i++)
			{
				Decode8x8x4bpp(tilebuf, 0, 32 * i);
				int ty = i / 64;
				int tx = i % 64;
				ty *= 8;
				tx *= 8;
				for (int y = 0; y < 8; y++)
					for (int x = 0; x < 8; x++)
					{
						screen[(ty + y) * stride + tx + x] = tilebuf[y * 8 + x];
					}
			}

			Paletteize(screen, 0, startcolor, 64 * 32 * 8 * 8);
			Colorize(screen, 0, 64 * 32 * 8 * 8);
		}

		/// <summary>
		/// decodes all the tiles in vram as if they were 4bpp tiles to a 32x32 tile (256x256 pixel) screen
		/// </summary>
		public void DecodeTiles8bpp(int* screen, int stride, int startcolor)
		{
			//cant handle this with our speed optimized routines
			Debug.Assert(stride == 256);

			int[] tilebuf = new int[8 * 8];
			for (int i = 0; i < 32 * 32; i++)
			{
				Decode8x8x8bpp(tilebuf, 0, 64 * i);
				int ty = i / 32;
				int tx = i % 32;
				ty *= 8;
				tx *= 8;
				for (int y = 0; y < 8; y++)
					for (int x = 0; x < 8; x++)
					{
						screen[(ty + y) * stride + tx + x] = tilebuf[y * 8 + x];
					}
			}

			Paletteize(screen, 0, startcolor, 32 * 32 * 8 * 8);
			Colorize(screen, 0, 32 * 32 * 8 * 8);
		}
	}
}


//GraphicsDecoder dec = new GraphicsDecoder();
//int[] tilebuf = new int[8 * 8];
//int[] screen = new int[64*64*8*8];
//for (int i = 0; i < 64 * 64; i++)
//{
//  dec.Decode8x8x2bpp(tilebuf, 0, 16 * i);
//  int ty = i / 64;
//  int tx = i % 64;
//  ty *= 8;
//  tx *= 8;
//  for(int y=0;y<8;y++)
//    for (int x = 0; x < 8; x++)
//    {
//      screen[(ty + y) * 512 + tx + x] = tilebuf[y * 8 + x];
//    }
//}
//dec.Paletteize2bpp(screen, 0, 0, 64 * 64 * 8 * 8);
//dec.Colorize(screen, 0, 64 * 64 * 8 * 8);
//MemoryStream ms = new MemoryStream();
//foreach (int i in screen)
//{
//  ms.WriteByte((byte)(i & 0xFF));
//  ms.WriteByte((byte)((i >> 8) & 0xFF));
//  ms.WriteByte((byte)((i >> 16) & 0xFF));
//}
//File.WriteAllBytes("c:\\dump\\file" + ctr, ms.ToArray());
//ctr++;