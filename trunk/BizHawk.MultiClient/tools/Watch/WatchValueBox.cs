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

		public override void ResetText()
		{
			switch (Type)
			{
				default:
				case Watch.DisplayType.Signed:
				case Watch.DisplayType.Unsigned:
					Text = "0";
					break;
				case Watch.DisplayType.Hex:
					string formatstr = "{0:X" + MaxLength.ToString() + "}";
					Text = String.Format(formatstr, 0);
					break;
				case Watch.DisplayType.FixedPoint_12_4:
				case Watch.DisplayType.FixedPoint_20_12:
				case Watch.DisplayType.Float:
					Text = "0.0";
					break;
				case Watch.DisplayType.Binary:
					Text = "0".PadLeft(((int)_size) * 8);
					break;
			}

			
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
					case Watch.DisplayType.FixedPoint_12_4:
						MaxLength = 9;
						break;
					case Watch.DisplayType.Float:
						MaxLength = 21;
						break;
					case Watch.DisplayType.FixedPoint_20_12:
						MaxLength = 64;
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
				switch (_type)
				{
					default:
					case Watch.DisplayType.Signed:
						int val = ToRawInt();
						if (val == MaxSignedInt)
						{
							val = 0;
						}
						else
						{
							val++;
						}
						Text = val.ToString();
						break;
					case Watch.DisplayType.Unsigned:
						uint uval = (uint)ToRawInt();
						if (uval == MaxUnsignedInt)
						{
							uval = 0;
						}
						else
						{
							uval++;
						}
						Text = uval.ToString();
						break;
					case Watch.DisplayType.Binary:
						uint bval = (uint)ToRawInt();
						if (bval == MaxUnsignedInt)
						{
							bval = 0;
						}
						else
						{
							bval++;
						}
						int numBits = ((int)ByteSize) * 8;
						Text = Convert.ToString(bval, 2).PadLeft(numBits, '0');
						break;
					case Watch.DisplayType.Hex:
						uint hexVal = (uint)ToRawInt();
						if (hexVal == MaxUnsignedInt)
						{
							hexVal = 0;
						}
						else
						{
							hexVal++;
						}
						string formatstr = "{0:X" + MaxLength.ToString() + "}";
						Text = String.Format(formatstr, hexVal);
						break;
					case Watch.DisplayType.FixedPoint_12_4:
						double f12val = double.Parse(Text);
						if (f12val > Max12_4 - _12_4_Unit)
						{
							f12val = 0;
						}
						else
						{
							f12val += _12_4_Unit;
						}
						Text = f12val.ToString();
						break;
					case Watch.DisplayType.FixedPoint_20_12:
						double f24val = double.Parse(Text);
						if (f24val >= Max20_12 - _20_12_Unit)
						{
							f24val = 0;
						}
						else
						{
							f24val += _20_12_Unit;
						}
						Text = f24val.ToString();
						break;
					case Watch.DisplayType.Float:
						double dval = double.Parse(Text);
						if (dval > double.MaxValue - 1)
						{
							dval = 0;
						}
						else
						{
							dval++;
						}
						Text = dval.ToString();
						break;
				}
			}
			else if (e.KeyCode == Keys.Down)
			{
				switch (_type)
				{
					default:
					case Watch.DisplayType.Signed:
						int val = ToRawInt();
						if (val == 0)
						{
							val = MaxSignedInt;
						}
						else
						{
							val--;
						}
						Text = val.ToString();
						break;
					case Watch.DisplayType.Unsigned:
						uint uval = (uint)ToRawInt();
						if (uval == 0)
						{
							uval = MaxUnsignedInt;
						}
						else
						{
							uval--;
						}
						Text = uval.ToString();
						break;
					case Watch.DisplayType.Binary:
						uint bval = (uint)ToRawInt();
						if (bval == 0)
						{
							bval = MaxUnsignedInt;
						}
						else
						{
							bval--;
						}
						int numBits = ((int)ByteSize) * 8;
						Text = Convert.ToString(bval, 2).PadLeft(numBits, '0');
						break;
					case Watch.DisplayType.Hex:
						uint hexVal = (uint)ToRawInt();
						if (hexVal == 0)
						{
							hexVal = MaxUnsignedInt;
						}
						else
						{
							hexVal--;
						}
						string formatstr = "{0:X" + MaxLength.ToString() + "}";
						Text = String.Format(formatstr, hexVal);
						break;
					case Watch.DisplayType.FixedPoint_12_4:
						double f12val = double.Parse(Text);
						if (f12val < 0 + _12_4_Unit)
						{
							f12val = Max12_4;
						}
						else
						{
							f12val -= _12_4_Unit;
						}
						Text = f12val.ToString();
						break;
					case Watch.DisplayType.FixedPoint_20_12:
						double f24val = double.Parse(Text);
						if (f24val < 0 + _20_12_Unit)
						{
							f24val = Max20_12;
						}
						else
						{
							f24val -= _20_12_Unit;
						}
						Text = f24val.ToString();
						break;
					case Watch.DisplayType.Float:
						double dval = double.Parse(Text);
						if (dval > double.MaxValue - 1)
						{
							dval = 0;
						}
						else
						{
							dval--;
						}
						Text = dval.ToString();
						break;
				}
			}
			else
			{
				base.OnKeyDown(e);
			}
		}

		uint MaxUnsignedInt
		{
			get
			{
				switch (ByteSize)
				{
					default:
					case Watch.WatchSize.Byte:
						return byte.MaxValue;
					case Watch.WatchSize.Word:
						return ushort.MaxValue;
					case Watch.WatchSize.DWord:
						return uint.MaxValue;
				}
			}
		}

		int MaxSignedInt
		{
			get
			{
				switch (ByteSize)
				{
					default:
					case Watch.WatchSize.Byte:
						return sbyte.MaxValue;
					case Watch.WatchSize.Word:
						return short.MaxValue;
					case Watch.WatchSize.DWord:
						return int.MaxValue;
				}
			}
		}

		double Max12_4
		{
			get
			{
				return MaxUnsignedInt / 16.0;
			}
		}

		double Max20_12
		{
			get
			{
				return MaxUnsignedInt / 4096.0;
			}
		}

		double _12_4_Unit { get { return 1 / 16.0; } }
		double _20_12_Unit { get { return 1 / 4096.0; } }


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
				switch (_type)
				{
					default:
					case Watch.DisplayType.Signed:
						return int.Parse(Text);
					case Watch.DisplayType.Unsigned:
						return (int)uint.Parse(Text);
					case Watch.DisplayType.Binary:
						return Convert.ToInt32(Text, 2);
					case Watch.DisplayType.Hex:
						return int.Parse(Text, NumberStyles.HexNumber);
					case Watch.DisplayType.FixedPoint_12_4:
						return (int)(double.Parse(Text) * 16.0);
					case Watch.DisplayType.FixedPoint_20_12:
						return (int)(double.Parse(Text) * 4096.0);
					case Watch.DisplayType.Float:
						float val = float.Parse(Text);
						byte[] bytes = BitConverter.GetBytes(val);
						return BitConverter.ToInt32(bytes, 0);
				}
			}
		}
	}
}
