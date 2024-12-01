﻿//http://wiki.superfamicom.org/snes/show/Backgrounds
//http://board.zsnes.com/phpBB3/viewtopic.php?f=10&t=13029&start=75 yoshis island offset per tile demos. and other demos of advanced modes
//but we wont worry about offset per tile modes here.

//helpful detailed reg list
//http://wiki.superfamicom.org/snes/show/Registers

//TODO
//when a BG is not available, the last rendered BG still shows up. should clear it

using BizHawk.Common;

using System.Drawing;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public unsafe interface ISNESGraphicsDecoder : IMonitor
	{
		public interface OAMInfo
		{
			ushort X { get; }

			byte Y { get; }

			int Tile { get; }

			bool Table { get; }

			int Palette { get; }

			byte Priority { get; }

			bool VFlip { get; }

			bool HFlip { get; }

			bool Size { get; }

			int Address { get; }
		}

		void CacheTiles();

		int Colorize(int rgb555);

		void Colorize(int* buf, int offset, int numpixels);

		OAMInfo CreateOAMInfo(SNESGraphicsDecoder.ScreenInfo si, int num);

		void DecodeBG(
			int* screen,
			int stride,
			SNESGraphicsDecoder.TileEntry[] map,
			int tiledataBaseAddr,
			SNESGraphicsDecoder.ScreenSize size,
			int bpp,
			int tilesize,
			int paletteStart);

		void DecodeMode7BG(int* screen, int stride, bool extBg);

		void DirectColorify(int* screen, int numPixels);

		SNESGraphicsDecoder.TileEntry[] FetchMode7Tilemap();

		SNESGraphicsDecoder.TileEntry[] FetchTilemap(int addr, SNESGraphicsDecoder.ScreenSize size);

		int[] GetPalette();

		void Paletteize(int* buf, int offset, int startcolor, int numpixels);

		void RenderMode7TilesToScreen(
			int* screen,
			int stride,
			bool ext,
			bool directColor,
			int tilesWide = 16,
			int startTile = 0,
			int numTiles = 256);

		void RenderSpriteToScreen(
			int* screen,
			int stride,
			int destx,
			int desty,
			SNESGraphicsDecoder.ScreenInfo si,
			int spritenum,
			OAMInfo oam = null,
			int xlimit = 1024,
			int ylimit = 1024,
			byte[,] spriteMap = null);

		void RenderTilesToScreen(
			int* screen,
			int stride,
			int bpp,
			int startcolor,
			int startTile = 0,
			int numTiles = -1);

		SNESGraphicsDecoder.ScreenInfo ScanScreenInfo();

		void SetBackColor(int snescol = -1);
	}

	public unsafe class SNESGraphicsDecoder : ISNESGraphicsDecoder
	{
		public class PaletteSelection
		{
			public PaletteSelection() { }
			public PaletteSelection(int start, int size)
			{
				this.start = start;
				this.size = size;
			}
			public int start, size;
		}

		public struct Dimensions
		{
			public Dimensions(int w, int h) { Width = w; Height = h; }
			public int Width, Height;
			public override string ToString() => $"{Width}x{Height}";
		}

		public enum ScreenSize
		{
			AAAA_32x32 = 0, ABAB_64x32 = 1, AABB_32x64 = 2, ABCD_64x64 = 3,
			Hacky_1x1 = 4,
		}

		public static Dimensions SizeInTilesForBGSize(ScreenSize size)
		{
			if (size == ScreenSize.Hacky_1x1) return new Dimensions(1, 1);
			var ret = SizeInBlocksForBGSize(size);
			ret.Width *= 32;
			ret.Height *= 32;
			return ret;
		}

		public static readonly Size[,] ObjSizes =
		{
			{ new(8,8), new(16,16) },
			{ new(8,8), new(32,32) },
			{ new(8,8), new(64,64) },
			{ new(16,16), new(32,32) },
			{ new(16,16), new(64,64) },
			{ new(32,32), new(64,64) },
			{ new(16,32), new(32,64) },
			{ new(16,32), new(32,32) }
		};

		public static Dimensions SizeInBlocksForBGSize(ScreenSize size)
		{
			return size switch
			{
				ScreenSize.AAAA_32x32 => new Dimensions(1, 1),
				ScreenSize.ABAB_64x32 => new Dimensions(2, 1),
				ScreenSize.AABB_32x64 => new Dimensions(1, 2),
				ScreenSize.ABCD_64x64 => new Dimensions(2, 2),
				_ => throw new InvalidOperationException()
			};
		}

		public enum BGMode
		{
			Unavailable, Text, Mode7, Mode7Ext, Mode7DC, OBJ
		}

		/// <summary>
		/// is a BGMode a mode7 type (mode7, mode7ext, mode7DC)
		/// </summary>
		public static bool BGModeIsMode7Type(BGMode bgMode) => bgMode == BGMode.Mode7 || bgMode == BGMode.Mode7DC || bgMode == BGMode.Mode7Ext;

		/// <summary>
		/// this class is not 'smart' - it wont recompute values for you. it's meant to be read only (we should find some way to protect write access to make that clear)
		/// </summary>
		public class BGInfo
		{
			public BGInfo(int num)
			{
			}

			/// <summary>
			/// what type of BG is it?
			/// </summary>
			public BGMode BGMode;

			/// <summary>
			/// is this BGMode a mode7 type (mode7, mode7ext, mode7DC)
			/// </summary>
			public bool BGModeIsMode7Type => BGModeIsMode7Type(BGMode);

			/// <summary>
			/// Is the layer even enabled?
			/// </summary>
			public bool Enabled => Bpp != 0;

			/// <summary>
			/// screen and tiledata register values
			/// </summary>
			public int SCADDR, TDADDR;

			/// <summary>
			/// SCSIZE register
			/// </summary>
			public int SCSIZE;

			/// <summary>
			/// which Mode this BG came from
			/// </summary>
			public int Mode;

			/// <summary>
			/// the address of the screen data
			/// </summary>
			public int ScreenAddr;

			/// <summary>
			/// the address of the tile data
			/// </summary>
			public int TiledataAddr;

			/// <summary>
			/// Screen size (shape, really.)
			/// </summary>
			public ScreenSize ScreenSize => (ScreenSize)SCSIZE;

			/// <summary>
			/// the BPP of the BG, as derived from the current mode
			/// </summary>
			public int Bpp;

			/// <summary>
			/// value of the tilesize register; 1 implies 16x16 tiles
			/// </summary>
			public int TILESIZE;

			/// <summary>
			/// enabled on MAIN Screen via $212C
			/// </summary>
			public bool MainEnabled;

			/// <summary>
			/// enabled on SUB Screen via $212D
			/// </summary>
			public bool SubEnabled;

			/// <summary>
			/// enabled for color math via $2131
			/// </summary>
			public bool MathEnabled;

			/// <summary>
			/// scroll registers
			/// </summary>
			public int HOFS, VOFS;

			/// <summary>
			/// TileSize; 8 or 16
			/// </summary>
			public int TileSize => TILESIZE == 1 ? 16 : 8;

			/// <summary>
			/// The size of the layer, in tiles
			/// </summary>
			public Dimensions ScreenSizeInTiles => BGMode == BGMode.Text
				? SizeInTilesForBGSize(ScreenSize)
				: new Dimensions(128, 128);

			/// <summary>
			/// The size of the layer, in pixels. This has factored in the selection of 8x8 or 16x16 tiles
			/// </summary>
			public Dimensions ScreenSizeInPixels => new Dimensions(ScreenSizeInTiles.Width * TileSize, ScreenSizeInTiles.Height * TileSize);

			/// <summary>
			/// returns information about what colors could possibly be used for this bg
			/// </summary>
			public PaletteSelection PaletteSelection;
		}

		public class BGInfos
		{
			private readonly BGInfo[] bgs = new BGInfo[4] { new BGInfo(1), new BGInfo(2), new BGInfo(3), new BGInfo(4) };
			public BGInfo BG1 => bgs[0];
			public BGInfo BG2 => bgs[1];
			public BGInfo BG3 => bgs[2];
			public BGInfo BG4 => bgs[3];
			public BGInfo this[int index] => bgs[index - 1];
		}

		public class OAMInfo : ISNESGraphicsDecoder.OAMInfo
		{
			public int Index { get; }
			public ushort X { get; }
			public byte Y { get; }
			public int Tile { get; }
			public bool Table { get; }
			public int Palette { get; }
			public byte Priority { get; }
			public bool VFlip { get; }
			public bool HFlip { get; }
			public bool Size { get; }

			/// <summary>
			/// tiledata address
			/// </summary>
			public int Address { get; }

			public OAMInfo(SNESGraphicsDecoder dec, ScreenInfo si, int num)
			{
				Index = num;

				int lowaddr = num*4;
				X = dec.oam[lowaddr++];
				Y = dec.oam[lowaddr++];
				byte name = dec.oam[lowaddr++];
				Table = (dec.oam[lowaddr] & 1) == 1;
				Palette = (dec.oam[lowaddr]>>1) & 7;
				Priority = (byte)((dec.oam[lowaddr] >> 4) & 3);
				HFlip = ((dec.oam[lowaddr] >> 6) & 1) == 1;
				VFlip = ((dec.oam[lowaddr] >> 7) & 1) == 1;

				int highaddr = num / 4;
				int shift = (num % 4) * 2;
				int high = dec.oam[512+highaddr];
				high >>= shift;
				int x = high & 1;
				high >>= 1;
				Size = (high & 1) != 0;
				X = (ushort)(X | (x << 8));

				Tile = name + (Table ? 256 : 0);
				Address = 32 * Tile;

				if (Tile < 256)
					Address += si.OBJTable0Addr;
				else
					Address += si.OBJTable1Addr - (256 * 32);

				Address &= 0xFFFF;
			}
		}

		public class ScreenInfo
		{
			public Size ObjSizeBounds;
			public Size ObjSizeBoundsSquare;

			public BGInfos BG = new BGInfos();

			public int Mode { get; init; }
			public bool Mode1_BG3_Priority { get; init; }

			public bool SETINI_Mode7ExtBG { get; init; }
			public bool SETINI_HiRes { get; init; }
			public bool SETINI_Overscan { get; init; }
			public bool SETINI_ObjInterlace { get; init; }
			public bool SETINI_ScreenInterlace { get; init; }

			public int CGWSEL_ColorMask { get; init; }
			public int CGWSEL_ColorSubMask { get; init; }
			public int CGWSEL_AddSubMode { get; init; }
			public bool CGWSEL_DirectColor { get; init; }
			public int CGADSUB_AddSub { get; init; }
			public bool CGADSUB_Half { get; init; }

			public int OBSEL_Size { get; init; }
			public int OBSEL_NameSel { get; init; }
			public int OBSEL_NameBase { get; init; }

			public int OBJTable0Addr { get; init; }
			public int OBJTable1Addr { get; init; }

			public bool OBJ_MainEnabled { get; init; }
			public bool OBJ_SubEnabled { get; init; }
			public bool OBJ_MathEnabled { get; init; }
			public bool BK_MathEnabled { get; init; }

			public int M7HOFS { get; init; }
			public int M7VOFS { get; init; }
			public int M7A { get; init; }
			public int M7B { get; init; }
			public int M7C { get; init; }
			public int M7D { get; init; }
			public int M7X { get; init; }
			public int M7Y { get; init; }
			public int M7SEL_REPEAT { get; init; }
			public bool M7SEL_HFLIP { get; init; }
			public bool M7SEL_VFLIP { get; init; }
		}

		public static readonly int[,] ModeBpps = {
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
			int OBSEL_NameSel = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.OBSEL_NAMESEL);
			int OBSEL_NameBase = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.OBSEL_NAMEBASE);

			var si = new ScreenInfo
			{
				Mode = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG_MODE),
				Mode1_BG3_Priority = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG3_PRIORITY) == 1,
				OBSEL_Size = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.OBSEL_SIZE),
				OBSEL_NameSel = OBSEL_NameSel,
				OBSEL_NameBase = OBSEL_NameBase,
				OBJTable0Addr = OBSEL_NameBase << 14,
				OBJTable1Addr = ((OBSEL_NameBase << 14) + ((OBSEL_NameSel + 1) << 13)) & 0xFFFF,
				SETINI_Mode7ExtBG = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.SETINI_MODE7_EXTBG) == 1,
				SETINI_HiRes = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.SETINI_HIRES) == 1,
				SETINI_Overscan = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.SETINI_OVERSCAN) == 1,
				SETINI_ObjInterlace = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.SETINI_OBJ_INTERLACE) == 1,
				SETINI_ScreenInterlace = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.SETINI_SCREEN_INTERLACE) == 1,
				CGWSEL_ColorMask = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.CGWSEL_COLORMASK),
				CGWSEL_ColorSubMask = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.CGWSEL_COLORSUBMASK),
				CGWSEL_AddSubMode = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.CGWSEL_ADDSUBMODE),
				CGWSEL_DirectColor = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.CGWSEL_DIRECTCOLOR) == 1,
				CGADSUB_AddSub = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.CGADSUB_MODE),
				CGADSUB_Half = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.CGADSUB_HALF) == 1,
				OBJ_MainEnabled = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.TM_OBJ) == 1,
				OBJ_SubEnabled = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.TS_OBJ) == 1,
				OBJ_MathEnabled = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.CGADSUB_OBJ) == 1,
				BK_MathEnabled = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.CGADSUB_BACKDROP) == 1,
				M7HOFS = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.M7HOFS),
				M7VOFS = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.M7VOFS),
				M7A = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.M7A),
				M7B = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.M7B),
				M7C = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.M7C),
				M7D = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.M7D),
				M7X = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.M7X),
				M7Y = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.M7Y),
				M7SEL_REPEAT = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.M7SEL_REPEAT),
				M7SEL_HFLIP = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.M7SEL_HFLIP)!=0,
				M7SEL_VFLIP = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.M7SEL_VFLIP)!=0,
			};

			si.ObjSizeBounds = ObjSizes[si.OBSEL_Size,1];
			int square = Math.Max(si.ObjSizeBounds.Width, si.ObjSizeBounds.Height);
			si.ObjSizeBoundsSquare = new Size(square, square);

			si.BG.BG1.Bpp = ModeBpps[si.Mode, 0];
			si.BG.BG2.Bpp = ModeBpps[si.Mode, 1];
			si.BG.BG3.Bpp = ModeBpps[si.Mode, 2];
			si.BG.BG4.Bpp = ModeBpps[si.Mode, 3];

			//initial setting of mode type (derived from bpp table.. mode7 bg types will be fixed up later)
			for(int i=1;i<=4;i++)
				si.BG[i].BGMode = si.BG[i].Bpp == 0 ? BGMode.Unavailable : BGMode.Text;

			si.BG.BG1.TILESIZE = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG1_TILESIZE);
			si.BG.BG2.TILESIZE = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG2_TILESIZE);
			si.BG.BG3.TILESIZE = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG3_TILESIZE);
			si.BG.BG4.TILESIZE = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG4_TILESIZE);

			si.BG.BG1.SCSIZE = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG1_SCSIZE);
			si.BG.BG2.SCSIZE = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG2_SCSIZE);
			si.BG.BG3.SCSIZE = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG3_SCSIZE);
			si.BG.BG4.SCSIZE = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG4_SCSIZE);
			si.BG.BG1.SCADDR = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG1_SCADDR);
			si.BG.BG2.SCADDR = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG2_SCADDR);
			si.BG.BG3.SCADDR = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG3_SCADDR);
			si.BG.BG4.SCADDR = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG4_SCADDR);
			si.BG.BG1.TDADDR = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG1_TDADDR);
			si.BG.BG2.TDADDR = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG2_TDADDR);
			si.BG.BG3.TDADDR = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG3_TDADDR);
			si.BG.BG4.TDADDR = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG4_TDADDR);

			si.BG.BG1.MainEnabled = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.TM_BG1) == 1;
			si.BG.BG2.MainEnabled = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.TM_BG2) == 1;
			si.BG.BG3.MainEnabled = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.TM_BG3) == 1;
			si.BG.BG4.MainEnabled = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.TM_BG4) == 1;
			si.BG.BG1.SubEnabled = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.TS_BG1) == 1;
			si.BG.BG2.SubEnabled = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.TS_BG2) == 1;
			si.BG.BG3.SubEnabled = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.TS_BG3) == 1;
			si.BG.BG4.SubEnabled = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.TS_BG4) == 1;
			si.BG.BG1.MathEnabled = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.CGADSUB_BG1) == 1;
			si.BG.BG2.MathEnabled = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.CGADSUB_BG2) == 1;
			si.BG.BG3.MathEnabled = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.CGADSUB_BG3) == 1;
			si.BG.BG4.MathEnabled = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.CGADSUB_BG4) == 1;

			si.BG.BG1.HOFS = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG1HOFS);
			si.BG.BG1.VOFS = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG1VOFS);
			si.BG.BG2.HOFS = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG2HOFS);
			si.BG.BG2.VOFS = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG2VOFS);
			si.BG.BG3.HOFS = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG3HOFS);
			si.BG.BG3.VOFS = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG3VOFS);
			si.BG.BG4.HOFS = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG4HOFS);
			si.BG.BG4.VOFS = api.QUERY_peek_logical_register(LibsnesApi.SNES_REG.BG4VOFS);

			for (int i = 1; i <= 4; i++)
			{
				si.BG[i].Mode = si.Mode;
				si.BG[i].TiledataAddr = si.BG[i].TDADDR << 13;
				si.BG[i].ScreenAddr = si.BG[i].SCADDR << 9;
			}

			//fixup irregular things for mode 7
			if (si.Mode == 7)
			{
				si.BG.BG1.TiledataAddr = 0;
				si.BG.BG1.ScreenAddr = 0;

				if (si.CGWSEL_DirectColor)
				{
					si.BG.BG1.BGMode = BGMode.Mode7DC;
				}
				else
					si.BG.BG1.BGMode = BGMode.Mode7;

				if (si.SETINI_Mode7ExtBG)
				{
					si.BG.BG2.BGMode = BGMode.Mode7Ext;
					si.BG.BG2.Bpp = 7;
					si.BG.BG2.TiledataAddr = 0;
					si.BG.BG2.ScreenAddr = 0;
				}
			}

			//determine which colors each BG could use
			switch (si.Mode)
			{
				case 0:
					si.BG.BG1.PaletteSelection = new PaletteSelection(0, 32);
					si.BG.BG2.PaletteSelection = new PaletteSelection(32, 32);
					si.BG.BG3.PaletteSelection = new PaletteSelection(64, 32);
					si.BG.BG4.PaletteSelection = new PaletteSelection(96, 32);
					break;
				case 1:
					si.BG.BG1.PaletteSelection = new PaletteSelection(0, 128);
					si.BG.BG2.PaletteSelection = new PaletteSelection(0, 128);
					si.BG.BG3.PaletteSelection = new PaletteSelection(0, 32);
					si.BG.BG4.PaletteSelection = new PaletteSelection(0, 0);
					break;
				case 2:
					si.BG.BG1.PaletteSelection = new PaletteSelection(0, 128);
					si.BG.BG2.PaletteSelection = new PaletteSelection(0, 128);
					si.BG.BG3.PaletteSelection = new PaletteSelection(0, 0);
					si.BG.BG4.PaletteSelection = new PaletteSelection(0, 0);
					break;
				case 3:
					si.BG.BG1.PaletteSelection = new PaletteSelection(0, 256);
					si.BG.BG2.PaletteSelection = new PaletteSelection(0, 128);
					si.BG.BG3.PaletteSelection = new PaletteSelection(0, 0);
					si.BG.BG4.PaletteSelection = new PaletteSelection(0, 0);
					break;
				case 4:
					si.BG.BG1.PaletteSelection = new PaletteSelection(0, 256);
					si.BG.BG2.PaletteSelection = new PaletteSelection(0, 32);
					si.BG.BG3.PaletteSelection = new PaletteSelection(0, 0);
					si.BG.BG4.PaletteSelection = new PaletteSelection(0, 0);
					break;
				case 5:
					si.BG.BG1.PaletteSelection = new PaletteSelection(0, 128);
					si.BG.BG2.PaletteSelection = new PaletteSelection(0, 32);
					si.BG.BG3.PaletteSelection = new PaletteSelection(0, 0);
					si.BG.BG4.PaletteSelection = new PaletteSelection(0, 0);
					break;
				case 6:
					si.BG.BG1.PaletteSelection = new PaletteSelection(0, 128);
					si.BG.BG2.PaletteSelection = new PaletteSelection(0, 32);
					si.BG.BG3.PaletteSelection = new PaletteSelection(0, 0);
					si.BG.BG4.PaletteSelection = new PaletteSelection(0, 0);
					break;
				case 7:
					si.BG.BG1.PaletteSelection = new PaletteSelection(0, 256);
					si.BG.BG2.PaletteSelection = new PaletteSelection(0, 128);
					si.BG.BG3.PaletteSelection = new PaletteSelection(0, 0);
					si.BG.BG4.PaletteSelection = new PaletteSelection(0, 0);
					break;
			}

			return si;
		}

		//the same basic color table that libsnes uses to convert from snes 555 to rgba32
		private static readonly int[] directColorTable = new int[256]; //8bpp gfx -> rgb555
		static SNESGraphicsDecoder()
		{
			//make directColorTable
			for (int i = 0; i < 256; i++)
			{
				int r = i & 7;
				int g = (i >> 3) & 7;
				int b = (i >> 6) & 3;
				r <<= 2;
				g <<= 2;
				b <<= 3;
				int color = (b << 10) | (g << 5) | r;
				directColorTable[i] = color;
			}
		}

		private readonly int[] colortable;
		public byte* vram, oam;
		public ushort* cgram, vram16;

		private readonly LibsnesApi api;

		public SNESGraphicsDecoder(LibsnesApi api, SnesColors.ColorType pal)
		{
			this.api = api;
			colortable = SnesColors.GetLUT(pal);
			IntPtr block = (IntPtr)api.QUERY_get_memory_data(LibsnesApi.SNES_MEMORY.VRAM);
			vram = (byte*)block;
			vram16 = (ushort*)block;
			block = (IntPtr)api.QUERY_get_memory_data(LibsnesApi.SNES_MEMORY.CGRAM);
			cgram = (ushort*)block;
			block = (IntPtr)api.QUERY_get_memory_data(LibsnesApi.SNES_MEMORY.OAM);
			oam = (byte*)block;
		}

		public void Dispose()
		{
			//todo - unhook from api?
		}

		public struct TileEntry
		{
			public ushort tilenum;
			public byte palette;
			public TileEntryFlags flags;
			public int address;
		}

		[Flags]
		public enum TileEntryFlags : byte
		{
			None = 0, Priority = 1, Horz = 2, Vert = 4,
		}

		public ISNESGraphicsDecoder.OAMInfo CreateOAMInfo(ScreenInfo si, int num)
			=> new OAMInfo(this, si, num);

		/// <summary>
		/// decodes a mode7 BG. youll still need to paletteize and colorize it.
		/// </summary>
		public void DecodeMode7BG(int* screen, int stride, bool extBg)
		{
			int[] tileCache = _tileCache[extBg?17:7];
			for (int ty = 0, tidx = 0; ty < 128; ty++)
			{
				for (int tx = 0; tx < 128; tx++, tidx++)
				{
					int tileEntry = vram[tidx * 2];
					int src = tileEntry * 64;
					for (int py = 0, pix=src; py < 8; py++)
					{
						for (int px = 0; px < 8; px++, pix++)
						{
							int dst = (ty * 8 + py) * stride + (tx * 8 + px);
							int srcData = tileCache[pix];
							screen[dst] = srcData;
						}
					}
				}
			}
		}

