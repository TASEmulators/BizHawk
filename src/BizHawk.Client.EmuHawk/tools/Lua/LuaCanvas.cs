using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using BizHawk.Client.Common;
using BizHawk.Common.PathExtensions;

using NLua;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.EmuHawk
{
	[Description("Represents a canvas object returned by the gui.createcanvas() method")]
	public sealed class LuaCanvas : Form
	{
		private readonly EmulationLuaLibrary _emuLib;

		private readonly NLuaTableHelper _th;

		private readonly Action<string> LogOutputCallback;

		private readonly LuaPictureBox luaPictureBox;

		public LuaCanvas(
			EmulationLuaLibrary emuLib,
			int width,
			int height,
			int? x,
			int? y,
			NLuaTableHelper tableHelper,
			Action<string> logOutputCallback)
		{
			_emuLib = emuLib;
			_th = tableHelper;
			LogOutputCallback = logOutputCallback;

			SuspendLayout();

			AutoScaleDimensions = new SizeF(6F, 13F);
			AutoScaleMode = AutoScaleMode.Font;
			AutoSize = true;
			AutoSizeMode = AutoSizeMode.GrowAndShrink;
			ClientSize = new Size(284, 261);
			FormBorderStyle = FormBorderStyle.FixedSingle;
			Name = "LuaCanvas";
			Text = "LuaCanvas";

			if (x.HasValue)
			{
				StartPosition = FormStartPosition.Manual;
				Left = (int)x;
				if (y.HasValue)
				{
					Top = (int)y;
				}
			}

			luaPictureBox = new(_th, LogOutputCallback)
			{
				Image = Properties.Resources.LuaPictureBox,
				Location = new Point(0, 0),
				Margin = new Padding(0),
				Name = "luaPictureBox",
				Size = new Size(100, 50),
				SizeMode = PictureBoxSizeMode.AutoSize,
				TabIndex = 0,
				TabStop = false
			};
			Controls.Add(luaPictureBox);

			ResumeLayout();

			// was this done after reflow for a reason? --yoshi
			luaPictureBox.Width = width;
			luaPictureBox.Height = height;
			luaPictureBox.Image = new Bitmap(width, height);
			PerformLayout();
		}

		[LuaMethodExample(
			"LuaCanvas.SetTitle( \"Title\" );")]
		[LuaMethod(
			"SetTitle",
			"Sets the canvas window title")]
		public void SetTitle(string title)
			=> Text = title;

		[LuaMethodExample(
			"LuaCanvas.SetLocation( 16, 32 );")]
		[LuaMethod(
			"SetLocation",
			"Sets the location of the canvas window")]
		public void SetLocation(int x, int y)
		{
			StartPosition = FormStartPosition.Manual;
			Left = x;
			Top = y;
		}

		[LuaMethodExample(
			"LuaCanvas.Clear( 0x000000FF );")]
		[LuaMethod(
			"Clear",
			"Clears the canvas")]
		public void Clear([LuaColorParam] object color)
		{
			luaPictureBox.Clear(_th.ParseColor(color));
		}

		[LuaMethodExample(
			"LuaCanvas.Refresh( );")]
		[LuaMethod(
			"Refresh",
			"Redraws the canvas")]
		public new void Refresh()
		{
			luaPictureBox.Refresh();
		}

		[LuaMethodExample(
			"LuaCanvas.SetDefaultForegroundColor( 0x000000FF );")]
		[LuaMethod(
			"SetDefaultForegroundColor",
			"Sets the default foreground color to use in drawing methods, white by default")]
		public void SetDefaultForegroundColor([LuaColorParam] object color)
		{
			luaPictureBox.SetDefaultForegroundColor(_th.ParseColor(color));
		}

		[LuaMethodExample(
			"LuaCanvas.SetDefaultBackgroundColor( 0x000000FF );")]
		[LuaMethod(
			"SetDefaultBackgroundColor",
			"Sets the default background color to use in drawing methods, transparent by default")]
		public void SetDefaultBackgroundColor([LuaColorParam] object color)
		{
			luaPictureBox.SetDefaultBackgroundColor(_th.ParseColor(color));
		}

		[LuaMethodExample(
			"LuaCanvas.SetDefaultTextBackground( 0x000000FF );")]
		[LuaMethod(
			"SetDefaultTextBackground",
			"Sets the default background color to use in text drawing methods, half-transparent black by default")]
		public void SetDefaultTextBackground([LuaColorParam] object color)
		{
			luaPictureBox.SetDefaultTextBackground(_th.ParseColor(color));
		}

		[LuaMethodExample(
			"LuaCanvas.DrawBezier( { { 5, 10 }, { 10, 10 }, { 10, 20 }, { 5, 20 } }, 0x000000FF );")]
		[LuaMethod(
			"DrawBezier",
			"Draws a Bezier curve using the table of coordinates provided in the given color")]
		public void DrawBezier(LuaTable points, [LuaColorParam] object color)
		{
			try
			{
				luaPictureBox.DrawBezier(points, _th.ParseColor(color));
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample(
			"LuaCanvas.DrawBox( 16, 32, 162, 322, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"DrawBox",
			"Draws a rectangle on screen from x1/y1 to x2/y2. Same as drawRectangle except it receives two points intead of a point and width/height")]
		public void DrawBox(
			int x,
			int y,
			int x2,
			int y2,
			[LuaColorParam] object line = null,
			[LuaColorParam] object background = null)
		{
			try
			{
				luaPictureBox.DrawBox(x, y, x2, y2, _th.SafeParseColor(line), _th.SafeParseColor(background));
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample(
			"LuaCanvas.DrawEllipse( 16, 32, 77, 99, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"DrawEllipse",
			"Draws an ellipse at the given coordinates and the given width and height. Line is the color of the ellipse. Background is the optional fill color")]
		public void DrawEllipse(
			int x,
			int y,
			int width,
			int height,
			[LuaColorParam] object line = null,
			[LuaColorParam] object background = null)
		{
			try
			{
				luaPictureBox.DrawEllipse(x, y, width, height, _th.SafeParseColor(line), _th.SafeParseColor(background));
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample(
			"LuaCanvas.DrawIcon( \"C:\\icon.ico\", 16, 32, 18, 24 );")]
		[LuaMethod(
			"DrawIcon",
			"draws an Icon (.ico) file from the given path at the given coordinate. width and height are optional. If specified, it will resize the image accordingly")]
		public void DrawIcon(string path, int x, int y, int? width = null, int? height = null)
		{
			try
			{
				luaPictureBox.DrawIcon(
					path: path,
					x: x,
					y: y,
					width: width,
					height: height,
					functionName: "(LuaCanvas).DrawIcon");
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample(
			"LuaCanvas.DrawImage( \"C:\\image.bmp\", 16, 32, 18, 24, false );")]
		[LuaMethod(
			"DrawImage",
			"draws an image file from the given path at the given coordinate. width and height are optional. If specified, it will resize the image accordingly")]
		public void DrawImage(
			string path,
			int x,
			int y,
			int? width = null,
			int? height = null,
			bool cache = true)
		{
			var path1 = path;
			if (!File.Exists(path1))
			{
				LogOutputCallback($"File not found: {path1}\nScript Terminated");
				return;
			}
			luaPictureBox.DrawImage(path1, x, y, width, height, cache);
		}

		[LuaMethodExample(
			"LuaCanvas.ClearImageCache( );")]
		[LuaMethod(
			"ClearImageCache",
			"clears the image cache that is built up by using gui.drawImage, also releases the file handle for cached images")]
		public void ClearImageCache()
		{
			luaPictureBox.ClearImageCache();
		}

		[LuaMethodExample(
			"LuaCanvas.DrawImageRegion( \"C:\\image.png\", 11, 22, 33, 44, 21, 43, 34, 45 );")]
		[LuaMethod(
			"DrawImageRegion",
			"draws a given region of an image file from the given path at the given coordinate, and optionally with the given size")]
		public void DrawImageRegion(
			string path,
			int sourceX,
			int sourceY,
			int sourceWidth,
			int sourceHeight,
			int destX,
			int destY,
			int? destWidth = null,
			int? destHeight = null)
		{
			var path1 = path;
			if (!File.Exists(path1))
			{
				LogOutputCallback($"File not found: {path1}\nScript Terminated");
				return;
			}
			luaPictureBox.DrawImageRegion(path1, sourceX, sourceY, sourceWidth, sourceHeight, destX, destY, destWidth, destHeight);
		}

		[LuaMethodExample(
			"LuaCanvas.DrawLine( 161, 321, 162, 322, 0xFFFFFFFF );")]
		[LuaMethod(
			"DrawLine",
			"Draws a line from the first coordinate pair to the 2nd. Color is optional (if not specified it will be drawn black)")]
		public void DrawLine(int x1, int y1, int x2, int y2, [LuaColorParam] object color = null)
		{
			luaPictureBox.DrawLine(x1, y1, x2, y2, _th.SafeParseColor(color));
		}

		[LuaMethodExample(
			"LuaCanvas.DrawAxis( 16, 32, int size, 0xFFFFFFFF );")]
		[LuaMethod(
			"DrawAxis",
			"Draws an axis of the specified size at the coordinate pair.)")]
		public void DrawAxis(int x, int y, int size, [LuaColorParam] object color = null)
		{
			luaPictureBox.DrawAxis(x, y, size, _th.SafeParseColor(color));
		}

		[LuaMethodExample(
			"LuaCanvas.DrawArc( 16, 32, 77, 99, 180, 90, 0x007F00FF );")]
		[LuaMethod(
			"DrawArc",
			"draws a Arc shape at the given coordinates and the given width and height"
		)]
		public void DrawArc(
			int x,
			int y,
			int width,
			int height,
			int startAngle,
			int sweepAngle,
			[LuaColorParam] object line = null)
		{
			luaPictureBox.DrawArc(x, y, width, height, startAngle, sweepAngle, _th.SafeParseColor(line));
		}

		[LuaMethodExample(
			"LuaCanvas.DrawPie( 16, 32, 77, 99, 180, 90, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"DrawPie",
			"draws a Pie shape at the given coordinates and the given width and height")]
		public void DrawPie(
			int x,
			int y,
			int width,
			int height,
			int startAngle,
			int sweepAngle,
			[LuaColorParam] object line = null,
			[LuaColorParam] object background = null)
		{
			luaPictureBox.DrawPie(x, y, width, height, startAngle, sweepAngle, _th.SafeParseColor(line), _th.SafeParseColor(background));
		}

		[LuaMethodExample(
			"LuaCanvas.DrawPixel( 16, 32, 0xFFFFFFFF );")]
		[LuaMethod(
			"DrawPixel",
			"Draws a single pixel at the given coordinates in the given color. Color is optional (if not specified it will be drawn black)")]
		public void DrawPixel(int x, int y, [LuaColorParam] object color = null)
		{
			try
			{
				luaPictureBox.DrawPixel(x, y, _th.SafeParseColor(color));
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample(
			"LuaCanvas.DrawPolygon( { 10, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"DrawPolygon",
			"Draws a polygon using the table of coordinates specified in points. This should be a table of tables(each of size 2). Line is the color of the polygon. Background is the optional fill color")]
		public void DrawPolygon(
			LuaTable points,
			int? x = null,
			int? y = null,
			[LuaColorParam] object line = null,
			[LuaColorParam] object background = null)
		{
			try
			{
				luaPictureBox.DrawPolygon(points, x, y, _th.SafeParseColor(line), _th.SafeParseColor(background));
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}


		[LuaMethodExample(
			"LuaCanvas.DrawRectangle( 16, 32, 77, 99, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"DrawRectangle",
			"Draws a rectangle at the given coordinate and the given width and height. Line is the color of the box. Background is the optional fill color")]
		public void DrawRectangle(
			int x,
			int y,
			int width,
			int height,
			[LuaColorParam] object line = null,
			[LuaColorParam] object background = null)
		{
			luaPictureBox.DrawRectangle(x, y, width, height, _th.SafeParseColor(line), _th.SafeParseColor(background));
		}

		[LuaMethodExample(
			"LuaCanvas.DrawString( 16, 32, \"Some message\", 0x7F0000FF, 0x00007FFF, 8, \"Arial Narrow\", \"bold\", \"center\", \"middle\" );")]
		[LuaMethod(
			"DrawString",
			"Alias of DrawText()")]
		public void DrawString(
			int x,
			int y,
			string message,
			[LuaColorParam] object foreColor = null,
			[LuaColorParam] object backColor = null,
			int? fontSize = null,
			string fontFamily = null,
			string fontStyle = null,
			string horizontalAlign = null,
			string verticalAlign = null)
		{
			luaPictureBox.DrawText(x, y, message, _th.SafeParseColor(foreColor), _th.SafeParseColor(backColor), fontSize, fontFamily, fontStyle, horizontalAlign, verticalAlign);
		}

		[LuaMethodExample(
			"LuaCanvas.DrawText( 16, 32, \"Some message\", 0x7F0000FF, 0x00007FFF, 8, \"Arial Narrow\", \"bold\", \"center\", \"middle\" );")]
		[LuaMethod(
			"DrawText",
			"Draws the given message at the given x,y coordinates and the given color. The default color is white. A fontfamily can be specified and is monospace generic if none is specified (font family options are the same as the .NET FontFamily class). The fontsize default is 12. The default font style is regular. Font style options are regular, bold, italic, strikethrough, underline. Horizontal alignment options are left (default), center, or right. Vertical alignment options are bottom (default), middle, or top. Alignment options specify which ends of the text will be drawn at the x and y coordinates.")]
		public void DrawText(
			int x,
			int y,
			string message,
			[LuaColorParam] object foreColor = null,
			[LuaColorParam] object backColor = null,
			int? fontSize = null,
			string fontFamily = null,
			string fontStyle = null,
			string horizontalAlign = null,
			string verticalAlign = null)
		{
			luaPictureBox.DrawText(x, y, message, _th.SafeParseColor(foreColor), _th.SafeParseColor(backColor), fontSize, fontFamily, fontStyle, horizontalAlign, verticalAlign);
		}


		// It'd be great if these were simplified into 1 function, but I cannot figure out how to return a LuaTable from this class
		[LuaMethodExample(
			"local inLuaget = LuaCanvas.GetMouseX( );")]
		[LuaMethod(
			"GetMouseX",
			"Returns an integer representation of the mouse X coordinate relative to the canvas window.")]
		public int GetMouseX()
		{
			var position = luaPictureBox.GetMouse();
			return position.X;
		}

		[LuaMethodExample(
			"local inLuaget = LuaCanvas.GetMouseY( );")]
		[LuaMethod(
			"GetMouseY",
			"Returns an integer representation of the mouse Y coordinate relative to the canvas window.")]
		public int GetMouseY()
		{
			var position = luaPictureBox.GetMouse();
			return position.Y;
		}

		[LuaMethod("save_image_to_disk", "Saves everything that's been drawn to a .png file at the given path. Relative paths are relative to the path set for \"Screenshots\" for the current system.")]
		public void SaveImageToDisk(string path)
		{
			luaPictureBox.Image.Save(path.MakeAbsolute(_emuLib.PathEntries.ScreenshotAbsolutePathFor(_emuLib.GetSystemId())));
		}
	}
}
