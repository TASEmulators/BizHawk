-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---@class gui
gui = {}

---Adds a message to the OSD's message area
---
---Example:
---
---	gui.addmessage( "Some message" );
---@param message string | number
function gui.addmessage(message) end

---clears all lua drawn graphics from the screen
---
---Example:
---
---	gui.clearGraphics( );
---@param surfaceName? surface
function gui.clearGraphics(surfaceName) end

---clears the image cache that is built up by using gui.drawImage, also releases the file handle for cached images
---
---Example:
---
---	gui.clearImageCache( );
function gui.clearImageCache() end

---clears all text created by gui.text()
---
---Example:
---
---	gui.cleartext( );
function gui.cleartext() end

---Creates a dedicated canvas window, returning a table containing some callbacks for drawing. These are the LuaCanvas functions in the API reference. The width and height parameters determine the size of the canvas. If the x and y parameters are both nil/unset, the form (window) will appear at the default position. If both are specified, the form will be positioned at (x, y) on the screen.
---
---Example:
---
---		local canvas = gui.createcanvas(77, 99, 2, 48);
---@param width integer
---@param height integer
---@param x? integer
---@param y? integer
---@return table
function gui.createcanvas(width, height, x, y) end

---Sets the default background color to use in drawing methods, transparent by default
---
---Example:
---
---	gui.defaultBackground( 0xFFFFFFFF );
---@param color color
function gui.defaultBackground(color) end

---Sets the default foreground color to use in drawing methods, white by default
---
---Example:
---
---	gui.defaultForeground( 0x000000FF );
---@param color color
function gui.defaultForeground(color) end

---Sets the default font to use in gui.pixelText(). Two font families are available, "fceux" and "gens" (or  "0" and "1" respectively), "gens" is used by default
---
---Example:
---
---	gui.defaultPixelFont( "Arial Narrow");
---@param fontfamily string
function gui.defaultPixelFont(fontfamily) end

---Sets the default background color to use in text drawing methods, half-transparent black by default
---
---Example:
---
---	gui.defaultTextBackground( 0x000000FF );
---@param color color
function gui.defaultTextBackground(color) end

---Draws an axis of the specified size at the coordinate pair.)
---
---Example:
---
---	gui.drawAxis( 16, 32, 15, 0xFFFFFFFF );
---@param x integer
---@param y integer
---@param size integer
---@param color? color
---@param surfaceName? surface
function gui.drawAxis(x, y, size, color, surfaceName) end

---Draws a Bezier curve using the table of coordinates provided in the given color
---
---Example:
---
---	gui.drawBezier( { { 5, 10 }, { 10, 10 }, { 10, 20 }, { 5, 20 } }, 0x000000FF );
---@param points table
---@param color color
---@param surfaceName? surface
function gui.drawBezier(points, color, surfaceName) end

---Draws a rectangle on screen from x1/y1 to x2/y2. Same as drawRectangle except it receives two points intead of a point and width/height
---
---Example:
---
---	gui.drawBox( 16, 32, 162, 322, 0x007F00FF, 0x7F7F7FFF );
---@param x integer
---@param y integer
---@param x2 integer
---@param y2 integer
---@param line? color
---@param background? color
---@param surfaceName? surface
function gui.drawBox(x, y, x2, y2, line, background, surfaceName) end

---Draws an ellipse at the given coordinates and the given width and height. Line is the color of the ellipse. Background is the optional fill color
---
---Example:
---
---	gui.drawEllipse( 16, 32, 77, 99, 0x007F00FF, 0x7F7F7FFF );
---@param x integer
---@param y integer
---@param width integer
---@param height integer
---@param line? color
---@param background? color
---@param surfaceName? surface
function gui.drawEllipse(x, y, width, height, line, background, surfaceName) end

---Finishes drawing to the current lua surface and causes it to get displayed.
---@deprecated
function gui.DrawFinish() end

---Draws the image in the given .ico file to the surface specified by the surfaceName parameter, or the current surface if nil/unset. The image will be positioned such that its top-left corner will be at (x, y) on the surface. If width and height are both nil/unset, the image will be drawn at full size (100%). If both are specified, the image will be stretched to that size.
---
---Example:
---
---		gui.drawIcon("C:\\sample.ico", 16, 32, 18, 24);
---@param path string
---@param x integer
---@param y integer
---@param width? integer
---@param height? integer
---@param surfaceName? surface
function gui.drawIcon(path, x, y, width, height, surfaceName) end

