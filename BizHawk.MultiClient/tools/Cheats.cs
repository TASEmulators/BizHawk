using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Globalization;

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
            Closing += (o, e) => SaveConfigSettings();
        }

        private void Cheats_Load(object sender, EventArgs e)
        {
            LoadConfigSettings();
        }

        public void LoadWatchFromRecent(string file)
        {
            bool z = true;
            /*
            if (changes) z = AskSave();

            if (z)
            {
                bool r = LoadWatchFile(file, false);
                if (!r)
                {
                    DialogResult result = MessageBox.Show("Could not open " + file + "\nRemove from list?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                    if (result == DialogResult.Yes)
                        Global.Config.RecentWatches.Remove(file);
                }
                DisplayWatchList();
                changes = false;
            }
            */
        }

        private void LoadConfigSettings()
        {
            defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
            defaultHeight = Size.Height;

            if (Global.Config.CheatsWndx >= 0 && Global.Config.CheatsWndy >= 0)
                Location = new Point(Global.Config.CheatsWndx, Global.Config.CheatsWndy);

            if (Global.Config.CheatsWidth >= 0 && Global.Config.CheatsHeight >= 0)
            {
                Size = new System.Drawing.Size(Global.Config.CheatsWidth, Global.Config.CheatsHeight);
            }
        }

        public void SaveConfigSettings()
        {
            Global.Config.CheatsWndx = this.Location.X;
            Global.Config.CheatsWndy = this.Location.Y;
            Global.Config.CheatsWidth = this.Right - this.Left;
            Global.Config.CheatsHeight = this.Bottom - this.Top;
        }

        private void DisplayCheatsList()
        {

        }

        private void MoveUp()
        {

        }

        private void MoveDown()
        {
            ListView.SelectedIndexCollection indexes = CheatListView.SelectedIndices;
            Cheat temp = new Cheat();

            foreach (int index in indexes)
            {
                temp = cheatList[index];

                if (index < cheatList.Count - 1)
                {

                    cheatList.Remove(cheatList[index]);
                    cheatList.Insert(index + 1, temp);

                }

                //Note: here it will get flagged many times redundantly potnetially, 
                //but this avoids it being flagged falsely when the user did not select an index
                Changes();
            }

            List<int> i = new List<int>();
            for (int z = 0; z < indexes.Count; z++)
                i.Add(indexes[z] + 1);

            CheatListView.SelectedIndices.Clear();
            //for (int z = 0; z < i.Count; z++)
                //CheatListView.SelectItem(i[z], true); //TODO

            DisplayCheatsList();
        }

        private void toolStripButtonMoveUp_Click(object sender, EventArgs e)
        {
            MoveUp();
        }

        private void moveUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MoveUp();
        }

        private void toolStripButtonMoveDown_Click(object sender, EventArgs e)
        {
            MoveDown();
        }

        private void moveDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MoveDown();
        }

        void Changes()
        {
            changes = true;
            MessageLabel.Text = Path.GetFileName(currentCheatFile) + " *";
        }

        private FileInfo GetSaveFileFromUser()
        {
            var sfd = new SaveFileDialog();
            sfd.InitialDirectory = Global.Config.LastRomPath;
            sfd.Filter = "Cheat Files (*.cht)|*.cht|All Files|*.*";
            sfd.RestoreDirectory = true;
            Global.Sound.StopSound();
            var result = sfd.ShowDialog();
            Global.Sound.StartSound();
            if (result != DialogResult.OK)
                return null;
            var file = new FileInfo(sfd.FileName);
            Global.Config.LastRomPath = file.DirectoryName;
            return file;
        }

        private void SaveAs()
        {
            var file = GetSaveFileFromUser();
            if (file != null)
            {
                SaveCheatFile(file.FullName);
                currentCheatFile = file.FullName;
                MessageLabel.Text = Path.GetFileName(currentCheatFile) + " saved.";
            }
        }

        private bool SaveCheatFile(string path)
        {
            var file = new FileInfo(path);

            using (StreamWriter sw = new StreamWriter(path))
            {
                string str = "";

                for (int x = 0; x < cheatList.Count; x++)
                {
                    //str += string.Format("{0:X4}", watchList[x].address) + "\t";
                    //str += watchList[x].GetTypeByChar().ToString() + "\t";
                    //str += watchList[x].GetSignedByChar().ToString() + "\t";

                    //if (watchList[x].bigendian == true)
                    //    str += "1\t";
                    //else
                    //    str += "0\t";

                    //str += watchList[x].notes + "\n";
                }

                sw.WriteLine(str);
            }
            changes = false;
            return true;
        }

        public bool AskSave()
        {
            if (changes)
            {
                DialogResult result = MessageBox.Show("Save Changes?", "Ram Watch", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);

                if (result == DialogResult.Yes)
                {
                    //TOOD: Do quicksave if filename, else save as
                    if (string.Compare(currentCheatFile, "") == 0)
                    {
                        SaveAs();
                    }
                    else
                        SaveCheatFile(currentCheatFile);
                    return true;
                }
                else if (result == DialogResult.No)
                    return true;
                else if (result == DialogResult.Cancel)
                    return false;
            }
            return true;
        }

        private void NewCheatList()
        {
            bool result = true;
            if (changes) result = AskSave();

            if (result == true)
            {
                cheatList.Clear();
                DisplayCheatsList();
                currentCheatFile = "";
                changes = false;
                MessageLabel.Text = "";
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewCheatList();
        }

        private void newToolStripButton_Click(object sender, EventArgs e)
        {
            NewCheatList();
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            if (changes)
            {
                SaveCheatFile(currentCheatFile);
            }
            else
            {
                SaveAs();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.Compare(currentCheatFile, "") == 0) return;

            if (changes)
                SaveCheatFile(currentCheatFile);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs();
        }

        private FileInfo GetFileFromUser()
        {
            var ofd = new OpenFileDialog();
            ofd.InitialDirectory = Global.Config.LastRomPath;
            ofd.Filter = "Cheat Files (*.cht)|*.cht|All Files|*.*";
            ofd.RestoreDirectory = true;

            Global.Sound.StopSound();
            var result = ofd.ShowDialog();
            Global.Sound.StartSound();
            if (result != DialogResult.OK)
                return null;
            var file = new FileInfo(ofd.FileName);
            Global.Config.LastRomPath = file.DirectoryName;
            return file;
        }

        bool LoadCheatFile(string path, bool append)
        {
            int y, z;
            var file = new FileInfo(path);
            if (file.Exists == false) return false;

            using (StreamReader sr = file.OpenText())
            {

            }

            return true; //TODO
        }

        private void OpenCheatFile()
        {
            var file = GetFileFromUser();
            if (file != null)
            {
                bool r = true;
                if (changes) r = AskSave();
                if (r)
                {
                    LoadCheatFile(file.FullName, false);
                    DisplayCheatsList();
                }
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenCheatFile();
        }

        private void openToolStripButton_Click(object sender, EventArgs e)
        {
            OpenCheatFile();
        }
    }
}
