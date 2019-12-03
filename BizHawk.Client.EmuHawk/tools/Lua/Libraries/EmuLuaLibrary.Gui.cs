using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using NLua;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class GuiLuaLibrary : LuaLibraryBase
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		public GuiLuaLibrary(Lua lua)
			: base(lua) { }

		public GuiLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		private Color _defaultForeground = Color.White;
		private Color? _defaultBackground;
		private Color? _defaultTextBackground = Color.FromArgb(128, 0, 0, 0);
		private int _defaultPixelFont = 1; // gens

		public override string Name => "gui";

		#region Gui API

		public void Dispose()
		{
			foreach (var brush in _solidBrushes.Values)
			{
				brush.Dispose();
			}

			foreach (var brush in _pens.Values)
			{
				brush.Dispose();
			}
		}

		public bool SurfaceIsNull => _luaSurface == null;

		[LuaMethodExample("gui.DrawNew( \"native\", false );")]
		[LuaMethod("DrawNew", "Changes drawing target to the specified lua surface name. This may clobber any previous drawing to this surface (pass false if you don't want it to)")]
		public void DrawNew(string name, bool? clear = true)
		{
			try
			{
				DrawFinish();
				_luaSurface = GlobalWin.DisplayManager.LockLuaSurface(name, clear ?? true);
			}
			catch (InvalidOperationException ex)
			{
				Log(ex.ToString());
			}
		}

		[LuaMethodExample("gui.DrawFinish( );")]
		[LuaMethod("DrawFinish", "Finishes drawing to the current lua surface and causes it to get displayed.")]
		public void DrawFinish()
		{
			if (_luaSurface != null)
			{
				GlobalWin.DisplayManager.UnlockLuaSurface(_luaSurface);
			}

			_luaSurface = null;
		}

		public bool HasLuaSurface => _luaSurface != null;

		#endregion

		#region Helpers
		private readonly Dictionary<string, Image> _imageCache = new Dictionary<string, Image>();

		private readonly Dictionary<Color, SolidBrush> _solidBrushes = new Dictionary<Color, SolidBrush>();
		private readonly Dictionary<Color, Pen> _pens = new Dictionary<Color, Pen>();
		private readonly Bitmap _nullGraphicsBitmap = new Bitmap(1, 1);

		private DisplaySurface _luaSurface;

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

		private Graphics GetGraphics()
		{
			var g = _luaSurface == null ? Graphics.FromImage(_nullGraphicsBitmap) : _luaSurface.GetGraphics();

			// we don't like CoreComm, right? Someone should find a different way to do this then.
			var tx = Emulator.CoreComm.ScreenLogicalOffsetX;
			var ty = Emulator.CoreComm.ScreenLogicalOffsetY;
			if (tx != 0 || ty != 0)
			{
				var transform = g.Transform;
				transform.Translate(-tx, -ty);
				g.Transform = transform;
			}

			return g;
		}

		#endregion

		[LuaMethodExample("gui.addmessage( \"Some message\" );")]
		[LuaMethod("addmessage", "Adds a message to the OSD's message area")]
		public void AddMessage(string message)
		{
			GlobalWin.OSD.AddMessage(message);
		}

		[LuaMethodExample("gui.clearGraphics( );")]
		[LuaMethod("clearGraphics", "clears all lua drawn graphics from the screen")]
		public void ClearGraphics()
		{
			_luaSurface.Clear();
			DrawFinish();
		}

		[LuaMethodExample("gui.cleartext( );")]
		[LuaMethod("cleartext", "clears all text created by gui.text()")]
		public static void ClearText()
		{
			GlobalWin.OSD.ClearGuiText();
		}

		[LuaMethodExample("gui.defaultForeground( 0x000000FF );")]
		[LuaMethod("defaultForeground", "Sets the default foreground color to use in drawing methods, white by default")]
		public void SetDefaultForegroundColor(Color color)
		{
			_defaultForeground = color;
		}

		[LuaMethodExample("gui.defaultBackground( 0xFFFFFFFF );")]
		[LuaMethod("defaultBackground", "Sets the default background color to use in drawing methods, transparent by default")]
		public void SetDefaultBackgroundColor(Color color)
		{
			_defaultBackground = color;
		}

		[LuaMethodExample("gui.defaultTextBackground( 0x000000FF );")]
		[LuaMethod("defaultTextBackground", "Sets the default backgroiund color to use in text drawing methods, half-transparent black by default")]
		public void SetDefaultTextBackground(Color color)
		{
			_defaultTextBackground = color;
		}

		[LuaMethodExample("gui.defaultPixelFont( \"Arial Narrow\");")]
		[LuaMethod("defaultPixelFont", "Sets the default font to use in gui.pixelText(). Two font families are available, \"fceux\" and \"gens\" (or  \"0\" and \"1\" respectively), \"gens\" is used by default")]
		public void SetDefaultTextBackground(string fontfamily)
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
					Log($"Unable to find font family: {fontfamily}");
					return;
			}
		}

		[LuaMethodExample("gui.drawBezier( { { 5, 10 }, { 10, 10 }, { 10, 20 }, { 5, 20 } }, 0x000000FF );")]
		[LuaMethod("drawBezier", "Draws a Bezier curve using the table of coordinates provided in the given color")]
		public void DrawBezier(LuaTable points, Color color)
		{
			using (var g = GetGraphics())
			{
				try
				{
					var pointsArr = new Point[4];

					var i = 0;
					foreach (LuaTable point in points.Values)
					{
						pointsArr[i] = new Point(LuaInt(point[1]), LuaInt(point[2]));
						i++;
						if (i >= 4)
						{
							break;
						}
					}

					g.DrawBezier(GetPen(color), pointsArr[0], pointsArr[1], pointsArr[2], pointsArr[3]);
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		[LuaMethodExample("gui.drawBox( 16, 32, 162, 322, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod("drawBox", "Draws a rectangle on screen from x1/y1 to x2/y2. Same as drawRectangle except it receives two points intead of a point and width/height")]
		public void DrawBox(int x, int y, int x2, int y2, Color? line = null, Color? background = null)
		{
			using (var g = GetGraphics())
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

					g.DrawRectangle(GetPen(line ?? _defaultForeground), x, y, x2, y2);

					var bg = background ?? _defaultBackground;
					if (bg.HasValue)
					{
						g.FillRectangle(GetBrush(bg.Value), x + 1, y + 1, x2 - 1, y2 - 1);
					}
				}
				catch (Exception)
				{
					// need to stop the script from here
					return;
				}
			}
		}

		[LuaMethodExample("gui.drawEllipse( 16, 32, 77, 99, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod("drawEllipse", "Draws an ellipse at the given coordinates and the given width and height. Line is the color of the ellipse. Background is the optional fill color")]
		public void DrawEllipse(int x, int y, int width, int height, Color? line = null, Color? background = null)
		{
			using (var g = GetGraphics())
			{
				try
				{
					var bg = background ?? _defaultBackground;
					if (bg.HasValue)
					{
						var brush = GetBrush(bg.Value);
						g.FillEllipse(brush, x, y, width, height);
					}

					g.DrawEllipse(GetPen(line ?? _defaultForeground), x, y, width, height);
				}
				catch (Exception)
				{
					// need to stop the script from here
					return;
				}
			}
		}

		[LuaMethodExample("gui.drawIcon( \"C:\\sample.ico\", 16, 32, 18, 24 );")]
		[LuaMethod("drawIcon", "draws an Icon (.ico) file from the given path at the given coordinate. width and height are optional. If specified, it will resize the image accordingly")]
		public void DrawIcon(string path, int x, int y, int? width = null, int? height = null)
		{
			using (var g = GetGraphics())
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

					g.DrawIcon(icon, x, y);
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		[LuaMethodExample("gui.drawImage( \"C:\\sample.bmp\", 16, 32, 18, 24, false );")]
		[LuaMethod("drawImage", "draws an image file from the given path at the given coordinate. width and height are optional. If specified, it will resize the image accordingly")]
		public void DrawImage(string path, int x, int y, int? width = null, int? height = null, bool cache = true)
		{
			if (!File.Exists(path))
			{
				Log($"File not found: {path}");
				return;
			}

			using (var g = GetGraphics())
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

				g.DrawImage(img, x, y, width ?? img.Width, height ?? img.Height);
			}
		}

		[LuaMethodExample("gui.clearImageCache( );")]
		[LuaMethod("clearImageCache", "clears the image cache that is built up by using gui.drawImage, also releases the file handle for cached images")]
		public void ClearImageCache()
		{
			foreach (var image in _imageCache)
			{
				image.Value.Dispose();
			}

			_imageCache.Clear();
		}

		[LuaMethodExample("gui.drawImageRegion( \"C:\\sample.png\", 11, 22, 33, 44, 21, 43, 34, 45 );")]
		[LuaMethod("drawImageRegion", "draws a given region of an image file from the given path at the given coordinate, and optionally with the given size")]
		public void DrawImageRegion(string path, int source_x, int source_y, int source_width, int source_height, int dest_x, int dest_y, int? dest_width = null, int? dest_height = null)
		{
			if (!File.Exists(path))
			{
				Log($"File not found: {path}");
				return;
			}

			using (var g = GetGraphics())
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

				g.DrawImage(img, destRect, source_x, source_y, source_width, source_height, GraphicsUnit.Pixel);
			}
		}

		[LuaMethodExample("gui.drawLine( 161, 321, 162, 322, 0xFFFFFFFF );")]
		[LuaMethod("drawLine", "Draws a line from the first coordinate pair to the 2nd. Color is optional (if not specified it will be drawn black)")]
		public void DrawLine(int x1, int y1, int x2, int y2, Color? color = null)
		{
			using (var g = GetGraphics())
			{
				g.DrawLine(GetPen(color ?? _defaultForeground), x1, y1, x2, y2);
			}
		}

		[LuaMethodExample("gui.drawAxis( 16, 32, 15, 0xFFFFFFFF );")]
		[LuaMethod("drawAxis", "Draws an axis of the specified size at the coordinate pair.)")]
		public void DrawAxis(int x, int y, int size, Color? color = null)
		{
			DrawLine(x + size, y, x - size, y, color);
			DrawLine(x, y + size, x, y - size, color);
		}

		[LuaMethodExample("gui.drawPie( 16, 32, 77, 99, 180, 90, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod("drawPie", "draws a Pie shape at the given coordinates and the given width and height")]
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
			using (var g = GetGraphics())
			{
				var bg = background ?? _defaultBackground;
				if (bg.HasValue)
				{
					var brush = GetBrush(bg.Value);
					g.FillPie(brush, x, y, width, height, startangle, sweepangle);
				}

				g.DrawPie(GetPen(line ?? _defaultForeground), x + 1, y + 1, width - 1, height - 1, startangle, sweepangle);
			}
		}

		[LuaMethodExample("gui.drawPixel( 16, 32, 0xFFFFFFFF );")]
		[LuaMethod("drawPixel", "Draws a single pixel at the given coordinates in the given color. Color is optional (if not specified it will be drawn black)")]
		public void DrawPixel(int x, int y, Color? color = null)
		{
			using (var g = GetGraphics())
			{
				try
				{
					g.DrawLine(GetPen(color ?? _defaultForeground), x, y, x + 0.1F, y);
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		[LuaMethodExample("gui.drawPolygon( { { 5, 10 }, { 10, 10 }, { 10, 20 }, { 5, 20 } }, 10, 30, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod("drawPolygon", "Draws a polygon using the table of coordinates specified in points. This should be a table of tables(each of size 2). If x or y is passed, the polygon will be translated by the passed coordinate pair. Line is the color of the polygon. Background is the optional fill color")]
		public void DrawPolygon(LuaTable points, int? offsetX = null, int? offsetY = null, Color? line = null, Color? background = null)
		{
			using (var g = GetGraphics())
			{
				try
				{
					var pointsArr = new Point[points.Values.Count];
					var i = 0;
					foreach (LuaTable point in points.Values)
					{
						pointsArr[i] = new Point(LuaInt(point[1]) + (offsetX ?? 0), LuaInt(point[2]) + (offsetY ?? 0));
						i++;
					}

					g.DrawPolygon(GetPen(line ?? _defaultForeground), pointsArr);
					var bg = background ?? _defaultBackground;
					if (bg.HasValue)
					{
						g.FillPolygon(GetBrush(bg.Value), pointsArr);
					}
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		[LuaMethodExample("gui.drawRectangle( 16, 32, 77, 99, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod("drawRectangle", "Draws a rectangle at the given coordinate and the given width and height. Line is the color of the box. Background is the optional fill color")]
		public void DrawRectangle(int x, int y, int width, int height, Color? line = null, Color? background = null)
		{
			using (var g = GetGraphics())
			{
				g.DrawRectangle(GetPen(line ?? _defaultForeground), x, y, width, height);
				var bg = background ?? _defaultBackground;
				if (bg.HasValue)
				{
					g.FillRectangle(GetBrush(bg.Value), x + 1, y + 1, width - 1, height - 1);
				}
			}
		}

		[LuaMethodExample("gui.drawString( 16, 32, \"Some message\", 0x7F0000FF, 0x00007FFF, 8, \"Arial Narrow\", \"bold\", \"center\", \"middle\" );")]
		[LuaMethod("drawString", "Alias of gui.drawText()")]
		public void DrawString(
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
			DrawText(x, y, message, forecolor, backcolor, fontsize, fontfamily, fontstyle, horizalign, vertalign);
		}

		[LuaMethodExample("gui.drawText( 16, 32, \"Some message\", 0x7F0000FF, 0x00007FFF, 8, \"Arial Narrow\", \"bold\", \"center\", \"middle\" );")]
		[LuaMethod("drawText", "Draws the given message in the emulator screen space (like all draw functions) at the given x,y coordinates and the given color. The default color is white. A fontfamily can be specified and is monospace generic if none is specified (font family options are the same as the .NET FontFamily class). The fontsize default is 12. The default font style is regular. Font style options are regular, bold, italic, strikethrough, underline. Horizontal alignment options are left (default), center, or right. Vertical alignment options are bottom (default), middle, or top. Alignment options specify which ends of the text will be drawn at the x and y coordinates. For pixel-perfect font look, make sure to disable aspect ratio correction.")]
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
			using (var g = GetGraphics())
			{
				try
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

					// The text isn't written out using GenericTypographic, so measuring it using GenericTypographic seemed to make it worse.
					// And writing it out with GenericTypographic just made it uglier. :p
					var f = new StringFormat(StringFormat.GenericDefault);
					var font = new Font(family, fontsize ?? 12, fstyle, GraphicsUnit.Pixel);
					Size sizeOfText = g.MeasureString(message, font, 0, f).ToSize();
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
					g.FillRectangle(GetBrush(backcolor ?? _defaultTextBackground.Value), rect);
					g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
					g.DrawString(message, font, GetBrush(forecolor ?? _defaultForeground), x, y);
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		[LuaMethodExample("gui.pixelText( 16, 32, \"Some message\", 0x7F0000FF, 0x00007FFF, \"Arial Narrow\" );")]
		[LuaMethod("pixelText", "Draws the given message in the emulator screen space (like all draw functions) at the given x,y coordinates and the given color. The default color is white. Two font families are available, \"fceux\" and \"gens\" (or  \"0\" and \"1\" respectively), both are monospace and have the same size as in the emulaors they've been taken from. If no font family is specified, it uses \"gens\" font, unless that's overridden via gui.defaultPixelFont()")]
		public void DrawText(
			int x,
			int y,
			string message,
			Color? forecolor = null,
			Color? backcolor = null,
			string fontfamily = null)
		{
			using (var g = GetGraphics())
			{
				try
				{
					var index = 0;
					if (string.IsNullOrEmpty(fontfamily))
					{
						index = _defaultPixelFont;
					}
					else
					{
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
								Log($"Unable to find font family: {fontfamily}");
								return;
						}
					}

					var f = new StringFormat(StringFormat.GenericTypographic)
					{
						FormatFlags = StringFormatFlags.MeasureTrailingSpaces
					};
					var font = new Font(GlobalWin.DisplayManager.CustomFonts.Families[index], 8, FontStyle.Regular, GraphicsUnit.Pixel);
					Size sizeOfText = g.MeasureString(message, font, 0, f).ToSize();
					var rect = new Rectangle(new Point(x, y), sizeOfText + new Size(1, 0));
					g.FillRectangle(GetBrush(backcolor ?? _defaultTextBackground.Value), rect);
					g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
					g.DrawString(message, font, GetBrush(forecolor ?? _defaultForeground), x, y);
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		[LuaMethodExample("gui.text( 16, 32, \"Some message\", 0x7F0000FF, \"bottomleft\" );")]
		[LuaMethod("text", "Displays the given text on the screen at the given coordinates. Optional Foreground color. The optional anchor flag anchors the text to one of the four corners. Anchor flag parameters: topleft, topright, bottomleft, bottomright")]
		public void Text(
			int x,
			int y,
			string message,
			Color? forecolor = null,
			string anchor = null)
		{
			var a = 0;

			if (!string.IsNullOrEmpty(anchor))
			{
				switch (anchor)
				{
					case "0":
					case "topleft":
						a = 0;
						break;
					case "1":
					case "topright":
						a = 1;
						break;
					case "2":
					case "bottomleft":
						a = 2;
						break;
					case "3":
					case "bottomright":
						a = 3;
						break;
				}
			}
			else
			{
				x -= Emulator.CoreComm.ScreenLogicalOffsetX;
				y -= Emulator.CoreComm.ScreenLogicalOffsetY;
			}

			GlobalWin.OSD.AddGuiText(message, x, y, Color.Black, forecolor ?? Color.White, a);
		}

		[LuaMethodExample("local nlguicre = gui.createcanvas( 77, 99, 2, 48 );")]
		[LuaMethod("createcanvas", "Creates a canvas of the given size and, if specified, the given coordinates.")]
		public LuaTable Text(int width, int height, int? x = null, int? y = null)
		{
			var canvas = new LuaCanvas(width, height, x, y);
			canvas.Show();
			return LuaHelper.ToLuaTable(Lua, canvas);
		}
	}
}
