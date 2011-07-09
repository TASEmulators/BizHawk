using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;

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
		
		public InputWidget()
		{
			this.ContextMenu = new ContextMenu();
			this.timer.Tick += new System.EventHandler(this.Timer_Tick);
			InitializeBindings();
		}

		public InputWidget(int maxBindings)
		{
			this.ContextMenu = new ContextMenu();
			this.timer.Tick += new System.EventHandler(this.Timer_Tick);
			MaxBind = maxBindings;
			Bindings = new string[MaxBind];
			InitializeBindings();
		}

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
			base.OnEnter(e);
			Input.Update();
			wasPressed = Input.GetPressedKey();
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

		private void ReadKeys()
		{
			Input.Update();
			string TempBindingStr = Input.GetPressedKey();
			if (wasPressed != "" && TempBindingStr == wasPressed) return;
			if (TempBindingStr != null)
			{
				if (TempBindingStr == "Escape")
				{
					ClearBindings();
					Increment();
					return;
				}

				if (TempBindingStr == "Alt+F4")
					return;

				Bindings[pos] = TempBindingStr;
				wasPressed = TempBindingStr;
				UpdateLabel();
				Increment();
			}
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
			}
			e.Handled = true;
		}

		// Advances to the next widget or the next binding depending on the autotab setting
		private void Increment()
		{
			if (AutoTab)
				this.Parent.SelectNextControl(this, true, true, true, true);
			else
			{
				if (pos == MaxBind - 1)
				    pos = 0;
				else
				    pos++;
			}
		}

		public void ClearBindings()
		{
			for (int x = 0; x < MaxBind; x++)
				Bindings[x] = "";
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
				ReadKeys();
			}
			return false;
		}
	}
}