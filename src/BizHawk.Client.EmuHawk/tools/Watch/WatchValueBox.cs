using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.CustomControls;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Client.EmuHawk
{
	public class WatchValueBox : ClipboardEventTextBox, INumberBox
	{
		private WatchSize _size = WatchSize.Byte;
		private WatchDisplayType _type = WatchDisplayType.Hex;

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
						_type = WatchDisplayType.Unsigned;
					}

					ResetText();
				}
			}
		}

		public WatchDisplayType Type
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

		private const double Max12_4 = short.MaxValue / 16.0;
		private const double Min12_4 = short.MinValue / 16.0;

		private const double Max20_12 = int.MaxValue / 4096.0;
		private const double Min20_12 = int.MinValue / 4096.0;

		private const double Max16_16 = int.MaxValue / 65536.0;
		private const double Min16_16 = int.MinValue / 65536.0;

		private const double _12_4_Unit = 1 / 16.0;

		private const double _20_12_Unit = 1 / 4096.0;

		private const double _16_16_Unit = 1 / 65536.0;

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
					case WatchDisplayType.Signed:
					case WatchDisplayType.Unsigned:
						Text = "0";
						break;
					case WatchDisplayType.Hex:
						Text = 0.ToHexString(MaxLength);
						break;
					case WatchDisplayType.FixedPoint_12_4:
					case WatchDisplayType.FixedPoint_20_12:
					case WatchDisplayType.FixedPoint_16_16:
					case WatchDisplayType.Float:
						Text = "0.0";
						break;
					case WatchDisplayType.Binary:
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
				case WatchDisplayType.Binary:
					MaxLength = _size switch
					{
						WatchSize.Byte => 8,
						WatchSize.Word => 16,
						WatchSize.DWord => 32,
						_ => 8
					};
					break;
				case WatchDisplayType.Hex:
					MaxLength = _size switch
					{
						WatchSize.Byte => 2,
						WatchSize.Word => 4,
						WatchSize.DWord => 8,
						_ => 2
					};
					break;
				case WatchDisplayType.Signed:
					MaxLength = _size switch
					{
						WatchSize.Byte => 4,
						WatchSize.Word => 6,
						WatchSize.DWord => 11,
						_ => 4
					};
					break;
				case WatchDisplayType.Unsigned:
					MaxLength = _size switch
					{
						WatchSize.Byte => 3,
						WatchSize.Word => 5,
						WatchSize.DWord => 10,
						_ => 3
					};
					break;
				case WatchDisplayType.FixedPoint_12_4:
					MaxLength = 10;
					break;
				case WatchDisplayType.Float:
					MaxLength = 40;
					break;
				case WatchDisplayType.FixedPoint_20_12:
				case WatchDisplayType.FixedPoint_16_16:
					MaxLength = 24;
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
					case WatchDisplayType.Signed:
						int val = ToRawInt() ?? 0;
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
					case WatchDisplayType.Unsigned:
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
					case WatchDisplayType.Binary:
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
					case WatchDisplayType.Hex:
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
					case WatchDisplayType.FixedPoint_12_4:
						var f12val = double.Parse(text, NumberFormatInfo.InvariantInfo);
						if (f12val > Max12_4 - _12_4_Unit)
						{
							f12val = Min12_4;
						}
						else
						{
							f12val += _12_4_Unit;
						}

						Text = f12val.ToString(NumberFormatInfo.InvariantInfo);
						break;
					case WatchDisplayType.FixedPoint_20_12:
						var f20val = double.Parse(text, NumberFormatInfo.InvariantInfo);
						if (f20val > Max20_12 - _20_12_Unit)
						{
							f20val = Min20_12;
						}
						else
						{
							f20val += _20_12_Unit;
						}

						Text = f20val.ToString(NumberFormatInfo.InvariantInfo);
						break;
					case WatchDisplayType.FixedPoint_16_16:
						var f16val = double.Parse(text, NumberFormatInfo.InvariantInfo);
						if (f16val > Max16_16 - _16_16_Unit)
						{
							f16val = Min16_16;
						}
						else
						{
							f16val += _16_16_Unit;
						}

						Text = f16val.ToString(NumberFormatInfo.InvariantInfo);
						break;
					case WatchDisplayType.Float:
						var dVal = double.Parse(text, NumberFormatInfo.InvariantInfo);
						if (dVal > float.MaxValue - 1)
						{
							dVal = 0;
						}
						else
						{
							dVal++;
						}

						Text = dVal.ToString(NumberFormatInfo.InvariantInfo);
						break;
				}
			}
			else if (e.KeyCode == Keys.Down)
			{
				switch (_type)
				{
					default:
					case WatchDisplayType.Signed:
						int val = ToRawInt() ?? 0;
						if (val == MinSignedInt)
						{
							val = MaxSignedInt;
						}
						else
						{
							val--;
						}

						Text = val.ToString();
						break;
					case WatchDisplayType.Unsigned:
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
					case WatchDisplayType.Binary:
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
					case WatchDisplayType.Hex:
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
					case WatchDisplayType.FixedPoint_12_4:
						var f12val = double.Parse(text, NumberFormatInfo.InvariantInfo);
						if (f12val < Min12_4 + _12_4_Unit)
						{
							f12val = Max12_4;
						}
						else
						{
							f12val -= _12_4_Unit;
						}

						Text = f12val.ToString(NumberFormatInfo.InvariantInfo);
						break;
					case WatchDisplayType.FixedPoint_20_12:
						var f20val = double.Parse(text, NumberFormatInfo.InvariantInfo);
						if (f20val < Min20_12 + _20_12_Unit)
						{
							f20val = Max20_12;
						}
						else
						{
							f20val -= _20_12_Unit;
						}

						Text = f20val.ToString(NumberFormatInfo.InvariantInfo);
						break;
					case WatchDisplayType.FixedPoint_16_16:
						var f16val = double.Parse(text, NumberFormatInfo.InvariantInfo);
						if (f16val < Min16_16 + _16_16_Unit)
						{
							f16val = Max16_16;
						}
						else
						{
							f16val -= _16_16_Unit;
						}

						Text = f16val.ToString(NumberFormatInfo.InvariantInfo);
						break;
					case WatchDisplayType.Float:
						var dval = double.Parse(text, NumberFormatInfo.InvariantInfo);
						if (dval < float.MinValue + 1)
						{
							dval = 0;
						}
						else
						{
							dval--;
						}

						Text = dval.ToString(NumberFormatInfo.InvariantInfo);
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

			base.OnTextChanged(e);
		}

		protected override void OnPaste(PasteEventArgs e)
		{
			if (Type is WatchDisplayType.Hex && e.ContainsText)
			{
				string text = e.Text.CleanHex();
				PasteWithMaxLength(text);
				e.Handled = true;
			}

			base.OnPaste(e);
		}

		public int? ToRawInt()
		{
			try
			{
				return _type switch
				{
					WatchDisplayType.Signed => int.Parse(Text),
					WatchDisplayType.Unsigned => (int)uint.Parse(Text),
					WatchDisplayType.Binary => Convert.ToInt32(Text, 2),
					WatchDisplayType.Hex => int.Parse(Text, NumberStyles.HexNumber),
					WatchDisplayType.FixedPoint_12_4 => (int)(double.Parse(Text, NumberFormatInfo.InvariantInfo) * 16.0),
					WatchDisplayType.FixedPoint_20_12 => (int)(double.Parse(Text, NumberFormatInfo.InvariantInfo) * 4096.0),
					WatchDisplayType.FixedPoint_16_16 => (int)(double.Parse(Text, NumberFormatInfo.InvariantInfo) * 65536.0),
					WatchDisplayType.Float => (int)NumberExtensions.ReinterpretAsUInt32(float.Parse(Text, NumberFormatInfo.InvariantInfo)),
					_ => int.Parse(Text)
				};
			}
			catch
			{
				// ignored
			}

			return Nullable ? null : 0;
		}

		public void SetFromRawInt(int? val)
		{
			if (val is not int i)
			{
				Text = string.Empty;
				return;
			}
			Text = _type switch
			{
				WatchDisplayType.Signed => i.ToString(),
				WatchDisplayType.Unsigned => ((uint) i).ToString(),
				WatchDisplayType.Binary => Convert.ToString(i, toBase: 2).PadLeft(8 * (int) ByteSize, '0'),
				WatchDisplayType.Hex => i.ToHexString(MaxLength),
				WatchDisplayType.FixedPoint_12_4 => (i / 16.0).ToString("F5", NumberFormatInfo.InvariantInfo),
				WatchDisplayType.FixedPoint_20_12 => (i / 4096.0).ToString("F5", NumberFormatInfo.InvariantInfo),
				WatchDisplayType.FixedPoint_16_16 => (i / 65536.0).ToString("F5", NumberFormatInfo.InvariantInfo),
				WatchDisplayType.Float => NumberExtensions.ReinterpretAsF32((uint)i).ToString("F6", NumberFormatInfo.InvariantInfo),
				_ => i.ToString()
			};
		}
	}
}
