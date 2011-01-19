using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
    //Data structure for a watch item in the Ram Watch Dialog
    enum atype { BYTE, WORD, DWORD };
    enum asigned { SIGNED, UNSIGNED, HEX };
    class Watch
    {
        public Watch()
        {
            address = 0;
            value = 0;
            type = atype.BYTE;
            signed = asigned.SIGNED;
            bigendian = false;
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
                default:
                    return false;
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
    }
}
