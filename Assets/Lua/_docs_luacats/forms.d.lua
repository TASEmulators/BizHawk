-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---A library for creating and managing custom dialogs
---@class forms
forms = {}

---adds the given lua function as a click event to the given control
---
---Example:
---
---	forms.addclick( 332, function()
---		console.log( "adds the given lua function as a click event to the given control" );
---	end );
---@param handle integer
---@param clickEvent function
function forms.addclick(handle, clickEvent) end

---Creates a button control on the form at formHandle, returning an opaque handle to the new control. The button's label will be set to the value passed for the caption parameter. The callback passed for the clickEvent parameter will be invoked whenever the button is clicked. If the x and y parameters are both nil/unset, the control's Location property won't be set. If both are specified, the control will be positioned at (x, y) within the given form. If the width and height parameters are both nil/unset, the control will be the default size. If both are specified, the control will be that size.
---
---Example:
---
---		local button_handle = forms.button(form_handle, "Click me", function() console.writeline("boop"); end, 2, 48, 18, 24);
---@param formHandle integer
---@param caption string | number
---@param clickEvent function
---@param x? integer
---@param y? integer
---@param width? integer
---@param height? integer
---@return integer
function forms.button(formHandle, caption, clickEvent, x, y, width, height) end

---Creates a checkbox control on the form at formHandle, returning an opaque handle to the new control. The checkbox' label will be set to the value passed for the caption parameter. If the x and y parameters are both nil/unset, the control's Location property won't be set. If both are specified, the control will be positioned at (x, y) within the given form.
---
---Example:
---
---		local checkbox_handle = forms.checkbox(form_handle, "Enable", 2, 48);
---@param formHandle integer
---@param caption string | number
---@param x? integer
---@param y? integer
---@return integer
function forms.checkbox(formHandle, caption, x, y) end

---Clears the canvas
---
---Example:
---
---	forms.clear( 334, 0x000000FF );
---@param componentHandle integer
---@param color color
function forms.clear(componentHandle, color) end

---Removes all click events from the given widget at the specified handle
---
---Example:
---
---	forms.clearclicks( 332 );
---@param handle integer
function forms.clearclicks(handle) end

---clears the image cache that is built up by using gui.drawImage, also releases the file handle for cached images
---
---Example:
---
---	forms.clearImageCache( 334 );
---@param componentHandle integer
function forms.clearImageCache(componentHandle) end

---Creates a color object useful with setproperty
---
---Example:
---
---	local coforcre = forms.createcolor( 0x7F, 0x3F, 0x1F, 0xCF );
---@param r integer
---@param g integer
---@param b integer
---@param a integer
---@return dotnetcolor
function forms.createcolor(r, g, b, a) end

---Closes and removes a Lua created form with the specified handle. If a dialog was found and removed true is returned, else false
---
---Example:
---
---	if ( forms.destroy( 332 ) ) then
---		console.log( "Closes and removes a Lua created form with the specified handle. If a dialog was found and removed true is returned, else false" );
---	end;
---@param handle integer
---@return boolean
function forms.destroy(handle) end

---Closes and removes all Lua created dialogs
---
---Example:
---
---	forms.destroyall();
function forms.destroyall() end

---draws a Arc shape at the given coordinates and the given width and height
---
---Example:
---
---	forms.drawArc( 334, 16, 32, 77, 99, 180, 90, 0x007F00FF );
---@param componentHandle integer
---@param x integer
---@param y integer
---@param width integer
---@param height integer
---@param startangle integer
---@param sweepangle integer
---@param line? color
function forms.drawArc(componentHandle, x, y, width, height, startangle, sweepangle, line) end

---Draws an axis of the specified size at the coordinate pair.)
---
---Example:
---
---	forms.drawAxis( 334, 16, 32, int size, 0xFFFFFFFF );
---@param componentHandle integer
---@param x integer
---@param y integer
---@param size integer
---@param color? color
function forms.drawAxis(componentHandle, x, y, size, color) end

