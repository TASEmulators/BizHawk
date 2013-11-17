using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using LuaInterface;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class FormsLuaLibrary : LuaLibraryBase
	{
		public override string Name { get { return "forms"; } }
		public override string[] Functions
		{
			get
			{
				return new[]
				{
					"addclick",
					"button",
					"clearclicks",
					"destroy",
					"destroyall",
					"getproperty",
					"gettext",
					"label",
					"newform",
					"openfile",
					"setlocation",
					"setproperty",
					"setsize",
					"settext",
					"textbox"
				};
			}
		}

		#region Forms Library Helpers

		private readonly List<LuaWinform> _luaForms = new List<LuaWinform>();

		public void WindowClosed(IntPtr handle)
		{
			foreach (LuaWinform form in _luaForms)
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
			IntPtr ptr = new IntPtr(LuaInt(formHandle));
			return _luaForms.FirstOrDefault(form => form.Handle == ptr);
		}

		private void SetLocation(Control control, object X, object Y)
		{
			try
			{
				if (X != null && Y != null)
				{
					control.Location = new Point(LuaInt(X), LuaInt(Y));
				}
			}
			catch { /*Do nothing*/ }
		}

		private void SetSize(Control control, object Width, object Height)
		{
			try
			{
				if (Width != null && Height != null)
				{
					control.Size = new Size(LuaInt(Width), LuaInt(Height));
				}
			}
			catch { /*Do nothing*/ }
		}

		private void SetText(Control control, object caption)
		{
			if (caption != null)
			{
				control.Text = caption.ToString();
			}
		}

		#endregion

		public void forms_addclick(object handle, LuaFunction lua_event)
		{
			IntPtr ptr = new IntPtr(LuaInt(handle));
			foreach (LuaWinform form in _luaForms)
			{
				foreach (Control control in form.Controls)
				{
					if (control.Handle == ptr)
					{
						form.ControlEvents.Add(new LuaWinform.LuaEvent(control.Handle, lua_event));
					}
				}
			}
		}

		public int forms_button(object form_handle, object caption, LuaFunction lua_event, object X = null, object Y = null, object width = null, object height = null)
		{
			LuaWinform form = GetForm(form_handle);
			if (form == null)
			{
				return 0;
			}

			LuaButton button = new LuaButton();
			SetText(button, caption);
			form.Controls.Add(button);
			form.ControlEvents.Add(new LuaWinform.LuaEvent(button.Handle, lua_event));

			SetLocation(button, X, Y);
			SetSize(button, width, height);

			return (int)button.Handle;
		}

		public void forms_clearclicks(object handle)
		{
			IntPtr ptr = new IntPtr(LuaInt(handle));
			foreach (LuaWinform form in _luaForms)
			{
				foreach (Control control in form.Controls)
				{
					if (control.Handle == ptr)
					{
						List<LuaWinform.LuaEvent> lua_events = form.ControlEvents.Where(x => x.Control == ptr).ToList();
						foreach (LuaWinform.LuaEvent levent in lua_events)
						{
							form.ControlEvents.Remove(levent);
						}
					}
				}
			}
		}

		public bool forms_destroy(object handle)
		{
			IntPtr ptr = new IntPtr(LuaInt(handle));
			foreach (LuaWinform form in _luaForms)
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

		public void forms_destroyall()
		{
			foreach (LuaWinform form in _luaForms)
			{
				form.Close();
				_luaForms.Remove(form);
			}
		}

		public string forms_getproperty(object handle, object property)
		{
			try
			{
				IntPtr ptr = new IntPtr(LuaInt(handle));
				foreach (LuaWinform form in _luaForms)
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
				ConsoleLuaLibrary.console_output(ex.Message);
			}

			return String.Empty;
		}

		public string forms_gettext(object handle)
		{
			try
			{
				IntPtr ptr = new IntPtr(LuaInt(handle));
				foreach (LuaWinform form in _luaForms)
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
								return control.Text;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLuaLibrary.console_output(ex.Message);
			}

			return String.Empty;
		}

		public int forms_label(object form_handle, object caption, object X = null, object Y = null, object width = null, object height = null)
		{
			LuaWinform form = GetForm(form_handle);
			if (form == null)
			{
				return 0;
			}

			Label label = new Label();
			SetText(label, caption);
			form.Controls.Add(label);
			SetLocation(label, X, Y);
			SetSize(label, width, height);

			return (int)label.Handle;
		}

		public int forms_newform(object Width = null, object Height = null, object title = null)
		{

			LuaWinform theForm = new LuaWinform();
			_luaForms.Add(theForm);
			if (Width != null && Height != null)
			{
				theForm.Size = new Size(LuaInt(Width), LuaInt(Height));
			}

			theForm.Text = (string)title;
			theForm.Show();
			return (int)theForm.Handle;
		}

		public string forms_openfile(string FileName = null, string InitialDirectory = null, string Filter = "All files (*.*)|*.*")
		{
			// filterext format ex: "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*"
			OpenFileDialog openFileDialog1 = new OpenFileDialog();
			if (InitialDirectory != null)
			{
				openFileDialog1.InitialDirectory = InitialDirectory;
			}
			
			if (FileName != null)
			{
				openFileDialog1.FileName = FileName;
			}
			
			if (Filter != null)
			{
				openFileDialog1.AddExtension = true;
				openFileDialog1.Filter = Filter;
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

		public void forms_setlocation(object handle, object X, object Y)
		{
			IntPtr ptr = new IntPtr(LuaInt(handle));
			foreach (LuaWinform form in _luaForms)
			{
				if (form.Handle == ptr)
				{
					SetLocation(form, X, Y);
				}
				else
				{
					foreach (Control control in form.Controls)
					{
						if (control.Handle == ptr)
						{
							SetLocation(control, X, Y);
						}
					}
				}
			}
		}

		public void forms_setproperty(object handle, object property, object value)
		{
			IntPtr ptr = new IntPtr(LuaInt(handle));
			foreach (LuaWinform form in _luaForms)
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

		public void forms_setsize(object handle, object Width, object Height)
		{
			IntPtr ptr = new IntPtr(LuaInt(handle));
			foreach (LuaWinform form in _luaForms)
			{
				if (form.Handle == ptr)
				{
					SetSize(form, Width, Height);
				}
				else
				{
					foreach (Control control in form.Controls)
					{
						if (control.Handle == ptr)
						{
							SetSize(control, Width, Height);
						}
					}
				}
			}
		}

		public void forms_settext(object handle, object caption)
		{
			IntPtr ptr = new IntPtr(LuaInt(handle));
			foreach (LuaWinform form in _luaForms)
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

		public int forms_textbox(object form_handle, object caption = null, object width = null, object height = null, object boxtype = null, object X = null, object Y = null)
		{
			LuaWinform form = GetForm(form_handle);
			if (form == null)
			{
				return 0;
			}

			LuaTextBox textbox = new LuaTextBox();
			SetText(textbox, caption);
			SetLocation(textbox, X, Y);
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
