using System;
using System.Globalization;
using System.Windows.Forms;

using BizHawk.Common.StringExtensions;
using BizHawk.Common.NumberExtensions;
using BizHawk.Client.Common;
using System.Linq;

namespace BizHawk.Client.EmuHawk
{
	public class WatchValueBox : TextBox, INumberBox
	{
		private WatchSize _size = WatchSize.Byte;
		private DisplayType _type = DisplayType.Hex;
		private bool _nullable = true;

		public WatchValueBox()
		{
			CharacterCasing = CharacterCasing.Upper;
		}

		public bool Nullable { get { return _nullable; } set { _nullable = value; } }

		public WatchSize ByteSize
		{
			get
			{
				return _size;
			}

			set
			{
				var changed = _size != value;
				
				_size = value;
				if (changed)
				{
					SetMaxLength();

					bool isTypeCompatible = false;
					switch (value)
					{
						case WatchSize.Byte:
							isTypeCompatible = ByteWatch.ValidTypes.Where(t => t == _type).Any();
							break;

						case WatchSize.Word:
							isTypeCompatible = WordWatch.ValidTypes.Where(t => t == _type).Any();
							break;

						case WatchSize.DWord:
							isTypeCompatible = DWordWatch.ValidTypes.Where(t => t == _type).Any();
							break;
					}

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
			get
			{
				return _type;
			}

			set
			{
				_type = value;
				var val = ToRawInt();
				SetMaxLength();
				SetFromRawInt(val);
			}
		}

		private uint MaxUnsignedInt
		{
			get
			{
				switch (ByteSize)
				{
					default:
					case WatchSize.Byte:
						return byte.MaxValue;
					case WatchSize.Word:
						return ushort.MaxValue;
					case WatchSize.DWord:
						return uint.MaxValue;
				}
			}
		}

		private int MaxSignedInt
		{
			get
			{
				switch (ByteSize)
				{
					default:
					case WatchSize.Byte:
						return sbyte.MaxValue;
					case WatchSize.Word:
						return short.MaxValue;
					case WatchSize.DWord:
						return int.MaxValue;
				}
			}
		}

		private int MinSignedInt
		{
			get
			{
				switch (ByteSize)
				{
					default:
					case WatchSize.Byte:
						return sbyte.MinValue;
					case WatchSize.Word:
						return short.MinValue;
					case WatchSize.DWord:
						return int.MinValue;
				}
			}
		}

		private double Max12_4
		{
			get { return MaxUnsignedInt / 16.0; }
		}

		private double Max20_12
		{
			get { return MaxUnsignedInt / 4096.0; }
		}

		private double Max16_16
		{
			get { return MaxUnsignedInt / 65536.0; }
		}

		private static double _12_4_Unit
		{
			get { return 1 / 16.0; }
		}

		private static double _20_12_Unit
		{
			get { return 1 / 4096.0; }
		}

		private static double _16_16_Unit
		{
			get { return 1 / 65536.0; }
		}

		public override void ResetText()
		{
			if (_nullable)
			{
				Text = string.Empty;
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
					switch (_size)
					{
						default:
						case WatchSize.Byte:
							MaxLength = 8;
							break;
						case WatchSize.Word:
							MaxLength = 16;
							break;
					}

					break;
				case DisplayType.Hex:
					switch (_size)
					{
						default:
						case WatchSize.Byte:
							MaxLength = 2;
							break;
						case WatchSize.Word:
							MaxLength = 4;
							break;
						case WatchSize.DWord:
							MaxLength = 8;
							break;
					}

					break;
				case DisplayType.Signed:
					switch (_size)
					{
						default:
						case WatchSize.Byte:
							MaxLength = 4;
							break;
						case WatchSize.Word:
							MaxLength = 6;
							break;
						case WatchSize.DWord:
							MaxLength = 11;
							break;
					}

					break;
				case DisplayType.Unsigned:
					switch (_size)
					{
						default:
						case WatchSize.Byte:
							MaxLength = 3;
							break;
						case WatchSize.Word:
							MaxLength = 5;
							break;
						case WatchSize.DWord:
							MaxLength = 10;
							break;
					}

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
						var bval = (uint)(ToRawInt() ?? 0);
						if (bval == MaxUnsignedInt)
						{
							bval = 0;
						}
						else
						{
							bval++;
						}

						var numBits = ((int)ByteSize) * 8;
						Text = Convert.ToString(bval, 2).PadLeft(numBits, '0');
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
						var dval = double.Parse(text);
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
					case DisplayType.Signed:
						var val = ToRawInt();
						if (!val.HasValue)
						{
							Text = string.Empty;
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
						var bval = (uint)(ToRawInt() ?? 0);
						if (bval == 0)
						{
							bval = MaxUnsignedInt;
						}
						else
						{
							bval--;
						}

						var numBits = ((int)ByteSize) * 8;
						Text = Convert.ToString(bval, 2).PadLeft(numBits, '0');
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
						if (Text == "-")
							return 0;
						return int.Parse(Text);
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
							return 0;
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
						var bval = (uint)val.Value;
						var numBits = ((int)ByteSize) * 8;
						Text = Convert.ToString(bval, 2).PadLeft(numBits, '0');
						break;
					case DisplayType.Hex:
						Text = val.Value.ToHexString(MaxLength);
						break;
					case DisplayType.FixedPoint_12_4:
						Text = string.Format("{0:F5}", val.Value / 16.0);
						break;
					case DisplayType.FixedPoint_20_12:
						Text = string.Format("{0:F5}", val.Value / 4096.0);
						break;
					case DisplayType.FixedPoint_16_16:
						Text = string.Format("{0:F5}", val.Value / 65536.0);
						break;
					case DisplayType.Float:
						var bytes = BitConverter.GetBytes(val.Value);
						float _float = BitConverter.ToSingle(bytes, 0);
						Text = string.Format("{0:F6}", _float);
						break;
				}
			}
			else
			{
				Text = string.Empty;
			}
		}
	}
}
