using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

using System;
using System.Collections.Generic;
using System.Globalization;

namespace BizHawk.Client.Common
{
	public abstract partial class Watch
	{
		#region Fields

		protected long _address;
		protected MemoryDomain _domain;
		protected DisplayType _type;
		protected bool _bigEndian;
		protected int _changecount;
		protected string _notes = string.Empty;

		#endregion

		#region Methods

		#region Static

		/// <summary>
		/// Generate a <see cref="Watch"/> from a given string
		/// String is tab separate
		/// </summary>
		/// <param name="line">Entire string, tab seperated for each value Order is:
		/// <list type="number">
		/// <item>
		/// <term>0x00</term>
		/// <description>Address in hexadecimal</description>
		/// </item>
		/// <item>
		/// <term>b,w or d</term>
		/// <description>The <see cref="WatchSize"/>, byte, word or double word</description>
		/// <term>s, u, h, b, 1, 2, 3, f</term>
		/// <description>The <see cref="DisplayType"/> signed, unsigned,etc...</description>
		/// </item>
		/// <item>
		/// <term>0 or 1</term>
		/// <description>Big endian or not</description>
		/// </item>
		/// <item>
		/// <term>RDRAM,ROM,...</term>
		/// <description>The <see cref="IMemoryDomains"/></description>
		/// </item>
		/// <item>
		/// <term>Plain text</term>
		/// <description>Notes</description>
		/// </item>
		/// </list>
		/// </param>
		/// <param name="domains"><see cref="Watch"/>'s memory domain</param>
		/// <returns></returns>
		public static Watch FromString(string line, IMemoryDomains domains)
		{
			string[] parts = line.Split(new char[] { '\t' }, 6);

			if (parts.Length < 6)
			{
				if (parts.Length >= 3 && parts[2] == "_")
				{
					return SeparatorWatch.Instance;
				}

				return null;
			}
			long address;

			if (long.TryParse(parts[0], NumberStyles.HexNumber, CultureInfo.CurrentCulture, out address))
			{
				WatchSize size = Watch.SizeFromChar(parts[1][0]);
				DisplayType type = Watch.DisplayTypeFromChar(parts[2][0]);
				bool bigEndian = parts[3] == "0" ? false : true;
				MemoryDomain domain = domains[parts[4]];
				string notes = parts[5].Trim(new char[] { '\r', '\n' });

				return Watch.GenerateWatch(
					domain,
					address,
					size,
					type,
					notes,
					bigEndian
					);
			}
			else
			{
				return null;
			}
		}

		#endregion Static

		/// <summary>
		/// Transform the current instance into a string
		/// </summary>
		/// <returns>A <see cref="string"/> representation of the current <see cref="Watch"/></returns>
		public override string ToString()
		{
			return string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}"
				, (Address ?? 0).ToHexString((Domain.Size - 1).NumHexDigits())
				, SizeAsChar
				, TypeAsChar
				, BigEndian
				, DomainName
				, Notes.Trim('\r', '\n')
				);
		}

		#endregion

		#region Properties

		#region Abstracts

		public abstract uint MaxValue { get; }
		public abstract int? Value { get; }
		//zero 15-nov-2015 - bypass LIAR LOGIC, see fdc9ea2aa922876d20ba897fb76909bf75fa6c92 https://github.com/TASVideos/BizHawk/issues/326
		public abstract int? ValueNoFreeze { get; }
		public abstract string ValueString { get; }
		public abstract WatchSize Size { get; }
		public abstract bool Poke(string value);
		public abstract int? Previous { get; }
		public abstract string PreviousStr { get; }
		public abstract void ResetPrevious();

		#endregion Abstracts

		#region Virtual

		/// <summary>
		/// Gets the address in the <see cref="IMemoryDomains"/>
		/// </summary>
		public virtual long? Address
		{
			get
			{
				return _address;
			}
		}

		/// <summary>
		/// Gets or sets the endianess of current <see cref="Watch"/>
		/// True for big endian, flase for little endian
		/// </summary>
		public virtual bool BigEndian
		{
			get
			{
				return _bigEndian;
			}
			set
			{
				_bigEndian = value;
			}
		}

		/// <summary>
		/// Gets or set the way current <see cref="Watch"/> is displayed
		/// </summary>
		public virtual DisplayType Type
		{
			get
			{
				return _type;
			}
			set
			{
				_type = value;
			}
		}

		/// <summary>
		/// Gets the address in the <see cref="IMemoryDomains"/> formatted as string
		/// </summary>
		public virtual string AddressString
		{
			get
			{
				return _address.ToString(AddressFormatStr);
			}
		}

		/// <summary>
		/// Gets a value that defined if the current <see cref="Watch"/> is actually a <see cref="SeparatorWatch"/>
		/// </summary>
		public virtual bool IsSeparator
		{
			get
			{
				return false;
			}
		}

		#endregion Virtual


		public MemoryDomain Domain { get { return _domain; } set { _domain = value; } }

		public string DomainName { get { return _domain != null ? _domain.Name : string.Empty; } }

		#endregion

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

		protected byte GetByte(bool bypassFreeze = false)
		{
			if (!bypassFreeze && Global.CheatList.IsActive(_domain, _address))
			{
				//LIAR logic
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

		protected ushort GetWord(bool bypassFreeze = false)
		{
			if (!bypassFreeze && Global.CheatList.IsActive(_domain, _address))
			{
				//LIAR logic
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

		protected uint GetDWord(bool bypassFreeze = false)
		{
			if (!bypassFreeze && Global.CheatList.IsActive(_domain, _address))
			{
				//LIAR logic
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
			if (_domain.Size == 0)
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
}
