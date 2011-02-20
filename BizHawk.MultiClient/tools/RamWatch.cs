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
    /// <summary>
    /// A winform designed to display ram address values of the user's choice
    /// </summary>
    public partial class RamWatch : Form
    {
        //TODO: 
        //Call AskSave in main client X function
        //DWORD display
        //On Movie UP/Down set highlighted items to be what the user had selected (in their new position)
        //DisplayWatches needs to do value display properly like updatevalues, or just run update values
        //When Watch object has a changes member, display in watch list with a reset changes function
        //Ability to watch in different memory domains

        //IDEAS:
        //show a change count column?
        //Take advantage of the prev member to show amount changes from prev in a column?

        int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
        int defaultHeight;
        List<Watch> watchList = new List<Watch>();
        string currentWatchFile = "";
        bool changes = false;

        public void DisplayWatchList()
        {
            WatchListView.ItemCount = watchList.Count;
        }

        public void UpdateValues()
        {
            for (int x = 0; x < watchList.Count; x++)
            {
                if (watchList[x].type == atype.SEPARATOR) continue;
                switch (watchList[x].type)
                {
                    case atype.BYTE:
                        watchList[x].value = Global.Emulator.MainMemory.PeekByte(watchList[x].address);
                        break;
                    case atype.WORD:
                        {
                            if (watchList[x].bigendian)
                            {
                                watchList[x].value = ((Global.Emulator.MainMemory.PeekByte(watchList[x].address)*256) + 
                                    Global.Emulator.MainMemory.PeekByte((watchList[x+1].address)+1) );
                            }
                            else
                            {
                                watchList[x].value = (Global.Emulator.MainMemory.PeekByte(watchList[x].address) +
                                   (Global.Emulator.MainMemory.PeekByte((watchList[x].address)+1)*256) );
                            }
                        }
                        break;
                    case atype.DWORD:
                        break;
                }
                WatchListView.Refresh();
            }          
        }

        public void AddWatch(Watch w)
        {
            watchList.Add(w);
            DisplayWatchList();
        }

        public RamWatch()
        {
            InitializeComponent();
            WatchListView.QueryItemText += new QueryItemTextHandler(WatchListView_QueryItemText);
            WatchListView.QueryItemBkColor += new QueryItemBkColorHandler(WatchListView_QueryItemBkColor);
            WatchListView.VirtualMode = true;
        }

        private void WatchListView_QueryItemBkColor(int index, int column, ref Color color)
        {
            if (watchList[index].type == atype.SEPARATOR)
                color = this.BackColor;
        }

        void WatchListView_QueryItemText(int index, int column, out string text)
        {
            text = "";
            if (column == 0)
            {
                if (watchList[index].type == atype.SEPARATOR)
                    text = "";
                else
                    text = String.Format("{0:X}", watchList[index].address);
            }
            if (column == 1)
            {
                if (watchList[index].type == atype.SEPARATOR)
                    text = "";
                else
                {
                    switch (watchList[index].signed)
                    {
                        case asigned.HEX:
                            switch (watchList[index].type)
                            {
                                case atype.BYTE:
                                    text = String.Format("{0:X2}", watchList[index].value);
                                    break;
                                case atype.WORD:
                                    text = String.Format("{0:X4}", watchList[index].value);
                                    break;
                                case atype.DWORD:
                                    text = String.Format("{0:X8}", watchList[index].value);
                                    break;
                            }
                            break;
                        case asigned.SIGNED:
                            text = ((sbyte)watchList[index].value).ToString();
                            break;
                        case asigned.UNSIGNED:
                            text = watchList[index].value.ToString();
                            break;
                    }
                }
            }
            if (column == 2)
            {
                if (watchList[index].type == atype.SEPARATOR)
                    text = "";
                else
                    text = watchList[index].notes;
            }
        }

        public int HowMany(string str, char c)
        {
            int count = 0;
            for (int x = 0; x < str.Length; x++)
            {
                if (str[x] == c)
                    count++;
            }
            return count;
        }

        public bool AskSave()
        {
            if (changes)
            {
                DialogResult result = MessageBox.Show("Save Changes?", "Ram Watch", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);

                if (result == DialogResult.Yes)
                {
                    //TOOD: Do quicksave if filename, else save as
                    if (string.Compare(currentWatchFile, "") == 0)
                    {
                        SaveAs();
                    }
                    else
                        SaveWatchFile(currentWatchFile);
                    return true;
                }
                else if (result == DialogResult.No)
                    return true;
                else if (result == DialogResult.Cancel)
                    return false;
            }
            return true;
        }

        public void LoadWatchFromRecent(string file)
        {
            bool z = true;
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
        }

        private void NewWatchList()
        {
            bool result = true;
            if (changes) result = AskSave();

            if (result == true)
            {
                watchList.Clear();
                DisplayWatchList();
                currentWatchFile = "";
                changes = false;
                MessageLabel.Text = "";
            }
        }
        
        private  bool SaveWatchFile(string path)
        {
            var file = new FileInfo(path);

            using (StreamWriter sw = new StreamWriter(path))
            {
                string str = "";
                
                for (int x = 0; x < watchList.Count; x++)
                {
                    str += string.Format("{0:X4}", watchList[x].address) + "\t";
                    str += watchList[x].GetTypeByChar().ToString() + "\t";
                    str += watchList[x].GetSignedByChar().ToString() + "\t";

                    if (watchList[x].bigendian == true)
                        str += "1\t";
                    else
                        str += "0\t";

                    str += watchList[x].notes + "\n";
                }

                sw.WriteLine(str);
            }
            changes = false;
            return true;
        }

        bool LoadWatchFile(string path, bool append)
        {
            int y, z;
            var file = new FileInfo(path);
            if (file.Exists == false) return false;

            using (StreamReader sr = file.OpenText())
            {
                if (!append)
                    currentWatchFile = path;

                int count = 0;
                string s = "";
                string temp = "";
                
                if (append == false)
                    watchList.Clear();  //Wipe existing list and read from file
                
                while ((s = sr.ReadLine()) != null)
                {
                    //parse each line and add to watchList

                    //.wch files from other emulators start with a number representing the number of watch, that line can be discarded here
                    //Any properly formatted line couldn't possibly be this short anyway, this also takes care of any garbage lines that might be in a file
                    if (s.Length < 5) continue;
                    
                    z = HowMany(s, '\t');
                    if (z == 5)
                    {
                        //If 5, then this is a .wch file format made from another emulator, the first column (watch position) is not needed here
                        y = s.IndexOf('\t') + 1;
                        s = s.Substring(y, s.Length - y);   //5 digit value representing the watch position number
                    }
                    else if (z != 4) 
                        continue;   //If not 4, something is wrong with this line, ignore it
                    count++;
                    Watch w = new Watch();

                    temp = s.Substring(0, s.IndexOf('\t'));
                    w.address = int.Parse(temp, NumberStyles.HexNumber);

                    y = s.IndexOf('\t') + 1;
                    s = s.Substring(y, s.Length - y);   //Type
                    w.SetTypeByChar(s[0]);

                    y = s.IndexOf('\t') + 1;
                    s = s.Substring(y, s.Length - y);   //Signed
                    w.SetSignedByChar(s[0]);

                    y = s.IndexOf('\t') + 1;
                    s = s.Substring(y, s.Length - y);   //Endian
                    y = Int16.Parse(s[0].ToString());
                    if (y == 0)
                        w.bigendian = false;
                    else
                        w.bigendian = true;
                                        
                    w.notes =  s.Substring(2, s.Length - 2);   //User notes

                    watchList.Add(w);
                }

                Global.Config.RecentWatches.Add(file.FullName);
                changes = false;
                MessageLabel.Text = Path.GetFileName(file.FullName);
                //Update the number of watches
                WatchCountLabel.Text = count.ToString() + " watches";
            }

            return true;
        }

        private Point GetPromptPoint()
        {

            Point p = new Point(WatchListView.Location.X, WatchListView.Location.Y);
            Point q = new Point();
            q = PointToScreen(p);
            return q;
        }

        void AddNewWatch()
        {
            
            RamWatchNewWatch r = new RamWatchNewWatch();
            r.location = GetPromptPoint();
                        
            r.ShowDialog();
            if (r.userSelected == true)
            {
                watchList.Add(r.watch);
                DisplayWatchList(); //TODO: Do I need these calls?
            }
        }

        void Changes()
        {
            changes = true;
            MessageLabel.Text = Path.GetFileName(currentWatchFile) + " *";
        }

        void EditWatchObject(int pos)
        {
            RamWatchNewWatch r = new RamWatchNewWatch();
            r.location = GetPromptPoint();
            r.SetToEditWatch(watchList[pos], "Edit Watch");
            r.ShowDialog();

            if (r.userSelected == true)
            {
                Changes();
                watchList[pos] = r.watch;
                DisplayWatchList();
            }
        }

        void EditWatch()
        {
            ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
            if (indexes.Count > 0)
                EditWatchObject(indexes[0]);
        }

        void RemoveWatch()
        {
            Changes();
            ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
            if (indexes.Count > 0)
            {
                foreach (int index in indexes)
                {
                    watchList.Remove(watchList[indexes[0]]); //index[0] used since each iteration will make this the correct list index
                }
                DisplayWatchList();
            }
        }

        void DuplicateWatch()
        {
            ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
            if (indexes.Count > 0)
            {
                RamWatchNewWatch r = new RamWatchNewWatch();
                r.location = GetPromptPoint();
                int x = indexes[0];
                r.SetToEditWatch(watchList[x], "Duplicate Watch");
                r.ShowDialog();

                if (r.userSelected == true)
                {
                    Changes();
                    watchList.Add(r.watch);
                    DisplayWatchList();
                }
            }
        }

        void MoveUp()
        {
            ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
            Watch temp = new Watch();

            foreach (int index in indexes)
            {
                temp = watchList[index];
                watchList.Remove(watchList[index]);
                watchList.Insert(index - 1, temp);
                
                //Note: here it will get flagged many times redundantly potentially, 
                //but this avoids it being flagged falsely when the user did not select an index
                Changes();
            }
            DisplayWatchList();
        }

        void MoveDown()
        {
            ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
            Watch temp = new Watch();

            foreach (int index in indexes)
            {
                temp = watchList[index];
                
                if (index < watchList.Count - 1)
                {
                    watchList.Remove(watchList[index]);
                    watchList.Insert(index + 1, temp);
                }

                //Note: here it will get flagged many times redundantly potnetially, 
                //but this avoids it being flagged falsely when the user did not select an index
                Changes(); 
            }
            DisplayWatchList();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void newListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewWatchList();
        }

        private FileInfo GetFileFromUser()
        {
            var ofd = new OpenFileDialog();
            ofd.InitialDirectory = Global.Config.LastRomPath;
            ofd.Filter = "Watch Files (*.wch)|*.wch|All Files|*.*";
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

        private void OpenWatchFile()
        {

            var file = GetFileFromUser();
            if (file != null)
            {
                bool r = true;
                if (changes) r = AskSave();
                if (r)
                {
                    LoadWatchFile(file.FullName, false);
                    DisplayWatchList();
                }
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenWatchFile();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.Compare(currentWatchFile, "") == 0) return;

            if (changes)
                SaveWatchFile(currentWatchFile);
        }

        private FileInfo GetSaveFileFromUser()
        {
            var sfd = new SaveFileDialog();
            sfd.InitialDirectory = Global.Config.LastRomPath;
            sfd.Filter = "Watch Files (*.wch)|*.wch|All Files|*.*";
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
                SaveWatchFile(file.FullName);
                currentWatchFile = file.FullName;
            }
            MessageLabel.Text = Path.GetFileName(currentWatchFile) + " saved.";
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs();
        }

        private void appendFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var file = GetFileFromUser();
            if (file != null)
                LoadWatchFile(file.FullName, true);
            DisplayWatchList();
            Changes();
        }

        private void autoLoadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateAutoLoadRamWatch();
        }

        private void newWatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddNewWatch();
        }

        private void editWatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditWatch();
        }

        private void removeWatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RemoveWatch();
        }

        private void duplicateWatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DuplicateWatch();
        }

        private void moveUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MoveUp();
        }

        private void moveDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MoveDown();
        }

        private void RamWatch_Load(object sender, EventArgs e)
        {
            defaultWidth = this.Size.Width;     //Save these first so that the user can restore to its original size
            defaultHeight = this.Size.Height;

            if (Global.Config.RamWatchWndx >= 0 && Global.Config.RamWatchWndy >= 0)
                this.Location = new Point(Global.Config.RamWatchWndx, Global.Config.RamWatchWndy);

            if (Global.Config.RamWatchWidth >= 0 && Global.Config.RamWatchHeight >= 0)
            {
                this.Size = new System.Drawing.Size(Global.Config.RamWatchWidth, Global.Config.RamWatchHeight);
            }
        }

        private void filesToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            if (Global.Config.AutoLoadRamWatch == true)
                autoLoadToolStripMenuItem.Checked = true;
            else
                autoLoadToolStripMenuItem.Checked = false;

            if (string.Compare(currentWatchFile, "") == 0 || !changes)
            {
                saveToolStripMenuItem.Enabled = false;
            }
            else
            {
                saveToolStripMenuItem.Enabled = true;
            }
        }

        private void UpdateAutoLoadRamWatch()
        {
            if (Global.Config.AutoLoadRamWatch == true)
            {
                Global.Config.AutoLoadRamWatch = false;
                autoLoadToolStripMenuItem.Checked = false;
            }
            else
            {
                Global.Config.AutoLoadRamWatch = true;
                autoLoadToolStripMenuItem.Checked = true;
            }
        }
        private void recentToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            //Clear out recent Roms list
            //repopulate it with an up to date list
            recentToolStripMenuItem.DropDownItems.Clear();

            if (Global.Config.RecentWatches.IsEmpty())
            {
                recentToolStripMenuItem.DropDownItems.Add("None");
            }
            else
            {
                for (int x = 0; x < Global.Config.RecentWatches.Length(); x++)
                {
                    string path = Global.Config.RecentWatches.GetRecentFileByPosition(x);
                    var item = new ToolStripMenuItem();
                    item.Text = path;
                    item.Click += (o, ev) => LoadWatchFromRecent(path);
                    recentToolStripMenuItem.DropDownItems.Add(item);
                }
            }

            recentToolStripMenuItem.DropDownItems.Add("-");

            var clearitem = new ToolStripMenuItem();
            clearitem.Text = "&Clear";
            clearitem.Click += (o, ev) => Global.Config.RecentRoms.Clear();
            recentToolStripMenuItem.DropDownItems.Add(clearitem);

            var auto = new ToolStripMenuItem();
            auto.Text = "&Auto-Load";
            auto.Click += (o, ev) => UpdateAutoLoadRamWatch();
            if (Global.Config.AutoLoadRamWatch == true)
                auto.Checked = true;
            else
                auto.Checked = false;
            recentToolStripMenuItem.DropDownItems.Add(auto);
        }

        private void WatchListView_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (e.Label == null) //If no change
                return;
           string Str = e.Label.ToUpper().Trim();
           int index = e.Item;
           
           if (InputValidate.IsValidHexNumber(Str))
           {
               watchList[e.Item].address = int.Parse(Str, NumberStyles.HexNumber);
               EditWatchObject(index);
           }
           else
           {
               MessageBox.Show("Invalid number!"); //TODO: More parameters and better message
               WatchListView.Items[index].Text = watchList[index].address.ToString(); //TODO: Why doesn't the list view update to the new value? It won't until something else changes
           }
        }

        private void RamWatch_LocationChanged(object sender, EventArgs e)
        {
            Global.Config.RamWatchWndx = this.Location.X;
            Global.Config.RamWatchWndy = this.Location.Y;
        }

        private void RamWatch_Resize(object sender, EventArgs e)
        {
            Global.Config.RamWatchWidth = this.Right - this.Left;
            Global.Config.RamWatchHeight = this.Bottom - this.Top;
        }

        private void restoreWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Size = new System.Drawing.Size(defaultWidth, defaultHeight);
        }

        private void newToolStripButton_Click(object sender, EventArgs e)
        {
            NewWatchList();
        }

        private void openToolStripButton_Click(object sender, EventArgs e)
        {
            OpenWatchFile();
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            if (changes)
            {
                SaveWatchFile(currentWatchFile);
            }
            else
            {
                SaveAs();
            }
        }

        private void InsertSeparator()
        {
            Watch w = new Watch();
            w.type = atype.SEPARATOR;

            ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
            int x;
            if (indexes.Count > 0)
            {
                x = indexes[0];
                if (indexes[0] > 0)
                    watchList.Insert(indexes[0], w);
            }
            else
                watchList.Add(w);
            DisplayWatchList();
        }

        private void cutToolStripButton_Click(object sender, EventArgs e)
        {
            RemoveWatch();
        }

        private void NewWatchStripButton1_Click(object sender, EventArgs e)
        {
            AddNewWatch();
        }

        private void MoveUpStripButton1_Click(object sender, EventArgs e)
        {
            MoveUp();
        }

        private void MoveDownStripButton1_Click(object sender, EventArgs e)
        {
            MoveDown();
        }

        private void EditWatchToolStripButton1_Click(object sender, EventArgs e)
        {
            EditWatch();
        }

        private void DuplicateWatchToolStripButton_Click(object sender, EventArgs e)
        {
            DuplicateWatch();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            InsertSeparator();
        }

        private void insertSeparatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InsertSeparator();
        }

        private void PoketoolStripButton2_Click(object sender, EventArgs e)
        {
            PokeAddress();
        }

        private void PokeAddress()
        {
            ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
            RamPoke p = new RamPoke();

            if (indexes.Count > 0)
                p.SetWatchObject(watchList[indexes[0]]);
            p.location = GetPromptPoint();
            p.ShowDialog();
        }

        private void pokeAddressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PokeAddress();
        }

        private void watchesToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
            if (indexes.Count > 0)
            {
                editWatchToolStripMenuItem.Enabled = true;
                duplicateWatchToolStripMenuItem.Enabled = true;
                removeWatchToolStripMenuItem.Enabled = true;
                moveUpToolStripMenuItem.Enabled = true;
                moveDownToolStripMenuItem.Enabled = true;
            }
            else
            {
                editWatchToolStripMenuItem.Enabled = false;
                duplicateWatchToolStripMenuItem.Enabled = false;
                removeWatchToolStripMenuItem.Enabled = false;
                moveUpToolStripMenuItem.Enabled = false;
                moveDownToolStripMenuItem.Enabled = false;
            }
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditWatch();
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RemoveWatch();
        }

        private void duplicateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DuplicateWatch();
        }

        private void pokeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PokeAddress();
        }

        private void insertSeperatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InsertSeparator();
        }

        private void moveUpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MoveUp();
        }

        private void moveDownToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MoveDown();
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
            if (indexes.Count == 0)
            {
                contextMenuStrip1.Items[0].Visible = false;
                contextMenuStrip1.Items[1].Visible = false;
                contextMenuStrip1.Items[2].Visible = false;
                contextMenuStrip1.Items[3].Visible = false;
                contextMenuStrip1.Items[4].Visible = false;
                contextMenuStrip1.Items[6].Visible = false;
                contextMenuStrip1.Items[7].Visible = false;

            }
            else
            {
                for (int x = 0; x < contextMenuStrip1.Items.Count; x++)
                    contextMenuStrip1.Items[x].Visible = true;
            }
        }

        private void WatchListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
            if (indexes.Count > 0)
            {
                EditWatch();
            }
        }

        private void RamWatch_DragDrop(object sender, DragEventArgs e)
        {
            string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (filePaths[0].Contains(".wch")) //TODO: a less lazy way to check file extension?
            {
                LoadWatchFile(filePaths[0], false);
                DisplayWatchList();
            }
        }

        private void RamWatch_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;string[] filePaths = (string[]) e.Data.GetData(DataFormats.FileDrop);
        }

        private void WatchListView_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
