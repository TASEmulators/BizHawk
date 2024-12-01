using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[SpecializedTool("GPU Viewer")]
	public partial class GbaGpuView : ToolFormBase, IToolFormAutoConfig
	{
		public static Icon ToolIcon
			=> Properties.Resources.GbaIcon.Value;

		[RequiredService]
		public IGBAGPUViewable/*?*/ _gbaCore { get; set; }

		private IGBAGPUViewable GBA
			=> _gbaCore!;

		// emulator memory areas
		private IntPtr _vram;
		private IntPtr _oam;
		private IntPtr _mmio;
		private IntPtr _palRam;
		// color conversion to RGB888
		private readonly int[] _colorConversion;

		private MobileBmpView _bg0, _bg1, _bg2, _bg3, _bgPal, _spPal, _sprites, _bgTiles16, _bgTiles256, _spTiles16, _spTiles256;

		// MobileDetailView memory;

		protected override string WindowTitleStatic => "GBA GPU Viewer";

		public GbaGpuView()
		{
			InitializeComponent();
			Icon = ToolIcon;
			// TODO: hook up something
			// we do this twice to avoid having to & 0x7fff with every color
			int[] tmp = GBColors.GetLut(GBColors.ColorType.vivid);
			_colorConversion = new int[65536];
			Buffer.BlockCopy(tmp, 0, _colorConversion, 0, sizeof(int) * tmp.Length);
			Buffer.BlockCopy(tmp, 0, _colorConversion, sizeof(int) * tmp.Length, sizeof(int) * tmp.Length);
			radioButtonManual.Checked = true;
			GenerateWidgets();
			hScrollBar1_ValueChanged(null, null);
			RecomputeRefresh();
		}

		private unsafe void DrawTile256(int* dest, int pitch, byte* tile, ushort* palette, bool hFlip, bool vFlip)
		{
			if (vFlip)
			{
				dest += pitch * 7;
				pitch = -pitch;
			}

			if (hFlip)
			{
				dest += 7;
				for (int y = 0; y < 8; y++)
				{
					for (int x = 0; x < 8; x++)
					{
						*dest-- = _colorConversion[palette[*tile++]];
					}
					dest += 8;
					dest += pitch;
				}
			}
			else
			{
				for (int y = 0; y < 8; y++)
				{
					for (int x = 0; x < 8; x++)
					{
						*dest++ = _colorConversion[palette[*tile++]];
					}
					dest -= 8;
					dest += pitch;
				}
			}
		}

		private unsafe void DrawTile16(int* dest, int pitch, byte* tile, ushort* palette, bool hFlip, bool vFlip)
		{
			if (vFlip)
			{
				dest += pitch * 7;
				pitch = -pitch;
			}
			if (hFlip)
			{
				dest += 7;
				for (int y = 0; y < 8; y++)
				{
					for (int i = 0; i < 4; i++)
					{
						*dest-- = _colorConversion[palette[*tile & 15]];
						*dest-- = _colorConversion[palette[*tile >> 4]];
						tile++;
					}
					dest += 8;
					dest += pitch;
				}
			}
			else
			{
				for (int y = 0; y < 8; y++)
				{
					for (int i = 0; i < 4; i++)
					{
						*dest++ = _colorConversion[palette[*tile & 15]];
						*dest++ = _colorConversion[palette[*tile >> 4]];
						tile++;
					}
					dest -= 8;
					dest += pitch;
				}
			}
		}

		private unsafe void DrawTextNameTable16(int* dest, int pitch, ushort* nametable, byte* tiles)
		{
			for (int ty = 0; ty < 32; ty++)
			{
				for (int tx = 0; tx < 32; tx++)
				{
					ushort ntent = *nametable++;
					DrawTile16(dest, pitch, tiles + (ntent & 1023) * 32, (ushort*)_palRam + (ntent >> 12 << 4), ntent.Bit(10), ntent.Bit(11));
					dest += 8;
				}
				dest -= 256;
				dest += 8 * pitch;
			}
		}

		private unsafe void DrawTextNameTable256(int* dest, int pitch, ushort* nametable, byte* tiles)
		{
			for (int ty = 0; ty < 32; ty++)
			{
				for (int tx = 0; tx < 32; tx++)
				{
					ushort ntent = *nametable++;
					DrawTile256(dest, pitch, tiles + (ntent & 1023) * 64, (ushort*)_palRam, ntent.Bit(10), ntent.Bit(11));
					dest += 8;
				}
				dest -= 256;
				dest += 8 * pitch;
			}
		}

		private unsafe void DrawTextNameTable(int* dest, int pitch, ushort* nametable, byte* tiles, bool eightbit)
		{
			if (eightbit)
				DrawTextNameTable256(dest, pitch, nametable, tiles);
			else
				DrawTextNameTable16(dest, pitch, nametable, tiles);
		}

		private unsafe void DrawTextBG(int n, MobileBmpView mbv)
		{
			ushort bgcnt = ((ushort*)_mmio)[4 + n];
			int ssize = bgcnt >> 14;
			switch (ssize)
			{
				case 0: mbv.ChangeAllSizes(256, 256); break;
				case 1: mbv.ChangeAllSizes(512, 256); break;
				case 2: mbv.ChangeAllSizes(256, 512); break;
				case 3: mbv.ChangeAllSizes(512, 512); break;
			}
			Bitmap bmp = mbv.BmpView.Bmp;
			var lockData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			int* pixels = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);

			byte* tiles = (byte*)_vram + ((bgcnt & 0xc) << 12);

			ushort* nametable = (ushort*)_vram + ((bgcnt & 0x1f00) << 2);

			bool eighthBit = bgcnt.Bit(7);

			switch (ssize)
			{
				case 0:
					DrawTextNameTable(pixels, pitch, nametable, tiles, eighthBit);
					break;
				case 1:
					DrawTextNameTable(pixels, pitch, nametable, tiles, eighthBit);
					pixels += 256;
					nametable += 1024;
					DrawTextNameTable(pixels, pitch, nametable, tiles, eighthBit);
					break;
				case 2:
					DrawTextNameTable(pixels, pitch, nametable, tiles, eighthBit);
					pixels += pitch * 256;
					nametable += 1024;
					DrawTextNameTable(pixels, pitch, nametable, tiles, eighthBit);
					break;
				case 3:
					DrawTextNameTable(pixels, pitch, nametable, tiles, eighthBit);
					pixels += 256;
					nametable += 1024;
					DrawTextNameTable(pixels, pitch, nametable, tiles, eighthBit);
					pixels -= 256;
					pixels += pitch * 256;
					nametable += 1024;
					DrawTextNameTable(pixels, pitch, nametable, tiles, eighthBit);
					pixels += 256;
					nametable += 1024;
					DrawTextNameTable(pixels, pitch, nametable, tiles, eighthBit);
					break;
			}

			bmp.UnlockBits(lockData);
			mbv.BmpView.Refresh();
		}

		private unsafe void DrawAffineBG(int n, MobileBmpView mbv)
		{
			ushort bgcnt = ((ushort*)_mmio)[4 + n];
			int ssize = bgcnt >> 14;
			switch (ssize)
			{
				case 0: mbv.ChangeAllSizes(128, 128); break;
				case 1: mbv.ChangeAllSizes(256, 256); break;
				case 2: mbv.ChangeAllSizes(512, 512); break;
				case 3: mbv.ChangeAllSizes(1024, 1024); break;
			}
			Bitmap bmp = mbv.BmpView.Bmp;
			var lockData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			int* pixels = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);

			byte* tiles = (byte*)_vram + ((bgcnt & 0xc) << 12);

			byte* nametable = (byte*)_vram + ((bgcnt & 0x1f00) << 3);

			for (int ty = 0; ty < bmp.Height / 8; ty++)
			{
				for (int tx = 0; tx < bmp.Width / 8; tx++)
				{
					DrawTile256(pixels, pitch, tiles + *nametable++ * 64, (ushort*)_palRam, false, false);
					pixels += 8;
				}
				pixels -= bmp.Width;
				pixels += 8 * pitch;
			}

			bmp.UnlockBits(lockData);
			mbv.BmpView.Refresh();
		}

		private unsafe void DrawM3BG(MobileBmpView mbv)
		{
			mbv.ChangeAllSizes(240, 160);
			Bitmap bmp = mbv.BmpView.Bmp;
			var lockData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			int* pixels = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);

			ushort* frame = (ushort*)_vram;

			for (int y = 0; y < 160; y++)
			{
				for (int x = 0; x < 240; x++)
				{
					*pixels++ = _colorConversion[*frame++];
				}

				pixels -= 240;
				pixels += pitch;
			}

			bmp.UnlockBits(lockData);
			mbv.BmpView.Refresh();
		}

		private unsafe void DrawM4BG(MobileBmpView mbv, bool secondFrame)
		{
			mbv.ChangeAllSizes(240, 160);
			Bitmap bmp = mbv.BmpView.Bmp;
			var lockData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			int* pixels = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);

			byte* frame = (byte*)_vram + (secondFrame ? 40960 : 0);
			ushort* palette = (ushort*)_palRam;

			for (int y = 0; y < 160; y++)
			{
				for (int x = 0; x < 240; x++)
				{
					*pixels++ = _colorConversion[palette[*frame++]];
				}

				pixels -= 240;
				pixels += pitch;
			}

			bmp.UnlockBits(lockData);
			mbv.BmpView.Refresh();
		}

		private unsafe void DrawM5BG(MobileBmpView mbv, bool secondFrame)
		{
			mbv.ChangeAllSizes(160, 128);
			Bitmap bmp = mbv.BmpView.Bmp;
			var lockData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			int* pixels = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);

			ushort* frame = (ushort*)_vram + (secondFrame ? 20480 : 0);

			for (int y = 0; y < 128; y++)
			{
				for (int x = 0; x < 160; x++)
					*pixels++ = _colorConversion[*frame++];
				pixels -= 160;
				pixels += pitch;
			}

			bmp.UnlockBits(lockData);
			mbv.BmpView.Refresh();
		}

		private static readonly int[, ,] SpriteSizes = { { { 1, 1 }, { 2, 2 }, { 4, 4 }, { 8, 8 } }, { { 2, 1 }, { 4, 1 }, { 4, 2 }, { 8, 4 } }, { { 1, 2 }, { 1, 4 }, { 2, 4 }, { 4, 8 } } };

		private unsafe void DrawSprite(int* dest, int pitch, ushort* sprite, byte* tiles, bool twodee)
		{
			ushort attr0 = sprite[0];
			ushort attr1 = sprite[1];
			ushort attr2 = sprite[2];

			if (!attr0.Bit(8) && attr0.Bit(9))
				return; // 2x with affine off

			int shape = attr0 >> 14;
			if (shape == 3)
				return;
			int size = attr1 >> 14;
			int tw = SpriteSizes[shape, size, 0];
			int th = SpriteSizes[shape, size, 1];

			bool eighthBit = attr0.Bit(13);
			bool hFlip = attr1.Bit(12);
			bool vFlip = attr1.Bit(13);

			ushort* palette = (ushort*)_palRam + 256;
			if (!eighthBit)
				palette += attr2 >> 12 << 4;

			int tileIndex = eighthBit ? attr2 & 1022 : attr2 & 1023;
			int tileStride = twodee ? 1024 - tw * (eighthBit ? 64 : 32) : 0;

			// see if the sprite would read past the end of vram, and skip it if it would
			{
				int tileEnd;

				if (!twodee)
					tileEnd = tileIndex + tw * th * (eighthBit ? 2 : 1);
				else
					tileEnd = tileIndex + tw * (eighthBit ? 2 : 1) + (th - 1) * 32;

				if (tileEnd > 1024)
					return;
			}

			tiles += 32 * tileIndex;

			if (vFlip)
				dest += pitch * 8 * (th - 1);
			if (hFlip)
				dest += 8 * (tw - 1);
			for (int ty = 0; ty < th; ty++)
			{
				for (int tx = 0; tx < tw; tx++)
				{
					if (eighthBit)
					{
						DrawTile256(dest, pitch, tiles, palette, hFlip, vFlip);
						tiles += 64;
					}
					else
					{
						DrawTile16(dest, pitch, tiles, palette, hFlip, vFlip);
						tiles += 32;
					}
					if (hFlip)
						dest -= 8;
					else
						dest += 8;
				}
				if (hFlip)
					dest += tw * 8;
				else
					dest -= tw * 8;
				if (vFlip)
					dest -= pitch * 8;
				else
					dest += pitch * 8;
				tiles += tileStride;
			}
		}

		private unsafe void DrawSprites(MobileBmpView mbv)
		{
			mbv.BmpView.ChangeBitmapSize(1024, 512);
			Bitmap bmp = mbv.BmpView.Bmp;
			var lockData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			BmpView.Clear_Selected_Region((byte*)lockData.Scan0, (uint)(lockData.Height * lockData.Stride));

			int* pixels = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);

			ushort* sprites = (ushort*)_oam;
			byte* tiles = (byte*)_vram + 65536;

			ushort dispcnt = ((ushort*)_mmio)[0];
			bool twodee = !dispcnt.Bit(6);

			for (int sy = 0; sy < 8; sy++)
			{
				for (int sx = 0; sx < 16; sx++)
				{
					DrawSprite(pixels, pitch, sprites, tiles, twodee);
					pixels += 64;
					sprites += 4;
				}
				pixels -= 1024;
				pixels += pitch * 64;
			}

			bmp.UnlockBits(lockData);
			mbv.BmpView.Refresh();
		}

		private unsafe void DrawPalette(MobileBmpView mbv, bool sprite)
		{
			mbv.BmpView.ChangeBitmapSize(16, 16);
			Bitmap bmp = mbv.BmpView.Bmp;
			var lockData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			int* pixels = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);

			ushort* palette = (ushort*)_palRam + (sprite ? 256 : 0);

			for (int j = 0; j < 16; j++)
			{
				for (int i = 0; i < 16; i++)
					*pixels++ = _colorConversion[*palette++];
				pixels -= 16;
				pixels += pitch;
			}

			bmp.UnlockBits(lockData);
			mbv.BmpView.Refresh();
		}

		private unsafe void DrawTileRange(int* dest, int pitch, byte* tiles, ushort* palette, int tw, int th, bool eightbit)
		{
			for (int ty = 0; ty < th; ty++)
			{
				for (int tx = 0; tx < tw; tx++)
				{
					if (eightbit)
					{
						DrawTile256(dest, pitch, tiles, palette, false, false);
						dest += 8;
						tiles += 64;
					}
					else
					{
						DrawTile16(dest, pitch, tiles, palette, false, false);
						dest += 8;
						tiles += 32;
					}
				}
				dest -= tw * 8;
				dest += pitch * 8;
			}
		}

		private unsafe void DrawSpriteTiles(MobileBmpView mbv, bool tophalfonly, bool eightbit)
		{
			int tw = eightbit ? 16 : 32;
			int th = tophalfonly ? 16 : 32;

			mbv.BmpView.ChangeBitmapSize(tw * 8, 256);
			Bitmap bmp = mbv.BmpView.Bmp;
			var lockData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			int* pixels = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);

			byte* tiles = (byte*)_vram + 65536;
			// TODO: palette changing (in 4 bit mode anyway)
			ushort* palette = (ushort*)_palRam + 256;

			if (tophalfonly)
			{
				BmpView.Clear_Selected_Region((byte*)lockData.Scan0, (uint)(128 * lockData.Stride));

				pixels += 128 * pitch;
				tiles += 16384;
			}
			DrawTileRange(pixels, pitch, tiles, palette, tw, th, eightbit);
			bmp.UnlockBits(lockData);
			mbv.BmpView.Refresh();
		}

		private unsafe void DrawBGTiles(MobileBmpView mbv, bool eightbit)
		{
			int tw = eightbit ? 32 : 64;
			int th = 32;

			mbv.BmpView.ChangeBitmapSize(tw * 8, th * 8);
			Bitmap bmp = mbv.BmpView.Bmp;
			var lockData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			int* pixels = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);

			byte* tiles = (byte*)_vram;
			// TODO: palette changing (in 4 bit mode anyway)
			ushort* palette = (ushort*)_palRam;
			DrawTileRange(pixels, pitch, tiles, palette, tw, th, eightbit);
			bmp.UnlockBits(lockData);
			mbv.BmpView.Refresh();
		}


		private unsafe void DrawEverything()
		{
			ushort dispcnt = ((ushort*)_mmio)[0];

			int bgMode = dispcnt & 7;
			switch (bgMode)
			{
				case 0:
					if (_bg0.ShouldDraw) DrawTextBG(0, _bg0);
					if (_bg1.ShouldDraw) DrawTextBG(1, _bg1);
					if (_bg2.ShouldDraw) DrawTextBG(2, _bg2);
					if (_bg3.ShouldDraw) DrawTextBG(3, _bg3);
					if (_bgTiles16.ShouldDraw) DrawBGTiles(_bgTiles16, false);
					if (_bgTiles256.ShouldDraw) DrawBGTiles(_bgTiles256, true);
					if (_spTiles16.ShouldDraw) DrawSpriteTiles(_spTiles16, false, false);
					if (_spTiles256.ShouldDraw) DrawSpriteTiles(_spTiles256, false, true);
					break;
				case 1:
					if (_bg0.ShouldDraw) DrawTextBG(0, _bg0);
					if (_bg1.ShouldDraw) DrawTextBG(1, _bg1);
					if (_bg2.ShouldDraw) DrawAffineBG(2, _bg2);
					if (_bg3.ShouldDraw) _bg3.BmpView.Clear();
					if (_bgTiles16.ShouldDraw) DrawBGTiles(_bgTiles16, false);
					if (_bgTiles256.ShouldDraw) DrawBGTiles(_bgTiles256, true);
					if (_spTiles16.ShouldDraw) DrawSpriteTiles(_spTiles16, false, false);
					if (_spTiles256.ShouldDraw) DrawSpriteTiles(_spTiles256, false, true);
					break;
				case 2:
					if (_bg0.ShouldDraw) _bg0.BmpView.Clear();
					if (_bg1.ShouldDraw) _bg1.BmpView.Clear();
					if (_bg2.ShouldDraw) DrawAffineBG(2, _bg2);
					if (_bg3.ShouldDraw) DrawAffineBG(3, _bg3);
					// while there are no 4bpp tiles possible in mode 2, there might be some in memory
					// due to midframe mode switching.  no real reason not to display them if that's
					// what the user wants to see
					if (_bgTiles16.ShouldDraw) DrawBGTiles(_bgTiles16, false);
					if (_bgTiles256.ShouldDraw) DrawBGTiles(_bgTiles256, true);
					if (_spTiles16.ShouldDraw) DrawSpriteTiles(_spTiles16, false, false);
					if (_spTiles256.ShouldDraw) DrawSpriteTiles(_spTiles256, false, true);
					break;
				case 3:
					if (_bg0.ShouldDraw) _bg0.BmpView.Clear();
					if (_bg1.ShouldDraw) _bg1.BmpView.Clear();
					if (_bg2.ShouldDraw) DrawM3BG(_bg2);
					if (_bg3.ShouldDraw) _bg3.BmpView.Clear();
					if (_bgTiles16.ShouldDraw) _bgTiles16.BmpView.Clear();
					if (_bgTiles256.ShouldDraw) _bgTiles256.BmpView.Clear();
					if (_spTiles16.ShouldDraw) DrawSpriteTiles(_spTiles16, true, false);
					if (_spTiles256.ShouldDraw) DrawSpriteTiles(_spTiles256, true, true);
					break;
				//in modes 4, 5, bg3 is repurposed as bg2 invisible frame
				case 4:
					if (_bg0.ShouldDraw) _bg0.BmpView.Clear();
					if (_bg1.ShouldDraw) _bg1.BmpView.Clear();
					if (_bg2.ShouldDraw) DrawM4BG(_bg2, dispcnt.Bit(4));
					if (_bg3.ShouldDraw) DrawM4BG(_bg3, !dispcnt.Bit(4));
					if (_bgTiles16.ShouldDraw) _bgTiles16.BmpView.Clear();
					if (_bgTiles256.ShouldDraw) _bgTiles256.BmpView.Clear();
					if (_spTiles16.ShouldDraw) DrawSpriteTiles(_spTiles16, true, false);
					if (_spTiles256.ShouldDraw) DrawSpriteTiles(_spTiles256, true, true);
					break;
				case 5:
					if (_bg0.ShouldDraw) _bg0.BmpView.Clear();
					if (_bg1.ShouldDraw) _bg1.BmpView.Clear();
					if (_bg2.ShouldDraw) DrawM5BG(_bg2, dispcnt.Bit(4));
					if (_bg3.ShouldDraw) DrawM5BG(_bg3, !dispcnt.Bit(4));
					if (_bgTiles16.ShouldDraw) _bgTiles16.BmpView.Clear();
					if (_bgTiles256.ShouldDraw) _bgTiles256.BmpView.Clear();
					if (_spTiles16.ShouldDraw) DrawSpriteTiles(_spTiles16, true, false);
					if (_spTiles256.ShouldDraw) DrawSpriteTiles(_spTiles256, true, true);
					break;
				default:
					// shouldn't happen, but shouldn't be our problem either
					if (_bg0.ShouldDraw) _bg0.BmpView.Clear();
					if (_bg1.ShouldDraw) _bg1.BmpView.Clear();
					if (_bg2.ShouldDraw) _bg2.BmpView.Clear();
					if (_bg3.ShouldDraw) _bg3.BmpView.Clear();
					if (_bgTiles16.ShouldDraw) _bgTiles16.BmpView.Clear();
					if (_bgTiles256.ShouldDraw) _bgTiles256.BmpView.Clear();
					if (_spTiles16.ShouldDraw) _spTiles16.BmpView.Clear();
					if (_spTiles256.ShouldDraw) _spTiles256.BmpView.Clear();
					break;
			}

			if (_bgPal.ShouldDraw) DrawPalette(_bgPal, false);
			if (_spPal.ShouldDraw) DrawPalette(_spPal, true);

			if (_sprites.ShouldDraw) DrawSprites(_sprites);
		}

		private MobileBmpView MakeMBVWidget(string text, int w, int h)
		{
			var mbv = new MobileBmpView { Text = text };
			mbv.BmpView.Text = text;
			mbv.TopLevel = false;
			mbv.ChangeViewSize(w, h);
			mbv.BmpView.Clear();
			panel1.Controls.Add(mbv);
			listBoxWidgets.Items.Add(mbv);
			return mbv;
		}

		private MobileDetailView MakeMDVWidget(string text, int w, int h)
		{
			var mdv = new MobileDetailView { Text = text };
			mdv.BmpView.Text = text;
			mdv.TopLevel = false;
			mdv.ClientSize = new Size(w, h);
			mdv.BmpView.Clear();
			panel1.Controls.Add(mdv);
			listBoxWidgets.Items.Add(mdv);
			return mdv;
		}

		private void GenerateWidgets()
		{
			listBoxWidgets.BeginUpdate();
			_bg0 = MakeMBVWidget("Background 0", 256, 256);
			_bg1 = MakeMBVWidget("Background 1", 256, 256);
			_bg2 = MakeMBVWidget("Background 2", 256, 256);
			_bg3 = MakeMBVWidget("Background 3", 256, 256);
			_bgPal = MakeMBVWidget("Background Palettes", 256, 256);
			_spPal = MakeMBVWidget("Sprite Palettes", 256, 256);
			_sprites = MakeMBVWidget("Sprites", 1024, 512);
			_spTiles16 = MakeMBVWidget("Sprite Tiles (4bpp)", 256, 256);
			_spTiles256 = MakeMBVWidget("Sprite Tiles (8bpp)", 128, 256);
			_bgTiles16 = MakeMBVWidget("Background Tiles (4bpp)", 512, 256);
			_bgTiles256 = MakeMBVWidget("Background Tiles (8bpp)", 256, 256);
			// todo: finish these
			// MakeMDVWidget("Details", 128, 192);
			// memory = MakeMDVWidget("Details - Memory", 128, 192);
			listBoxWidgets.EndUpdate();

			foreach (var f in listBoxWidgets.Items)
			{
				Form form = (Form)f;
				// close becomes hide
				form.FormClosing += (sender, e) =>
				{
					e.Cancel = true;
					listBoxWidgets.Items.Add(sender);
					(sender as Form).Hide();
				};
				// hackish, and why doesn't winforms handle this directly?
				BringToFrontHack(form, form);
			}
		}

		private static void BringToFrontHack(Control c, Control top)
		{
			c.Click += (o, e) => top.BringToFront();
			if (c.HasChildren)
			{
				foreach (Control cc in c.Controls)
				{
					BringToFrontHack(cc, top);
				}
			}
		}

		public override void Restart()
		{
			var mem = GBA.GetMemoryAreas();
			_vram = mem.vram;
			_palRam = mem.palram;
			_oam = mem.oam;
			_mmio = mem.mmio;

			_cbScanlineEmu = 500; // force an update
			GeneralUpdate();
		}

		protected override void GeneralUpdate() => UpdateBefore();

		protected override void UpdateBefore()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			if (_cbScanlineEmu != _cbScanline)
			{
				_cbScanlineEmu = _cbScanline;
				if (!_cbScanline.HasValue) // manual, deactivate callback
				{
					GBA.SetScanlineCallback(null, 0);
				}
				else
				{
					GBA.SetScanlineCallback(DrawEverything, _cbScanline.Value);
				}
			}
		}

		private void ShowSelectedWidget()
		{
			if (listBoxWidgets.SelectedItem != null)
			{
				var form = listBoxWidgets.SelectedItem as Form;
				form.Show();
				form.BringToFront();
				listBoxWidgets.Items.RemoveAt(listBoxWidgets.SelectedIndex);
			}
		}

		private void buttonShowWidget_Click(object sender, EventArgs e)
		{
			ShowSelectedWidget();
		}

		private void listBoxWidgets_DoubleClick(object sender, EventArgs e)
		{
			ShowSelectedWidget();
		}

		private int? _cbScanline;
		private int? _cbScanlineEmu = 500;

		private void RecomputeRefresh()
		{
			if (radioButtonScanline.Checked)
			{
				hScrollBar1.Enabled = true;
				buttonRefresh.Enabled = false;
				_cbScanline = (hScrollBar1.Value + 160) % 228;
			}
			else if (radioButtonManual.Checked)
			{
				hScrollBar1.Enabled = false;
				buttonRefresh.Enabled = true;
				_cbScanline = null;
			}
		}

		private void radioButtonScanline_CheckedChanged(object sender, EventArgs e)
		{
			RecomputeRefresh();
		}

		private void hScrollBar1_ValueChanged(object sender, EventArgs e)
		{
			_cbScanline = (hScrollBar1.Value + 160) % 228;
			radioButtonScanline.Text = $"Scanline {_cbScanline}";
		}

		private void radioButtonManual_CheckedChanged(object sender, EventArgs e)
		{
			RecomputeRefresh();
		}

		private void buttonRefresh_Click(object sender, EventArgs e)
		{
			DrawEverything();
		}

		private void GbaGpuView_FormClosed(object sender, FormClosedEventArgs e)
			=> GBA.SetScanlineCallback(null, 0);

		private void timerMessage_Tick(object sender, EventArgs e)
		{
			timerMessage.Stop();
			labelClipboard.Text = "CTRL + C: Copy under mouse to clipboard.";
		}

		private void GbaGpuView_KeyDown(object sender, KeyEventArgs e)
		{
			if (ModifierKeys.HasFlag(Keys.Control) && e.KeyCode == Keys.C)
			{
				// find the control under the mouse
				Point m = Cursor.Position;
				Control top = this;
				Control found;
				do
				{
					found = top.GetChildAtPoint(top.PointToClient(m));
					top = found;
				} while (found != null && found.HasChildren);

				if (found is BmpView view)
				{
					Clipboard.SetImage(view.Bmp);
					labelClipboard.Text = $"{view.Text} copied to clipboard.";
					timerMessage.Stop();
					timerMessage.Start();
				}
			}
		}
	}
}