---Draws a Bezier curve using the table of coordinates provided in the given color
---
---Example:
---
---	forms.drawBezier( 334, { { 5, 10 }, { 10, 10 }, { 10, 20 }, { 5, 20 } }, 0x000000FF );
---@param componentHandle integer
---@param points table
---@param color color
function forms.drawBezier(componentHandle, points, color) end

---Draws a rectangle on screen from x1/y1 to x2/y2. Same as drawRectangle except it receives two points intead of a point and width/height
---
---Example:
---
---	forms.drawBox( 334, 16, 32, 162, 322, 0x007F00FF, 0x7F7F7FFF );
---@param componentHandle integer
---@param x integer
---@param y integer
---@param x2 integer
---@param y2 integer
---@param line? color
---@param background? color
function forms.drawBox(componentHandle, x, y, x2, y2, line, background) end

---Draws an ellipse at the given coordinates and the given width and height. Line is the color of the ellipse. Background is the optional fill color
---
---Example:
---
---	forms.drawEllipse( 334, 16, 32, 77, 99, 0x007F00FF, 0x7F7F7FFF );
---@param componentHandle integer
---@param x integer
---@param y integer
---@param width integer
---@param height integer
---@param line? color
---@param background? color
function forms.drawEllipse(componentHandle, x, y, width, height, line, background) end

---Draws the image in the given .ico file to a canvas. Canvases can be created with the forms.pictureBox function. The image will be positioned such that its top-left corner will be at (x, y) on the canvas. If width and height are both nil/unset, the image will be drawn at full size (100%). If both are specified, the image will be stretched to that size.
---
---Example:
---
---		forms.drawIcon(picturebox_handle, "C:\\icon.ico", 16, 32, 18, 24);
---@param componentHandle integer
---@param path string
---@param x integer
---@param y integer
---@param width? integer
---@param height? integer
function forms.drawIcon(componentHandle, path, x, y, width, height) end

---Draws the image in the given file (.bmp, .gif, .jpg, .png, or .tif) to a canvas. Canvases can be created with the forms.pictureBox function. The image will be positioned such that its top-left corner will be at (x, y) on the canvas. If width and height are both nil/unset, the image will be drawn at full size (100%). If both are specified, the image will be stretched to that size. If true is passed for the cache parameter, or if it's omitted, the file contents will be cached and re-used next time this function is called with the same path and canvas handle. The canvas' cache can be cleared with forms.clearImageCache.
---
---Example:
---
---		forms.drawImage(picturebox_handle, "C:\\image.png", 16, 32, 18, 24, false);
---@param componentHandle integer
---@param path string
---@param x integer
---@param y integer
---@param width? integer
---@param height? integer
---@param cache? boolean Defaults to `true`
function forms.drawImage(componentHandle, path, x, y, width, height, cache) end

