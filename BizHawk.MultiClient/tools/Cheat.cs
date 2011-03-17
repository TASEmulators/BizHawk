using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
    public class Cheat
    {
        public string name { get; set; }
        public int address { get; set; }
        public byte value { get; set; }
        public bool enabled { get; set; }

        public Cheat()
        {
            name = "";
            address = 0;
            value = 0;
            enabled = false;
        }

        public Cheat(Cheat c)
        {
            name = c.name;
            address = c.address;
            value = c.value;
            enabled = c.enabled;
        }

        public Cheat(string cname, int addr, byte val, int comp, bool e)
        {
            name = cname;
            address = addr;
            value = val;
            enabled = e;
        }

    }
}
