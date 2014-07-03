using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Common.StringExtensions;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// A simple form that prompts the user for a single line of input
	/// </summary>
	public partial class InputPrompt : Form
	{
		public InputPrompt()
		{
			InitializeComponent();
			UserText = string.Empty;
			StartLocation = new Point(-1, -1);
		}

		public enum InputType { Hex, Unsigned, Signed, Text }
		public bool UserOk { get; set; } // Will be true if the user selects Ok
		public string UserText { get; set; } // What the user selected
		public Point StartLocation { get; set; }
		public InputType TextInputType { get; set; }

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
			if (StartLocation.X > 0 && StartLocation.Y > 0)
			{
				Location = StartLocation;
			}
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			UserOk = true;
			UserText = PromptBox.Text;
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			UserOk = false;
			Close();
		}

		private void PromptBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			switch (TextInputType)
			{
				default:
				case InputType.Text:
					break;
				case InputType.Hex:
					if (e.KeyChar == '\b' || e.KeyChar == 22 || e.KeyChar == 1 || e.KeyChar == 3)
					{
						return;
					}
					
					if (!e.KeyChar.IsHex())
					{
						e.Handled = true;
					}

					break;
				case InputType.Signed:
					if (e.KeyChar == '\b' || e.KeyChar == 22 || e.KeyChar == 1 || e.KeyChar == 3)
					{
						return;
					}
					
					if (!e.KeyChar.IsUnsigned())
					{
						e.Handled = true;
					}

					break;
				case InputType.Unsigned:
					if (e.KeyChar == '\b' || e.KeyChar == 22 || e.KeyChar == 1 || e.KeyChar == 3)
					{
						return;
					}
					
					if (!e.KeyChar.IsSigned())
					{
						e.Handled = true;
					}

					break;
			}
		}
	}
}
