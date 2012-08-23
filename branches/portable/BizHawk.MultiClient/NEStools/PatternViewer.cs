using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.IO;
using System.Drawing.Imaging;

namespace BizHawk.MultiClient
{
	public class PatternViewer : Control
	{
		Size pSize;
		public Bitmap pattern;
		public int Pal0 = 0; //0-7 Palette choice
		public int Pal1 = 0;

		public PatternViewer()
		{
			pSize = new Size(256, 128);
			pattern = new Bitmap(pSize.Width, pSize.Height);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			this.Size = pSize;
			this.BackColor = Color.Transparent;
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.PatternViewer_Paint);
		}

		private void PatternViewer_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.DrawImage(pattern, 0, 0);
		}

		public void Screenshot()
		{
			var sfd = new SaveFileDialog();
			sfd.FileName = PathManager.FilesystemSafeName(Global.Game) + "-Patterns";
			sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathNESScreenshots, "NES");
			sfd.Filter = "PNG (*.png)|*.png|Bitmap (*.bmp)|*.bmp|All Files|*.*";

			sfd.RestoreDirectory = true;
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
				System.Windows.Forms.Clipboard.SetImage(img);
			}
		}
	}
}
