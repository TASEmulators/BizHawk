using System;
using System.Globalization;
using System.Windows.Forms;
namespace BizHawk.MultiClient
{
	class WatchValueBox : TextBox, INumberBox
	{
		private Watch.WatchSize _size = Watch.WatchSize.Byte;
		private Watch.DisplayType _type = Watch.DisplayType.Hex;

		public WatchValueBox()
		{
			CharacterCasing = CharacterCasing.Upper;
		}

		public Watch.WatchSize ByteSize
		{
			get { return _size; }
			set
			{
				if (_size != value)
				{
					if (!Watch.AvailableTypes(value).Contains(_type))
					{
						Type = Watch.AvailableTypes(value)[0];
					}
				}
				_size = value;
			}
		}

		public Watch.DisplayType Type
		{
			get { return _type; }
			set
			{
				_type = value;
				switch(_type)
				{
					default:
						MaxLength = 8;
						break;
					case Watch.DisplayType.Binary:
						switch (_size)
						{
							default:
							case Watch.WatchSize.Byte:
								MaxLength = 8;
								break;
							case Watch.WatchSize.Word:
								MaxLength = 16;
								break;
						}
						break;
					case Watch.DisplayType.Hex:
						switch (_size)
						{
							default:
							case Watch.WatchSize.Byte:
								MaxLength = 2;
								break;
							case Watch.WatchSize.Word:
								MaxLength = 4;
								break;
							case Watch.WatchSize.DWord:
								MaxLength = 8;
								break;
						}
						break;
					case Watch.DisplayType.Signed:
						switch (_size)
						{
							default:
							case Watch.WatchSize.Byte:
								MaxLength = 4;
								break;
							case Watch.WatchSize.Word:
								MaxLength = 6;
								break;
							case Watch.WatchSize.DWord:
								MaxLength = 11;
								break;
						}
						break;
					case Watch.DisplayType.Unsigned:
						switch (_size)
						{
							default:
							case Watch.WatchSize.Byte:
								MaxLength = 3;
								break;
							case Watch.WatchSize.Word:
								MaxLength = 5;
								break;
							case Watch.WatchSize.DWord:
								MaxLength = 10;
								break;
						}
						break;
					case Watch.DisplayType.Float:
					case Watch.DisplayType.FixedPoint_12_4:
					case Watch.DisplayType.FixedPoint_20_12:
						MaxLength = 32;
						break;
				}
			}
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar == '\b' || e.KeyChar == 22 || e.KeyChar == 1 || e.KeyChar == 3)
			{
				return;
			}

			if (e.KeyChar == '.')
			{
				if (Text.Contains("."))
				{
					e.Handled = true;
					return;
				}
			}
			else if (e.KeyChar == '-')
			{
				if (Text.Contains("-"))
				{
					e.Handled = true;
					return;
				}
			}

			switch(_type)
			{
				default:
				case Watch.DisplayType.Binary:
					if (!InputValidate.IsValidBinaryNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
				case Watch.DisplayType.FixedPoint_12_4:
				case Watch.DisplayType.FixedPoint_20_12:
					if (!InputValidate.IsValidFixedPointNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
				case Watch.DisplayType.Float:
					if (!InputValidate.IsValidDecimalNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
				case Watch.DisplayType.Hex:
					if (!InputValidate.IsValidHexNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
				case Watch.DisplayType.Signed:
					if (!InputValidate.IsValidSignedNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
				case Watch.DisplayType.Unsigned:
					if (!InputValidate.IsValidUnsignedNumber(e.KeyChar))
					{
						e.Handled = true;
					}
					break;
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Up)
			{
				int val = ToInt();
				val++;

				switch (_type)
				{
					default:
						Text = val.ToString();
						break;
					case Watch.DisplayType.Binary:
						throw new NotImplementedException();
					case Watch.DisplayType.Hex:
						string formatstr = "{0:X" + MaxLength.ToString() + "}";
						Text = String.Format(formatstr, val);
						break;
				}
			}
			else if (e.KeyCode == Keys.Down)
			{
				int val = ToInt();
				val--;

				switch (_type)
				{
					default:
						Text = val.ToString();
						break;
					case Watch.DisplayType.Binary:
						throw new NotImplementedException();
					case Watch.DisplayType.Hex:
						string formatstr = "{0:X" + MaxLength.ToString() + "}";
						Text = String.Format(formatstr, val);
						break;
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

		public int ToInt()
		{
			if (String.IsNullOrWhiteSpace(Text))
			{
				return 0;
			}
			else
			{
				switch (_type)
				{
					default:
						return int.Parse(Text);
					case Watch.DisplayType.Binary:
						return Convert.ToInt32(Text, 2);
					case Watch.DisplayType.Hex:
						return int.Parse(Text, NumberStyles.HexNumber);
				}
			}
		}
	}
}
