using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.MultiClient.GBtools;

namespace BizHawk.MultiClient.GBAtools
{
	public partial class GBAGPUView : Form
	{
		Emulation.Consoles.Nintendo.GBA.GBA gba;

		// emulator memory areas
		IntPtr vram;
		IntPtr oam;
		IntPtr mmio;
		IntPtr palram;
		// color conversion to RGB888
		int[] ColorConversion;

		MobileBmpView bg0, bg1, bg2, bg3, bgpal, sppal, sprites, bgtiles16, bgtiles256, sptiles16, sptiles256;

		MobileDetailView details, memory;

		public GBAGPUView()
		{
			InitializeComponent();
			// TODO: hook up something
			// we do this twice to avoid having to & 0x7fff with every color
			int[] tmp = Emulation.Consoles.GB.GBColors.GetLut(Emulation.Consoles.GB.GBColors.ColorType.vivid);
			ColorConversion = new int[65536];
			Buffer.BlockCopy(tmp, 0, ColorConversion, 0, sizeof(int) * tmp.Length);
			Buffer.BlockCopy(tmp, 0, ColorConversion, sizeof(int) * tmp.Length, sizeof(int) * tmp.Length);

			GenerateWidgets();
			radioButtonFrame.Checked = true;
			hScrollBar1_ValueChanged(null, null);
			RecomputeRefresh();
		}

		#region drawing primitives

		unsafe void DrawTile256(int* dest, int pitch, byte* tile, ushort* palette, bool hflip, bool vflip)
		{
			if (vflip)
			{
				dest += pitch * 7;
				pitch = -pitch;
			}

			if (hflip)
			{
				dest += 7;
				for (int y = 0; y < 8; y++)
				{
					for (int x = 0; x < 8; x++)
					{
						*dest-- = ColorConversion[palette[*tile++]];
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
						*dest++ = ColorConversion[palette[*tile++]];
					}
					dest -= 8;
					dest += pitch;
				}
			}
		}

		unsafe void DrawTile16(int* dest, int pitch, byte* tile, ushort* palette, bool hflip, bool vflip)
		{
			if (vflip)
			{
				dest += pitch * 7;
				pitch = -pitch;
			}
			if (hflip)
			{
				dest += 7;
				for (int y = 0; y < 8; y++)
				{
					for (int i = 0; i < 4; i++)
					{
						*dest-- = ColorConversion[palette[*tile & 15]];
						*dest-- = ColorConversion[palette[*tile >> 4]];
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
						*dest++ = ColorConversion[palette[*tile & 15]];
						*dest++ = ColorConversion[palette[*tile >> 4]];
						tile++;
					}
					dest -= 8;
					dest += pitch;
				}
			}
		}

		unsafe void DrawTextNameTable16(int* dest, int pitch, ushort* nametable, byte* tiles)
		{
			for (int ty = 0; ty < 32; ty++)
			{
				for (int tx = 0; tx < 32; tx++)
				{
					ushort ntent = *nametable++;
					DrawTile16(dest, pitch, tiles + (ntent & 1023) * 32, (ushort*)palram + (ntent >> 12 << 4), ntent.Bit(10), ntent.Bit(11));
					dest += 8;
				}
				dest -= 256;
				dest += 8 * pitch;
			}
		}

		unsafe void DrawTextNameTable256(int* dest, int pitch, ushort* nametable, byte* tiles)
		{
			for (int ty = 0; ty < 32; ty++)
			{
				for (int tx = 0; tx < 32; tx++)
				{
					ushort ntent = *nametable++;
					DrawTile256(dest, pitch, tiles + (ntent & 1023) * 64, (ushort*)palram, ntent.Bit(10), ntent.Bit(11));
					dest += 8;
				}
				dest -= 256;
				dest += 8 * pitch;
			}
		}

