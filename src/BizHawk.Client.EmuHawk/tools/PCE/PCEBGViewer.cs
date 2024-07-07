using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[SpecializedTool("BG Viewer")]
	public partial class PceBgViewer : ToolFormBase, IToolFormAutoConfig
	{
		public static Icon ToolIcon
			=> Properties.Resources.PceIcon;

		[RequiredService]
		public IPceGpuView Viewer { get; private set; }
		[RequiredService]
		public IEmulator Emulator { get; private set; }

		[ConfigPersist]
		// ReSharper disable once UnusedMember.Local
		private int RefreshRateConfig
		{
			get => RefreshRate.Value;
			set => RefreshRate.Value = Math.Max(Math.Min(value, RefreshRate.Maximum), RefreshRate.Minimum);
		}

		private int _vdcType;

		protected override string WindowTitleStatic => "Background Viewer";

		public PceBgViewer()
		{
			InitializeComponent();
			Icon = ToolIcon;
			Activated += (o, e) => Generate();
		}

		public unsafe void Generate()
		{
			if (Emulator.Frame % RefreshRate.Value != 0)
			{
				return;
			}

			Viewer.GetGpuData(_vdcType, view =>
			{
				var width = 8 * view.BatWidth;
				var height = 8 * view.BatHeight;
				var buf = canvas.Bat.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, canvas.Bat.PixelFormat);
				var pitch = buf.Stride / 4;
				var begin = (int*)buf.Scan0.ToPointer();
				var vram = view.Vram;
				var patternBuffer = view.BackgroundCache;
				var palette = view.PaletteCache;

				int* p = begin;
				for (int y = 0; y < height; ++y)
				{
					int yTile = y / 8;
					int yOfs = y % 8;
					for (int x = 0; x < width; ++x, ++p)
					{
						int xTile = x / 8;
						int xOfs = x % 8;
						int tileNo = vram[(ushort)(((yTile * view.BatWidth) + xTile))] & 0x07FF;
						int paletteNo = vram[(ushort)(((yTile * view.BatWidth) + xTile))] >> 12;
						int paletteBase = paletteNo * 16;

						byte c = patternBuffer[(tileNo * 64) + (yOfs * 8) + xOfs];
						if (c == 0)
						{
							*p = palette[0];
						}
						else
						{
							*p = palette[paletteBase + c];
						}
					}

					p += pitch - width;
				}

				canvas.Bat.UnlockBits(buf);
				canvas.Refresh();
			});
		}

		protected override void UpdateBefore() => Generate();

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			VDC2MenuItem.Enabled = Viewer.IsSgx;

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

		private unsafe void Canvas_MouseMove(object sender, MouseEventArgs e)
		{
			Viewer.GetGpuData(_vdcType, view =>
			{
				var vram = view.Vram;
				int xTile = e.X / 8;
				int yTile = e.Y / 8;
				int tileNo = vram[(ushort)((yTile * view.BatWidth) + xTile)] & 0x07FF;
				int paletteNo = vram[(ushort)((yTile * view.BatWidth) + xTile)] >> 12;
				TileIDLabel.Text = tileNo.ToString();
				XYLabel.Text = $"{xTile}:{yTile}";
				PaletteLabel.Text = paletteNo.ToString();
			});
		}
	}
}
