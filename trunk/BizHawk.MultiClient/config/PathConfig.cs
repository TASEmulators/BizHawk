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
    public partial class PathConfig : Form
    {
        //TODO:
        // `exe` shoudl be valid notation to mean path that the .exe is in ex: `exe`/NES
        // ./  and ../ are always always relative to base path
        // `recent` notation for most recently used path
        //If "always use recent path for roms" is checked then base path of each platorm should be disabled
        //Path text boxes shoudl be anchored L + R and the remaining widgets anchored R
        //Alight everything in each tab the same


        public PathConfig()
        {
            InitializeComponent();
        }

        private void PathConfig_Load(object sender, EventArgs e)
        {

        }

        private void SaveSettings()
        {

        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OK_Click(object sender, EventArgs e)
        {
            SaveSettings();
            this.Close();
        }

        private void textBox24_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
