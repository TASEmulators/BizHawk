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
	public class PaletteViewer : Control
	{
		public class Palette
		{
			public int Address { get; private set; }
			public int Value { get; set; }
			public Color Color { get { return Color.FromArgb(Value); } private set { Value = value.ToArgb(); } }

			public Palette(int address)
			{
				Address = address;
				Value = -1;
			}
		}

		public Palette[] bgPalettes = new Palette[16];
		public Palette[] spritePalettes = new Palette[16];

		public Palette[] bgPalettesPrev = new Palette[16];
		public Palette[] spritePalettesPrev = new Palette[16];

		public PaletteViewer()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			this.Size = new Size(128, 32);
			this.BackColor = Color.Transparent;
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.PaletteViewer_Paint);

			for (int x = 0; x < 16; x++)
			{
				bgPalettes[x] = new Palette(x);
				spritePalettes[x] = new Palette(x + 16);
				bgPalettesPrev[x] = new Palette(x);
				spritePalettesPrev[x] = new Palette(x + 16);
			}

		}

		private void PaletteViewer_Paint(object sender, PaintEventArgs e)
		{
			for (int x = 0; x < 16; x++)
			{
				e.Graphics.FillRectangle(new SolidBrush(bgPalettes[x].Color), new Rectangle(x * 16, 0, 16, 16));
				e.Graphics.FillRectangle(new SolidBrush(spritePalettes[x].Color), new Rectangle(x * 16, 16, 16, 16));
			}
		}

		public bool HasChanged()
		{
			for (int x = 0; x < 16; x++)
			{
				if (bgPalettes[x].Value != bgPalettesPrev[x].Value) 
					return true;
				if (spritePalettes[x].Value != spritePalettesPrev[x].Value) 
					return true;
			}
			return false;
		}

		public void Screenshot()
		{
			var sfd = new SaveFileDialog();
			sfd.FileName = PathManager.FilesystemSafeName(Global.Game) + "-Palettes";
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
