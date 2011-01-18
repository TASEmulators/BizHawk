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
    }
}
