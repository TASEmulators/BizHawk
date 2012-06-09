using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
	public enum atype { BYTE, WORD, DWORD, SEPARATOR };   //TODO: more custom types too like 12.4 and 24.12 fixed point
	public enum asigned { SIGNED, UNSIGNED, HEX };

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
			prev = 0;
			original = value;
			lastchange = 0;
			lastsearch = value;
		}
		public int address { get; set; }
		public int value { get; set; }         //Current value
		public int prev { get; set; }
		public int original { get; set; }
		public int lastchange { get; set; }
		public int lastsearch { get; set; }
		public atype type { get; set; }        //Address type (byte, word, dword, etc
		public asigned signed { get; set; }    //Signed/Unsigned?
		public bool bigendian { get; set; }
		public string notes { get; set; }      //User notes
		public int changecount { get; set; }


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

		private int PeekWord(MemoryDomain domain, int addr)
		{
			if (bigendian)
			{
				return ((domain.PeekByte(addr) << 8) +
					domain.PeekByte(addr + 1));
			}
			else
			{
				return ((domain.PeekByte(addr) +
					domain.PeekByte(addr + 1) << 8));
			}
		}

		private void PeekDWord(MemoryDomain domain)
		{
			value = ((PeekWord(domain, address) << 16) +
				PeekWord(domain, address + 2));
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
					value = PeekWord(domain, address);
					break;
				case atype.DWORD:
					PeekDWord(domain);
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
				domain.PokeByte(address, (byte)(value >> 8));
				domain.PokeByte(address + 1, (byte)(value & 256));
			}
			else
			{
				domain.PokeByte(address + 1, (byte)(value >> 8));
				domain.PokeByte(address, (byte)(value & 256));
			}
		}

		private void PokeDWord(MemoryDomain domain)
		{
			if (bigendian)
			{
				domain.PokeByte(address, (byte)(value << 6));
				domain.PokeByte(address + 1, (byte)(value << 4));
				domain.PokeByte(address + 2, (byte)(value << 2));
				domain.PokeByte(address + 3, (byte)(value));
			}
			else
			{
				domain.PokeByte(address + 1, (byte)(value << 6));
				domain.PokeByte(address, (byte)(value << 4));
				domain.PokeByte(address + 3, (byte)(value << 2));
				domain.PokeByte(address + 2, (byte)(value));
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
					case asigned.SIGNED:
						switch (type)
						{
							default:
							case atype.BYTE:
								return ((sbyte)val).ToString();
							case atype.WORD:
								return ((short)val).ToString();
							case atype.DWORD:
								return ((int)val).ToString();
						}
					default:
					case asigned.UNSIGNED:
						switch (type)
						{
							default:
							case atype.BYTE:
								return ((byte)val).ToString();
							case atype.WORD:
								return ((ushort)val).ToString();
							case atype.DWORD:
								return ((uint)val).ToString();
						}
				}
			}
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
			if (this.value < Other.value)
				return -1;
			else if (this.value > Other.value)
				return 1;
			else
				return 0;
		}

		private int ComparePrev(Watch Other)
		{
			if (this.prev < Other.prev)
				return -1;
			else if (this.prev > Other.prev)
				return 1;
			else
				return 0;
		}

		private int CompareOriginal(Watch Other)
		{
			if (this.original < Other.original)
				return -1;
			else if (this.original > Other.original)
				return 1;
			else
				return 0;
		}

		private int CompareLastChange(Watch Other)
		{
			if (this.lastchange < Other.lastchange)
				return -1;
			else if (this.lastchange > Other.lastchange)
				return 1;
			else
				return 0;
		}

		private int CompareLastSearch(Watch Other)
		{
			if (this.lastsearch < Other.lastsearch)
				return -1;
			else if (this.lastsearch > Other.lastsearch)
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

		public int CompareTo(Watch Other, string parameter, string previous)
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
							switch (previous)
							{
								case "Last Search":
									compare = CompareLastSearch(Other);
									break;
								case "Original":
									compare = CompareOriginal(Other);
									break;
								case "Last Frame":
								default:
									compare = ComparePrev(Other);
									break;
							}
							if (compare == 0)
								compare = CompareNotes(Other);
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
							switch (previous)
							{
								case "Last Search":
									compare = CompareLastSearch(Other);
									break;
								case "Original":
									compare = CompareOriginal(Other);
									break;
								case "Last Frame":
								default:
									compare = ComparePrev(Other);
									break;
							}
							if (compare == 0)
								compare = CompareNotes(Other);
						}
					}
				}
			}

			else if (parameter == "Prev")
			{
				switch (previous)
				{
					case "Last Search":
						compare = CompareLastSearch(Other);
						break;
					case "Original":
						compare = CompareOriginal(Other);
						break;
					case "Last Frame":
					default:
						compare = ComparePrev(Other);
						break;
				}
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
								compare = CompareNotes(Other);
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
							switch (previous)
							{
								case "Last Search":
									compare = CompareLastSearch(Other);
									break;
								case "Original":
									compare = CompareOriginal(Other);
									break;
								case "Last Frame":
								default:
									compare = ComparePrev(Other);
									break;
							}
							if (compare == 0)
								compare = CompareNotes(Other);
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
								switch (previous)
								{
									case "Last Search":
										compare = CompareLastSearch(Other);
										break;
									case "Original":
										compare = CompareOriginal(Other);
										break;
									case "Last Frame":
									default:
										compare = ComparePrev(Other);
										break;
								}
						}
					}
				}
			}

			return compare;
		}
	}
}
