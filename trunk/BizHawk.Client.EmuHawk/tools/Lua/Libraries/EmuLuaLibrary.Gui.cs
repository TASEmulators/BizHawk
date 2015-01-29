using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;

using LuaInterface;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class GuiLuaLibrary : LuaLibraryBase
	{
		[RequiredService]
		public IEmulator Emulator { get; set; }

		public GuiLuaLibrary(Lua lua)
			: base(lua) { }

		public GuiLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name { get { return "gui"; } }

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

		public bool SurfaceIsNull
		{
			get
			{
				return _luaSurface == null;
			}
		}

		[LuaMethodAttributes(
			"DrawNew",
			"Changes drawing target to the specified lua surface name. This may clobber any previous drawing to this surface."
		)]
		public void DrawNew(string name)
		{
			try
			{
				DrawFinish();
				_luaSurface = GlobalWin.DisplayManager.LockLuaSurface(name);
			}
			catch (InvalidOperationException ex)
			{
				Log(ex.ToString());
			}
		}

		public void DrawFinish()
		{
			if(_luaSurface != null)
				GlobalWin.DisplayManager.UnlockLuaSurface(_luaSurface);
			_luaSurface = null;
		}

		#endregion

		#region Helpers

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

			var tx = Global.Emulator.CoreComm.ScreenLogicalOffsetX;
			var ty = Global.Emulator.CoreComm.ScreenLogicalOffsetY;
			if (tx != 0 || ty != 0)
			{
				var transform = g.Transform;
				transform.Translate(-tx, -ty);
				g.Transform = transform;
			}

			return g;
		}

		#endregion

		[LuaMethodAttributes(
			"addmessage",
			"Adds a message to the OSD's message area"
		)]
		public void AddMessage(string message)
		{
			GlobalWin.OSD.AddMessage(message);
		}

		[LuaMethodAttributes(
			"clearGraphics",
			"clears all lua drawn graphics from the screen"
		)]
		public void ClearGraphics()
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			_luaSurface.Clear();
		}

		[LuaMethodAttributes(
			"cleartext",
			"clears all text created by gui.text()"
		)]
		public static void ClearText()
		{
			GlobalWin.OSD.ClearGUIText();
		}

		[LuaMethodAttributes(
			"drawBezier",
			"Draws a Bezier curve using the table of coordinates provided in the given color"
		)]
		public void DrawBezier(LuaTable points, Color color)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
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

		[LuaMethodAttributes(
			"drawBox",
			"Draws a rectangle on screen from x1/y1 to x2/y2. Same as drawRectangle except it receives two points intead of a point and width/height"
		)]
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

					g.DrawRectangle(GetPen(line ?? Color.White), x, y, x2, y2);
					if (background.HasValue)
					{
						g.FillRectangle(GetBrush(background.Value), x, y, x2, y2);
					}
				}
				catch (Exception)
				{
					// need to stop the script from here
					return;
				}
			}
		}

		[LuaMethodAttributes(
			"drawEllipse",
			"Draws an ellipse at the given coordinates and the given width and height. Line is the color of the ellipse. Background is the optional fill color"
		)]
		public void DrawEllipse(int x, int y, int width, int height, Color? line, Color? background = null)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					g.DrawEllipse(GetPen(line ?? Color.White), x, y, width, height);
					if (background.HasValue)
					{
						var brush = GetBrush(background.Value);
						g.FillEllipse(brush, x, y, width, height);
					}
				}
				catch (Exception)
				{
					// need to stop the script from here
					return;
				}
			}
		}

		[LuaMethodAttributes(
			"drawIcon",
			"draws an Icon (.ico) file from the given path at the given coordinate. width and height are optional. If specified, it will resize the image accordingly"
		)]
		public void DrawIcon(string path, int x, int y, int? width = null, int? height = null)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
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

		[LuaMethodAttributes(
			"drawImage",
			"draws an image file from the given path at the given coordinate. width and height are optional. If specified, it will resize the image accordingly"
		)]
		public void DrawImage(string path, int x, int y, int? width = null, int? height = null)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				var img = Image.FromFile(path);
				g.DrawImage(img, x, y, width ?? img.Width, height ?? img.Height);
			}
		}

		[LuaMethodAttributes(
			"drawLine",
			"Draws a line from the first coordinate pair to the 2nd. Color is optional (if not specified it will be drawn black)"
		)]
		public void DrawLine(int x1, int y1, int x2, int y2, Color? color = null)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				g.DrawLine(GetPen(color ?? Color.White), x1, y1, x2, y2);
			}
		}

		[LuaMethodAttributes(
			"drawPie",
			"draws a Pie shape at the given coordinates and the given width and height"
		)]
		public void DrawPie(
			int x,
			int y,
			int width,
			int height,
			int startangle,
			int sweepangle,
			Color line,
			Color? background = null)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				g.DrawPie(GetPen(line), x, y, width, height, startangle, sweepangle);
				if (background.HasValue)
				{
					var brush = GetBrush(background.Value);
					g.FillPie(brush, x, y, width, height, startangle, sweepangle);
				}
			}
		}

		[LuaMethodAttributes(
			"drawPixel",
			"Draws a single pixel at the given coordinates in the given color. Color is optional (if not specified it will be drawn black)"
		)]
		public void DrawPixel(int x, int y, Color? color = null)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					g.DrawLine(GetPen(color ?? Color.White), x, y, x + 0.1F, y);
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		[LuaMethodAttributes(
			"drawPolygon",
			"Draws a polygon using the table of coordinates specified in points. Line is the color of the polygon. Background is the optional fill color"
		)]
		public void DrawPolygon(LuaTable points, Color line, Color? background = null)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;

			using (var g = GetGraphics())
			{
				try
				{
					var pointsArr = new Point[points.Values.Count];
					var i = 0;
					foreach (LuaTable point in points.Values)
					{
						pointsArr[i] = new Point(LuaInt(point[1]), LuaInt(point[2]));
						i++;
					}

					g.DrawPolygon(GetPen(line), pointsArr);
					if (background.HasValue)
					{
						g.FillPolygon(GetBrush(background.Value), pointsArr);
					}
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		[LuaMethodAttributes(
			"drawRectangle",
			"Draws a rectangle at the given coordinate and the given width and height. Line is the color of the box. Background is the optional fill color"
		)]
		public void DrawRectangle(int x, int y, int width, int height, Color? line = null, Color? background = null)
		{
			using (var g = GetGraphics())
			{
				g.DrawRectangle(GetPen(line ?? Color.White), x, y, width, height);
				if (background.HasValue)
				{
					g.FillRectangle(GetBrush(background.Value), x, y, width, height);
				}
			}
		}

		[LuaMethodAttributes(
			"drawString",
			"Alias of gui.drawText()"
		)]
		public void DrawString(
			int x,
			int y,
			string message,
			Color? color = null,
			int? fontsize = null,
			string fontfamily = null,
			string fontstyle = null)
		{
			DrawText(x, y, message, color, fontsize, fontfamily, fontstyle);
		}

		[LuaMethodAttributes(
			"drawText",
			"Draws the given message in the emulator screen space (like all draw functions) at the given x,y coordinates and the given color. The default color is white. A fontfamily can be specified and is monospace generic if none is specified (font family options are the same as the .NET FontFamily class. The fontsize default is 12. The default font style. Font style options are regular, bold, italic, strikethrough, underline"
		)]
		public void DrawText(
			int x,
			int y,
			string message,
			Color? color = null,
			int? fontsize = null,
			string fontfamily = null,
			string fontstyle = null)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
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

					var font = new Font(family, fontsize ?? 12, fstyle, GraphicsUnit.Pixel);
					g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
					g.DrawString(message, font, GetBrush(color ?? Color.White), x, y);
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		[LuaMethodAttributes(
			"text",
			"Displays the given text on the screen at the given coordinates. Optional Foreground and background colors. The optional anchor flag anchors the text to one of the four corners. Anchor flag parameters: topleft, topright, bottomleft, bottomright"
		)]
		public void Text(
			int x,
			int y,
			string message,
			Color? background = null,
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
				x -= Global.Emulator.CoreComm.ScreenLogicalOffsetX;
				y -= Global.Emulator.CoreComm.ScreenLogicalOffsetY;
			}

			GlobalWin.OSD.AddGUIText(message, x, y, background ?? Color.Black, forecolor ?? Color.White, a);
		}
	}
}
