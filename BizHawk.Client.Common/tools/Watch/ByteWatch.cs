using BizHawk.Common.NumberExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BizHawk.Client.Common
{
	public sealed class ByteWatch : Watch
	{
		private byte _previous;
		private byte _value;

		public ByteWatch(MemoryDomain domain, long address, DisplayType type, bool bigEndian, string notes)
		{
			_address = address;
			_domain = domain;
			_value = _previous = GetByte();
			if (AvailableTypes(WatchSize.Byte).Contains(type))
			{
				_type = type;
			}

			_bigEndian = bigEndian;
			if (notes != null)
			{
				Notes = notes;
			}
		}

		public ByteWatch(MemoryDomain domain, long address, DisplayType type, bool bigEndian, byte prev, int changeCount, string notes = null)
			: this(domain, address, type, bigEndian, notes)
		{
			_previous = prev;
			_changecount = changeCount;
		}

		public override long? Address
		{
			get { return _address; }
		}

		public override int? Value
		{
			get { return GetByte(); }
		}

		public override int? ValueNoFreeze
		{
			get { return GetByte(true); }
		}

		public override string ValueString
		{
			get { return FormatValue(GetByte()); }
		}

		public override int? Previous
		{
			get { return _previous; }
		}

		public override string PreviousStr
		{
			get { return FormatValue(_previous); }
		}

		public override void ResetPrevious()
		{
			_previous = GetByte();
		}

		public override string ToString()
		{
			return Notes + ": " + ValueString;
		}

		public override bool IsSeparator
		{
			get { return false; }
		}

		public override WatchSize Size
		{
			get { return WatchSize.Byte; }
		}

		public static List<DisplayType> ValidTypes
		{
			get
			{
				return new List<DisplayType>
				{
					DisplayType.Unsigned, DisplayType.Signed, DisplayType.Hex, DisplayType.Binary
				};
			}
		}

		public override uint MaxValue
		{
			get { return byte.MaxValue; }
		}

		public string FormatValue(byte val)
		{
			switch (Type)
			{
				default:
				case DisplayType.Unsigned:
					return val.ToString();
				case DisplayType.Signed:
					return ((sbyte)val).ToString();
				case DisplayType.Hex:
					return val.ToHexString(2);
				case DisplayType.Binary:
					return Convert.ToString(val, 2).PadLeft(8, '0').Insert(4, " ");
			}
		}

		public override bool Poke(string value)
		{
			try
			{
				byte val = 0;
				switch (Type)
				{
					case DisplayType.Unsigned:
						if (value.IsUnsigned())
						{
							val = (byte)int.Parse(value);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.Signed:
						if (value.IsSigned())
						{
							val = (byte)(sbyte)int.Parse(value);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.Hex:
						if (value.IsHex())
						{
							val = (byte)int.Parse(value, NumberStyles.HexNumber);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.Binary:
						if (value.IsBinary())
						{
							val = (byte)Convert.ToInt32(value, 2);
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
						cheat.PokeValue(val);
						PokeByte(val);
						return true;
					}
				}

				PokeByte(val);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public override string Diff
		{
			get
			{
				var diff = string.Empty;
				var diffVal = _value - _previous;
				if (diffVal > 0)
				{
					diff = "+";
				}
				else if (diffVal < 0)
				{
					diff = "-";
				}

				return diff + FormatValue((byte)(_previous - _value));
			}
		}

		public override void Update()
		{
			switch (Global.Config.RamWatchDefinePrevious)
			{
				case PreviousType.Original:
					return;
				case PreviousType.LastChange:
					var temp = _value;
					_value = GetByte();
					if (_value != temp)
					{
						_previous = _value;
						_changecount++;
					}

					break;
				case PreviousType.LastFrame:
					_previous = _value;
					_value = GetByte();
					if (_value != Previous)
					{
						_changecount++;
					}

					break;
			}
		}
	}
}
