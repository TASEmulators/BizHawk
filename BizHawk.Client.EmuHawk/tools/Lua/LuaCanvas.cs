using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using BizHawk.Client.Common;
using LuaInterface;
using System;
using System.Collections.Generic;

namespace BizHawk.Client.EmuHawk
{
	[Description("Represents a canvas object returned by the gui.createcanvas() method")]
	public partial class LuaCanvas : Form
	{
		private Color _defaultForeground = Color.White;
		private Color? _defaultBackground;

		#region Helpers
		private readonly Dictionary<string, Image> _imageCache = new Dictionary<string, Image>();

		private readonly Dictionary<Color, SolidBrush> _solidBrushes = new Dictionary<Color, SolidBrush>();
		private readonly Dictionary<Color, Pen> _pens = new Dictionary<Color, Pen>();

		private SolidBrush GetBrush(Color color)
		{
			SolidBrush b;
			if (!_solidBrushes.TryGetValue(color, out b))
			{
				b = new SolidBrush(color);
				_solidBrushes[color] = b;
			}

			return b;
		}

		private Pen GetPen(Color color)
		{
			Pen p;
			if (!_pens.TryGetValue(color, out p))
			{
				p = new Pen(color);
				_pens[color] = p;
			}

			return p;
		}

		#endregion

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

		[LuaMethodAttributes("defaultForeground", "Sets the default foreground color to use in drawing methods, white by default")]
		public void SetDefaultForegroundColor(Color color)
		{
			_defaultForeground = color;
		}

		[LuaMethodAttributes("defaultBackground", "Sets the default background color to use in drawing methods, transparent by default")]
		public void SetDefaultBackgroundColor(Color color)
		{
			_defaultBackground = color;
		}
		
		[LuaMethodAttributes("drawBezier", "Draws a Bezier curve using the table of coordinates provided in the given color")]
		public void DrawBezier(LuaTable points, Color color)
		{
			try
			{
				var pointsArr = new Point[4];

				var i = 0;
				foreach (LuaTable point in points.Values)
				{
					pointsArr[i] = new Point((int)(double)(point[1]), (int)(double)(point[2]));
					i++;
					if (i >= 4)
					{
						break;
					}
				}

				_graphics.DrawBezier(GetPen(color), pointsArr[0], pointsArr[1], pointsArr[2], pointsArr[3]);
			}
			catch (Exception)
			{
				// need to stop the script from here
				return;
			}
		}

		[LuaMethodAttributes(
			"drawBox", "Draws a rectangle on screen from x1/y1 to x2/y2. Same as drawRectangle except it receives two points intead of a point and width/height")]
		public void DrawBox(int x, int y, int x2, int y2, Color? line = null, Color? background = null)
		{
			try
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

				_graphics.DrawRectangle(GetPen(line ?? _defaultForeground), x, y, x2, y2);

				var bg = background ?? _defaultBackground;
				if (bg.HasValue)
				{
					_graphics.FillRectangle(GetBrush(bg.Value), x + 1, y + 1, x2 - 1, y2 - 1);
				}
			}
			catch (Exception)
			{
				// need to stop the script from here
				return;
			}
		}

		[LuaMethodAttributes(
			"drawEllipse", "Draws an ellipse at the given coordinates and the given width and height. Line is the color of the ellipse. Background is the optional fill color")]
		public void DrawEllipse(int x, int y, int width, int height, Color? line = null, Color? background = null)
		{
			try
			{
				var bg = background ?? _defaultBackground;
				if (bg.HasValue)
				{
					var brush = GetBrush(bg.Value);
					_graphics.FillEllipse(brush, x, y, width, height);
				}

				_graphics.DrawEllipse(GetPen(line ?? _defaultForeground), x, y, width, height);
			}
			catch (Exception)
			{
				// need to stop the script from here
				return;
			}
		}

