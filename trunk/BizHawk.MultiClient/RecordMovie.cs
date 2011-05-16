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
    public partial class RecordMovie : Form
    {
        //TODO: on OK check that the user actually selected a movie (text box != empty?)
        //Have an editiable listview for header info and any other settings, or appropriate widgets
        //Some header/settings that needs to be editable:
        //  System ID
        //  Game
        //  Author (very important)
        //  Some comments?
        //  Platform specific bools like PAL vs NTSC or an FDS flag, etc

        Movie MovieToRecord;

        public RecordMovie()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, EventArgs e)
        {
            Global.MainForm.StartNewMovie(MovieToRecord, true);
            this.Close();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.MoviesPath, "");
            sfd.DefaultExt = ".tas";
            sfd.FileName = Global.Game.Name;
            sfd.Filter = "Movie files (*.tas)|*.TAS";

            Global.Sound.StopSound();
            var result = sfd.ShowDialog();
            Global.Sound.StartSound();
            if (result == DialogResult.OK)
            {
                var file = new FileInfo(sfd.FileName);
                MovieToRecord = new Movie(sfd.FileName, MOVIEMODE.RECORD);
                RecordBox.Text = sfd.FileName;
            }
        }

        private void RecordMovie_Load(object sender, EventArgs e)
        {

        }
    }
}
