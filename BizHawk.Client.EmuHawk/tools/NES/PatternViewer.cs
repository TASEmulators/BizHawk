using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	public sealed class PatternViewer : Control
	{
		public Bitmap Pattern { get; set; }
		public int Pal0 { get; set; } = 0; // 0-7 Palette choice
		public int Pal1 { get; set; } = 0;

		public PatternViewer()
		{
			var pSize = new Size(256, 128);
			Pattern = new Bitmap(pSize.Width, pSize.Height);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			Size = pSize;
			BackColor = Color.Transparent;
			Paint += PatternViewer_Paint;
		}

		private void PatternViewer_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.DrawImage(Pattern, 0, 0);
		}

		public void Screenshot()
		{
			var sfd = new SaveFileDialog
				{
					FileName = $"{PathManager.FilesystemSafeName(Global.Game)}-Patterns",
					InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries["NES", "Screenshots"].Path, "NES"),
					Filter = "PNG (*.png)|*.png|Bitmap (*.bmp)|*.bmp|All Files|*.*",
					RestoreDirectory = true
				};

			var result = sfd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return;
			}

			var file = new FileInfo(sfd.FileName);
			var b = new Bitmap(Width, Height);
			var rect = new Rectangle(new Point(0, 0), Size);
			DrawToBitmap(b, rect);

			ImageFormat i;
			string extension = file.Extension.ToUpper();
			switch (extension)
			{
				default:
				case ".PNG":
					i = ImageFormat.Png;
					break;
				case ".BMP":
					i = ImageFormat.Bmp;
					break;
			}

			b.Save(file.FullName, i);
		}

		public void ScreenshotToClipboard()
		{
			var b = new Bitmap(Width, Height);
			var rect = new Rectangle(new Point(0, 0), Size);
			DrawToBitmap(b, rect);

			using var img = b;
			Clipboard.SetImage(img);
		}
	}
}
