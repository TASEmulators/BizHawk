using System;
using System.Globalization;
using System.Windows.Forms;

namespace BizHawk
{
	public interface INumberBox
	{
		int ToRawInt();
		void SetFromRawInt(int rawint);
		bool Nullable { get; }
	}

	public class HexTextBox : TextBox, INumberBox
	{
		private string _addressFormatStr = "{0:X4}";
		private int? _maxSize = null;
		private bool _nullable = true;

		public bool Nullable { get { return _nullable; } set { _nullable = value; } }

		public void SetHexProperties(int domainSize)
		{
			_maxSize = domainSize - 1;
			MaxLength = IntHelpers.GetNumDigits(_maxSize.Value);
			_addressFormatStr = "{0:X" + MaxLength.ToString() + "}";
			
			ResetText();
		}

		private uint GetMax()
		{
			if (_maxSize.HasValue)
			{
				return (uint)_maxSize.Value;
			}
			else
			{
				return IntHelpers.MaxHexValueFromMaxDigits(MaxLength);
			}
		}

		public override void ResetText()
		{
			if (_nullable)
			{
				Text = String.Empty;
			}
			else
			{
				Text = String.Format(_addressFormatStr, 0);
			}
		}

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

					if (val == GetMax())
					{
						val = 0;
					}
					else
					{
						val++;
					}

					Text = String.Format(_addressFormatStr, val);
				}
			}
			else if (e.KeyCode == Keys.Down)
			{
				if (InputValidate.IsValidHexNumber(Text))
				{
					uint val = (uint)ToRawInt();
					if (val == 0)
					{
						val = GetMax();
					}
					else
					{
						val--;
					}

					Text = String.Format(_addressFormatStr, val);
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
				ResetText();
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

		public void SetFromRawInt(int val)
		{
			Text = String.Format(_addressFormatStr, val);
		}
	}

	public class UnsignedIntegerBox : TextBox, INumberBox
	{
		public UnsignedIntegerBox()
		{
			CharacterCasing = CharacterCasing.Upper;
		}

		private bool _nullable = true;

		public bool Nullable { get { return _nullable; } set { _nullable = value; } }

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

		public override void ResetText()
		{
			if (_nullable)
			{
				Text = String.Empty;
			}
			else
			{
				Text = "0";
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
				ResetText();
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

		public void SetFromRawInt(int val)
		{
			Text = val.ToString();
		}
	}
}
