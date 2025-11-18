using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class InputWidget : TextBox
	{
		// TODO: when binding, make sure that the new key combo is not in one of the other bindings
		private readonly Timer _timer = new Timer();
		private readonly List<string> _bindings = new List<string>();

		private InputEvent _lastPress;

		public InputCompositeWidget CompositeWidget { get; set; }

		public class SpecialBindingInfo
		{
			public string BindingName { get; set; }
			public string TooltipText { get; set; }
		}

		/// <summary>
		/// These bindings get ignored by the widget and can only be entered by SetBinding() via the context menu from the InputCompositeWidget
		/// </summary>
		public static readonly SpecialBindingInfo[] SpecialBindings =
		{
			new SpecialBindingInfo { BindingName = "Escape", TooltipText = "Binds the Escape key" },
			new SpecialBindingInfo { BindingName = "WMouse L", TooltipText = "Binds the left mouse button" },
			new SpecialBindingInfo { BindingName = "WMouse M", TooltipText = "Binds the middle mouse button" },
			new SpecialBindingInfo { BindingName = "WMouse R", TooltipText = "Binds the right mouse button" },
			new SpecialBindingInfo { BindingName = "WMouse 1", TooltipText = "Binds the mouse auxiliary button 1" },
			new SpecialBindingInfo { BindingName = "WMouse 2", TooltipText = "Binds the mouse auxiliary button 2" },
		};

		public InputWidget()
		{
			_timer.Tick += Timer_Tick;
			ClearBindings();
			AutoTab = true;
			Cursor = Cursors.Arrow;
		}

		public bool AutoTab { get; set; }
		public string WidgetName { get; set; }

		public string Bindings
		{
			get => Text;
			set
			{
				ClearBindings();
				var newBindings = value.Trim().Split(',');
				_bindings.AddRange(newBindings);
				UpdateLabel();
			}
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			if (!OSTailoredCode.IsUnixHost)
			{
				WmImports.HideCaret(new(Handle));
			}

			base.OnMouseClick(e);
		}

		public void ClearAll()
		{
			ClearBindings();
			Clear();
		}

		private void ClearBindings()
		{
			_bindings.Clear();
		}

		protected override void OnEnter(EventArgs e)
		{
			Input.Instance.ClearEvents();
			_lastPress = null;
			_timer.Start();
			BackColor = Color.FromArgb(unchecked((int)0xFFC0FFFF)); // Color.LightCyan is too light on Windows 8, this is a bit darker
		}

		protected override void OnLeave(EventArgs e)
		{
			_timer.Stop();
			UpdateLabel();
			BackColor = SystemColors.Window;
			base.OnLeave(e);
		}

		protected override void OnHandleDestroyed(EventArgs e)
		{
			_timer.Stop();
			base.OnHandleDestroyed(e);
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			ReadKeys();
		}

		private void EraseMappings()
		{
			ClearBindings();
			Text = "";
		}

		/// <summary>
		/// sets a binding manually. This may not be implemented quite right.
		/// </summary>
		public void SetBinding(string bindingStr)
		{
			_bindings.Add(bindingStr);
			UpdateLabel();
			Increment();
		}

		/// <summary>
		/// Poll input events and apply processing related to accepting that as a binding
		/// </summary>
		private void ReadKeys()
		{
			Input.Instance.Update();
			var bindingStr = Input.Instance.GetNextBindEvent(ref _lastPress);

			if (bindingStr != null)
			{
				// has special meaning for the binding UI system (clear it).
				// you can set it through the special bindings dropdown menu
				if (bindingStr == "Escape")
				{
					EraseMappings();
					Increment();
					return;
				}

				// seriously, we refuse to allow you to bind this to anything else.
				if (bindingStr == "Alt+F4")
				{
					return;
				}

				// ignore special bindings
				foreach (var spec in SpecialBindings)
				{
					if (spec.BindingName == bindingStr)
					{
						return;
					}
				}

				if (!IsDuplicate(bindingStr))
				{
					if (AutoTab)
					{
						ClearBindings();
					}

					_bindings.Add(bindingStr);
				}

				UpdateLabel();
				Increment();
			}
		}

		private bool IsDuplicate(string binding)
			=> _bindings.Contains(binding);

		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (e.IsAlt(Keys.F4))
			{
				base.OnKeyUp(e);
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.IsAlt(Keys.F4))
			{
				base.OnKeyDown(e);
				return;
			}

			e.Handled = true;
		}

		// Advances to the next widget depending on the autotab setting
		public void Increment()
		{
			if (AutoTab)
			{
				CompositeWidget.TabNext();
			}
		}

		public void Decrement()
		{
			if (AutoTab)
			{
				Parent.SelectNextControl(this, false, true, true, true);
			}
		}

		public void UpdateLabel()
		{
			Text = string.Join(",", _bindings.Where(str => !string.IsNullOrWhiteSpace(str)));
			CompositeWidget.RefreshTooltip();
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
#pragma warning disable RS0030 //TODO is this correct, or should it be `Select()`? --yoshi
					Focus();
#pragma warning restore RS0030
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
			if (!OSTailoredCode.IsUnixHost)
			{
				WmImports.HideCaret(new(Handle));
			}
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			return !(keyData.ToString() == "F4" || keyData.ToString().Contains("Alt"));
		}
	}
}
