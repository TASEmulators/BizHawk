using System;
using System.Globalization;
using System.Windows.Forms;

namespace BizHawk
{
	public interface INumberBox
	{
		int ToInt();
	}

	public class HexTextBox : TextBox, INumberBox
	{
		public HexTextBox()
		{
			CharacterCasing = CharacterCasing.Upper;
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b' || e.KeyChar == 22 || e.KeyChar == 1 || e.KeyChar == 3)
			{
				return;
			}
			else if (!InputValidate.IsValidHexNumber(e.KeyChar))
			{
				e.Handled = true;
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Up)
			{
				if (InputValidate.IsValidHexNumber(Text))
				{
					int val = ToInt();
					val++;
					string formatstr = "{0:X" + MaxLength.ToString() + "}";
					Text = String.Format(formatstr, val);
				}
			}
			else if (e.KeyCode == Keys.Down)
			{
				if (InputValidate.IsValidHexNumber(Text))
				{
					int val = ToInt();
					val--;
					string formatstr = "{0:X" + MaxLength.ToString() + "}";
					Text = String.Format(formatstr, val);
				}
			}
			else
			{
				base.OnKeyDown(e);
			}
		}

		public int ToInt()
		{
			return int.Parse(Text, NumberStyles.HexNumber);
		}
	}

	public class UnsignedIntegerBox : TextBox, INumberBox
	{
		public UnsignedIntegerBox()
		{
			CharacterCasing = CharacterCasing.Upper;
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b' || e.KeyChar == 22 || e.KeyChar == 1 || e.KeyChar == 3)
			{
				return;
			}
			else if (!InputValidate.IsValidUnsignedNumber(e.KeyChar))
			{
				e.Handled = true;
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Up)
			{
				if (InputValidate.IsValidUnsignedNumber(Text))
				{
					int val = ToInt();
					val++;
					Text = val.ToString();
				}
			}
			else if (e.KeyCode == Keys.Down)
			{
				if (InputValidate.IsValidUnsignedNumber(Text))
				{
					int val = ToInt();
					val--;
					Text = val.ToString();
				}
			}
			else
			{
				base.OnKeyDown(e);
			}
		}

		public int ToInt()
		{
			return int.Parse(Text);
		}
	}
}