//		/// <summary>
//		/// returns a tilemap which might be resized into 8x8 physical tiles if the 16x16 logical tilesize is specified
//		/// </summary>
//		TileEntry[] AdaptTilemap(TileEntry[] map8x8, int tilesWide, int tilesTall, int tilesize)
//		{
//			if (tilesize == 8) return map8x8;
//			int numTiles = tilesWide * tilesTall;
//			var ret = new TileEntry[numTiles * 4];
//			for(int y=0;y<tilesTall;y++)
//			{
//				for (int x = 0; x < tilesWide; x++)
//				{
//					int si = tilesWide * y + x;
//					int di = tilesHigh
//					for (int tx = 0; tx < 2; tx++)
//					{
//						for (int ty = 0; ty < 2; ty++)
//						{
//						}
//					}
//				}
//			}
//		}

		/// <summary>
		/// decodes a BG. youll still need to paletteize and colorize it.
		/// someone else has to take care of calculating the starting color from the mode and layer number.
		/// </summary>
		public void DecodeBG(int* screen, int stride, TileEntry[] map, int tiledataBaseAddr, ScreenSize size, int bpp, int tilesize, int paletteStart)
		{
			//emergency backstop. this can only happen if we're displaying an unavailable BG or other similar such value
			if (bpp == 0) return;

			int ncolors = 1 << bpp;

			int[] tileBuf = new int[16*16];
			var dims = SizeInTilesForBGSize(size);
			int count8x8 = tilesize / 8;
			int tileSizeBytes = 8 * bpp;
			int baseTileNum = tiledataBaseAddr / tileSizeBytes;
			int[] tileCache = _tileCache[bpp];
			int tileCacheMask = tileCache.Length - 1;

			int screenWidth = dims.Width * count8x8 * 8;

			for (int mty = 0; mty < dims.Height; mty++)
			{
				for (int mtx = 0; mtx < dims.Width; mtx++)
				{
					for (int tx = 0; tx < count8x8; tx++)
					{
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
							for (int i = 0, y = 0; y < 8; y++)
							{
								for (int x = 0; x < 8; x++, i++)
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
									if (color == 0 && usingUserBackColor)
									{ }
									else
									{
										color += te.palette * ncolors;
										color += paletteStart;
									}
									screen[dstOfs] = color;
								}
							}
						}
					}
				}
			}
		}

		public TileEntry[] FetchMode7Tilemap()
		{
			TileEntry[] buf = new TileEntry[128*128];
			for (int ty = 0, tidx = 0; ty < 128; ty++)
			{
				for (int tx = 0; tx < 128; tx++, tidx++)
				{
					int tileEntry = vram[tidx * 2];
					buf[tidx].address = tidx * 2;
					buf[tidx].tilenum = (ushort)tileEntry;
					//palette and flags are ok defaulting to 0
				}
			}

			return buf;
		}

		/// <summary>
		/// fetches a tilemap. this is simple; apparently only the screen size (shape) is a factor (not the tile size)
		/// </summary>
		public TileEntry[] FetchTilemap(int addr, ScreenSize size)
		{
			var blockDims = SizeInBlocksForBGSize(size);
			int blocksw = blockDims.Width;
			int blocksh = blockDims.Height;
			int width = blockDims.Width * 32;
			int height = blockDims.Height * 32;
			TileEntry[] buf = new TileEntry[width*height];

			for (int by = 0; by < blocksh; by++)
			{
				for (int bx = 0; bx < blocksw; bx++)
				{
					for (int y = 0; y < 32; y++)
					{
						for (int x = 0; x < 32; x++)
						{
							int idx = (by * 32 + y) * width + bx * 32 + x;
							ushort entry = *(ushort*)(vram + addr);
							buf[idx].tilenum = (ushort)(entry & 0x3FF);
							buf[idx].palette = (byte)((entry >> 10) & 7);
							buf[idx].flags = (TileEntryFlags)((entry >> 13) & 7);
							buf[idx].address = addr;
							addr += 2;
						}
					}
				}
			}

			return buf;
		}

		//TODO - paletteize and colorize could be in one step, for more speed
		public void Paletteize(int* buf, int offset, int startcolor, int numpixels)
		{
			for (int i = 0; i < numpixels; i++)
			{
				int entry = buf[offset + i];
				int color;
				if (entry == 0 && usingUserBackColor)
					color = userBackColor;
				else color = cgram[startcolor + entry] & 0x7FFF; //unfortunate that we have to mask this here.. maybe do it in a more optimal spot when we port it to c++

				buf[offset + i] = color;
			}
		}
		public void Colorize(int* buf, int offset, int numpixels)
		{
			for (int i = 0; i < numpixels; i++)
			{
				buf[offset + i] = colortable[491520 + buf[offset + i]];
			}
		}

		private readonly int[][] _tileCache = new int[18][];

		private bool usingUserBackColor;
		private int userBackColor;

		public void SetBackColor(int snescol)
		{
			if (snescol == -1)
			{
				usingUserBackColor = false;
			}
			else
			{
				usingUserBackColor = true;
				userBackColor = snescol;
			}
		}

		/// <summary>
		/// Caches all tiles at the 2bpp, 4bpp, and 8bpp decoded states.
		/// we COULD defer this til we need it, you know. sort of a cool idea, not too hard
		/// </summary>
		public void CacheTiles()
		{
			//generate 2bpp tiles
			int numtiles = 65536/8/2;
			int[] tiles = new int[8 * 8 * numtiles];
			_tileCache[2] = tiles;
			for (int i = 0; i < numtiles; i++)
			{
				Decode8x8x2bpp(tiles, i * 64, 16 * i, 8);
			}

			//merge 2bpp tiles into 4bpp and 8bpp
			CacheTiles_Merge(2);
			CacheTiles_Merge(4);
			CacheTilesMode7();
			CacheTilesMode7ExtBg();
		}

		public void CacheTilesMode7()
		{
			int numtiles = 256;
			int[] tiles = new int[8 * 8 * numtiles];
			_tileCache[7] = tiles;
			for (int i = 0, j=0; i < numtiles; i++)
			{
				for (int y = 0; y < 8; y++)
					for (int x = 0; x < 8; x++, j++)
						tiles[j] = vram[j * 2 + 1];
			}
		}

		//not being used.. do we need it?
		public int[] GetCachedTile(int bpp, int tilenum)
		{
			int[] ret = new int[8 * 8];
			int idx = tilenum * 64;
			for (int i = 0; i < 64; i++)
				ret[i] = _tileCache[bpp][idx + i];
			return ret;
		}

		private void CacheTilesMode7ExtBg()
		{
			int numtiles = 256;
			int[] tiles = new int[8 * 8 * numtiles];
			_tileCache[17] = tiles;
			int[] mode7tiles = _tileCache[7];
			int numPixels = numtiles*8*8;
			for (int i = 0; i < numPixels; i++)
				tiles[i] = mode7tiles[i] & 0x7F;
		}


		/// <summary>
		/// merges one type of tiles with another to create the higher-order bitdepth.
		/// TODO - templateize this when we change it to c++
		/// </summary>
		private void CacheTiles_Merge(int fromBpp)
		{
			int toBpp = fromBpp * 2;
			int shift = fromBpp;
			int numtiles = 8192 / toBpp;
			int[] tilesDst = new int[8 * 8 * numtiles];
			_tileCache[toBpp] = tilesDst;
			int[] tilesSrc = _tileCache[fromBpp];

			for (int i = 0; i < numtiles; i++)
			{
				int srcAddr = i * 128;
				int dstAddr = i * 64;
				for (int p = 0; p < 64; p++)
				{
					int tileA = tilesSrc[srcAddr + p];
					int tileB = tilesSrc[srcAddr + p + 64];
					tilesDst[dstAddr + p] = tileA | (tileB << shift);
				}
			}
		}

		/// <summary>
		/// decodes an 8x8 tile to a linear framebuffer type thing. fundamental unit of tile decoding.
		/// </summary>
		public void Decode8x8x2bpp(int[] buf, int offset, int addr, int stride=8)
		{
			for (int y = 0; y < 8; y++)
			{
				byte val = vram[addr + 1];
				for (int x = 0; x < 8; x++) buf[offset + y * stride + x] = val >> (7 - x) & 1;
				val = vram[addr + 0];
				for (int x = 0; x < 8; x++) buf[offset + y * stride + x] = (buf[offset + y * stride + x] << 1) | (val >> (7 - x) & 1);
				addr += 2;
			}
		}

		/// <summary>
		/// renders the mode7 tiles to a screen with the predefined size.
		/// </summary>
		public void RenderMode7TilesToScreen(int* screen, int stride, bool ext, bool directColor, int tilesWide = 16, int startTile = 0, int numTiles = 256)
		{
			int[] tilebuf = _tileCache[ext?17:7];
			for (int i = 0; i < numTiles; i++)
			{
				int tnum = startTile + i;
				//TODO - mask by possible number of tiles? only in OBJ rendering mode?

				int ty = i / tilesWide;
				int tx = i % tilesWide;
				int dstOfs = (ty * 8) * stride + tx * 8;
				int srcOfs = tnum * 64;
				for (int y = 0, p = 0; y < 8; y++)
				{
					for (int x = 0; x < 8; x++, p++)
					{
						screen[dstOfs + y * stride + x] = tilebuf[srcOfs + p];
					}
				}
			}

			int numPixels = numTiles * 8 * 8;
			if (directColor) DirectColorify(screen, numPixels);
			else Paletteize(screen, 0, 0, numPixels);
			Colorize(screen, 0, numPixels);
		}


		/// <summary>
		/// renders the tiles to a screen of the crudely specified size.
		/// we might need 16x16 unscrambling and some other perks here eventually.
		/// provide a start color to use as the basis for the palette
		/// </summary>
		public void RenderTilesToScreen(int* screen, int stride, int bpp, int startcolor, int startTile = 0, int numTiles = -1)
		{
			if (numTiles == -1)
				numTiles = 8192 / bpp;
			int[] tilebuf = _tileCache[bpp];
			int tilesWide = stride / 8;
			for (int i = 0; i < numTiles; i++)
			{
				int tnum = startTile + i;
				//TODO - mask by possible number of tiles? only in OBJ rendering mode?
				int ty = i / tilesWide;
				int tx = i % tilesWide;
				int dstOfs = (ty * 8) * stride + tx * 8;
				int srcOfs = tnum * 64;
				for (int y = 0, p = 0; y < 8; y++)
					for (int x = 0; x < 8; x++, p++)
					{
						screen[dstOfs + y * stride + x] = tilebuf[srcOfs + p];
					}
			}

			int numPixels = numTiles * 8 * 8;
			Paletteize(screen, 0, startcolor, numPixels);
			Colorize(screen, 0, numPixels);
		}


		public void RenderSpriteToScreen(int* screen, int stride, int destx, int desty, ScreenInfo si, int spritenum, ISNESGraphicsDecoder.OAMInfo oam = null, int xlimit = 1024, int ylimit = 1024, byte[,] spriteMap = null)
		{
			oam ??= new OAMInfo(this, si, spritenum);
			var dim = ObjSizes[si.OBSEL_Size, oam.Size ? 1 : 0];

			int[] tilebuf = _tileCache[4];

			int baseaddr = oam.Table ? si.OBJTable1Addr : si.OBJTable0Addr;

			//TODO - flips of 'undocumented' rectangular oam settings are wrong. probably easy to do right, but we need a test

			int bcol = oam.Tile & 0xF;
			int brow = (oam.Tile >> 4) & 0xF;
			for(int oy=0;oy<dim.Height;oy++)
				for (int ox = 0; ox < dim.Width; ox++)
				{
					int x = ox;
					int y = oy;

					int dy, dx;

					if (oam.HFlip)
						dx = dim.Width - 1 - x;
					else dx = x;
					if (oam.VFlip)
						dy = dim.Height - 1 - y;
					else dy = y;

					dx += destx;
					dy += desty;

					if(dx>=xlimit || dy>=ylimit || dx<0 || dy<0)
						continue;

					int col = (bcol + (x >> 3)) & 0xF;
					int row = (brow + (y >> 3)) & 0xF;
					int sx = x & 0x7;
					int sy = y & 0x7;

					int addr = baseaddr*2 + (row * 16 + col) * 64;
					addr += sy * 8 + sx;

					int dofs = stride*dy+dx;
					int color = tilebuf[addr];
					if (spriteMap != null && color == 0)
					{
						//skip transparent pixels
					}
					else
					{
						screen[dofs] = color;
						Paletteize(screen, dofs, oam.Palette * 16 + 128, 1);
						Colorize(screen, dofs, 1);
						if (spriteMap != null) spriteMap[dx, dy] = (byte)spritenum;
					}
				}
		}

		public int Colorize(int rgb555)
		{
			//skip to max luminance in the palette table
			return colortable[491520 + rgb555];
		}

		/// <summary>
		/// returns the current palette, transformed into an int array, for more convenience
		/// </summary>
		public int[] GetPalette()
		{
			var ret = new int[256];
			for (int i = 0; i < 256; i++)
				ret[i] = cgram[i] & 0x7FFF;
			return ret;
		}

		public void DirectColorify(int* screen, int numPixels)
		{
			for (int i = 0; i < numPixels; i++)
				screen[i] = directColorTable[screen[i]];

		}

		public void Enter()
		{
			((IMonitor)api).Enter();
		}

		public void Exit()
		{
			((IMonitor)api).Exit();
		}
	} //class SNESGraphicsDecoder
} //namespace
