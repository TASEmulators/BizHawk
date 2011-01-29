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
    public partial class InputConfig : Form
    {
        public InputConfig()
        {
            InitializeComponent();
        }

        private void InputConfig_Load(object sender, EventArgs e)
        {
            //Determine the System currently loaded, and set that one up first, if null emulator set, what is the default?
        }

        private void OK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
