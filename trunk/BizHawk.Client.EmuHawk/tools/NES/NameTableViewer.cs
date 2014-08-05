using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;
using System.Text;
using System;
using BizHawk.Client.EmuHawk.CustomControls;

namespace BizHawk.Client.EmuHawk
{
	public sealed class NameTableViewer : Control
	{
		public Bitmap Nametables;

		private readonly Size pSize;

		public NameTableViewer()
		{
			pSize = new Size(512, 480);
			Nametables = new Bitmap(pSize.Width, pSize.Height);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			//SetStyle(ControlStyles.Opaque, true);
			Size = new Size(256, 224);
			BackColor = Color.Transparent;
			//Paint += NameTableViewer_Paint;
		}

		public enum WhichNametable
		{
			NT_2000, NT_2400, NT_2800, NT_2C00, NT_ALL, TOPS, BOTTOMS
		}

		public WhichNametable Which = WhichNametable.NT_ALL;

		protected override void OnPaint(PaintEventArgs e)
		{
			
			using (var ntr = new NativeTextRenderer(e.Graphics))
			{
				for (int y = 0; y < 16; y++)
				{
					StringBuilder sb = new StringBuilder();
					Random r = new Random((int)DateTime.Now.Ticks);
					for (int i = 0; i < 64; i++)
					{
						sb.Append((char)r.Next(0, 255));
					}

					ntr.DrawString(sb.ToString(), this.Font, Color.Black, new Point(15, y * 30));
					//e.Graphics.DrawString(sb.ToString(), this.Font, Brushes.Black, new Point(15, y * 30));
				}
			}
			
			/*
			for (int y = 0; y < 16; y++)
			{
				StringBuilder sb = new StringBuilder();
				Random r = new Random((int)DateTime.Now.Ticks);
				for (int i = 0; i < 64; i++)
				{
					sb.Append((char)r.Next(0, 255));
				}

				e.Graphics.DrawString(sb.ToString(), this.Font, Brushes.Black, new Point(15, y * 30));
			}
			 */
			//base.OnPaint(e);
		}

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
			var sfd = new SaveFileDialog
				{
					FileName = PathManager.FilesystemSafeName(Global.Game) + "-Nametables",
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
				Clipboard.SetImage(b);
			}
		}
	}
}
