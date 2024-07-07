using System.Drawing;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using static BizHawk.Emulation.Cores.Nintendo.BSNES.BsnesApi.SNES_REGISTER;
using static BizHawk.Emulation.Cores.Nintendo.SNES.SNESGraphicsDecoder;

namespace BizHawk.Emulation.Cores.Nintendo.BSNES
{
	public sealed unsafe class SNESGraphicsDecoder : ISNESGraphicsDecoder
	{
		private readonly BsnesApi _api;
		private readonly byte* vram; // waterbox pointer, ALWAYS access with EnterExit()
		private readonly ushort* cgram; // waterbox pointer, ALWAYS access with EnterExit()
		private readonly byte[][] cachedTiles = new byte[5][];
		private readonly int[] bppArrayIndex = { 0, 0, 0, 0, 1, 0, 0, 0, 2 };

		private bool useBackColor;
		private int backColor;

		private readonly int[] palette = new int[32768];
		private readonly short[] directColorTable = new short[256];
		private void generate_palette()
		{
			const int a = 0xFF;
			for (int color = 0; color < 32768; color++) {
				int r = (color >> 10) & 31;
				int g = (color >>  5) & 31;
				int b = (color >>  0) & 31;

				r = r << 3 | r >> 2; r = r << 8 | r << 0;
				g = g << 3 | g >> 2; g = g << 8 | g << 0;
				b = b << 3 | b >> 2; b = b << 8 | b << 0;

				palette[color] = a << 24 | b >> 8 << 16 | g >> 8 <<  8 | r >> 8 << 0;
			}
		}

		private void generate_directColorTable()
		{
			for (int i = 0; i < 256; i++)
			{
				directColorTable[i] = (short)
						( (i << 2 & 0x001c)    //R
						+ (i << 4 & 0x0380)    //G
						+ (i << 7 & 0x6000) ); //B
			}
		}

		public SNESGraphicsDecoder(BsnesApi api)
		{
			_api = api;
			vram = (byte*)api.core.snes_get_memory_region((int)BsnesApi.SNES_MEMORY.VRAM, out _, out _);
			cgram = (ushort*)api.core.snes_get_memory_region((int)BsnesApi.SNES_MEMORY.CGRAM, out _, out _);
			generate_palette();
			generate_directColorTable();
		}

		public void CacheTiles()
		{
			CacheTiles2Bpp();
			CacheTiles_Merge(4);
			CacheTiles_Merge(8);
			CacheTilesMode7();
			CacheTilesMode7ExtBg();
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
				cachedTiles[3][i] = vram[2 * i + 1];
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
			return palette[rgb555];
		}

		public void Colorize(int* buf, int offset, int numpixels)
		{
			for (int i = 0; i < numpixels; i++)
			{
				buf[offset + i] = palette[buf[offset + i]];
			}
		}

		public ISNESGraphicsDecoder.OAMInfo CreateOAMInfo(ScreenInfo si, int num) => new OAMInfo(_api.core.snes_read_oam, si, num);

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
		{
			byte[] cachedTileBuffer = cachedTiles[extBg ? 4 : 3];
			for (int ty = 0, tidx = 0; ty < 128; ty++)
			for (int tx = 0; tx < 128; tx++, tidx++)
			{
				int tileEntry = vram[tidx * 2];
				int src = tileEntry * 64;
				for (int py = 0; py < 8; py++)
				for (int px = 0; px < 8; px++)
				{
					int dst = (ty * 8 + py) * stride + (tx * 8 + px);
					int srcData = cachedTileBuffer[src++];
					screen[dst] = srcData;
				}
			}
		}

		public void DirectColorify(int* screen, int numPixels)
		{
			for (int i = 0; i < numPixels; i++)
			{
				screen[i] = directColorTable[screen[i]];
			}
		}

		public void Enter()
			=> _api.Enter();

		public void Exit()
			=> _api.Exit();

		public TileEntry[] FetchMode7Tilemap()
		{
			var buf = new TileEntry[128 * 128];
			for (int tidx = 0; tidx < 128 * 128; tidx++)
			{
				buf[tidx].address = tidx * 2;
				buf[tidx].tilenum = vram[tidx * 2];
			}

			return buf;
		}

		public TileEntry[] FetchTilemap(int addr, ScreenSize size)
		{
			Dimensions blockDimensions = SizeInBlocksForBGSize(size);
			int realWidth = blockDimensions.Width * 32;
			int realHeight = blockDimensions.Height * 32;
			TileEntry[] buf = new TileEntry[realWidth * realHeight];

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

			return buf;
		}

		public int[] GetPalette()
		{
			int[] ret = new int[256];
			for (int i = 0; i < 256; i++)
				ret[i] = cgram[i] & 0x7FFF;
			return ret;
		}

