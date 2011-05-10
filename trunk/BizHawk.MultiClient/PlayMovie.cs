using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace BizHawk.MultiClient
{
    public partial class PlayMovie : Form
    {
        //TODO: Think about this: .\Movies is the default folder, when shoudl this be created? On load (no platform specific folders do this)
        //Upon open file dialog? that's weird, record movie? more often people will use play movie first
        //Never? then the path default must be .\ not .\movies

        List<Movie> MovieList = new List<Movie>();

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
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.MoviesPath, "");
            ofd.Filter = "Movie files (*.tas)|*.TAS;*.ZIP;*.7z|FCEUX Movies|*.FM2|PCEjin Movies|*.PCE|Archive Files|*.zip;*.7z|All Files|*.*";
            
            Global.Sound.StopSound();
            var result = ofd.ShowDialog();
            Global.Sound.StartSound();
            if (result == DialogResult.OK)
            {
                var file = new FileInfo(ofd.FileName);
                if (!file.Exists)
                    return;
                else
                    PreLoadMovieFile(file);

                
            }
        }

        private void PreLoadMovieFile(FileInfo path)
        {

        }
    }
}
