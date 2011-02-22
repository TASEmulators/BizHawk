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
        }
        public int address { get; set; }
        public int value { get; set; }         //Current value
        public int prev { get; set; }
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

        private void PeekByte(MemoryDomain domain)
        {
            value = domain.PeekByte(address);
        }

        private int PeekWord(MemoryDomain domain, int addr)
        {
            int temp = 0;
            if (bigendian)
            {
                temp = ((domain.PeekByte(addr) * 256) +
                    domain.PeekByte(addr + 1));
            }
            else
            {
                temp = ((domain.PeekByte(addr) +
                    domain.PeekByte(addr + 1) * 256));
            }
            return temp;
        }

        private void PeekDWord(MemoryDomain domain)
        {
            value = ((PeekWord(domain, address) * 65536) +
                PeekWord(domain, address + 2));
        }

        public void PeekAddress(MemoryDomain domain)
        {
            if (type == atype.SEPARATOR)
                return;

            switch(type)
            {
                case atype.BYTE:        
                    PeekByte(domain);
                    break;
                case atype.WORD:
                    value = PeekWord(domain, address);
                    break;
                case atype.DWORD:
                    PeekDWord(domain);
                    break;
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
                domain.PokeByte(address, (byte)(value / 256));
                domain.PokeByte(address + 1, (byte)(value % 256));
            }
            else
            {
                domain.PokeByte(address + 1, (byte)(value / 256));
                domain.PokeByte(address, (byte)(value % 256));
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
    }
}
