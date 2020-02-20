using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class SpriteViewer : Control
	{
		public Bitmap Sprites { get; set; }

		public SpriteViewer()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			var pSize = new Size(256, 96);
			Sprites = new Bitmap(pSize.Width, pSize.Height);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			Size = pSize;
			BackColor = Color.Transparent;
			Paint += SpriteViewer_Paint;
		}

		private void Display(Graphics g)
		{
			g.DrawImage(Sprites, 1, 1);
		}

		private void SpriteViewer_Paint(object sender, PaintEventArgs e)
		{
			Display(e.Graphics);
		}

		public void Screenshot()
		{
			var sfd = new SaveFileDialog
				{
					FileName = $"{PathManager.FilesystemSafeName(Global.Game)}-Sprites",
					InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries["NES", "Screenshots"].Path, "NES"),
					Filter = FilesystemFilterSet.Screenshots.ToString(),
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
