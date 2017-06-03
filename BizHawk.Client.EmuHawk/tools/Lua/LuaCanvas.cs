using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	[Description("Represents a canvas object returned by the gui.createcanvas() method")]
	public partial class LuaCanvas : Form
	{
		private readonly Graphics _graphics;

		public LuaCanvas(int width, int height)
		{
			InitializeComponent();
			pictureBox.Width = width;
			pictureBox.Height = height;
			pictureBox.Image = new Bitmap(width, height);
			_graphics = Graphics.FromImage(pictureBox.Image);
		}

		[LuaMethodAttributes("SetTitle", "Sets the canvas window title")]
		public void SetTitle(string title)
		{
			Text = title;
		}

		[LuaMethodAttributes("Clear", "Clears the canvas")]
		public void Clear(Color color)
		{
			_graphics.Clear(color);
		}

		[LuaMethodAttributes("Refresh", "Redraws the canvas")]
		public new void Refresh()
		{
			pictureBox.Refresh();
		}

		[LuaMethodAttributes(
			"DrawRectangle",
			"Draws a rectangle at the given coordinate and the given width and height. Line is the color of the box. Background is the optional fill color")]
		public void DrawRectangle(int x, int y, int width, int height, Color? outline = null, Color? fill = null)
		{
			if (fill.HasValue)
			{
				var brush = new SolidBrush(fill.Value);
				_graphics.FillRectangle(brush, x, y, width, height);
			}

			var pen = new Pen(outline.HasValue ? outline.Value : Color.Black);
			_graphics.DrawRectangle(pen, x, y, width, height);
		}

		[LuaMethodAttributes(
			"DrawText",
			"Draws the given message at the given x,y coordinates and the given color. The default color is white. A fontfamily can be specified and is monospace generic if none is specified (font family options are the same as the .NET FontFamily class). The fontsize default is 12. The default font style is regular. Font style options are regular, bold, italic, strikethrough, underline. Horizontal alignment options are left (default), center, or right. Vertical alignment options are bottom (default), middle, or top. Alignment options specify which ends of the text will be drawn at the x and y coordinates.")]
		public void DrawText(int x, int y, string message, Color? color = null, int? fontsize = null, string fontfamily = null, string fontstyle = null)
		{
			var family = FontFamily.GenericMonospace;
			if (fontfamily != null)
			{
				family = new FontFamily(fontfamily);
			}

			var fstyle = FontStyle.Regular;
			if (fontstyle != null)
			{
				switch (fontstyle.ToLower())
				{
					default:
					case "regular":
						break;
					case "bold":
						fstyle = FontStyle.Bold;
						break;
					case "italic":
						fstyle = FontStyle.Italic;
						break;
					case "strikethrough":
						fstyle = FontStyle.Strikeout;
						break;
					case "underline":
						fstyle = FontStyle.Underline;
						break;
				}
			}

			var font = new Font(family, fontsize ?? 12, fstyle, GraphicsUnit.Pixel);
			_graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
			_graphics.DrawString(message, font, new SolidBrush(color ?? Color.White), x, y);
		}
	}
}
