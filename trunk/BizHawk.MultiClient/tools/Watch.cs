using System;
using System.Text;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace BizHawk.MultiClient
{
	#region Legacy watch object
	/// <summary>
	/// An object that represent a ram address and related properties
	/// </summary>
	public class Watch
	{
		public enum TYPE { BYTE, WORD, DWORD, SEPARATOR };
		public enum DISPTYPE { SIGNED, UNSIGNED, HEX };
		public enum PREVDEF { LASTSEARCH, ORIGINAL, LASTFRAME, LASTCHANGE };

		#region Constructors

		public Watch()
		{
			Address = 0;
			Value = 0;
			Type = TYPE.BYTE;
			Signed = DISPTYPE.UNSIGNED;
			BigEndian = true;
			Notes = "";
			Changecount = 0;
			Prev = 0;
			Original = 0;
			LastChange = 0;
			LastSearch = 0;
			Deleted = false;
			Domain = Global.Emulator.MainMemory;
		}

		public Watch(Watch w)
		{
			Address = w.Address;
			Value = w.Value;
			Type = w.Type;
			Signed = w.Signed;
			BigEndian = w.BigEndian;
			Notes = w.Notes;
			Changecount = w.Changecount;
			Prev = w.Prev;
			Original = w.Original;
			LastChange = w.LastChange;
			LastSearch = w.LastSearch;
			Domain = w.Domain;
			Deleted = w.Deleted;
		}

		public Watch(MemoryDomain _domain, int _address, int _value, TYPE _type, DISPTYPE _disptype, bool _bigendian, string _notes)
		{
			Domain = _domain;
			Address = _address;
			Value = _value;
			Type = _type;
			Signed = _disptype;
			BigEndian = _bigendian;
			Notes = _notes;
			Changecount = 0;
			Prev = _value;
			Original = _value;
			LastChange = _value;
			LastSearch = _value;
		}

		#endregion

		#region Publics

		public MemoryDomain Domain;
		public int Address;
		public int Value;
		public int Prev;
		public int Original;
		public int LastChange;
		public int LastSearch;
		public TYPE Type;
		public DISPTYPE Signed;
		public bool BigEndian;
		public string Notes;
		public int Changecount;
		public bool Deleted;

		#endregion

		#region Properties

		public int DiffPrev
		{
			get { return Value - Prev; }
		}

		public int DiffOriginal
		{
			get { return Value - Original; }
		}

		public int DiffLastChange
		{
			get { return Value - LastChange; }
		}

		public int DiffLastSearch
		{
			get { return Value - LastSearch; }
		}

		public string ValueString
		{
			get { return ValToString(Value); }
		}

		public string PrevString
		{
			get { return ValToString(Prev); }
		}

		public string OriginalString
		{
			get { return ValToString(Original); }
		}

		public string LastChangeString
		{
			get { return ValToString(LastChange); }
		}

		public string LastSearchString
		{
			get { return ValToString(LastSearch); }
		}

		public string DiffOriginalString
		{
			get { return DiffToString(Original); }
		}

		public string DiffLastSearchString
		{
			get { return DiffToString(LastSearch); }
		}

		public uint UnsignedValue
		{
			get { return UnsignedVal(Value); }
		}

		public int SignedValue
		{
			get { return SignedVal(Value); }
		}

		public char TypeChar
		{
			get
			{
				switch (Type)
				{
					case TYPE.BYTE:
						return 'b';
					case TYPE.WORD:
						return 'w';
					case TYPE.DWORD:
						return 'd';
					case TYPE.SEPARATOR:
						return 'S';
					default:
						return 'b'; //Just in case
				}
			}
		}

		public char SignedChar
		{
			get
			{
				switch (Signed)
				{
					case DISPTYPE.SIGNED:
						return 's';
					case DISPTYPE.UNSIGNED:
						return 'u';
					case DISPTYPE.HEX:
						return 'h';
					default:
						return 's'; //Just in case
				}
			}
		}

		public string DiffPrevString
		{
			get { return DiffToString(DiffPrev); }
		}

		public string DiffLastChangeString
		{
			get { return DiffToString(DiffLastChange); }
		}

		#endregion

		#region Public Methods

		public bool SetTypeByChar(char c)     //b = byte, w = word, d = dword
		{
			switch (c)
			{
				case 'b':
					Type = TYPE.BYTE;
					return true;
				case 'w':
					Type = TYPE.WORD;
					return true;
				case 'd':
					Type = TYPE.DWORD;
					return true;
				case 'S':
					Type = TYPE.SEPARATOR;
					return true;
				default:
					return false;
			}
		}

		public bool SetSignedByChar(char c) //s = signed, u = unsigned, h = hex
		{
			switch (c)
			{
				case 's':
					Signed = DISPTYPE.SIGNED;
					return true;
				case 'u':
					Signed = DISPTYPE.UNSIGNED;
					return true;
				case 'h':
					Signed = DISPTYPE.HEX;
					return true;
				default:
					return false;
			}
		}

		public void PeekAddress()
		{
			if (Type == TYPE.SEPARATOR)
			{
				return;
			}

			Prev = Value;

			switch (Type)
			{
				case TYPE.BYTE:
					Value = Domain.PeekByte(Address);
					break;
				case TYPE.WORD:
					if (BigEndian)
					{
						Value = 0;
						Value |= Domain.PeekByte(Address) << 8;
						Value |= Domain.PeekByte(Address + 1);
					}
					else
					{
						Value = 0;
						Value |= Domain.PeekByte(Address);
						Value |= Domain.PeekByte(Address + 1) << 8;
					}
					break;
				case TYPE.DWORD:
					if (BigEndian)
					{
						Value = 0;
						Value |= Domain.PeekByte(Address) << 24;
						Value |= Domain.PeekByte(Address + 1) << 16;
						Value |= Domain.PeekByte(Address + 2) << 8;
						Value |= Domain.PeekByte(Address + 3) << 0;
					}
					else
					{
						Value = 0;
						Value |= Domain.PeekByte(Address) << 0;
						Value |= Domain.PeekByte(Address + 1) << 8;
						Value |= Domain.PeekByte(Address + 2) << 16;
						Value |= Domain.PeekByte(Address + 3) << 24;
					}
					break;
			}

			if (Value != Prev)
			{
				LastChange = Prev;
				Changecount++;
			}
		}

		public void PokeAddress()
		{
			if (Type == TYPE.SEPARATOR)
				return;

			switch (Type)
			{
				case TYPE.BYTE:
					PokeByte();
					break;
				case TYPE.WORD:
					PokeWord();
					break;
				case TYPE.DWORD:
					PokeDWord();
					break;
			}
		}

		public uint UnsignedVal(int val)
		{
			switch (Type)
			{
				case TYPE.BYTE:
					return (byte)val;
				case TYPE.WORD:
					return (ushort)val;
			}
			return (uint)val;
		}

		public int SignedVal(int val)
		{
			switch (Type)
			{
				case TYPE.BYTE:
					return (sbyte)val;
				case TYPE.WORD:
					return (short)val;
			}
			return val;
		}

		public override string ToString()
		{
			if (Type == TYPE.SEPARATOR)
			{
				return "----";
			}

			StringBuilder str = new StringBuilder(Notes);
			str.Append(": ");
			str.Append(ValToString(Value));
			return str.ToString();
		}

		public void TrySetValue(string value)
		{
			switch (Signed)
			{
				case DISPTYPE.SIGNED:
					try
					{
						Value = int.Parse(value);
					}
					catch { }
					break;
				case DISPTYPE.UNSIGNED:
					try
					{
						Value = (int)uint.Parse(value);
					}
					catch { }
					break;
				case DISPTYPE.HEX:
					try
					{
						Value = int.Parse(value, NumberStyles.HexNumber);
					}
					catch { }
					break;
			}
		}

		#endregion

		#region Helpers

		private string ValToString(int val)
		{
			if (Type == TYPE.SEPARATOR)
			{
				return "";
			}
			else
			{
				switch (Signed)
				{
					default:
					case DISPTYPE.UNSIGNED:
						return UnsignedVal(val).ToString();
					case DISPTYPE.SIGNED:
						return SignedVal(val).ToString();
					case DISPTYPE.HEX:
						switch (Type)
						{
							default:
							case TYPE.BYTE:
								return String.Format("{0:X2}", val);
							case TYPE.WORD:
								return String.Format("{0:X4}", val);
							case TYPE.DWORD:
								return String.Format("{0:X8}", val);
						}
				}
			}
		}



		private string DiffToString(int diff)
		{
			string converted = diff.ToString();
			if (diff >= 0)
				converted = "+" + converted;
			return converted;
		}

		private void PokeByte()
		{
			Domain.PokeByte(Address, (byte)Value);
		}

		private void PokeWord()
		{
			if (BigEndian)
			{
				Domain.PokeByte(Address + 0, (byte)(Value >> 8));
				Domain.PokeByte(Address + 1, (byte)(Value));
			}
			else
			{
				Domain.PokeByte(Address + 0, (byte)(Value));
				Domain.PokeByte(Address + 1, (byte)(Value >> 8));
			}
		}

		private void PokeDWord()
		{
			if (BigEndian)
			{
				Domain.PokeByte(Address + 0, (byte)(Value >> 24));
				Domain.PokeByte(Address + 1, (byte)(Value >> 16));
				Domain.PokeByte(Address + 2, (byte)(Value >> 8));
				Domain.PokeByte(Address + 3, (byte)(Value));
			}
			else
			{
				Domain.PokeByte(Address + 0, (byte)(Value));
				Domain.PokeByte(Address + 1, (byte)(Value >> 8));
				Domain.PokeByte(Address + 2, (byte)(Value >> 16));
				Domain.PokeByte(Address + 3, (byte)(Value >> 24));
			}
		}

		#endregion

		#region Compare Methods

		private int ComparePrevious(Watch Other, PREVDEF previous)
		{
			switch (previous)
			{
				case PREVDEF.LASTSEARCH:
					return CompareLastSearch(Other);
				case PREVDEF.ORIGINAL:
					return CompareOriginal(Other);
				default:
				case PREVDEF.LASTFRAME:
					return ComparePrev(Other);
				case PREVDEF.LASTCHANGE:
					return CompareLastChange(Other);
			}
		}

		private int CompareDiff(Watch Other, PREVDEF previous)
		{
			switch (previous)
			{
				case PREVDEF.LASTSEARCH:
					return CompareDiffLastSearch(Other);
				case PREVDEF.ORIGINAL:
					return CompareDiffOriginal(Other);
				default:
				case PREVDEF.LASTFRAME:
					return CompareDiffPrev(Other);
				case PREVDEF.LASTCHANGE:
					return CompareDiffLastChange(Other);
			}
		}

		private int CompareAddress(Watch Other)
		{
			if (Address < Other.Address)
				return -1;
			else if (Address > Other.Address)
				return 1;
			else
				return 0;
		}

		private int CompareValue(Watch Other)
		{
			if (Signed == DISPTYPE.SIGNED)
			{
				if (SignedVal(Value) < SignedVal(Other.Value))
					return -1;
				else if (SignedVal(Value) > SignedVal(Other.Value))
					return 1;
				else
					return 0;
			}
			if (UnsignedVal(Value) < UnsignedVal(Other.Value))
				return -1;
			else if (UnsignedVal(Value) > UnsignedVal(Other.Value))
				return 1;
			else
				return 0;
		}

		private int ComparePrev(Watch Other)
		{
			if (Signed == DISPTYPE.SIGNED)
			{
				if (SignedVal(Prev) < SignedVal(Other.Prev))
					return -1;
				else if (SignedVal(Prev) > SignedVal(Other.Prev))
					return 1;
				else
					return 0;
			}
			if (UnsignedVal(Prev) < UnsignedVal(Other.Prev))
				return -1;
			else if (UnsignedVal(Prev) > UnsignedVal(Other.Prev))
				return 1;
			else
				return 0;
		}

		private int CompareOriginal(Watch Other)
		{
			if (Signed == DISPTYPE.SIGNED)
			{
				if (SignedVal(Original) < SignedVal(Other.Original))
					return -1;
				else if (SignedVal(Original) > SignedVal(Other.Original))
					return 1;
				else
					return 0;
			}
			if (UnsignedVal(Original) < UnsignedVal(Other.Original))
				return -1;
			else if (UnsignedVal(Original) > UnsignedVal(Other.Original))
				return 1;
			else
				return 0;
		}

		private int CompareLastChange(Watch Other)
		{
			if (Signed == DISPTYPE.SIGNED)
			{
				if (SignedVal(LastChange) < SignedVal(Other.LastChange))
					return -1;
				else if (SignedVal(LastChange) > SignedVal(Other.LastChange))
					return 1;
				else
					return 0;
			}
			if (UnsignedVal(LastChange) < UnsignedVal(Other.LastChange))
				return -1;
			else if (UnsignedVal(LastChange) > UnsignedVal(Other.LastChange))
				return 1;
			else
				return 0;
		}

		private int CompareLastSearch(Watch Other)
		{
			if (Signed == DISPTYPE.SIGNED)
			{
				if (SignedVal(LastSearch) < SignedVal(Other.LastSearch))
					return -1;
				else if (SignedVal(LastSearch) > SignedVal(Other.LastSearch))
					return 1;
				else
					return 0;
			}
			if (UnsignedVal(LastSearch) < UnsignedVal(Other.LastSearch))
				return -1;
			else if (UnsignedVal(LastSearch) > UnsignedVal(Other.LastSearch))
				return 1;
			else
				return 0;
		}

		private int CompareDiffPrev(Watch Other)
		{
			if (DiffPrev < Other.DiffPrev)
				return -1;
			else if (DiffPrev > Other.DiffPrev)
				return 1;
			else
				return 0;
		}

		private int CompareDiffOriginal(Watch Other)
		{
			if (DiffOriginal < Other.DiffOriginal)
				return -1;
			else if (DiffOriginal > Other.DiffOriginal)
				return 1;
			else
				return 0;
		}

		private int CompareDiffLastChange(Watch Other)
		{
			if (DiffLastChange < Other.DiffLastChange)
				return -1;
			else if (DiffLastChange > Other.DiffLastChange)
				return 1;
			else
				return 0;
		}

		private int CompareDiffLastSearch(Watch Other)
		{
			if (DiffLastSearch < Other.DiffLastSearch)
				return -1;
			else if (DiffLastSearch > Other.DiffLastSearch)
				return 1;
			else
				return 0;
		}

		private int CompareChanges(Watch Other)
		{
			if (Changecount < Other.Changecount)
				return -1;
			else if (Changecount > Other.Changecount)
				return 1;
			else
				return 0;
		}

		private int CompareNotes(Watch Other)
		{
			if (Notes == null & Other.Notes == null)
				return 0;
			else if (Notes == null)
				return -1;
			else if (Other.Notes == null)
				return 1;
			else
				return Notes.CompareTo(Other.Notes);
		}

		private int CompareDomain(Watch Other)
		{
			if (Domain == null & Other.Domain == null)
			{
				return 0;
			}
			else if (Domain == null)
			{
				return -1;
			}
			else if (Other.Domain == null)
			{
				return 1;
			}
			else
			{
				return Domain.Name.CompareTo(Other.Domain.Name);
			}
		}

		public int CompareTo(Watch Other, string parameter, PREVDEF previous)
		{
			int compare = 0;
			if (parameter == "Address")
			{
				compare = CompareAddress(Other);
				if (compare == 0)
				{
					compare = CompareValue(Other);
					if (compare == 0)
					{
						compare = CompareChanges(Other);
						if (compare == 0)
						{
							compare = ComparePrevious(Other, previous);
							if (compare == 0)
							{
								compare = CompareDomain(Other);
								if (compare == 0)
								{
									compare = CompareDiff(Other, previous);
									if (compare == 0)
									{
										compare = CompareNotes(Other);
									}
								}
							}
						}
					}
				}
			}

			else if (parameter == "Value")
			{
				compare = CompareValue(Other);
				if (compare == 0)
				{
					compare = CompareAddress(Other);
					if (compare == 0)
					{
						compare = CompareChanges(Other);
						if (compare == 0)
						{
							compare = ComparePrevious(Other, previous);
							if (compare == 0)
							{
								compare = CompareDiff(Other, previous);
								if (compare == 0)
								{
									compare = CompareDomain(Other);
									if (compare == 0)
									{
										compare = CompareNotes(Other);
									}
								}
							}
						}
					}
				}
			}

			else if (parameter == "Changes")
			{
				compare = CompareChanges(Other);
				if (compare == 0)
				{
					compare = CompareAddress(Other);
					if (compare == 0)
					{
						compare = CompareValue(Other);
						if (compare == 0)
						{
							compare = ComparePrevious(Other, previous);
							if (compare == 0)
							{
								compare = CompareDiff(Other, previous);
								if (compare == 0)
									compare = CompareNotes(Other);
							}
						}
					}
				}
			}

			else if (parameter == "Prev")
			{
				compare = ComparePrevious(Other, previous);
				if (compare == 0)
				{
					compare = CompareAddress(Other);
					if (compare == 0)
					{
						compare = CompareValue(Other);
						if (compare == 0)
						{
							compare = CompareChanges(Other);
							if (compare == 0)
							{
								compare = CompareDiff(Other, previous);
								if (compare == 0)
								{
									compare = CompareDomain(Other);
									if (compare == 0)
									{
										compare = CompareNotes(Other);
									}
								}
							}
						}
					}
				}
			}

			else if (parameter == "Diff")
			{
				compare = CompareDiff(Other, previous);
				if (compare == 0)
				{
					compare = CompareAddress(Other);
					if (compare == 0)
					{
						compare = CompareValue(Other);
						if (compare == 0)
						{
							compare = CompareChanges(Other);
							if (compare == 0)
							{
								compare = ComparePrevious(Other, previous);
								if (compare == 0)
								{
									compare = CompareDomain(Other);
									if (compare == 0)
									{
										compare = CompareNotes(Other);
									}
								}
							}
						}
					}
				}
			}

			else if (parameter == "Domain")
			{
				compare = CompareDomain(Other);
				if (compare == 0)
				{
					compare = CompareAddress(Other);
					if (compare == 0)
					{
						compare = CompareValue(Other);
						if (compare == 0)
						{
							compare = CompareChanges(Other);
							if (compare == 0)
							{
								compare = ComparePrevious(Other, previous);
								if (compare == 0)
								{
									compare = CompareNotes(Other);
								}
							}
						}
					}
				}
			}

			else if (parameter == "Notes")
			{
				compare = CompareNotes(Other);
				if (compare == 0)
				{
					compare = CompareAddress(Other);
					if (compare == 0)
					{
						compare = CompareValue(Other);
						if (compare == 0)
						{
							compare = CompareChanges(Other);
							if (compare == 0)
							{
								compare = ComparePrevious(Other, previous);
								if (compare == 0)
								{
									compare = CompareDiff(Other, previous);
									if (compare == 0)
									{
										compare = CompareDomain(Other);
									}
								}
							}
						}
					}
				}
			}

			return compare;
		}

		#endregion
	}

	#endregion

	public abstract class WatchEntryBase
	{
		public enum WatchSize { Byte = 1, Word = 2, DWord = 4, Separator = 0 };
		public enum DisplayType { Separator, Signed, Unsigned, Hex };

		protected MemoryDomain _domain;
		protected DisplayType _type;
		protected bool _bigEndian;

		public abstract int? Address { get; }
		public abstract int? Value { get; }
		public abstract string AddressString { get; }
		public abstract string ValueString { get; }
		public abstract WatchSize Size { get; }
		public abstract bool IsSeparator { get; }
		public abstract List<DisplayType> ValidTypes { get; }

		public virtual DisplayType Type { get { return _type; } set { _type = value; } }
		public virtual bool BigEndian { get { return _bigEndian; } set { _bigEndian = value; } }

		public string DomainName
		{
			get { return _domain.Name; }
		}

		public static WatchSize SizeFromChar(char c)     //b = byte, w = word, d = dword
		{
			switch (c)
			{
				case 'b':
					return WatchSize.Byte;
				case 'w':
					return WatchSize.Word;
				case 'd':
					return WatchSize.DWord;
				default:
				case 'S':
					return WatchSize.Separator;
			}
		}

		public static DisplayType DisplayTypeFromChar(char c) //s = signed, u = unsigned, h = hex
		{
			switch (c)
			{
				default:
				case 'u':
					return DisplayType.Unsigned;
				case 's':
					return DisplayType.Signed;
				case 'h':
					return DisplayType.Hex;
			}
		}

		public string AddressFormatStr
		{
			get
			{
				if (_domain != null)
				{
					return "X" + IntHelpers.GetNumDigits(_domain.Size - 1).ToString();
				}
				else
				{
					return "";
				}
			}
		}

		public static WatchEntryBase GenerateWatch(MemoryDomain domain, int address, WatchSize size, bool details)
		{
			switch (size)
			{
				default:
				case WatchSize.Separator:
					return new SeparatorWatch();
				case WatchSize.Byte:
					if (details)
					{
						return new DetailedByteWatch(domain, address);
					}
					else
					{
						return new ByteWatch(domain, address);
					}
					break;
				case WatchSize.Word:
					throw new NotImplementedException();
				case WatchSize.DWord:
					throw new NotImplementedException();
			}
		}
	}

	public interface iWatchEntryDetails
	{
		int ChangeCount { get; }
		void ClearChangeCount();

		int? Previous { get; }
		string PreviousStr { get; }
		void ResetPrevious();
		string Diff { get; }
		string Notes { get; set; }

		void Update();
	}

	public class SeparatorWatch : WatchEntryBase
	{
		public SeparatorWatch() { }

		public override int? Address
		{
			get { return null; }
		}

		public override int? Value
		{
			get { return null; }
		}

		public override string AddressString
		{
			get { return ""; }
		}

		public override string ValueString
		{
			get { return ""; }
		}

		public override string ToString()
		{
			switch (Type)
			{
				default:
					return Value.ToString(); //TODO
			}
		}

		public override bool IsSeparator
		{
			get { return true; }
		}

		public override WatchSize Size
		{
			get { return WatchSize.Separator; }
		}

		public override List<DisplayType> ValidTypes
		{
			get { return new List<DisplayType>() { DisplayType.Separator }; }
		}
	}

	public class ByteWatch : WatchEntryBase
	{
		protected int _address;

		public ByteWatch(MemoryDomain domain, int address)
		{
			_address = address;
			_domain = domain;
		}

		public override int? Address
		{
			get { return _address; }
		}

		public override int? Value
		{
			get
			{
				return GetValue();
			}
		}

		public override string AddressString
		{
			get { return Address.Value.ToString(AddressFormatStr); }
		}

		public override string ValueString
		{
			get { return FormatValue(GetValue()); }
		}

		public override string ToString()
		{
			switch (Type)
			{
				default:
					return Value.ToString(); //TODO - this is used by on screen display
			}
		}

		public override bool IsSeparator
		{
			get { return false; }
		}

		public override WatchSize Size
		{
			get { return WatchSize.Byte; }
		}

		public override List<DisplayType> ValidTypes
		{
			get
			{
				return new List<DisplayType>()
				{
					DisplayType.Signed, DisplayType.Unsigned, DisplayType.Hex
				};
			}
		}

		protected byte GetValue()
		{
			return _domain.PeekByte(_address);
		}

		protected string FormatValue(byte val)
		{
			switch (Type)
			{
				default:
				case DisplayType.Unsigned:
					return ((byte)val).ToString();
				case DisplayType.Signed:
					return ((sbyte)val).ToString();
				case DisplayType.Hex:
					return String.Format("{0:X2}", val);
			}
		}
	}

	public class DetailedByteWatch : ByteWatch, iWatchEntryDetails
	{
		private byte _value;
		private byte _previous;

		public DetailedByteWatch(MemoryDomain domain, int address)
			: base(domain, address)
		{
			Notes = String.Empty;
			_previous = _value = _domain.PeekByte(_address);
		}

		public int ChangeCount { get; private set; }
		public void ClearChangeCount() { ChangeCount = 0; }

		public int? Previous { get { return _previous; } }
		public string PreviousStr { get { return FormatValue(_previous); } }
		public void ResetPrevious() { _previous = _value; }

		public string Diff
		{
			get { return FormatValue((byte)(_previous - _value)); }
		}

		public string Notes { get; set; }

		public void Update() //TODO: different notions of previous
		{
			_previous = _value;
			_value = _domain.PeekByte(_address);
			if (_value != Previous)
			{
				ChangeCount++;
			}
		}
	}

	public class WatchList : IEnumerable
	{
		private string _currentFilename = "";

		public enum WatchPrevDef { LastSearch, Original, LastFrame, LastChange };

		private List<WatchEntryBase> _watchList = new List<WatchEntryBase>();
		private MemoryDomain _domain = null;

		public WatchList() { }

		public IEnumerator<WatchEntryBase> GetEnumerator()
		{
			return _watchList.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public WatchEntryBase this[int index]
		{
			get
			{
				return _watchList[index];
			}
		}

		public int WatchCount
		{
			get
			{
				return _watchList.Count(w => !w.IsSeparator);
			}
		}

		public int ItemCount
		{
			get
			{
				return _watchList.Count;
			}
		}

		public string AddressFormatStr
		{
			get
			{
				if (_domain != null)
				{
					return "X" + IntHelpers.GetNumDigits(_domain.Size - 1).ToString();
				}
				else
				{
					return "";
				}
			}
		}

		public void Clear()
		{
			_watchList.Clear();
			Changes = false;
			_currentFilename = "";
		}

		public MemoryDomain Domain { get { return _domain; } set { _domain = value; } }

		public void UpdateValues()
		{
			var detailedWatches = _watchList.OfType<iWatchEntryDetails>().ToList();
			foreach (var watch in detailedWatches)
			{
				watch.Update();
			}
		}

		#region File handling logic - probably needs to be its own class

		public string CurrentFileName { get { return _currentFilename; } set { _currentFilename = value; } }
		public bool Changes { get; set; }

		public void Save()
		{
			if (!String.IsNullOrWhiteSpace(CurrentFileName))
			{
				SaveFile();
			}
			else
			{
				SaveFileAs();
			}
		}

		public bool Load(string path, bool details, bool append)
		{
			bool result = LoadFile(path, details, append);

			if (result)
			{
				if (!append)
				{
					CurrentFileName = path;
				}
				else
				{
					Changes = false;
				}
			}

			return result;
		}

		private void SaveFile()
		{
			//TODO
			throw new NotImplementedException();
		}

		private void SaveFileAs()
		{
			//TODO
			throw new NotImplementedException();
		}

		private bool LoadFile(string path, bool details, bool append)
		{
			string domain = "";
			var file = new FileInfo(path);
			if (file.Exists == false) return false;
			bool isBizHawkWatch = true; //Hack to support .wch files from other emulators
			bool isOldBizHawkWatch = false;
			using (StreamReader sr = file.OpenText())
			{
				string line;

				if (append == false)
				{
					Clear();
				}

				while ((line = sr.ReadLine()) != null)
				{
					//.wch files from other emulators start with a number representing the number of watch, that line can be discarded here
					//Any properly formatted line couldn't possibly be this short anyway, this also takes care of any garbage lines that might be in a file
					if (line.Length < 5)
					{
						isBizHawkWatch = false;
						continue;
					}

					if (line.Length >= 6 && line.Substring(0, 6) == "Domain")
					{
						domain = line.Substring(7, line.Length - 7);
						isBizHawkWatch = true;
					}

					if (line.Length >= 8 && line.Substring(0, 8) == "SystemID")
					{
						continue;
					}

					int numColumns = StringHelpers.HowMany(line, '\t');
					int startIndex;
					if (numColumns == 5)
					{
						//If 5, then this is a post 1.0.5 .wch file
						if (isBizHawkWatch)
						{
							//Do nothing here
						}
						else
						{
							startIndex = line.IndexOf('\t') + 1;
							line = line.Substring(startIndex, line.Length - startIndex);   //5 digit value representing the watch position number
						}
					}
					else if (numColumns == 4)
					{
						isOldBizHawkWatch = true;
					}
					else //4 is 1.0.5 and earlier
					{
						continue;   //If not 4, something is wrong with this line, ignore it
					}



					//Temporary, rename if kept
					int addr = 0;
					WatchEntryBase.WatchSize size = WatchEntryBase.WatchSize.Separator;
					WatchEntryBase.DisplayType type = WatchEntryBase.DisplayType.Unsigned;
					bool bigEndian = false;
					MemoryDomain memDomain = Global.Emulator.MainMemory;
					string notes;

					string temp = line.Substring(0, line.IndexOf('\t'));
					try
					{
						addr = Int32.Parse(temp, NumberStyles.HexNumber);
					}
					catch
					{
						continue;
					}

					startIndex = line.IndexOf('\t') + 1;
					line = line.Substring(startIndex, line.Length - startIndex);   //Type
					size = WatchEntryBase.SizeFromChar(line[0]);


					startIndex = line.IndexOf('\t') + 1;
					line = line.Substring(startIndex, line.Length - startIndex);   //Signed
					type = WatchEntryBase.DisplayTypeFromChar(line[0]);

					startIndex = line.IndexOf('\t') + 1;
					line = line.Substring(startIndex, line.Length - startIndex);   //Endian
					try
					{
						startIndex = Int16.Parse(line[0].ToString());
					}
					catch
					{
						continue;
					}
					if (startIndex == 0)
					{
						bigEndian = false;
					}
					else
					{
						bigEndian = true;
					}

					if (isBizHawkWatch && !isOldBizHawkWatch)
					{
						startIndex = line.IndexOf('\t') + 1;
						line = line.Substring(startIndex, line.Length - startIndex);   //Domain
						temp = line.Substring(0, line.IndexOf('\t'));
						memDomain = Global.Emulator.MemoryDomains[GetDomainPos(temp)];
					}

					startIndex = line.IndexOf('\t') + 1;
					notes = line.Substring(startIndex, line.Length - startIndex);   //User notes

					WatchEntryBase w = WatchEntryBase.GenerateWatch(memDomain, addr, size, details);
					w.BigEndian = bigEndian;
					w.Type = type;
					(w as iWatchEntryDetails).Notes = notes;

					_watchList.Add(w);
					_domain = Global.Emulator.MemoryDomains[GetDomainPos(domain)];
				}
			}

			return true;
		}

		private static int GetDomainPos(string name)
		{
			//Attempts to find the memory domain by name, if it fails, it defaults to index 0
			for (int x = 0; x < Global.Emulator.MemoryDomains.Count; x++)
			{
				if (Global.Emulator.MemoryDomains[x].Name == name)
					return x;
			}
			return 0;
		}

		#endregion
	}
}
