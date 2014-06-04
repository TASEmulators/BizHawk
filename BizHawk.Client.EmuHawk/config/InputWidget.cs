using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public sealed class InputWidget : TextBox
	{
		// TODO: when binding, make sure that the new key combo is not in one of the other bindings
		private readonly ToolTip _tooltip1 = new ToolTip();
		private readonly Timer _timer = new Timer();
		private readonly string[] _bindings = new string[4];
		private readonly int _maxBind = 4; // Max number of bindings allowed
	
		private int _pos;	 // Which mapping the widget will listen for
		private string _wasPressed = string.Empty;

		public InputCompositeWidget CompositeWidget;

		/// <summary>
		/// These bindings get ignored by the widget and can only be entered by SetBinding() via the contextmenu from the InputCompositeWidget
		/// </summary>
		public static readonly string[] SpecialBindings = new[] {
			"Escape",
			"WMouse L","WMouse M","WMouse R",
			"WMouse 1", "WMouse 2"
		};

		public InputWidget()
		{
			ContextMenu = new ContextMenu();
			_timer.Tick += Timer_Tick;
			ClearBindings();
			_tooltip1.AutoPopDelay = 2000;
			AutoTab = true;
		}

		public InputWidget(int maxBindings, bool autotab)
		{
			AutoTab = autotab;
			ContextMenu = new ContextMenu();
			_timer.Tick += Timer_Tick;
			_maxBind = maxBindings;
			_bindings = new string[_maxBind];
			ClearBindings();
			_tooltip1.AutoPopDelay = 2000;
		}

		public bool AutoTab { get; set; }
		public string WidgetName { get; set; }

		public string Bindings
		{
			get
			{
				return Text;
			}

			set
			{
				ClearBindings();
				var newBindings = value.Trim().Split(',');
				for (var i = 0; i < _maxBind; i++)
				{
					if (i < newBindings.Length)
					{
						_bindings[i] = newBindings[i];
					}
				}

				UpdateLabel();
			}
		}

		[DllImport("user32")]
		private static extern bool HideCaret(IntPtr hWnd);

		protected override void OnMouseClick(MouseEventArgs e)
		{
			HideCaret(Handle);
			base.OnMouseClick(e);
		}

		public void ClearAll()
		{
			ClearBindings();
			Clear();
		}

		private void ClearBindings()
		{
			for (var i = 0; i < _maxBind; i++)
			{
				_bindings[i] = string.Empty;
			}
		}

		protected override void OnEnter(EventArgs e)
		{
			_pos = 0;
			_timer.Start();

			_wasPressed = Input.Instance.GetNextBindEvent();
			BackColor = Color.FromArgb(unchecked((int)0xFFC0FFFF)); // Color.LightCyan is too light on Windows 8, this is a bit darker
		}

		protected override void OnLeave(EventArgs e)
		{
			_timer.Stop();
			UpdateLabel();
			BackColor = SystemColors.Window;
			base.OnLeave(e);
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			ReadKeys();
		}

		public void EraseMappings()
		{
			ClearBindings();
			Text = string.Empty;
			_tooltip1.SetToolTip(this, string.Empty);
		}

		/// <summary>
		/// sets a binding manually. This may not be implemented quite right.
		/// </summary>
		public void SetBinding(string bindingStr)
		{
			_bindings[_pos] = bindingStr;
			UpdateLabel();
			Increment();
		}

		/// <summary>
		/// Poll input events and apply processing related to accepting that as a binding
		/// </summary>
		private void ReadKeys()
		{
			Input.Instance.Update();
			var bindingStr = Input.Instance.GetNextBindEvent();
			if (!string.IsNullOrEmpty(_wasPressed) && bindingStr == _wasPressed)
			{
				return;
			}
			
			if (bindingStr != null)
			{
				
				//has special meaning for the binding UI system (clear it).
				//you can set it through the special bindings dropdown menu
				if (bindingStr == "Escape")
				{
					EraseMappings();
					Increment();
					return;
				}

				//seriously, we refuse to allow you to bind this to anything else.
				if (bindingStr == "Alt+F4")
				{
					return;
				}

				//ignore special bindings
				if (SpecialBindings.Contains(bindingStr))
				{
					return;
				}


				if (!IsDuplicate(bindingStr))
				{
					_bindings[_pos] = bindingStr;
				}
				
				_wasPressed = bindingStr;
				UpdateLabel();
				Increment();
			}
		}

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

			_wasPressed = string.Empty;
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
			{
				CompositeWidget.TabNext();
			}
			else
			{
				if (_pos < _maxBind)
				{
					_pos++;
				}
				else
				{
					_pos = 0;
				}
			}
		}

		public void Decrement()
		{
			if (AutoTab)
			{
				Parent.SelectNextControl(this, false, true, true, true);
			}
			else
			{
				if (_pos == 0)
				{
					_pos = _maxBind - 1;
				}
				else
				{
					_pos--;
				}
			}
		}

		public void UpdateLabel()
		{
			Text = string.Join(",", _bindings.Where(str => !string.IsNullOrWhiteSpace(str)));
			_tooltip1.SetToolTip(this, Text);
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			e.Handled = true;
		}

		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case 0x0201: // WM_LBUTTONDOWN
					Focus();
					return;
				case 0x0203: // WM_LBUTTONDBLCLK
				case 0x0204: // WM_RBUTTONDOWN
				case 0x0205: // WM_RBUTTONUP
				case 0x0206: // WM_RBUTTONDBLCLK
					return;
			}

			base.WndProc(ref m);
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if (e.Delta > 0)
			{
				Decrement();
			}
			else
			{
				Increment();
			}

			base.OnMouseWheel(e);
		}

		protected override void OnGotFocus(EventArgs e)
		{
			HideCaret(Handle);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			return !(keyData.ToString() == "F4" || keyData.ToString().Contains("Alt"));
		}
	}
}