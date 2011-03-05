using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.MultiClient
{
    public partial class NESPPU : Form
    {
        public NESPPU()
        {
            InitializeComponent();
        }

        public void UpdateValues()
        {
            if (!(Global.Emulator is NES)) return;
        }

        private void NESPPU_Load(object sender, EventArgs e)
        {
            
        }
    }
}
