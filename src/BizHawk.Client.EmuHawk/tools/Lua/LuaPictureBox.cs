using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common.CollectionExtensions;

using NLua;

namespace BizHawk.Client.EmuHawk
{
	public class LuaPictureBox : PictureBox
	{
		private readonly Dictionary<string, Image> _imageCache = new Dictionary<string, Image>();

		private readonly SolidBrush _brush = new(default);

		private readonly Pen _pen = new(default(Color));

		private readonly Action<string> LogOutputCallback;

		private readonly NLuaTableHelper TableHelper;

		private SolidBrush GetBrush([LuaColorParam] object color)
		{
			_brush.Color = TableHelper.ParseColor(color);
			return _brush;
		}

		private Pen GetPen([LuaColorParam] object color)
		{
			_pen.Color = TableHelper.ParseColor(color);
			return _pen;
		}

		private Color _defaultForeground = Color.Black;
		private Color? _defaultBackground;
		private Color? _defaultTextBackground = Color.FromArgb(128, 0, 0, 0);

		public LuaPictureBox(NLuaTableHelper tableHelper, Action<string> logOutputCallback)
		{
			Image = new Bitmap(Width, Height);
			LogOutputCallback = logOutputCallback;
			TableHelper = tableHelper;
		}
		
		public void LuaResize(int width, int height)
		{
			Width = width;
			Height = height;
			Image = new Bitmap(width, height);
		}

		public void Clear([LuaColorParam] object color)
		{
			var boxBackground = Graphics.FromImage(Image);
			boxBackground.Clear(TableHelper.ParseColor(color));
		}

		public void SetDefaultForegroundColor([LuaColorParam] object color)
		{
			_defaultForeground = TableHelper.ParseColor(color);
		}

		public void SetDefaultBackgroundColor([LuaColorParam] object color)
		{
			_defaultBackground = TableHelper.ParseColor(color);
		}

		public void SetDefaultTextBackground([LuaColorParam] object color)
		{
			_defaultTextBackground = TableHelper.ParseColor(color);
		}

		public void DrawBezier(LuaTable points, [LuaColorParam] object color)
		{
			var pointsArr = new Point[4];

			var i = 0;
			foreach (var point in TableHelper.EnumerateValues<LuaTable>(points)
				.Select(table => TableHelper.EnumerateValues<long>(table).ToList()))
			{
				pointsArr[i] = new Point((int) point[0], (int) point[1]);
				i++;
				if (i >= 4)
				{
					break;
				}
			}

			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawBezier(GetPen(TableHelper.ParseColor(color)), pointsArr[0], pointsArr[1], pointsArr[2], pointsArr[3]);
		}

		public void DrawBox(
			int x,
			int y,
			int x2,
			int y2,
			[LuaColorParam] object line,
			[LuaColorParam] object background)
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
			boxBackground.DrawRectangle(GetPen(TableHelper.SafeParseColor(line) ?? _defaultForeground), x, y, x2, y2);

			var bg = TableHelper.SafeParseColor(background) ?? _defaultBackground;
			if (bg.HasValue)
			{
				boxBackground = Graphics.FromImage(Image);
				boxBackground.FillRectangle(GetBrush(bg.Value), x + 1, y + 1, x2 - 1, y2 - 1);
			}
		}

		public void DrawEllipse(
			int x,
			int y,
			int width,
			int height,
			[LuaColorParam] object line,
			[LuaColorParam] object background)
		{
			var bg = TableHelper.SafeParseColor(background) ?? _defaultBackground;
			var boxBackground = Graphics.FromImage(Image);
			if (bg.HasValue)
			{
				var brush = GetBrush(bg.Value);
				boxBackground.FillEllipse(brush, x, y, width, height);
				boxBackground = Graphics.FromImage(Image);
			}
			
			boxBackground.DrawEllipse(GetPen(TableHelper.SafeParseColor(line) ?? _defaultForeground), x, y, width, height);
		}

		public void DrawIcon(string path, int x, int y, int? width, int? height, string functionName)
		{
			Icon icon;
			if (width is int w && height is int h)
			{
				icon = new Icon(path, width: w, height: h);
			}
			else
			{
				if (width is not null || height is not null) WarnForMismatchedPair(functionName: functionName, kind: "width and height");
				icon = new Icon(path);
			}

			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawIcon(icon, x, y);
		}

