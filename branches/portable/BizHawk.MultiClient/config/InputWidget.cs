using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;

namespace BizHawk.MultiClient
{
	public class InputWidget : TextBox
	{
		//TODO: when binding, make sure that the new key combo is not in one of the other bindings

		int MaxBind = 4; //Max number of bindings allowed
		int pos = 0;	 //Which mapping the widget will listen for
		private Timer timer = new Timer();
		public bool AutoTab = true;
		string[] Bindings = new string[4];
		string wasPressed = "";
		ToolTip tooltip1 = new ToolTip();
		public string ButtonName;
		Color _highlight_color = Color.LightCyan;
		Color _no_highlight_color = SystemColors.Window;

		private void Highlight()
		{
			BackColor = _highlight_color;
		}

		private void UnHighlight()
		{
			BackColor = _no_highlight_color;
		}

		private List<KeyValuePair<string, string>> ConflictLookup = new List<KeyValuePair<string, string>>();

		[DllImport("user32")]
		private static extern bool HideCaret(IntPtr hWnd);

		public InputWidget()
		{
			this.ContextMenu = new ContextMenu();
			this.timer.Tick += new System.EventHandler(this.Timer_Tick);
			InitializeBindings();
			tooltip1.AutoPopDelay = 2000;
		}

		public InputWidget(List<KeyValuePair<string, string>> conflictList)
		{
			this.ContextMenu = new ContextMenu();
			this.timer.Tick += new System.EventHandler(this.Timer_Tick);
			InitializeBindings();
			tooltip1.AutoPopDelay = 2000;
			ConflictLookup = conflictList;
		}

		public InputWidget(int maxBindings)
		{
			this.ContextMenu = new ContextMenu();
			this.timer.Tick += new System.EventHandler(this.Timer_Tick);
			MaxBind = maxBindings;
			Bindings = new string[MaxBind];
			InitializeBindings();
			tooltip1.AutoPopDelay = 2000;
		}

		public void SetConflictList(List<KeyValuePair<string, string>> conflictLookup)
		{
			ConflictLookup = conflictLookup;
		}

#if WINDOWS
		protected override void OnMouseClick(MouseEventArgs e)
		{
			HideCaret(this.Handle);
			base.OnMouseClick(e);
		}
#endif

		private void InitializeBindings()
		{
			for (int x = 0; x < MaxBind; x++)
			{
				Bindings[x] = "";
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
			ClearBindings();
			HideConflicts();
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
					ClearBindings();
					HideConflicts();
					Increment();
					return;
				}

				if (TempBindingStr == "Alt+F4")
					return;

				if (!IsDuplicate(TempBindingStr))
				{
					Bindings[pos] = TempBindingStr;
				}
				wasPressed = TempBindingStr;

				DoConflictCheck();

				UpdateLabel();
				Increment();
			}
		}

		private string Conflicts = "";

		private void DoConflictCheck()
		{
			StringBuilder conflicts = new StringBuilder(); ;
			foreach (KeyValuePair<string, string> conflict in ConflictLookup)
			{
				foreach (string binding in Bindings)
				{
					if (conflict.Key == binding)
					{
						conflicts.Append(binding);
						conflicts.Append(" conflicts with Hotkey - "); //Ideally we don't hardcode Hotkey, we may want to check mappings on specific controllers or unforeseen things
						conflicts.Append(conflict.Value);
						conflicts.Append('\n');
					}
				}
			}
			Conflicts = conflicts.ToString();

			if (String.IsNullOrWhiteSpace(Conflicts))
			{
				HideConflicts();
			}
			else
			{
				ShowConflicts();
			}
		}

		private void ShowConflicts()
		{
			_no_highlight_color = Color.LightCoral;
			_highlight_color = Color.Violet;
			tooltip1.SetToolTip(this, Conflicts);
		}

		private void HideConflicts()
		{
			_highlight_color = Color.LightCyan;
			_no_highlight_color = SystemColors.Window;
			tooltip1.SetToolTip(this, "");
		}

		//Checks if the key is already mapped to this widget
		private bool IsDuplicate(string binding)
		{
			for (int x = 0; x < MaxBind; x++)
			{
				if (Bindings[x] == binding)
					return true;
			}

			return false;
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

		public void ClearBindings()
		{
			for (int i = 0; i < MaxBind; i++)
			{
				Bindings[i] = "";
			}
		}

		public void UpdateLabel()
		{
			Text = "";
			for (int x = 0; x < MaxBind; x++)
			{
				if (Bindings[x].Length > 0)
				{
					Text += Bindings[x];
					if (x < MaxBind - 1 && Bindings[x+1].Length > 0)
						Text += ", ";
				}
			}
		}

		public void SetBindings(string bindingsString)
		{
			Text = "";
			ClearBindings();
			string str = bindingsString.Trim();
			int x;
			for (int i = 0; i < MaxBind; i++)
			{
				str = str.Trim();
				x = str.IndexOf(',');
				if (x < 0)
				{
					Bindings[i] = str;
					str = "";
				}
				else
				{
					Bindings[i] = str.Substring(0, x);
					str = str.Substring(x + 1, str.Length - x - 1);
				}
			}
			UpdateLabel();
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			e.Handled = true;
		}
		
#if WINDOWS
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
#endif

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
#if WINDOWS
			HideCaret(this.Handle);
#endif
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