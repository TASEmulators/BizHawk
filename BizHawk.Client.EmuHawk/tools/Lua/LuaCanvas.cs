using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using BizHawk.Client.Common;
using NLua;
using System;
using System.Collections.Generic;

namespace BizHawk.Client.EmuHawk
{
	[Description("Represents a canvas object returned by the gui.createcanvas() method")]
	public partial class LuaCanvas : Form
	{
		//public List<LuaEvent> ControlEvents { get; } = new List<LuaEvent>();

		public LuaCanvas(int width, int height, int? x = null, int? y = null)
		{
			InitializeComponent();
			luaPictureBox.Width = width;
			luaPictureBox.Height = height;
			luaPictureBox.Image = new Bitmap(width, height);

			if (x.HasValue)
			{
				StartPosition = System.Windows.Forms.FormStartPosition.Manual;
				Left = (int)x;
				if (y.HasValue)
				{
					Top = (int)y;
				}
			}
		}

		[LuaMethod(
			"setTitle",
			"Sets the canvas window title")]
		public void SetTitle(string title)
		{
			Text = title;
		}

		[LuaMethod(
			"setLocation",
			"Sets the location of the canvas window")]
		public void SetLocation(int x, int y) 
		{
			StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			Left = (int)x;
			Top = (int)y;
		}

		[LuaMethod(
			"clear",
			"Clears the canvas")]
		public void Clear(Color color)
		{
			luaPictureBox.Clear(color);
		}

		[LuaMethod(
			"refresh",
			"Redraws the canvas")]
		public new void Refresh()
		{
			luaPictureBox.Refresh();
		}

		[LuaMethod(
			"setDefaultForegroundColor",
			"Sets the default foreground color to use in drawing methods, white by default")]
		public void SetDefaultForegroundColor(Color color)
		{
			luaPictureBox.SetDefaultForegroundColor(color);
		}

		[LuaMethod(
			"setDefaultBackgroundColor",
			"Sets the default background color to use in drawing methods, transparent by default")]
		public void SetDefaultBackgroundColor(Color color)
		{
			luaPictureBox.SetDefaultBackgroundColor(color);
		}

		[LuaMethod(
			"setDefaultTextBackground",
			"Sets the default backgroiund color to use in text drawing methods, half-transparent black by default")]
		public void SetDefaultTextBackground(Color color)
		{
			luaPictureBox.SetDefaultTextBackground(color);
		}

		[LuaMethod(
			"drawBezier",
			"Draws a Bezier curve using the table of coordinates provided in the given color")]
		public void DrawBezier(LuaTable points, Color color)
		{
			try
			{
				luaPictureBox.DrawBezier(points, color);
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
				return;
			}
		}

		[LuaMethod(
			"drawBox",
			"Draws a rectangle on screen from x1/y1 to x2/y2. Same as drawRectangle except it receives two points intead of a point and width/height")]
		public void DrawBox(int x, int y, int x2, int y2, Color? line = null, Color? background = null)
		{
			try
			{
				luaPictureBox.DrawBox(x, y, x2, y2, line, background);
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
				return;
			}
		}

		[LuaMethod(
			"drawEllipse",
			"Draws an ellipse at the given coordinates and the given width and height. Line is the color of the ellipse. Background is the optional fill color")]
		public void DrawEllipse(int x, int y, int width, int height, Color? line = null, Color? background = null)
		{
			try
			{
				luaPictureBox.DrawEllipse(x, y, width, height, line, background);
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
				return;
			}
		}

		[LuaMethod(
			"drawIcon",
			"draws an Icon (.ico) file from the given path at the given coordinate. width and height are optional. If specified, it will resize the image accordingly")]
		public void DrawIcon(string path, int x, int y, int? width = null, int? height = null)
		{
			try
			{
				luaPictureBox.DrawIcon(path, x, y, width, height);
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
				return;
			}
		}

		[LuaMethod(
			"drawImage",
			"draws an image file from the given path at the given coordinate. width and height are optional. If specified, it will resize the image accordingly")]
		public void DrawImage(string path, int x, int y, int? width = null, int? height = null, bool cache = true)
		{
			if (!File.Exists(path))
			{
				ConsoleLuaLibrary.Log("File not found: " + path + "\nScript Terminated");
				return;
			}

			luaPictureBox.DrawImage(path, x, y, width, height, cache);
		}

