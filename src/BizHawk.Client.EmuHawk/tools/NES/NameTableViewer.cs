using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class NameTableViewer : Control
	{
		public Bitmap Nametables;

		public NameTableViewer()
		{
			var pSize = new Size(512, 480);
			Nametables = new Bitmap(pSize.Width, pSize.Height);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			Size = new Size(256, 224);
			BackColor = Color.Transparent;
			Paint += NameTableViewer_Paint;
		}

		public enum WhichNametable
		{
			NT_2000, NT_2400, NT_2800, NT_2C00, NT_ALL, TOPS, BOTTOMS
		}

		public WhichNametable Which = WhichNametable.NT_ALL;

		private void NameTableViewer_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
			switch (Which)
			{
				case WhichNametable.NT_ALL:
					e.Graphics.DrawImageUnscaled(Nametables, 0, 0);
					break;
				case WhichNametable.NT_2000:
					e.Graphics.DrawImage(Nametables, new Rectangle(0, 0, 512, 480), 0, 0, 256, 240, GraphicsUnit.Pixel);
					break;
				case WhichNametable.NT_2400:
					e.Graphics.DrawImage(Nametables, new Rectangle(0, 0, 512, 480), 256, 0, 256, 240, GraphicsUnit.Pixel);
					break;
				case WhichNametable.NT_2800:
					e.Graphics.DrawImage(Nametables, new Rectangle(0, 0, 512, 480), 0, 240, 256, 240, GraphicsUnit.Pixel);
					break;
				case WhichNametable.NT_2C00:
					e.Graphics.DrawImage(Nametables, new Rectangle(0, 0, 512, 480), 256, 240, 256, 240, GraphicsUnit.Pixel);
					break;

				//adelikat: Meh, just in case we might want these, someone requested it but I can't remember the justification so I didn't do the UI part
				case WhichNametable.TOPS:
					e.Graphics.DrawImage(Nametables, new Rectangle(0, 0, 512, 240), 0, 0, 512, 240, GraphicsUnit.Pixel);
					break;
				case WhichNametable.BOTTOMS:
					e.Graphics.DrawImage(Nametables, new Rectangle(0, 240, 512, 240), 0, 240, 512, 240, GraphicsUnit.Pixel);
					break;
			}
		}

		public void Screenshot()
		{
			using var sfd = new SaveFileDialog
			{
				FileName = $"{Global.Game.FilesystemSafeName()}-Nametables",
				InitialDirectory = Global.Config.PathEntries.ScreenshotAbsolutePathFor("NES"),
				Filter = FilesystemFilterSet.Screenshots.ToString(),
				RestoreDirectory = true
			};

			var result = sfd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return;
			}

			var file = new FileInfo(sfd.FileName);
			using Bitmap b = new Bitmap(Width, Height);
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
			using var b = new Bitmap(Width, Height);
			var rect = new Rectangle(new Point(0, 0), Size);
			DrawToBitmap(b, rect);
			Clipboard.SetImage(b);
		}
	}
}
