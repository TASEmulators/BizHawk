using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
	
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

		#region Properties

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

		public int DiffPrev
		{
			get
			{
				return Value - Prev;
			}
		}
		
		public int DiffOriginal
		{
			get
			{
				return Value - Original;
			}
		}

		public int DiffLastChange
		{
			get
			{
				return Value - LastChange;
			}
		}

		public int DiffLastSearch
		{
			get
			{
				return Value - LastSearch;
			}
		
		}

		public string ValueString
		{
			get
			{
				return ValToString(Value);
			}
		}

		public string PrevString
		{
			get
			{
				return ValToString(Prev);
			}
		}

		public string OriginalString
		{
			get
			{
				return ValToString(Original);
			}
		}

		public string LastChangeString
		{
			get
			{
				return ValToString(LastChange);
			}
		}

		public string LastSearchString
		{
			get
			{
				return ValToString(LastSearch);
			}
		}

		public string DiffPrevString
		{
			get
			{
				return DiffToString(Prev);
			}
		}

		public string DiffOriginalString
		{
			get
			{
				return DiffToString(Original);
			}
		}

		public string DiffLastChangeString
		{
			get
			{
				return DiffToString(LastChange);
			}
		}

		public string DiffLastSearchString
		{
			get
			{
				return DiffToString(LastSearch);
			}
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
					return (uint)(byte)val;
				case TYPE.WORD:
					return (uint)(ushort)val;
			}
			return (uint)val;
		}

		public int SignedVal(int val)
		{
			switch (Type)
			{
				case TYPE.BYTE:
					return (int)(sbyte)val;
				case TYPE.WORD:
					return (int)(short)val;
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

		public string ValToString(int val)
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

		public string DiffToString(int diff)
		{
			string converted = diff.ToString();
			if (diff >= 0)
				converted = "+" + converted;
			return converted;
		}

		#endregion

		#region Helpers

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
				Domain.PokeByte(Address + 0, (byte)(Value << 24));
				Domain.PokeByte(Address + 1, (byte)(Value << 16));
				Domain.PokeByte(Address + 2, (byte)(Value << 8));
				Domain.PokeByte(Address + 3, (byte)(Value));
			}
			else
			{
				Domain.PokeByte(Address + 0, (byte)(Value));
				Domain.PokeByte(Address + 1, (byte)(Value << 8));
				Domain.PokeByte(Address + 2, (byte)(Value << 16));
				Domain.PokeByte(Address + 3, (byte)(Value << 24));
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
			if (this.Address < Other.Address)
				return -1;
			else if (this.Address > Other.Address)
				return 1;
			else
				return 0;
		}

		private int CompareValue(Watch Other)
		{
			if (Signed == DISPTYPE.SIGNED)
			{
				if (SignedVal(this.Value) < SignedVal(Other.Value))
					return -1;
				else if (SignedVal(this.Value) > SignedVal(Other.Value))
					return 1;
				else
					return 0;
			}
			if (UnsignedVal(this.Value) < UnsignedVal(Other.Value))
				return -1;
			else if (UnsignedVal(this.Value) > UnsignedVal(Other.Value))
				return 1;
			else
				return 0;
		}

		private int ComparePrev(Watch Other)
		{
			if (Signed == DISPTYPE.SIGNED)
			{
				if (SignedVal(this.Prev) < SignedVal(Other.Prev))
					return -1;
				else if (SignedVal(this.Prev) > SignedVal(Other.Prev))
					return 1;
				else
					return 0;
			}
			if (UnsignedVal(this.Prev) < UnsignedVal(Other.Prev))
				return -1;
			else if (UnsignedVal(this.Prev) > UnsignedVal(Other.Prev))
				return 1;
			else
				return 0;
		}

		private int CompareOriginal(Watch Other)
		{
			if (Signed == DISPTYPE.SIGNED)
			{
				if (SignedVal(this.Original) < SignedVal(Other.Original))
					return -1;
				else if (SignedVal(this.Original) > SignedVal(Other.Original))
					return 1;
				else
					return 0;
			}
			if (UnsignedVal(this.Original) < UnsignedVal(Other.Original))
				return -1;
			else if (UnsignedVal(this.Original) > UnsignedVal(Other.Original))
				return 1;
			else
				return 0;
		}

		private int CompareLastChange(Watch Other)
		{
			if (Signed == DISPTYPE.SIGNED)
			{
				if (SignedVal(this.LastChange) < SignedVal(Other.LastChange))
					return -1;
				else if (SignedVal(this.LastChange) > SignedVal(Other.LastChange))
					return 1;
				else
					return 0;
			}
			if (UnsignedVal(this.LastChange) < UnsignedVal(Other.LastChange))
				return -1;
			else if (UnsignedVal(this.LastChange) > UnsignedVal(Other.LastChange))
				return 1;
			else
				return 0;
		}

		private int CompareLastSearch(Watch Other)
		{
			if (Signed == DISPTYPE.SIGNED)
			{
				if (SignedVal(this.LastSearch) < SignedVal(Other.LastSearch))
					return -1;
				else if (SignedVal(this.LastSearch) > SignedVal(Other.LastSearch))
					return 1;
				else
					return 0;
			}
			if (UnsignedVal(this.LastSearch) < UnsignedVal(Other.LastSearch))
				return -1;
			else if (UnsignedVal(this.LastSearch) > UnsignedVal(Other.LastSearch))
				return 1;
			else
				return 0;
		}

		private int CompareDiffPrev(Watch Other)
		{
			if (this.DiffPrev < Other.DiffPrev)
				return -1;
			else if (this.DiffPrev > Other.DiffPrev)
				return 1;
			else
				return 0;
		}

		private int CompareDiffOriginal(Watch Other)
		{
			if (this.DiffOriginal < Other.DiffOriginal)
				return -1;
			else if (this.DiffOriginal > Other.DiffOriginal)
				return 1;
			else
				return 0;
		}

		private int CompareDiffLastChange(Watch Other)
		{
			if (this.DiffLastChange < Other.DiffLastChange)
				return -1;
			else if (this.DiffLastChange > Other.DiffLastChange)
				return 1;
			else
				return 0;
		}

		private int CompareDiffLastSearch(Watch Other)
		{
			if (this.DiffLastSearch < Other.DiffLastSearch)
				return -1;
			else if (this.DiffLastSearch > Other.DiffLastSearch)
				return 1;
			else
				return 0;
		}

		private int CompareChanges(Watch Other)
		{
			if (this.Changecount < Other.Changecount)
				return -1;
			else if (this.Changecount > Other.Changecount)
				return 1;
			else
				return 0;
		}

		private int CompareNotes(Watch Other)
		{
			if (this.Notes == null & Other.Notes == null)
				return 0;
			else if (this.Notes == null)
				return -1;
			else if (Other.Notes == null)
				return 1;
			else
				return this.Notes.CompareTo(Other.Notes);
		}

		private int CompareDomain(Watch Other)
		{
			if (this.Domain == null & Other.Domain == null)
			{
				return 0;
			}
			else if (this.Domain == null)
			{
				return -1;
			}
			else if (Other.Domain == null)
			{
				return 1;
			}
			else
			{
				return this.Domain.Name.CompareTo(Other.Domain.Name);
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
}
