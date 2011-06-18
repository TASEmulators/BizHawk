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
        //Allow relative paths in record textbox
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

        private string MakePath()
        {
            string path = RecordBox.Text;
            int x = path.LastIndexOf('\\');
            if (path.LastIndexOf('\\') == -1)
            {
                if (path[0] != '\\') 
                    path = path.Insert(0, "\\");
                path = PathManager.MakeAbsolutePath(Global.Config.MoviesPath, "") + path;
                
                if (path[path.Length-4] != '.') //If no file extension, add .tas
                    path += ".tas";
                return path;
            }
            else
                return path;
        }

        private void OK_Click(object sender, EventArgs e)
        {
            string path = MakePath();
            MovieToRecord = new Movie(path, MOVIEMODE.RECORD);
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
            sfd.FileName = Global.Game.FilesystemSafeName;
            sfd.Filter = "Movie files (*.tas)|*.TAS";

            Global.Sound.StopSound();
            var result = sfd.ShowDialog();
            Global.Sound.StartSound();
            if (result == DialogResult.OK)
            {
                RecordBox.Text = sfd.FileName;
            }
        }

        private void RecordMovie_Load(object sender, EventArgs e)
        {
            StartFromCombo.SelectedIndex = 0;
            //TODO: populate combo with savestate slots that currently exist
        }

        private void RecordBox_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
        }

        private void RecordBox_DragDrop(object sender, DragEventArgs e)
        {
            string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            RecordBox.Text = filePaths[0];
        }
    }
}
