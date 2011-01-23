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
    public partial class RamSearch : Form
    {
        //TODO:
        //Save window position & Size
        //Menu Bar
        //Reset window position item
     

        List<Watch> searchList = new List<Watch>();

        public RamSearch()
        {
            InitializeComponent();
        }

        private void RamSearch_Load(object sender, EventArgs e)
        {
            SetTotal();

            for (int x = 0; x < Global.Emulator.MainMemory.Size; x++)
            {
                Wat
            }
        }

        private void SetTotal()
        {
            int x = Global.Emulator.MainMemory.Size;
            string str;
            if (x == 1)
                str = " address";
            else
                str = " addresses";
            TotalSearchLabel.Text = x.ToString() + str;
        }
    }
}
