using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
    public partial class Cheats : Form
    {
        int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
        int defaultHeight;

        List<Cheat> cheatList = new List<Cheat>();
        string currentCheatFile = "";
        bool changes = false;
        /*
        public List<Cheat> GetCheatList()
        {
            List<Cheat> c = new List<Cheat>();
            for (int x = 0; x < cheatList.Count; x++)
                c.Add(new Cheat(cheatList[x]));

            return c;
        }
        */
        public Cheats()
        {
            InitializeComponent();
        }
    }
}