		unsafe void DrawTextNameTable(int* dest, int pitch, ushort* nametable, byte* tiles, bool eightbit)
		{
			if (eightbit)
				DrawTextNameTable256(dest, pitch, nametable, tiles);
			else
				DrawTextNameTable16(dest, pitch, nametable, tiles);
		}

		unsafe void DrawTextBG(int n, MobileBmpView mbv)
		{
			ushort bgcnt = ((ushort*)mmio)[4 + n];
			int ssize = bgcnt >> 14;
			switch (ssize)
			{
				case 0: mbv.ChangeAllSizes(256, 256); break;
				case 1: mbv.ChangeAllSizes(512, 256); break;
				case 2: mbv.ChangeAllSizes(256, 512); break;
				case 3: mbv.ChangeAllSizes(512, 512); break;
			}
			Bitmap bmp = mbv.bmpView.bmp;
			var lockdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			int* pixels = (int*)lockdata.Scan0;
			int pitch = lockdata.Stride / sizeof(int);

			byte* tiles = (byte*)vram + ((bgcnt & 0xc) << 12);

			ushort* nametable = (ushort*)vram + ((bgcnt & 0x1f00) << 2);

			bool eightbit = bgcnt.Bit(7);

			switch (ssize)
			{
				case 0:
					DrawTextNameTable(pixels, pitch, nametable, tiles, eightbit);
					break;
				case 1:
					DrawTextNameTable(pixels, pitch, nametable, tiles, eightbit);
					pixels += 256;
					nametable += 1024;
					DrawTextNameTable(pixels, pitch, nametable, tiles, eightbit);
					break;
				case 2:
					DrawTextNameTable(pixels, pitch, nametable, tiles, eightbit);
					pixels += pitch * 256;
					nametable += 1024;
					DrawTextNameTable(pixels, pitch, nametable, tiles, eightbit);
					break;
				case 3:
					DrawTextNameTable(pixels, pitch, nametable, tiles, eightbit);
					pixels += 256;
					nametable += 1024;
					DrawTextNameTable(pixels, pitch, nametable, tiles, eightbit);
					pixels -= 256;
					pixels += pitch * 256;
					nametable += 1024;
					DrawTextNameTable(pixels, pitch, nametable, tiles, eightbit);
					pixels += 256;
					nametable += 1024;
					DrawTextNameTable(pixels, pitch, nametable, tiles, eightbit);
					break;
			}

			bmp.UnlockBits(lockdata);
			mbv.bmpView.Refresh();
		}

		unsafe void DrawAffineBG(int n, MobileBmpView mbv)
		{
			ushort bgcnt = ((ushort*)mmio)[4 + n];
			int ssize = bgcnt >> 14;
			switch (ssize)
			{
				case 0: mbv.ChangeAllSizes(128, 128); break;
				case 1: mbv.ChangeAllSizes(256, 256); break;
				case 2: mbv.ChangeAllSizes(512, 512); break;
				case 3: mbv.ChangeAllSizes(1024, 1024); break;
			}
			Bitmap bmp = mbv.bmpView.bmp;
			var lockdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			int* pixels = (int*)lockdata.Scan0;
			int pitch = lockdata.Stride / sizeof(int);

			byte* tiles = (byte*)vram + ((bgcnt & 0xc) << 12);

			byte* nametable = (byte*)vram + ((bgcnt & 0x1f00) << 3);

			for (int ty = 0; ty < bmp.Height / 8; ty++)
			{
				for (int tx = 0; tx < bmp.Width / 8; tx++)
				{
					DrawTile256(pixels, pitch, tiles + *nametable++ * 64, (ushort*)palram, false, false);
					pixels += 8;
				}
				pixels -= bmp.Width;
				pixels += 8 * pitch;
			}

			bmp.UnlockBits(lockdata);
			mbv.bmpView.Refresh();
		}

