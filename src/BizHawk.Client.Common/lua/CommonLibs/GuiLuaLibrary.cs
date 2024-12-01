using System.Drawing;
using System.Linq;

using NLua;

namespace BizHawk.Client.Common
{
	public sealed class GuiLuaLibrary : LuaLibraryBase, IDisposable
	{
		private DisplaySurfaceID _rememberedSurfaceID = DisplaySurfaceID.EmuCore;

		public Func<int, int, int?, int?, LuaTable> CreateLuaCanvasCallback { get; set; }

		public GuiLuaLibrary(ILuaLibraries luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "gui";

		private DisplaySurfaceID UseOrFallback(string surfaceName)
			=> DisplaySurfaceIDParser.Parse(surfaceName) ?? _rememberedSurfaceID;

#pragma warning disable CS0612
#pragma warning disable CS0618
		[LuaDeprecatedMethod]
		[LuaMethod("DrawNew", "Changes drawing target to the specified lua surface name. This may clobber any previous drawing to this surface (pass false if you don't want it to)")]
		public void DrawNew(string name, bool? clear = true)
			=> APIs.Gui.DrawNew(name, clear ?? true);

		[LuaDeprecatedMethod]
		[LuaMethod("DrawFinish", "Finishes drawing to the current lua surface and causes it to get displayed.")]
		public void DrawFinish()
			=> APIs.Gui.DrawFinish();
#pragma warning restore CS0612
#pragma warning restore CS0618

		[LuaMethodExample("gui.addmessage( \"Some message\" );")]
		[LuaMethod("addmessage", "Adds a message to the OSD's message area")]
		public void AddMessage(string message)
			=> APIs.Gui.AddMessage(message);

		[LuaMethodExample("gui.clearGraphics( );")]
		[LuaMethod("clearGraphics", "clears all lua drawn graphics from the screen")]
		public void ClearGraphics(string surfaceName = null)
			=> APIs.Gui.ClearGraphics(surfaceID: UseOrFallback(surfaceName));

		[LuaMethodExample("gui.cleartext( );")]
		[LuaMethod("cleartext", "clears all text created by gui.text()")]
		public void ClearText()
			=> APIs.Gui.ClearText();

		[LuaMethodExample("gui.defaultForeground( 0x000000FF );")]
		[LuaMethod("defaultForeground", "Sets the default foreground color to use in drawing methods, white by default")]
		public void SetDefaultForegroundColor([LuaColorParam] object color)
			=> APIs.Gui.SetDefaultForegroundColor(_th.ParseColor(color));

		[LuaMethodExample("gui.defaultBackground( 0xFFFFFFFF );")]
		[LuaMethod("defaultBackground", "Sets the default background color to use in drawing methods, transparent by default")]
		public void SetDefaultBackgroundColor([LuaColorParam] object color)
			=> APIs.Gui.SetDefaultBackgroundColor(_th.ParseColor(color));

		[LuaMethodExample("gui.defaultTextBackground( 0x000000FF );")]
		[LuaMethod("defaultTextBackground", "Sets the default background color to use in text drawing methods, half-transparent black by default")]
		public void SetDefaultTextBackground([LuaColorParam] object color)
			=> APIs.Gui.SetDefaultTextBackground(_th.ParseColor(color));

		[LuaMethodExample("gui.defaultPixelFont( \"Arial Narrow\");")]
		[LuaMethod("defaultPixelFont", "Sets the default font to use in gui.pixelText(). Two font families are available, \"fceux\" and \"gens\" (or  \"0\" and \"1\" respectively), \"gens\" is used by default")]
		public void SetDefaultPixelFont(string fontfamily)
			=> APIs.Gui.SetDefaultPixelFont(fontfamily);

		[LuaMethodExample("gui.drawBezier( { { 5, 10 }, { 10, 10 }, { 10, 20 }, { 5, 20 } }, 0x000000FF );")]
		[LuaMethod("drawBezier", "Draws a Bezier curve using the table of coordinates provided in the given color")]
		public void DrawBezier(
			LuaTable points,
			[LuaColorParam] object color,
			string surfaceName = null)
		{
			try
			{
				var pointsArr = new Point[4];
				var i = 0;
				foreach (var point in _th.EnumerateValues<LuaTable>(points)
					.Select(table => _th.EnumerateValues<long>(table).ToList()))
				{
					pointsArr[i] = new Point((int) point[0], (int) point[1]);
					i++;
					if (i >= 4)
					{
						break;
					}
				}
				APIs.Gui.DrawBezier(pointsArr[0], pointsArr[1], pointsArr[2], pointsArr[3], _th.ParseColor(color), surfaceID: UseOrFallback(surfaceName));
			}
			catch (Exception)
			{
				// ignored
			}
		}

		[LuaMethodExample("gui.drawBox( 16, 32, 162, 322, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod("drawBox", "Draws a rectangle on screen from x1/y1 to x2/y2. Same as drawRectangle except it receives two points intead of a point and width/height")]
		public void DrawBox(
			int x,
			int y,
			int x2,
			int y2,
			[LuaColorParam] object line = null,
			[LuaColorParam] object background = null,
			string surfaceName = null)
				=> APIs.Gui.DrawBox(x, y, x2, y2, _th.SafeParseColor(line), _th.SafeParseColor(background), surfaceID: UseOrFallback(surfaceName));

		[LuaMethodExample("gui.drawEllipse( 16, 32, 77, 99, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod("drawEllipse", "Draws an ellipse at the given coordinates and the given width and height. Line is the color of the ellipse. Background is the optional fill color")]
		public void DrawEllipse(
			int x,
			int y,
			int width,
			int height,
			[LuaColorParam] object line = null,
			[LuaColorParam] object background = null,
			string surfaceName = null)
				=> APIs.Gui.DrawEllipse(x, y, width, height, _th.SafeParseColor(line), _th.SafeParseColor(background), surfaceID: UseOrFallback(surfaceName));

		[LuaMethodExample("gui.drawIcon( \"C:\\sample.ico\", 16, 32, 18, 24 );")]
		[LuaMethod("drawIcon", "draws an Icon (.ico) file from the given path at the given coordinate. width and height are optional. If specified, it will resize the image accordingly")]
		public void DrawIcon(
			string path,
			int x,
			int y,
			int? width = null,
			int? height = null,
			string surfaceName = null)
				=> APIs.Gui.DrawIcon(path, x, y, width, height, surfaceID: UseOrFallback(surfaceName));

		[LuaMethodExample("gui.drawImage( \"C:\\sample.bmp\", 16, 32, 18, 24, false );")]
		[LuaMethod("drawImage", "draws an image file from the given path at the given coordinate. width and height are optional. If specified, it will resize the image accordingly")]
		public void DrawImage(
			string path,
			int x,
			int y,
			int? width = null,
			int? height = null,
			bool cache = true,
			string surfaceName = null)
				=> APIs.Gui.DrawImage(path, x, y, width, height, cache, surfaceID: UseOrFallback(surfaceName));

		[LuaMethodExample("gui.clearImageCache( );")]
		[LuaMethod("clearImageCache", "clears the image cache that is built up by using gui.drawImage, also releases the file handle for cached images")]
		public void ClearImageCache()
			=> APIs.Gui.ClearImageCache();

		[LuaMethodExample("gui.drawImageRegion( \"C:\\sample.png\", 11, 22, 33, 44, 21, 43, 34, 45 );")]
		[LuaMethod("drawImageRegion", "draws a given region of an image file from the given path at the given coordinate, and optionally with the given size")]
		public void DrawImageRegion(
			string path,
			int source_x,
			int source_y,
			int source_width,
			int source_height,
			int dest_x,
			int dest_y,
			int? dest_width = null,
			int? dest_height = null,
			string surfaceName = null)
				=> APIs.Gui.DrawImageRegion(path, source_x, source_y, source_width, source_height, dest_x, dest_y, dest_width, dest_height, surfaceID: UseOrFallback(surfaceName));

		[LuaMethodExample("gui.drawLine( 161, 321, 162, 322, 0xFFFFFFFF );")]
		[LuaMethod("drawLine", "Draws a line from the first coordinate pair to the 2nd. Color is optional (if not specified it will be drawn black)")]
		public void DrawLine(
			int x1,
			int y1,
			int x2,
			int y2,
			[LuaColorParam] object color = null,
			string surfaceName = null)
				=> APIs.Gui.DrawLine(x1, y1, x2, y2, _th.SafeParseColor(color), surfaceID: UseOrFallback(surfaceName));

		[LuaMethodExample("gui.drawAxis( 16, 32, 15, 0xFFFFFFFF );")]
		[LuaMethod("drawAxis", "Draws an axis of the specified size at the coordinate pair.)")]
		public void DrawAxis(
			int x,
			int y,
			int size,
			[LuaColorParam] object color = null,
			string surfaceName = null)
				=> APIs.Gui.DrawAxis(x, y, size, _th.SafeParseColor(color), surfaceID: UseOrFallback(surfaceName));

		[LuaMethodExample("gui.drawPie( 16, 32, 77, 99, 180, 90, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod("drawPie", "draws a Pie shape at the given coordinates and the given width and height")]
		public void DrawPie(
			int x,
			int y,
			int width,
			int height,
			int startangle,
			int sweepangle,
			[LuaColorParam] object line = null,
			[LuaColorParam] object background = null,
			string surfaceName = null)
				=> APIs.Gui.DrawPie(x, y, width, height, startangle, sweepangle, _th.SafeParseColor(line), _th.SafeParseColor(background), surfaceID: UseOrFallback(surfaceName));

		[LuaMethodExample("gui.drawPixel( 16, 32, 0xFFFFFFFF );")]
		[LuaMethod("drawPixel", "Draws a single pixel at the given coordinates in the given color. Color is optional (if not specified it will be drawn black)")]
		public void DrawPixel(
			int x,
			int y,
			[LuaColorParam] object color = null,
			string surfaceName = null)
				=> APIs.Gui.DrawPixel(x, y, _th.SafeParseColor(color), surfaceID: UseOrFallback(surfaceName));

		[LuaMethodExample("gui.drawPolygon( { { 5, 10 }, { 10, 10 }, { 10, 20 }, { 5, 20 } }, 10, 30, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod("drawPolygon", "Draws a polygon using the table of coordinates specified in points. This should be a table of tables(each of size 2). If x or y is passed, the polygon will be translated by the passed coordinate pair. Line is the color of the polygon. Background is the optional fill color")]
		public void DrawPolygon(
			LuaTable points,
			int? offsetX = null,
			int? offsetY = null,
			[LuaColorParam] object line = null,
			[LuaColorParam] object background = null,
			string surfaceName = null)
		{
			var pointsList = _th.EnumerateValues<LuaTable>(points)
				.Select(table => _th.EnumerateValues<long>(table).ToList()).ToList();
			try
			{
				var pointsArr = new Point[pointsList.Count];
				var i = 0;
				foreach (var point in pointsList)
				{
					pointsArr[i] = new Point((int) point[0] + (offsetX ?? 0), (int) point[1] + (offsetY ?? 0));
					i++;
				}
				APIs.Gui.DrawPolygon(pointsArr, _th.SafeParseColor(line), _th.SafeParseColor(background), surfaceID: UseOrFallback(surfaceName));
			}
			catch (Exception)
			{
				// ignored
			}
		}

		[LuaMethodExample("gui.drawRectangle( 16, 32, 77, 99, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod("drawRectangle", "Draws a rectangle at the given coordinate and the given width and height. Line is the color of the box. Background is the optional fill color")]
		public void DrawRectangle(
			int x,
			int y,
			int width,
			int height,
			[LuaColorParam] object line = null,
			[LuaColorParam] object background = null,
			string surfaceName = null)
				=> APIs.Gui.DrawRectangle(x, y, width, height, _th.SafeParseColor(line), _th.SafeParseColor(background), surfaceID: UseOrFallback(surfaceName));

		[LuaMethodExample("gui.drawString( 16, 32, \"Some message\", 0x7F0000FF, 0x00007FFF, 8, \"Arial Narrow\", \"bold\", \"center\", \"middle\" );")]
		[LuaMethod("drawString", "Draws the given message in the emulator screen space (like all draw functions) at the given x,y coordinates and the given color. The default color is white. A fontfamily can be specified and is monospace generic if none is specified (font family options are the same as the .NET FontFamily class). The fontsize default is 12. The default font style is regular. Font style options are regular, bold, italic, strikethrough, underline. Horizontal alignment options are left (default), center, or right. Vertical alignment options are bottom (default), middle, or top. Alignment options specify which ends of the text will be drawn at the x and y coordinates. For pixel-perfect font look, make sure to disable aspect ratio correction.")]
		public void DrawString(
			int x,
			int y,
			string message,
			[LuaColorParam] object forecolor = null,
			[LuaColorParam] object backcolor = null,
			int? fontsize = null,
			string fontfamily = null,
			string fontstyle = null,
			string horizalign = null,
			string vertalign = null,
			string surfaceName = null)
				=> APIs.Gui.DrawString(
					x: x,
					y: y,
					message: message,
					forecolor: _th.SafeParseColor(forecolor),
					backcolor: _th.SafeParseColor(backcolor),
					fontsize: fontsize,
					fontfamily: fontfamily,
					fontstyle: fontstyle,
					horizalign: horizalign,
					vertalign: vertalign,
					surfaceID: UseOrFallback(surfaceName));

		/// <remarks>TODO do this in Lua binding code?</remarks>
		[LuaMethodExample("gui.drawText( 16, 32, \"Some message\", 0x7F0000FF, 0x00007FFF, 8, \"Arial Narrow\", \"bold\", \"center\", \"middle\" );")]
		[LuaMethod("drawText", "alias for gui.drawString")]
		public void DrawText(
			int x,
			int y,
			string message,
			[LuaColorParam] object forecolor = null,
			[LuaColorParam] object backcolor = null,
			int? fontsize = null,
			string fontfamily = null,
			string fontstyle = null,
			string horizalign = null,
			string vertalign = null,
			string surfaceName = null)
				=> DrawString(
					x: x,
					y: y,
					message: message,
					forecolor: forecolor,
					backcolor: backcolor,
					fontsize: fontsize,
					fontfamily: fontfamily,
					fontstyle: fontstyle,
					horizalign: horizalign,
					vertalign: vertalign,
					surfaceName: surfaceName);

		[LuaMethodExample("gui.pixelText( 16, 32, \"Some message\", 0x7F0000FF, 0x00007FFF, \"Arial Narrow\" );")]
		[LuaMethod("pixelText", "Draws the given message in the emulator screen space (like all draw functions) at the given x,y coordinates and the given color. The default color is white. Two font families are available, \"fceux\" and \"gens\" (or  \"0\" and \"1\" respectively), both are monospace and have the same size as in the emulators they've been taken from. If no font family is specified, it uses \"gens\" font, unless that's overridden via gui.defaultPixelFont().")]
		public void PixelText(
			int x,
			int y,
			string message,
			[LuaColorParam] object forecolor = null,
			[LuaColorParam] object backcolor = null,
			string fontfamily = null,
			string surfaceName = null)
				=> APIs.Gui.PixelText(x, y, message, _th.SafeParseColor(forecolor), _th.SafeParseColor(backcolor) ?? APIs.Gui.GetDefaultTextBackground(), fontfamily, surfaceID: UseOrFallback(surfaceName));

		[LuaMethodExample("gui.text( 16, 32, \"Some message\", 0x7F0000FF, \"bottomleft\" );")]
		[LuaMethod("text", "Displays the given text on the screen at the given coordinates. Optional Foreground color. The optional anchor flag anchors the text to one of the four corners. Anchor flag parameters: topleft, topright, bottomleft, bottomright. This function is generally much faster than other text drawing functions, at the cost of customization.")]
		public void Text(
			int x,
			int y,
			string message,
			[LuaColorParam] object forecolor = null,
			string anchor = null)
				=> APIs.Gui.Text(x, y, message, _th.SafeParseColor(forecolor), anchor);

		[LuaMethodExample("local nlguicre = gui.createcanvas( 77, 99, 2, 48 );")]
		[LuaMethod("createcanvas", "Creates a canvas of the given size and, if specified, the given coordinates.")]
		public LuaTable CreateCanvas(int width, int height, int? x = null, int? y = null)
			=> CreateLuaCanvasCallback(width, height, x, y);

		[LuaMethodExample("gui.use_surface( \"client\" );")]
		[LuaMethod("use_surface", "Stores the name of a surface to draw on, so you don't need to pass it to every draw function. The default is \"emucore\", and the other valid value is \"client\".")]
		public void UseSurface(string surfaceName)
		{
			if (surfaceName == null)
			{
				Log("Surface name cannot be nil. Pass \"emucore\" to `gui.use_surface` to restore the default.");
				return;
			}
			_rememberedSurfaceID = DisplaySurfaceIDParser.Parse(surfaceName).Value;
		}

		public void Dispose()
			=> APIs.Gui.Dispose();
	}
}
