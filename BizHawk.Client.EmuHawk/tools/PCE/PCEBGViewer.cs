using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.PCEngine;

namespace BizHawk.Client.EmuHawk
{
	public partial class PceBgViewer : Form, IToolForm
	{
		private PCEngine _pce;
		private int _vdcType;

		public PceBgViewer()
		{
			InitializeComponent();
			TopMost = Global.Config.PceBgViewerSettings.TopMost;
			Activated += (o, e) => Generate();
			Closing += (o, e) =>
				{
					Global.Config.PceBgViewerSettings.Wndx = Location.X;
					Global.Config.PceBgViewerSettings.Wndy = Location.Y;
					Global.Config.PCEBGViewerRefreshRate = RefreshRate.Value;
				};
		}

		private void PceBgViewer_Load(object sender, EventArgs e)
		{
			_pce = Global.Emulator as PCEngine;

			if (Global.Config.PceBgViewerSettings.UseWindowPosition)
			{
				Location = Global.Config.PceBgViewerSettings.WindowPosition;
			}

			if (Global.Config.PCEBGViewerRefreshRate >= RefreshRate.Minimum && Global.Config.PCEBGViewerRefreshRate <= RefreshRate.Maximum)
			{
				RefreshRate.Value = Global.Config.PCEBGViewerRefreshRate;
			}
			else
			{
				RefreshRate.Value = RefreshRate.Maximum;
			}
		}

		private void RefreshFloatingWindowControl()
		{
			Owner = Global.Config.PceBgViewerSettings.FloatingWindow ? null : GlobalWin.MainForm;
		}

		#region Public API

		public bool AskSaveChanges() { return true; }
		public bool UpdateBefore { get { return true; } }

		public unsafe void Generate()
		{
			if (Global.Emulator.Frame % RefreshRate.Value != 0)
			{
				return;
			}

			var vdc = _vdcType == 0 ? _pce.VDC1 : _pce.VDC2;

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
						*p = _pce.VCE.Palette[0];
					}
					else
					{
						*p = _pce.VCE.Palette[paletteBase + c];
					}
				}

				p += pitch - width;
			}

			canvas.Bat.UnlockBits(buf);
			canvas.Refresh();
		}

		public void Restart()
		{
			if (Global.Emulator is PCEngine)
			{
				_pce = Global.Emulator as PCEngine;
			}
			else
			{
				Close();
			}
		}

		public void UpdateValues()
		{
			if (Global.Emulator is PCEngine)
			{
				Generate();
			}
			else
			{
				Close();
			}
		}

		public void FastUpdate()
		{
			// Do nothing
		}

		#endregion

		#region Events

		#region Menu

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			VDC2MenuItem.Enabled = _pce.SystemId == "SGX";

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

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveWindowPositionMenuItem.Checked = Global.Config.PceBgViewerSettings.SaveWindowPosition;
			AutoloadMenuItem.Checked = Global.Config.PCEBGViewerAutoload;
			AlwaysOnTopMenuItem.Checked = Global.Config.PceBgViewerSettings.TopMost;
			FloatingWindowMenuItem.Checked = Global.Config.PceBgViewerSettings.FloatingWindow;
		}

		private void AutoloadMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.PCEBGViewerAutoload ^= true;
		}

		private void SaveWindowPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.PceBgViewerSettings.SaveWindowPosition ^= true;
		}

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.PceBgViewerSettings.TopMost ^= true;
			TopMost = Global.Config.PceBgViewerSettings.TopMost;
		}

		private void FloatingWindowMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.PceBgViewerSettings.FloatingWindow ^= true;
			RefreshFloatingWindowControl();
		}

		#endregion

		#region Dialog and Controls

		private void Canvas_MouseMove(object sender, MouseEventArgs e)
		{
			var vdc = _vdcType == 0 ? _pce.VDC1 : _pce.VDC2;
			int xTile = e.X / 8;
			int yTile = e.Y / 8;
			int tileNo = vdc.VRAM[(ushort)((yTile * vdc.BatWidth) + xTile)] & 0x07FF;
			int paletteNo = vdc.VRAM[(ushort)((yTile * vdc.BatWidth) + xTile)] >> 12;
			TileIDLabel.Text = tileNo.ToString();
			XYLabel.Text = xTile + ":" + yTile;
			PaletteLabel.Text = paletteNo.ToString();
		}

		protected override void OnShown(EventArgs e)
		{
			RefreshFloatingWindowControl();
			base.OnShown(e);
		}

		#endregion

		#endregion
	}
}
