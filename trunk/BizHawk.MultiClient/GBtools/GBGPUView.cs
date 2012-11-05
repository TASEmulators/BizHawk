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

		public GBGPUView()
		{
			InitializeComponent();

		}

		public void Restart()
		{
			if (Global.Emulator is Emulation.Consoles.GB.Gameboy)
			{
				gb = Global.Emulator as Emulation.Consoles.GB.Gameboy;
			}
			else
			{
				this.Close();
			}
		}

		/// <summary>
		/// put me in ToolsBefore
		/// </summary>
		public void UpdateValues()
		{
			if (!this.IsHandleCreated || this.IsDisposed)
				return;
			if (gb != null)
				if (this.Visible)
					gb.SetScanlineCallback(ScanlineCallback, 0);
				else
					gb.SetScanlineCallback(null, 0);
		}

		static unsafe void DrawTileDMG(byte* tile, int* dest, int pitch, int *pal)
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

		static unsafe void DrawTileCGB(byte* tile, int* dest, int pitch, int* pal, bool hflip, bool vflip)
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

		static unsafe void DrawBGCGB(Bitmap b, IntPtr _map, IntPtr _tiles, bool wrap, IntPtr _pal)
		{
			if (b.Width != 256 || b.Height != 256)
				throw new Exception("GPUView screwed up.");

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

					DrawTileCGB(tile, dest, pitch, thispal, tileext.Bit(5), tileext.Bit(6));
					map++;
					dest += 8;
				}
				dest -= 256;
				dest += pitch * 8;
			}
			b.UnlockBits(lockdata);
		}

		static unsafe void DrawBGDMG(Bitmap b, IntPtr _map, IntPtr _tiles, bool wrap, IntPtr _pal)
		{
			if (b.Width != 256 || b.Height != 256)
				throw new Exception("GPUView screwed up.");

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
					DrawTileDMG(tile, dest, pitch, pal);
					map++;
					dest += 8;
				}
				dest -= 256;
				dest += pitch * 8;
			}
			b.UnlockBits(lockdata);
		}

		void ScanlineCallback(IntPtr vram, bool cgb, int lcdc, IntPtr bgpal, IntPtr sppal)
		{
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
			gb = Global.Emulator as Emulation.Consoles.GB.Gameboy;
		}
	}
}
