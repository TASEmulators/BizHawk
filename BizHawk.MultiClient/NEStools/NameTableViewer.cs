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
	public class NameTableViewer : Control
	{
		Size pSize;
		public Bitmap nametables;

		public NameTableViewer()
		{
			pSize = new Size(512, 480);
			nametables = new Bitmap(pSize.Width, pSize.Height);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			this.Size = new Size(256, 224);
			this.BackColor = Color.Transparent;
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.NameTableViewer_Paint);
		}

		public enum WhichNametable
		{
			NT_2000, NT_2400, NT_2800, NT_2C00, NT_ALL, TOPS, BOTTOMS
		}

		public WhichNametable Which = WhichNametable.NT_ALL;

		private void Display(Graphics g)
		{
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
			switch (Which)
			{
				case WhichNametable.NT_ALL:
					g.DrawImageUnscaled(nametables, 1, 1);
					break;
				case WhichNametable.NT_2000:
					g.DrawImage(nametables, new Rectangle(0, 0, 512, 480), 0, 0, 256, 240, GraphicsUnit.Pixel);
					break;
				case WhichNametable.NT_2400:
					g.DrawImage(nametables, new Rectangle(0, 0, 512, 480), 256, 0, 256, 240, GraphicsUnit.Pixel);
					break;
				case WhichNametable.NT_2800:
					g.DrawImage(nametables, new Rectangle(0, 0, 512, 480), 0, 240, 256, 240, GraphicsUnit.Pixel);
					break;
				case WhichNametable.NT_2C00:
					g.DrawImage(nametables, new Rectangle(0, 0, 512, 480), 256, 240, 256, 240, GraphicsUnit.Pixel);
					break;

				//adelikat: Meh, just in case we might want these, someone requested it but I can't remember the justification so I didn't do the UI part
				case WhichNametable.TOPS:
					g.DrawImage(nametables, new Rectangle(0, 0, 512, 240), 0, 0, 512, 240, GraphicsUnit.Pixel);
					break;
				case WhichNametable.BOTTOMS:
					g.DrawImage(nametables, new Rectangle(0, 240, 512, 240), 0, 240, 512, 240, GraphicsUnit.Pixel);
					break;
			}
		}

		private void NameTableViewer_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
			switch (Which)
			{
				case WhichNametable.NT_ALL:
					e.Graphics.DrawImageUnscaled(nametables, 0, 0);
					break;
				case WhichNametable.NT_2000:
					e.Graphics.DrawImage(nametables, new Rectangle(0, 0, 512, 480), 0, 0, 256, 240, GraphicsUnit.Pixel);
					break;
				case WhichNametable.NT_2400:
					e.Graphics.DrawImage(nametables, new Rectangle(0, 0, 512, 480), 256, 0, 256, 240, GraphicsUnit.Pixel);
					break;
				case WhichNametable.NT_2800:
					e.Graphics.DrawImage(nametables, new Rectangle(0, 0, 512, 480), 0, 240, 256, 240, GraphicsUnit.Pixel);
					break;
				case WhichNametable.NT_2C00:
					e.Graphics.DrawImage(nametables, new Rectangle(0, 0, 512, 480), 256, 240, 256, 240, GraphicsUnit.Pixel);
					break;

				//adelikat: Meh, just in case we might want these, someone requested it but I can't remember the justification so I didn't do the UI part
				case WhichNametable.TOPS:
					e.Graphics.DrawImage(nametables, new Rectangle(0, 0, 512, 240), 0, 0, 512, 240, GraphicsUnit.Pixel);
					break;
				case WhichNametable.BOTTOMS:
					e.Graphics.DrawImage(nametables, new Rectangle(0, 240, 512, 240), 0, 240, 512, 240, GraphicsUnit.Pixel);
					break;
			}
		}

		public void Screenshot()
		{
			var sfd = new SaveFileDialog();
			sfd.FileName = PathManager.FilesystemSafeName(Global.Game) + "-Nametables";
			sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathNESScreenshots, "NES");
			sfd.Filter = "PNG (*.png)|*.png|Bitmap (*.bmp)|*.bmp|All Files|*.*";

			sfd.RestoreDirectory = true;
			Global.Sound.StopSound();
			var result = sfd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return;

			var file = new FileInfo(sfd.FileName);
			using (Bitmap b = new Bitmap(Width, Height))
			{
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
		}

		public void ScreenshotToClipboard()
		{
			using(var b = new Bitmap(Width, Height))
			{
				Rectangle rect = new Rectangle(new Point(0, 0), Size);
				DrawToBitmap(b, rect);
				System.Windows.Forms.Clipboard.SetImage(b);
			}
		}
	}
}
