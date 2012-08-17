using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	/// <summary>
	/// A simple form that prompts the user for a single line of input
	/// </summary>
	public partial class InputPrompt : Form
	{
		public enum InputType { HEX, UNSIGNED, SIGNED, TEXT };
		public bool UserOK;    //Will be true if the user selects Ok
		public string UserText = "";   //What the user selected

		private InputType itype = InputType.TEXT;

		public InputType TextInputType
		{
			get { return itype; }
			set { itype = value; }
		}

		public InputPrompt()
		{
			InitializeComponent();
		}

		public void SetMessage(string message)
		{
			PromptLabel.Text = message;
		}

		public void SetCasing(CharacterCasing casing)
		{
			PromptBox.CharacterCasing = casing;
		}

		public void SetInitialValue(string value)
		{
			PromptBox.Text = value;
		}

		public void SetTitle(string value)
		{
			Text = value;
		}

		private void InputPrompt_Load(object sender, EventArgs e)
		{

		}

		private void OK_Click(object sender, EventArgs e)
		{
			UserOK = true;
			UserText = PromptBox.Text;
			this.Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			UserOK = false;
			this.Close();
		}

		private void PromptBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			switch (itype)
			{
				default:
				case InputType.TEXT:
					break;
				case InputType.HEX:
					if (e.KeyChar == '\b')
					{
						return;
					}
					else if (!InputValidate.IsValidHexNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
				case InputType.SIGNED:
					if (e.KeyChar == '\b')
					{
						return;
					}
					else if (!InputValidate.IsValidUnsignedNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
				case InputType.UNSIGNED:
					if (e.KeyChar == '\b')
					{
						return;
					}
					else if (!InputValidate.IsValidSignedNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
			}
		}
	}
}
