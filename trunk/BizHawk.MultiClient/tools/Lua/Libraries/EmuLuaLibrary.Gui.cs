using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;

using LuaInterface;
using BizHawk.Client.Common;

namespace BizHawk.MultiClient
{
	public partial class EmuLuaLibrary
	{
		#region Gui Library Helpers

		private readonly Dictionary<Color, SolidBrush> SolidBrushes = new Dictionary<Color, SolidBrush>();
		private readonly Dictionary<Color, Pen> Pens = new Dictionary<Color, Pen>();

		public Color GetColor(object color)
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

		public SolidBrush GetBrush(object color)
		{
			Color c = GetColor(color);
			SolidBrush b;
			if (!SolidBrushes.TryGetValue(c, out b))
			{
				b = new SolidBrush(c);
				SolidBrushes[c] = b;
			}
			return b;
		}

		public Pen GetPen(object color)
		{
			Color c = GetColor(color);
			Pen p;
			if (!Pens.TryGetValue(c, out p))
			{
				p = new Pen(c);
				Pens[c] = p;
			}
			return p;
		}

		public void gui_clearGraphics()
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			luaSurface.Clear();
		}

		/// <summary>
		/// sets the current drawing context to a new surface.
		/// you COULD pass these back to lua to use as a target in rendering jobs, instead of setting it as current here.
		/// could be more powerful.
		/// performance test may reveal that repeatedly calling GetGraphics could be slow.
		/// we may want to make a class here in LuaImplementation that wraps a DisplaySurface and a Graphics which would be created once
		/// </summary>
		public void gui_drawNew()
		{
			luaSurface = GlobalWinF.DisplayManager.GetLuaSurfaceNative();
		}

		public void gui_drawNewEmu()
		{
			luaSurface = GlobalWinF.DisplayManager.GetLuaEmuSurfaceEmu();
		}

		/// <summary>
		/// finishes the current drawing and submits it to the display manager (at native [host] resolution pre-osd)
		/// you would probably want some way to specify which surface to set it to, when there are other surfaces.
		/// most notably, the client output [emulated] resolution 
		/// </summary>
		public void gui_drawFinish()
		{
			GlobalWinF.DisplayManager.SetLuaSurfaceNativePreOSD(luaSurface);
			luaSurface = null;
		}

		public void gui_drawFinishEmu()
		{
			GlobalWinF.DisplayManager.SetLuaSurfaceEmu(luaSurface);
			luaSurface = null;
		}

		Graphics GetGraphics()
		{
			var g = luaSurface.GetGraphics();
			int tx = Global.Emulator.CoreComm.ScreenLogicalOffsetX;
			int ty = Global.Emulator.CoreComm.ScreenLogicalOffsetY;
			if (tx != 0 || ty != 0)
			{
				var transform = g.Transform;
				transform.Translate(-tx, -ty);
				g.Transform = transform;
			}
			return g;
		}

		public DisplaySurface luaSurface;

