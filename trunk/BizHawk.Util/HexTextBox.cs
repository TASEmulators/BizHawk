using System;
using System.Globalization;
using System.Windows.Forms;

namespace BizHawk
{
	public interface INumberBox
	{
		int ToRawInt();
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
					uint val = (uint)ToRawInt();

					if (val == IntHelpers.MaxHexValueFromMaxDigits(MaxLength))
					{
						val = 0;
					}
					else
					{
						val++;
					}
					string formatstr = "{0:X" + MaxLength.ToString() + "}";
					Text = String.Format(formatstr, val);
				}
			}
			else if (e.KeyCode == Keys.Down)
			{
				if (InputValidate.IsValidHexNumber(Text))
				{
					uint val = (uint)ToRawInt();
					if (val == 0)
					{
						val = IntHelpers.MaxHexValueFromMaxDigits(MaxLength);
					}
					else
					{
						val--;
					}

					string formatstr = "{0:X" + MaxLength.ToString() + "}";
					Text = String.Format(formatstr, val);
				}
			}
			else
			{
				base.OnKeyDown(e);
			}
		}

		protected override void OnTextChanged(EventArgs e)
		{
			if (String.IsNullOrWhiteSpace(Text))
			{
				Text = "0";
			}
		}

		public int ToRawInt()
		{
			if (String.IsNullOrWhiteSpace(Text))
			{
				return 0;
			}
			else
			{
				return int.Parse(Text, NumberStyles.HexNumber);
			}
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
					uint val = (uint)ToRawInt();
					if (val == uint.MaxValue)
					{
						val = 0;
					}
					else
					{
						val++;
					}
					Text = val.ToString();
				}
			}
			else if (e.KeyCode == Keys.Down)
			{
				if (InputValidate.IsValidUnsignedNumber(Text))
				{
					uint val = (uint)ToRawInt();

					if (val == 0)
					{
						val = uint.MaxValue;
					}
					else
					{
						val--;
					}

					Text = val.ToString();
				}
			}
			else
			{
				base.OnKeyDown(e);
			}
		}

		protected override void OnTextChanged(EventArgs e)
		{
			if (String.IsNullOrWhiteSpace(Text))
			{
				Text = "0";
			}
		}

		public int ToRawInt()
		{
			if (String.IsNullOrWhiteSpace(Text))
			{
				return 0;
			}
			else
			{
				return (int)uint.Parse(Text);
			}
		}
	}
}
