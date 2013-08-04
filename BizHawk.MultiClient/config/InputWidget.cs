using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;

namespace BizHawk.MultiClient
{
	public class InputWidget : TextBox
	{
		//TODO: when binding, make sure that the new key combo is not in one of the other bindings

		private int MaxBind = 4; //Max number of bindings allowed
		private int pos = 0;	 //Which mapping the widget will listen for
		private Timer timer = new Timer();
		private string[] _bindings = new string[4];
		private string wasPressed = "";
		private ToolTip tooltip1 = new ToolTip();
		private Color _highlight_color = Color.LightCyan;
		private Color _no_highlight_color = SystemColors.Window;
		private bool conflicted = false;

		public bool AutoTab = true;
		public string WidgetName;

		public bool Conflicted
		{
			get
			{
				return conflicted;
			}
			set
			{
				conflicted = value;
				if (conflicted)
				{
					_no_highlight_color = Color.LightCoral;
					_highlight_color = Color.Violet;
				}
				else
				{
					_highlight_color = Color.LightCyan;
					_no_highlight_color = SystemColors.Window;
				}

				if (Focused)
				{
					Highlight();
				}
				else
				{
					UnHighlight();
				}
			}
		}

		private void Highlight()
		{
			BackColor = _highlight_color;
		}

		private void UnHighlight()
		{
			BackColor = _no_highlight_color;
		}

		[DllImport("user32")]
		private static extern bool HideCaret(IntPtr hWnd);

		public InputWidget()
		{
			this.ContextMenu = new ContextMenu();
			this.timer.Tick += new System.EventHandler(this.Timer_Tick);
			_clearBindings();
			tooltip1.AutoPopDelay = 2000;
		}

		public InputWidget(int maxBindings, bool autotab)
		{
			this.AutoTab = autotab;
			this.ContextMenu = new ContextMenu();
			this.timer.Tick += new System.EventHandler(this.Timer_Tick);
			MaxBind = maxBindings;
			_bindings = new string[MaxBind];
			_clearBindings();
			tooltip1.AutoPopDelay = 2000;
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			HideCaret(this.Handle);
			base.OnMouseClick(e);
		}

		private void _clearBindings()
		{
			for (int i = 0; i < MaxBind; i++)
			{
				_bindings[i] = "";
			}
		}

		protected override void OnEnter(EventArgs e)
		{
			pos = 0;
			timer.Start();
			//Input.Update();

			//zero: ??? what is this all about ???
			wasPressed = Input.Instance.GetNextBindEvent();
		}

		protected override void OnLeave(EventArgs e)
		{
			timer.Stop();
			UpdateLabel();
			base.OnLeave(e);
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			ReadKeys();
		}

		public void EraseMappings()
		{
			_clearBindings();
			Conflicted = false;
			Text = "";
		}

		private void ReadKeys()
		{
			Input.Instance.Update();
			string TempBindingStr = Input.Instance.GetNextBindEvent();
			if (wasPressed != "" && TempBindingStr == wasPressed)
			{
				return;
			}
			else if (TempBindingStr != null)
			{
				if (TempBindingStr == "Escape")
				{
					_clearBindings();
					Conflicted = false;
					Increment();
					return;
				}

				if (TempBindingStr == "Alt+F4")
					return;

				if (!IsDuplicate(TempBindingStr))
				{
					_bindings[pos] = TempBindingStr;
				}
				wasPressed = TempBindingStr;

				UpdateLabel();
				Increment();
			}
		}

		//Checks if the key is already mapped to this widget
		private bool IsDuplicate(string binding)
		{
			return _bindings.FirstOrDefault(x => x == binding) != null;
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.F4 && e.Modifiers == Keys.Alt)
			{
				base.OnKeyUp(e);
			}
			wasPressed = "";
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.F4 && e.Modifiers == Keys.Alt)
			{
				base.OnKeyDown(e);
				return;
			}
			e.Handled = true;
		}

		// Advances to the next widget or the next binding depending on the autotab setting
		public void Increment()
		{
			if (AutoTab)
				this.Parent.SelectNextControl(this, true, true, true, true);
			else
			{
				if (pos == MaxBind - 1)
					pos = 0;
				else
					pos++;
				UpdateLabel();
			}
		}

		public void Decrement()
		{
			if (AutoTab)
				this.Parent.SelectNextControl(this, false, true, true, true);
			else
			{
				if (pos == 0)
					pos = MaxBind - 1;
				else
					pos--;
			}
		}

		public void UpdateLabel()
		{
			Text = "";
			for (int x = 0; x < MaxBind; x++)
			{
				if (_bindings[x].Length > 0)
				{
					Text += _bindings[x];
					if (x < MaxBind - 1 && _bindings[x+1].Length > 0)
						Text += ", ";
				}
			}
		}

		public string Bindings
		{
			get
			{
				return Text;
			}
			set
			{
				Text = "";
				_clearBindings();
				string str = value.Trim();
				int x;
				for (int i = 0; i < MaxBind; i++)
				{
					str = str.Trim();
					x = str.IndexOf(',');
					if (x < 0)
					{
						_bindings[i] = str;
						str = "";
					}
					else
					{
						_bindings[i] = str.Substring(0, x);
						str = str.Substring(x + 1, str.Length - x - 1);
					}
				}

				UpdateLabel();
			}
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			e.Handled = true;
		}

		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case 0x0201: //WM_LBUTTONDOWN
				{
					this.Focus();
					return;
				}
				//case 0x0202://WM_LBUTTONUP
				//{
				//	return;
				//}
				case 0x0203://WM_LBUTTONDBLCLK
				{
					return;
				}
				case 0x0204://WM_RBUTTONDOWN
				{
					return;
				}
				case 0x0205://WM_RBUTTONUP
				{
					return;
				}
				case 0x0206://WM_RBUTTONDBLCLK
				{
					return;
				}
			}

			base.WndProc(ref m);
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if (e.Delta > 0)
				Decrement();
			else
				Increment();
			base.OnMouseWheel(e);
		}

		protected override void OnGotFocus(EventArgs e)
		{
			//base.OnGotFocus(e);
			HideCaret(this.Handle);
			Highlight();
		}

		protected override void OnLostFocus(EventArgs e)
		{
			UnHighlight();
			base.OnLostFocus(e);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData.ToString() == "F4" || keyData.ToString().Contains("Alt"))
				return false;
			else
				return true;
		}
	}
}