		public void DrawImage(string path, int x, int y, int? width, int? height, bool cache)
		{
			if (!_imageCache.TryGetValue(path, out var img))
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

		public void DrawImageRegion(
			string path,
			int sourceX,
			int sourceY,
			int sourceWidth,
			int sourceHeight,
			int destX,
			int destY,
			int? destWidth,
			int? destHeight)
		{
			var img = _imageCache.GetValueOrPut(path, Image.FromFile);
			var destRect = new Rectangle(destX, destY, destWidth ?? sourceWidth, destHeight ?? sourceHeight);

			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawImage(img, destRect, sourceX, sourceY, sourceWidth, sourceHeight, GraphicsUnit.Pixel);
		}

		public void DrawLine(int x1, int y1, int x2, int y2, [LuaColorParam] object color)
		{
			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawLine(GetPen(TableHelper.SafeParseColor(color) ?? _defaultForeground), x1, y1, x2, y2);
		}

		public void DrawAxis(int x, int y, int size, [LuaColorParam] object color)
		{
			var color1 = TableHelper.SafeParseColor(color);
			DrawLine(x + size, y, x - size, y, color1);
			DrawLine(x, y + size, x, y - size, color1);
		}

		public void DrawArc(
			int x,
			int y,
			int width,
			int height,
			int startAngle,
			int sweepAngle,
			[LuaColorParam] object line)
		{
			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawArc(GetPen(TableHelper.SafeParseColor(line) ?? _defaultForeground), x, y, width, height, startAngle, sweepAngle);
		}

		public void DrawPie(
			int x,
			int y,
			int width,
			int height,
			int startAngle,
			int sweepAngle,
			[LuaColorParam] object line,
			[LuaColorParam] object background)
		{
			var bg = TableHelper.SafeParseColor(background) ?? _defaultBackground;
			var boxBackground = Graphics.FromImage(Image);
			if (bg.HasValue)
			{
				var brush = GetBrush(bg.Value);
				boxBackground.FillPie(brush, x, y, width, height, startAngle, sweepAngle);
				boxBackground = Graphics.FromImage(Image);
			}
			
			boxBackground.DrawPie(GetPen(TableHelper.SafeParseColor(line) ?? _defaultForeground), x + 1, y + 1, width - 1, height - 1, startAngle, sweepAngle);
		}

		public void DrawPixel(int x, int y, [LuaColorParam] object color)
		{
			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawLine(GetPen(TableHelper.SafeParseColor(color) ?? _defaultForeground), x, y, x + 0.1F, y);
		}

		public void DrawPolygon(
			LuaTable points,
			int? x,
			int? y,
			[LuaColorParam] object line,
			[LuaColorParam] object background)
		{
			var pointsList = TableHelper.EnumerateValues<LuaTable>(points)
				.Select(table => TableHelper.EnumerateValues<long>(table).ToList()).ToList();
			var pointsArr = new Point[pointsList.Count];
			var i = 0;
			foreach (var point in pointsList)
			{
				pointsArr[i] = new Point((int) point[0] + x ?? 0, (int) point[1] + y ?? 0);
				i++;
			}

			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawPolygon(GetPen(TableHelper.SafeParseColor(line) ?? _defaultForeground), pointsArr);
			var bg = TableHelper.SafeParseColor(background) ?? _defaultBackground;
			if (bg.HasValue)
			{
				boxBackground = Graphics.FromImage(Image);
				boxBackground.FillPolygon(GetBrush(bg.Value), pointsArr);
			}
		}

		public void DrawRectangle(
			int x,
			int y,
			int width,
			int height,
			[LuaColorParam] object line,
			[LuaColorParam] object background)
		{
			var bg = TableHelper.SafeParseColor(background) ?? _defaultBackground;
			var boxBackground = Graphics.FromImage(Image);
			if (bg.HasValue)
			{
				boxBackground.FillRectangle(GetBrush(bg.Value), x, y, width, height);
				boxBackground = Graphics.FromImage(Image);
			}
			
			boxBackground.DrawRectangle(GetPen(TableHelper.SafeParseColor(line) ?? _defaultForeground), x, y, width, height);
		}

		public void DrawText(
			int x,
			int y,
			string message,
			[LuaColorParam] object foreColor,
			[LuaColorParam] object backColor,
			int? fontSize,
			string fontFamily,
			string fontStyle,
			string horizAlign,
			string vertAlign)
		{
			var family = FontFamily.GenericMonospace;
			if (fontFamily != null)
			{
				family = new FontFamily(fontFamily);
			}

			var fStyle = FontStyle.Regular;
			if (fontStyle != null)
			{
				switch (fontStyle.ToLowerInvariant())
				{
					default:
					case "regular":
						break;
					case "bold":
						fStyle = FontStyle.Bold;
						break;
					case "italic":
						fStyle = FontStyle.Italic;
						break;
					case "strikethrough":
						fStyle = FontStyle.Strikeout;
						break;
					case "underline":
						fStyle = FontStyle.Underline;
						break;
				}
			}

			var f = new StringFormat(StringFormat.GenericDefault);
			var font = new Font(family, fontSize ?? 12, fStyle, GraphicsUnit.Pixel);
			var boxBackground = Graphics.FromImage(Image);

			Size sizeOfText = boxBackground.MeasureString(message, font, 0, f).ToSize();

			if (horizAlign != null)
			{
				switch (horizAlign.ToLowerInvariant())
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

			if (vertAlign != null)
			{
				switch (vertAlign.ToLowerInvariant())
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
			boxBackground.FillRectangle(GetBrush(TableHelper.SafeParseColor(backColor) ?? _defaultTextBackground.Value), rect);
			boxBackground = Graphics.FromImage(Image);
			boxBackground.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
			boxBackground.DrawString(message, font, GetBrush(TableHelper.SafeParseColor(foreColor) ?? Color.Black), x, y);
		}
		
		public Point GetMouse()
		{
			var p = PointToClient(MousePosition);
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

		private void WarnForMismatchedPair(string functionName, string kind)
			=> LogOutputCallback($"{functionName}: both {kind} must be set to have any effect");
	}
}
