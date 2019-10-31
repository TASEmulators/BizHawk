using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using NLua;
using System.IO;

namespace BizHawk.Client.EmuHawk
{
	[Description("A library for creating and managing custom dialogs")]
	public sealed class FormsLuaLibrary : LuaLibraryBase
	{
		public FormsLuaLibrary(Lua lua)
			: base(lua) { }

		public FormsLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		// TODO: replace references to ConsoleLuaLibrary.Log with a callback that is passed in
		public override string Name => "forms";

		#region Forms Library Helpers

		private readonly List<LuaWinform> _luaForms = new List<LuaWinform>();

		public void WindowClosed(IntPtr handle)
		{
			foreach (var form in _luaForms)
			{
				if (form.Handle == handle)
				{
					_luaForms.Remove(form);
					return;
				}
			}
		}

		private LuaWinform GetForm(int formHandle)
		{
			var ptr = new IntPtr(formHandle);
			return _luaForms.FirstOrDefault(form => form.Handle == ptr);
		}

		private static void SetLocation(Control control, int x, int y)
		{
			control.Location = new Point(x, y);
		}

		private static void SetSize(Control control, int width, int height)
		{
			control.Size = new Size(width, height);
		}

		private static void SetText(Control control, string caption)
		{
			control.Text = caption ?? "";
		}

		#endregion

		[LuaMethodExample("forms.addclick( 332, function()\r\n\tconsole.log( \"adds the given lua function as a click event to the given control\" );\r\nend );")]
		[LuaMethod("addclick", "adds the given lua function as a click event to the given control")]
		public void AddClick(int handle, LuaFunction clickEvent)
		{
			var ptr = new IntPtr(handle);
			foreach (var form in _luaForms)
			{
				foreach (Control control in form.Controls)
				{
					if (control.Handle == ptr)
					{
						form.ControlEvents.Add(new LuaWinform.LuaEvent(control.Handle, clickEvent));
					}
				}
			}
		}

		[LuaMethodExample("local inforbut = forms.button( 333, \"Caption\", function()\r\n\tconsole.log( \"Creates a button control on the given form. The caption property will be the text value on the button. clickEvent is the name of a Lua function that will be invoked when the button is clicked. x, and y are the optional location parameters for the position of the button within the given form. The function returns the handle of the created button. Width and Height are optional, if not specified they will be a default size\" );\r\nend, 2, 48, 18, 24 );")]
		[LuaMethod(
			"button", "Creates a button control on the given form. The caption property will be the text value on the button. clickEvent is the name of a Lua function that will be invoked when the button is clicked. x, and y are the optional location parameters for the position of the button within the given form. The function returns the handle of the created button. Width and Height are optional, if not specified they will be a default size")]
		public int Button(
			int formHandle,
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

			if (x.HasValue && y.HasValue)
			{
				SetLocation(button, x.Value, y.Value);
			}

			if (width.HasValue && height.HasValue)
			{
				SetSize(button, width.Value, height.Value);
			}

			return (int)button.Handle;
		}

		[LuaMethodExample("local inforche = forms.checkbox( 333, \"Caption\", 2, 48 );")]
		[LuaMethod(
			"checkbox", "Creates a checkbox control on the given form. The caption property will be the text of the checkbox. x and y are the optional location parameters for the position of the checkbox within the form")]
		public int Checkbox(int formHandle, string caption, int? x = null, int? y = null)
		{
			var form = GetForm(formHandle);
			if (form == null)
			{
				return 0;
			}

			var checkbox = new LuaCheckbox();
			form.Controls.Add(checkbox);
			SetText(checkbox, caption);

			if (x.HasValue && y.HasValue)
			{
				SetLocation(checkbox, x.Value, y.Value);
			}

			return (int)checkbox.Handle;
		}

