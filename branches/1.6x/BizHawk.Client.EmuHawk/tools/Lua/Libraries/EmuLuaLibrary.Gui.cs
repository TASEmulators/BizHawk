using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;

using BizHawk.Client.Common;
using LuaInterface;

namespace BizHawk.Client.EmuHawk
{
	public class GuiLuaLibrary : LuaLibraryBase
	{
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

		/// <summary>
		/// sets the current drawing context to a new surface.
		/// you COULD pass these back to lua to use as a target in rendering jobs, instead of setting it as current here.
		/// could be more powerful.
		/// performance test may reveal that repeatedly calling GetGraphics could be slow.
		/// we may want to make a class here in LuaImplementation that wraps a DisplaySurface and a Graphics which would be created once
		/// </summary>
		public void DrawNew()
		{
			_luaSurface = GlobalWin.DisplayManager.GetLuaSurfaceNative();
		}

		public void DrawNewEmu()
		{
			_luaSurface = GlobalWin.DisplayManager.GetLuaEmuSurfaceEmu();
		}

		/// <summary>
		/// finishes the current drawing and submits it to the display manager (at native [host] resolution pre-osd)
		/// you would probably want some way to specify which surface to set it to, when there are other surfaces.
		/// most notably, the client output [emulated] resolution 
		/// </summary>
		public void DrawFinish()
		{
			GlobalWin.DisplayManager.SetLuaSurfaceNativePreOSD(_luaSurface);
			_luaSurface = null;
		}

		public void DrawFinishEmu()
		{
			GlobalWin.DisplayManager.SetLuaSurfaceEmu(_luaSurface);
			_luaSurface = null;
		}

		#endregion

		#region Helpers

		private readonly Dictionary<Color, SolidBrush> _solidBrushes = new Dictionary<Color, SolidBrush>();
		private readonly Dictionary<Color, Pen> _pens = new Dictionary<Color, Pen>();
		private readonly Bitmap _nullGraphicsBitmap = new Bitmap(1, 1);

		private DisplaySurface _luaSurface;

		private static Color GetColor(object color)
		{
			if (color is double)
			{
				return Color.FromArgb(int.Parse(long.Parse(color.ToString()).ToString("X"), NumberStyles.HexNumber));
			}
			else
			{
				return Color.FromName(color.ToString().ToLower());
			}
		}

		private SolidBrush GetBrush(object color)
		{
			var c = GetColor(color);
			SolidBrush b;
			if (!_solidBrushes.TryGetValue(c, out b))
			{
				b = new SolidBrush(c);
				_solidBrushes[c] = b;
			}

			return b;
		}

