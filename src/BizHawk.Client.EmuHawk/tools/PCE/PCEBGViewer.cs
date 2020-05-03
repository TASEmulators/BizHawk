using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class PceBgViewer : ToolFormBase, IToolFormAutoConfig
	{
		[RequiredService]
		private PCEngine PCE { get; set; }

		[ConfigPersist]
		// ReSharper disable once UnusedMember.Local
		private int RefreshRateConfig
		{
			get => RefreshRate.Value;
			set => RefreshRate.Value = Math.Max(Math.Min(value, RefreshRate.Maximum), RefreshRate.Minimum);
		}

		private int _vdcType;

		public PceBgViewer()
		{
			InitializeComponent();
			Activated += (o, e) => Generate();
		}

		public unsafe void Generate()
		{
			if (PCE.Frame % RefreshRate.Value != 0)
			{
				return;
			}

			var vdc = _vdcType == 0 ? PCE.VDC1 : PCE.VDC2;

			var width = 8 * vdc.BatWidth;
			var height = 8 * vdc.BatHeight;
			var buf = canvas.Bat.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, canvas.Bat.PixelFormat);
			var pitch = buf.Stride / 4;
			var begin = (int*)buf.Scan0.ToPointer();

			int* p = begin;
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
					{
						*p = PCE.VCE.Palette[0];
					}
					else
					{
						*p = PCE.VCE.Palette[paletteBase + c];
					}
				}

				p += pitch - width;
			}

			canvas.Bat.UnlockBits(buf);
			canvas.Refresh();
		}

		public void Restart()
		{
			// Nothing to do
		}

		protected override void UpdateValuesBefore()
		{
			// NewUpdate TODO: consolidate
			Generate();
		}

		public void FastUpdate()
		{
			// Do nothing
		}

		#region Events

		#region Menu

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			VDC2MenuItem.Enabled = PCE.SystemId == "SGX";

			VDC1MenuItem.Checked = _vdcType == 0;
			VDC2MenuItem.Checked = _vdcType == 1;
		}

		private void VDC1MenuItem_Click(object sender, EventArgs e)
		{
			_vdcType = 0;
		}

		private void VDC2MenuItem_Click(object sender, EventArgs e)
		{
			_vdcType = 1;
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		private void Canvas_MouseMove(object sender, MouseEventArgs e)
		{
			var vdc = _vdcType == 0 ? PCE.VDC1 : PCE.VDC2;
			int xTile = e.X / 8;
			int yTile = e.Y / 8;
			int tileNo = vdc.VRAM[(ushort)((yTile * vdc.BatWidth) + xTile)] & 0x07FF;
			int paletteNo = vdc.VRAM[(ushort)((yTile * vdc.BatWidth) + xTile)] >> 12;
			TileIDLabel.Text = tileNo.ToString();
			XYLabel.Text = $"{xTile}:{yTile}";
			PaletteLabel.Text = paletteNo.ToString();
		}

		#endregion
	}
}
