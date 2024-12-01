using System.Drawing;
using System.Windows.Forms;
using System.Linq;

using BizHawk.Common.StringExtensions;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// A simple form that prompts the user for a single line of input.
	/// Supports multiline messages
	/// </summary>
	public partial class InputPrompt : Form
	{
		public InputPrompt()
		{
			InitializeComponent();
			StartLocation = new Point(-1, -1);
		}

		public enum InputType { Hex, Unsigned, Signed, Text }

		public Point StartLocation { get; set; }
		public InputType TextInputType { get; set; }

		public string Message
		{
			get => PromptLabel.Text;
			set
			{
				PromptLabel.Text = value ?? "";
				Height += PromptLabel.Font.Height * Message.Count(x => x == '\n');
			}
		}

		public string InitialValue
		{
			get => PromptBox.Text;
			set => PromptBox.Text = value ?? "";
		}

		public string PromptText => PromptBox.Text ?? "";

		private void InputPrompt_Load(object sender, EventArgs e)
		{
			if (StartLocation.X > 0 && StartLocation.Y > 0)
			{
				Location = StartLocation;
			}
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
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
					
					if (!e.KeyChar.IsSigned())
					{
						e.Handled = true;
					}

					break;
				case InputType.Unsigned:
					if (e.KeyChar == '\b' || e.KeyChar == 22 || e.KeyChar == 1 || e.KeyChar == 3)
					{
						return;
					}

					if (!e.KeyChar.IsUnsigned())
					{
						e.Handled = true;
					}

					break;
			}
		}
	}
}
