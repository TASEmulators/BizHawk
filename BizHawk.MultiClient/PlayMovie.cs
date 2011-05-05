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
    public partial class PlayMovie : Form
    {
        //TODO: Think about this: .\Movies is the default folder, when shoudl this be created? On load (no platform specific folders do this)
        //Upon open file dialog? that's weird, record movie? more often people will use play movie first
        //Never? then the path default must be .\ not .\movies
        public PlayMovie()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BrowseMovies_Click(object sender, EventArgs e)
        {
            OpenFileDialog o = new OpenFileDialog();
            o.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.MoviesPath);
            if (o.ShowDialog() == DialogResult.OK)
            {

            }
        }
    }
}