		unsafe void DrawM3BG(MobileBmpView mbv)
		{
			mbv.ChangeAllSizes(240, 160);
			Bitmap bmp = mbv.bmpView.bmp;
			var lockdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			int* pixels = (int*)lockdata.Scan0;
			int pitch = lockdata.Stride / sizeof(int);

			ushort* frame = (ushort*)vram;

			for (int y = 0; y < 160; y++)
			{
				for (int x = 0; x < 240; x++)
					*pixels++ = ColorConversion[*frame++];
				pixels -= 240;
				pixels += pitch;
			}

			bmp.UnlockBits(lockdata);
			mbv.bmpView.Refresh();
		}

		unsafe void DrawM4BG(MobileBmpView mbv, bool secondframe)
		{
			mbv.ChangeAllSizes(240, 160);
			Bitmap bmp = mbv.bmpView.bmp;
			var lockdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			int* pixels = (int*)lockdata.Scan0;
			int pitch = lockdata.Stride / sizeof(int);

			byte* frame = (byte*)vram + (secondframe ? 40960 : 0);
			ushort* palette = (ushort*)palram;

			for (int y = 0; y < 160; y++)
			{
				for (int x = 0; x < 240; x++)
					*pixels++ = ColorConversion[palette[*frame++]];
				pixels -= 240;
				pixels += pitch;
			}

			bmp.UnlockBits(lockdata);
			mbv.bmpView.Refresh();
		}

		unsafe void DrawM5BG(MobileBmpView mbv, bool secondframe)
		{
			mbv.ChangeAllSizes(160, 128);
			Bitmap bmp = mbv.bmpView.bmp;
			var lockdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			int* pixels = (int*)lockdata.Scan0;
			int pitch = lockdata.Stride / sizeof(int);

			ushort* frame = (ushort*)vram + (secondframe ? 20480 : 0);

			for (int y = 0; y < 128; y++)
			{
				for (int x = 0; x < 160; x++)
					*pixels++ = ColorConversion[*frame++];
				pixels -= 160;
				pixels += pitch;
			}

			bmp.UnlockBits(lockdata);
			mbv.bmpView.Refresh();
		}

		static readonly int[, ,] spritesizes = { { { 1, 1 }, { 2, 2 }, { 4, 4 }, { 8, 8 } }, { { 2, 1 }, { 4, 1 }, { 4, 2 }, { 8, 4 } }, { { 1, 2 }, { 1, 4 }, { 2, 4 }, { 4, 8 } } };

		unsafe void DrawSprite(int* dest, int pitch, ushort* sprite, byte* tiles, bool twodee)
		{
			ushort attr0 = sprite[0];
			ushort attr1 = sprite[1];
			ushort attr2 = sprite[2];

			if (!attr0.Bit(8) && attr0.Bit(9))
				return; // 2x with affine off

			int tw, th;
			int shape = attr0 >> 14;
			if (shape == 3)
				return;
			int size = attr1 >> 14;
			tw = spritesizes[shape, size, 0];
			th = spritesizes[shape, size, 1];

			bool eightbit = attr0.Bit(13);
			bool hflip = attr1.Bit(12);
			bool vflip = attr1.Bit(13);

			ushort* palette = (ushort*)palram + 256;
			if (!eightbit)
				palette += attr2 >> 12 << 4;
			if (!eightbit)
				tiles += 32 * (attr2 & 1023);
			else
				tiles += 32 * (attr2 & 1022);

			int tilestride = 0;
			if (twodee)
				tilestride = 1024 - tw * (eightbit ? 64 : 32);
			if (vflip)
				dest += pitch * 8 * (th - 1);
			if (hflip)
				dest += 8 * (tw - 1);
			for (int ty = 0; ty < th; ty++)
			{
				for (int tx = 0; tx < tw; tx++)
				{
					if (eightbit)
					{
						DrawTile256(dest, pitch, tiles, palette, hflip, vflip);
						tiles += 64;
					}
					else
					{
						DrawTile16(dest, pitch, tiles, palette, hflip, vflip);
						tiles += 32;
					}
					if (hflip)
						dest -= 8;
					else
						dest += 8;
				}
				if (hflip)
					dest += tw * 8;
				else
					dest -= tw * 8;
				if (vflip)
					dest -= pitch * 8;
				else
					dest += pitch * 8;
				tiles += tilestride;
			}
		}

