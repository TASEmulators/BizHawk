using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient.GBtools
{
	public partial class GBGPUView : Form
	{
		Emulation.Consoles.GB.Gameboy gb;

		// gambatte doesn't modify these memory locations unless you reconstruct, so we can store
		IntPtr vram;
		IntPtr bgpal;
		IntPtr sppal;
		IntPtr oam;

		bool cgb; // set once at start
		int lcdc; // set at each callback

		IntPtr tilespal; // current palette to use on tiles

		public GBGPUView()
		{
			InitializeComponent();
			bmpViewBG.ChangeBitmapSize(256, 256);
			bmpViewWin.ChangeBitmapSize(256, 256);
			bmpViewTiles1.ChangeBitmapSize(128, 192);
			bmpViewTiles2.ChangeBitmapSize(128, 192);
			bmpViewBGPal.ChangeBitmapSize(8, 4);
			bmpViewSPPal.ChangeBitmapSize(8, 4);
			bmpViewOAM.ChangeBitmapSize(320, 16);
			bmpViewDetails.ChangeBitmapSize(8, 16);
			bmpViewMemory.ChangeBitmapSize(8, 16);

			hScrollBarScanline.Value = 0;
			hScrollBarScanline_ValueChanged(null, null); // not firing in this case??
			radioButtonRefreshFrame.Checked = true;
		}

		public void Restart()
		{
			if (Global.Emulator is Emulation.Consoles.GB.Gameboy)
			{
				gb = Global.Emulator as Emulation.Consoles.GB.Gameboy;
				cgb = gb.IsCGBMode();
				lcdc = 0;
				if (!gb.GetGPUMemoryAreas(out vram, out bgpal, out sppal, out oam))
				{
					gb = null;
					if (Visible)
						Close();
				}
				tilespal = bgpal;

				if (cgb)
					label4.Enabled = true;
				else
					label4.Enabled = false;
				bmpViewBG.Clear();
				bmpViewWin.Clear();
				bmpViewTiles1.Clear();
				bmpViewTiles2.Clear();
				bmpViewBGPal.Clear();
				bmpViewSPPal.Clear();
				bmpViewOAM.Clear();
				bmpViewDetails.Clear();
				bmpViewMemory.Clear();
				cbscanline_emu = -4; // force refresh
			}
			else
			{
				gb = null;
				if (Visible)
					Close();
			}
		}


		#region drawing primitives

		/// <summary>
		/// draw a single 2bpp tile
		/// </summary>
		/// <param name="tile">16 byte 2bpp 8x8 tile (gb format)</param>
		/// <param name="dest">top left origin on 32bit bitmap</param>
		/// <param name="pitch">pitch of bitmap in 4 byte units</param>
		/// <param name="pal">4 palette colors</param>
		static unsafe void DrawTile(byte* tile, int* dest, int pitch, int* pal)
		{
			for (int y = 0; y < 8; y++)
			{
				int loplane = *tile++;
				int hiplane = *tile++;
				hiplane <<= 1; // msb
				dest += 7;
				for (int x = 0; x < 8; x++) // right to left
				{
					int color = loplane & 1 | hiplane & 2;
					*dest-- = pal[color];
					loplane >>= 1;
					hiplane >>= 1;
				}
				dest++;
				dest += pitch;
			}
		}

		/// <summary>
		/// draw a single 2bpp tile, with hflip and vflip
		/// </summary>
		/// <param name="tile">16 byte 2bpp 8x8 tile (gb format)</param>
		/// <param name="dest">top left origin on 32bit bitmap</param>
		/// <param name="pitch">pitch of bitmap in 4 byte units</param>
		/// <param name="pal">4 palette colors</param>
		/// <param name="hflip">true to flip horizontally</param>
		/// <param name="vflip">true to flip vertically</param>
		static unsafe void DrawTileHV(byte* tile, int* dest, int pitch, int* pal, bool hflip, bool vflip)
		{
			if (vflip)
				dest += pitch * 7;
			for (int y = 0; y < 8; y++)
			{
				int loplane = *tile++;
				int hiplane = *tile++;
				hiplane <<= 1; // msb
				if (!hflip)
					dest += 7;
				for (int x = 0; x < 8; x++) // right to left
				{
					int color = loplane & 1 | hiplane & 2;
					*dest = pal[color];
					if (!hflip)
						dest--;
					else
						dest++;
					loplane >>= 1;
					hiplane >>= 1;
				}
				if (!hflip)
					dest++;
				else
					dest -= 8;
				if (!vflip)
					dest += pitch;
				else
					dest -= pitch;
			}
		}

		/// <summary>
		/// draw a bg map, cgb format
		/// </summary>
		/// <param name="b">bitmap to draw to, should be 256x256</param>
		/// <param name="_map">tilemap, 32x32 bytes. extended tilemap assumed to be @+8k</param>
		/// <param name="_tiles">base tiledata location. second bank tiledata assumed to be @+8k</param>
		/// <param name="wrap">true if tileindexes are s8 (not u8)</param>
		/// <param name="_pal">8 palettes (4 colors each)</param>
		static unsafe void DrawBGCGB(Bitmap b, IntPtr _map, IntPtr _tiles, bool wrap, IntPtr _pal)
		{
			var lockdata = b.LockBits(new Rectangle(0, 0, 256, 256), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			byte* map = (byte*)_map;
			int* dest = (int*)lockdata.Scan0;
			int pitch = lockdata.Stride / sizeof(int); // in int*s, not bytes
			int* pal = (int*)_pal;

			for (int ty = 0; ty < 32; ty++)
			{
				for (int tx = 0; tx < 32; tx++)
				{
					int tileindex = map[0];
					int tileext = map[8192];
					if (wrap && tileindex >= 128)
						tileindex -= 256;
					byte* tile = (byte*)(_tiles + tileindex * 16);
					if (tileext.Bit(3)) // second bank
						tile += 8192;

					int* thispal = pal + 4 * (tileext & 7);

					DrawTileHV(tile, dest, pitch, thispal, tileext.Bit(5), tileext.Bit(6));
					map++;
					dest += 8;
				}
				dest -= 256;
				dest += pitch * 8;
			}
			b.UnlockBits(lockdata);
		}

		/// <summary>
		/// draw a bg map, dmg format
		/// </summary>
		/// <param name="b">bitmap to draw to, should be 256x256</param>
		/// <param name="_map">tilemap, 32x32 bytes</param>
		/// <param name="_tiles">base tiledata location</param>
		/// <param name="wrap">true if tileindexes are s8 (not u8)</param>
		/// <param name="_pal">1 palette (4 colors)</param>
		static unsafe void DrawBGDMG(Bitmap b, IntPtr _map, IntPtr _tiles, bool wrap, IntPtr _pal)
		{
			var lockdata = b.LockBits(new Rectangle(0, 0, 256, 256), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			byte* map = (byte*)_map;
			int* dest = (int*)lockdata.Scan0;
			int pitch = lockdata.Stride / sizeof(int); // in int*s, not bytes
			int* pal = (int*)_pal;

			for (int ty = 0; ty < 32; ty++)
			{
				for (int tx = 0; tx < 32; tx++)
				{
					int tileindex = map[0];
					if (wrap && tileindex >= 128)
						tileindex -= 256;
					byte* tile = (byte*)(_tiles + tileindex * 16);
					DrawTile(tile, dest, pitch, pal);
					map++;
					dest += 8;
				}
				dest -= 256;
				dest += pitch * 8;
			}
			b.UnlockBits(lockdata);
		}

		/// <summary>
		/// draw a full bank of 384 tiles
		/// </summary>
		/// <param name="b">bitmap to draw to, should be 128x192</param>
		/// <param name="_tiles">base tile address</param>
		/// <param name="_pal">single palette to use on all tiles</param>
		static unsafe void DrawTiles(Bitmap b, IntPtr _tiles, IntPtr _pal)
		{
			var lockdata = b.LockBits(new Rectangle(0, 0, 128, 192), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			int* dest = (int*)lockdata.Scan0;
			int pitch = lockdata.Stride / sizeof(int);
			int* pal = (int*)_pal;
			byte* tile = (byte*)_tiles;

			for (int ty = 0; ty < 24; ty++)
			{
				for (int tx = 0; tx < 16; tx++)
				{
					DrawTile(tile, dest, pitch, pal);
					tile += 16;
					dest += 8;
				}
				dest -= 128;
				dest += pitch * 8;
			}
			b.UnlockBits(lockdata);
		}

		/// <summary>
		/// draw oam data
		/// </summary>
		/// <param name="b">bitmap to draw to.  should be 320x8 (!tall), 320x16 (tall)</param>
		/// <param name="_oam">oam data, 4 * 40 bytes</param>
		/// <param name="_tiles">base tiledata location. cgb: second bank tiledata assumed to be @+8k</param>
		/// <param name="_pal">2 (dmg) or 8 (cgb) palettes</param>
		/// <param name="tall">true for 8x16 sprites; else 8x8</param>
		/// <param name="cgb">true for cgb (more palettes, second bank tiles)</param>
		static unsafe void DrawOam(Bitmap b, IntPtr _oam, IntPtr _tiles, IntPtr _pal, bool tall, bool cgb)
		{
			var lockdata = b.LockBits(new Rectangle(0, 0, 320, tall ? 16 : 8), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			int* dest = (int*)lockdata.Scan0;
			int pitch = lockdata.Stride / sizeof(int);
			int* pal = (int*)_pal;
			byte* oam = (byte*)_oam;

			for (int s = 0; s < 40; s++)
			{
				oam += 2; // ypos, xpos
				int tileindex = *oam++;
				int flags = *oam++;
				bool vflip = flags.Bit(6);
				bool hflip = flags.Bit(5);
				if (tall)
					// i assume 8x16 vflip flips the whole thing, not just each tile?
					if (vflip)
						tileindex |= 1;
					else
						tileindex &= 0xfe;
				byte* tile = (byte*)(_tiles + tileindex * 16);
				int* thispal = pal + 4 * (cgb ? flags & 7 : flags >> 4 & 1);
				if (cgb && flags.Bit(3))
					tile += 8192;
				DrawTileHV(tile, dest, pitch, thispal, hflip, vflip);
				if (tall)
					DrawTileHV((byte*)((int)tile ^ 16), dest + pitch * 8, pitch, thispal, hflip, vflip);
				dest += 8;
			}
			b.UnlockBits(lockdata);
		}

		/// <summary>
		/// draw a palette directly
		/// </summary>
		/// <param name="b">bitmap to draw to.  should be numpals x 4</param>
		/// <param name="_pal">start of palettes</param>
		/// <param name="numpals">number of palettes (not colors)</param>
		static unsafe void DrawPal(Bitmap b, IntPtr _pal, int numpals)
		{
			var lockdata = b.LockBits(new Rectangle(0, 0, numpals, 4), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			int* dest = (int*)lockdata.Scan0;
			int pitch = lockdata.Stride / sizeof(int);
			int* pal = (int*)_pal;

			for (int px = 0; px < numpals; px++)
			{
				for (int py = 0; py < 4; py++)
				{
					*dest = *pal++;
					dest += pitch;
				}
				dest -= pitch * 4;
				dest++;
			}
			b.UnlockBits(lockdata);
		}

		#endregion

		void ScanlineCallback(int lcdc)
		{
			this.lcdc = lcdc;
			// set alpha on all pixels
			unsafe
			{
				int* p;
				p = (int*)bgpal;
				for (int i = 0; i < 32; i++)
					p[i] |= unchecked((int)0xff000000);
				p = (int*)sppal;
				for (int i = 0; i < 32; i++)
					p[i] |= unchecked((int)0xff000000);
			}

			// bg maps
			if (!cgb)
			{
				DrawBGDMG(
					bmpViewBG.bmp,
					vram + (lcdc.Bit(3) ? 0x1c00 : 0x1800),
					vram + (lcdc.Bit(4) ? 0x0000 : 0x1000),
					!lcdc.Bit(4),
					bgpal);

				DrawBGDMG(
					bmpViewWin.bmp,
					vram + (lcdc.Bit(6) ? 0x1c00 : 0x1800),
					vram + 0x1000, // force win to second tile bank???
					true,
					bgpal);
			}
			else
			{
				DrawBGCGB(
					bmpViewBG.bmp,
					vram + (lcdc.Bit(3) ? 0x1c00 : 0x1800),
					vram + (lcdc.Bit(4) ? 0x0000 : 0x1000),
					!lcdc.Bit(4),
					bgpal);

				DrawBGCGB(
					bmpViewWin.bmp,
					vram + (lcdc.Bit(6) ? 0x1c00 : 0x1800),
					vram + 0x1000, // force win to second tile bank???
					true,
					bgpal);
			}
			bmpViewBG.Refresh();
			bmpViewWin.Refresh();

			// tile display
			// TODO: user selects palette to use, instead of fixed palette 0
			// or possibly "smart" where, if a tile is in use, it's drawn with one of the palettes actually being used with it?
			DrawTiles(bmpViewTiles1.bmp, vram, tilespal);
			bmpViewTiles1.Refresh();
			if (cgb)
			{
				DrawTiles(bmpViewTiles2.bmp, vram + 0x2000, tilespal);
				bmpViewTiles2.Refresh();
			}

			// palettes
			if (cgb)
			{
				bmpViewBGPal.ChangeBitmapSize(8, 4);
				if (bmpViewBGPal.Width != 128)
					bmpViewBGPal.Width = 128;
				bmpViewSPPal.ChangeBitmapSize(8, 4);
				if (bmpViewSPPal.Width != 128)
					bmpViewSPPal.Width = 128;
				DrawPal(bmpViewBGPal.bmp, bgpal, 8);
				DrawPal(bmpViewSPPal.bmp, sppal, 8);
			}
			else
			{
				bmpViewBGPal.ChangeBitmapSize(1, 4);
				if (bmpViewBGPal.Width != 16)
					bmpViewBGPal.Width = 16;
				bmpViewSPPal.ChangeBitmapSize(2, 4);
				if (bmpViewSPPal.Width != 32)
					bmpViewSPPal.Width = 32;
				DrawPal(bmpViewBGPal.bmp, bgpal, 1);
				DrawPal(bmpViewSPPal.bmp, sppal, 2);
			}
			bmpViewBGPal.Refresh();
			bmpViewSPPal.Refresh();

			// oam
			if (lcdc.Bit(2)) // 8x16
			{
				bmpViewOAM.ChangeBitmapSize(320, 16);
				if (bmpViewOAM.Height != 16)
					bmpViewOAM.Height = 16;
			}
			else
			{
				bmpViewOAM.ChangeBitmapSize(320, 8);
				if (bmpViewOAM.Height != 8)
					bmpViewOAM.Height = 8;
			}
			DrawOam(bmpViewOAM.bmp, oam, vram, sppal, lcdc.Bit(2), cgb);
			bmpViewOAM.Refresh();
		}

		private void GBGPUView_FormClosed(object sender, FormClosedEventArgs e)
		{
			if (gb != null)
			{
				gb.SetScanlineCallback(null, 0);
				gb = null;
			}
		}

		private void GBGPUView_Load(object sender, EventArgs e)
		{
			Restart();
		}

		#region refresh

		private void radioButtonRefreshFrame_CheckedChanged(object sender, EventArgs e) { ComputeRefreshValues(); }
		private void radioButtonRefreshScanline_CheckedChanged(object sender, EventArgs e) { ComputeRefreshValues(); }
		private void radioButtonRefreshManual_CheckedChanged(object sender, EventArgs e) { ComputeRefreshValues(); }

		void ComputeRefreshValues()
		{
			if (radioButtonRefreshFrame.Checked)
			{
				labelScanline.Enabled = false;
				hScrollBarScanline.Enabled = false;
				buttonRefresh.Enabled = false;
				cbscanline = -1;
			}
			else if (radioButtonRefreshScanline.Checked)
			{
				labelScanline.Enabled = true;
				hScrollBarScanline.Enabled = true;
				buttonRefresh.Enabled = false;
				cbscanline = (hScrollBarScanline.Value + 145) % 154;
			}
			else if (radioButtonRefreshManual.Checked)
			{
				labelScanline.Enabled = false;
				hScrollBarScanline.Enabled = false;
				buttonRefresh.Enabled = true;
				cbscanline = -2;
			}
		}

		private void buttonRefresh_Click(object sender, EventArgs e)
		{
			if (cbscanline == -2 && gb != null)
				gb.SetScanlineCallback(ScanlineCallback, -2);
		}

		private void hScrollBarScanline_ValueChanged(object sender, EventArgs e)
		{
			labelScanline.Text = ((hScrollBarScanline.Value + 145) % 154).ToString();
		}

		/// <summary>
		/// 0..153: scanline number. -1: frame.  -2: manual
		/// </summary>
		int cbscanline;
		/// <summary>
		/// what was last passed to the emu core
		/// </summary>
		int cbscanline_emu = -4; // force refresh

		/// <summary>
		/// put me in ToolsBefore
		/// </summary>
		public void UpdateValues()
		{
			if (!this.IsHandleCreated || this.IsDisposed)
				return;
			if (gb != null)
			{
				if (!this.Visible)
				{
					if (cbscanline_emu != -2)
					{
						cbscanline_emu = -2;
						gb.SetScanlineCallback(null, 0);
					}
				}
				else
				{
					if (cbscanline != cbscanline_emu)
					{
						cbscanline_emu = cbscanline;
						if (cbscanline == -2)
							gb.SetScanlineCallback(null, 0);
						else
							gb.SetScanlineCallback(ScanlineCallback, cbscanline);
					}
				}
			}
		}

		#endregion

		#region mouseovers

		string freeze_label;
		Bitmap freeze_bmp;
		string freeze_details;

		void SaveDetails()
		{
			freeze_label = groupBoxDetails.Text;
			if (freeze_bmp != null)
				freeze_bmp.Dispose();
			freeze_bmp = (Bitmap)bmpViewDetails.bmp.Clone();
			freeze_details = labelDetails.Text;
		}

		void LoadDetails()
		{
			groupBoxDetails.Text = freeze_label;
			bmpViewDetails.Height = freeze_bmp.Height * 8;
			bmpViewDetails.ChangeBitmapSize(freeze_bmp.Size);
			using (var g = Graphics.FromImage(bmpViewDetails.bmp))
				g.DrawImageUnscaled(freeze_bmp, 0, 0);
			labelDetails.Text = freeze_details;
			bmpViewDetails.Refresh();
		}

		void SetFreeze()
		{
			groupBoxMemory.Text = groupBoxDetails.Text;
			bmpViewMemory.Size = bmpViewDetails.Size;
			bmpViewMemory.ChangeBitmapSize(bmpViewDetails.bmp.Size);
			using (var g = Graphics.FromImage(bmpViewMemory.bmp))
				g.DrawImageUnscaled(bmpViewDetails.bmp, 0, 0);
			labelMemory.Text = labelDetails.Text;
			bmpViewMemory.Refresh();
		}

		unsafe void PaletteMouseover(int x, int y, bool sprite)
		{
			bmpViewDetails.ChangeBitmapSize(8, 10);
			if (bmpViewDetails.Height != 80)
				bmpViewDetails.Height = 80;
			var sb = new StringBuilder();
			x /= 16;
			y /= 16;
			int *pal = (int*)(sprite ? sppal : bgpal) + x * 4;
			int color = pal[y];

			sb.AppendLine(string.Format("Palette {0}", x));
			sb.AppendLine(string.Format("Color {0}", y));
			sb.AppendLine(string.Format("(R,G,B) = ({0},{1},{2})", color >> 16 & 255, color >> 8 & 255, color & 255));

			var lockdata = bmpViewDetails.bmp.LockBits(new Rectangle(0, 0, 8, 10), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			int* dest = (int*)lockdata.Scan0;
			int pitch = lockdata.Stride / sizeof(int);
			
			for (int py = 0; py < 10; py++)
			{
				for (int px = 0; px < 8; px++)
				{
					if (py < 8)
						*dest++ = color;
					else
						*dest++ = pal[px / 2];
				}
				dest -= 8;
				dest += pitch;
			}
			bmpViewDetails.bmp.UnlockBits(lockdata);
			labelDetails.Text = sb.ToString();
			bmpViewDetails.Refresh();
		}

		unsafe void TileMouseover(int x, int y, bool secondbank)
		{
			// todo: draw with a specific palette
			bmpViewDetails.ChangeBitmapSize(8, 8);
			if (bmpViewDetails.Height != 64)
				bmpViewDetails.Height = 64;
			var sb = new StringBuilder();
			x /= 8;
			y /= 8;
			int tileindex = y * 16 + x;
			int tileoffs = tileindex * 16;
			if (cgb)
				sb.AppendLine(string.Format("Tile #{0} @{2}:{1:x4}", tileindex, tileoffs + 0x8000, secondbank ? 1 : 0));
			else
				sb.AppendLine(string.Format("Tile #{0} @{1:x4}", tileindex, tileoffs + 0x8000));

			var lockdata = bmpViewDetails.bmp.LockBits(new Rectangle(0, 0, 8, 8), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			DrawTile((byte*)vram + tileoffs + (secondbank ? 8192 : 0), (int*)lockdata.Scan0, lockdata.Stride / sizeof(int), (int*)tilespal);
			bmpViewDetails.bmp.UnlockBits(lockdata);
			labelDetails.Text = sb.ToString();
			bmpViewDetails.Refresh();
		}

		unsafe void TilemapMouseover(int x, int y, bool win)
		{
			bmpViewDetails.ChangeBitmapSize(8, 8);
			if (bmpViewDetails.Height != 64)
				bmpViewDetails.Height = 64;
			var sb = new StringBuilder();
			bool secondmap = win ? lcdc.Bit(6) : lcdc.Bit(3);
			int mapoffs = secondmap ? 0x1c00 : 0x1800;
			x /= 8;
			y /= 8;
			mapoffs += y * 32 + x;
			byte *mapbase = (byte *)vram + mapoffs;
			int tileindex = mapbase[0];
			if (win || !lcdc.Bit(4)) // 0x9000 base
				if (tileindex < 128)
					tileindex += 256; // compute all if from 0x8000 base
			int tileoffs = tileindex * 16;
			var lockdata = bmpViewDetails.bmp.LockBits(new Rectangle(0, 0, 8, 8), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			if (!cgb)
			{
				sb.AppendLine(string.Format("{0} Map ({1},{2}) @{3:x4}", win ? "Win" : "BG", x, y, mapoffs + 0x8000));
				sb.AppendLine(string.Format("  Tile #{0} @{1:x4}", tileindex, tileoffs + 0x8000));
				DrawTile((byte*)vram + tileoffs, (int*)lockdata.Scan0, lockdata.Stride / sizeof(int), (int*)bgpal);
			}
			else
			{
				int tileext = mapbase[8192];

				sb.AppendLine(string.Format("{0} Map ({1},{2}) @{3:x4}", win ? "Win" : "BG", x, y, mapoffs + 0x8000));
				sb.AppendLine(string.Format("  Tile #{0} @{2}:{1:x4}", tileindex, tileoffs + 0x8000, tileext.Bit(3) ? 1 : 0));
				sb.AppendLine(string.Format("  Palette {0}", tileext & 7));
				sb.AppendLine(string.Format("  Flags {0}{1}{2}", tileext.Bit(5) ? 'H' : ' ', tileext.Bit(6) ? 'V' : ' ', tileext.Bit(7) ? 'P' : ' '));
				DrawTileHV((byte*)vram + tileoffs + (tileext.Bit(3) ? 8192 : 0), (int*)lockdata.Scan0, lockdata.Stride / sizeof(int), (int*)bgpal + 4 * (tileext & 7), tileext.Bit(5), tileext.Bit(6));
			}
			bmpViewDetails.bmp.UnlockBits(lockdata);
			labelDetails.Text = sb.ToString();
			bmpViewDetails.Refresh();
		}

		unsafe void SpriteMouseover(int x, int y)
		{
			bool tall = lcdc.Bit(2);
			x /= 8;
			y /= 8;
			bmpViewDetails.ChangeBitmapSize(8, tall ? 16 : 8);
			if (bmpViewDetails.Height != bmpViewDetails.bmp.Height * 8)
				bmpViewDetails.Height = bmpViewDetails.bmp.Height * 8;
			var sb = new StringBuilder();

			byte* oament = (byte*)oam + 4 * x;
			int sy = oament[0];
			int sx = oament[1];
			int tilenum = oament[2];
			int flags = oament[3];
			bool hflip = flags.Bit(5);
			bool vflip = flags.Bit(6);
			if (tall)
				tilenum = vflip ? tilenum | 1 : tilenum & ~1;
			int tileoffs = tilenum * 16;
			sb.AppendLine(string.Format("Sprite #{0} @{1:x4}", x, 4 * x + 0xfe00));
			sb.AppendLine(string.Format("  (x,y) = ({0},{1})", sx, sy));
			var lockdata = bmpViewDetails.bmp.LockBits(new Rectangle(0, 0, 8, tall ? 16 : 8), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			if (cgb)
			{
				sb.AppendLine(string.Format("  Tile #{0} @{2}:{1:x4}", y == 1 ? tilenum ^ 1 : tilenum, tileoffs + 0x8000, flags.Bit(3) ? 1 : 0));
				sb.AppendLine(string.Format("  Palette {0}", flags & 7));
				DrawTileHV((byte*)vram + tileoffs + (flags.Bit(3) ? 8192 : 0), (int*)lockdata.Scan0, lockdata.Stride / sizeof(int), (int*)sppal + 4 * (flags & 7), hflip, vflip);
				if (tall)
					DrawTileHV((byte*)vram + (tileoffs ^ 16) + (flags.Bit(3) ? 8192 : 0), (int*)(lockdata.Scan0 + lockdata.Stride * 8), lockdata.Stride / sizeof(int), (int*)sppal + 4 * (flags & 7), hflip, vflip);
			}
			else
			{
				sb.AppendLine(string.Format("  Tile #{0} @{1:x4}", y == 1 ? tilenum ^ 1 : tilenum, tileoffs + 0x8000));
				sb.AppendLine(string.Format("  Palette {0}", flags.Bit(4) ? 1 : 0));
				DrawTileHV((byte*)vram + tileoffs, (int*)lockdata.Scan0, lockdata.Stride / sizeof(int), (int*)sppal + (flags.Bit(4) ? 4 : 0), hflip, vflip);
				if (tall)
					DrawTileHV((byte*)vram + (tileoffs ^ 16), (int*)(lockdata.Scan0 + lockdata.Stride * 8), lockdata.Stride / sizeof(int), (int*)sppal + 4 * (flags.Bit(4) ? 4 : 0), hflip, vflip);
			}
			sb.AppendLine(string.Format("  Flags {0}{1}{2}", hflip ? 'H' : ' ', vflip ? 'V' : ' ', flags.Bit(7) ? 'P' : ' '));
			bmpViewDetails.bmp.UnlockBits(lockdata);
			labelDetails.Text = sb.ToString();
			bmpViewDetails.Refresh();
		}

		private void bmpViewBG_MouseEnter(object sender, EventArgs e)
		{
			SaveDetails();
			groupBoxDetails.Text = "Details - Background";
		}

		private void bmpViewBG_MouseLeave(object sender, EventArgs e)
		{
			LoadDetails();
		}

		private void bmpViewBG_MouseMove(object sender, MouseEventArgs e)
		{
			TilemapMouseover(e.X, e.Y, false);
		}

		private void bmpViewWin_MouseEnter(object sender, EventArgs e)
		{
			SaveDetails();
			groupBoxDetails.Text = "Details - Window";
		}

		private void bmpViewWin_MouseLeave(object sender, EventArgs e)
		{
			LoadDetails();
		}

		private void bmpViewWin_MouseMove(object sender, MouseEventArgs e)
		{
			TilemapMouseover(e.X, e.Y, true);
		}

		private void bmpViewTiles1_MouseEnter(object sender, EventArgs e)
		{
			SaveDetails();
			groupBoxDetails.Text = "Details - Tiles";
		}

		private void bmpViewTiles1_MouseLeave(object sender, EventArgs e)
		{
			LoadDetails();
		}

		private void bmpViewTiles1_MouseMove(object sender, MouseEventArgs e)
		{
			TileMouseover(e.X, e.Y, false);
		}

		private void bmpViewTiles2_MouseEnter(object sender, EventArgs e)
		{
			if (!cgb)
				return;
			SaveDetails();
			groupBoxDetails.Text = "Details - Tiles";
		}

		private void bmpViewTiles2_MouseLeave(object sender, EventArgs e)
		{
			if (!cgb)
				return;
			LoadDetails();
		}

		private void bmpViewTiles2_MouseMove(object sender, MouseEventArgs e)
		{
			if (!cgb)
				return;
			TileMouseover(e.X, e.Y, true);
		}

		private void bmpViewBGPal_MouseEnter(object sender, EventArgs e)
		{
			SaveDetails();
			groupBoxDetails.Text = "Details - Palette";
		}

		private void bmpViewBGPal_MouseLeave(object sender, EventArgs e)
		{
			LoadDetails();
		}

		private void bmpViewBGPal_MouseMove(object sender, MouseEventArgs e)
		{
			PaletteMouseover(e.X, e.Y, false);
		}

		private void bmpViewSPPal_MouseEnter(object sender, EventArgs e)
		{
			SaveDetails();
			groupBoxDetails.Text = "Details - Palette";
		}

		private void bmpViewSPPal_MouseLeave(object sender, EventArgs e)
		{
			LoadDetails();
		}

		private void bmpViewSPPal_MouseMove(object sender, MouseEventArgs e)
		{
			PaletteMouseover(e.X, e.Y, true);
		}

		private void bmpViewOAM_MouseEnter(object sender, EventArgs e)
		{
			SaveDetails();
			groupBoxDetails.Text = "Details - Sprite";
		}

		private void bmpViewOAM_MouseLeave(object sender, EventArgs e)
		{
			LoadDetails();
		}

		private void bmpViewOAM_MouseMove(object sender, MouseEventArgs e)
		{
			SpriteMouseover(e.X, e.Y);
		}

		#endregion

		private void bmpView_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Right)
				SetFreeze();
			else if (e.Button == System.Windows.Forms.MouseButtons.Left)
			{
				if (sender == bmpViewBGPal)
					tilespal = bgpal + e.X / 16 * 16;
				else if (sender == bmpViewSPPal)
					tilespal = sppal + e.X / 16 * 16;
			}
		}

	}
}
