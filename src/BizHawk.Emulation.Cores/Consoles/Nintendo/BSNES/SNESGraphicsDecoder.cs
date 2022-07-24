using System;
using System.Drawing;
using BizHawk.Common;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using static BizHawk.Emulation.Cores.Nintendo.BSNES.BsnesApi.SNES_REGISTER;
using static BizHawk.Emulation.Cores.Nintendo.SNES.SNESGraphicsDecoder;

namespace BizHawk.Emulation.Cores.Nintendo.BSNES
{
	public sealed unsafe class SNESGraphicsDecoder : ISNESGraphicsDecoder
	{
		private struct Object // size: 10 bytes; equivalent to the c++ version
		{
			private ushort x;
			private byte y;
			private byte character;
			private bool nameSelect;
			private bool vflip;
			private bool hflip;
			private byte priority;
			private byte palette;
			private bool size;
		}

		private readonly BsnesApi _api;
		private readonly byte* vram; // waterbox pointer, ALWAYS access with EnterExit()
		private Object* objects; // waterbox pointer, ALWAYS access with EnterExit()
		private readonly ushort* cgram; // waterbox pointer, ALWAYS access with EnterExit()
		private readonly byte[][] cachedTiles = new byte[5][];
		private readonly int[] bppArrayIndex = { 0, 0, 0, 0, 1, 0, 0, 0, 2 };

		private readonly int[] palette;

		public SNESGraphicsDecoder(BsnesApi api)
		{
			_api = api;
			vram = (byte*) api.core.snes_get_memory_region((int)BsnesApi.SNES_MEMORY.VRAM, out _, out _);
			objects = (Object*) api.core.snes_get_memory_region((int)BsnesApi.SNES_MEMORY.OBJECTS, out _, out _);
			cgram = (ushort*)api.core.snes_get_memory_region((int)BsnesApi.SNES_MEMORY.CGRAM, out _, out _);
			palette = SnesColors.GetLUT(SnesColors.ColorType.BSNES);
		}

		public void CacheTiles()
		{
			using (_api.EnterExit()) // for vram access
			{
				CacheTiles2Bpp();
				CacheTiles_Merge(4);
				CacheTiles_Merge(8);
				CacheTilesMode7();
				CacheTilesMode7ExtBg();
			}
		}

		private void CacheTiles2Bpp()
		{
			const int tileCount = 65536 / 8 / 2;
			cachedTiles[0] ??= new byte[8 * 8 * tileCount];

			for (int i = 0; i < tileCount; i++)
			{
				int offset = 64 * i;
				int addr = 16 * i;
				const int stride = 8;
				for (int y = 0; y < 8; y++)
				{
					byte val = vram[addr + 1];
					for (int x = 0; x < 8; x++)
						cachedTiles[0][offset + y * stride + x] = (byte)(val >> (7 - x) & 1);
					val = vram[addr + 0];
					for (int x = 0; x < 8; x++)
						cachedTiles[0][offset + y * stride + x] = (byte)((cachedTiles[0][offset + y * stride + x] << 1) | (val >> (7 - x) & 1));
					addr += 2;
				}
			}

		}

		private void CacheTiles_Merge(int toBpp)
		{
			int shift = toBpp / 2;
			int tileCount = 8192 / toBpp;
			int destinationIndex = bppArrayIndex[toBpp];
			cachedTiles[destinationIndex] ??= new byte[8 * 8 * tileCount];

			for (int i = 0; i < tileCount; i++)
			{
				int srcAddr = 128 * i;
				int dstAddr = 64 * i;
				for (int p = 0; p < 64; p++)
				{
					int tileA = cachedTiles[destinationIndex - 1][srcAddr + p];
					int tileB = cachedTiles[destinationIndex - 1][srcAddr + p + 64];
					cachedTiles[destinationIndex][dstAddr + p] = (byte)(tileA | (tileB << shift));
				}
			}
		}

		private void CacheTilesMode7()
		{
			const int tileCount = 256;
			const int pixelCount = 8 * 8 * tileCount;
			cachedTiles[3] ??= new byte[pixelCount];

			for (int i = 0; i < pixelCount; i++)
				cachedTiles[3][i] = vram[2*i + 1];
		}

		private void CacheTilesMode7ExtBg()
		{
			const int tileCount = 256;
			const int pixelCount = 8 * 8 * tileCount;
			cachedTiles[4] ??= new byte[pixelCount];

			for (int i = 0; i < pixelCount; i++)
				cachedTiles[4][i] = (byte)(cachedTiles[3][i] & 0x7F);
		}

