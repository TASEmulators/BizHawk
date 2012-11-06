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
			}
			else
			{
				gb = null;
				if (Visible)
					Close();
			}
		}

		/// <summary>
		/// 0..153: scanline number. -1: frame.  -2: manual
		/// </summary>
		int cbscanline;
		/// <summary>
		/// what was last passed to the emu core
		/// </summary>
		int cbscanline_emu = -4;

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
			DrawTiles(bmpViewTiles1.bmp, vram, bgpal);
			bmpViewTiles1.Refresh();
			if (cgb)
			{
				DrawTiles(bmpViewTiles2.bmp, vram + 0x2000, bgpal);
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
	}
}
