using System;
using System.Drawing;
using System.Windows.Forms;

using NLua;
using System.Collections.Generic;

namespace BizHawk.Client.EmuHawk
{
	public class LuaPictureBox : PictureBox
	{
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

		private Color _defaultForeground = Color.Black;
		private Color? _defaultBackground;
		private Color? _defaultTextBackground = Color.FromArgb(128, 0, 0, 0);

		public LuaPictureBox()
		{
			Image = new Bitmap(Width, Height);
		}
		
		public void LuaResize(int width, int height)
		{
			Width = width;
			Height = height;
			Image = new Bitmap(width, height);
		}

		public void Clear(Color color)
		{
			var boxBackground = Graphics.FromImage(Image);
			boxBackground.Clear(color);
		}

		public void SetDefaultForegroundColor(Color color)
		{
			_defaultForeground = color;
		}

		public void SetDefaultBackgroundColor(Color color)
		{
			_defaultBackground = color;
		}

		public void SetDefaultTextBackground(Color color)
		{
			_defaultTextBackground = color;
		}

		public void DrawBezier(LuaTable points, Color color)
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

			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawBezier(GetPen(color), pointsArr[0], pointsArr[1], pointsArr[2], pointsArr[3]);
		}

		public void DrawBox(int x, int y, int x2, int y2, Color? line = null, Color? background = null)
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

			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawRectangle(GetPen(line ?? _defaultForeground), x, y, x2, y2);

			var bg = background ?? _defaultBackground;
			if (bg.HasValue)
			{
				boxBackground = Graphics.FromImage(Image);
				boxBackground.FillRectangle(GetBrush(bg.Value), x + 1, y + 1, x2 - 1, y2 - 1);
			}
		}

		public void DrawEllipse(int x, int y, int width, int height, Color? line = null, Color? background = null)
		{
			var bg = background ?? _defaultBackground;
			var boxBackground = Graphics.FromImage(Image);
			if (bg.HasValue)
			{
				var brush = GetBrush(bg.Value);
				boxBackground.FillEllipse(brush, x, y, width, height);
				boxBackground = Graphics.FromImage(Image);
			}
			
			boxBackground.DrawEllipse(GetPen(line ?? _defaultForeground), x, y, width, height);
		}

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

			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawIcon(icon, x, y);
		}

		public void DrawImage(string path, int x, int y, int? width = null, int? height = null, bool cache = true)
		{
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

			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawImage(img, x, y, width ?? img.Width, height ?? img.Height);
		}

		public void ClearImageCache()
		{
			foreach (var image in _imageCache)
			{
				image.Value.Dispose();
			}

			_imageCache.Clear();
		}

		public void DrawImageRegion(string path, int source_x, int source_y, int source_width, int source_height, int dest_x, int dest_y, int? dest_width = null, int? dest_height = null)
		{
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

			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawImage(img, destRect, source_x, source_y, source_width, source_height, GraphicsUnit.Pixel);
		}

		public void DrawLine(int x1, int y1, int x2, int y2, Color? color = null)
		{
			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawLine(GetPen(color ?? _defaultForeground), x1, y1, x2, y2);
		}

		public void DrawAxis(int x, int y, int size, Color? color = null)
		{
			DrawLine(x + size, y, x - size, y, color);
			DrawLine(x, y + size, x, y - size, color);
		}

		public void DrawArc(int x, int y, int width, int height, int startangle, int sweepangle, Color? line = null)
		{
			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawArc(GetPen(line ?? _defaultForeground), x, y, width, height, startangle, sweepangle);
		}

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
			var boxBackground = Graphics.FromImage(Image);
			if (bg.HasValue)
			{
				var brush = GetBrush(bg.Value);
				boxBackground.FillPie(brush, x, y, width, height, startangle, sweepangle);
				boxBackground = Graphics.FromImage(Image);
			}
			
			boxBackground.DrawPie(GetPen(line ?? _defaultForeground), x + 1, y + 1, width - 1, height - 1, startangle, sweepangle);
		}

		public void DrawPixel(int x, int y, Color? color = null)
		{
			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawLine(GetPen(color ?? _defaultForeground), x, y, x + 0.1F, y);
		}

		public void DrawPolygon(LuaTable points, int? x = null, int? y = null, Color? line = null, Color? background = null)
		{
			var pointsArr = new Point[points.Values.Count];
			var i = 0;
			foreach (LuaTable point in points.Values)
			{
				pointsArr[i] = new Point((int)(double)(point[1]) + x ?? 0, (int)(double)(point[2]) + y ?? 0);
				i++;
			}

			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawPolygon(GetPen(line ?? _defaultForeground), pointsArr);
			var bg = background ?? _defaultBackground;
			if (bg.HasValue)
			{
				boxBackground = Graphics.FromImage(Image);
				boxBackground.FillPolygon(GetBrush(bg.Value), pointsArr);
			}
		}

		public void DrawRectangle(int x, int y, int width, int height, Color? line = null, Color? background = null)
		{
			var bg = background ?? _defaultBackground;
			var boxBackground = Graphics.FromImage(Image);
			if (bg.HasValue)
			{
				boxBackground.FillRectangle(GetBrush(bg.Value), x, y, width, height);
				boxBackground = Graphics.FromImage(Image);
			}
			
			boxBackground.DrawRectangle(GetPen(line ?? _defaultForeground), x, y, width, height);
		}

		public void DrawText(
			int x,
			int y,
			string message,
			Color? forecolor = null,
			Color? backcolor = null,
			int? fontsize = null,
			string fontfamily = null,
			string fontstyle = null,
			string horizalign = null,
			string vertalign = null)
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

			var f = new StringFormat(StringFormat.GenericDefault);
			var font = new Font(family, fontsize ?? 12, fstyle, GraphicsUnit.Pixel);
			var boxBackground = Graphics.FromImage(Image);

			Size sizeOfText = boxBackground.MeasureString(message, font, 0, f).ToSize();

			if (horizalign != null)
			{
				switch (horizalign.ToLower())
				{
					default:
					case "left":
						break;
					case "center":
					case "middle":
						x -= sizeOfText.Width / 2;
						break;
					case "right":
						x -= sizeOfText.Width;
						break;
				}
			}

			if (vertalign != null)
			{
				switch (vertalign.ToLower())
				{
					default:
					case "top":
						break;
					case "center":
					case "middle":
						y -= sizeOfText.Height / 2;
						break;
					case "bottom":
						y -= sizeOfText.Height;
						break;
				}
			}
			Rectangle rect = new Rectangle(new Point(x, y), sizeOfText);
			boxBackground = Graphics.FromImage(Image);
			boxBackground.FillRectangle(GetBrush(backcolor ?? _defaultTextBackground.Value), rect);
			boxBackground = Graphics.FromImage(Image);
			boxBackground.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
			boxBackground.DrawString(message, font, new SolidBrush(forecolor ?? Color.Black), x, y);
		}
		
		public Point GetMouse()
		{
			var p = PointToClient(Control.MousePosition);
			return p;
		}

		private void DoLuaClick(object sender, EventArgs e)
		{
			LuaWinform parent = Parent as LuaWinform;
			parent?.DoLuaEvent(Handle);
		}

		protected override void OnClick(EventArgs e)
		{
			DoLuaClick(this, e);
			base.OnClick(e);
		}
	}
}
