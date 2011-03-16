using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
    class Cheat
    {
        public string name { get; set; }
        public int address { get; set; }
        public int value { get; set; }
        public int compare { get; set; }
        public bool enabled { get; set; }

        public Cheat()
        {
            name = "";
            address = 0;
            value = 0;
            compare = 0;
            enabled = false;
        }

        public Cheat(Cheat c)
        {
            name = c.name;
            address = c.address;
            value = c.value;
            compare = c.compare;
            enabled = c.enabled;
        }

        public Cheat(string cname, int addr, int val, int comp, bool e)
        {
            name = cname;
            address = addr;
            value = val;
            compare = comp;
            enabled = e;
        }

    }
}