---Draws the image in the given file (.bmp, .gif, .jpg, .png, or .tif) to the surface specified by the surfaceName parameter, or the current surface if nil/unset. The image will be positioned such that its top-left corner will be at (x, y) on the surface. If width and height are both nil/unset, the image will be drawn at full size (100%). If both are specified, the image will be stretched to that size. If true is passed for the cache parameter, or if it's omitted, the file contents will be cached and re-used next time this function is called with the same path. The cache can be cleared with gui.clearImageCache.
---
---Example:
---
---		gui.drawImage("C:\\sample.bmp", 16, 32, 18, 24, false);
---@param path string
---@param x integer
---@param y integer
---@param width? integer
---@param height? integer
---@param cache? boolean Defaults to `true`
---@param surfaceName? surface
function gui.drawImage(path, x, y, width, height, cache, surfaceName) end

---Draws part of the image in the given file (.bmp, .gif, .jpg, .png, or .tif) to the surface specified by the surfaceName parameter, or the current surface if nil/unset. Consult this diagram to see its usage (renders embedded on the TASVideos Wiki): ![Diagram showing how to use forms.drawImageRegion](https://user-images.githubusercontent.com/13409956/198868522-55dc1e5f-ae67-4ebb-a75f-558656cb4468.png) The file contents will be cached and re-used next time this function is called with the same path. The cache can be cleared with gui.clearImageCache.
---
---Example:
---
---		gui.drawImageRegion("C:\\sample.png", 11, 22, 33, 44, 21, 43, 34, 45);
---@param path string
---@param source_x integer
---@param source_y integer
---@param source_width integer
---@param source_height integer
---@param dest_x integer
---@param dest_y integer
---@param dest_width? integer
---@param dest_height? integer
---@param surfaceName? surface
function gui.drawImageRegion(path, source_x, source_y, source_width, source_height, dest_x, dest_y, dest_width, dest_height, surfaceName) end

---Draws a line from the first coordinate pair to the 2nd. Color is optional (if not specified it will be drawn black)
---
---Example:
---
---	gui.drawLine( 161, 321, 162, 322, 0xFFFFFFFF );
---@param x1 integer
---@param y1 integer
---@param x2 integer
---@param y2 integer
---@param color? color
---@param surfaceName? surface
function gui.drawLine(x1, y1, x2, y2, color, surfaceName) end

---Changes drawing target to the specified lua surface name. This may clobber any previous drawing to this surface (pass false if you don't want it to)
---@deprecated
---@param name string
---@param clear? boolean Defaults to `true`
function gui.DrawNew(name, clear) end

---draws a Pie shape at the given coordinates and the given width and height
---
---Example:
---
---	gui.drawPie( 16, 32, 77, 99, 180, 90, 0x007F00FF, 0x7F7F7FFF );
---@param x integer
---@param y integer
---@param width integer
---@param height integer
---@param startangle integer
---@param sweepangle integer
---@param line? color
---@param background? color
---@param surfaceName? surface
function gui.drawPie(x, y, width, height, startangle, sweepangle, line, background, surfaceName) end

---Draws a single pixel at the given coordinates in the given color. Color is optional (if not specified it will be drawn black)
---
---Example:
---
---	gui.drawPixel( 16, 32, 0xFFFFFFFF );
---@param x integer
---@param y integer
---@param color? color
---@param surfaceName? surface
function gui.drawPixel(x, y, color, surfaceName) end

---Draws a polygon (cyclic polyline) to the surface specified by the surfaceName parameter, or the current surface if nil/unset. The polygon must be given as a list of length-2 lists (co-ordinate pairs). Each pair is interpreted as the absolute co-ordinates of one of the vertices, and these are joined together in sequence to form a polygon. The last is connected to the first; you DON'T need to end with a copy of the first to close the cycle. If the offsetX and offsetY parameters are both specified, the whole polygon will be offset by that amount. If a value is passed for the line parameter, the polygon's edges are drawn in that color (i.e. the stroke color). If a value is passed for the background parameter, the polygon's face is filled in that color.
---
---Example:
---
---		gui.drawPolygon({ { 5, 10 }, { 10, 10 }, { 10, 20 }, { 5, 20 } }, 10, 30, 0x007F00FF, 0x7F7F7FFF);
---@param points table
---@param offsetX? integer
---@param offsetY? integer
---@param line? color
---@param background? color
---@param surfaceName? surface
function gui.drawPolygon(points, offsetX, offsetY, line, background, surfaceName) end

---Draws a rectangle at the given coordinate and the given width and height. Line is the color of the box. Background is the optional fill color
---
---Example:
---
---	gui.drawRectangle( 16, 32, 77, 99, 0x007F00FF, 0x7F7F7FFF );
---@param x integer
---@param y integer
---@param width integer
---@param height integer
---@param line? color
---@param background? color
---@param surfaceName? surface
function gui.drawRectangle(x, y, width, height, line, background, surfaceName) end

---Draws the given message in the emulator screen space (like all draw functions) at the given x,y coordinates and the given color. The default color is white. A fontfamily can be specified and is monospace generic if none is specified (font family options are the same as the .NET FontFamily class). The fontsize default is 12. The default font style is regular. Font style options are regular, bold, italic, strikethrough, underline. Horizontal alignment options are left (default), center, or right. Vertical alignment options are bottom (default), middle, or top. Alignment options specify which ends of the text will be drawn at the x and y coordinates. For pixel-perfect font look, make sure to disable aspect ratio correction.
---
---Example:
---
---	gui.drawString( 16, 32, "Some message", 0x7F0000FF, 0x00007FFF, 8, "Arial Narrow", "bold", "center", "middle" );
---@param x integer
---@param y integer
---@param message string | number
---@param forecolor? color
---@param backcolor? color
---@param fontsize? integer
---@param fontfamily? string
---@param fontstyle? string
---@param horizalign? string
---@param vertalign? string
---@param surfaceName? surface
function gui.drawString(x, y, message, forecolor, backcolor, fontsize, fontfamily, fontstyle, horizalign, vertalign, surfaceName) end

---alias for gui.drawString
---
---Example:
---
---	gui.drawText( 16, 32, "Some message", 0x7F0000FF, 0x00007FFF, 8, "Arial Narrow", "bold", "center", "middle" );
---@param x integer
---@param y integer
---@param message string | number
---@param forecolor? color
---@param backcolor? color
---@param fontsize? integer
---@param fontfamily? string
---@param fontstyle? string
---@param horizalign? string
---@param vertalign? string
---@param surfaceName? surface
function gui.drawText(x, y, message, forecolor, backcolor, fontsize, fontfamily, fontstyle, horizalign, vertalign, surfaceName) end

---Draws the given message in the emulator screen space (like all draw functions) at the given x,y coordinates and the given color. The default color is white. Two font families are available, "fceux" and "gens" (or  "0" and "1" respectively), both are monospace and have the same size as in the emulators they've been taken from. If no font family is specified, it uses "gens" font, unless that's overridden via gui.defaultPixelFont().
---
---Example:
---
---	gui.pixelText( 16, 32, "Some message", 0x7F0000FF, 0x00007FFF, "Arial Narrow" );
---@param x integer
---@param y integer
---@param message string | number
---@param forecolor? color
---@param backcolor? color
---@param fontfamily? string
---@param surfaceName? surface
function gui.pixelText(x, y, message, forecolor, backcolor, fontfamily, surfaceName) end

---Displays the given text on the screen at the given coordinates. Optional Foreground color. The optional anchor flag anchors the text to one of the four corners. Anchor flag parameters: topleft, topright, bottomleft, bottomright. This function is generally much faster than other text drawing functions, at the cost of customization.
---
---Example:
---
---	gui.text( 16, 32, "Some message", 0x7F0000FF, "bottomleft" );
---@param x integer
---@param y integer
---@param message string | number
---@param forecolor? color
---@param anchor? string
function gui.text(x, y, message, forecolor, anchor) end

---Stores the name of a surface to draw on, so you don't need to pass it to every draw function. The default is "emucore", and the other valid value is "client".
---
---Example:
---
---	gui.use_surface( "client" );
---@param surfaceName surface
function gui.use_surface(surfaceName) end

