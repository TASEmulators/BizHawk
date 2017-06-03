using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using LuaInterface;

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

		[LuaMethodAttributes("addclick", "adds the given lua function as a click event to the given control")]
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

		[LuaMethodAttributes(
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

		[LuaMethodAttributes(
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

		[LuaMethodAttributes("clearclicks", "Removes all click events from the given widget at the specified handle")]
		public void ClearClicks(int handle)
		{
			var ptr = new IntPtr(handle);
			foreach (var form in _luaForms)
			{
				foreach (Control control in form.Controls)
				{
					if (control.Handle == ptr)
					{
						var luaEvents = form.ControlEvents.Where(x => x.Control == ptr).ToList();
						foreach (var luaEvent in luaEvents)
						{
							form.ControlEvents.Remove(luaEvent);
						}
					}
				}
			}
		}

		[LuaMethodAttributes("destroy", "Closes and removes a Lua created form with the specified handle. If a dialog was found and removed true is returned, else false")]
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

		[LuaMethodAttributes("destroyall", "Closes and removes all Lua created dialogs")]
		public void DestroyAll()
		{
			for (var i = _luaForms.Count - 1; i >= 0; i--)
			{
				_luaForms.ElementAt(i).Close();
			}
		}

		[LuaMethodAttributes(
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

		[LuaMethodAttributes("getproperty", "returns a string representation of the value of a property of the widget at the given handle")]
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

		[LuaMethodAttributes("gettext", "Returns the text property of a given form or control")]
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

		[LuaMethodAttributes("ischecked", "Returns the given checkbox's checked property")]
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

		[LuaMethodAttributes(
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

		[LuaMethodAttributes(
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

		[LuaMethodAttributes(
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

		[LuaMethodAttributes("setdropdownitems", "Sets the items for a given dropdown box")]
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

		[LuaMethodAttributes("setlocation", "Sets the location of a control or form by passing in the handle of the created object")]
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

		[LuaMethodAttributes("setproperty", "Attempts to set the given property of the widget with the given value.  Note: not all properties will be able to be represented for the control to accept")]
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

						value = Color.FromArgb(int.Parse(sval.Substring(1),System.Globalization.NumberStyles.HexNumber));
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

		[LuaMethodAttributes("createcolor", "Creates a color object useful with setproperty")]
		public Color CreateColor(int r, int g, int b, int a)
		{
			return Color.FromArgb(a, r, g, b);
		}

		[LuaMethodAttributes("setsize", "TODO")]
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

		[LuaMethodAttributes("settext", "Sets the text property of a control or form by passing in the handle of the created object")]
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

		[LuaMethodAttributes(
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
