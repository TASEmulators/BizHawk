using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
	public enum atype { BYTE, WORD, DWORD, SEPARATOR };   //TODO: more custom types too like 12.4 and 24.12 fixed point
	public enum asigned { SIGNED, UNSIGNED, HEX };
	public enum prevDef { LASTSEARCH, ORIGINAL, LASTFRAME, LASTCHANGE };

	/// <summary>
	/// An object that represent a ram address and related properties
	/// </summary>
	public class Watch
	{
		public Watch()
		{
			address = 0;
			value = 0;
			type = atype.BYTE;
			signed = asigned.UNSIGNED;
			bigendian = true;
			notes = "";
			changecount = 0;
			prev = 0;
			original = 0;
			lastchange = 0;
			lastsearch = 0;
			deleted = false;
		}

		public Watch(Watch w)
		{
			address = w.address;
			value = w.value;
			type = w.type;
			signed = w.signed;
			bigendian = w.bigendian;
			notes = w.notes;
			changecount = w.changecount;
			prev = w.prev;
			original = w.original;
			lastchange = w.lastchange;
			lastsearch = w.lastsearch;
			deleted = w.deleted;
		}

		public Watch(int Address, int Value, atype Type, asigned Signed, bool BigEndian, string Notes)
		{
			address = Address;
			value = Value;
			type = Type;
			signed = Signed;
			bigendian = BigEndian;
			notes = Notes;
			changecount = 0;
			prev = Value;
			original = Value;
			lastchange = Value;
			lastsearch = Value;
		}
		public int address { get; set; }
		public int value { get; set; }         //Current value
		public int prev { get; set; }
		public int original { get; set; }
		public int lastchange { get; set; }
		public int lastsearch { get; set; }
		public int diffPrev { get { return value - prev; } }
		public int diffOriginal { get { return value - original; } }
		public int diffLastChange { get { return value - lastchange; } }
		public int diffLastSearch { get { return value - lastsearch; } }
		public atype type { get; set; }        //Address type (byte, word, dword, etc
		public asigned signed { get; set; }    //Signed/Unsigned?
		public bool bigendian { get; set; }
		public string notes { get; set; }      //User notes
		public int changecount { get; set; }
		public bool deleted { get; set; } //For weeding out addresses in things like ram search, without actually removing them from the list (in order to preview, undo, etc)


		public bool SetTypeByChar(char c)     //b = byte, w = word, d = dword
		{
			switch (c)
			{
				case 'b':
					type = atype.BYTE;
					return true;
				case 'w':
					type = atype.WORD;
					return true;
				case 'd':
					type = atype.DWORD;
					return true;
				case 'S':
					type = atype.SEPARATOR;
					return true;
				default:
					return false;
			}
		}

		public char GetTypeByChar()
		{
			switch (type)
			{
				case atype.BYTE:
					return 'b';
				case atype.WORD:
					return 'w';
				case atype.DWORD:
					return 'd';
				case atype.SEPARATOR:
					return 'S';
				default:
					return 'b'; //Just in case
			}
		}

		public bool SetSignedByChar(char c) //s = signed, u = unsigned, h = hex
		{
			switch (c)
			{
				case 's':
					signed = asigned.SIGNED;
					return true;
				case 'u':
					signed = asigned.UNSIGNED;
					return true;
				case 'h':
					signed = asigned.HEX;
					return true;
				default:
					return false;
			}
		}

		public char GetSignedByChar()
		{
			switch (signed)
			{
				case asigned.SIGNED:
					return 's';
				case asigned.UNSIGNED:
					return 'u';
				case asigned.HEX:
					return 'h';
				default:
					return 's'; //Just in case
			}
		}

		public void PeekAddress(MemoryDomain domain)
		{
			if (type == atype.SEPARATOR)
				return;

			prev = value;
			
			switch (type)
			{
				case atype.BYTE:
					value = domain.PeekByte(address);
					break;
				case atype.WORD:
					if (bigendian)
					{
						value = 0;
						value |= domain.PeekByte(address) << 8;
						value |= domain.PeekByte(address + 1);
					}
					else
					{
						value = 0;
						value |= domain.PeekByte(address);
						value |= domain.PeekByte(address + 1) << 8;
					}
					break;
				case atype.DWORD:
					if (bigendian)
					{
						value = 0;
						value |= domain.PeekByte(address) << 24;
						value |= domain.PeekByte(address + 1) << 16;
						value |= domain.PeekByte(address + 2) << 8;
						value |= domain.PeekByte(address + 3) << 0;
					}
					else
					{
						value = 0;
						value |= domain.PeekByte(address) << 0;
						value |= domain.PeekByte(address + 1) << 8;
						value |= domain.PeekByte(address + 2) << 16;
						value |= domain.PeekByte(address + 3) << 24;
					}
					break;
			}

			if (value != prev)
			{
				lastchange = prev;
				changecount++;
			}
		}

		private void PokeByte(MemoryDomain domain)
		{
			domain.PokeByte(address, (byte)value);
		}

		private void PokeWord(MemoryDomain domain)
		{
			if (bigendian)
			{
				domain.PokeByte(address + 0, (byte)(value >> 8));
				domain.PokeByte(address + 1, (byte)(value));
			}
			else
			{
				domain.PokeByte(address + 0, (byte)(value));
				domain.PokeByte(address + 1, (byte)(value >> 8));
			}
		}

		private void PokeDWord(MemoryDomain domain)
		{
			if (bigendian)
			{
				domain.PokeByte(address + 0, (byte)(value << 24));
				domain.PokeByte(address + 1, (byte)(value << 16));
				domain.PokeByte(address + 2, (byte)(value << 8));
				domain.PokeByte(address + 3, (byte)(value));
			}
			else
			{
				domain.PokeByte(address + 0, (byte)(value));
				domain.PokeByte(address + 1, (byte)(value << 8));
				domain.PokeByte(address + 2, (byte)(value << 16));
				domain.PokeByte(address + 3, (byte)(value << 24));
			}
		}

		public void PokeAddress(MemoryDomain domain)
		{
			if (type == atype.SEPARATOR)
				return;

			switch (type)
			{
				case atype.BYTE:
					PokeByte(domain);
					break;
				case atype.WORD:
					PokeWord(domain);
					break;
				case atype.DWORD:
					PokeDWord(domain);
					break;
			}
		}

		public uint UnsignedVal(int val)
		{
			switch (type)
			{
				case atype.BYTE:
					return (uint)(byte)val;
				case atype.WORD:
					return (uint)(ushort)val;
			}
			return (uint)val;
		}

		public int SignedVal(int val)
		{
			switch (type)
			{
				case atype.BYTE:
					return (int)(sbyte)val;
				case atype.WORD:
					return (int)(short)val;
			}
			return val;
		}

		public override string ToString()
		{
			if (type == atype.SEPARATOR)
				return "----";

			StringBuilder str = new StringBuilder(notes);
			str.Append(": ");
			str.Append(ValToString(value));
			return str.ToString();
		}

		public string ValToString(int val)
		{
			if (type == atype.SEPARATOR)
				return "";
			else
			{
				switch (signed)
				{
					default:
					case asigned.UNSIGNED:
						return UnsignedVal(val).ToString();
					case asigned.SIGNED:
						return SignedVal(val).ToString();
					case asigned.HEX:
						switch (type)
						{
							default:
							case atype.BYTE:
								return String.Format("{0:X2}", val);
							case atype.WORD:
								return String.Format("{0:X4}", val);
							case atype.DWORD:
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

		public string ValueToString()
		{
			return ValToString(value);
		}

		public string PrevToString()
		{
			return ValToString(prev);
		}

		public string OriginalToString()
		{
			return ValToString(original);
		}

		public string LastChangeToString()
		{
			return ValToString(lastchange);
		}

		public string LastSearchToString()
		{
			return ValToString(lastsearch);
		}

		public string DiffPrevToString()
		{
			return DiffToString(prev);
		}

		public string DiffOriginalToString()
		{
			return DiffToString(original);
		}

		public string DiffLastChangeToString()
		{
			return DiffToString(lastchange);
		}

		public string DiffLastSearchToString()
		{
			return DiffToString(lastsearch);
		}

		private int ComparePrevious(Watch Other, prevDef previous)
		{
			switch (previous)
			{
				case prevDef.LASTSEARCH:
					return CompareLastSearch(Other);
				case prevDef.ORIGINAL:
					return CompareOriginal(Other);
				default:
				case prevDef.LASTFRAME:
					return ComparePrev(Other);
				case prevDef.LASTCHANGE:
					return CompareLastChange(Other);
			}
		}

		private int CompareDiff(Watch Other, prevDef previous)
		{
			switch (previous)
			{
				case prevDef.LASTSEARCH:
					return CompareDiffLastSearch(Other);
				case prevDef.ORIGINAL:
					return CompareDiffOriginal(Other);
				default:
				case prevDef.LASTFRAME:
					return CompareDiffPrev(Other);
				case prevDef.LASTCHANGE:
					return CompareDiffLastChange(Other);
			}
		}

		private int CompareAddress(Watch Other)
		{
			if (this.address < Other.address)
				return -1;
			else if (this.address > Other.address)
				return 1;
			else
				return 0;
		}

		private int CompareValue(Watch Other)
		{
			if (signed == asigned.SIGNED)
			{
				if (SignedVal(this.value) < SignedVal(Other.value))
					return -1;
				else if (SignedVal(this.value) > SignedVal(Other.value))
					return 1;
				else
					return 0;
			}
			if (UnsignedVal(this.value) < UnsignedVal(Other.value))
				return -1;
			else if (UnsignedVal(this.value) > UnsignedVal(Other.value))
				return 1;
			else
				return 0;
		}

		private int ComparePrev(Watch Other)
		{
			if (signed == asigned.SIGNED)
			{
				if (SignedVal(this.prev) < SignedVal(Other.prev))
					return -1;
				else if (SignedVal(this.prev) > SignedVal(Other.prev))
					return 1;
				else
					return 0;
			}
			if (UnsignedVal(this.prev) < UnsignedVal(Other.prev))
				return -1;
			else if (UnsignedVal(this.prev) > UnsignedVal(Other.prev))
				return 1;
			else
				return 0;
		}

		private int CompareOriginal(Watch Other)
		{
			if (signed == asigned.SIGNED)
			{
				if (SignedVal(this.original) < SignedVal(Other.original))
					return -1;
				else if (SignedVal(this.original) > SignedVal(Other.original))
					return 1;
				else
					return 0;
			}
			if (UnsignedVal(this.original) < UnsignedVal(Other.original))
				return -1;
			else if (UnsignedVal(this.original) > UnsignedVal(Other.original))
				return 1;
			else
				return 0;
		}

		private int CompareLastChange(Watch Other)
		{
			if (signed == asigned.SIGNED)
			{
				if (SignedVal(this.lastchange) < SignedVal(Other.lastchange))
					return -1;
				else if (SignedVal(this.lastchange) > SignedVal(Other.lastchange))
					return 1;
				else
					return 0;
			}
			if (UnsignedVal(this.lastchange) < UnsignedVal(Other.lastchange))
				return -1;
			else if (UnsignedVal(this.lastchange) > UnsignedVal(Other.lastchange))
				return 1;
			else
				return 0;
		}

		private int CompareLastSearch(Watch Other)
		{
			if (signed == asigned.SIGNED)
			{
				if (SignedVal(this.lastsearch) < SignedVal(Other.lastsearch))
					return -1;
				else if (SignedVal(this.lastsearch) > SignedVal(Other.lastsearch))
					return 1;
				else
					return 0;
			}
			if (UnsignedVal(this.lastsearch) < UnsignedVal(Other.lastsearch))
				return -1;
			else if (UnsignedVal(this.lastsearch) > UnsignedVal(Other.lastsearch))
				return 1;
			else
				return 0;
		}

		private int CompareDiffPrev(Watch Other)
		{
			if (this.diffPrev < Other.diffPrev)
				return -1;
			else if (this.diffPrev > Other.diffPrev)
				return 1;
			else
				return 0;
		}

		private int CompareDiffOriginal(Watch Other)
		{
			if (this.diffOriginal < Other.diffOriginal)
				return -1;
			else if (this.diffOriginal > Other.diffOriginal)
				return 1;
			else
				return 0;
		}

		private int CompareDiffLastChange(Watch Other)
		{
			if (this.diffLastChange < Other.diffLastChange)
				return -1;
			else if (this.diffLastChange > Other.diffLastChange)
				return 1;
			else
				return 0;
		}

		private int CompareDiffLastSearch(Watch Other)
		{
			if (this.diffLastSearch < Other.diffLastSearch)
				return -1;
			else if (this.diffLastSearch > Other.diffLastSearch)
				return 1;
			else
				return 0;
		}

		private int CompareChanges(Watch Other)
		{
			if (this.changecount < Other.changecount)
				return -1;
			else if (this.changecount > Other.changecount)
				return 1;
			else
				return 0;
		}

		private int CompareNotes(Watch Other)
		{
			if (this.notes == null & Other.notes == null)
				return 0;
			else if (this.notes == null)
				return -1;
			else if (Other.notes == null)
				return 1;
			else
				return this.notes.CompareTo(Other.notes);
		}

		public int CompareTo(Watch Other, string parameter, prevDef previous)
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
								compare = CompareDiff(Other, previous);
								if (compare == 0)
									compare = CompareNotes(Other);
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
									compare = CompareNotes(Other);
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
									compare = CompareNotes(Other);
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
									compare = CompareNotes(Other);
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
									compare = CompareDiff(Other, previous);
							}
						}
					}
				}
			}

			return compare;
		}
	}
}
