using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
    //Data structure for a watch item in the Ram Watch Dialog
    public enum atype { BYTE, WORD, DWORD, SEPARATOR };   //TODO: more custom types too like 12.4 and 24.12 fixed point
    public enum asigned { SIGNED, UNSIGNED, HEX };
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
        }
        public Watch(int Address, int Value, atype Type, asigned Signed, bool BigEndian, string Notes)
        {
            address = Address;
            value = Value;
            type = Type;
            signed = Signed;
            bigendian = BigEndian;
            notes = Notes;
        }
        public int address { get; set; }   
        public int value { get; set; }         //Current value
        public atype type { get; set; }        //Address type (byte, word, dword, etc
        public asigned signed { get; set; }    //Signed/Unsigned?
        public bool bigendian { get; set; }
        public string notes { get; set; }      //User notes

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
    }
}
