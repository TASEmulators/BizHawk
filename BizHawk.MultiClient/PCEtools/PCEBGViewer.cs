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

		public PCEBGViewer()
		{
			InitializeComponent();
			vdcComboBox.Items.Add("VDC1");
			vdcComboBox.Items.Add("VDC2");
			vdcComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			vdcComboBox.SelectedIndex = 0;
			Activated += (o, e) => Generate();
		}

		public unsafe void Generate()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;
			if (pce == null) return;

			if (Global.Emulator.Frame % 20 != 0) return; // TODO: just a makeshift. hard-coded 3fps

			VDC vdc = vdcComboBox.SelectedIndex == 0 ? pce.VDC1 : pce.VDC2;

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
			vdcComboBox.SelectedIndex = 0;
			vdcComboBox.Enabled = pce.SystemId == "SGX";
		}

		public void UpdateValues()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;
			if (!(Global.Emulator is PCEngine)) return;

			
		}

		private void PCEBGViewer_Load(object sender, EventArgs e)
		{
			pce = Global.Emulator as PCEngine;
			vdcComboBox.SelectedIndex = 0;
			vdcComboBox.Enabled = pce.SystemId == "SGX";
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
	}
}
