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
        //TODO: Get vlist display working
        //Input validation on address & value boxes
        //Remove compare column? make it conditional? Think about this
        //Set address box text load based on memory domain size
        //Recent files 
        //Memory domains
        //File format - saving & loading
        //Shortcuts for Cheat menu items

        int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
        int defaultHeight;

        List<Cheat> cheatList = new List<Cheat>();
        string currentCheatFile = "";
        bool changes = false;

        public List<Cheat> GetCheatList()
        {
            List<Cheat> c = new List<Cheat>();
            for (int x = 0; x < cheatList.Count; x++)
                c.Add(new Cheat(cheatList[x]));

            return c;
        }

        public Cheats()
        {
            InitializeComponent();
            Closing += (o, e) => SaveConfigSettings();
            CheatListView.QueryItemText += new QueryItemTextHandler(CheatListView_QueryItemText);
            CheatListView.QueryItemBkColor += new QueryItemBkColorHandler(CheatListView_QueryItemBkColor);
            CheatListView.VirtualMode = true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!AskSave())
                e.Cancel = true;
            base.OnClosing(e);
        }

        private void CheatListView_QueryItemBkColor(int index, int column, ref Color color)
        {
            if (cheatList[index].address < 0)
                color = this.BackColor;
        }

        private void CheatListView_QueryItemText(int index, int column, out string text)
        {
            text = "";
            if (column == 0) //Name
            {
                text = cheatList[index].name;
            }
            if (column == 1) //Address
            {
                text = String.Format("{0:X" + GetNumDigits((Global.Emulator.MainMemory.Size - 1)).ToString() + "}", cheatList[index].address);
            }
            if (column == 2) //Value
            {
                text = String.Format("{0:2X", cheatList[index].value);
            }
            if (column == 3) //Compare
            {
                text = String.Format("{0:2X", cheatList[index].compare);
            }

        }

        private int GetNumDigits(Int32 i)
        {
            //if (i == 0) return 0;
            //if (i < 0x10) return 1;
            //if (i < 0x100) return 2;
            //if (i < 0x1000) return 3; //adelikat: commenting these out because I decided that regardless of domain, 4 digits should be the minimum
            if (i < 0x10000) return 4;
            //if (i < 0x100000) return 5;
            if (i < 0x1000000) return 6;
            //if (i < 0x10000000) return 7;
            else return 8;
        }

        private void Cheats_Load(object sender, EventArgs e)
        {
            LoadConfigSettings();
        }

        public void AddCheat(Cheat c)
        {
            cheatList.Add(c);
            UpdateNumberOfCheats();
            DisplayCheatsList();
            CheatListView.Refresh();
        }

        public void LoadCheatFromRecent(string file)
        {
            bool z = true;

            if (changes) z = AskSave();

            if (z)
            {
                bool r = LoadCheatFile(file, false);
                if (!r)
                {
                    DialogResult result = MessageBox.Show("Could not open " + file + "\nRemove from list?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                    if (result == DialogResult.Yes)
                        Global.Config.RecentCheats.Remove(file);
                }
                DisplayCheatsList();
                changes = false;
            }
        }

        private void UpdateAutoLoadCheats()
        {
            autoLoadToolStripMenuItem.Checked = Global.Config.AutoLoadCheats ^= true;
        }

        private void recentToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            //Clear out recent Roms list
            //repopulate it with an up to date list
            recentToolStripMenuItem.DropDownItems.Clear();

            if (Global.Config.RecentCheats.IsEmpty())
            {
                recentToolStripMenuItem.DropDownItems.Add("None");
            }
            else
            {
                for (int x = 0; x < Global.Config.RecentCheats.Length(); x++)
                {
                    string path = Global.Config.RecentCheats.GetRecentFileByPosition(x);
                    var item = new ToolStripMenuItem();
                    item.Text = path;
                    item.Click += (o, ev) => LoadCheatFromRecent(path);
                    recentToolStripMenuItem.DropDownItems.Add(item);
                }
            }

            recentToolStripMenuItem.DropDownItems.Add("-");

            var clearitem = new ToolStripMenuItem();
            clearitem.Text = "&Clear";
            clearitem.Click += (o, ev) => Global.Config.RecentCheats.Clear();
            recentToolStripMenuItem.DropDownItems.Add(clearitem);

            var auto = new ToolStripMenuItem();
            auto.Text = "&Auto-Load";
            auto.Click += (o, ev) => UpdateAutoLoadCheats();
            if (Global.Config.AutoLoadCheats == true)
                auto.Checked = true;
            else
                auto.Checked = false;
            recentToolStripMenuItem.DropDownItems.Add(auto);
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
            CheatListView.ItemCount = cheatList.Count;
        }

        private void MoveUp()
        {
            ListView.SelectedIndexCollection indexes = CheatListView.SelectedIndices;
            Cheat temp = new Cheat();
            if (indexes[0] == 0) return;
            foreach (int index in indexes)
            {
                temp = cheatList[index];
                cheatList.Remove(cheatList[index]);
                cheatList.Insert(index - 1, temp);

                //Note: here it will get flagged many times redundantly potentially, 
                //but this avoids it being flagged falsely when the user did not select an index
                Changes();
            }
            List<int> i = new List<int>();
            for (int z = 0; z < indexes.Count; z++)
                i.Add(indexes[z] - 1);

            CheatListView.SelectedIndices.Clear();
            for (int z = 0; z < i.Count; z++)
                CheatListView.SelectItem(i[z], true);


            DisplayCheatsList();
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

        private void InsertSeparator()
        {
            Cheat c = new Cheat();
            c.address = -1;
            c.name = "Separator"; //TODO: remove me

            ListView.SelectedIndexCollection indexes = CheatListView.SelectedIndices;
            int x;
            if (indexes.Count > 0)
            {
                x = indexes[0];
                if (indexes[0] > 0)
                    cheatList.Insert(indexes[0], c);
            }
            else
                cheatList.Add(c);
            DisplayCheatsList();
            CheatListView.Refresh();
        }

        private void toolStripButtonSeparator_Click(object sender, EventArgs e)
        {
            InsertSeparator();
        }

        private void insertSeparatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InsertSeparator();
        }
        
        private Cheat MakeCheat()
        {
            Cheat c = new Cheat();
            c.name = NameBox.Text;
            c.address = int.Parse(AddressBox.Text, NumberStyles.HexNumber); //TODO: validation
            c.value = int.Parse(ValueBox.Text, NumberStyles.HexNumber);
            return c;
        }

        private void AddCheatButton_Click(object sender, EventArgs e)
        {
            AddCheat(MakeCheat());
        }

        private void addCheatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddCheat(MakeCheat());
        }

        private void RemoveCheat()
        {
            Changes();
            ListView.SelectedIndexCollection indexes = CheatListView.SelectedIndices;
            if (indexes.Count > 0)
            {
                foreach (int index in indexes)
                {
                    cheatList.Remove(cheatList[indexes[0]]); //index[0] used since each iteration will make this the correct list index
                }
                DisplayCheatsList();
            }
        }

        private void cutToolStripButton_Click(object sender, EventArgs e)
        {
            RemoveCheat();
        }

        private void removeCheatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RemoveCheat();
        }

        private void UpdateNumberOfCheats()
        {
            int z = cheatList.Count;
            if (z == 1)
                NumCheatsLabel.Text = z.ToString() + " cheat";
            else
                NumCheatsLabel.Text = z.ToString() + " cheats";
        }

        private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.CheatsSaveWindowPosition ^= true;
        }

        private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            saveWindowPositionToolStripMenuItem.Checked = Global.Config.CheatsSaveWindowPosition;
        }
    }
}
