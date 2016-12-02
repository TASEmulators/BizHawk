using System;
using System.Globalization;
using System.Windows.Forms;

using BizHawk.Common.StringExtensions;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Client.EmuHawk
{
	// TODO: add a MaxValue property, nullable int, that will show up in Designer, change events will check that value and fix entries that exceed that value
	public interface INumberBox
	{
		bool Nullable { get; }
		int? ToRawInt();
		void SetFromRawInt(int? rawint);
	}

	public class HexTextBox : TextBox, INumberBox
	{
		private string _addressFormatStr = string.Empty;
		private long? _maxSize;
		private bool _nullable = true;

		public HexTextBox()
		{
			CharacterCasing = CharacterCasing.Upper;
		}

		public bool Nullable { get { return _nullable; } set { _nullable = value; } }

		public void SetHexProperties(long domainSize)
		{
			bool wasMaxSizeSet = _maxSize.HasValue;
			int currMaxLength = MaxLength;

			_maxSize = domainSize - 1;

			MaxLength = _maxSize.Value.NumHexDigits();
			_addressFormatStr = "{0:X" + MaxLength + "}";

			//try to preserve the old value, as best we can
			if(!wasMaxSizeSet)
				ResetText();
			else if(_nullable)
				Text = "";
			else if (MaxLength != currMaxLength)
			{
				long? value = ToLong();
				if (value.HasValue)
					value = value.Value & ((1L << (MaxLength * 4)) - 1);
				else value = 0;
				Text = string.Format(_addressFormatStr, value.Value);
			}
		}

		public long GetMax()
		{
			if (_maxSize.HasValue)
			{
				return _maxSize.Value;
			}

			return ((long)1 << (4 * MaxLength)) - 1;
		}

		public override void ResetText()
		{
			Text = _nullable ? string.Empty : string.Format(_addressFormatStr, 0);
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b' || e.KeyChar == 22 || e.KeyChar == 1 || e.KeyChar == 3)
			{
				return;
			}
			
			if (!e.KeyChar.IsHex())
			{
				e.Handled = true;
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Up)
			{
				if (Text.IsHex() && !string.IsNullOrEmpty(_addressFormatStr))
				{
					var val = (uint)ToRawInt();

					if (val == GetMax())
					{
						val = 0;
					}
					else
					{
						val++;
					}

					Text = string.Format(_addressFormatStr, val);
				}
			}
			else if (e.KeyCode == Keys.Down)
			{
				if (Text.IsHex() && !string.IsNullOrEmpty(_addressFormatStr))
				{
					var val = (uint)ToRawInt();
					if (val == 0)
					{
						val = (uint)GetMax(); // int to long todo
					}
					else
					{
						val--;
					}

					Text = string.Format(_addressFormatStr, val);
				}
			}
			else
			{
				base.OnKeyDown(e);
			}
		}

		protected override void OnTextChanged(EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(Text))
			{
				ResetText();
				SelectAll();
				return;
			}

			base.OnTextChanged(e);
		}

		public int? ToRawInt()
		{
			if (string.IsNullOrWhiteSpace(Text))
			{
				if (Nullable)
				{
					return null;
				}
				
				return 0;
			}

			return int.Parse(Text, NumberStyles.HexNumber);
		}

		public void SetFromRawInt(int? val)
		{
			Text = val.HasValue ? string.Format(_addressFormatStr, val) : string.Empty;
		}

		public void SetFromLong(long val)
		{
			Text = string.Format(_addressFormatStr, val);
		}

		public long? ToLong()
		{
			if (string.IsNullOrWhiteSpace(Text))
			{
				if (Nullable)
				{
					return null;
				}

				return 0;
			}

			return long.Parse(Text, NumberStyles.HexNumber);
		}
	}

	public class UnsignedIntegerBox : TextBox, INumberBox
	{
		private bool _nullable = true;

		public UnsignedIntegerBox()
		{
			CharacterCasing = CharacterCasing.Upper;
		}

		public bool Nullable { get { return _nullable; } set { _nullable = value; } }

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b' || e.KeyChar == 22 || e.KeyChar == 1 || e.KeyChar == 3)
			{
				return;
			}
			
			if (!e.KeyChar.IsUnsigned())
			{
				e.Handled = true;
			}
		}

		public override void ResetText()
		{
			Text = _nullable ? string.Empty : "0";
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Up)
			{
				if (Text.IsHex())
				{
					var val = (uint)ToRawInt();
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
				if (Text.IsHex())
				{
					var val = (uint)ToRawInt();

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
			if (string.IsNullOrWhiteSpace(Text) || !Text.IsHex())
			{
				ResetText();
				SelectAll();
				return;
			}

			base.OnTextChanged(e);
		}

		public int? ToRawInt()
		{
			if (string.IsNullOrWhiteSpace(Text) || !Text.IsHex())
			{
				if (Nullable)
				{
					return null;
				}
				
				return 0;
			}

			return (int)uint.Parse(Text);
		}

		public void SetFromRawInt(int? val)
		{
			Text = val.HasValue ? val.ToString() : string.Empty;
		}
	}
}
