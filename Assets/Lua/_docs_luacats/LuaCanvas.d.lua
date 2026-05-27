-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---Represents a canvas object, as returned by the `gui.createcanvas()` method.
---Note that the member functions do not have a `self` parameter, and so must be called with the regular invocation syntax, not the OOP syntax.
---@class LuaCanvas
local LuaCanvas = {}

---Clears the canvas
---
---Example:
---
---	LuaCanvas.Clear( 0x000000FF );
---@param color color
function LuaCanvas.Clear(color) end

---clears the image cache that is built up by using gui.drawImage, also releases the file handle for cached images
---
---Example:
---
---	LuaCanvas.ClearImageCache( );
function LuaCanvas.ClearImageCache() end

---draws a Arc shape at the given coordinates and the given width and height
---
---Example:
---
---	LuaCanvas.DrawArc( 16, 32, 77, 99, 180, 90, 0x007F00FF );
---@param x integer
---@param y integer
---@param width integer
---@param height integer
---@param startAngle integer
---@param sweepAngle integer
---@param line? color
function LuaCanvas.DrawArc(x, y, width, height, startAngle, sweepAngle, line) end

---Draws an axis of the specified size at the coordinate pair.)
---
---Example:
---
---	LuaCanvas.DrawAxis( 16, 32, int size, 0xFFFFFFFF );
---@param x integer
---@param y integer
---@param size integer
---@param color? color
function LuaCanvas.DrawAxis(x, y, size, color) end

---Draws a Bezier curve using the table of coordinates provided in the given color
---
---Example:
---
---	LuaCanvas.DrawBezier( { { 5, 10 }, { 10, 10 }, { 10, 20 }, { 5, 20 } }, 0x000000FF );
---@param points drawingpoint[]
---@param color color
function LuaCanvas.DrawBezier(points, color) end

---Draws a rectangle on screen from x1/y1 to x2/y2. Same as drawRectangle except it receives two points intead of a point and width/height
---
---Example:
---
---	LuaCanvas.DrawBox( 16, 32, 162, 322, 0x007F00FF, 0x7F7F7FFF );
---@param x integer
---@param y integer
---@param x2 integer
---@param y2 integer
---@param line? color
---@param background? color
function LuaCanvas.DrawBox(x, y, x2, y2, line, background) end

---Draws an ellipse at the given coordinates and the given width and height. Line is the color of the ellipse. Background is the optional fill color
---
---Example:
---
---	LuaCanvas.DrawEllipse( 16, 32, 77, 99, 0x007F00FF, 0x7F7F7FFF );
---@param x integer
---@param y integer
---@param width integer
---@param height integer
---@param line? color
---@param background? color
function LuaCanvas.DrawEllipse(x, y, width, height, line, background) end

---Draws the image in the given .ico file to the referenced canvas. The image will be positioned such that its top-left corner will be at (x, y) on the canvas. If width and height are both nil/unset, the image will be drawn at full size (100%). If both are specified, the image will be stretched to that size.
---
---Example:
---
---		canvas.DrawIcon("C:\\icon.ico", 16, 32, 18, 24);
---@param path string
---@param x integer
---@param y integer
---@param width? integer
---@param height? integer
function LuaCanvas.DrawIcon(path, x, y, width, height) end

---Draws the image in the given file (.bmp, .gif, .jpg, .png, or .tif) to the referenced canvas. The image will be positioned such that its top-left corner will be at (x, y) on the canvas. If width and height are both nil/unset, the image will be drawn at full size (100%). If both are specified, the image will be stretched to that size. If true is passed for the cache parameter, or if it's omitted, the file contents will be cached and re-used next time this function is called with the same path on this canvas. The canvas' cache can be cleared with ClearImageCache.
---
---Example:
---
---		canvas.DrawImage("C:\\image.png", 16, 32, 18, 24, false);
---@param path string
---@param x integer
---@param y integer
---@param width? integer
---@param height? integer
---@param cache? boolean Defaults to `true`
function LuaCanvas.DrawImage(path, x, y, width, height, cache) end

