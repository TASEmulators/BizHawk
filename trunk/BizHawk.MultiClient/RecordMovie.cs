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
        Movie MovieToRecord;

        public RecordMovie()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, EventArgs e)
        {
            Global.MainForm.UserMovie = MovieToRecord;
            Global.MainForm.UserMovie.StartNewRecording();
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
