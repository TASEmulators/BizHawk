using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaCanvas : Form
	{
		private Graphics graphics;

		public LuaCanvas(int width, int height)
		{
			InitializeComponent();
			pictureBox.Width = width;
			pictureBox.Height = height;
			pictureBox.Image = new Bitmap(width, height);
			graphics = Graphics.FromImage(pictureBox.Image);
		}

		public void SetTitle(string title)
		{
			this.Text = title;
		}

		public void Clear(Color color)
		{
			graphics.Clear(color);
		}

		public new void Refresh()
		{
			pictureBox.Refresh();
		}

		public void DrawRectangle(int x, int y, int width, int height, Color? outline = null, Color? fill = null)
		{
			if (fill.HasValue)
			{
				var brush = new SolidBrush(fill.Value);
				graphics.FillRectangle(brush, x, y, width, height);
			}

			var pen = new Pen(outline.HasValue ? outline.Value : Color.Black);
			graphics.DrawRectangle(pen, x, y, width, height);
		}

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
			graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
			graphics.DrawString(message, font, new SolidBrush(color ?? Color.White), x, y);
		}
	}
}