		public void Paletteize(int* buf, int offset, int startcolor, int numpixels)
		{
			for (int i = 0; i < numpixels; i++)
			{
				int entry = buf[offset + i];
				int color = entry == 0 && useBackColor ? backColor : cgram[startcolor + entry] & 0x7FFF;

				buf[offset + i] = color;
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
		{
			byte[] cachedTileBuffer = cachedTiles[ext ? 4 : 3];
			for (int i = 0; i < numTiles; i++)
			{
				int targetYPos = i / tilesWide * stride * 8;
				int targetXPos = i % tilesWide * 8;
				int destinationOffset = targetYPos + targetXPos;
				int sourceOffset = (startTile + i) * 64;
				for (int y = 0; y < 8; y++)
				for (int x = 0; x < 8; x++)
				{
					screen[destinationOffset + y * stride + x] = cachedTileBuffer[sourceOffset++];
				}
			}

			int numPixels = numTiles * 8 * 8;
			if (directColor) DirectColorify(screen, numPixels);
			else Paletteize(screen, 0, 0, numPixels);
			Colorize(screen, 0, numPixels);
		}

		public void RenderSpriteToScreen(
			int* screen,
			int stride,
			int destx,
			int desty, ScreenInfo si,
			int spritenum,
			ISNESGraphicsDecoder.OAMInfo oamInfo,
			int xlimit,
			int ylimit,
			byte[,] spriteMap)
		{
			oamInfo ??= new OAMInfo(_api.core.snes_read_oam, si, spritenum);
			Size dim = ObjSizes[si.OBSEL_Size, oamInfo.Size ? 1 : 0];

			byte[] cachedTileBuffer = cachedTiles[bppArrayIndex[4]];

			int baseaddr = oamInfo.Table ? si.OBJTable1Addr : si.OBJTable0Addr;

			//TODO - flips of 'undocumented' rectangular oam settings are wrong. probably easy to do right, but we need a test

			int bcol = oamInfo.Tile & 0xF;
			int brow = (oamInfo.Tile >> 4) & 0xF;
			for (int oy = 0; oy < dim.Height; oy++)
			for (int ox = 0; ox < dim.Width; ox++)
			{
				int dy, dx;

				if (oamInfo.HFlip)
					dx = dim.Width - 1 - ox;
				else dx = ox;
				if (oamInfo.VFlip)
					dy = dim.Height - 1 - oy;
				else dy = oy;

				dx += destx;
				dy += desty;

				if (dx >= xlimit || dy >= ylimit || dx < 0 || dy < 0)
					continue;

				int col = (bcol + (ox >> 3)) & 0xF;
				int row = (brow + (oy >> 3)) & 0xF;
				int sx = ox & 0x7;
				int sy = oy & 0x7;

				int addr = baseaddr * 2 + (row * 16 + col) * 64;
				addr += sy * 8 + sx;

				int dofs = stride * dy + dx;
				int color = cachedTileBuffer[addr];
				if (spriteMap != null && color == 0)
				{
					//skip transparent pixels
				}
				else
				{
					screen[dofs] = color;
					Paletteize(screen, dofs, oamInfo.Palette * 16 + 128, 1);
					Colorize(screen, dofs, 1);
					if (spriteMap != null) spriteMap[dx, dy] = (byte)spritenum;
				}
			}
		}

		public void RenderTilesToScreen(
			int* screen,
			int stride,
			int bpp,
			int startcolor,
			int startTile,
			int numTiles)
		{
			if (numTiles == -1)
				numTiles = 8192 / bpp;
			byte[] cachedTileBuffer = cachedTiles[bppArrayIndex[bpp]];
			int tilesPerRow = stride / 8;
			for (int i = 0; i < numTiles; i++)
			{
				int targetYPos = i / tilesPerRow * stride * 8;
				int targetXPos = i % tilesPerRow * 8;
				int destinationOffset = targetYPos + targetXPos;
				int sourceOffset = (startTile + i) * 64;
				for (int y = 0; y < 8; y++)
				for (int x = 0; x < 8; x++)
				{
					screen[destinationOffset + y * stride + x] = cachedTileBuffer[sourceOffset++];
				}
			}

			int numPixels = numTiles * 8 * 8;
			Paletteize(screen, 0, startcolor, numPixels);
			Colorize(screen, 0, numPixels);
		}

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

			screenInfo.ObjSizeBounds = ObjSizes[screenInfo.OBSEL_Size, 1];
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
		{
			if (snescol == -1)
			{
				useBackColor = false;
			}
			else
			{
				useBackColor = true;
				backColor = snescol;
			}
		}
	}

	internal class OAMInfo : ISNESGraphicsDecoder.OAMInfo
	{
		public ushort X { get; }
		public byte Y { get; }
		public int Tile { get; }
		public bool Table { get; }
		public int Palette { get; }
		public byte Priority { get; }
		public bool VFlip { get; }
		public bool HFlip { get; }
		public bool Size { get; }
		public int Address { get; }

		public OAMInfo(Func<ushort, byte> readOam, ScreenInfo si, int index)
		{
			ushort lowaddr = (ushort)(index * 4);
			X = readOam(lowaddr++);
			Y = readOam(lowaddr++);
			byte character = readOam(lowaddr++);
			Table = (readOam(lowaddr) & 1) == 1;
			Palette = (readOam(lowaddr) >> 1) & 7;
			Priority = (byte)((readOam(lowaddr) >> 4) & 3);
			HFlip = ((readOam(lowaddr) >> 6) & 1) == 1;
			VFlip = ((readOam(lowaddr) >> 7) & 1) == 1;

			int highaddr = index / 4;
			int shift = (index % 4) * 2;
			byte high = readOam((ushort)(512 + highaddr));

			bool highX = (high & (1 << shift++)) != 0;
			Size = (high & (1 << shift)) != 0;
			if (highX) X += 256;

			Tile = character + (Table ? 256 : 0);
			Address = character * 32 + (Table ? si.OBJTable1Addr : si.OBJTable0Addr);
			Address &= 0xFFFF;
		}
	}
}
