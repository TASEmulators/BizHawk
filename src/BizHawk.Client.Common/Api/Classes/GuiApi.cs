using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

using BizHawk.Bizware.Graphics;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class GuiApi : IGuiApi
	{
		private static readonly StringFormat PixelTextFormat = new(StringFormat.GenericTypographic)
		{
			FormatFlags = StringFormatFlags.MeasureTrailingSpaces,
		};

		[RequiredService]
		private IEmulator Emulator { get; set; }

		private readonly Action<string> LogCallback;

		private readonly DisplayManagerBase _displayManager;

		private readonly Dictionary<string, Bitmap> _imageCache = new();

		private readonly Bitmap _nullGraphicsBitmap = new(1, 1);

		private CompositingMode _compositingMode = CompositingMode.SourceOver;

		private Color? _defaultBackground;

		private Color _defaultForeground = Color.White;

		private int _defaultPixelFont = 1; // = "gens"

		private Color _defaultTextBackground = Color.FromArgb(128, 0, 0, 0);

		private (int Left, int Top, int Right, int Bottom) _padding = (0, 0, 0, 0);

		private DisplaySurfaceID? _usingSurfaceID;
		public bool HasGUISurface => true;

		public GuiApi(Action<string> logCallback, DisplayManagerBase displayManager)
		{
			LogCallback = logCallback;
			_displayManager = displayManager;
		}

		private I2DRenderer Get2DRenderer(DisplaySurfaceID? surfaceID)
		{
			var nnID = surfaceID ?? _usingSurfaceID ?? throw new Exception();
			return _displayManager.GetApiHawk2DRenderer(nnID);
		}

		public void ToggleCompositingMode()
			=> _compositingMode = (CompositingMode) (1 - (int) _compositingMode); // enum has two members, 0 and 1

		public ImageAttributes GetAttributes() => null;

		public void SetAttributes(ImageAttributes a)
		{
		}

		public void WithSurface(DisplaySurfaceID surfaceID, Action<IGuiApi> drawingCallsFunc)
		{
			_usingSurfaceID = surfaceID;
			try
			{
				drawingCallsFunc(this);
			}
			finally
			{
				_usingSurfaceID = null;
			}
		}

		public void WithSurface(DisplaySurfaceID surfaceID, Action drawingCallsFunc)
		{
			_usingSurfaceID = surfaceID;
			try
			{
				drawingCallsFunc();
			}
			finally
			{
				_usingSurfaceID = null;
			}
		}

		public void DrawNew(string name, bool clear)
		{
			switch (name)
			{
				case null:
				case "emu":
					LogCallback("the `DrawNew(\"emu\")` function has been deprecated");
					return;
				case "native":
					throw new InvalidOperationException("the ability to draw in the margins with `DrawNew(\"native\")` has been removed");
				default:
					throw new InvalidOperationException("invalid surface name");
			}
		}

		public void DrawFinish() => LogCallback("the `DrawFinish()` function has been deprecated");

		public void SetPadding(int all) => _padding = (all, all, all, all);

		public void SetPadding(int x, int y) => _padding = (x / 2, y / 2, x / 2 + x & 1, y / 2 + y & 1);

		public void SetPadding(int l, int t, int r, int b) => _padding = (l, t, r, b);

		public (int Left, int Top, int Right, int Bottom) GetPadding() => _padding;

		public void AddMessage(string message, int? duration = null)
			=> _displayManager.OSD.AddMessage(message, duration);

		public void ClearGraphics(DisplaySurfaceID? surfaceID = null) => Get2DRenderer(surfaceID).Clear();

		public void ClearText() => _displayManager.OSD.ClearGuiText();

		public void SetDefaultForegroundColor(Color color) => _defaultForeground = color;

		public void SetDefaultBackgroundColor(Color color) => _defaultBackground = color;

		public Color GetDefaultTextBackground() => _defaultTextBackground;

		public void SetDefaultTextBackground(Color color) => _defaultTextBackground = color;

		public void SetDefaultPixelFont(string fontfamily)
		{
			switch (fontfamily)
			{
				case "fceux":
				case "0":
					_defaultPixelFont = 0;
					break;
				case "gens":
				case "1":
					_defaultPixelFont = 1;
					break;
				default:
					LogCallback($"Unable to find font family: {fontfamily}");
					return;
			}
		}

		public void DrawBezier(Point p1, Point p2, Point p3, Point p4, Color? color = null, DisplaySurfaceID? surfaceID = null)
		{
			try
			{
				var r = Get2DRenderer(surfaceID);
				r.CompositingMode = _compositingMode;
				r.DrawBezier(color ?? _defaultForeground, p1, p2, p3, p4);
			}
			catch (Exception)
			{
				// ignored
			}
		}

		public void DrawBeziers(Point[] points, Color? color = null, DisplaySurfaceID? surfaceID = null)
		{
			try
			{
				var r = Get2DRenderer(surfaceID);
				r.CompositingMode = _compositingMode;
				r.DrawBeziers(color ?? _defaultForeground, points);
			}
			catch (Exception)
			{
				// ignored
			}
		}

		public void DrawBox(int x, int y, int x2, int y2, Color? line = null, Color? background = null, DisplaySurfaceID? surfaceID = null)
		{
			try
			{
				if (x > x2)
				{
					(x, x2) = (x2, x);
				}

				if (y > y2)
				{
					(y, y2) = (y2, y);
				}

				var w = x2 - x + 1;
				var h = y2 - y + 1;

				var r = Get2DRenderer(surfaceID);
				r.CompositingMode = _compositingMode;
				r.DrawRectangle(line ?? _defaultForeground, x, y, w, h);
				var bg = background ?? _defaultBackground;
				if (bg != null) r.FillRectangle(bg.Value, x + 1, y + 1, Math.Max(w - 2, 0), Math.Max(h - 2, 0));
			}
			catch (Exception)
			{
				// need to stop the script from here
			}
		}

		public void DrawEllipse(int x, int y, int width, int height, Color? line = null, Color? background = null, DisplaySurfaceID? surfaceID = null)
		{
			try
			{
				var r = Get2DRenderer(surfaceID);
				var bg = background ?? _defaultBackground;
				// GDI+ had an off by one here, we increment width and height to preserve backwards compatibility
				width++; height++;
				if (bg != null) r.FillEllipse(bg.Value, x, y, width, height);
				r.CompositingMode = _compositingMode;
				r.DrawEllipse(line ?? _defaultForeground, x, y, width, height);
			}
			catch (Exception)
			{
				// need to stop the script from here
			}
		}

		public void DrawIcon(string path, int x, int y, int? width = null, int? height = null, DisplaySurfaceID? surfaceID = null)
		{
			try
			{
				if (!File.Exists(path))
				{
					AddMessage($"File not found: {path}");
					return;
				}

				var icon = width != null && height != null
					? new Icon(path, width.Value, height.Value)
					: new Icon(path);

				var r = Get2DRenderer(surfaceID);
				r.CompositingMode = _compositingMode;
				r.DrawImage(icon.ToBitmap(), x, y);
			}
			catch (Exception)
			{
				// ignored
			}
		}

		public void DrawImage(Image img, int x, int y, int? width = null, int? height = null, bool cache = true, DisplaySurfaceID? surfaceID = null)
		{
			var r = Get2DRenderer(surfaceID);
			r.CompositingMode = _compositingMode;
			r.DrawImage(
				new(img),
				new Rectangle(x, y, width ?? img.Width, height ?? img.Height),
				0,
				0,
				img.Width,
				img.Height,
				cache: false // caching is meaningless here
			);
		}

		public void DrawImage(string path, int x, int y, int? width = null, int? height = null, bool cache = true, DisplaySurfaceID? surfaceID = null)
		{
			if (!File.Exists(path))
			{
				LogCallback($"File not found: {path}");
				return;
			}

			if (!_imageCache.TryGetValue(path, out var img))
			{
				using var file = Image.FromFile(path);
				img = new(file);
				if (cache) _imageCache[path] = img;
			}

			var r = Get2DRenderer(surfaceID);
			r.CompositingMode = _compositingMode;
			r.DrawImage(
				img,
				new Rectangle(x, y, width ?? img.Width, height ?? img.Height),
				0,
				0,
				img.Width,
				img.Height,
				cache
			);
		}

		public void ClearImageCache()
		{
			_imageCache.Clear();
			_displayManager.ClearApiHawkTextureCache();
		}

		public void DrawImageRegion(Image img, int source_x, int source_y, int source_width, int source_height, int dest_x, int dest_y, int? dest_width = null, int? dest_height = null, DisplaySurfaceID? surfaceID = null)
		{
			var r = Get2DRenderer(surfaceID);
			r.CompositingMode = _compositingMode;
			r.DrawImage(
				new(img),
				new Rectangle(dest_x, dest_y, dest_width ?? source_width, dest_height ?? source_height),
				source_x,
				source_y,
				source_width,
				source_height,
				cache: false
			);
		}

		public void DrawImageRegion(string path, int source_x, int source_y, int source_width, int source_height, int dest_x, int dest_y, int? dest_width = null, int? dest_height = null, DisplaySurfaceID? surfaceID = null)
		{
			if (!File.Exists(path))
			{
				LogCallback($"File not found: {path}");
				return;
			}

			var r = Get2DRenderer(surfaceID);
			r.CompositingMode = _compositingMode;
			r.DrawImage(
				_imageCache.GetValueOrPut(path, static i =>
				{
					using var file = Image.FromFile(i);
					return new(file);
				}),
				new Rectangle(dest_x, dest_y, dest_width ?? source_width, dest_height ?? source_height),
				source_x,
				source_y,
				source_width,
				source_height,
				cache: true
			);
		}

		public void DrawLine(int x1, int y1, int x2, int y2, Color? color = null, DisplaySurfaceID? surfaceID = null)
		{
			var r = Get2DRenderer(surfaceID);
			r.CompositingMode = _compositingMode;
			r.DrawLine(color ?? _defaultForeground, x1, y1, x2, y2);
		}

		public void DrawAxis(int x, int y, int size, Color? color = null, DisplaySurfaceID? surfaceID = null)
		{
			DrawLine(x + size, y, x - size, y, color ?? _defaultForeground, surfaceID: surfaceID);
			DrawLine(x, y + size, x, y - size, color ?? _defaultForeground, surfaceID: surfaceID);
		}

		public void DrawPie(int x, int y, int width, int height, int startangle, int sweepangle, Color? line = null, Color? background = null, DisplaySurfaceID? surfaceID = null)
		{
			var r = Get2DRenderer(surfaceID);
			r.CompositingMode = _compositingMode;
			var bg = background ?? _defaultBackground;
			// GDI+ had an off by one here, we increment width and height to preserve backwards compatibility
			width++; height++;
			if (bg != null) r.FillPie(bg.Value, x, y, width, height, startangle, sweepangle);
			r.DrawPie(line ?? _defaultForeground, x + 1, y + 1, width - 1, height - 1, startangle, sweepangle);
		}

		public void DrawPixel(int x, int y, Color? color = null, DisplaySurfaceID? surfaceID = null)
		{
			try
			{
				var r = Get2DRenderer(surfaceID);
				r.DrawLine(color ?? _defaultForeground, x, y, x, y);
			}
			catch (Exception)
			{
				// ignored
			}
		}

		public void DrawPolygon(Point[] points, Color? line = null, Color? background = null, DisplaySurfaceID? surfaceID = null)
		{
			try
			{
				var r = Get2DRenderer(surfaceID);
				r.DrawPolygon(line ?? _defaultForeground, points);
				var bg = background ?? _defaultBackground;
				if (bg != null) r.FillPolygon(bg.Value, points);
			}
			catch (Exception)
			{
				// ignored
			}
		}

		public void DrawRectangle(int x, int y, int width, int height, Color? line = null, Color? background = null, DisplaySurfaceID? surfaceID = null)
		{
			var r = Get2DRenderer(surfaceID);
			var w = Math.Max(width, 0);
			var h = Math.Max(height, 0);
			// GDI+ had an off by one here, we increment width and height to preserve backwards compatibility
			w++; h++;
			r.DrawRectangle(line ?? _defaultForeground, x, y, w, h);
			var bg = background ?? _defaultBackground;
			if (bg != null) r.FillRectangle(bg.Value, x + 1, y + 1, Math.Max(w - 2, 0), Math.Max(h - 2, 0));
		}

		public void DrawString(int x, int y, string message, Color? forecolor = null, Color? backcolor = null, int? fontsize = null, string fontfamily = null, string fontstyle = null, string horizalign = null, string vertalign = null, DisplaySurfaceID? surfaceID = null)
		{
			try
			{
				var family = fontfamily != null ? new FontFamily(fontfamily) : FontFamily.GenericMonospace;

				var fstyle = fontstyle?.ToLowerInvariant() switch
				{
					"bold" => FontStyle.Bold,
					"italic" => FontStyle.Italic,
					"strikethrough" => FontStyle.Strikeout,
					"underline" => FontStyle.Underline,
					_ => FontStyle.Regular
				};

				using var g = Graphics.FromImage(_nullGraphicsBitmap);

				// The text isn't written out using GenericTypographic, so measuring it using GenericTypographic seemed to make it worse.
				// And writing it out with GenericTypographic just made it uglier. :p
				var font = new Font(family, fontsize ?? 12, fstyle, GraphicsUnit.Pixel);
				var sizeOfText = g.MeasureString(message, font, 0, new StringFormat(StringFormat.GenericDefault)).ToSize();

				switch (horizalign?.ToLowerInvariant())
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

				switch (vertalign?.ToLowerInvariant())
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

				var r = Get2DRenderer(surfaceID);
				var bg = backcolor ?? _defaultBackground;
				if (bg != null)
				{
					for (var xd = -1; xd <= 1; xd++) for (var yd = -1; yd <= 1; yd++)
					{
						r.DrawString(message, font, bg.Value, x + xd, y + yd);
					}
				}

				r.DrawString(message, font, forecolor ?? _defaultForeground, x, y, textRenderingHint: TextRenderingHint.SingleBitPerPixelGridFit);
			}
			catch (Exception)
			{
				// ignored
			}
		}

		public void DrawText(int x, int y, string message, Color? forecolor = null, Color? backcolor = null, string fontfamily = null, DisplaySurfaceID? surfaceID = null)
			=> PixelText(
				x: x,
				y: y,
				message: message,
				forecolor: forecolor,
				backcolor: backcolor,
				fontfamily: fontfamily,
				surfaceID: surfaceID);

		public void PixelText(
			int x,
			int y,
			string message,
			Color? forecolor = null,
			Color? backcolor = null,
			string fontfamily = null,
			DisplaySurfaceID? surfaceID = null)
		{
			try
			{
				int index;
				switch (fontfamily)
				{
					case "fceux":
					case "0":
						index = 0;
						break;
					case "gens":
					case "1":
						index = 1;
						break;
					default:
						if (!string.IsNullOrEmpty(fontfamily)) // not a typo
						{
							LogCallback($"Unable to find font family: {fontfamily}");
							return;
						}
						index = _defaultPixelFont;
						break;
				}

				using var g = Graphics.FromImage(_nullGraphicsBitmap);
				var font = new Font(_displayManager.CustomFonts.Families[index], 8, FontStyle.Regular, GraphicsUnit.Pixel);
				var sizeOfText = g.MeasureString(message, font, width: 0, PixelTextFormat).ToSize();

				var r = Get2DRenderer(surfaceID);
				if (backcolor.HasValue) r.FillRectangle(backcolor.Value, x, y, sizeOfText.Width + 2, sizeOfText.Height);
				r.DrawString(message, font, forecolor ?? _defaultForeground, x + 1, y, PixelTextFormat, TextRenderingHint.SingleBitPerPixelGridFit);
			}
			catch (Exception)
			{
				// ignored
			}
		}

		public void Text(int x, int y, string message, Color? forecolor = null, string anchor = null)
		{
			int a = default;
			if (!string.IsNullOrEmpty(anchor))
			{
				a = anchor switch
				{
					"0" => 0,
					"topleft" => 0,
					"1" => 1,
					"topright" => 1,
					"2" => 2,
					"bottomleft" => 2,
					"3" => 3,
					"bottomright" => 3,
					_ => default
				};
			}
			else
			{
				var (ox, oy) = Emulator.ScreenLogicalOffsets();
				x -= ox;
				y -= oy;
			}

			var pos = new MessagePosition { X = x, Y = y, Anchor = (MessagePosition.AnchorType)a };
			_displayManager.OSD.AddGuiText(message,  pos, Color.Black, forecolor ?? Color.White);
		}

		public void Dispose()
			=> ClearImageCache();
	}
}