		[LuaMethodAttributes(
			"drawIcon", "draws an Icon (.ico) file from the given path at the given coordinate. width and height are optional. If specified, it will resize the image accordingly")]
		public void DrawIcon(string path, int x, int y, int? width = null, int? height = null)
		{
			try
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

				_graphics.DrawIcon(icon, x, y);
			}
			catch (Exception)
			{
				// need to stop the script from here
				return;
			}
		}

		[LuaMethodAttributes(
			"drawImage", "draws an image file from the given path at the given coordinate. width and height are optional. If specified, it will resize the image accordingly")]
		public void DrawImage(string path, int x, int y, int? width = null, int? height = null, bool cache = true)
		{
			if (!File.Exists(path))
			{
				//Log("File not found: " + path);
				return;
			}
			
			Image img;
			if (_imageCache.ContainsKey(path))
			{
				img = _imageCache[path];
			}
			else
			{
				img = Image.FromFile(path);
				if (cache)
				{
					_imageCache.Add(path, img);
				}
			}

			_graphics.DrawImage(img, x, y, width ?? img.Width, height ?? img.Height);
		}

		[LuaMethodAttributes(
			"clearImageCache", "clears the image cache that is built up by using gui.drawImage, also releases the file handle for cached images")]
		public void ClearImageCache()
		{
			foreach (var image in _imageCache)
			{
				image.Value.Dispose();
			}

			_imageCache.Clear();
		}

		[LuaMethodAttributes(
			"drawImageRegion", "draws a given region of an image file from the given path at the given coordinate, and optionally with the given size")]
		public void DrawImageRegion(string path, int source_x, int source_y, int source_width, int source_height, int dest_x, int dest_y, int? dest_width = null, int? dest_height = null)
		{
			if (!File.Exists(path))
			{
				//Log("File not found: " + path);
				return;
			}

			Image img;
			if (_imageCache.ContainsKey(path))
			{
				img = _imageCache[path];
			}
			else
			{
				img = Image.FromFile(path);
				_imageCache.Add(path, img);
			}

			var destRect = new Rectangle(dest_x, dest_y, dest_width ?? source_width, dest_height ?? source_height);

			_graphics.DrawImage(img, destRect, source_x, source_y, source_width, source_height, GraphicsUnit.Pixel);
		}

		[LuaMethodAttributes(
			"drawLine", "Draws a line from the first coordinate pair to the 2nd. Color is optional (if not specified it will be drawn black)")]
		public void DrawLine(int x1, int y1, int x2, int y2, Color? color = null)
		{
			_graphics.DrawLine(GetPen(color ?? _defaultForeground), x1, y1, x2, y2);
		}

		[LuaMethodAttributes("drawAxis", "Draws an axis of the specified size at the coordinate pair.)")]
		public void DrawAxis(int x, int y, int size, Color? color = null)
		{
			DrawLine(x + size, y, x - size, y, color);
			DrawLine(x, y + size, x, y - size, color);
		}

		[LuaMethodAttributes(
			"drawArc",
			"draws a Arc shape at the given coordinates and the given width and height"
		)]
		public void DrawArc(int x, int y, int width, int height, int startangle, int sweepangle, Color? line = null)
		{
			var pen = new Pen(line.HasValue ? line.Value : Color.Black);
			_graphics.DrawArc(pen, x, y, width, height, startangle, sweepangle);
		}

		[LuaMethodAttributes("drawPie", "draws a Pie shape at the given coordinates and the given width and height")]
		public void DrawPie(
			int x,
			int y,
			int width,
			int height,
			int startangle,
			int sweepangle,
			Color? line = null,
			Color? background = null)
		{
			var bg = background ?? _defaultBackground;
			if (bg.HasValue)
			{
				var brush = GetBrush(bg.Value);
				_graphics.FillPie(brush, x, y, width, height, startangle, sweepangle);
			}

			_graphics.DrawPie(GetPen(line ?? _defaultForeground), x + 1, y + 1, width - 1, height - 1, startangle, sweepangle);
		}

		[LuaMethodAttributes(
			"drawPixel", "Draws a single pixel at the given coordinates in the given color. Color is optional (if not specified it will be drawn black)")]
		public void DrawPixel(int x, int y, Color? color = null)
		{
			try
			{
				_graphics.DrawLine(GetPen(color ?? _defaultForeground), x, y, x + 0.1F, y);
			}
			catch (Exception)
			{
				// need to stop the script from here
				return;
			}
		}

		[LuaMethodAttributes(
			"drawPolygon", "Draws a polygon using the table of coordinates specified in points. This should be a table of tables(each of size 2). Line is the color of the polygon. Background is the optional fill color")]
		public void DrawPolygon(LuaTable points, Color? line = null, Color? background = null)
		{
			try
			{
				var pointsArr = new Point[points.Values.Count];
				var i = 0;
				foreach (LuaTable point in points.Values)
				{
					pointsArr[i] = new Point((int)(double)(point[1]), (int)(double)(point[2]));
					i++;
				}

				_graphics.DrawPolygon(GetPen(line ?? _defaultForeground), pointsArr);
				var bg = background ?? _defaultBackground;
				if (bg.HasValue)
				{
					_graphics.FillPolygon(GetBrush(bg.Value), pointsArr);
				}
			}
			catch (Exception)
			{
				// need to stop the script from here
				return;
			}
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