		[LuaMethod(
			"clearImageCache",
			"clears the image cache that is built up by using gui.drawImage, also releases the file handle for cached images")]
		public void ClearImageCache()
		{
			luaPictureBox.ClearImageCache();
		}

		[LuaMethod(
			"drawImageRegion",
			"draws a given region of an image file from the given path at the given coordinate, and optionally with the given size")]
		public void DrawImageRegion(string path, int source_x, int source_y, int source_width, int source_height, int dest_x, int dest_y, int? dest_width = null, int? dest_height = null)
		{
			if (!File.Exists(path))
			{
				ConsoleLuaLibrary.Log("File not found: " + path + "\nScript Terminated");
				return;
			}

			luaPictureBox.DrawImageRegion(path, source_x, source_y, source_width, source_height, dest_x, dest_y, dest_width, dest_height);
		}

		[LuaMethod(
			"drawLine",
			"Draws a line from the first coordinate pair to the 2nd. Color is optional (if not specified it will be drawn black)")]
		public void DrawLine(int x1, int y1, int x2, int y2, Color? color = null)
		{
			luaPictureBox.DrawLine(x1, y1, x2, y2, color);
		}

		[LuaMethod(
			"drawAxis",
			"Draws an axis of the specified size at the coordinate pair.)")]
		public void DrawAxis(int x, int y, int size, Color? color = null)
		{
			luaPictureBox.DrawAxis(x, y, size, color);
		}

		[LuaMethod(
			"drawArc",
			"draws a Arc shape at the given coordinates and the given width and height"
		)]
		public void DrawArc(int x, int y, int width, int height, int startangle, int sweepangle, Color? line = null)
		{
			luaPictureBox.DrawArc(x, y, width, height, startangle, sweepangle, line);
		}

		[LuaMethod(
			"drawPie",
			"draws a Pie shape at the given coordinates and the given width and height")]
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
			luaPictureBox.DrawPie(x, y, width, height, startangle, sweepangle, line, background);
		}

		[LuaMethod(
			"drawPixel",
			"Draws a single pixel at the given coordinates in the given color. Color is optional (if not specified it will be drawn black)")]
		public void DrawPixel(int x, int y, Color? color = null)
		{
			try
			{
				luaPictureBox.DrawPixel(x, y, color);
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
				return;
			}
		}

		[LuaMethod(
			"drawPolygon",
			"Draws a polygon using the table of coordinates specified in points. This should be a table of tables(each of size 2). Line is the color of the polygon. Background is the optional fill color")]
		public void DrawPolygon(LuaTable points, Color? line = null, Color? background = null)
		{
			try
			{
				luaPictureBox.DrawPolygon(points, line, background);
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
				return;
			}
		}


		[LuaMethod(
			"drawRectangle",
			"Draws a rectangle at the given coordinate and the given width and height. Line is the color of the box. Background is the optional fill color")]
		public void DrawRectangle(int x, int y, int width, int height, Color? line = null, Color? background = null)
		{
			luaPictureBox.DrawRectangle(x, y, width, height, line, background);
		}

		[LuaMethod(
			"drawString",
			"Alias of DrawText()")]
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
			luaPictureBox.DrawText(x, y, message, forecolor, backcolor, fontsize, fontfamily, fontstyle, horizalign, vertalign);
		}

		[LuaMethod(
			"drawText",
			"Draws the given message at the given x,y coordinates and the given color. The default color is white. A fontfamily can be specified and is monospace generic if none is specified (font family options are the same as the .NET FontFamily class). The fontsize default is 12. The default font style is regular. Font style options are regular, bold, italic, strikethrough, underline. Horizontal alignment options are left (default), center, or right. Vertical alignment options are bottom (default), middle, or top. Alignment options specify which ends of the text will be drawn at the x and y coordinates.")]
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
			luaPictureBox.DrawText(x, y, message, forecolor, backcolor, fontsize, fontfamily, fontstyle, horizalign, vertalign);
		}


		// It'd be great if these were simplified into 1 function, but I cannot figure out how to return a LuaTable from this class
		[LuaMethod(
			"getMouseX",
			"Returns an integer representation of the mouse X coordinate relative to the canvas window.")]
		public int GetMouseX()
		{
			var position = luaPictureBox.GetMouse();
			return position.X;
		}

		[LuaMethod(
			"getMouseY",
			"Returns an integer representation of the mouse Y coordinate relative to the canvas window.")]
		public int GetMouseY()
		{
			var position = luaPictureBox.GetMouse();
			return position.Y;
		}
	}
}
