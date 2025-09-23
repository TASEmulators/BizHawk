using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;

using NLua;

namespace BizHawk.Client.EmuHawk
{
	[Description("A library for creating and managing custom dialogs")]
	public sealed partial class FormsLuaLibrary : LuaLibraryBase
	{
		private const string DESC_LINE_OPT_CTRL_POS = " If the x and y parameters are both nil/unset, the control's Location property won't be set. If both are specified, the control will be positioned at (x, y) within the given form.";

		private const string DESC_LINE_OPT_CTRL_SIZE = " If the width and height parameters are both nil/unset, the control will be the default size. If both are specified, the control will be that size.";

		private const string DESC_LINE_OPT_MONOSPACE = " If true is passed for the fixedWidth parameters, a monospace font will be used.";

		private const string ERR_MSG_CONTROL_NOT_LPB = "Drawing functions can only be used on PictureBox components.";

		private const string ERR_MSG_DRAW_ON_FORM = "Drawing functions cannot be used on forms directly. Use them on a PictureBox component.";

		public FormsLuaLibrary(ILuaLibraries luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public Form MainForm { get; set; }

		public override string Name => "forms";

		private readonly List<LuaWinform> _luaForms = new List<LuaWinform>();

		public void WindowClosed(IntPtr handle)
		{
			var i = _luaForms.FindIndex(form => form.Handle == handle);
			if (i is not -1) _luaForms.RemoveAt(i);
		}

		private LuaWinform GetForm(long formHandle)
		{
			var ptr = new IntPtr(formHandle);
			return _luaForms.Find(form => form.Handle == ptr);
		}

		private static void SetLocation(Control control, int x, int y)
			=> control.Location = UIHelper.Scale(new Point(x, y));

		private static void SetSize(Control control, int width, int height)
		{
			var scaled = UIHelper.Scale(new Size(width, height));
			if (control is LuaPictureBox lpb) lpb.LuaResize(scaled.Width, scaled.Height);
			else control.Size = scaled;
		}

		private static void SetText(Control control, string caption)
			=> control.Text = caption ?? string.Empty;

		[LuaMethodExample("forms.addclick( 332, function()\r\n\tconsole.log( \"adds the given lua function as a click event to the given control\" );\r\nend );")]
		[LuaMethod("addclick", "adds the given lua function as a click event to the given control")]
		public void AddClick(long handle, LuaFunction clickEvent)
		{
			var found = FindControlWithHandle(handle, out var form);
			if (found is not null) form.ControlEvents.Add(new(found.Handle, clickEvent));
		}

		[LuaMethodExample("""
			local button_handle = forms.button(form_handle, "Click me", function() console.writeline("boop"); end, 2, 48, 18, 24);
		""")]
		[LuaMethod(
			name: "button",
			description: "Creates a button control on the form at formHandle, returning an opaque handle to the new control."
				+ " The button's label will be set to the value passed for the caption parameter."
				+ " The callback passed for the clickEvent parameter will be invoked whenever the button is clicked."
				+ DESC_LINE_OPT_CTRL_POS
				+ DESC_LINE_OPT_CTRL_SIZE)]
		public long Button(
			long formHandle,
			string caption,
			LuaFunction clickEvent,
			int? x = null,
			int? y = null,
			int? width = null,
			int? height = null)
		{
			var form = GetForm(formHandle);
			if (form == null)
			{
				return 0;
			}

			var button = new LuaButton();
			SetText(button, caption);
			form.Controls.Add(button);
			form.ControlEvents.Add(new LuaWinform.LuaEvent(button.Handle, clickEvent));
			const string FUNC_NAME = "forms.button";
			ProcessPositionArguments(x: x, y: y, button, functionName: FUNC_NAME);
			ProcessSizeArguments(width: width, height: height, button, functionName: FUNC_NAME);
			return (long)button.Handle;
		}

		[LuaMethodExample("""
			local checkbox_handle = forms.checkbox(form_handle, "Enable", 2, 48);
		""")]
		[LuaMethod(
			name: "checkbox",
			description: "Creates a checkbox control on the form at formHandle, returning an opaque handle to the new control."
				+ " The checkbox' label will be set to the value passed for the caption parameter."
				+ DESC_LINE_OPT_CTRL_POS)]
		public long Checkbox(long formHandle, string caption, int? x = null, int? y = null)
		{
			var form = GetForm(formHandle);
			if (form == null)
			{
				return 0;
			}

			var checkbox = new LuaCheckbox();
			form.Controls.Add(checkbox);
			SetText(checkbox, caption);
			const string FUNC_NAME = "forms.checkbox";
			ProcessPositionArguments(x: x, y: y, checkbox, functionName: FUNC_NAME);
			return (long)checkbox.Handle;
		}

		[LuaMethodExample("forms.clearclicks( 332 );")]
		[LuaMethod("clearclicks", "Removes all click events from the given widget at the specified handle")]
		public void ClearClicks(long handle)
		{
			var ptr = new IntPtr(handle);
			var found = FindControlWithHandle(ptr, out var form);
			if (found is not null) form.ControlEvents.RemoveAll(x => x.Control == ptr);
		}

		[LuaMethodExample("if ( forms.destroy( 332 ) ) then\r\n\tconsole.log( \"Closes and removes a Lua created form with the specified handle. If a dialog was found and removed true is returned, else false\" );\r\nend;")]
		[LuaMethod("destroy", "Closes and removes a Lua created form with the specified handle. If a dialog was found and removed true is returned, else false")]
		public bool Destroy(long handle)
		{
			var form = GetForm(handle);
			if (form is null) return false;
			form.Close();
			return _luaForms.Remove(form);
		}

		[LuaMethodExample("forms.destroyall();")]
		[LuaMethod("destroyall", "Closes and removes all Lua created dialogs")]
		public void DestroyAll()
		{
			for (var i = _luaForms.Count - 1; i >= 0; i--)
			{
				_luaForms[i].Close();
			}
			_luaForms.Clear();
		}

		[LuaMethodExample("""
			local dropdown_handle = forms.dropdown(form_handle, { "item 1", "item 2" }, 2, 48, 18, 24);
		""")]
		[LuaMethod(
			name: "dropdown",
			description: "Creates a dropdown menu control on the form at formHandle, returning an opaque handle to the new control."
				+ " The items table should contain all the items (strings) you want to be in the menu. Items will be sorted alphabetically."
				+ " It doesn't matter if the items table has out-of-order keys or non-numeric keys."
				+ DESC_LINE_OPT_CTRL_POS
				+ DESC_LINE_OPT_CTRL_SIZE)]
		public long Dropdown(
			long formHandle,
			LuaTable items,
			int? x = null,
			int? y = null,
			int? width = null,
			int? height = null)
		{
			var form = GetForm(formHandle);
			if (form == null)
			{
				return 0;
			}

			// Include non-numeric, unordered keys for backwards compatibility
			var dropdownItems = items.Values.Cast<string>().ToList();
			dropdownItems.Sort();

			var dropdown = new LuaDropDown(dropdownItems);
			form.Controls.Add(dropdown);
			const string FUNC_NAME = "forms.dropdown";
			ProcessPositionArguments(x: x, y: y, dropdown, functionName: FUNC_NAME);
			ProcessSizeArguments(width: width, height: height, dropdown, functionName: FUNC_NAME);
			return (long)dropdown.Handle;
		}

		[LuaMethodExample("local stforget = forms.getproperty(332, \"Property\");")]
		[LuaMethod("getproperty", "returns a string representation of the value of a property of the widget at the given handle")]
		public string GetProperty(long handle, string property)
		{
			try
			{
				var found = FindFormOrControlWithHandle(handle);
				if (found is not null) return found.GetType().GetProperty(property).GetValue(found, null).ToString();
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}

			return "";
		}

		[LuaMethodExample("local stforget = forms.gettext(332);")]
		[LuaMethod("gettext", "Returns the text property of a given form or control")]
		public string GetText(long handle)
		{
			try
			{
				var found = FindFormOrControlWithHandle(handle);
				if (found is not null) return found is LuaDropDown dd ? dd.SelectedItem.ToString() : found.Text;
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}

			return "";
		}

		[LuaMethodExample("if ( forms.ischecked( 332 ) ) then\r\n\tconsole.log( \"Returns the given checkbox's checked property\" );\r\nend;")]
		[LuaMethod("ischecked", "Returns the given checkbox's checked property")]
		public bool IsChecked(long handle)
			=> FindControlWithHandle(handle) is LuaCheckbox { Checked: true };

		[LuaMethodExample("""
			local label_handle = forms.label(form_handle, "Caption", 2, 48, 18, 24);
		""")]
		[LuaMethod(
			name: "label",
			description: "Creates a string label control on the form at formHandle, returning an opaque handle to the new control."
				+ " The label text will be set to the value passed for the caption parameter."
				+ DESC_LINE_OPT_CTRL_POS
				+ DESC_LINE_OPT_CTRL_SIZE
				+ DESC_LINE_OPT_MONOSPACE)]
		public long Label(
			long formHandle,
			string caption,
			int? x = null,
			int? y = null,
			int? width = null,
			int? height = null,
			bool fixedWidth = false)
		{
			var form = GetForm(formHandle);
			if (form == null)
			{
				return 0;
			}

			var label = new Label();
			if (fixedWidth)
			{
				label.Font = new Font("Courier New", 8);
			}

			SetText(label, caption);
			form.Controls.Add(label);
			const string FUNC_NAME = "forms.label";
			ProcessPositionArguments(x: x, y: y, label, functionName: FUNC_NAME);
			ProcessSizeArguments(width: width, height: height, label, functionName: FUNC_NAME);
			return (long)label.Handle;
		}

		[LuaMethodExample("""
			local form_handle = forms.newform(180, 240, "Cool Tool", function() savestate.loadslot(1); end);
		""")]
		[LuaMethod(
			name: "newform",
			description: "Creates a new form (window), returning an opaque handle to it."
				+ " If width and height are both nil/unset, the window will be the default size. If both are specified, the window will be that size."
				+ " The window's title will be set to the value passed for the title parameter, or \"Lua Dialog\" if nil/unset."
				+ " If a callback is passed for the onClose parameter, it will be invoked when the window is closed.")]
		public long NewForm(
			int? width = null,
			int? height = null,
			string title = null,
			LuaFunction onClose = null)
		{
			var form = new LuaWinform(CurrentFile, WindowClosed);
			_luaForms.Add(form);
			if (width.HasValue && height.HasValue)
			{
				form.ClientSize = UIHelper.Scale(new Size(width.Value, height.Value));
			}

			if (!string.IsNullOrWhiteSpace(title)) form.Text = title;
			form.MaximizeBox = false;
			form.FormBorderStyle = FormBorderStyle.FixedDialog;
			form.Icon = SystemIcons.Application;
			form.Owner = MainForm;
			form.Show();

			form.FormClosed += (o, e) =>
			{
				if (onClose != null)
				{
					try
					{
						onClose.Call();
					}
					catch (Exception ex)
					{
						Log(ex.ToString());
					}
				}
			};

			return (long)form.Handle;
		}

		[LuaMethodExample(@"local filename = forms.openfile(""C:\filename.bin"", ""C:\"", ""Raster Images (*.bmp;*.gif;*.jpg;*.png)|*.bmp;*.gif;*.jpg;*.png|All Files (*.*)|*.*"")")]
		[LuaMethod(
			"openfile", "Creates a standard openfile dialog with optional parameters for the filename, directory, and filter. The return value is the directory that the user picked. If they chose to cancel, it will return an empty string")]
		public string OpenFile(
			string fileName = null,
			string initialDirectory = null,
			string filter = null)
		{
			if (initialDirectory is null && fileName is not null) initialDirectory = Path.GetDirectoryName(fileName);
			var result = ((IDialogParent) MainForm).ShowFileOpenDialog(
				filterStr: filter ?? FilesystemFilter.AllFilesEntry,
				initDir: initialDirectory ?? PathEntries.LuaAbsolutePath(),
				initFileName: fileName);
			return result ?? string.Empty;
		}

		[LuaMethodExample("""
			local picturebox_handle = forms.pictureBox(form_handle, 2, 48, 18, 24);
		""")]
		[LuaMethod(
			name: "pictureBox",
			description: "Creates a drawing canvas control on the form at formHandle, returning an opaque handle to the new control."
				+ DESC_LINE_OPT_CTRL_POS
				+ DESC_LINE_OPT_CTRL_SIZE)]
		public long PictureBox(long formHandle, int? x = null, int? y = null, int? width = null, int? height = null)
		{
			var form = GetForm(formHandle);
			if (form == null)
			{
				return 0;
			}

			LuaPictureBox pictureBox = new(_th, LogOutputCallback);
			form.Controls.Add(pictureBox);
			const string FUNC_NAME = "forms.pictureBox";
			ProcessPositionArguments(x: x, y: y, pictureBox, functionName: FUNC_NAME);
			if (width is int w && height is int h)
			{
				pictureBox.LuaResize(width: w, height: h);
				SetSize(pictureBox, width: w, height: h);
			}
			else if (width.HasValue || height.HasValue)
			{
				WarnForMismatchedPair(functionName: FUNC_NAME, kind: "width and height");
			}

			return (long)pictureBox.Handle;
		}

		[LuaMethodExample("forms.clear( 334, 0x000000FF );")]
		[LuaMethod(
			"clear",
			"Clears the canvas")]
		public void Clear(long componentHandle, [LuaColorParam] object color)
		{
			try
			{
				var color1 = _th.ParseColor(color);
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control) control.Clear(color1);
				else if (match is Form) LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				else if (match is not null) LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample("forms.refresh( 334 );")]
		[LuaMethod(
			"refresh",
			"Redraws the canvas")]
		public void Refresh(long componentHandle)
		{
			try
			{
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control) control.Refresh();
				else if (match is Form) LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				else if (match is not null) LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample("forms.setDefaultForegroundColor( 334, 0xFFFFFFFF );")]
		[LuaMethod(
			"setDefaultForegroundColor",
			"Sets the default foreground color to use in drawing methods, white by default")]
		public void SetDefaultForegroundColor(long componentHandle, [LuaColorParam] object color)
		{
			try
			{
				var color1 = _th.ParseColor(color);
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control) control.SetDefaultForegroundColor(color1);
				else if (match is Form) LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				else if (match is not null) LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample("forms.setDefaultBackgroundColor( 334, 0x000000FF );")]
		[LuaMethod(
			"setDefaultBackgroundColor",
			"Sets the default background color to use in drawing methods, transparent by default")]
		public void SetDefaultBackgroundColor(long componentHandle, [LuaColorParam] object color)
		{
			try
			{
				var color1 = _th.ParseColor(color);
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control) control.SetDefaultBackgroundColor(color1);
				else if (match is Form) LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				else if (match is not null) LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample("forms.setDefaultTextBackground( 334, 0x000000FF );")]
		[LuaMethod(
			"setDefaultTextBackground",
			"Sets the default backgroiund color to use in text drawing methods, half-transparent black by default")]
		public void SetDefaultTextBackground(long componentHandle, [LuaColorParam] object color)
		{
			try
			{
				var color1 = _th.ParseColor(color);
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control) control.SetDefaultTextBackground(color1);
				else if (match is Form) LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				else if (match is not null) LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawBezier( 334, { { 5, 10 }, { 10, 10 }, { 10, 20 }, { 5, 20 } }, 0x000000FF );")]
		[LuaMethod(
			"drawBezier",
			"Draws a Bezier curve using the table of coordinates provided in the given color")]
		public void DrawBezier(long componentHandle, LuaTable points, [LuaColorParam] object color)
		{
			try
			{
				var color1 = _th.ParseColor(color);
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control) control.DrawBezier(points, color1);
				else if (match is Form) LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				else if (match is not null) LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawBox( 334, 16, 32, 162, 322, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"drawBox",
			"Draws a rectangle on screen from x1/y1 to x2/y2. Same as drawRectangle except it receives two points intead of a point and width/height")]
		public void DrawBox(
			long componentHandle,
			int x,
			int y,
			int x2,
			int y2,
			[LuaColorParam] object line = null,
			[LuaColorParam] object background = null)
		{
			try
			{
				var strokeColor = _th.SafeParseColor(line);
				var fillColor = _th.SafeParseColor(background);
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control)
				{
					control.DrawBox(x: x, y: y, x2: x2, y2: y2, line: strokeColor, background: fillColor);
				}
				else if (match is Form)
				{
					LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				}
				else if (match is not null)
				{
					LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
				}
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawEllipse( 334, 16, 32, 77, 99, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"drawEllipse",
			"Draws an ellipse at the given coordinates and the given width and height. Line is the color of the ellipse. Background is the optional fill color")]
		public void DrawEllipse(
			long componentHandle,
			int x,
			int y,
			int width,
			int height,
			[LuaColorParam] object line = null,
			[LuaColorParam] object background = null)
		{
			try
			{
				var strokeColor = _th.SafeParseColor(line);
				var fillColor = _th.SafeParseColor(background);
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control)
				{
					control.DrawEllipse(x: x, y: y, width: width, height: height, line: strokeColor, background: fillColor);
				}
				else if (match is Form)
				{
					LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				}
				else if (match is not null)
				{
					LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
				}
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample("""
			forms.drawIcon(picturebox_handle, "C:\\icon.ico", 16, 32, 18, 24);
		""")]
		[LuaMethod(
			name: "drawIcon",
			description: "Draws the image in the given .ico file to a canvas. Canvases can be created with the forms.pictureBox function."
				+ " The image will be positioned such that its top-left corner will be at (x, y) on the canvas."
				+ " If width and height are both nil/unset, the image will be drawn at full size (100%). If both are specified, the image will be stretched to that size.")]
		public void DrawIcon(
			long componentHandle,
			string path,
			int x,
			int y,
			int? width = null,
			int? height = null)
		{
			const string FUNC_NAME = "forms.drawIcon";
			if (!File.Exists(path))
			{
				LogOutputCallback($"{FUNC_NAME}: file \"{path}\" not found");
				return;
			}
			try
			{
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control)
				{
					control.DrawIcon(
						path: path,
						x: x,
						y: y,
						width: width,
						height: height,
						functionName: FUNC_NAME);
				}
				else if (match is Form)
				{
					LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				}
				else if (match is not null)
				{
					LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
				}
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample("""
			forms.drawImage(picturebox_handle, "C:\\image.png", 16, 32, 18, 24, false);
		""")]
		[LuaMethod(
			name: "drawImage",
			description: "Draws the image in the given file (.bmp, .gif, .jpg, .png, or .tif) to a canvas. Canvases can be created with the forms.pictureBox function."
				+ " The image will be positioned such that its top-left corner will be at (x, y) on the canvas."
				+ " If width and height are both nil/unset, the image will be drawn at full size (100%). If both are specified, the image will be stretched to that size." // technically width or height can be specified w/o the other but let's leave that as UB
				+ " If true is passed for the cache parameter, or if it's omitted, the file contents will be cached and re-used next time this function is called with the same path and canvas handle. The canvas' cache can be cleared with forms.clearImageCache.")]
		public void DrawImage(
			long componentHandle,
			string path,
			int x,
			int y,
			int? width = null,
			int? height = null,
			bool cache = true)
		{
			if (!File.Exists(path))
			{
				LogOutputCallback($"forms.drawImage: file \"{path}\" not found");
				return;
			}
			try
			{
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control)
				{
					control.DrawImage(path, x: x, y: y, width: width, height: height, cache: cache);
				}
				else if (match is Form)
				{
					LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				}
				else if (match is not null)
				{
					LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
				}
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample("forms.clearImageCache( 334 );")]
		[LuaMethod(
			"clearImageCache",
			"clears the image cache that is built up by using gui.drawImage, also releases the file handle for cached images")]
		public void ClearImageCache(long componentHandle)
		{
			try
			{
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control) control.ClearImageCache();
				else if (match is Form) LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				else if (match is not null) LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample("""
			forms.drawImageRegion(picturebox_handle, "C:\\image.png", 11, 22, 33, 44, 21, 43, 34, 45);
		""")]
		[LuaMethod(
			name: "drawImageRegion",
			description: "Draws part of the image in the given file (.bmp, .gif, .jpg, .png, or .tif) to a canvas. Canvases can be created with the forms.pictureBox function."
				+ " Consult this diagram to see its usage (renders embedded on the TASVideos Wiki): [https://user-images.githubusercontent.com/13409956/198868522-55dc1e5f-ae67-4ebb-a75f-558656cb4468.png|alt=Diagram showing how to use forms.drawImageRegion]"
				+ " The file contents will be cached and re-used next time this function is called with the same path and canvas handle. The canvas' cache can be cleared with forms.clearImageCache.")]
		public void DrawImageRegion(
			long componentHandle,
			string path,
			int source_x,
			int source_y,
			int source_width,
			int source_height,
			int dest_x,
			int dest_y,
			int? dest_width = null,
			int? dest_height = null)
		{
			if (!File.Exists(path))
			{
				LogOutputCallback($"forms.drawImageRegion: file \"{path}\" not found");
				return;
			}
			try
			{
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control)
				{
					control.DrawImageRegion(
						path,
						sourceX: source_x,
						sourceY: source_y,
						sourceWidth: source_width,
						sourceHeight: source_height,
						destX: dest_x,
						destY: dest_y,
						destWidth: dest_width,
						destHeight: dest_height);
				}
				else if (match is Form)
				{
					LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				}
				else if (match is not null)
				{
					LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
				}
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawLine( 334, 161, 321, 162, 322, 0xFFFFFFFF );")]
		[LuaMethod(
			"drawLine",
			"Draws a line from the first coordinate pair to the 2nd. Color is optional (if not specified it will be drawn black)")]
		public void DrawLine(long componentHandle, int x1, int y1, int x2, int y2, [LuaColorParam] object color = null)
		{
			try
			{
				var color1 = _th.SafeParseColor(color);
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control) control.DrawLine(x1: x1, y1: y1, x2: x2, y2: y2, color1);
				else if (match is Form) LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				else if (match is not null) LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawAxis( 334, 16, 32, int size, 0xFFFFFFFF );")]
		[LuaMethod(
			"drawAxis",
			"Draws an axis of the specified size at the coordinate pair.)")]
		public void DrawAxis(long componentHandle, int x, int y, int size, [LuaColorParam] object color = null)
		{
			try
			{
				var color1 = _th.SafeParseColor(color);
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control) control.DrawAxis(x: x, y: y, size, color1);
				else if (match is Form) LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				else if (match is not null) LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawArc( 334, 16, 32, 77, 99, 180, 90, 0x007F00FF );")]
		[LuaMethod(
			"drawArc",
			"draws a Arc shape at the given coordinates and the given width and height"
		)]
		public void DrawArc(
			long componentHandle,
			int x,
			int y,
			int width,
			int height,
			int startangle,
			int sweepangle,
			[LuaColorParam] object line = null)
		{
			try
			{
				var strokeColor = _th.SafeParseColor(line);
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control)
				{
					control.DrawArc(
						x: x,
						y: y,
						width: width,
						height: height,
						startAngle: startangle,
						sweepAngle: sweepangle,
						strokeColor);
				}
				else if (match is Form)
				{
					LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				}
				else if (match is not null)
				{
					LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
				}
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawPie( 334, 16, 32, 77, 99, 180, 90, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"drawPie",
			"draws a Pie shape at the given coordinates and the given width and height")]
		public void DrawPie(
			long componentHandle,
			int x,
			int y,
			int width,
			int height,
			int startangle,
			int sweepangle,
			[LuaColorParam] object line = null,
			[LuaColorParam] object background = null)
		{
			try
			{
				var strokeColor = _th.SafeParseColor(line);
				var fillColor = _th.SafeParseColor(background);
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control)
				{
					control.DrawPie(
						x: x,
						y: y,
						width: width,
						height: height,
						startAngle: startangle,
						sweepAngle: sweepangle,
						line: strokeColor,
						background: fillColor);
				}
				else if (match is Form)
				{
					LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				}
				else if (match is not null)
				{
					LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
				}
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawPixel( 334, 16, 32, 0xFFFFFFFF );")]
		[LuaMethod(
			"drawPixel",
			"Draws a single pixel at the given coordinates in the given color. Color is optional (if not specified it will be drawn black)")]
		public void DrawPixel(long componentHandle, int x, int y, [LuaColorParam] object color = null)
		{
			try
			{
				var color1 = _th.SafeParseColor(color);
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control) control.DrawPixel(x: x, y: y, color1);
				else if (match is Form) LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				else if (match is not null) LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample("""
			forms.drawPolygon(picturebox_handle, { { 5, 10 }, { 10, 10 }, { 10, 20 }, { 5, 20 } }, 10, 30, 0x007F00FF, 0x7F7F7FFF);
		""")]
		[LuaMethod(
			name: "drawPolygon",
			description: "Draws a polygon (cyclic polyline) to a canvas. Canvases can be created with the forms.pictureBox function."
				+ " The polygon must be given as a list of length-2 lists (co-ordinate pairs). Each pair is interpreted as the absolute co-ordinates of one of the vertices, and these are joined together in sequence to form a polygon. The last is connected to the first; you DON'T need to end with a copy of the first to close the cycle."
				+ " If the x and y parameters are both specified, the whole polygon will be offset by that amount." // technically x or y can be specified w/o the other but let's leave that as UB
				+ " If a value is passed for the line parameter, the polygon's edges are drawn in that color (i.e. the stroke color)."
				+ " If a value is passed for the background parameter, the polygon's face is filled in that color.")]
		public void DrawPolygon(
			long componentHandle,
			LuaTable points,
			int? x = null,
			int? y = null,
			[LuaColorParam] object line = null,
			[LuaColorParam] object background = null)
		{
			try
			{
				var strokeColor = _th.SafeParseColor(line);
				var fillColor = _th.SafeParseColor(background);
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control)
				{
					control.DrawPolygon(points, x: x, y: y, line: strokeColor, background: fillColor);
				}
				else if (match is Form)
				{
					LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				}
				else if (match is not null)
				{
					LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
				}
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}


		[LuaMethodExample("forms.drawRectangle( 334, 16, 32, 77, 99, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"drawRectangle",
			"Draws a rectangle at the given coordinate and the given width and height. Line is the color of the box. Background is the optional fill color")]
		public void DrawRectangle(
			long componentHandle,
			int x,
			int y,
			int width,
			int height,
			[LuaColorParam] object line = null,
			[LuaColorParam] object background = null)
		{
			try
			{
				var strokeColor = _th.SafeParseColor(line);
				var fillColor = _th.SafeParseColor(background);
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control)
				{
					control.DrawRectangle(
						x: x,
						y: y,
						width: width,
						height: height,
						line: strokeColor,
						background: fillColor);
				}
				else if (match is Form)
				{
					LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				}
				else if (match is not null)
				{
					LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
				}
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawString( 334, 16, 32, \"Some message\", 0x7F0000FF, 0x00007FFF, 8, \"Arial Narrow\", \"bold\", \"center\", \"middle\" );")]
		[LuaMethod(
			"drawString",
			"Alias of DrawText()")]
		public void DrawString(
			long componentHandle,
			int x,
			int y,
			string message,
			[LuaColorParam] object forecolor = null,
			[LuaColorParam] object backcolor = null,
			int? fontsize = null,
			string fontfamily = null,
			string fontstyle = null,
			string horizalign = null,
			string vertalign = null)
				=> DrawText(
					componentHandle: componentHandle,
					x: x,
					y: y,
					message: message,
					forecolor: forecolor,
					backcolor: backcolor,
					fontsize: fontsize,
					fontfamily: fontfamily,
					fontstyle: fontstyle,
					horizalign: horizalign,
					vertalign: vertalign);

		[LuaMethodExample("forms.drawText( 334, 16, 32, \"Some message\", 0x7F0000FF, 0x00007FFF, 8, \"Arial Narrow\", \"bold\", \"center\", \"middle\" );")]
		[LuaMethod(
			"drawText",
			"Draws the given message at the given x,y coordinates and the given color. The default color is white. A fontfamily can be specified and is monospace generic if none is specified (font family options are the same as the .NET FontFamily class). The fontsize default is 12. The default font style is regular. Font style options are regular, bold, italic, strikethrough, underline. Horizontal alignment options are left (default), center, or right. Vertical alignment options are bottom (default), middle, or top. Alignment options specify which ends of the text will be drawn at the x and y coordinates.")]
		public void DrawText(
			long componentHandle,
			int x,
			int y,
			string message,
			[LuaColorParam] object forecolor = null,
			[LuaColorParam] object backcolor = null,
			int? fontsize = null,
			string fontfamily = null,
			string fontstyle = null,
			string horizalign = null,
			string vertalign = null)
		{
			try
			{
				var fgColor = _th.SafeParseColor(forecolor);
				var bgColor = _th.SafeParseColor(backcolor);
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control)
				{
					control.DrawText(
						x: x,
						y: y,
						message: message,
						foreColor: fgColor,
						backColor: bgColor,
						fontSize: fontsize,
						fontFamily: fontfamily,
						fontStyle: fontstyle,
						horizAlign: horizalign,
						vertAlign: vertalign);
				}
				else if (match is Form)
				{
					LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
				}
				else if (match is not null)
				{
					LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
				}
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		private Control/*?*/ FindControlWithHandle(IntPtr handle)
		{
			foreach (var form in _luaForms) foreach (Control control in form.Controls)
			{
				if (control.Handle == handle) return control;
			}
			return null;
		}

		private Control/*?*/ FindControlWithHandle(IntPtr handle, out LuaWinform parentForm)
		{
			foreach (var form in _luaForms) foreach (Control control in form.Controls)
			{
				if (control.Handle != handle) continue;
				parentForm = form;
				return control;
			}
			parentForm = null;
			return null;
		}

		private Control/*?*/ FindControlWithHandle(long handle)
			=> FindControlWithHandle(new IntPtr(handle));

		private Control/*?*/ FindControlWithHandle(long handle, out LuaWinform parentForm)
			=> FindControlWithHandle(new IntPtr(handle), out parentForm);

		private Control/*?*/ FindFormOrControlWithHandle(IntPtr handle)
		{
			foreach (var form in _luaForms)
			{
				if (form.Handle == handle) return form;
				foreach (Control control in form.Controls) if (control.Handle == handle) return control;
			}
			return null;
		}

		private Control/*?*/ FindFormOrControlWithHandle(long handle)
			=> FindFormOrControlWithHandle(new IntPtr(handle));

		// It'd be great if these were simplified into 1 function, but I cannot figure out how to return a LuaTable from this class
		[LuaMethodExample("local inforget = forms.getMouseX( 334 );")]
		[LuaMethod(
			"getMouseX",
			"Returns an integer representation of the mouse X coordinate relative to the PictureBox.")]
		public int GetMouseX(long componentHandle)
		{
			try
			{
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control) return control.GetMouse().X;
				if (match is Form)
				{
					LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
					return 0;
				}
				if (match is not null) LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
				return default;
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}

			return 0;
		}

		[LuaMethodExample("local inforget = forms.getMouseY( 334 );")]
		[LuaMethod(
			"getMouseY",
			"Returns an integer representation of the mouse Y coordinate relative to the PictureBox.")]
		public int GetMouseY(long componentHandle)
		{
			try
			{
				var match = FindFormOrControlWithHandle(componentHandle);
				if (match is LuaPictureBox control) return control.GetMouse().Y;
				if (match is Form)
				{
					LogOutputCallback(ERR_MSG_DRAW_ON_FORM);
					return 0;
				}
				if (match is not null) LogOutputCallback(ERR_MSG_CONTROL_NOT_LPB);
				return default;
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}

			return 0;
		}

		private void ProcessPositionArguments(int? x, int? y, Control c, string functionName)
		{
			if (x is int x1 && y is int y1) SetLocation(c, x: x1, y: y1);
			else if (x.HasValue || y.HasValue) WarnForMismatchedPair(functionName: functionName, kind: "x and y");
		}

		private void ProcessSizeArguments(int? width, int? height, Control c, string functionName)
		{
			if (width is int w && height is int h) SetSize(c, width: w, height: h);
			else if (width.HasValue || height.HasValue) WarnForMismatchedPair(functionName: functionName, kind: "width and height");
		}

		[LuaMethodExample("""
			forms.setdropdownitems(dropdown_handle, { "item1", "item2" });
		""")]
		[LuaMethod(
			name: "setdropdownitems",
			description: "Replaces the list of options (strings) of a dropdown menu with a new list."
				+ " If alphabetize is true or unset, it doesn't matter if the items table has out-of-order keys or non-numeric keys, all strings will be in chronological order (by codepoint)."
				+ " If alphabetize is false, items will appear in the given order; the table's keys will be sorted by their numeric value, even if <= 0, and non-numeric keys will come at the end in an undefined order.")]
		public void SetDropdownItems(long handle, LuaTable items, bool alphabetize = true)
		{
			try
			{
				if (FindControlWithHandle(handle) is LuaDropDown ldd)
				{
					// Include non-numeric, unordered keys for backwards compatibility
					// Sort numeric keys to maintain order of sequential {"Foo", "Bar"} tables when values are not alphabetized
					// Order of non-numeric keys is undetermined
					var dropdownItems = alphabetize
						? items.Values.Cast<string>().Order()
						: items.OrderBy(kvp => kvp.Key as long? ?? long.MaxValue).Select(kvp => (string)kvp.Value);
					ldd.SetItems(dropdownItems.ToList());
				}
			}
			catch (Exception ex)
			{
				Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.setlocation( 332, 16, 32 );")]
		[LuaMethod("setlocation", "Sets the location of a control or form by passing in the handle of the created object")]
		public void SetLocation(long handle, int x, int y)
		{
			var found = FindFormOrControlWithHandle(handle);
			if (found is not null) SetLocation(found, x: x, y: y);
		}

		/// <exception cref="Exception">
		/// identifier doesn't match any prop of referenced <see cref="Form"/>/<see cref="Control"/>; or
		/// property is of type <see cref="Color"/> and a string was passed with the wrong format
		/// </exception>
		[LuaMethodExample("forms.setproperty( 332, \"Property\", \"Property value\" );")]
		[LuaMethod("setproperty", "Attempts to set the given property of the widget with the given value.  Note: not all properties will be able to be represented for the control to accept")]
		public void SetProperty(long handle, string property, object value)
		{
			var c = FindFormOrControlWithHandle(handle);
			if (c is null) return;
			// relying on exceptions for error handling here
			var pi = c.GetType().GetProperty(property) ?? throw new Exception($"no property with the identifier {property}");
			var pt = pi.PropertyType;
			var o = pt.IsEnum
				? Enum.Parse(pt, value.ToString(), true)
				: pt == typeof(Color)
					? _th.ParseColor(value)
					: Convert.ChangeType(value, pt);
			pi.SetValue(c, o, null);
		}

		[LuaMethodExample("local coforcre = forms.createcolor( 0x7F, 0x3F, 0x1F, 0xCF );")]
		[LuaMethod("createcolor", "Creates a color object useful with setproperty")]
		public Color CreateColor(int r, int g, int b, int a)
		{
			return Color.FromArgb(a, r, g, b);
		}

		[LuaMethodExample("""
			forms.setsize(textbox_handle, 640, 96);
		""")]
		[LuaMethod(
			name: "setsize",
			description: "Sets the size of a form (window) or a UI element.")]
		public void SetSize(long handle, int width, int height)
		{
			var control = FindFormOrControlWithHandle(handle);
			if (control is Form form)
			{
				form.ClientSize = UIHelper.Scale(new Size(width: width, height: height));
				return;
			}
			if (control is not null) SetSize(control, width: width, height: height);
		}

		[LuaMethodExample("forms.settext( 332, \"Caption\" );")]
		[LuaMethod("settext", "Sets the text property of a control or form by passing in the handle of the created object")]
		public void Settext(long handle, string caption)
		{
			var found = FindFormOrControlWithHandle(handle);
			if (found is not null) SetText(found, caption);
		}

		[LuaMethodExample("""
			local textbox_handle = forms.textbox(form_handle, "Caption", 18, 24, "HEX", 2, 48, true, false, "Both");
		""")]
		[LuaMethod(
			name: "textbox",
			description: "Creates a textbox control on the form at formHandle, returning an opaque handle to the new control."
				+ " The initial value of the textbox will be set to the value passed for the caption parameter, or if nil/unset, left blank."
				+ DESC_LINE_OPT_CTRL_POS
				+ DESC_LINE_OPT_CTRL_SIZE
				+ " Passing \"HEX\", \"SIGNED\", or \"UNSIGNED\" for the boxtype parameter will restrict the textbox to accepting valid numbers in that format. If nil/unset, any string value can be entered." // technically case-insensitive but let's stick to fixed values
				+ DESC_LINE_OPT_MONOSPACE
				+ " If true is passed for the multiline parameter, the textbox will accept line breaks."
				+ " Passing \"Vertical\", \"Horizontal\", \"Both\", or \"None\" for the scrollbars parameter will set whether the vertical scrollbar is visible for a multiline textbox, and also whether lines should wrap or remain in-line with a scrollbar.")] // technically case-insensitive but let's stick to fixed values
		public long Textbox(
			long formHandle,
			string caption = null,
			int? width = null,
			int? height = null,
			string boxtype = null,
			int? x = null,
			int? y = null,
			bool multiline = false,
			bool fixedWidth = false,
			string scrollbars = null)
		{
			var form = GetForm(formHandle);
			if (form == null)
			{
				return 0;
			}

			var textbox = new LuaTextBox();
			if (fixedWidth)
			{
				textbox.Font = new Font("Courier New", 8);
			}

			textbox.Multiline = multiline;
			if (scrollbars != null)
			{
				switch (scrollbars.ToUpperInvariant())
				{
					case "VERTICAL":
						textbox.ScrollBars = ScrollBars.Vertical;
						break;
					case "HORIZONTAL":
						textbox.ScrollBars = ScrollBars.Horizontal;
						textbox.WordWrap = false;
						break;
					case "BOTH":
						textbox.ScrollBars = ScrollBars.Both;
						textbox.WordWrap = false;
						break;
					case "NONE":
						textbox.ScrollBars = ScrollBars.None;
						break;
				}
			}

			SetText(textbox, caption);
			const string FUNC_NAME = "forms.textbox";
			ProcessPositionArguments(x: x, y: y, textbox, functionName: FUNC_NAME);
			ProcessSizeArguments(width: width, height: height, textbox, functionName: FUNC_NAME);

			if (boxtype != null)
			{
				switch (boxtype.ToUpperInvariant())
				{
					case "HEX":
					case "HEXADECIMAL":
						textbox.SetType(BoxType.Hex);
						break;
					case "UNSIGNED":
					case "UINT":
						textbox.SetType(BoxType.Unsigned);
						break;
					case "NUMBER":
					case "NUM":
					case "SIGNED":
					case "INT":
						textbox.SetType(BoxType.Signed);
						break;
				}
			}

			form.Controls.Add(textbox);
			return (long)textbox.Handle;
		}

		private void WarnForMismatchedPair(string functionName, string kind)
			=> LogOutputCallback($"{functionName}: both {kind} must be set to have any effect");
	}
}