---Draws part of the image in the given file (.bmp, .gif, .jpg, .png, or .tif) to the referenced canvas. Consult this diagram to see its usage (renders embedded on the TASVideos Wiki): ![Diagram showing how to use forms.drawImageRegion](https://user-images.githubusercontent.com/13409956/198868522-55dc1e5f-ae67-4ebb-a75f-558656cb4468.png) The file contents will be cached and re-used next time this function is called with the same path on this canvas. The canvas' cache can be cleared with ClearImageCache.
---
---Example:
---
---		canvas.DrawImageRegion("C:\\image.png", 11, 22, 33, 44, 21, 43, 34, 45);
---@param path string
---@param sourceX integer
---@param sourceY integer
---@param sourceWidth integer
---@param sourceHeight integer
---@param destX integer
---@param destY integer
---@param destWidth? integer
---@param destHeight? integer
function LuaCanvas.DrawImageRegion(path, sourceX, sourceY, sourceWidth, sourceHeight, destX, destY, destWidth, destHeight) end

---Draws a line from the first coordinate pair to the 2nd. Color is optional (if not specified it will be drawn black)
---
---Example:
---
---	LuaCanvas.DrawLine( 161, 321, 162, 322, 0xFFFFFFFF );
---@param x1 integer
---@param y1 integer
---@param x2 integer
---@param y2 integer
---@param color? color
function LuaCanvas.DrawLine(x1, y1, x2, y2, color) end

---draws a Pie shape at the given coordinates and the given width and height
---
---Example:
---
---	LuaCanvas.DrawPie( 16, 32, 77, 99, 180, 90, 0x007F00FF, 0x7F7F7FFF );
---@param x integer
---@param y integer
---@param width integer
---@param height integer
---@param startAngle integer
---@param sweepAngle integer
---@param line? color
---@param background? color
function LuaCanvas.DrawPie(x, y, width, height, startAngle, sweepAngle, line, background) end

---Draws a single pixel at the given coordinates in the given color. Color is optional (if not specified it will be drawn black)
---
---Example:
---
---	LuaCanvas.DrawPixel( 16, 32, 0xFFFFFFFF );
---@param x integer
---@param y integer
---@param color? color
function LuaCanvas.DrawPixel(x, y, color) end

---Draws a polygon (cyclic polyline) to the referenced canvas. The polygon must be given as a list of length-2 lists (co-ordinate pairs). Each pair is interpreted as the absolute co-ordinates of one of the vertices, and these are joined together in sequence to form a polygon. The last is connected to the first; you DON'T need to end with a copy of the first to close the cycle. If the x and y parameters are both specified, the whole polygon will be offset by that amount. If a value is passed for the line parameter, the polygon's edges are drawn in that color (i.e. the stroke color). If a value is passed for the background parameter, the polygon's face is filled in that color.
---
---Example:
---
---		canvas.DrawPolygon({ { 5, 10 }, { 10, 10 }, { 10, 20 }, { 5, 20 } }, 10, 30, 0x007F00FF, 0x7F7F7FFF);
---@param points drawingpoint[]
---@param x? integer
---@param y? integer
---@param line? color
---@param background? color
function LuaCanvas.DrawPolygon(points, x, y, line, background) end

---Draws a rectangle at the given coordinate and the given width and height. Line is the color of the box. Background is the optional fill color
---
---Example:
---
---	LuaCanvas.DrawRectangle( 16, 32, 77, 99, 0x007F00FF, 0x7F7F7FFF );
---@param x integer
---@param y integer
---@param width integer
---@param height integer
---@param line? color
---@param background? color
function LuaCanvas.DrawRectangle(x, y, width, height, line, background) end

---Alias of DrawText()
---
---Example:
---
---	LuaCanvas.DrawString( 16, 32, "Some message", 0x7F0000FF, 0x00007FFF, 8, "Arial Narrow", "bold", "center", "middle" );
---@param x integer
---@param y integer
---@param message string|number
---@param foreColor? color
---@param backColor? color
---@param fontSize? integer
---@param fontFamily? string
---@param fontStyle? string
---@param horizontalAlign? string
---@param verticalAlign? string
function LuaCanvas.DrawString(x, y, message, foreColor, backColor, fontSize, fontFamily, fontStyle, horizontalAlign, verticalAlign) end

---Draws the given message at the given x,y coordinates and the given color. The default color is white. A fontfamily can be specified and is monospace generic if none is specified (font family options are the same as the .NET FontFamily class). The fontsize default is 12. The default font style is regular. Font style options are regular, bold, italic, strikethrough, underline. Horizontal alignment options are left (default), center, or right. Vertical alignment options are bottom (default), middle, or top. Alignment options specify which ends of the text will be drawn at the x and y coordinates.
---
---Example:
---
---	LuaCanvas.DrawText( 16, 32, "Some message", 0x7F0000FF, 0x00007FFF, 8, "Arial Narrow", "bold", "center", "middle" );
---@param x integer
---@param y integer
---@param message string|number
---@param foreColor? color
---@param backColor? color
---@param fontSize? integer
---@param fontFamily? string
---@param fontStyle? string
---@param horizontalAlign? string
---@param verticalAlign? string
function LuaCanvas.DrawText(x, y, message, foreColor, backColor, fontSize, fontFamily, fontStyle, horizontalAlign, verticalAlign) end

---Returns an integer representation of the mouse X coordinate relative to the canvas window.
---
---Example:
---
---	local inLuaget = LuaCanvas.GetMouseX( );
---@return integer
function LuaCanvas.GetMouseX() end

---Returns an integer representation of the mouse Y coordinate relative to the canvas window.
---
---Example:
---
---	local inLuaget = LuaCanvas.GetMouseY( );
---@return integer
function LuaCanvas.GetMouseY() end

---Redraws the canvas
---
---Example:
---
---	LuaCanvas.Refresh( );
function LuaCanvas.Refresh() end

---Saves everything that's been drawn to a .png file at the given path. Relative paths are relative to the path set for "Screenshots" for the current system.
---@param path string
function LuaCanvas.save_image_to_disk(path) end

---Sets the default background color to use in drawing methods, transparent by default
---
---Example:
---
---	LuaCanvas.SetDefaultBackgroundColor( 0x000000FF );
---@param color color
function LuaCanvas.SetDefaultBackgroundColor(color) end

---Sets the default foreground color to use in drawing methods, white by default
---
---Example:
---
---	LuaCanvas.SetDefaultForegroundColor( 0x000000FF );
---@param color color
function LuaCanvas.SetDefaultForegroundColor(color) end

---Sets the default background color to use in text drawing methods, half-transparent black by default
---
---Example:
---
---	LuaCanvas.SetDefaultTextBackground( 0x000000FF );
---@param color color
function LuaCanvas.SetDefaultTextBackground(color) end

---Sets the location of the canvas window
---
---Example:
---
---	LuaCanvas.SetLocation( 16, 32 );
---@param x integer
---@param y integer
function LuaCanvas.SetLocation(x, y) end

---Sets the canvas window title
---
---Example:
---
---	LuaCanvas.SetTitle( "Title" );
---@param title string
function LuaCanvas.SetTitle(title) end