		unsafe void DrawSprites(MobileBmpView mbv)
		{
			mbv.bmpView.ChangeBitmapSize(1024, 512);
			Bitmap bmp = mbv.bmpView.bmp;
			var lockdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			// Clear()
			Win32.MemSet(lockdata.Scan0, 0xff, (uint)(lockdata.Height * lockdata.Stride));

			int* pixels = (int*)lockdata.Scan0;
			int pitch = lockdata.Stride / sizeof(int);

			ushort* sprites = (ushort*)oam;
			byte* tiles = (byte*)vram + 65536;

			ushort dispcnt = ((ushort*)mmio)[0];
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

			bmp.UnlockBits(lockdata);
			mbv.bmpView.Refresh();
		}

		unsafe void DrawPalette(MobileBmpView mbv, bool sprite)
		{
			mbv.bmpView.ChangeBitmapSize(16, 16);
			Bitmap bmp = mbv.bmpView.bmp;
			var lockdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			int* pixels = (int*)lockdata.Scan0;
			int pitch = lockdata.Stride / sizeof(int);

			ushort* palette = (ushort*)palram + (sprite ? 256 : 0);

			for (int j = 0; j < 16; j++)
			{
				for (int i = 0; i < 16; i++)
					*pixels++ = ColorConversion[*palette++];
				pixels -= 16;
				pixels += pitch;
			}

			bmp.UnlockBits(lockdata);
			mbv.bmpView.Refresh();
		}

		unsafe void DrawTileRange(int* dest, int pitch, byte* tiles, ushort* palette, int tw, int th, bool eightbit)
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

		unsafe void DrawSpriteTiles(MobileBmpView mbv, bool tophalfonly, bool eightbit)
		{
			int tw = eightbit ? 16 : 32;
			int th = tophalfonly ? 16 : 32;

			mbv.bmpView.ChangeBitmapSize(tw * 8, 256);
			Bitmap bmp = mbv.bmpView.bmp;
			var lockdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			int* pixels = (int*)lockdata.Scan0;
			int pitch = lockdata.Stride / sizeof(int);

			byte* tiles = (byte*)vram + 65536;
			// TODO: palette changing (in 4 bit mode anyway)
			ushort* palette = (ushort*)palram + 256;

			if (tophalfonly)
			{
				Win32.MemSet(lockdata.Scan0, 0xff, (uint)(128 * lockdata.Stride));
				pixels += 128 * pitch;
				tiles += 16384;
			}
			DrawTileRange(pixels, pitch, tiles, palette, tw, th, eightbit);
			bmp.UnlockBits(lockdata);
			mbv.bmpView.Refresh();
		}

		unsafe void DrawBGTiles(MobileBmpView mbv, bool eightbit)
		{
			int tw = eightbit ? 32 : 64;
			int th = 32;

			mbv.bmpView.ChangeBitmapSize(tw * 8, th * 8);
			Bitmap bmp = mbv.bmpView.bmp;
			var lockdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			int* pixels = (int*)lockdata.Scan0;
			int pitch = lockdata.Stride / sizeof(int);

			byte* tiles = (byte*)vram;
			// TODO: palette changing (in 4 bit mode anyway)
			ushort* palette = (ushort*)palram;
			DrawTileRange(pixels, pitch, tiles, palette, tw, th, eightbit);
			bmp.UnlockBits(lockdata);
			mbv.bmpView.Refresh();
		}


		#endregion

