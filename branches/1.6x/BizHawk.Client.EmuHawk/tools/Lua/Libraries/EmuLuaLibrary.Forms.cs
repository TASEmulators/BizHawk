using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using LuaInterface;

namespace BizHawk.Client.EmuHawk
{
	public class FormsLuaLibrary : LuaLibraryBase
	{
		// TODO: replace references to ConsoleLuaLibrary.Log with a callback that is passed in
		public override string Name { get { return "forms"; } }

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

		private LuaWinform GetForm(object formHandle)
		{
			var ptr = new IntPtr(LuaInt(formHandle));
			return _luaForms.FirstOrDefault(form => form.Handle == ptr);
		}

		private static void SetLocation(Control control, object x, object y)
		{
			try
			{
				if (x != null && y != null)
				{
					control.Location = new Point(LuaInt(x), LuaInt(y));
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		private static void SetSize(Control control, object width, object height)
		{
			try
			{
				if (width != null && height != null)
				{
					control.Size = new Size(LuaInt(width), LuaInt(height));
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}
		}

		private static void SetText(Control control, object caption)
		{
			if (caption != null)
			{
				control.Text = caption.ToString();
			}
		}

		#endregion

		[LuaMethodAttributes(
			"addclick",
			"TODO"
		)]
		public void AddClick(object handle, LuaFunction clickEvent)
		{
			var ptr = new IntPtr(LuaInt(handle));
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
			"button",
			"TODO"
		)]
		public int Button(
			object formHandle,
			object caption,
			LuaFunction clickEvent,
			object x = null,
			object y = null,
			object width = null,
			object height = null)
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

			SetLocation(button, x, y);
			SetSize(button, width, height);

			return (int)button.Handle;
		}

		[LuaMethodAttributes(
			"checkbox",
			"TODO"
		)]
		public int Checkbox(object formHandle, string caption, object x = null, object y = null)
		{
			var form = GetForm(formHandle);
			if (form == null)
			{
				return 0;
			}

			var checkbox = new LuaCheckbox();
			form.Controls.Add(checkbox);
			SetText(checkbox, caption);
			SetLocation(checkbox, x, y);

			return (int)checkbox.Handle;
		}

		[LuaMethodAttributes(
			"clearclicks",
			"TODO"
		)]
		public void ClearClicks(object handle)
		{
			var ptr = new IntPtr(LuaInt(handle));
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

		[LuaMethodAttributes(
			"destroy",
			"TODO"
		)]
		public bool Destroy(object handle)
		{
			var ptr = new IntPtr(LuaInt(handle));
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

		[LuaMethodAttributes(
			"destroyall",
			"TODO"
		)]
		public void DestroyAll()
		{
			foreach (var form in _luaForms)
			{
				form.Close();
				_luaForms.Remove(form);
			}
		}

		[LuaMethodAttributes(
			"dropdown",
			"TODO"
		)]
		public int Dropdown(
			object formHandle,
			LuaTable items,
			object x = null,
			object y = null,
			object width = null,
			object height = null)
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
			SetLocation(dropdown, x, y);
			SetSize(dropdown, width, height);
			return (int)dropdown.Handle;
		}

		[LuaMethodAttributes(
			"getproperty",
			"TODO"
		)]
		public string GetProperty(object handle, object property)
		{
			try
			{
				var ptr = new IntPtr(LuaInt(handle));
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						return form.GetType().GetProperty(property.ToString()).GetValue(form, null).ToString();
					}
					else
					{
						foreach (Control control in form.Controls)
						{
							if (control.Handle == ptr)
							{
								return control.GetType().GetProperty(property.ToString()).GetValue(control, null).ToString();
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}

			return String.Empty;
		}

		[LuaMethodAttributes(
			"gettext",
			"TODO"
		)]
		public string GetText(object handle)
		{
			try
			{
				var ptr = new IntPtr(LuaInt(handle));
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						return form.Text;
					}
					else
					{
						foreach (Control control in form.Controls)
						{
							if (control.Handle == ptr)
							{
								if (control is LuaDropDown)
								{
									return (control as LuaDropDown).SelectedItem.ToString();
								}
								else
								{
									return control.Text;
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}

			return String.Empty;
		}

		[LuaMethodAttributes(
			"ischecked",
			"TODO"
		)]
		public bool IsChecked(object handle)
		{
			try
			{
				var ptr = new IntPtr(LuaInt(handle));
				foreach (var form in _luaForms)
				{
					if (form.Handle == ptr)
					{
						return false;
					}
					else
					{
						foreach (Control control in form.Controls)
						{
							if (control.Handle == ptr)
							{
								if (control is LuaCheckbox)
								{
									return (control as LuaCheckbox).Checked;
								}
								else
								{
									return false;
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.Log(ex.Message);
			}

			return false;
		}

		[LuaMethodAttributes(
			"label",
			"TODO"
		)]
		public int Label(
			object formHandle,
			object caption,
			object x = null,
			object y = null,
			object width = null,
			object height = null)
		{
			var form = GetForm(formHandle);
			if (form == null)
			{
				return 0;
			}

			var label = new Label();
			SetText(label, caption);
			form.Controls.Add(label);
			SetLocation(label, x, y);
			SetSize(label, width, height);

			return (int)label.Handle;
		}

		[LuaMethodAttributes(
			"newform",
			"TODO"
		)]
		public int NewForm(object width = null, object height = null, string title = null)
		{
			var form = new LuaWinform();
			_luaForms.Add(form);
			if (width != null && height != null)
			{
				form.Size = new Size(LuaInt(width), LuaInt(height));
			}

			form.Text = title;
			form.Show();
			return (int)form.Handle;
		}

		[LuaMethodAttributes(
			"openfile",
			"TODO"
		)]
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
			else
			{
				return String.Empty;
			}
		}

		[LuaMethodAttributes(
			"setlocation",
			"TODO"
		)]
		public void SetLocation(object handle, object x, object y)
		{
			var ptr = new IntPtr(LuaInt(handle));
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

		[LuaMethodAttributes(
			"setproperty",
			"TODO"
		)]
		public void SetProperty(object handle, object property, object value)
		{
			var ptr = new IntPtr(LuaInt(handle));
			foreach (var form in _luaForms)
			{
				if (form.Handle == ptr)
				{
					form.GetType().GetProperty(property.ToString()).SetValue(form, Convert.ChangeType(value, form.GetType().GetProperty(property.ToString()).PropertyType), null);
				}
				else
				{
					foreach (Control control in form.Controls)
					{
						if (control.Handle == ptr)
						{
							control.GetType().GetProperty(property.ToString()).SetValue(control, Convert.ChangeType(value, form.GetType().GetProperty(property.ToString()).PropertyType), null);
						}
					}
				}
			}
		}

		[LuaMethodAttributes(
			"setsize",
			"TODO"
		)]
		public void SetSize(object handle, object width, object height)
		{
			var ptr = new IntPtr(LuaInt(handle));
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

		[LuaMethodAttributes(
			"settext",
			"TODO"
		)]
		public void Settext(object handle, object caption)
		{
			var ptr = new IntPtr(LuaInt(handle));
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
			"textbox",
			"TODO"
		)]
		public int Textbox(
			object formHandle,
			object caption = null,
			object width = null,
			object height = null,
			object boxtype = null,
			object x = null,
			object y = null,
			bool multiline = false,
			bool fixedWidth = false)
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
			SetText(textbox, caption);
			SetLocation(textbox, x, y);
			SetSize(textbox, width, height);

			if (boxtype != null)
			{
				switch (boxtype.ToString().ToUpper())
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
