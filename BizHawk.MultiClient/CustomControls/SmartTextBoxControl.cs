using System;
using System.Windows.Forms;

namespace BizHawk
{
	public class SmartTextBoxControl : TextBox
	{
		public SmartTextBoxControl()
		{
			ReadOnly = true;
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.Text = e.KeyCode.ToString();
			OnTextChanged(new EventArgs());
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
		}

		public override string Text
		{
			get { return base.Text; }
			set { }
		}

	}
}