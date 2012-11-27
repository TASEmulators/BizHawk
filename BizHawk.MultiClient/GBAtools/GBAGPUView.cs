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

		MobileBmpView bg0, bg1, bg2, bg3, bgpal, sppal;

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
				
		unsafe void DrawTile16(int* dest, int pitch, byte* tile, ushort *palette, bool hflip, bool vflip)
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

			ushort *nametable = (ushort*)vram + ((bgcnt & 0x1f00) << 2);

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
			
			ushort *frame = (ushort*)vram;

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
					break;
				case 1:
					if (bg0.ShouldDraw) DrawTextBG(0, bg0);
					if (bg1.ShouldDraw) DrawTextBG(1, bg1);
					if (bg2.ShouldDraw) DrawAffineBG(2, bg2);
					if (bg3.ShouldDraw) bg3.bmpView.Clear();
					break;
				case 2:
					if (bg0.ShouldDraw) bg0.bmpView.Clear();
					if (bg1.ShouldDraw) bg1.bmpView.Clear();
					if (bg2.ShouldDraw) DrawAffineBG(2, bg2);
					if (bg3.ShouldDraw) DrawAffineBG(3, bg3);
					break;
				case 3:
					if (bg0.ShouldDraw) bg0.bmpView.Clear();
					if (bg1.ShouldDraw) bg1.bmpView.Clear();
					if (bg2.ShouldDraw) DrawM3BG(bg2);
					if (bg3.ShouldDraw) bg3.bmpView.Clear();
					break;
				//in modes 4, 5, bg3 is repurposed as bg2 invisible frame
				case 4:
					if (bg0.ShouldDraw) bg0.bmpView.Clear();
					if (bg1.ShouldDraw) bg1.bmpView.Clear();
					if (bg2.ShouldDraw) DrawM4BG(bg2, dispcnt.Bit(4));
					if (bg3.ShouldDraw) DrawM4BG(bg3, !dispcnt.Bit(4));
					break;
				case 5:
					if (bg0.ShouldDraw) bg0.bmpView.Clear();
					if (bg1.ShouldDraw) bg1.bmpView.Clear();
					if (bg2.ShouldDraw) DrawM5BG(bg2, dispcnt.Bit(4));
					if (bg3.ShouldDraw) DrawM5BG(bg3, !dispcnt.Bit(4));
					break;
			}

			if (bgpal.ShouldDraw) DrawPalette(bgpal, false);
			if (sppal.ShouldDraw) DrawPalette(sppal, true);

		}

		MobileBmpView MakeWidget(string text, int w, int h)
		{
			var mbv = new MobileBmpView();
			mbv.Text = text;
			mbv.TopLevel = false;
			mbv.ChangeViewSize(w, h);
			mbv.bmpView.Clear();
			mbv.FormClosing += delegate(object sender, FormClosingEventArgs e)
			{
				e.Cancel = true;
				listBoxWidgets.Items.Add(sender);
				(sender as Form).Hide();
			};
			panel1.Controls.Add(mbv);
			listBoxWidgets.Items.Add(mbv);
			return mbv;
		}

		void GenerateWidgets()
		{
			listBoxWidgets.BeginUpdate();
			bg0 = MakeWidget("Background 0", 256, 256);
			bg1 = MakeWidget("Background 1", 256, 256);
			bg2 = MakeWidget("Background 2", 256, 256);
			bg3 = MakeWidget("Background 3", 256, 256);
			bgpal = MakeWidget("Background Palettes", 256, 256);
			sppal = MakeWidget("Sprite Palettes", 256, 256);
			listBoxWidgets.EndUpdate();
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
				DrawEverything();
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
				(listBoxWidgets.SelectedItem as MobileBmpView).Show();
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
	}
}
