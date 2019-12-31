using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Diagnostics;

using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// This class holds a watch i.e. something inside a <see cref="MemoryDomain"/> identified by an address
	/// with a specific size (8, 16 or 32bits).
	/// This is an abstract class
	/// </summary>
	[DebuggerDisplay("Note={Notes}, Value={ValueString}")]
	public abstract class Watch
		: IEquatable<Watch>,
		IEquatable<Cheat>,
		IComparable<Watch>
	{
		private MemoryDomain _domain;
		private DisplayType _type;

		/// <summary>
		/// Initializes a new instance of the <see cref="Watch"/> class
		/// </summary>
		/// <param name="domain"><see cref="MemoryDomain"/> where you want to track</param>
		/// <param name="address">The address you want to track</param>
		/// <param name="size">A <see cref="WatchSize"/> (byte, word, double word)</param>
		/// <param name="type">How you you want to display the value See <see cref="DisplayType"/></param>
		/// <param name="bigEndian">Specify the endianess. true for big endian</param>
		/// <param name="note">A custom note about the <see cref="Watch"/></param>
		/// <exception cref="ArgumentException">Occurs when a <see cref="DisplayType"/> is incompatible with the <see cref="WatchSize"/></exception>
		protected Watch(MemoryDomain domain, long address, WatchSize size, DisplayType type, bool bigEndian, string note)
		{
			if (IsDiplayTypeAvailable(type))
			{
				_domain = domain;
				Address = address;
				Size = size;
				_type = type;
				BigEndian = bigEndian;
				Notes = note;
			}
			else
			{
				throw new ArgumentException($"{nameof(DisplayType)} {type} is invalid for this type of {nameof(Watch)}", nameof(type));
			}
		}

		#region Methods

		#region Static

		/// <summary>
		/// Generate sa <see cref="Watch"/> from a given string
		/// String is tab separate
		/// </summary>
		/// <param name="line">Entire string, tab separated for each value Order is:
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
		/// <returns>A brand new <see cref="Watch"/></returns>
		public static Watch FromString(string line, IMemoryDomains domains)
		{
			string[] parts = line.Split(new[] { '\t' }, 6);

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
				WatchSize size = SizeFromChar(parts[1][0]);
				DisplayType type = DisplayTypeFromChar(parts[2][0]);
				bool bigEndian = parts[3] != "0";
				MemoryDomain domain = domains[parts[4]];
				string notes = parts[5].Trim('\r', '\n');

				return GenerateWatch(
					domain,
					address,
					size,
					type,
					bigEndian,
					notes);
			}

			return null;
		}

		/// <summary>
		/// Generates a new <see cref="Watch"/> instance
		/// Can be either <see cref="ByteWatch"/>, <see cref="WordWatch"/>, <see cref="DWordWatch"/> or <see cref="SeparatorWatch"/>
		/// </summary>
		/// <param name="domain">The <see cref="MemoryDomain"/> where you want to watch</param>
		/// <param name="address">The address into the <see cref="MemoryDomain"/></param>
		/// <param name="size">The size</param>
		/// <param name="type">How the watch will be displayed</param>
		/// <param name="bigEndian">Endianess (true for big endian)</param>
		/// <param name="note">A custom note about the <see cref="Watch"/></param>
		/// <param name="value">The current watch value</param>
		/// <param name="prev">Previous value</param>
		/// <param name="changeCount">Number of changes occurs in current <see cref="Watch"/></param>
		/// <returns>New <see cref="Watch"/> instance. True type is depending of size parameter</returns>
		public static Watch GenerateWatch(MemoryDomain domain, long address, WatchSize size, DisplayType type, bool bigEndian, string note = "", long value = 0, long prev = 0, int changeCount = 0)
		{
			switch (size)
			{
				default:
				case WatchSize.Separator:
					return SeparatorWatch.NewSeparatorWatch(note);
				case WatchSize.Byte:
					return new ByteWatch(domain, address, type, bigEndian, note, (byte)value, (byte)prev, changeCount);
				case WatchSize.Word:
					return new WordWatch(domain, address, type, bigEndian, note, (ushort)value, (ushort)prev, changeCount);
				case WatchSize.DWord:
					return new DWordWatch(domain, address, type, bigEndian, note, (uint)value, (uint)prev, changeCount);
			}
		}

		#region Operators

		/// <summary>
		/// Equality operator between two <see cref="Watch"/>
		/// </summary>
		/// <param name="a">First watch</param>
		/// <param name="b">Second watch</param>
		/// <returns>True if both watch are equals; otherwise, false</returns>
		public static bool operator ==(Watch a, Watch b)
		{
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
			{
				return false;
			}

			if (ReferenceEquals(a, b))
			{
				return true;
			}

			return a.Equals(b);
		}

		/// <summary>
		/// Equality operator between a <see cref="Watch"/> and a <see cref="Cheat"/>
		/// </summary>
		/// <param name="a">The watch</param>
		/// <param name="b">The cheat</param>
		/// <returns>True if they are equals; otherwise, false</returns>
		public static bool operator ==(Watch a, Cheat b)
		{
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
			{
				return false;
			}

			return a.Equals(b);
		}

		/// <summary>
		/// Inequality operator between two <see cref="Watch"/>
		/// </summary>
		/// <param name="a">First watch</param>
		/// <param name="b">Second watch</param>
		/// <returns>True if both watch are different; otherwise, false</returns>
		public static bool operator !=(Watch a, Watch b)
		{
			return !(a == b);
		}

		/// <summary>
		/// Inequality operator between a <see cref="Watch"/> and a <see cref="Cheat"/>
		/// </summary>
		/// <param name="a">The watch</param>
		/// <param name="b">The cheat</param>
		/// <returns>True if they are different; otherwise, false</returns>
		public static bool operator !=(Watch a, Cheat b)
		{
			return !(a == b);
		}

		/// <summary>
		/// Compare two <see cref="Watch"/> together
		/// </summary>
		/// <param name="a">First <see cref="Watch"/></param>
		/// <param name="b">Second <see cref="Watch"/></param>
		/// <returns>True if first is lesser than b; otherwise, false</returns>
		/// <exception cref="InvalidOperationException">Occurs when you try to compare two <see cref="Watch"/> throughout different <see cref="MemoryDomain"/></exception>
		public static bool operator <(Watch a, Watch b)
		{
			return a.CompareTo(b) < 0;
		}

		/// <summary>
		/// Compare two <see cref="Watch"/> together
		/// </summary>
		/// <param name="a">First <see cref="Watch"/></param>
		/// <param name="b">Second <see cref="Watch"/></param>
		/// <returns>True if first is greater than b; otherwise, false</returns>
		/// <exception cref="InvalidOperationException">Occurs when you try to compare two <see cref="Watch"/> throughout different <see cref="MemoryDomain"/></exception>
		public static bool operator >(Watch a, Watch b)
		{
			return a.CompareTo(b) > 0;
		}

		/// <summary>
		/// Compare two <see cref="Watch"/> together
		/// </summary>
		/// <param name="a">First <see cref="Watch"/></param>
		/// <param name="b">Second <see cref="Watch"/></param>
		/// <returns>True if first is lesser or equals to b; otherwise, false</returns>
		/// <exception cref="InvalidOperationException">Occurs when you try to compare two <see cref="Watch"/> throughout different <see cref="MemoryDomain"/></exception>
		public static bool operator <=(Watch a, Watch b)
		{
			return a.CompareTo(b) <= 0;
		}

		/// <summary>
		/// Compare two <see cref="Watch"/> together
		/// </summary>
		/// <param name="a">First <see cref="Watch"/></param>
		/// <param name="b">Second <see cref="Watch"/></param>
		/// <returns>True if first is greater or equals to b; otherwise, false</returns>
		/// <exception cref="InvalidOperationException">Occurs when you try to compare two <see cref="Watch"/> throughout different <see cref="MemoryDomain"/></exception>
		public static bool operator >=(Watch a, Watch b)
		{
			return a.CompareTo(b) >= 0;
		}

		#endregion Operators

		#endregion Static

		#region Abstracts

		/// <summary>
		/// Gets a list a <see cref="DisplayType"/> that can be used for this <see cref="Watch"/>
		/// </summary>
		/// <returns>An enumeration that contains all valid <see cref="DisplayType"/></returns>
		public abstract IEnumerable<DisplayType> AvailableTypes();

		/// <summary>
		/// Resets the previous value; set it to the current one
		/// </summary>
		public abstract void ResetPrevious();

		/// <summary>
		/// Updates the Watch (read it from <see cref="MemoryDomain"/>
		/// </summary>
		public abstract void Update();

		#endregion Abstracts

		#region Protected

		protected byte GetByte(bool bypassFreeze = false)
		{
			if (!bypassFreeze && Global.CheatList.IsActive(_domain, Address))
			{
				// LIAR logic
				return Global.CheatList.GetByteValue(_domain, Address) ?? 0;
			}
			
			if (_domain.Size == 0)
			{
				return _domain.PeekByte(Address);
			}

			return _domain.PeekByte(Address % _domain.Size);
		}

		protected ushort GetWord(bool bypassFreeze = false)
		{
			if (!bypassFreeze && Global.CheatList.IsActive(_domain, Address))
			{
				// LIAR logic
				return (ushort)(Global.CheatList.GetCheatValue(_domain, Address, WatchSize.Word) ?? 0);
			}

			if (_domain.Size == 0)
			{
				return _domain.PeekUshort(Address, BigEndian);
			}

			return _domain.PeekUshort(Address % _domain.Size, BigEndian); // TODO: % size stil lisn't correct since it could be the last byte of the domain
		}

		protected uint GetDWord(bool bypassFreeze = false)
		{
			if (!bypassFreeze && Global.CheatList.IsActive(_domain, Address))
			{
				// LIAR logic
				return (uint)(Global.CheatList.GetCheatValue(_domain, Address, WatchSize.DWord) ?? 0);
			}

			if (_domain.Size == 0)
			{
				return _domain.PeekUint(Address, BigEndian); // TODO: % size still isn't correct since it could be the last byte of the domain
			}

			return _domain.PeekUint(Address % _domain.Size, BigEndian); // TODO: % size still isn't correct since it could be the last byte of the domain
		}

		protected void PokeByte(byte val)
		{
			if (_domain.Size == 0)
			{
				_domain.PokeByte(Address, val);
			}
			else
			{
				_domain.PokeByte(Address % _domain.Size, val);
			}
		}

		protected void PokeWord(ushort val)
		{
			if (_domain.Size == 0)
			{
				_domain.PokeUshort(Address, val, BigEndian); // TODO: % size still isn't correct since it could be the last byte of the domain
			}
			else
			{
				_domain.PokeUshort(Address % _domain.Size, val, BigEndian); // TODO: % size still isn't correct since it could be the last byte of the domain
			}
		}

		protected void PokeDWord(uint val)
		{
			if (_domain.Size == 0)
			{
				_domain.PokeUint(Address, val, BigEndian); // TODO: % size still isn't correct since it could be the last byte of the domain
			}
			else
			{
				_domain.PokeUint(Address % _domain.Size, val, BigEndian); // TODO: % size still isn't correct since it could be the last byte of the domain
			}
		}

		#endregion Protected

		/// <summary>
		/// Sets the number of changes to 0
		/// </summary>
		public void ClearChangeCount()
		{
			ChangeCount = 0;
		}

		#region IEquatable<Watch>

		/// <summary>
		/// Determines if this <see cref="Watch"/> is equals to another
		/// </summary>
		/// <param name="other">The <see cref="Watch"/> to compare</param>
		/// <returns>True if both object are equals; otherwise, false</returns>
		public bool Equals(Watch other)
		{
			if (ReferenceEquals(other, null))
			{
				return false;
			}

			return _domain == other._domain &&
				Address == other.Address &&
				Size == other.Size;
		}

		#endregion IEquatable<Watch>

		#region IEquatable<Cheat>

		/// <summary>
		/// Determines if this <see cref="Watch"/> is equals to an instance of <see cref="Cheat"/>
		/// </summary>
		/// <param name="other">The <see cref="Cheat"/> to compare</param>
		/// <returns>True if both object are equals; otherwise, false</returns>
		public bool Equals(Cheat other)
		{
			return !ReferenceEquals(other, null)
				&& _domain == other.Domain
				&& Address == other.Address
				&& Size == other.Size;
		}

		#endregion IEquatable<Cheat>

		#region  IComparable<Watch>

		/// <summary>
		/// Compares two <see cref="Watch"/> together and determine which one comes first.
		/// First we look the address and then the size
		/// </summary>
		/// <param name="other">The other <see cref="Watch"/> to compare to</param>
		/// <returns>0 if they are equals, 1 if the other is greater, -1 if the other is lesser</returns>
		/// <exception cref="InvalidOperationException">Occurs when you try to compare two <see cref="Watch"/> throughout different <see cref="MemoryDomain"/></exception>
		public int CompareTo(Watch other)
		{
			if (_domain != other._domain)
			{
				throw new InvalidOperationException("Watch cannot be compared through different domain");
			}

			if (Equals(other))
			{
				return 0;
			}

			if (Address.Equals(other.Address))
			{
				return ((int)Size).CompareTo((int)other.Size);
			}

			return Address.CompareTo(other.Address);
		}

		#endregion IComparable<Watch>

		/// <summary>
		/// Determines if this object is Equals to another
		/// </summary>
		/// <param name="obj">The object to compare</param>
		/// <returns>True if both object are equals; otherwise, false</returns>
		public override bool Equals(object obj)
		{
			if (obj is Watch)
			{
				return Equals((Watch)obj);
			}

			if (obj is Cheat)
			{
				return Equals((Cheat)obj);
			}

			return base.Equals(obj);
		}

		/// <summary>
		/// Hash the current watch and gets a unique value
		/// </summary>
		/// <returns><see cref="int"/> that can serves as a unique representation of current Watch</returns>
		public override int GetHashCode()
		{
			return Domain.GetHashCode() + (int)Address;
		}

		/// <summary>
		/// Determines if the specified <see cref="DisplayType"/> can be
		/// used for the current <see cref="Watch"/>
		/// </summary>
		/// <param name="type"><see cref="DisplayType"/> you want to check</param>
		public bool IsDiplayTypeAvailable(DisplayType type)
		{
			return AvailableTypes().Any(d => d == type);
		}

		/// <summary>
		/// Transforms the current instance into a string
		/// </summary>
		/// <returns>A <see cref="string"/> representation of the current <see cref="Watch"/></returns>
		public override string ToString()
		{
			return $"{(Domain == null && Address == 0 ? "0" : Address.ToHexString((Domain?.Size ?? 0xFF - 1).NumHexDigits()))}\t{SizeAsChar}\t{TypeAsChar}\t{Convert.ToInt32(BigEndian)}\t{Domain?.Name}\t{Notes.Trim('\r', '\n')}";
		}

		/// <summary>
		/// Transform the current instance into a displayable (short representation) string
		/// It's used by the "Display on screen" option in the RamWatch window
		/// </summary>
		/// <returns>A well formatted string representation</returns>
		public virtual string ToDisplayString()
		{
			return $"{Notes}: {ValueString}";
		}

		#endregion

		#region Properties

		#region Abstracts

		/// <summary>
		/// Gets a string representation of difference
		/// between current value and the previous one
		/// </summary>
		public abstract string Diff { get; }

		/// <summary>
		/// Gets the maximum possible value
		/// </summary>
		public abstract uint MaxValue { get; }

		/// <summary>
		/// Gets the current value
		/// </summary>
		public abstract int Value { get; }

		/// <summary>
		/// Gets the current value
		/// but with stuff I don't understand
		/// </summary>
		/// <remarks>zero 15-nov-2015 - bypass LIAR LOGIC, see fdc9ea2aa922876d20ba897fb76909bf75fa6c92 https://github.com/TASVideos/BizHawk/issues/326 </remarks>
		public abstract int ValueNoFreeze { get; }

		/// <summary>
		/// Gets a string representation of the current value
		/// </summary>
		public abstract string ValueString { get; }

		/// <summary>
		/// Try to sets the value into the <see cref="MemoryDomain"/>
		/// at the current <see cref="Watch"/> address
		/// </summary>
		/// <param name="value">Value to set</param>
		/// <returns>True if value successfully sets; otherwise, false</returns>
		public abstract bool Poke(string value);

		/// <summary>
		/// Gets the previous value
		/// </summary>
		public abstract int Previous { get; }

		/// <summary>
		/// Gets a string representation of the previous value
		/// </summary>
		public abstract string PreviousStr { get; }

		#endregion Abstracts

		/// <summary>
		/// Gets the address in the <see cref="MemoryDomain"/>
		/// </summary>
		public long Address { get; }

		private string AddressFormatStr
		{
			get
			{
				if (_domain != null)
				{
					return $"X{(_domain.Size - 1).NumHexDigits()}";
				}

				return "";
			}
		}

		/// <summary>
		/// Gets the address in the <see cref="MemoryDomain"/> formatted as string
		/// </summary>
		public string AddressString => Address.ToString(AddressFormatStr);

		/// <summary>
		/// Gets or sets a value indicating the endianess of current <see cref="Watch"/>
		/// True for big endian, false for little endian
		/// </summary>
		public bool BigEndian { get; set; }

		/// <summary>
		/// Gets or sets the number of times that value of current <see cref="Watch"/> value has changed
		/// </summary>
		public int ChangeCount { get; protected set; }

		/// <summary>
		/// Gets or sets the way current <see cref="Watch"/> is displayed
		/// </summary>
		/// <exception cref="ArgumentException">Occurs when a <see cref="DisplayType"/> is incompatible with the <see cref="WatchSize"/></exception>
		public DisplayType Type
		{
			get
			{
				return _type;
			}

			set
			{
				if (IsDiplayTypeAvailable(value))
				{
					_type = value;
				}
				else
				{
					throw new ArgumentException($"DisplayType {value} is invalid for this type of Watch");
				}
			}
		}

		/// <value>the domain of <see cref="Address"/></value>
		/// <exception cref="InvalidOperationException">(from setter) <paramref name="value"/> does not have the same name as this property's value</exception>
		public MemoryDomain Domain
		{
			get
			{
				return _domain;
			}

			internal set
			{
				if (value != null && _domain.Name == value.Name)
				{
					_domain = value;
				}
				else
				{
					throw new InvalidOperationException("You cannot set a different domain to a watch on the fly");
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether the current address is
		/// within in the range of current <see cref="MemoryDomain"/>
		/// </summary>
		public bool IsOutOfRange => !IsSeparator && _domain.Size != 0 && Address >= _domain.Size;

		/// <summary>
		/// Gets a value that defined if the current <see cref="Watch"/> is actually a <see cref="SeparatorWatch"/>
		/// </summary>
		public bool IsSeparator => this is SeparatorWatch;

		/// <summary>
		/// Gets or sets notes for current <see cref="Watch"/>
		/// </summary>
		public string Notes { get; set; }

		/// <summary>
		/// Gets the current size of the watch
		/// </summary>
		public WatchSize Size { get; }

		#endregion

		// TODO: Replace all the following stuff by implementing ISerializable
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
	}
}