		public int Colorize(int rgb555)
		{
			return palette[491520 + rgb555];
		}

		public void Colorize(int* buf, int offset, int numpixels)
		{
			for (int i = 0; i < numpixels; i++)
			{
				buf[offset + i] = palette[491520 + buf[offset + i]];
			}
		}

		public ISNESGraphicsDecoder.OAMInfo CreateOAMInfo(ScreenInfo si, int num)
			=> throw new NotImplementedException();

		public void DecodeBG(
			int* screen,
			int stride, TileEntry[] map,
			int tiledataBaseAddr, ScreenSize size,
			int bpp,
			int tilesize,
			int paletteStart)
		{
			//emergency backstop. this can only happen if we're displaying an unavailable BG or other similar such value
			if (bpp == 0) return;

			int ncolors = 1 << bpp;

			var dims = SizeInTilesForBGSize(size);
			int count8x8 = tilesize / 8;
			int tileSizeBytes = 8 * bpp;
			int baseTileNum = tiledataBaseAddr / tileSizeBytes;
			byte[] tileCache = cachedTiles[bppArrayIndex[bpp]];
			int tileCacheMask = tileCache.Length - 1;

			for (int mty = 0; mty < dims.Height; mty++)
			for (int mtx = 0; mtx < dims.Width; mtx++)
			for (int tx = 0; tx < count8x8; tx++)
			for (int ty = 0; ty < count8x8; ty++)
			{
				int mapIndex = mty * dims.Width + mtx;
				var te = map[mapIndex];

				//apply metatile flipping
				int tnx = tx, tny = ty;
				if (tilesize == 16)
				{
					if ((te.flags & TileEntryFlags.Horz) != 0) tnx = 1 - tnx;
					if ((te.flags & TileEntryFlags.Vert) != 0) tny = 1 - tny;
				}

				int tileNum = te.tilenum + tnx + tny * 16 + baseTileNum;
				int srcOfs = tileNum * 64;

				for (int y = 0; y < 8; y++)
				for (int x = 0; x < 8; x++)
				{
					int px = x;
					int py = y;
					if ((te.flags & TileEntryFlags.Horz) != 0) px = 7 - x;
					if ((te.flags & TileEntryFlags.Vert) != 0) py = 7 - y;
					int dstX = (mtx * count8x8 + tx) * 8 + px;
					int dstY = (mty * count8x8 + ty) * 8 + py;
					int dstOfs = dstY * stride + dstX;
					int color = tileCache[srcOfs & tileCacheMask];
					srcOfs++;
					// if (color != 0)
					// {
						color += te.palette * ncolors;
						color += paletteStart;
					// }

					screen[dstOfs] = color;
				}
			}
		}

		public void DecodeMode7BG(int* screen, int stride, bool extBg)
			=> throw new NotImplementedException();

		public void DirectColorify(int* screen, int numPixels)
			=> throw new NotImplementedException();

		public void Dispose() {}

		public void Enter()
			=> _api.Enter();

		public void Exit()
			=> _api.Exit();

		public TileEntry[] FetchMode7Tilemap()
			=> throw new NotImplementedException();

		public TileEntry[] FetchTilemap(int addr, ScreenSize size)
		{
			Dimensions blockDimensions = SizeInBlocksForBGSize(size);
			int realWidth = blockDimensions.Width * 32;
			int realHeight = blockDimensions.Height * 32;
			TileEntry[] buf = new TileEntry[realWidth*realHeight];

			using (_api.EnterExit())
			{
				for (int by = 0; by < blockDimensions.Height; by++)
				for (int bx = 0; bx < blockDimensions.Width; bx++)
				for (int y = 0; y < 32; y++)
				for (int x = 0; x < 32; x++)
				{
					int idx = (by * 32 + y) * realWidth + bx * 32 + x;
					ushort entry = *(ushort*)(vram + addr);
					buf[idx].tilenum = (ushort)(entry & 0x3FF);
					buf[idx].palette = (byte)((entry >> 10) & 7);
					buf[idx].flags = (TileEntryFlags)((entry >> 13) & 7);
					buf[idx].address = addr;
					addr += 2;
				}
			}

			return buf;
		}

		public int[] GetPalette()
		{
			using (_api.EnterExit())
			{
				int[] ret = new int[256];
				for (int i = 0; i < 256; i++)
					ret[i] = cgram[i] & 0x7FFF;
				return ret;
			}
		}

