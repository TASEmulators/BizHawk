using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public class InputWidget : TextBox
	{
		public InputWidget()
		{
			this.ContextMenu = new ContextMenu();
		}

		public List<IBinding> Bindings = new List<IBinding>();
		public bool AutoTab = true;
		bool ctrlWasPressed = false;
		bool altWasPressed = false;
		bool shiftWasPressed = false;

		void UpdateLabel()
		{
			if (Bindings.Count == 0)
			{
				Text = "";
			}
			else
			{
				Text = "";
				for (int x = 0; x < Bindings.Count; x++)
					Text += Bindings[x].ToString();
			}
			Update();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			e.Handled = true;
			if (e.KeyCode == Keys.ControlKey) return;
			if (e.KeyCode == Keys.ShiftKey) return;
			if (e.KeyCode == Keys.Menu) return;
			if (e.KeyCode != Keys.Escape)
			{
				KeyboardBinding kb = new KeyboardBinding();
				kb.key = e.KeyCode;
				kb.modifiers = e.Modifiers;
				Bindings.Clear();
				Bindings.Add(kb);
				UpdateLabel();
			}
			else
			{
				Bindings.Clear();
				UpdateLabel();
			}

			if (e.KeyCode == Keys.F4)
				e.Handled = false;

			if (AutoTab)
				this.Parent.SelectNextControl(this, true, true, true, true);
			
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (e.KeyData == Keys.ControlKey)
			{
				if (ctrlWasPressed)
				{
					KeyboardBinding kb = new KeyboardBinding();
					kb.key = Keys.ControlKey;
					Bindings.Clear();
					Bindings.Add(kb);
					UpdateLabel();
					this.Parent.SelectNextControl(this, true, true, true, true);
					ctrlWasPressed = false;
					shiftWasPressed = false;
					altWasPressed = false;
				}
				else
				{
					ctrlWasPressed = true;
					shiftWasPressed = false;
					altWasPressed = false;
					BackColor = SystemColors.ControlLight;
				}
			}
			else if (e.KeyData == Keys.ShiftKey)
			{
				if (shiftWasPressed)
				{
					KeyboardBinding kb = new KeyboardBinding();
					kb.key = Keys.ShiftKey;
					Bindings.Clear();
					Bindings.Add(kb);
					UpdateLabel();
					this.Parent.SelectNextControl(this, true, true, true, true);
					ctrlWasPressed = false;
					shiftWasPressed = false;
					altWasPressed = false;
				}
				else
				{
					shiftWasPressed = true;
					altWasPressed = false;
					ctrlWasPressed = false;
					BackColor = SystemColors.ControlLight;
				}
			}
			else if (e.KeyData == Keys.Menu)
			{
				if (altWasPressed)
				{
					KeyboardBinding kb = new KeyboardBinding();
					kb.key = Keys.Menu;
					Bindings.Clear();
					Bindings.Add(kb);
					UpdateLabel();
					this.Parent.SelectNextControl(this, true, true, true, true);
					ctrlWasPressed = false;
					shiftWasPressed = false;
					altWasPressed = false;
				}
				else
				{
					altWasPressed = true;
					ctrlWasPressed = false;
					shiftWasPressed = false;
					BackColor = SystemColors.ControlLight;
				}
			}

			base.OnKeyUp(e);
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			e.Handled = true;
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			BackColor = Color.Pink;
		}

		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			BackColor = SystemColors.Window;
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Tab)
			{
				KeyboardBinding kb = new KeyboardBinding();
				kb.key = keyData;
				Bindings.Clear();
				Bindings.Add(kb);
				UpdateLabel();
			}
			return false;
		}
	}

	public class KeyboardBinding : IBinding
	{
		public override string ToString()
		{
			// Convert Windows key names to SlimDX key names
			string str = "";
			
			str += key.ToString();
			if (str.Length == 2 && str == "F4") //Don't allow mapping of alt+f4
				if ((modifiers & Keys.Alt) != 0)
					return "";
			if (str.Length == 10 && str == "ControlKey")
				str = "Ctrl";
			if (str.Length == 4 && str == "Menu")
				str = "Alt";
			if (str.Length == 8 && str == "ShiftKey")
				str = "Shift";
			if (str.Length == 2 && str == "Up")
				str = "UpArrow";
			if (str.Length == 4 && str == "Down")
				str = "DownArrow";
			if (str.Length == 4 && str == "Left")
				str = "LeftArrow";
			if (str.Length == 5 && str == "Right")
				str = "RightArrow";
			if (str.Length >= 6 && str.Substring(0, 6) == "NumPad")
				str = str.Insert(3, "ber");
			if (str.Length == 7 && str.Substring(0, 7) == "Decimal")
				str = "NumberPadPeriod";
			if (str.Length == 6 && str.Substring(0, 6) == "Divide")
				str = "NumberPadSlash";
			if (str.Length == 8 && str.Substring(0, 8) == "Multiply")
				str = "NumberPadStar";
			if (str.Length == 8 && str.Substring(0, 8) == "Subtract")
				str = "NumberPadMinus";
			if (str.Length == 3 && str.Substring(0, 3) == "Add")
				str = "NumberPadPlus";
			if (str.Length == 4 && str == "Oem5")
				str = "BackSlash";
			if (str.Length == 4 && str == "Oem6")
				str = "RightBracket";
			if (str.Length == 4 && str == "Next")
				str = "PageDown";
			if (str.Length == 11 && str == "OemQuestion")
				str = "Slash";
			if (str.Length == 8 && str == "Oemtilde")
				str = "Grave";
			if (str.Length == 8 && str == "Oemcomma")
				str = "Comma";
			if (str.Length > 3)
			{
				if (str.Substring(0, 3) == "Oem")
					str = str.Substring(3, str.Length - 3);
			}
			//Oem Removed now removed from these but they still need conversion
			if (str.Length == 12 && str.Substring(0, 12) == "OpenBrackets")
				str = "LeftBracket";

			if ((modifiers & Keys.Shift) != 0)
				str = str.Insert(0, "Shift+");
			if ((modifiers & Keys.Control) != 0)
				str = str.Insert(0, "Ctrl+");
			if ((modifiers & Keys.Alt) != 0)
				str = str.Insert(0, "Alt+");

			return str;
		}
		public Keys key;
		public Keys modifiers;
	}

	public interface IBinding
	{

	}
}