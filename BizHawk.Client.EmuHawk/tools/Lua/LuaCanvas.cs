using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using LuaInterface;
using System;
using System.Collections.Generic;
using System.IO;

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
			"drawArc",
			"draws a Arc shape at the given coordinates and the given width and height"
		)]
		public void DrawArc(int x, int y, int width, int height, int startangle, int sweepangle, Color? line = null)
		{
			var pen = new Pen(line.HasValue ? line.Value : Color.Black);
			graphics.DrawArc(pen, x, y, width, height, startangle, sweepangle);
		}

		[LuaMethodAttributes(
			"drawBezier",
			"Draws a Bezier curve using the table of coordinates provided in the given color"
		)]
		public void DrawBezier(LuaTable points, Color? outline = null)
		{

		}

		[LuaMethodAttributes(
		"drawBox",
		"Draws a rectangle on screen from x1/y1 to x2/y2. Same as drawRectangle except it receives two points intead of a point and width/height"
		)]
		public void DrawBox(int x, int y, int x2, int y2, Color? outline = null, Color? fill = null)
		{
			if (x < x2)
			{
				x2 = Math.Abs(x - x2);
			}
			else
			{
				x2 = x - x2;
				x -= x2;
			}

			if (y < y2)
			{
				y2 = Math.Abs(y - y2);
			}
			else
			{
				y2 = y - y2;
				y -= y2;
			}

			if (fill.HasValue)
			{
				var brush = new SolidBrush(fill.Value);
				graphics.FillRectangle(brush, x, y, x2, y2);
			}

			var pen = new Pen(outline.HasValue ? outline.Value : Color.Black);
			graphics.DrawRectangle(pen, x, y, x2, y2);
		}

		[LuaMethodAttributes(
			"drawEllipse",
			"Draws an ellipse at the given coordinates and the given width and height. Line is the color of the ellipse. Background is the optional fill color"
		)]
		public void DrawEllipse(int x, int y, int width, int height, Color? outline = null, Color? fill = null)
		{
			if (fill.HasValue)
			{
				var brush = new SolidBrush(fill.Value);
				graphics.FillEllipse(brush, x, y, width, height);
			}

			var pen = new Pen(outline.HasValue ? outline.Value : Color.Black);
			graphics.DrawEllipse(pen, x, y, width, height);
		}

		[LuaMethodAttributes(
		"drawIcon",
		"draws an Icon (.ico) file from the given path at the given coordinate. width and height are optional. If specified, it will resize the image accordingly"
		)]
		public void DrawIcon(string path, int x, int y, int? width = null, int? height = null)
		{
			Icon icon;
			if (width.HasValue && height.HasValue)
			{
				icon = new Icon(path, width.Value, height.Value);
			}
			else
			{
				icon = new Icon(path);
			}

			graphics.DrawIcon(icon, x, y);
		}

		private readonly Dictionary<string, Image> ImageCache = new Dictionary<string, Image>();

		[LuaMethodAttributes(
		"drawImage",
		"draws an image file from the given path at the given coordinate. width and height are optional. If specified, it will resize the image accordingly"
		)]
		public void DrawImage(string path, int x, int y, int? width = null, int? height = null)
		{
			if (!File.Exists(path))
			{
				//LogConsole("File not found; " + path);
				return;
			}

			Image img;
			if (ImageCache.ContainsKey(path))
			{
				img = ImageCache[path];
			}
			else
			{
				img = Image.FromFile(path);
				ImageCache.Add(path, img);
			}

			graphics.DrawImage(img, x, y, width ?? img.Width, height ?? img.Height);
		}

		[LuaMethodAttributes(
			"drawImageRegion",
			"draws a given region of an image file from the given path at the given coordinate, and optionally with the given size"
		)]
		public void DrawImageRegion(string path, int source_x, int source_y, int source_width, int source_height, int dest_x, int dest_y, int? dest_width = null, int? dest_height = null)
		{
			if (!File.Exists(path))
			{
				//LogConsole("File not found; " + path);
				return;
			}

			Image img;
			if (ImageCache.ContainsKey(path))
			{
				img = ImageCache[path];
			}
			else
			{
				img = Image.FromFile(path);
				ImageCache.Add(path, img);
			}

			var dest_rect = new Rectangle(dest_x, dest_y, (dest_width ?? source_width), (dest_height ?? source_height));

			graphics.DrawImage(img, dest_rect, source_x, source_y, source_width, source_height, GraphicsUnit.Pixel);
		}

		[LuaMethodAttributes(
			"drawLine",
			"Draws a line from the first coordinate pair to the 2nd. Color is optional (if not specified it will be drawn black)"
		)]
		public void DrawLine(int x1, int y1, int x2, int y2, Color? line = null)
		{
			var pen = new Pen(line.HasValue ? line.Value : Color.Black);
			graphics.DrawLine(pen, x1, y1, x2, y2);
		}

		[LuaMethodAttributes(
			"drawAxis",
			"Draws an axis of the specified size at the coordinate pair.)"
		)]
		public void DrawAxis(int x, int y, int size, Color? line = null)
		{
			DrawLine(x + size, y, x - size, y, line);
			DrawLine(x, y + size, x, y - size, line);
		}

		[LuaMethodAttributes(
			"drawPie",
			"draws a Pie shape at the given coordinates and the given width and height"
		)]
		public void DrawPie(int x, int y, int width, int height, int startangle, int sweepangle, Color? outline = null, Color? fill = null)
		{
			if (fill.HasValue)
			{
				var brush = new SolidBrush(fill.Value);
				graphics.FillPie(brush, x, y, width, height, startangle, sweepangle);
			}

			var pen = new Pen(outline.HasValue ? outline.Value : Color.Black);
			graphics.DrawPie(pen, x, y, width, height, startangle, sweepangle);
		}

		[LuaMethodAttributes(
			"drawPolygon",
			"Draws a polygon using the table of coordinates specified in points. This should be a table of tables(each of size 2). Line is the color of the polygon. Background is the optional fill color"
		)]
		public void DrawPolygon(LuaTable points, Color? outline = null, Color? fill = null)
		{

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
