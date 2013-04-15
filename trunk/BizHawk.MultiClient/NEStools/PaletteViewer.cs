using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;

namespace BizHawk.MultiClient
{
	public sealed class PaletteViewer : Control
	{
		public class Palette
		{
			public int Address { get; private set; }
			public int Value { get; set; }
			public Color Color
			{
				get { return Color.FromArgb(Value); }
			}

			public Palette(int address)
			{
				Address = address;
				Value = -1;
			}
		}

		public Palette[] BgPalettes = new Palette[16];
		public Palette[] SpritePalettes = new Palette[16];

		public Palette[] BgPalettesPrev = new Palette[16];
		public Palette[] SpritePalettesPrev = new Palette[16];

		public PaletteViewer()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			Size = new Size(128, 32);
			BackColor = Color.Transparent;
			Paint += PaletteViewer_Paint;

			for (int x = 0; x < 16; x++)
			{
				BgPalettes[x] = new Palette(x);
				SpritePalettes[x] = new Palette(x + 16);
				BgPalettesPrev[x] = new Palette(x);
				SpritePalettesPrev[x] = new Palette(x + 16);
			}

		}

		private void PaletteViewer_Paint(object sender, PaintEventArgs e)
		{
			for (int x = 0; x < 16; x++)
			{
				e.Graphics.FillRectangle(new SolidBrush(BgPalettes[x].Color), new Rectangle(x * 16, 0, 16, 16));
				e.Graphics.FillRectangle(new SolidBrush(SpritePalettes[x].Color), new Rectangle(x * 16, 16, 16, 16));
			}
		}

		public bool HasChanged()
		{
			for (int x = 0; x < 16; x++)
			{
				if (BgPalettes[x].Value != BgPalettesPrev[x].Value) 
					return true;
				if (SpritePalettes[x].Value != SpritePalettesPrev[x].Value) 
					return true;
			}
			return false;
		}

		public void Screenshot()
		{
			var sfd = new SaveFileDialog
				{
					FileName = PathManager.FilesystemSafeName(Global.Game) + "-Palettes",
					InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathNESScreenshots, "NES"),
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