		private void do_gui_text(object luaX, object luaY, object luaStr, bool alert, object background = null,
								 object forecolor = null, object anchor = null)
		{
			if (!alert)
			{
				if (forecolor == null)
					forecolor = "white";
				if (background == null)
					background = "black";
			}
			int dx = LuaInt(luaX);
			int dy = LuaInt(luaY);
			int a = 0;
			if (anchor != null)
			{
				int x;
				if (int.TryParse(anchor.ToString(), out x) == false)
				{
					if (anchor.ToString().ToLower() == "topleft")
						a = 0;
					else if (anchor.ToString().ToLower() == "topright")
						a = 1;
					else if (anchor.ToString().ToLower() == "bottomleft")
						a = 2;
					else if (anchor.ToString().ToLower() == "bottomright")
						a = 3;
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
			dx *= client_getwindowsize();
			dy *= client_getwindowsize();

			GlobalWinF.OSD.AddGUIText(luaStr.ToString(), dx, dy, alert, GetColor(background), GetColor(forecolor), a);
		}

		#endregion

		public void gui_addmessage(object luaStr)
		{
			GlobalWinF.OSD.AddMessage(luaStr.ToString());
		}

		public void gui_alert(object luaX, object luaY, object luaStr, object anchor = null)
		{
			do_gui_text(luaX, luaY, luaStr, true, null, null, anchor);
		}

		public void gui_cleartext()
		{
			GlobalWinF.OSD.ClearGUIText();
		}

		public void gui_drawBezier(LuaTable points, object color)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					Point[] Points = new Point[4];
					int i = 0;
					foreach (LuaTable point in points.Values)
					{
						Points[i] = new Point(LuaInt(point[1]), LuaInt(point[2]));
						i++;
						if (i >= 4)
							break;
					}

					g.DrawBezier(GetPen(color), Points[0], Points[1], Points[2], Points[3]);
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void gui_drawBox(object X, object Y, object X2, object Y2, object line = null, object background = null)
		{
			using (var g = GetGraphics())
			{
				try
				{
					int int_x = LuaInt(X);
					int int_y = LuaInt(Y);
					int int_width = LuaInt(X2);
					int int_height = LuaInt(Y2);

					if (int_x < int_width)
					{
						int_width = Math.Abs(int_x - int_width);
					}
					else
					{
						int_width = int_x - int_width;
						int_x -= int_width;
					}

					if (int_y < int_height)
					{
						int_height = Math.Abs(int_y - int_height);
					}
					else
					{
						int_height = int_y - int_height;
						int_y -= int_height;
					}

					g.DrawRectangle(GetPen(line ?? "white"), int_x, int_y, int_width, int_height);
					if (background != null)
					{
						g.FillRectangle(GetBrush(background), int_x, int_y, int_width, int_height);
					}
				}
				catch (Exception)
				{
					// need to stop the script from here
					return;
				}
			}
		}

		public void gui_drawEllipse(object X, object Y, object width, object height, object line, object background = null)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					g.DrawEllipse(GetPen(line ?? "white"), LuaInt(X), LuaInt(Y), LuaInt(width), LuaInt(height));
					if (background != null)
					{
						var brush = GetBrush(background);
						g.FillEllipse(brush, LuaInt(X), LuaInt(Y), LuaInt(width), LuaInt(height));
					}
				}
				catch (Exception)
				{
					// need to stop the script from here
					return;
				}
			}
		}

		public void gui_drawIcon(object Path, object x, object y, object width = null, object height = null)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					Icon icon;
					if (width != null && height != null)
					{
						icon = new Icon(Path.ToString(), LuaInt(width), LuaInt(height));
					}
					else
					{
						icon = new Icon(Path.ToString());
					}

					g.DrawIcon(icon, LuaInt(x), LuaInt(y));
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void gui_drawImage(object Path, object x, object y, object width = null, object height = null)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					Image img = Image.FromFile(Path.ToString());

					if (width == null || width.GetType() != typeof(int))
						width = img.Width.ToString();
					if (height == null || height.GetType() != typeof(int))
						height = img.Height.ToString();

					g.DrawImage(img, LuaInt(x), LuaInt(y), int.Parse(width.ToString()), int.Parse(height.ToString()));
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void gui_drawLine(object x1, object y1, object x2, object y2, object color = null)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
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

		public void gui_drawPie(object X, object Y, object width, object height, object startangle, object sweepangle,
								object line, object background = null)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					g.DrawPie(GetPen(line), LuaInt(X), LuaInt(Y), LuaInt(width), LuaInt(height), LuaInt(startangle), LuaInt(sweepangle));
					if (background != null)
					{
						var brush = GetBrush(background);
						g.FillPie(brush, LuaInt(X), LuaInt(Y), LuaInt(width), LuaInt(height), LuaInt(startangle), LuaInt(sweepangle));
					}
				}
				catch (Exception)
				{
					// need to stop the script from here
					return;
				}
			}
		}


		public void gui_drawPixel(object X, object Y, object color = null)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				float x = LuaInt(X) + 0.1F;
				try
				{
					g.DrawLine(GetPen(color ?? "white"), LuaInt(X), LuaInt(Y), x, LuaInt(Y));
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void gui_drawPolygon(LuaTable points, object line, object background = null)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			//this is a test
			using (var g = GetGraphics())
			{
				try
				{
					Point[] Points = new Point[points.Values.Count];
					int i = 0;
					foreach (LuaTable point in points.Values)
					{
						Points[i] = new Point(LuaInt(point[1]), LuaInt(point[2]));
						i++;
					}

					g.DrawPolygon(GetPen(line), Points);
					if (background != null)
					{
						var brush = GetBrush(background);
						g.FillPolygon(brush, Points);
					}
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void gui_drawRectangle(object X, object Y, object width, object height, object line, object background = null)
		{
			using (var g = GetGraphics())
			{
				try
				{
					int int_x = LuaInt(X);
					int int_y = LuaInt(Y);
					int int_width = LuaInt(width);
					int int_height = LuaInt(height);
					g.DrawRectangle(GetPen(line ?? "white"), int_x, int_y, int_width, int_height);
					if (background != null)
					{
						g.FillRectangle(GetBrush(background), int_x, int_y, int_width, int_height);
					}
				}
				catch (Exception)
				{
					// need to stop the script from here
					return;
				}
			}
		}


		public void gui_drawString(object X, object Y, object message, object color = null, object fontsize = null, object fontfamily = null, object fontstyle = null)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			gui_drawText(X, Y, message, color, fontsize, fontfamily, fontstyle);
		}

		public void gui_drawText(object X, object Y, object message, object color = null, object fontsize = null, object fontfamily = null, object fontstyle = null)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			using (var g = GetGraphics())
			{
				try
				{
					int fsize = 12;
					if (fontsize != null)
					{
						fsize = LuaInt(fontsize);
					}

					FontFamily family = FontFamily.GenericMonospace;
					if (fontfamily != null)
					{
						family = new FontFamily(fontfamily.ToString());
					}

					FontStyle fstyle = FontStyle.Regular;
					if (fontstyle != null)
					{
						string tmp = fontstyle.ToString().ToLower();
						switch (tmp)
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

					Font font = new Font(family, fsize, fstyle, GraphicsUnit.Pixel);
					g.DrawString(message.ToString(), font, GetBrush(color ?? "white"), LuaInt(X), LuaInt(Y));
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void gui_text(object luaX, object luaY, object luaStr, object background = null, object forecolor = null,
							 object anchor = null)
		{
			do_gui_text(luaX, luaY, luaStr, false, background, forecolor, anchor);
		}
	}
}