---Draws part of the image in the given file (.bmp, .gif, .jpg, .png, or .tif) to a canvas. Canvases can be created with the forms.pictureBox function. Consult this diagram to see its usage (renders embedded on the TASVideos Wiki): ![Diagram showing how to use forms.drawImageRegion](https://user-images.githubusercontent.com/13409956/198868522-55dc1e5f-ae67-4ebb-a75f-558656cb4468.png) The file contents will be cached and re-used next time this function is called with the same path and canvas handle. The canvas' cache can be cleared with forms.clearImageCache.
---
---Example:
---
---		forms.drawImageRegion(picturebox_handle, "C:\\image.png", 11, 22, 33, 44, 21, 43, 34, 45);
---@param componentHandle integer
---@param path string
---@param source_x integer
---@param source_y integer
---@param source_width integer
---@param source_height integer
---@param dest_x integer
---@param dest_y integer
---@param dest_width? integer
---@param dest_height? integer
function forms.drawImageRegion(componentHandle, path, source_x, source_y, source_width, source_height, dest_x, dest_y, dest_width, dest_height) end

---Draws a line from the first coordinate pair to the 2nd. Color is optional (if not specified it will be drawn black)
---
---Example:
---
---	forms.drawLine( 334, 161, 321, 162, 322, 0xFFFFFFFF );
---@param componentHandle integer
---@param x1 integer
---@param y1 integer
---@param x2 integer
---@param y2 integer
---@param color? color
function forms.drawLine(componentHandle, x1, y1, x2, y2, color) end

---draws a Pie shape at the given coordinates and the given width and height
---
---Example:
---
---	forms.drawPie( 334, 16, 32, 77, 99, 180, 90, 0x007F00FF, 0x7F7F7FFF );
---@param componentHandle integer
---@param x integer
---@param y integer
---@param width integer
---@param height integer
---@param startangle integer
---@param sweepangle integer
---@param line? color
---@param background? color
function forms.drawPie(componentHandle, x, y, width, height, startangle, sweepangle, line, background) end

---Draws a single pixel at the given coordinates in the given color. Color is optional (if not specified it will be drawn black)
---
---Example:
---
---	forms.drawPixel( 334, 16, 32, 0xFFFFFFFF );
---@param componentHandle integer
---@param x integer
---@param y integer
---@param color? color
function forms.drawPixel(componentHandle, x, y, color) end

---Draws a polygon (cyclic polyline) to a canvas. Canvases can be created with the forms.pictureBox function. The polygon must be given as a list of length-2 lists (co-ordinate pairs). Each pair is interpreted as the absolute co-ordinates of one of the vertices, and these are joined together in sequence to form a polygon. The last is connected to the first; you DON'T need to end with a copy of the first to close the cycle. If the x and y parameters are both specified, the whole polygon will be offset by that amount. If a value is passed for the line parameter, the polygon's edges are drawn in that color (i.e. the stroke color). If a value is passed for the background parameter, the polygon's face is filled in that color.
---
---Example:
---
---		forms.drawPolygon(picturebox_handle, { { 5, 10 }, { 10, 10 }, { 10, 20 }, { 5, 20 } }, 10, 30, 0x007F00FF, 0x7F7F7FFF);
---@param componentHandle integer
---@param points table
---@param x? integer
---@param y? integer
---@param line? color
---@param background? color
function forms.drawPolygon(componentHandle, points, x, y, line, background) end

---Draws a rectangle at the given coordinate and the given width and height. Line is the color of the box. Background is the optional fill color
---
---Example:
---
---	forms.drawRectangle( 334, 16, 32, 77, 99, 0x007F00FF, 0x7F7F7FFF );
---@param componentHandle integer
---@param x integer
---@param y integer
---@param width integer
---@param height integer
---@param line? color
---@param background? color
function forms.drawRectangle(componentHandle, x, y, width, height, line, background) end

---Alias of DrawText()
---
---Example:
---
---	forms.drawString( 334, 16, 32, "Some message", 0x7F0000FF, 0x00007FFF, 8, "Arial Narrow", "bold", "center", "middle" );
---@param componentHandle integer
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
function forms.drawString(componentHandle, x, y, message, forecolor, backcolor, fontsize, fontfamily, fontstyle, horizalign, vertalign) end

---Draws the given message at the given x,y coordinates and the given color. The default color is white. A fontfamily can be specified and is monospace generic if none is specified (font family options are the same as the .NET FontFamily class). The fontsize default is 12. The default font style is regular. Font style options are regular, bold, italic, strikethrough, underline. Horizontal alignment options are left (default), center, or right. Vertical alignment options are bottom (default), middle, or top. Alignment options specify which ends of the text will be drawn at the x and y coordinates.
---
---Example:
---
---	forms.drawText( 334, 16, 32, "Some message", 0x7F0000FF, 0x00007FFF, 8, "Arial Narrow", "bold", "center", "middle" );
---@param componentHandle integer
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
function forms.drawText(componentHandle, x, y, message, forecolor, backcolor, fontsize, fontfamily, fontstyle, horizalign, vertalign) end

---Creates a dropdown menu control on the form at formHandle, returning an opaque handle to the new control. The items table should contain all the items (strings) you want to be in the menu. Items will be sorted alphabetically. It doesn't matter if the items table has out-of-order keys or non-numeric keys. If the x and y parameters are both nil/unset, the control's Location property won't be set. If both are specified, the control will be positioned at (x, y) within the given form. If the width and height parameters are both nil/unset, the control will be the default size. If both are specified, the control will be that size.
---
---Example:
---
---		local dropdown_handle = forms.dropdown(form_handle, { "item 1", "item 2" }, 2, 48, 18, 24);
---@param formHandle integer
---@param items table
---@param x? integer
---@param y? integer
---@param width? integer
---@param height? integer
---@return integer
function forms.dropdown(formHandle, items, x, y, width, height) end

---Returns an integer representation of the mouse X coordinate relative to the PictureBox.
---
---Example:
---
---	local inforget = forms.getMouseX( 334 );
---@param componentHandle integer
---@return integer
function forms.getMouseX(componentHandle) end

---Returns an integer representation of the mouse Y coordinate relative to the PictureBox.
---
---Example:
---
---	local inforget = forms.getMouseY( 334 );
---@param componentHandle integer
---@return integer
function forms.getMouseY(componentHandle) end

---returns a string representation of the value of a property of the widget at the given handle
---
---Example:
---
---	local stforget = forms.getproperty(332, "Property");
---@param handle integer
---@param property string
---@return string
function forms.getproperty(handle, property) end

---Returns the text property of a given form or control
---
---Example:
---
---	local stforget = forms.gettext(332);
---@param handle integer
---@return string
function forms.gettext(handle) end

---Returns the given checkbox's checked property
---
---Example:
---
---	if ( forms.ischecked( 332 ) ) then
---		console.log( "Returns the given checkbox's checked property" );
---	end;
---@param handle integer
---@return boolean
function forms.ischecked(handle) end

---Creates a string label control on the form at formHandle, returning an opaque handle to the new control. The label text will be set to the value passed for the caption parameter. If the x and y parameters are both nil/unset, the control's Location property won't be set. If both are specified, the control will be positioned at (x, y) within the given form. If the width and height parameters are both nil/unset, the control will be the default size. If both are specified, the control will be that size. If true is passed for the fixedWidth parameters, a monospace font will be used.
---
---Example:
---
---		local label_handle = forms.label(form_handle, "Caption", 2, 48, 18, 24);
---@param formHandle integer
---@param caption string | number
---@param x? integer
---@param y? integer
---@param width? integer
---@param height? integer
---@param fixedWidth? boolean Defaults to `false`
---@return integer
function forms.label(formHandle, caption, x, y, width, height, fixedWidth) end

---Creates a new form (window), returning an opaque handle to it. If width and height are both nil/unset, the window will be the default size. If both are specified, the window will be that size. The window's title will be set to the value passed for the title parameter, or "Lua Dialog" if nil/unset. If a callback is passed for the onClose parameter, it will be invoked when the window is closed.
---
---Example:
---
---		local form_handle = forms.newform(180, 240, "Cool Tool", function() savestate.loadslot(1); end);
---@param width? integer
---@param height? integer
---@param title? string
---@param onClose? function
---@return integer
function forms.newform(width, height, title, onClose) end

---Creates a standard openfile dialog with optional parameters for the filename, directory, and filter. The return value is the directory that the user picked. If they chose to cancel, it will return an empty string
---
---Example:
---
---	local filename = forms.openfile("C:\filename.bin", "C:\", "Raster Images (*.bmp;*.gif;*.jpg;*.png)|*.bmp;*.gif;*.jpg;*.png|All Files (*.*)|*.*")
---@param fileName? string
---@param initialDirectory? string
---@param filter? string
---@return string
function forms.openfile(fileName, initialDirectory, filter) end

---Creates a drawing canvas control on the form at formHandle, returning an opaque handle to the new control. If the x and y parameters are both nil/unset, the control's Location property won't be set. If both are specified, the control will be positioned at (x, y) within the given form. If the width and height parameters are both nil/unset, the control will be the default size. If both are specified, the control will be that size.
---
---Example:
---
---		local picturebox_handle = forms.pictureBox(form_handle, 2, 48, 18, 24);
---@param formHandle integer
---@param x? integer
---@param y? integer
---@param width? integer
---@param height? integer
---@return integer
function forms.pictureBox(formHandle, x, y, width, height) end

---Redraws the canvas
---
---Example:
---
---	forms.refresh( 334 );
---@param componentHandle integer
function forms.refresh(componentHandle) end

---Sets the default background color to use in drawing methods, transparent by default
---
---Example:
---
---	forms.setDefaultBackgroundColor( 334, 0x000000FF );
---@param componentHandle integer
---@param color color
function forms.setDefaultBackgroundColor(componentHandle, color) end

---Sets the default foreground color to use in drawing methods, white by default
---
---Example:
---
---	forms.setDefaultForegroundColor( 334, 0xFFFFFFFF );
---@param componentHandle integer
---@param color color
function forms.setDefaultForegroundColor(componentHandle, color) end

---Sets the default backgroiund color to use in text drawing methods, half-transparent black by default
---
---Example:
---
---	forms.setDefaultTextBackground( 334, 0x000000FF );
---@param componentHandle integer
---@param color color
function forms.setDefaultTextBackground(componentHandle, color) end

---Replaces the list of options (strings) of a dropdown menu with a new list. If alphabetize is true or unset, it doesn't matter if the items table has out-of-order keys or non-numeric keys, all strings will be in chronological order (by codepoint). If alphabetize is false, items will appear in the given order; the table's keys will be sorted by their numeric value, even if <= 0, and non-numeric keys will come at the end in an undefined order.
---
---Example:
---
---		forms.setdropdownitems(dropdown_handle, { "item1", "item2" });
---@param handle integer
---@param items table
---@param alphabetize? boolean Defaults to `true`
function forms.setdropdownitems(handle, items, alphabetize) end

---Sets the location of a control or form by passing in the handle of the created object
---
---Example:
---
---	forms.setlocation( 332, 16, 32 );
---@param handle integer
---@param x integer
---@param y integer
function forms.setlocation(handle, x, y) end

---Attempts to set the given property of the widget with the given value.  Note: not all properties will be able to be represented for the control to accept
---
---Example:
---
---	forms.setproperty( 332, "Property", "Property value" );
---@param handle integer
---@param property string
---@param value any
function forms.setproperty(handle, property, value) end

---Sets the size of a form (window) or a UI element.
---
---Example:
---
---		forms.setsize(textbox_handle, 640, 96);
---@param handle integer
---@param width integer
---@param height integer
function forms.setsize(handle, width, height) end

---Sets the text property of a control or form by passing in the handle of the created object
---
---Example:
---
---	forms.settext( 332, "Caption" );
---@param handle integer
---@param caption string | number
function forms.settext(handle, caption) end

---Creates a textbox control on the form at formHandle, returning an opaque handle to the new control. The initial value of the textbox will be set to the value passed for the caption parameter, or if nil/unset, left blank. If the x and y parameters are both nil/unset, the control's Location property won't be set. If both are specified, the control will be positioned at (x, y) within the given form. If the width and height parameters are both nil/unset, the control will be the default size. If both are specified, the control will be that size. Passing "HEX", "SIGNED", or "UNSIGNED" for the boxtype parameter will restrict the textbox to accepting valid numbers in that format. If nil/unset, any string value can be entered. If true is passed for the fixedWidth parameters, a monospace font will be used. If true is passed for the multiline parameter, the textbox will accept line breaks. Passing "Vertical", "Horizontal", "Both", or "None" for the scrollbars parameter will set whether the vertical scrollbar is visible for a multiline textbox, and also whether lines should wrap or remain in-line with a scrollbar.
---
---Example:
---
---		local textbox_handle = forms.textbox(form_handle, "Caption", 18, 24, "HEX", 2, 48, true, false, "Both");
---@param formHandle integer
---@param caption? string | number
---@param width? integer
---@param height? integer
---@param boxtype? string
---@param x? integer
---@param y? integer
---@param multiline? boolean Defaults to `false`
---@param fixedWidth? boolean Defaults to `false`
---@param scrollbars? string
---@return integer
function forms.textbox(formHandle, caption, width, height, boxtype, x, y, multiline, fixedWidth, scrollbars) end

