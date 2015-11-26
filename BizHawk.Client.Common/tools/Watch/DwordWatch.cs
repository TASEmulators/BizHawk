using BizHawk.Common.NumberExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public sealed class DWordWatch : Watch
	{
		private uint _value;
		private uint _previous;

		public DWordWatch(MemoryDomain domain, long address, DisplayType type, bool bigEndian, string notes)
		{
			_domain = domain;
			_address = address;
			_value = _previous = GetDWord();

			if (AvailableTypes(WatchSize.DWord).Contains(type))
			{
				_type = type;
			}

			_bigEndian = bigEndian;

			if (notes != null)
			{
				Notes = notes;
			}
		}

		public DWordWatch(MemoryDomain domain, long address, DisplayType type, bool bigEndian, uint prev, int changeCount, string notes = null)
			: this(domain, address, type, bigEndian, notes)
		{
			_previous = prev;
			_changecount = changeCount;
			_type = type;
			_bigEndian = bigEndian;
		}

		public override int? Value
		{
			get { return (int)GetDWord(); }
		}

		public override int? ValueNoFreeze
		{
			get { return (int)GetDWord(true); }
		}

		public override int? Previous
		{
			get { return (int)_previous; }
		}

		public override string PreviousStr
		{
			get { return FormatValue(_previous); }
		}

		public override void ResetPrevious()
		{
			_previous = GetWord();
		}

		public override WatchSize Size
		{
			get { return WatchSize.DWord; }
		}

		public static List<DisplayType> ValidTypes
		{
			get
			{
				return new List<DisplayType>
					{
					DisplayType.Unsigned, DisplayType.Signed, DisplayType.Hex, DisplayType.FixedPoint_20_12, DisplayType.FixedPoint_16_16, DisplayType.Float
				};
			}
		}

		public override uint MaxValue
		{
			get { return uint.MaxValue; }
		}

		public override string ValueString
		{
			get { return FormatValue(GetDWord()); }
		}

		public override string ToString()
		{
			return Notes + ": " + ValueString;
		}

		public string FormatValue(uint val)
		{
			switch (Type)
			{
				default:
				case DisplayType.Unsigned:
					return val.ToString();
				case DisplayType.Signed:
					return ((int)val).ToString();
				case DisplayType.Hex:
					return val.ToHexString(8);
				case DisplayType.FixedPoint_20_12:
					return string.Format("{0:0.######}", val / 4096.0);
				case DisplayType.FixedPoint_16_16:
					return string.Format("{0:0.######}", val / 65536.0);
				case DisplayType.Float:
					var bytes = BitConverter.GetBytes(val);
					var _float = BitConverter.ToSingle(bytes, 0);
					//return string.Format("{0:0.######}", _float);
					return _float.ToString(); // adelikat: decided that we like sci notation instead of spooky rounding
			}
		}

		public override bool Poke(string value)
		{
			try
			{
				uint val = 0;
				switch (Type)
				{
					case DisplayType.Unsigned:
						if (value.IsUnsigned())
						{
							val = (uint)int.Parse(value);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.Signed:
						if (value.IsSigned())
						{
							val = (uint)int.Parse(value);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.Hex:
						if (value.IsHex())
						{
							val = (uint)int.Parse(value, NumberStyles.HexNumber);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.FixedPoint_20_12:
						if (value.IsFixedPoint())
						{
							val = (uint)(int)(double.Parse(value) * 4096.0);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.FixedPoint_16_16:
						if (value.IsFixedPoint())
						{
							val = (uint)(int)(double.Parse(value) * 65536.0);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.Float:
						if (value.IsFloat())
						{
							var bytes = BitConverter.GetBytes(float.Parse(value));
							val = BitConverter.ToUInt32(bytes, 0);
						}
						else
						{
							return false;
						}

						break;
				}

				if (Global.CheatList.Contains(Domain, _address))
				{
					var cheat = Global.CheatList.FirstOrDefault(c => c.Address == _address && c.Domain == Domain);
					if (cheat != (Cheat)null)
					{
						cheat.PokeValue((int)val);
						PokeDWord(val);
						return true;
					}
				}

				PokeDWord(val);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public override string Diff
		{
			get { return FormatValue(_previous - _value); }
		}

		public override void Update()
		{
			switch (Global.Config.RamWatchDefinePrevious)
			{
				case PreviousType.Original:
					return;
				case PreviousType.LastChange:
					var temp = _value;
					_value = GetDWord();
					if (_value != temp)
					{
						_previous = _value;
						_changecount++;
					}

					break;
				case PreviousType.LastFrame:
					_previous = _value;
					_value = GetDWord();
					if (_value != Previous)
					{
						_changecount++;
					}

					break;
			}
		}
	}
}