		[LuaMethodExample("forms.clearclicks( 332 );")]
		[LuaMethod("clearclicks", "Removes all click events from the given widget at the specified handle")]
		public void ClearClicks(int handle)
		{
			var ptr = new IntPtr(handle);
			foreach (var form in _luaForms)
			{
				foreach (Control control in form.Controls)
				{
					if (control.Handle == ptr)
					{
						form.ControlEvents.RemoveAll(x => x.Control == ptr);
					}
				}
			}
		}

		[LuaMethodExample("if ( forms.destroy( 332 ) ) then\r\n\tconsole.log( \"Closes and removes a Lua created form with the specified handle. If a dialog was found and removed true is returned, else false\" );\r\nend;")]
		[LuaMethod("destroy", "Closes and removes a Lua created form with the specified handle. If a dialog was found and removed true is returned, else false")]
		public bool Destroy(int handle)
		{
			var ptr = new IntPtr(handle);
			foreach (var form in _luaForms)
			{
				if (form.Handle == ptr)
				{
					form.Close();
					_luaForms.Remove(form);
					return true;
				}
			}

			return false;
		}

		[LuaMethodExample("forms.destroyall();")]
		[LuaMethod("destroyall", "Closes and removes all Lua created dialogs")]
		public void DestroyAll()
		{
			for (var i = _luaForms.Count - 1; i >= 0; i--)
			{
				_luaForms[i].Close();
			}
		}

		[LuaMethodExample("local infordro = forms.dropdown(333, { \"item 1\", \"item2\" }, 2, 48, 18, 24);")]
		[LuaMethod(
			"dropdown", "Creates a dropdown (with a ComboBoxStyle of DropDownList) control on the given form. Dropdown items are passed via a lua table. Only the values will be pulled for the dropdown items, the keys are irrelevant. Items will be sorted alphabetically. x and y are the optional location parameters, and width and height are the optional size parameters.")]
		public int Dropdown(
			int formHandle,
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

			var dropdownItems = items.Values.Cast<string>().ToList();
			dropdownItems.Sort();

			var dropdown = new LuaDropDown(dropdownItems);
			form.Controls.Add(dropdown);

			if (x.HasValue && y.HasValue)
			{
				SetLocation(dropdown, x.Value, y.Value);
			}

			if (width.HasValue && height.HasValue)
			{
				SetSize(dropdown, width.Value, height.Value);
			}

			return (int)dropdown.Handle;
		}

