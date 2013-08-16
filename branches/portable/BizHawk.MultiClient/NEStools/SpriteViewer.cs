using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;

namespace BizHawk.MultiClient
{
	public sealed class SpriteViewer : Control
	{
		public Bitmap sprites;

		private readonly Size pSize;

		public SpriteViewer()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			pSize = new Size(256, 96);
			sprites = new Bitmap(pSize.Width, pSize.Height);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			Size = pSize;
			BackColor = Color.Transparent;
			Paint += SpriteViewer_Paint;
		}

		private void Display(Graphics g)
		{
			unchecked
			{
				g.DrawImage(sprites, 1, 1);
			}
		}

		private void SpriteViewer_Paint(object sender, PaintEventArgs e)
		{
			Display(e.Graphics);
		}

		public void Screenshot()
		{
			var sfd = new SaveFileDialog
				{
					FileName = PathManager.FilesystemSafeName(Global.Game) + "-Sprites",
					InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries["NES", "Screenshots"].Path, "NES"),
					Filter = "PNG (*.png)|*.png|Bitmap (*.bmp)|*.bmp|All Files|*.*",
					RestoreDirectory = true
				};

			Global.Sound.StopSound();
			var result = sfd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return;

			var file = new FileInfo(sfd.FileName);
			Bitmap b = new Bitmap(Width, Height);
			Rectangle rect = new Rectangle(new Point(0, 0), Size);
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
			Bitmap b = new Bitmap(Width, Height);
			Rectangle rect = new Rectangle(new Point(0, 0), Size);
			DrawToBitmap(b, rect);

			using (var img = b)
			{
				Clipboard.SetImage(img);
			}
		}
	}
}