		public void Paletteize(int* buf, int offset, int startcolor, int numpixels)
		{
			using (_api.EnterExit())
			{
				for (int i = 0; i < numpixels; i++)
				{
					int entry = buf[offset + i];
					int color = cgram[startcolor + entry] & 0x7FFF;

					buf[offset + i] = color;
				}
			}
		}

		public void RenderMode7TilesToScreen(
			int* screen,
			int stride,
			bool ext,
			bool directColor,
			int tilesWide,
			int startTile,
			int numTiles)
				=> throw new NotImplementedException();

		public void RenderSpriteToScreen(
			int* screen,
			int stride,
			int destx,
			int desty, ScreenInfo si,
			int spritenum,
			ISNESGraphicsDecoder.OAMInfo oam,
			int xlimit,
			int ylimit,
			byte[,] spriteMap)
				=> throw new NotImplementedException();

		public void RenderTilesToScreen(
			int* screen,
			int tilesWide,
			int tilesTall,
			int stride,
			int bpp,
			int startcolor,
			int startTile,
			int numTiles,
			bool descramble16)
				=> throw new NotImplementedException();

		public ScreenInfo ScanScreenInfo()
		{
			int OBSEL_NameSel = _api.core.snes_peek_logical_register(OBSEL_NAMESEL);
			int OBSEL_NameBase = _api.core.snes_peek_logical_register(OBSEL_NAMEBASE);

			ScreenInfo screenInfo = new()
			{
				Mode = _api.core.snes_peek_logical_register(BG_MODE),
				Mode1_BG3_Priority = _api.core.snes_peek_logical_register(BG3_PRIORITY) == 1,
				SETINI_Mode7ExtBG = _api.core.snes_peek_logical_register(SETINI_MODE7_EXTBG) == 1,
				SETINI_HiRes = _api.core.snes_peek_logical_register(SETINI_HIRES) == 1,
				SETINI_Overscan = _api.core.snes_peek_logical_register(SETINI_OVERSCAN) == 1,
				SETINI_ObjInterlace = _api.core.snes_peek_logical_register(SETINI_OBJ_INTERLACE) == 1,
				SETINI_ScreenInterlace = _api.core.snes_peek_logical_register(SETINI_SCREEN_INTERLACE) == 1,
				CGWSEL_ColorMask = _api.core.snes_peek_logical_register(CGWSEL_COLORMASK),
				CGWSEL_ColorSubMask = _api.core.snes_peek_logical_register(CGWSEL_COLORSUBMASK),
				CGWSEL_AddSubMode = _api.core.snes_peek_logical_register(CGWSEL_ADDSUBMODE),
				CGWSEL_DirectColor = _api.core.snes_peek_logical_register(CGWSEL_DIRECTCOLOR) == 1,
				CGADSUB_AddSub = _api.core.snes_peek_logical_register(CGADDSUB_MODE),
				CGADSUB_Half = _api.core.snes_peek_logical_register(CGADDSUB_HALF) == 1,
				OBSEL_Size = _api.core.snes_peek_logical_register(OBSEL_SIZE),
				OBSEL_NameSel = OBSEL_NameSel,
				OBSEL_NameBase = OBSEL_NameBase,
				OBJTable0Addr = OBSEL_NameBase << 14,
				OBJTable1Addr = ((OBSEL_NameBase << 14) + ((OBSEL_NameSel + 1) << 13)) & 0xFFFF,
				OBJ_MainEnabled = _api.core.snes_peek_logical_register(TM_OBJ) == 1,
				OBJ_SubEnabled = _api.core.snes_peek_logical_register(TS_OBJ) == 1,
				OBJ_MathEnabled = _api.core.snes_peek_logical_register(CGADDSUB_OBJ) == 1,
				BK_MathEnabled = _api.core.snes_peek_logical_register(CGADDSUB_BACKDROP) == 1,
				M7HOFS = _api.core.snes_peek_logical_register(M7HOFS),
				M7VOFS = _api.core.snes_peek_logical_register(M7VOFS),
				M7A = _api.core.snes_peek_logical_register(M7A),
				M7B = _api.core.snes_peek_logical_register(M7B),
				M7C = _api.core.snes_peek_logical_register(M7C),
				M7D = _api.core.snes_peek_logical_register(M7D),
				M7X = _api.core.snes_peek_logical_register(M7X),
				M7Y = _api.core.snes_peek_logical_register(M7Y),
				M7SEL_REPEAT = _api.core.snes_peek_logical_register(M7SEL_REPEAT),
				M7SEL_HFLIP = _api.core.snes_peek_logical_register(M7SEL_HFLIP) == 1,
				M7SEL_VFLIP = _api.core.snes_peek_logical_register(M7SEL_VFLIP) == 1
			};

			screenInfo.ObjSizeBounds = ObjSizes[screenInfo.OBSEL_Size,1];
			int square = Math.Max(screenInfo.ObjSizeBounds.Width, screenInfo.ObjSizeBounds.Height);
			screenInfo.ObjSizeBoundsSquare = new Size(square, square);

			screenInfo.BG.BG1.Bpp = ModeBpps[screenInfo.Mode, 0];
			screenInfo.BG.BG2.Bpp = ModeBpps[screenInfo.Mode, 1];
			screenInfo.BG.BG3.Bpp = ModeBpps[screenInfo.Mode, 2];
			screenInfo.BG.BG4.Bpp = ModeBpps[screenInfo.Mode, 3];

			//initial setting of mode type (derived from bpp table.. mode7 bg types will be fixed up later)
			for (int i = 1; i <= 4; i++)
				screenInfo.BG[i].BGMode = screenInfo.BG[i].Bpp == 0 ? BGMode.Unavailable : BGMode.Text;

			screenInfo.BG.BG1.TILESIZE = _api.core.snes_peek_logical_register(BG1_TILESIZE);
			screenInfo.BG.BG2.TILESIZE = _api.core.snes_peek_logical_register(BG2_TILESIZE);
			screenInfo.BG.BG3.TILESIZE = _api.core.snes_peek_logical_register(BG3_TILESIZE);
			screenInfo.BG.BG4.TILESIZE = _api.core.snes_peek_logical_register(BG4_TILESIZE);

			screenInfo.BG.BG1.SCSIZE = _api.core.snes_peek_logical_register(BG1_SCSIZE);
			screenInfo.BG.BG2.SCSIZE = _api.core.snes_peek_logical_register(BG2_SCSIZE);
			screenInfo.BG.BG3.SCSIZE = _api.core.snes_peek_logical_register(BG3_SCSIZE);
			screenInfo.BG.BG4.SCSIZE = _api.core.snes_peek_logical_register(BG4_SCSIZE);
			screenInfo.BG.BG1.SCADDR = _api.core.snes_peek_logical_register(BG1_SCADDR);
			screenInfo.BG.BG2.SCADDR = _api.core.snes_peek_logical_register(BG2_SCADDR);
			screenInfo.BG.BG3.SCADDR = _api.core.snes_peek_logical_register(BG3_SCADDR);
			screenInfo.BG.BG4.SCADDR = _api.core.snes_peek_logical_register(BG4_SCADDR);
			screenInfo.BG.BG1.TDADDR = _api.core.snes_peek_logical_register(BG1_TDADDR);
			screenInfo.BG.BG2.TDADDR = _api.core.snes_peek_logical_register(BG2_TDADDR);
			screenInfo.BG.BG3.TDADDR = _api.core.snes_peek_logical_register(BG3_TDADDR);
			screenInfo.BG.BG4.TDADDR = _api.core.snes_peek_logical_register(BG4_TDADDR);

			screenInfo.BG.BG1.MainEnabled = _api.core.snes_peek_logical_register(TM_BG1) == 1;
			screenInfo.BG.BG2.MainEnabled = _api.core.snes_peek_logical_register(TM_BG2) == 1;
			screenInfo.BG.BG3.MainEnabled = _api.core.snes_peek_logical_register(TM_BG3) == 1;
			screenInfo.BG.BG4.MainEnabled = _api.core.snes_peek_logical_register(TM_BG4) == 1;
			screenInfo.BG.BG1.SubEnabled = _api.core.snes_peek_logical_register(TS_BG1) == 1;
			screenInfo.BG.BG2.SubEnabled = _api.core.snes_peek_logical_register(TS_BG2) == 1;
			screenInfo.BG.BG3.SubEnabled = _api.core.snes_peek_logical_register(TS_BG3) == 1;
			screenInfo.BG.BG4.SubEnabled = _api.core.snes_peek_logical_register(TS_BG4) == 1;
			screenInfo.BG.BG1.MathEnabled = _api.core.snes_peek_logical_register(CGADDSUB_BG1) == 1;
			screenInfo.BG.BG2.MathEnabled = _api.core.snes_peek_logical_register(CGADDSUB_BG2) == 1;
			screenInfo.BG.BG3.MathEnabled = _api.core.snes_peek_logical_register(CGADDSUB_BG3) == 1;
			screenInfo.BG.BG4.MathEnabled = _api.core.snes_peek_logical_register(CGADDSUB_BG4) == 1;

			screenInfo.BG.BG1.HOFS = _api.core.snes_peek_logical_register(BG1HOFS);
			screenInfo.BG.BG1.VOFS = _api.core.snes_peek_logical_register(BG1VOFS);
			screenInfo.BG.BG2.HOFS = _api.core.snes_peek_logical_register(BG2HOFS);
			screenInfo.BG.BG2.VOFS = _api.core.snes_peek_logical_register(BG2VOFS);
			screenInfo.BG.BG3.HOFS = _api.core.snes_peek_logical_register(BG3HOFS);
			screenInfo.BG.BG3.VOFS = _api.core.snes_peek_logical_register(BG3VOFS);
			screenInfo.BG.BG4.HOFS = _api.core.snes_peek_logical_register(BG4HOFS);
			screenInfo.BG.BG4.VOFS = _api.core.snes_peek_logical_register(BG4VOFS);

			for (int i = 1; i <= 4; i++)
			{
				screenInfo.BG[i].Mode = screenInfo.Mode;
				screenInfo.BG[i].TiledataAddr = screenInfo.BG[i].TDADDR << 13;
				screenInfo.BG[i].ScreenAddr = screenInfo.BG[i].SCADDR << 9;
			}

			//fixup irregular things for mode 7
			if (screenInfo.Mode == 7)
			{
				screenInfo.BG.BG1.TiledataAddr = 0;
				screenInfo.BG.BG1.ScreenAddr = 0;

				screenInfo.BG.BG1.BGMode = screenInfo.CGWSEL_DirectColor ? BGMode.Mode7DC : BGMode.Mode7;

				if (screenInfo.SETINI_Mode7ExtBG)
				{
					screenInfo.BG.BG2.BGMode = BGMode.Mode7Ext;
					screenInfo.BG.BG2.Bpp = 7;
					screenInfo.BG.BG2.TiledataAddr = 0;
					screenInfo.BG.BG2.ScreenAddr = 0;
				}
			}

			//determine which colors each BG could use
			switch (screenInfo.Mode)
			{
				case 0:
					screenInfo.BG.BG1.PaletteSelection = new PaletteSelection(0, 32);
					screenInfo.BG.BG2.PaletteSelection = new PaletteSelection(32, 32);
					screenInfo.BG.BG3.PaletteSelection = new PaletteSelection(64, 32);
					screenInfo.BG.BG4.PaletteSelection = new PaletteSelection(96, 32);
					break;
				case 1:
					screenInfo.BG.BG1.PaletteSelection = new PaletteSelection(0, 128);
					screenInfo.BG.BG2.PaletteSelection = new PaletteSelection(0, 128);
					screenInfo.BG.BG3.PaletteSelection = new PaletteSelection(0, 32);
					screenInfo.BG.BG4.PaletteSelection = new PaletteSelection(0, 0);
					break;
				case 2:
					screenInfo.BG.BG1.PaletteSelection = new PaletteSelection(0, 128);
					screenInfo.BG.BG2.PaletteSelection = new PaletteSelection(0, 128);
					screenInfo.BG.BG3.PaletteSelection = new PaletteSelection(0, 0);
					screenInfo.BG.BG4.PaletteSelection = new PaletteSelection(0, 0);
					break;
				case 3:
				case 7:
					screenInfo.BG.BG1.PaletteSelection = new PaletteSelection(0, 256);
					screenInfo.BG.BG2.PaletteSelection = new PaletteSelection(0, 128);
					screenInfo.BG.BG3.PaletteSelection = new PaletteSelection(0, 0);
					screenInfo.BG.BG4.PaletteSelection = new PaletteSelection(0, 0);
					break;
				case 4:
					screenInfo.BG.BG1.PaletteSelection = new PaletteSelection(0, 256);
					screenInfo.BG.BG2.PaletteSelection = new PaletteSelection(0, 32);
					screenInfo.BG.BG3.PaletteSelection = new PaletteSelection(0, 0);
					screenInfo.BG.BG4.PaletteSelection = new PaletteSelection(0, 0);
					break;
				case 5:
				case 6:
					screenInfo.BG.BG1.PaletteSelection = new PaletteSelection(0, 128);
					screenInfo.BG.BG2.PaletteSelection = new PaletteSelection(0, 32);
					screenInfo.BG.BG3.PaletteSelection = new PaletteSelection(0, 0);
					screenInfo.BG.BG4.PaletteSelection = new PaletteSelection(0, 0);
					break;
			}

			return screenInfo;
		}

		public void SetBackColor(int snescol)
			=> throw new NotImplementedException();
	}
}
