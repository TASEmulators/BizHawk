using System;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Common.StringExtensions;
using BizHawk.Common.NumberExtensions;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class WatchValueBox : TextBox, INumberBox
	{
		private WatchSize _size = WatchSize.Byte;
		private DisplayType _type = DisplayType.Hex;

		public WatchValueBox()
		{
			CharacterCasing = CharacterCasing.Upper;
		}

		public bool Nullable { get; set; } = true;

		public WatchSize ByteSize
		{
			get => _size;
			set
			{
				var changed = _size != value;
				
				_size = value;
				if (changed)
				{
					SetMaxLength();

					var isTypeCompatible = value switch
					{
						WatchSize.Byte => ByteWatch.ValidTypes.Any(t => t == _type),
						WatchSize.Word => WordWatch.ValidTypes.Any(t => t == _type),
						WatchSize.DWord => DWordWatch.ValidTypes.Any(t => t == _type),
						_ => false
					};

					if (!isTypeCompatible)
					{
						_type = DisplayType.Unsigned;
					}

					ResetText();
				}
			}
		}

		public DisplayType Type
		{
			get => _type;
			set
			{
				var val = ToRawInt();
				_type = value;
				SetMaxLength();
				SetFromRawInt(val);
			}
		}

		private uint MaxUnsignedInt =>
			ByteSize switch
			{
				WatchSize.Word => ushort.MaxValue,
				WatchSize.DWord => uint.MaxValue,
				_ => byte.MaxValue
			};

		private int MaxSignedInt =>
			ByteSize switch
			{
				WatchSize.Word => short.MaxValue,
				WatchSize.DWord => int.MaxValue,
				_ => sbyte.MaxValue
			};

		private int MinSignedInt =>
			ByteSize switch
			{
				WatchSize.Word => short.MinValue,
				WatchSize.DWord => int.MinValue,
				_ => sbyte.MinValue
			};

		private double Max12_4 => MaxUnsignedInt / 16.0;

		private double Max20_12 => MaxUnsignedInt / 4096.0;

		private double Max16_16 => MaxUnsignedInt / 65536.0;

		private static double _12_4_Unit => 1 / 16.0;

		private static double _20_12_Unit => 1 / 4096.0;

		private static double _16_16_Unit => 1 / 65536.0;

		public override void ResetText()
		{
			if (Nullable)
			{
				Text = "";
			}
			else
			{
				switch (Type)
				{
					default:
					case DisplayType.Signed:
					case DisplayType.Unsigned:
						Text = "0";
						break;
					case DisplayType.Hex:
						Text = 0.ToHexString(MaxLength);
						break;
					case DisplayType.FixedPoint_12_4:
					case DisplayType.FixedPoint_20_12:
					case DisplayType.FixedPoint_16_16:
					case DisplayType.Float:
						Text = "0.0";
						break;
					case DisplayType.Binary:
						Text = "0".PadLeft(((int)_size) * 8);
						break;
				}
			}
		}

		private void SetMaxLength()
		{
			switch (_type)
			{
				default:
					MaxLength = 8;
					break;
				case DisplayType.Binary:
					MaxLength = _size switch
					{
						WatchSize.Byte => 8,
						WatchSize.Word => 16,
						_ => 8
					};
					break;
				case DisplayType.Hex:
					MaxLength = _size switch
					{
						WatchSize.Byte => 2,
						WatchSize.Word => 4,
						WatchSize.DWord => 8,
						_ => 2
					};
					break;
				case DisplayType.Signed:
					MaxLength = _size switch
					{
						WatchSize.Byte => 4,
						WatchSize.Word => 6,
						WatchSize.DWord => 11,
						_ => 4
					};
					break;
				case DisplayType.Unsigned:
					MaxLength = _size switch
					{
						WatchSize.Byte => 3,
						WatchSize.Word => 5,
						WatchSize.DWord => 10,
						_ => 3
					};
					break;
				case DisplayType.FixedPoint_12_4:
					MaxLength = 9;
					break;
				case DisplayType.Float:
					MaxLength = 21;
					break;
				case DisplayType.FixedPoint_20_12:
				case DisplayType.FixedPoint_16_16:
					MaxLength = 64;
					break;
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
				if (Text.Contains(".") && !SelectedText.Contains("."))
				{
					e.Handled = true;
					return;
				}
			}
			else if (e.KeyChar == '-')
			{
				if (Text.Contains("-") && !SelectedText.Contains("-"))
				{
					e.Handled = true;
					return;
				}
			}

			switch (_type)
			{
				default:
				case DisplayType.Binary:
					if (!e.KeyChar.IsBinary())
					{
						e.Handled = true;
					}

					break;
				case DisplayType.FixedPoint_12_4:
				case DisplayType.FixedPoint_20_12:
				case DisplayType.FixedPoint_16_16:
					if (!e.KeyChar.IsFixedPoint())
					{
						e.Handled = true;
					}

					break;
				case DisplayType.Float:
					if (!e.KeyChar.IsFloat())
					{
						e.Handled = true;
					}

					break;
				case DisplayType.Hex:
					if (!e.KeyChar.IsHex())
					{
						e.Handled = true;
					}

					break;
				case DisplayType.Signed:
					if (!e.KeyChar.IsSigned())
					{
						e.Handled = true;
					}

					break;
				case DisplayType.Unsigned:
					if (!e.KeyChar.IsUnsigned())
					{
						e.Handled = true;
					}

					break;
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			var text = string.IsNullOrWhiteSpace(Text) ? "0" : Text;
			if (e.KeyCode == Keys.Up)
			{
				switch (_type)
				{
					default:
					case DisplayType.Signed:
						int? val = ToRawInt() ?? 0;
						if (val == MaxSignedInt)
						{
							val = MinSignedInt;
						}
						else
						{
							val++;
						}

						Text = val.ToString();
						break;
					case DisplayType.Unsigned:
						var uval = (uint)(ToRawInt() ?? 0);
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
					case DisplayType.Binary:
						var bVal = (uint)(ToRawInt() ?? 0);
						if (bVal == MaxUnsignedInt)
						{
							bVal = 0;
						}
						else
						{
							bVal++;
						}

						var numBits = ((int)ByteSize) * 8;
						Text = Convert.ToString(bVal, 2).PadLeft(numBits, '0');
						break;
					case DisplayType.Hex:
						var hexVal = (uint)(ToRawInt() ?? 0);
						if (hexVal == MaxUnsignedInt)
						{
							hexVal = 0;
						}
						else
						{
							hexVal++;
						}

						Text = hexVal.ToHexString(MaxLength);
						break;
					case DisplayType.FixedPoint_12_4:
						var f12val = double.Parse(text);
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
					case DisplayType.FixedPoint_20_12:
						var f24val = double.Parse(text);
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
					case DisplayType.FixedPoint_16_16:
						var f16val = double.Parse(text);
						if (f16val >= Max16_16 - _16_16_Unit)
						{
							f16val = 0;
						}
						else
						{
							f16val += _16_16_Unit;
						}

						Text = f16val.ToString();
						break;
					case DisplayType.Float:
						var dVal = double.Parse(text);
						if (dVal > double.MaxValue - 1)
						{
							dVal = 0;
						}
						else
						{
							dVal++;
						}

						Text = dVal.ToString();
						break;
				}
			}
			else if (e.KeyCode == Keys.Down)
			{
				switch (_type)
				{
					default:
					case DisplayType.Signed:
						var val = ToRawInt();
						if (!val.HasValue)
						{
							Text = "";
						}
						else if (val == MinSignedInt)
						{
							val = MaxSignedInt;
						}
						else
						{
							val--;
						}

						Text = val.ToString();
						break;
					case DisplayType.Unsigned:
						var uval = (uint)(ToRawInt() ?? 0);
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
					case DisplayType.Binary:
						var bVal = (uint)(ToRawInt() ?? 0);
						if (bVal == 0)
						{
							bVal = MaxUnsignedInt;
						}
						else
						{
							bVal--;
						}

						var numBits = ((int)ByteSize) * 8;
						Text = Convert.ToString(bVal, 2).PadLeft(numBits, '0');
						break;
					case DisplayType.Hex:
						var hexVal = (uint)(ToRawInt() ?? 0);
						if (hexVal == 0)
						{
							hexVal = MaxUnsignedInt;
						}
						else
						{
							hexVal--;
						}

						Text = hexVal.ToHexString(MaxLength);
						break;
					case DisplayType.FixedPoint_12_4:
						var f12val = double.Parse(text);
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
					case DisplayType.FixedPoint_20_12:
						var f24val = double.Parse(text);
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
					case DisplayType.FixedPoint_16_16:
						var f16val = double.Parse(text);
						if (f16val < 0 + _16_16_Unit)
						{
							f16val = Max16_16;
						}
						else
						{
							f16val -= _16_16_Unit;
						}

						Text = f16val.ToString();
						break;
					case DisplayType.Float:
						var dval = double.Parse(text);
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

		protected override void OnTextChanged(EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(Text))
			{
				ResetText();
				SelectAll();
				return;
			}

			switch (_type)
			{
				case DisplayType.Signed:
					Text = Text.OnlySigned();
					break;
				case DisplayType.Unsigned:
					Text = Text.OnlyUnsigned();
					break;
				case DisplayType.Binary:
					Text = Text.OnlyBinary();
					break;
				case DisplayType.Hex:
					Text = Text.OnlyHex();
					break;
				case DisplayType.FixedPoint_12_4:
				case DisplayType.FixedPoint_20_12:
				case DisplayType.FixedPoint_16_16:
					Text = Text.OnlyFixedPoint();
					break;
				case DisplayType.Float:
					Text = Text.OnlyFloat();
					break;
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

			switch (_type)
			{
				case DisplayType.Signed:
					if (Text.IsSigned())
					{
						return Text == "-" ? 0 : int.Parse(Text);
					}

					break;
				case DisplayType.Unsigned:
					if (Text.IsUnsigned())
					{
						return (int)uint.Parse(Text);
					}

					break;
				case DisplayType.Binary:
					if (Text.IsBinary())
					{
						return Convert.ToInt32(Text, 2);
					}

					break;
				case DisplayType.Hex:
					if (Text.IsHex())
					{
						return int.Parse(Text, NumberStyles.HexNumber);
					}

					break;
				case DisplayType.FixedPoint_12_4:
					if (Text.IsFixedPoint())
					{
						return (int)(double.Parse(Text) * 16.0);
					}

					break;
				case DisplayType.FixedPoint_20_12:
					if (Text.IsFixedPoint())
					{
						return (int)(double.Parse(Text) * 4096.0);
					}

					break;
				case DisplayType.FixedPoint_16_16:
					if (Text.IsFixedPoint())
					{
						return (int)(double.Parse(Text) * 65536.0);
					}

					break;
				case DisplayType.Float:
					if (Text.IsFloat())
					{
						if (Text == "-" || Text == ".")
						{
							return 0;
						}

						float val = float.Parse(Text);
						var bytes = BitConverter.GetBytes(val);
						return BitConverter.ToInt32(bytes, 0);
					}

					break;
			}

			return 0;
		}

		public void SetFromRawInt(int? val)
		{
			if (val.HasValue)
			{
				switch (_type)
				{
					default:
					case DisplayType.Signed:
						Text = val.ToString();
						break;
					case DisplayType.Unsigned:
						var uval = (uint)val.Value;
						Text = uval.ToString();
						break;
					case DisplayType.Binary:
						var bVal = (uint)val.Value;
						var numBits = ((int)ByteSize) * 8;
						Text = Convert.ToString(bVal, 2).PadLeft(numBits, '0');
						break;
					case DisplayType.Hex:
						Text = val.Value.ToHexString(MaxLength);
						break;
					case DisplayType.FixedPoint_12_4:
						Text = $"{val.Value / 16.0:F5}";
						break;
					case DisplayType.FixedPoint_20_12:
						Text = $"{val.Value / 4096.0:F5}";
						break;
					case DisplayType.FixedPoint_16_16:
						Text = $"{val.Value / 65536.0:F5}";
						break;
					case DisplayType.Float:
						var bytes = BitConverter.GetBytes(val.Value);
						float _float = BitConverter.ToSingle(bytes, 0);
						Text = $"{_float:F6}";
						break;
				}
			}
			else
			{
				Text = "";
			}
		}
	}
}