		unsafe void DrawEverything()
		{
			ushort dispcnt = ((ushort*)mmio)[0];

			int bgmode = dispcnt & 7;
			switch (bgmode)
			{
				case 0:
					if (bg0.ShouldDraw) DrawTextBG(0, bg0);
					if (bg1.ShouldDraw) DrawTextBG(1, bg1);
					if (bg2.ShouldDraw) DrawTextBG(2, bg2);
					if (bg3.ShouldDraw) DrawTextBG(3, bg3);
					if (bgtiles16.ShouldDraw) DrawBGTiles(bgtiles16, false);
					if (bgtiles256.ShouldDraw) DrawBGTiles(bgtiles256, true);
					if (sptiles16.ShouldDraw) DrawSpriteTiles(sptiles16, false, false);
					if (sptiles256.ShouldDraw) DrawSpriteTiles(sptiles256, false, true);
					break;
				case 1:
					if (bg0.ShouldDraw) DrawTextBG(0, bg0);
					if (bg1.ShouldDraw) DrawTextBG(1, bg1);
					if (bg2.ShouldDraw) DrawAffineBG(2, bg2);
					if (bg3.ShouldDraw) bg3.bmpView.Clear();
					if (bgtiles16.ShouldDraw) DrawBGTiles(bgtiles16, false);
					if (bgtiles256.ShouldDraw) DrawBGTiles(bgtiles256, true);
					if (sptiles16.ShouldDraw) DrawSpriteTiles(sptiles16, false, false);
					if (sptiles256.ShouldDraw) DrawSpriteTiles(sptiles256, false, true);
					break;
				case 2:
					if (bg0.ShouldDraw) bg0.bmpView.Clear();
					if (bg1.ShouldDraw) bg1.bmpView.Clear();
					if (bg2.ShouldDraw) DrawAffineBG(2, bg2);
					if (bg3.ShouldDraw) DrawAffineBG(3, bg3);
					// while there are no 4bpp tiles possible in mode 2, there might be some in memory
					// due to midframe mode switching.  no real reason not to display them if that's
					// what the user wants to see
					if (bgtiles16.ShouldDraw) DrawBGTiles(bgtiles16, false);
					if (bgtiles256.ShouldDraw) DrawBGTiles(bgtiles256, true);
					if (sptiles16.ShouldDraw) DrawSpriteTiles(sptiles16, false, false);
					if (sptiles256.ShouldDraw) DrawSpriteTiles(sptiles256, false, true);
					break;
				case 3:
					if (bg0.ShouldDraw) bg0.bmpView.Clear();
					if (bg1.ShouldDraw) bg1.bmpView.Clear();
					if (bg2.ShouldDraw) DrawM3BG(bg2);
					if (bg3.ShouldDraw) bg3.bmpView.Clear();
					if (bgtiles16.ShouldDraw) bgtiles16.bmpView.Clear();
					if (bgtiles256.ShouldDraw) bgtiles256.bmpView.Clear();
					if (sptiles16.ShouldDraw) DrawSpriteTiles(sptiles16, true, false);
					if (sptiles256.ShouldDraw) DrawSpriteTiles(sptiles256, true, true);
					break;
				//in modes 4, 5, bg3 is repurposed as bg2 invisible frame
				case 4:
					if (bg0.ShouldDraw) bg0.bmpView.Clear();
					if (bg1.ShouldDraw) bg1.bmpView.Clear();
					if (bg2.ShouldDraw) DrawM4BG(bg2, dispcnt.Bit(4));
					if (bg3.ShouldDraw) DrawM4BG(bg3, !dispcnt.Bit(4));
					if (bgtiles16.ShouldDraw) bgtiles16.bmpView.Clear();
					if (bgtiles256.ShouldDraw) bgtiles256.bmpView.Clear();
					if (sptiles16.ShouldDraw) DrawSpriteTiles(sptiles16, true, false);
					if (sptiles256.ShouldDraw) DrawSpriteTiles(sptiles256, true, true);
					break;
				case 5:
					if (bg0.ShouldDraw) bg0.bmpView.Clear();
					if (bg1.ShouldDraw) bg1.bmpView.Clear();
					if (bg2.ShouldDraw) DrawM5BG(bg2, dispcnt.Bit(4));
					if (bg3.ShouldDraw) DrawM5BG(bg3, !dispcnt.Bit(4));
					if (bgtiles16.ShouldDraw) bgtiles16.bmpView.Clear();
					if (bgtiles256.ShouldDraw) bgtiles256.bmpView.Clear();
					if (sptiles16.ShouldDraw) DrawSpriteTiles(sptiles16, true, false);
					if (sptiles256.ShouldDraw) DrawSpriteTiles(sptiles256, true, true);
					break;
				default:
					// shouldn't happen, but shouldn't be our problem either
					if (bg0.ShouldDraw) bg0.bmpView.Clear();
					if (bg1.ShouldDraw) bg1.bmpView.Clear();
					if (bg2.ShouldDraw) bg2.bmpView.Clear();
					if (bg3.ShouldDraw) bg3.bmpView.Clear();
					if (bgtiles16.ShouldDraw) bgtiles16.bmpView.Clear();
					if (bgtiles256.ShouldDraw) bgtiles256.bmpView.Clear();
					if (sptiles16.ShouldDraw) sptiles16.bmpView.Clear();
					if (sptiles256.ShouldDraw) sptiles256.bmpView.Clear();
					break;
			}

			if (bgpal.ShouldDraw) DrawPalette(bgpal, false);
			if (sppal.ShouldDraw) DrawPalette(sppal, true);

			if (sprites.ShouldDraw) DrawSprites(sprites);
		}