		[LuaMethodExample("local stforget = forms.getproperty(332, \"Property\");")]
		[LuaMethod("getproperty", "returns a string representation of the value of a property of the widget at the given handle")]
		public string GetProperty(int handle, string property)
		{
			try
			{
				var ptr = new IntPtr(handle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						return form.GetType().GetProperty(property).GetValue(form, null).ToString();
					}

					foreach (Control control in form.Controls)
					{
						if (control.Handle == ptr)
						{
							return control.GetType().GetProperty(property).GetValue(control, null).ToString();
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}

			return "";
		}

		[LuaMethodExample("local stforget = forms.gettext(332);")]
		[LuaMethod("gettext", "Returns the text property of a given form or control")]
		public string GetText(int handle)
		{
			try
			{
				var ptr = new IntPtr(handle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						return form.Text;
					}

					foreach (Control control in form.Controls)
					{
						if (control.Handle == ptr)
						{
							if (control is LuaDropDown)
							{
								return (control as LuaDropDown).SelectedItem.ToString();
							}

							return control.Text;
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}

			return "";
		}

		[LuaMethodExample("if ( forms.ischecked( 332 ) ) then\r\n\tconsole.log( \"Returns the given checkbox's checked property\" );\r\nend;")]
		[LuaMethod("ischecked", "Returns the given checkbox's checked property")]
		public bool IsChecked(int handle)
		{
			var ptr = new IntPtr(handle);
			foreach (var form in _luaForms)
			{
				if (form.Handle == ptr)
				{
					return false;
				}

				foreach (Control control in form.Controls)
				{
					if (control.Handle == ptr)
					{
						if (control is LuaCheckbox)
						{
							return (control as LuaCheckbox).Checked;
						}

						return false;
					}
				}
			}

			return false;
		}

		[LuaMethodExample("local inforlab = forms.label( 333, \"Caption\", 2, 48, 18, 24, false );")]
		[LuaMethod(
			"label", "Creates a label control on the given form. The caption property is the text of the label. x, and y are the optional location parameters for the position of the label within the given form. The function returns the handle of the created label. Width and Height are optional, if not specified they will be a default size.")]
		public int Label(
			int formHandle,
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

			if (x.HasValue && y.HasValue)
			{
				SetLocation(label, x.Value, y.Value);
			}

			if (width.HasValue && height.HasValue)
			{
				SetSize(label, width.Value, height.Value);
			}

			return (int)label.Handle;
		}

		[LuaMethodExample("local infornew = forms.newform( 18, 24, \"Title\", function()\r\n\tconsole.log( \"creates a new default dialog, if both width and height are specified it will create a dialog of the specified size. If title is specified it will be the caption of the dialog, else the dialog caption will be 'Lua Dialog'. The function will return an int representing the handle of the dialog created.\" );\r\nend );")]
		[LuaMethod(
			"newform", "creates a new default dialog, if both width and height are specified it will create a dialog of the specified size. If title is specified it will be the caption of the dialog, else the dialog caption will be 'Lua Dialog'. The function will return an int representing the handle of the dialog created.")]
		public int NewForm(int? width = null, int? height = null, string title = null, LuaFunction onClose = null)
		{
			var form = new LuaWinform(CurrentThread);
			_luaForms.Add(form);
			if (width.HasValue && height.HasValue)
			{
				form.Size = new Size(width.Value, height.Value);
			}

			form.Text = title;
			form.MaximizeBox = false;
			form.FormBorderStyle = FormBorderStyle.FixedDialog;
			form.Icon = SystemIcons.Application;
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

			return (int)form.Handle;
		}

		[LuaMethodExample("local stforope = forms.openfile( \"C:\\filename.bin\", \"C:\\\", \"All files ( *.* )|*.*\");")]
		[LuaMethod(
			"openfile", "Creates a standard openfile dialog with optional parameters for the filename, directory, and filter. The return value is the directory that the user picked. If they chose to cancel, it will return an empty string")]
		public string OpenFile(string fileName = null, string initialDirectory = null, string filter = "All files (*.*)|*.*")
		{
			// filterext format ex: "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*"
			var openFileDialog1 = new OpenFileDialog();
			if (initialDirectory != null)
			{
				openFileDialog1.InitialDirectory = initialDirectory;
			}

			if (fileName != null)
			{
				openFileDialog1.FileName = fileName;
			}

			if (filter != null)
			{
				openFileDialog1.AddExtension = true;
				openFileDialog1.Filter = filter;
			}

			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				return openFileDialog1.FileName;
			}

			return "";
		}

		[LuaMethodExample("local inforpic = forms.pictureBox( 333, 2, 48, 18, 24 );")]
		[LuaMethod(
			"pictureBox",
			"Creates a new drawing area in the form. Optionally the location in the form as well as the size of the drawing area can be specified. Returns the handle the component can be refered to with.")]
		public int PictureBox(int formHandle, int? x = null, int? y = null, int? width = null, int? height = null)
		{
			var form = GetForm(formHandle);
			if (form == null)
			{
				return 0;
			}

			var pictureBox = new LuaPictureBox();
			form.Controls.Add(pictureBox);

			if (x.HasValue && y.HasValue)
			{
				SetLocation(pictureBox, x.Value, y.Value);
			}

			if (width.HasValue && height.HasValue)
			{
				pictureBox.LuaResize(width.Value, height.Value);
			}

			SetSize(pictureBox, width.Value, height.Value);

			return (int)pictureBox.Handle;
		}

		#region LuaPictureBox Methods

		[LuaMethodExample("forms.clear( 334, 0x000000FF );")]
		[LuaMethod(
			"clear",
			"Clears the canvas")]
		public void Clear(int componentHandle, Color color)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).Clear(color);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.refresh( 334 );")]
		[LuaMethod(
			"refresh",
			"Redraws the canvas")]
		public void Refresh(int componentHandle)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).Refresh();
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.setDefaultForegroundColor( 334, 0xFFFFFFFF );")]
		[LuaMethod(
			"setDefaultForegroundColor",
			"Sets the default foreground color to use in drawing methods, white by default")]
		public void SetDefaultForegroundColor(int componentHandle, Color color)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).SetDefaultForegroundColor(color);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.setDefaultBackgroundColor( 334, 0x000000FF );")]
		[LuaMethod(
			"setDefaultBackgroundColor",
			"Sets the default background color to use in drawing methods, transparent by default")]
		public void SetDefaultBackgroundColor(int componentHandle, Color color)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).SetDefaultBackgroundColor(color);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.setDefaultTextBackground( 334, 0x000000FF );")]
		[LuaMethod(
			"setDefaultTextBackground",
			"Sets the default backgroiund color to use in text drawing methods, half-transparent black by default")]
		public void SetDefaultTextBackground(int componentHandle, Color color)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).SetDefaultTextBackground(color);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawBezier( 334, { { 5, 10 }, { 10, 10 }, { 10, 20 }, { 5, 20 } }, 0x000000FF );")]
		[LuaMethod(
			"drawBezier",
			"Draws a Bezier curve using the table of coordinates provided in the given color")]
		public void DrawBezier(int componentHandle, LuaTable points, Color color)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).DrawBezier(points, color);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawBox( 334, 16, 32, 162, 322, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"drawBox",
			"Draws a rectangle on screen from x1/y1 to x2/y2. Same as drawRectangle except it receives two points intead of a point and width/height")]
		public void DrawBox(int componentHandle, int x, int y, int x2, int y2, Color? line = null, Color? background = null)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).DrawBox(x, y, x2, y2, line, background);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawEllipse( 334, 16, 32, 77, 99, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"drawEllipse",
			"Draws an ellipse at the given coordinates and the given width and height. Line is the color of the ellipse. Background is the optional fill color")]
		public void DrawEllipse(int componentHandle, int x, int y, int width, int height, Color? line = null, Color? background = null)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).DrawEllipse(x, y, width, height, line, background);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawIcon( 334, \"C:\\icon.ico\", 16, 32, 18, 24 );")]
		[LuaMethod(
			"drawIcon",
			"draws an Icon (.ico) file from the given path at the given coordinate. width and height are optional. If specified, it will resize the image accordingly")]
		public void DrawIcon(int componentHandle, string path, int x, int y, int? width = null, int? height = null)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).DrawIcon(path, x, y, width, height);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawImage( 334, \"C:\\image.png\", 16, 32, 18, 24, false );")]
		[LuaMethod(
			"drawImage",
			"draws an image file from the given path at the given coordinate. width and height are optional. If specified, it will resize the image accordingly")]
		public void DrawImage(int componentHandle, string path, int x, int y, int? width = null, int? height = null, bool cache = true)
		{
			if (!File.Exists(path))
			{
				ConsoleLuaLibrary.Log($"File not found: {path}\nScript Terminated");
				return;
			}
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).DrawImage(path, x, y, width, height, cache);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.clearImageCache( 334 );")]
		[LuaMethod(
			"clearImageCache",
			"clears the image cache that is built up by using gui.drawImage, also releases the file handle for cached images")]
		public void ClearImageCache(int componentHandle)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).ClearImageCache();
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawImageRegion( 334, \"C:\\image.bmp\", 11, 22, 33, 44, 21, 43, 34, 45 );")]
		[LuaMethod(
			"drawImageRegion",
			"draws a given region of an image file from the given path at the given coordinate, and optionally with the given size")]
		public void DrawImageRegion(int componentHandle, string path, int source_x, int source_y, int source_width, int source_height, int dest_x, int dest_y, int? dest_width = null, int? dest_height = null)
		{
			if (!File.Exists(path))
			{
				ConsoleLuaLibrary.Log($"File not found: {path}\nScript Terminated");
				return;
			}
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).DrawImageRegion(path, source_x, source_y, source_width, source_height, dest_x, dest_y, dest_width, dest_height);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawLine( 334, 161, 321, 162, 322, 0xFFFFFFFF );")]
		[LuaMethod(
			"drawLine",
			"Draws a line from the first coordinate pair to the 2nd. Color is optional (if not specified it will be drawn black)")]
		public void DrawLine(int componentHandle, int x1, int y1, int x2, int y2, Color? color = null)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).DrawLine(x1, y1, x2, y2, color);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawAxis( 334, 16, 32, int size, 0xFFFFFFFF );")]
		[LuaMethod(
			"drawAxis",
			"Draws an axis of the specified size at the coordinate pair.)")]
		public void DrawAxis(int componentHandle, int x, int y, int size, Color? color = null)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).DrawAxis(x, y, size, color);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawArc( 334, 16, 32, 77, 99, 180, 90, 0x007F00FF );")]
		[LuaMethod(
			"drawArc",
			"draws a Arc shape at the given coordinates and the given width and height"
		)]
		public void DrawArc(int componentHandle, int x, int y, int width, int height, int startangle, int sweepangle, Color? line = null)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).DrawArc(x, y, width, height, startangle, sweepangle, line);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawPie( 334, 16, 32, 77, 99, 180, 90, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"drawPie",
			"draws a Pie shape at the given coordinates and the given width and height")]
		public void DrawPie(
			int componentHandle,
			int x,
			int y,
			int width,
			int height,
			int startangle,
			int sweepangle,
			Color? line = null,
			Color? background = null)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).DrawPie(x, y, width, height, startangle, sweepangle, line, background);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawPixel( 334, 16, 32, 0xFFFFFFFF );")]
		[LuaMethod(
			"drawPixel",
			"Draws a single pixel at the given coordinates in the given color. Color is optional (if not specified it will be drawn black)")]
		public void DrawPixel(int componentHandle, int x, int y, Color? color = null)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).DrawPixel(x, y, color);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawPolygon( 334, { { 5, 10 }, { 10, 10 }, { 10, 20 }, { 5, 20 } }, 10, 30, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"drawPolygon",
			"Draws a polygon using the table of coordinates specified in points. This should be a table of tables(each of size 2). If x or y is passed, the polygon will be translated by the passed coordinate pair. Line is the color of the polygon. Background is the optional fill color")]
		public void DrawPolygon(int componentHandle, LuaTable points, int? x = null, int? y = null, Color? line = null, Color? background = null)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).DrawPolygon(points, x, y, line, background);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}


		[LuaMethodExample("forms.drawRectangle( 334, 16, 32, 77, 99, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"drawRectangle",
			"Draws a rectangle at the given coordinate and the given width and height. Line is the color of the box. Background is the optional fill color")]
		public void DrawRectangle(int componentHandle, int x, int y, int width, int height, Color? line = null, Color? background = null)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).DrawRectangle(x, y, width, height, line, background);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawString( 334, 16, 32, \"Some message\", 0x7F0000FF, 0x00007FFF, 8, \"Arial Narrow\", \"bold\", \"center\", \"middle\" );")]
		[LuaMethod(
			"drawString",
			"Alias of DrawText()")]
		public void DrawString(
			int componentHandle,
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
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).DrawText(x, y, message, forecolor, backcolor, fontsize, fontfamily, fontstyle, horizalign, vertalign);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.drawText( 334, 16, 32, \"Some message\", 0x7F0000FF, 0x00007FFF, 8, \"Arial Narrow\", \"bold\", \"center\", \"middle\" );")]
		[LuaMethod(
			"drawText",
			"Draws the given message at the given x,y coordinates and the given color. The default color is white. A fontfamily can be specified and is monospace generic if none is specified (font family options are the same as the .NET FontFamily class). The fontsize default is 12. The default font style is regular. Font style options are regular, bold, italic, strikethrough, underline. Horizontal alignment options are left (default), center, or right. Vertical alignment options are bottom (default), middle, or top. Alignment options specify which ends of the text will be drawn at the x and y coordinates.")]
		public void DrawText(
			int componentHandle,
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
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							(control as LuaPictureBox).DrawText(x, y, message, forecolor, backcolor, fontsize, fontfamily, fontstyle, horizalign, vertalign);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		// It'd be great if these were simplified into 1 function, but I cannot figure out how to return a LuaTable from this class
		[LuaMethodExample("local inforget = forms.getMouseX( 334 );")]
		[LuaMethod(
			"getMouseX",
			"Returns an integer representation of the mouse X coordinate relative to the PictureBox.")]
		public int GetMouseX(int componentHandle)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return 0;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							var position = (control as LuaPictureBox).GetMouse();
							return position.X;
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}

			return 0;
		}

		[LuaMethodExample("local inforget = forms.getMouseY( 334 );")]
		[LuaMethod(
			"getMouseY",
			"Returns an integer representation of the mouse Y coordinate relative to the PictureBox.")]
		public int GetMouseY(int componentHandle)
		{
			try
			{
				var ptr = new IntPtr(componentHandle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						ConsoleLuaLibrary.Log("Drawing functions cannot be used on forms directly. Use them on a PictureBox component.");
						return 0;
					}

					foreach (Control control in form.Controls)
					{
						if (control is LuaPictureBox)
						{
							var position = (control as LuaPictureBox).GetMouse();
							return position.Y;
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}

			return 0;
		}

		#endregion

		[LuaMethodExample("forms.setdropdownitems( 332, { \"item1\", \"item2\" } );")]
		[LuaMethod("setdropdownitems", "Sets the items for a given dropdown box")]
		public void SetDropdownItems(int handle, LuaTable items)
		{
			try
			{
				var ptr = new IntPtr(handle);
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						return;
					}

					foreach (Control control in form.Controls)
					{
						if (control.Handle == ptr)
						{
							if (control is LuaDropDown)
							{
								var dropdownItems = items.Values.Cast<string>().ToList();
								dropdownItems.Sort();
								(control as LuaDropDown).SetItems(dropdownItems);
							}

							return;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log(ex.Message);
			}
		}

		[LuaMethodExample("forms.setlocation( 332, 16, 32 );")]
		[LuaMethod("setlocation", "Sets the location of a control or form by passing in the handle of the created object")]
		public void SetLocation(int handle, int x, int y)
		{
			var ptr = new IntPtr(handle);
			foreach (var form in _luaForms)
			{
				if (form.Handle == ptr)
				{
					SetLocation(form, x, y);
				}
				else
				{
					foreach (Control control in form.Controls)
					{
						if (control.Handle == ptr)
						{
							SetLocation(control, x, y);
						}
					}
				}
			}
		}

		[LuaMethodExample("forms.setproperty( 332, \"Property\", \"Property value\" );")]
		[LuaMethod("setproperty", "Attempts to set the given property of the widget with the given value.  Note: not all properties will be able to be represented for the control to accept")]
		public void SetProperty(int handle, string property, object value)
		{
			var ptr = new IntPtr(handle);
			foreach (var form in _luaForms)
			{
				if (form.Handle == ptr)
				{
					var pt = form.GetType().GetProperty(property).PropertyType;
					if (pt.IsEnum)
					{
						value = Enum.Parse(form.GetType().GetProperty(property).PropertyType, value.ToString(), true);
					}

					if (pt == typeof(Color))
					{
						// relying on exceptions for error handling here
						var sval = (string)value;
						if (sval[0] != '#')
						{
							throw new Exception("Invalid #aarrggbb color");
						}

						if (sval.Length != 9)
						{
							throw new Exception("Invalid #aarrggbb color");
						}

						value = Color.FromArgb(int.Parse(sval.Substring(1), System.Globalization.NumberStyles.HexNumber));
					}

					form.GetType()
						.GetProperty(property)
						.SetValue(form, Convert.ChangeType(value, form.GetType().GetProperty(property).PropertyType), null);
				}
				else
				{
					foreach (Control control in form.Controls)
					{
						if (control.Handle == ptr)
						{
							if (control.GetType().GetProperty(property).PropertyType.IsEnum)
							{
								value = Enum.Parse(control.GetType().GetProperty(property).PropertyType, value.ToString(), true);
							}

							control.GetType()
								.GetProperty(property)
								.SetValue(control, Convert.ChangeType(value, control.GetType().GetProperty(property).PropertyType), null);
						}
					}
				}
			}
		}

		[LuaMethodExample("local coforcre = forms.createcolor( 0x7F, 0x3F, 0x1F, 0xCF );")]
		[LuaMethod("createcolor", "Creates a color object useful with setproperty")]
		public Color CreateColor(int r, int g, int b, int a)
		{
			return Color.FromArgb(a, r, g, b);
		}

		[LuaMethodExample("forms.setsize( 332, 77, 99 );")]
		[LuaMethod("setsize", "TODO")]
		public void SetSize(int handle, int width, int height)
		{
			var ptr = new IntPtr(handle);
			foreach (var form in _luaForms)
			{
				if (form.Handle == ptr)
				{
					SetSize(form, width, height);
				}
				else
				{
					foreach (Control control in form.Controls)
					{
						if (control.Handle == ptr)
						{
							SetSize(control, width, height);
						}
					}
				}
			}
		}

		[LuaMethodExample("forms.settext( 332, \"Caption\" );")]
		[LuaMethod("settext", "Sets the text property of a control or form by passing in the handle of the created object")]
		public void Settext(int handle, string caption)
		{
			var ptr = new IntPtr(handle);
			foreach (var form in _luaForms)
			{
				if (form.Handle == ptr)
				{
					SetText(form, caption);
				}
				else
				{
					foreach (Control control in form.Controls)
					{
						if (control.Handle == ptr)
						{
							SetText(control, caption);
						}
					}
				}
			}
		}

		[LuaMethodExample("local infortex = forms.textbox( 333, \"Caption\", 18, 24, \"HEX\", 2, 48, true, false, \"Both\" );")]
		[LuaMethod(
			"textbox", "Creates a textbox control on the given form. The caption property will be the initial value of the textbox (default is empty). Width and Height are option, if not specified they will be a default size of 100, 20. Type is an optional property to restrict the textbox input. The available options are HEX, SIGNED, and UNSIGNED. Passing it null or any other value will set it to no restriction. x, and y are the optional location parameters for the position of the textbox within the given form. The function returns the handle of the created textbox. If true, the multiline will enable the standard winform multi-line property. If true, the fixedWidth options will create a fixed width font. Scrollbars is an optional property to specify which scrollbars to display. The available options are Vertical, Horizontal, Both, and None. Scrollbars are only shown on a multiline textbox")]
		public int Textbox(
			int formHandle,
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
				switch (scrollbars.ToUpper())
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

			if (x.HasValue && y.HasValue)
			{
				SetLocation(textbox, x.Value, y.Value);
			}

			if (width.HasValue && height.HasValue)
			{
				SetSize(textbox, width.Value, height.Value);
			}

			if (boxtype != null)
			{
				switch (boxtype.ToUpper())
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
			return (int)textbox.Handle;
		}
	}
}
