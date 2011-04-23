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

        void UpdateLabel()
        {
            if (Bindings.Count == 0)
            {
                Text = "";
            }
            else
            {
                Text = Bindings[0].ToString();
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
            this.Parent.SelectNextControl(this, true, true, true, true);
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
            string str = "";
            if((modifiers & Keys.Shift)!=0)
                str += "LeftShift + ";
			if ((modifiers & Keys.Control) != 0)
                str += "LeftControl + ";
			if ((modifiers & Keys.Alt) != 0)
				str += "LeftAlt + ";
			str += key.ToString();
            if (str.Substring(0, 6) == "NumPad")
                str = str.Insert(3, "ber");
            if (str.Length > 3)
            {
                if (str.Substring(0, 3) == "Oem")
                    str = str.Substring(3, str.Length - 3);
            }
			return str;
		}
		public Keys key;
		public Keys modifiers;
	}

	public interface IBinding
	{

	}
}