		MobileBmpView MakeMBVWidget(string text, int w, int h)
		{
			var mbv = new MobileBmpView();
			mbv.Text = text;
			mbv.bmpView.Text = text;
			mbv.TopLevel = false;
			mbv.ChangeViewSize(w, h);
			mbv.bmpView.Clear();
			panel1.Controls.Add(mbv);
			listBoxWidgets.Items.Add(mbv);
			return mbv;
		}

		MobileDetailView MakeMDVWidget(string text, int w, int h)
		{
			var mdv = new MobileDetailView();
			mdv.Text = text;
			mdv.bmpView.Text = text;
			mdv.TopLevel = false;
			mdv.ClientSize = new Size(w, h);
			mdv.bmpView.Clear();
			panel1.Controls.Add(mdv);
			listBoxWidgets.Items.Add(mdv);
			return mdv;
		}

		void GenerateWidgets()
		{
			listBoxWidgets.BeginUpdate();
			bg0 = MakeMBVWidget("Background 0", 256, 256);
			bg1 = MakeMBVWidget("Background 1", 256, 256);
			bg2 = MakeMBVWidget("Background 2", 256, 256);
			bg3 = MakeMBVWidget("Background 3", 256, 256);
			bgpal = MakeMBVWidget("Background Palettes", 256, 256);
			sppal = MakeMBVWidget("Sprite Palettes", 256, 256);
			sprites = MakeMBVWidget("Sprites", 1024, 512);
			sptiles16 = MakeMBVWidget("Sprite Tiles (4bpp)", 256, 256);
			sptiles256 = MakeMBVWidget("Sprite Tiles (8bpp)", 128, 256);
			bgtiles16 = MakeMBVWidget("Background Tiles (4bpp)", 512, 256);
			bgtiles256 = MakeMBVWidget("Background Tiles (8bpp)", 256, 256);
			details = MakeMDVWidget("Details", 128, 192);
			memory = MakeMDVWidget("Details - Memory", 128, 192);
			listBoxWidgets.EndUpdate();

			foreach (var f in listBoxWidgets.Items)
			{
				Form form = (Form)f;
				// close becomes hide
				form.FormClosing += delegate(object sender, FormClosingEventArgs e)
				{
					e.Cancel = true;
					listBoxWidgets.Items.Add(sender);
					(sender as Form).Hide();
				};
				// hackish, and why doesn't winforms handle this directly?
				BringToFrontHack(form, form);
			}
		}

