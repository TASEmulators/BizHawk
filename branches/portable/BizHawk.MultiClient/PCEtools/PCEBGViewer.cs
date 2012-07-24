using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using BizHawk.Emulation.Consoles.TurboGrafx;

namespace BizHawk.MultiClient
{
	public partial class PCEBGViewer : Form
	{
		PCEngine pce;
		int VDCtype = 0;
		int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
		int defaultHeight;

		public PCEBGViewer()
		{
			InitializeComponent();
			Activated += (o, e) => Generate();
			Closing += (o, e) => SaveConfigSettings();
		}

		public unsafe void Generate()
		{
			if (Global.Emulator.Frame % RefreshRate.Value != 0) return;

			VDC vdc = VDCtype == 0 ? pce.VDC1 : pce.VDC2;

			int width = 8 * vdc.BatWidth;
			int height = 8 * vdc.BatHeight;
			BitmapData buf = canvas.bat.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, canvas.bat.PixelFormat);
			int pitch = buf.Stride / 4;
			int* begin = (int*)buf.Scan0.ToPointer();
			int* p = begin;

			// TODO: this does not clear background, why?
			//for (int i = 0; i < pitch * buf.Height; ++i, ++p)
			//	*p = canvas.BackColor.ToArgb();

			p = begin;
			for (int y = 0; y < height; ++y)
			{
				int yTile = y / 8;
				int yOfs = y % 8;
				for (int x = 0; x < width; ++x, ++p)
				{
					int xTile = x / 8;
					int xOfs = x % 8;
					int tileNo = vdc.VRAM[(ushort)(((yTile * vdc.BatWidth) + xTile))] & 0x07FF;
					int paletteNo = vdc.VRAM[(ushort)(((yTile * vdc.BatWidth) + xTile))] >> 12;
					int paletteBase = paletteNo * 16;

					byte c = vdc.PatternBuffer[(tileNo * 64) + (yOfs * 8) + xOfs];
					if (c == 0)
						*p = pce.VCE.Palette[0];
					else
					{
						*p = pce.VCE.Palette[paletteBase + c];
					}
				}
				p += pitch - width;
			}

			canvas.bat.UnlockBits(buf);
			canvas.Refresh();
		}

		public void Restart()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;
			if (!(Global.Emulator is PCEngine))
			{
				this.Close();
				return;
			}
			pce = Global.Emulator as PCEngine;
		}

		public void UpdateValues()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;
			if (!(Global.Emulator is PCEngine)) return;
			Generate();
		}

		private void SaveConfigSettings()
		{
			Global.Config.PCEBGViewerWndx = this.Location.X;
			Global.Config.PCEBGViewerWndy = this.Location.Y;
			Global.Config.PCEBGViewerRefreshRate = RefreshRate.Value;
		}

		private void LoadConfigSettings()
		{
			defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = Size.Height;

			if (Global.Config.PCEBGViewerSaveWIndowPosition && Global.Config.PCEBGViewerWndx >= 0 && Global.Config.PCEBGViewerWndy >= 0)
				Location = new Point(Global.Config.PCEBGViewerWndx, Global.Config.PCEBGViewerWndy);
		}

		private void PCEBGViewer_Load(object sender, EventArgs e)
		{
			pce = Global.Emulator as PCEngine;
			LoadConfigSettings();
			if (Global.Config.PCEBGViewerRefreshRate >= RefreshRate.Minimum && Global.Config.PCEBGViewerRefreshRate <= RefreshRate.Maximum)
			{
				RefreshRate.Value = Global.Config.PCEBGViewerRefreshRate;
			}
			else
			{
				RefreshRate.Value = RefreshRate.Maximum;
			}
		}

		private void PCEBGViewer_FormClosed(object sender, FormClosedEventArgs e)
		{

		}

		private void vdcComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			Generate();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.PCEBGViewerSaveWIndowPosition ^= true;
		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.PCEBGViewerAutoload ^= true;
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.PCEBGViewerSaveWIndowPosition;
			autoloadToolStripMenuItem.Checked = Global.Config.PCEBGViewerAutoload;
		}

		private void vDC1ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			VDCtype = 0;
		}

		private void vCD2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			VDCtype = 1;
		}

		private void fileToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (pce.SystemId == "SGX")
				vCD2ToolStripMenuItem.Enabled = true;
			else
				vCD2ToolStripMenuItem.Enabled = false;
			
			switch (VDCtype)
			{
				default:
				case 0:
					vDC1ToolStripMenuItem.Checked = true;
					vCD2ToolStripMenuItem.Checked = false;
					break;
				case 1:
					vDC1ToolStripMenuItem.Checked = false;
					vCD2ToolStripMenuItem.Checked = true;
					break;
			}
		}

		private void canvas_MouseMove(object sender, MouseEventArgs e)
		{
			VDC vdc = VDCtype == 0 ? pce.VDC1 : pce.VDC2;
			int xTile = e.X / 8;
			int yTile = e.Y / 8;
			int tileNo = vdc.VRAM[(ushort)(((yTile * vdc.BatWidth) + xTile))] & 0x07FF;
			int paletteNo = vdc.VRAM[(ushort)(((yTile * vdc.BatWidth) + xTile))] >> 12;
			TileIDLabel.Text = tileNo.ToString();
			XYLabel.Text = xTile.ToString() + ":" + yTile.ToString();
			PaletteLabel.Text = paletteNo.ToString();
		}
	}
}
