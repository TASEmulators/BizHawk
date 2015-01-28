using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public abstract class Watch
	{
		public enum WatchSize { Byte = 1, Word = 2, DWord = 4, Separator = 0 }
		public enum DisplayType { Separator, Signed, Unsigned, Hex, Binary, FixedPoint_12_4, FixedPoint_20_12, FixedPoint_16_16, Float }
		public enum PreviousType { Original = 0, LastSearch = 1, LastFrame = 2, LastChange = 3 }

		public static Watch FromString(string line, IMemoryDomains domains)
		{
			try
			{
				var parts = line.Split(new[] { '\t' }, 6);

				if (parts.Length < 6)
				{
					if (parts.Length >= 3 && parts[2] == "_")
					{
						return SeparatorWatch.Instance;
					}

					return null;
				}

				var address = long.Parse(parts[0], NumberStyles.HexNumber);
				var size = Watch.SizeFromChar(parts[1][0]);
				var type = Watch.DisplayTypeFromChar(parts[2][0]);
				var bigEndian = parts[3] == "0" ? false : true;
				var domain = domains[parts[4]];
				var notes = parts[5].Trim(new[] { '\r', '\n' });

				return Watch.GenerateWatch(
					domain,
					address,
					size,
					type,
					notes,
					bigEndian
					);
			}
			catch
			{
				return null;
			}
		}

		public static string ToString(Watch watch, MemoryDomain domain)
		{
			var numDigits = (domain.Size - 1).NumHexDigits();

			var sb = new StringBuilder();

			sb
				.Append((watch.Address ?? 0).ToHexString(numDigits)).Append('\t')
				.Append(watch.SizeAsChar).Append('\t')
				.Append(watch.TypeAsChar).Append('\t')
				.Append(watch.BigEndian ? '1' : '0').Append('\t')
				.Append(watch.DomainName).Append('\t')
				.Append(watch.Notes.Trim(new[] { '\r', '\n' }));

			return sb.ToString();
		}

		public static string DisplayTypeToString(DisplayType type)
		{
			switch (type)
			{
				default:
					return type.ToString();
				case DisplayType.FixedPoint_12_4:
					return "Fixed Point 12.4";
				case DisplayType.FixedPoint_20_12:
					return "Fixed Point 20.12";
				case DisplayType.FixedPoint_16_16:
					return "Fixed Point 16.16";
			}
		}

		public static DisplayType StringToDisplayType(string name)
		{
			switch (name)
			{
				default:
					return (DisplayType)Enum.Parse(typeof(DisplayType), name);
				case "Fixed Point 12.4":
					return DisplayType.FixedPoint_12_4;
				case "Fixed Point 20.12":
					return DisplayType.FixedPoint_20_12;
				case "Fixed Point 16.16":
					return DisplayType.FixedPoint_16_16;
			}
		}

		protected long _address;
		protected MemoryDomain _domain;
		protected DisplayType _type;
		protected bool _bigEndian;
		protected int _changecount;
		protected string _notes = string.Empty;

		public abstract int? Value { get; }
		public abstract string ValueString { get; }
		public abstract WatchSize Size { get; }

		public abstract uint MaxValue { get; }

		public abstract int? Previous { get; }
		public abstract string PreviousStr { get; }
		public abstract void ResetPrevious();

		public abstract bool Poke(string value);

		public virtual DisplayType Type { get { return _type; } set { _type = value; } }
		public virtual bool BigEndian { get { return _bigEndian; } set { _bigEndian = value; } }

		public MemoryDomain Domain { get { return _domain; } set { _domain = value; } }

		public string DomainName { get { return _domain != null ? _domain.Name : string.Empty; } }

		public virtual long? Address { get { return _address; } }

		public virtual string AddressString { get { return _address.ToString(AddressFormatStr); } }

		public virtual bool IsSeparator { get { return false; } }

		public char SizeAsChar
		{
			get
			{
				switch (Size)
				{
					default:
					case WatchSize.Separator:
						return 'S';
					case WatchSize.Byte:
						return 'b';
					case WatchSize.Word:
						return 'w';
					case WatchSize.DWord:
						return 'd';
				}
			}
		}

		public static WatchSize SizeFromChar(char c)
		{
			switch (c)
			{
				default:
				case 'S':
					return WatchSize.Separator;
				case 'b':
					return WatchSize.Byte;
				case 'w':
					return WatchSize.Word;
				case 'd':
					return WatchSize.DWord;
			}
		}

		public char TypeAsChar
		{
			get
			{
				switch (Type)
				{
					default:
					case DisplayType.Separator:
						return '_';
					case DisplayType.Unsigned:
						return 'u';
					case DisplayType.Signed:
						return 's';
					case DisplayType.Hex:
						return 'h';
					case DisplayType.Binary:
						return 'b';
					case DisplayType.FixedPoint_12_4:
						return '1';
					case DisplayType.FixedPoint_20_12:
						return '2';
					case DisplayType.FixedPoint_16_16:
						return '3';
					case DisplayType.Float:
						return 'f';
				}
			}
		}

		public static DisplayType DisplayTypeFromChar(char c)
		{
			switch (c)
			{
				default:
				case '_':
					return DisplayType.Separator;
				case 'u':
					return DisplayType.Unsigned;
				case 's':
					return DisplayType.Signed;
				case 'h':
					return DisplayType.Hex;
				case 'b':
					return DisplayType.Binary;
				case '1':
					return DisplayType.FixedPoint_12_4;
				case '2':
					return DisplayType.FixedPoint_20_12;
				case '3':
					return DisplayType.FixedPoint_16_16;
				case 'f':
					return DisplayType.Float;
			}
		}

		public string AddressFormatStr
		{
			get
			{
				if (_domain != null)
				{
					return "X" + (_domain.Size - 1).NumHexDigits();
				}
				
				return string.Empty;
			}
		}

		protected byte GetByte()
		{
			if (Global.CheatList.IsActive(_domain, _address))
			{
				return Global.CheatList.GetByteValue(_domain, _address).Value;
			}
			else
			{
				if (_domain.Size == 0)
				{
					return _domain.PeekByte(_address);
				}
				else
				{
					return _domain.PeekByte(_address % _domain.Size);
				}
			}
		}

		protected ushort GetWord()
		{
			if (Global.CheatList.IsActive(_domain, _address))
			{
				return (ushort)Global.CheatList.GetCheatValue(_domain, _address, WatchSize.Word).Value;
			}
			else
			{
				if (_domain.Size == 0)
				{
					return _domain.PeekWord(_address, _bigEndian);
				}
				else
				{
					return _domain.PeekWord(_address % _domain.Size, _bigEndian); // TODO: % size stil lisn't correct since it could be the last byte of the domain
				}
			}
		}

		protected uint GetDWord()
		{
			if (Global.CheatList.IsActive(_domain, _address))
			{
				return (uint)Global.CheatList.GetCheatValue(_domain, _address, WatchSize.DWord).Value;
			}
			else
			{
				if (_domain.Size == 0)
				{
					return _domain.PeekDWord(_address, _bigEndian); // TODO: % size stil lisn't correct since it could be the last byte of the domain
				}
				else
				{
					return _domain.PeekDWord(_address % _domain.Size, _bigEndian); // TODO: % size stil lisn't correct since it could be the last byte of the domain
				}
			}
		}

		protected void PokeByte(byte val)
		{
			if(_domain.Size == 0)
				_domain.PokeByte(_address, val);
			else _domain.PokeByte(_address % _domain.Size, val);
		}

		protected void PokeWord(ushort val)
		{
			if (_domain.Size == 0)
				_domain.PokeWord(_address, val, _bigEndian); // TODO: % size stil lisn't correct since it could be the last byte of the domain
			else _domain.PokeWord(_address % _domain.Size, val, _bigEndian); // TODO: % size stil lisn't correct since it could be the last byte of the domain
		}

		protected void PokeDWord(uint val)
		{
			if (_domain.Size == 0)
				_domain.PokeDWord(_address, val, _bigEndian); // TODO: % size stil lisn't correct since it could be the last byte of the domain
			else _domain.PokeDWord(_address % _domain.Size, val, _bigEndian); // TODO: % size stil lisn't correct since it could be the last byte of the domain
		}

		public void ClearChangeCount() { _changecount = 0; }

		public bool IsOutOfRange
		{
			get
			{
				return !IsSeparator && (Domain.Size != 0 && Address.Value >= Domain.Size);
			}
		}

		public string Notes { get { return _notes; } set { _notes = value; } }

		public static Watch GenerateWatch(MemoryDomain domain, long address, WatchSize size, DisplayType type, string notes, bool bigEndian)
		{
			switch (size)
			{
				default:
				case WatchSize.Separator:
					return SeparatorWatch.Instance;
				case WatchSize.Byte:
					return new ByteWatch(domain, address, type, bigEndian, notes);
				case WatchSize.Word:
					return new WordWatch(domain, address, type, bigEndian, notes);
				case WatchSize.DWord:
					return new DWordWatch(domain, address, type, bigEndian, notes);
			}
		}

		public static Watch GenerateWatch(MemoryDomain domain, long address, WatchSize size, DisplayType type, bool bigendian, long prev, int changecount)
		{
			switch (size)
			{
				default:
				case WatchSize.Separator:
					return SeparatorWatch.Instance;
				case WatchSize.Byte:
					return new ByteWatch(domain, address, type, bigendian, (byte)prev, changecount);
				case WatchSize.Word:
					return new WordWatch(domain, address, type, bigendian, (ushort)prev, changecount);
				case WatchSize.DWord:
					return new DWordWatch(domain, address, type, bigendian, (uint)prev, changecount);
			}
		}

		public static List<DisplayType> AvailableTypes(WatchSize size)
		{
			switch (size)
			{
				default:
				case WatchSize.Separator:
					return SeparatorWatch.ValidTypes;
				case WatchSize.Byte:
					return ByteWatch.ValidTypes;
				case WatchSize.Word:
					return WordWatch.ValidTypes;
				case WatchSize.DWord:
					return DWordWatch.ValidTypes;
			}
		}

		public int ChangeCount { get { return _changecount; } }
		
		public abstract string Diff { get; }
		
		public abstract void Update();

		public override bool Equals(object obj)
		{
			if (obj is Watch)
			{
				var watch = obj as Watch;

				return this.Domain == watch.Domain && 
					this.Address == watch.Address && 
					this.Size == watch.Size &&
					this.Type == watch.Type &&
					this.Notes == watch.Notes;
			}

			if (obj is Cheat)
			{
				var cheat = obj as Cheat;
				return this.Domain == cheat.Domain && this.Address == cheat.Address;
			}

			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return this.Domain.GetHashCode() + (int)(this.Address ?? 0);
		}

		public static bool operator ==(Watch a, Watch b)
		{
			// If one is null, but not both, return false.
			if (((object)a == null) || ((object)b == null))
			{
				return false;
			}

			return a.Equals(b);
		}

		public static bool operator !=(Watch a, Watch b)
		{
			return !a.Equals(b);
		}

		public static bool operator ==(Watch a, Cheat b)
		{
			// If one is null, but not both, return false.
			if (((object)a == null) || ((object)b == null))
			{
				return false;
			}

			return a.Domain == b.Domain && a.Address == b.Address;
		}

		public static bool operator !=(Watch a, Cheat b)
		{
			return !(a == b);
		}
	}

	public sealed class SeparatorWatch : Watch
	{
		public static SeparatorWatch Instance
		{
			get { return new SeparatorWatch(); }
		}

		public override long? Address
		{
			get { return null; }
		}

		public override int? Value
		{
			get { return null; }
		}

		public override int? Previous
		{
			get { return null; }
		}

		public override string AddressString
		{
			get { return string.Empty; }
		}

		public override string ValueString
		{
			get { return string.Empty; }
		}

		public override string PreviousStr
		{
			get { return string.Empty; }
		}

		public override string ToString()
		{
			return "----";
		}

		public override bool IsSeparator
		{
			get { return true; }
		}

		public override WatchSize Size
		{
			get { return WatchSize.Separator; }
		}

		public static List<DisplayType> ValidTypes
		{
			get { return new List<DisplayType> { DisplayType.Separator }; }
		}

		public override DisplayType Type
		{
			get { return DisplayType.Separator; }
		}

		public override bool Poke(string value)
		{
			return false;
		}

		public override void ResetPrevious()
		{
			return;
		}

		public override string Diff { get { return string.Empty; } }

		public override uint MaxValue
		{
			get { return 0; }
		}

		public override void Update() { return; }
	}

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
						if ( value.IsUnsigned())
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

	public sealed class WordWatch : Watch
	{
		private ushort _previous;
		private ushort _value;

		public WordWatch(MemoryDomain domain, long address, DisplayType type, bool bigEndian, string notes)
		{
			_domain = domain;
			_address = address;
			_value = _previous = GetWord();

			if (AvailableTypes(WatchSize.Word).Contains(type))
			{
				_type = type;
			}

			_bigEndian = bigEndian;

			if (notes != null)
			{
				Notes = notes;
			}
		}

		public WordWatch(MemoryDomain domain, long address, DisplayType type, bool bigEndian, ushort prev, int changeCount, string notes = null)
			: this(domain, address, type, bigEndian, notes)
		{
			_previous = prev;
			_changecount = changeCount;
		}

		public override uint MaxValue
		{
			get { return ushort.MaxValue; }
		}

		public override int? Value
		{
			get { return GetWord(); }
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
			_previous = GetWord();
		}

		public override WatchSize Size
		{
			get { return WatchSize.Word; }
		}

		public static List<DisplayType> ValidTypes
		{
			get
			{
				return new List<DisplayType>
				{
					DisplayType.Unsigned, DisplayType.Signed, DisplayType.Hex, DisplayType.FixedPoint_12_4, DisplayType.Binary
				};
			}
		}

		public override string ValueString
		{
			get { return FormatValue(GetWord()); }
		}

		public override string ToString()
		{
			return Notes + ": " + ValueString;
		}

		public string FormatValue(ushort val)
		{
			switch (Type)
			{
				default:
				case DisplayType.Unsigned:
					return val.ToString();
				case DisplayType.Signed:
					return ((short)val).ToString();
				case DisplayType.Hex:
					return val.ToHexString(4);
				case DisplayType.FixedPoint_12_4:
					return string.Format("{0:F4}", val / 16.0);
				case DisplayType.Binary:
					return Convert.ToString(val, 2).PadLeft(16, '0').Insert(8, " ").Insert(4, " ").Insert(14, " ");
			}
		}

		public override bool Poke(string value)
		{
			try
			{
				ushort val = 0;
				switch (Type)
				{
					case DisplayType.Unsigned:
						if (value.IsUnsigned())
						{
							val = (ushort)int.Parse(value);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.Signed:
						if (value.IsSigned())
						{
							val = (ushort)(short)int.Parse(value);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.Hex:
						if (value.IsHex())
						{
							val = (ushort)int.Parse(value, NumberStyles.HexNumber);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.Binary:
						if (value.IsBinary())
						{
							val = (ushort)Convert.ToInt32(value, 2);
						}
						else
						{
							return false;
						}

						break;
					case DisplayType.FixedPoint_12_4:
						if (value.IsFixedPoint())
						{
							val = (ushort)(double.Parse(value) * 16.0);
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
						PokeWord(val);
						return true;
					}
				}

				PokeWord(val);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public override string Diff
		{
			get { return FormatValue((ushort)(_previous - _value)); }
		}

		public override void Update()
		{
			switch (Global.Config.RamWatchDefinePrevious)
			{
				case PreviousType.Original:
					return;
				case PreviousType.LastChange:
					var temp = _value;
					_value = GetWord();

					if (_value != temp)
					{
						_previous = temp;
						_changecount++;
					}

					break;
				case PreviousType.LastFrame:
					_previous = _value;
					_value = GetWord();
					if (_value != Previous)
					{
						_changecount++;
					}

					break;
			}
		}
	}

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