		static void BringToFrontHack(Control c, Control top)
		{
			c.Click += (o, e) => top.BringToFront();
			if (c.HasChildren)
				foreach (Control cc in c.Controls)
					BringToFrontHack(cc, top);
		}


		public void Restart()
		{
			gba = Global.Emulator as Emulation.Consoles.Nintendo.GBA.GBA;
			if (gba != null)
			{
				gba.GetGPUMemoryAreas(out vram, out palram, out oam, out mmio);
			}
			else
			{
				if (Visible)
					Close();
			}
		}

		/// <summary>belongs in ToolsBefore</summary>
		public void UpdateValues()
		{
			if (!this.IsHandleCreated || this.IsDisposed)
				return;
			if (gba != null)
			{
				if (cbscanline_emu != cbscanline)
				{
					cbscanline_emu = cbscanline;
					if (cbscanline == -2) // manual, do nothing
					{
						gba.SetScanlineCallback(null, null);
					}
					else if (cbscanline == -1) // end of frame
					{
						gba.SetScanlineCallback(DrawEverything, null);
					}
					else
					{
						gba.SetScanlineCallback(DrawEverything, cbscanline);
					}
				}
			}
		}

		private void GBAGPUView_Load(object sender, EventArgs e)
		{
			Restart();
		}

		void ShowSelectedWidget()
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

		#region refresh control

		int cbscanline;
		int cbscanline_emu = 500;

		void RecomputeRefresh()
		{
			if (radioButtonFrame.Checked)
			{
				hScrollBar1.Enabled = false;
				buttonRefresh.Enabled = false;
				cbscanline = -1;
			}
			else if (radioButtonScanline.Checked)
			{
				hScrollBar1.Enabled = true;
				buttonRefresh.Enabled = false;
				cbscanline = (hScrollBar1.Value + 160) % 228;
			}
			else if (radioButtonManual.Checked)
			{
				hScrollBar1.Enabled = false;
				buttonRefresh.Enabled = true;
				cbscanline = -2;
			}
		}

		private void radioButtonFrame_CheckedChanged(object sender, EventArgs e)
		{
			RecomputeRefresh();
		}

		private void radioButtonScanline_CheckedChanged(object sender, EventArgs e)
		{
			RecomputeRefresh();
		}

		private void hScrollBar1_ValueChanged(object sender, EventArgs e)
		{
			cbscanline = (hScrollBar1.Value + 160) % 228;
			radioButtonScanline.Text = "Scanline " + cbscanline;
		}

		private void radioButtonManual_CheckedChanged(object sender, EventArgs e)
		{
			RecomputeRefresh();
		}

		private void buttonRefresh_Click(object sender, EventArgs e)
		{
			DrawEverything();
		}

		#endregion

		private void GBAGPUView_FormClosed(object sender, FormClosedEventArgs e)
		{
			if (gba != null)
			{
				gba.SetScanlineCallback(null, null);
				gba = null;
			}
		}

		#region copy to clipboard

		private void timerMessage_Tick(object sender, EventArgs e)
		{
			timerMessage.Stop();
			labelClipboard.Text = "CTRL + C: Copy under mouse to clipboard.";
		}

		private void GBAGPUView_KeyDown(object sender, KeyEventArgs e)
		{
			if (Control.ModifierKeys.HasFlag(Keys.Control) && e.KeyCode == Keys.C)
			{
				// find the control under the mouse
				Point m = System.Windows.Forms.Cursor.Position;
				Control top = this;
				Control found = null;
				do
				{
					found = top.GetChildAtPoint(top.PointToClient(m));
					top = found;
				} while (found != null && found.HasChildren);

				if (found != null && found is BmpView)
				{
					var bv = found as BmpView;
					Clipboard.SetImage(bv.bmp);
					labelClipboard.Text = found.Text + " copied to clipboard.";
					timerMessage.Stop();
					timerMessage.Start();
				}
			}
		}
		#endregion
	}
}