		private Pen GetPen(object color)
		{
			var c = GetColor(color);
			Pen p;
			if (!_pens.TryGetValue(c, out p))
			{
				p = new Pen(c);
				_pens[c] = p;
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

		private static void DoGuiText(
			object x,
			object y,
			string message,
			bool alert,
			object background = null,
			object forecolor = null, 
			object anchor = null)
		{
			if (!alert)
			{
				if (forecolor == null)
				{
					forecolor = "white";
				}

				if (background == null)
				{
					background = "black";
				}
			}

			var dx = LuaInt(x);
			var dy = LuaInt(y);
			var a = 0;
			
			if (anchor != null)
			{
				int dummy;
				if (int.TryParse(anchor.ToString(), out dummy) == false)
				{
					if (anchor.ToString().ToLower() == "topleft")
					{
						a = 0;
					}
					else if (anchor.ToString().ToLower() == "topright")
					{
						a = 1;
					}
					else if (anchor.ToString().ToLower() == "bottomleft")
					{
						a = 2;
					}
					else if (anchor.ToString().ToLower() == "bottomright")
					{
						a = 3;
					}
				}
				else
				{
					a = LuaInt(anchor);
				}
			}
			else
			{
				dx -= Global.Emulator.CoreComm.ScreenLogicalOffsetX;
				dy -= Global.Emulator.CoreComm.ScreenLogicalOffsetY;
			}

			// blah hacks
			dx *= EmuHawkLuaLibrary.GetWindowSize();
			dy *= EmuHawkLuaLibrary.GetWindowSize();

			GlobalWin.OSD.AddGUIText(message, dx, dy, alert, GetColor(background), GetColor(forecolor), a);
		}

		#endregion

		[LuaMethodAttributes(
			"addmessage",
			"TODO"
		)]
		public void AddMessage(object luaStr)
		{
			GlobalWin.OSD.AddMessage(luaStr.ToString());
		}

		[LuaMethodAttributes(
			"alert",
			"TODO"
		)]
		public void Alert(object x, object y, string message, object anchor = null)
		{
			DoGuiText(x, y, message, true, null, null, anchor); // TODO: refactor DoGuiText to take luaStr as string and refactor
		}

		[LuaMethodAttributes(
			"clearGraphics",
			"TODO"
		)]
		public void ClearGraphics()
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			_luaSurface.Clear();
		}

		[LuaMethodAttributes(
			"cleartext",
			"TODO"
		)]
		public static void ClearText()
		{
			GlobalWin.OSD.ClearGUIText();
		}

		[LuaMethodAttributes(
			"drawBezier",
			"TODO"
		)]
		public void DrawBezier(LuaTable points, object color)
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
			"TODO"
		)]
		public void DrawBox(object x, object y, object x2, object y2, object line = null, object background = null)
		{
			using (var g = GetGraphics())
			{
				try
				{
					var intX = LuaInt(x);
					var intY = LuaInt(y);
					var intWidth = LuaInt(x2);
					var intHeight = LuaInt(y2);

					if (intX < intWidth)
					{
						intWidth = Math.Abs(intX - intWidth);
					}
					else
					{
						intWidth = intX - intWidth;
						intX -= intWidth;
					}

					if (intY < intHeight)
					{
						intHeight = Math.Abs(intY - intHeight);
					}
					else
					{
						intHeight = intY - intHeight;
						intY -= intHeight;
					}

					g.DrawRectangle(GetPen(line ?? "white"), intX, intY, intWidth, intHeight);
					if (background != null)
					{
						g.FillRectangle(GetBrush(background), intX, intY, intWidth, intHeight);
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
			"TODO"
		)]
		public void DrawEllipse(object x, object y, object width, object height, object line, object background = null)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					g.DrawEllipse(GetPen(line ?? "white"), LuaInt(x), LuaInt(y), LuaInt(width), LuaInt(height));
					if (background != null)
					{
						var brush = GetBrush(background);
						g.FillEllipse(brush, LuaInt(x), LuaInt(y), LuaInt(width), LuaInt(height));
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
			"TODO"
		)]
		public void DrawIcon(string path, object x, object y, object width = null, object height = null)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					Icon icon;
					if (width != null && height != null)
					{
						icon = new Icon(path, LuaInt(width), LuaInt(height));
					}
					else
					{
						icon = new Icon(path);
					}

					g.DrawIcon(icon, LuaInt(x), LuaInt(y));
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		[LuaMethodAttributes(
			"drawImage",
			"TODO"
		)]
		public void DrawImage(string path, object x, object y, object width = null, object height = null)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					var img = Image.FromFile(path);

					if (width == null || width.GetType() != typeof(int))
					{
						width = img.Width.ToString();
					}

					if (height == null || height.GetType() != typeof(int))
					{
						height = img.Height.ToString();
					}

					g.DrawImage(img, LuaInt(x), LuaInt(y), LuaInt(width), LuaInt(height));
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		[LuaMethodAttributes(
			"drawLine",
			"TODO"
		)]
		public void DrawLine(object x1, object y1, object x2, object y2, object color = null)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					g.DrawLine(GetPen(color ?? "white"), LuaInt(x1), LuaInt(y1), LuaInt(x2), LuaInt(y2));
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		[LuaMethodAttributes(
			"drawPie",
			"TODO"
		)]
		public void DrawPie(
			object x,
			object y,
			object width,
			object height,
			object startangle,
			object sweepangle,
			object line,
			object background = null)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					g.DrawPie(GetPen(line), LuaInt(x), LuaInt(y), LuaInt(width), LuaInt(height), LuaInt(startangle), LuaInt(sweepangle));
					if (background != null)
					{
						var brush = GetBrush(background);
						g.FillPie(brush, LuaInt(x), LuaInt(y), LuaInt(width), LuaInt(height), LuaInt(startangle), LuaInt(sweepangle));
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
			"drawPixel",
			"TODO"
		)]
		public void DrawPixel(object x, object y, object color = null)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					g.DrawLine(GetPen(color ?? "white"), LuaInt(x), LuaInt(y), LuaInt(x) + 0.1F, LuaInt(y));
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		[LuaMethodAttributes(
			"drawPolygon",
			"TODO"
		)]
		public void DrawPolygon(LuaTable points, object line, object background = null)
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
					if (background != null)
					{
						g.FillPolygon(GetBrush(background), pointsArr);
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
			"TODO"
		)]
		public void DrawRectangle(object x, object y, object width, object height, object line, object background = null)
		{
			using (var g = GetGraphics())
			{
				try
				{
					var intX = LuaInt(x);
					var intY = LuaInt(y);
					var intWidth = LuaInt(width);
					var intHeight = LuaInt(height);
					g.DrawRectangle(GetPen(line ?? "white"), intX, intY, intWidth, intHeight);
					if (background != null)
					{
						g.FillRectangle(GetBrush(background), intX, intY, intWidth, intHeight);
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
			"drawString",
			"TODO"
		)]
		public void DrawString(
			object x,
			object y,
			string message,
			object color = null,
			object fontsize = null,
			string fontfamily = null,
			string fontstyle = null)
		{
			DrawText(x, y, message, color, fontsize, fontfamily, fontstyle);
		}

		[LuaMethodAttributes(
			"drawText",
			"TODO"
		)]
		public void DrawText(
			object x,
			object y,
			string message,
			object color = null,
			object fontsize = null,
			string fontfamily = null,
			string fontstyle = null)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					var fsize = 12;
					if (fontsize != null)
					{
						fsize = LuaInt(fontsize);
					}

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

					var font = new Font(family, fsize, fstyle, GraphicsUnit.Pixel);
					g.DrawString(message, font, GetBrush(color ?? "white"), LuaInt(x), LuaInt(y));
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		[LuaMethodAttributes(
			"text",
			"TODO"
		)]
		public void Text(
			object x,
			object y,
			string message,
			object background = null,
			object forecolor = null,
			object anchor = null)
		{
			DoGuiText(x, y, message, false, background, forecolor, anchor);
		}
	}
}
