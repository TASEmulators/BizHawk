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
        //Restore window size should restore column order as well
        //When receiving a watch from a different domain, should something be done?

        int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
        int defaultHeight;
        int defaultAddressWidth;
        int defaultValueWidth;
        int defaultPrevWidth;
        int defaultChangeWidth;
        int NotesWidth;

        string systemID = "NULL";
        MemoryDomain Domain = new MemoryDomain("NULL", 1, Endian.Little, addr => 0, (a, v) => { });
        List<Watch> watchList = new List<Watch>();
        string currentWatchFile = "";
        bool changes = false;
        List<ToolStripMenuItem> domainMenuItems = new List<ToolStripMenuItem>();

        public List<Watch> GetRamWatchList()
        {
            List<Watch> w = new List<Watch>();
            for (int x = 0; x < watchList.Count; x++)
                w.Add(new Watch(watchList[x]));

            return w;
        }

        public void DisplayWatchList()
        {
            WatchListView.ItemCount = watchList.Count;
        }

        public void UpdateValues()
        {
            if (!this.IsHandleCreated || this.IsDisposed) return;
            for (int x = 0; x < watchList.Count; x++)
            {
                watchList[x].prev = watchList[x].value;
                watchList[x].PeekAddress(Domain);
                if (watchList[x].value != watchList[x].prev)
                    watchList[x].changecount++;
            }
            WatchListView.Refresh();       
        }

        public void AddWatch(Watch w)
        {
            watchList.Add(w);
            DisplayWatchList();
        }

        private void LoadConfigSettings()
        {
            defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
            defaultHeight = Size.Height;
            defaultAddressWidth = WatchListView.Columns[0].Width;
            defaultValueWidth = WatchListView.Columns[1].Width;
            defaultPrevWidth = WatchListView.Columns[2].Width;
            defaultChangeWidth = WatchListView.Columns[3].Width;
            NotesWidth = WatchListView.Columns[4].Width;

            if (Global.Config.RamWatchSaveWindowPosition && Global.Config.RamWatchWndx >= 0 && Global.Config.RamWatchWndy >= 0)
                Location = new Point(Global.Config.RamWatchWndx, Global.Config.RamWatchWndy);

            if (Global.Config.RamWatchWidth >= 0 && Global.Config.RamWatchHeight >= 0)
            {
                Size = new System.Drawing.Size(Global.Config.RamWatchWidth, Global.Config.RamWatchHeight);
            }
            SetPrevColumn(Global.Config.RamWatchShowPrevColumn);
            SetChangesColumn(Global.Config.RamWatchShowChangeColumn);
            if (Global.Config.RamWatchAddressWidth > 0)
                WatchListView.Columns[0].Width = Global.Config.RamWatchAddressWidth;
            if (Global.Config.RamWatchValueWidth > 0)
                WatchListView.Columns[1].Width = Global.Config.RamWatchValueWidth;
            if (Global.Config.RamWatchPrevWidth > 0)
                WatchListView.Columns[2].Width = Global.Config.RamWatchPrevWidth;
            if (Global.Config.RamWatchChangeWidth > 0)
                WatchListView.Columns[3].Width = Global.Config.RamWatchChangeWidth;
            if (Global.Config.RamWatchNotesWidth > 0)
                WatchListView.Columns[4].Width = Global.Config.RamWatchNotesWidth;
        }

        public void SaveConfigSettings()
        {
            Global.Config.RamWatchAddressWidth = WatchListView.Columns[0].Width;
            Global.Config.RamWatchValueWidth   = WatchListView.Columns[1].Width;
            Global.Config.RamWatchPrevWidth    = WatchListView.Columns[2].Width;
            Global.Config.RamWatchChangeWidth  = WatchListView.Columns[3].Width;
            Global.Config.RamWatchNotesWidth   = WatchListView.Columns[4].Width;

            Global.Config.RamWatchWndx = this.Location.X;
            Global.Config.RamWatchWndy = this.Location.Y;
            Global.Config.RamWatchWidth = this.Right - this.Left;
            Global.Config.RamWatchHeight = this.Bottom - this.Top;
        }

        public RamWatch()
        {
            InitializeComponent();
            WatchListView.QueryItemText += new QueryItemTextHandler(WatchListView_QueryItemText);
            WatchListView.QueryItemBkColor += new QueryItemBkColorHandler(WatchListView_QueryItemBkColor);
            WatchListView.VirtualMode = true;
            Closing += (o, e) => SaveConfigSettings(); 
        }

        protected override void  OnClosing(CancelEventArgs e)
         {
            if (!AskSave())
                e.Cancel = true;    
            base.OnClosing(e);
         }

        private void WatchListView_QueryItemBkColor(int index, int column, ref Color color)
        {
            if (watchList[index].type == atype.SEPARATOR)
                color = this.BackColor;
            if (Global.MainForm.Cheats1.IsActiveCheat(Domain, watchList[index].address))
                color = Color.LightCyan;
        }

        void WatchListView_QueryItemText(int index, int column, out string text)
        {
            text = "";
            if (column == 0)    //Address
            {
                if (watchList[index].type == atype.SEPARATOR)
                    text = "";
                else
                    text = String.Format("{0:X" + GetNumDigits((Domain.Size - 1)).ToString() + "}", watchList[index].address);
            }
            if (column == 1) //Value
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
            if (column == 2) //Prev
            {
                if (watchList[index].type == atype.SEPARATOR)
                    text = "";
                else
                {
                    if (Global.Config.RamWatchShowChangeFromPrev)
                    {
                        int x = watchList[index].value - watchList[index].prev;
                        if (x < 0)
                            text = x.ToString();
                        else
                            text = "+" + x.ToString();
                    }
                    else
                    {
                        switch (watchList[index].signed)
                        {
                            case asigned.HEX:
                                switch (watchList[index].type)
                                {
                                    case atype.BYTE:
                                        text = String.Format("{0:X2}", watchList[index].prev);
                                        break;
                                    case atype.WORD:
                                        text = String.Format("{0:X4}", watchList[index].prev);
                                        break;
                                    case atype.DWORD:
                                        text = String.Format("{0:X8}", watchList[index].prev);
                                        break;
                                }
                                break;
                            case asigned.SIGNED:
                                text = ((sbyte)watchList[index].prev).ToString();
                                break;
                            case asigned.UNSIGNED:
                                text = watchList[index].prev.ToString();
                                break;
                        }
                    }
                }
            }
            if (column == 3) //Change Counts
            {
                text = watchList[index].changecount.ToString();
            }
            if (column == 4) //Notes
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
                string str = "Domain " + Domain.Name + "\n";
                
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

        private int GetDomainPos(string name)
        {
            //Attempts to find the memory domain by name, if it fails, it defaults to index 0
            for (int x = 0; x < Global.Emulator.MemoryDomains.Count; x++)
            {
                if (Global.Emulator.MemoryDomains[x].Name == name)
                    return x;
            }
            return 0;
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

                    if (s.Substring(0, 6) == "Domain")
                        SetMemoryDomain(GetDomainPos(s.Substring(7, s.Length - 7)));
                    
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
            if (indexes[0] == 0) return;
            foreach (int index in indexes)
            {
                temp = watchList[index];
                watchList.Remove(watchList[index]);
                watchList.Insert(index - 1, temp);
                
                //Note: here it will get flagged many times redundantly potentially, 
                //but this avoids it being flagged falsely when the user did not select an index
                Changes();
            }
            List<int> i = new List<int>();
            for (int z = 0; z < indexes.Count; z++)
                i.Add(indexes[z]-1);

            WatchListView.SelectedIndices.Clear();
            for (int z = 0; z < i.Count; z++)
                WatchListView.SelectItem(i[z], true);

            
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

            List<int> i = new List<int>();
            for (int z = 0; z < indexes.Count; z++)
                i.Add(indexes[z] + 1);

            WatchListView.SelectedIndices.Clear();
            for (int z = 0; z < i.Count; z++)
                WatchListView.SelectItem(i[z], true);

            DisplayWatchList();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!AskSave())
                return;

            this.Close();
        }

        private void newListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewWatchList();
        }

        private FileInfo GetFileFromUser()
        {
            var ofd = new OpenFileDialog();
            if (currentWatchFile.Length > 0)
                ofd.FileName = Path.GetFileNameWithoutExtension(currentWatchFile);
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
            if (currentWatchFile.Length > 0)
                sfd.FileName = Path.GetFileNameWithoutExtension(currentWatchFile);
            else if (!(Global.Emulator is NullEmulator))
                sfd.FileName = Global.Game.Name;
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
                MessageLabel.Text = Path.GetFileName(currentWatchFile) + " saved.";
            }
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
            LoadConfigSettings();
            SetMemoryDomainMenu();
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
            autoLoadToolStripMenuItem.Checked = Global.Config.AutoLoadRamWatch ^= true;
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
            clearitem.Click += (o, ev) => Global.Config.RecentWatches.Clear();
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

        private void restoreWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Size = new System.Drawing.Size(defaultWidth, defaultHeight);

            WatchListView.Columns[0].Width = Global.Config.RamWatchAddressWidth;
            WatchListView.Columns[1].Width = Global.Config.RamWatchValueWidth;
            if (showPreviousValueToolStripMenuItem.Checked)
                WatchListView.Columns[2].Width = Global.Config.RamWatchPrevWidth;
            else
                WatchListView.Columns[2].Width = 0;
            if (showChangeCountsToolStripMenuItem.Checked)
                WatchListView.Columns[3].Width = Global.Config.RamWatchChangeWidth;
            else
                WatchListView.Columns[3].Width = 0;
            WatchListView.Columns[4].Width = Global.Config.RamWatchNotesWidth;
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
                pokeAddressToolStripMenuItem.Enabled = true;
                freezeAddressToolStripMenuItem.Enabled = true;
            }
            else
            {
                editWatchToolStripMenuItem.Enabled = false;
                duplicateWatchToolStripMenuItem.Enabled = false;
                removeWatchToolStripMenuItem.Enabled = false;
                moveUpToolStripMenuItem.Enabled = false;
                moveDownToolStripMenuItem.Enabled = false;
                pokeAddressToolStripMenuItem.Enabled = false;
                freezeAddressToolStripMenuItem.Enabled = false;
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
                contextMenuStrip1.Items[5].Visible = false;
                contextMenuStrip1.Items[6].Visible = false;
                contextMenuStrip1.Items[8].Visible = false;
                contextMenuStrip1.Items[9].Visible = false;

            }
            else
            {
                for (int x = 0; x < contextMenuStrip1.Items.Count; x++)
                    contextMenuStrip1.Items[x].Visible = true;
            }

            if (Global.Config.RamWatchShowChangeColumn)
                contextMenuStrip1.Items[11].Text = "Hide change counts";
            else
                contextMenuStrip1.Items[11].Text = "Show change counts";

            if (Global.Config.RamWatchShowPrevColumn)
                contextMenuStrip1.Items[12].Text = "Hide previous value";
            else
                contextMenuStrip1.Items[12].Text = "Show previous value";

            if (Global.Config.RamWatchShowChangeFromPrev)
                contextMenuStrip1.Items[13].Text = "Display Previous value as previous";
            else
                contextMenuStrip1.Items[13].Text = "Display Previosu value as change amount";
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

        private void ClearChangeCounts()
        {
            for (int x = 0; x < watchList.Count; x++)
                watchList[x].changecount = 0;
            DisplayWatchList();
            MessageLabel.Text = "Change counts cleared";
        }

        private void ClearChangeCountstoolStripButton_Click(object sender, EventArgs e)
        {
            ClearChangeCounts();
        }

        private void clearChangeCountsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearChangeCounts();
        }

        

        private void showChangeCountsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.RamWatchShowChangeColumn ^= true;
            SetChangesColumn(Global.Config.RamWatchShowChangeColumn);
        }

        private void SetChangesColumn(bool show)
        {
            Global.Config.RamWatchShowChangeColumn = show;
            showChangeCountsToolStripMenuItem.Checked = show;
            if (show)
                WatchListView.Columns[3].Width = 54;
            else
                WatchListView.Columns[3].Width = 0;
        }

        private void SetPrevColumn(bool show)
        {
            Global.Config.RamWatchShowPrevColumn = show;
            showPreviousValueToolStripMenuItem.Checked = show;
            if (show)
                WatchListView.Columns[2].Width = 59;
            else
                WatchListView.Columns[2].Width = 0;
        }

        private void showPreviousValueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.RamWatchShowPrevColumn ^= true;
            SetPrevColumn(Global.Config.RamWatchShowPrevColumn);
        }

        private void prevValueShowsChangeAmountToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.RamWatchShowChangeFromPrev ^= true;
            DisplayWatchList();
        }

        private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            prevValueShowsChangeAmountToolStripMenuItem.Checked = Global.Config.RamWatchShowChangeFromPrev;
        }

        private void viewInHexEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
            if (indexes.Count > 0)
            {
                Global.MainForm.LoadHexEditor();
                Global.MainForm.HexEditor1.GoToAddress(watchList[indexes[0]].address);
            }
        }

        private int GetNumDigits(Int32 i)
        {
            //if (i == 0) return 0;
            //if (i < 0x10) return 1;
            //if (i < 0x100) return 2;
            //if (i < 0x1000) return 3; //adelikat: commenting these out because I decided that regardless of domain, 4 digits should be the minimum
            if (i < 0x10000) return 4;
            if (i < 0x1000000) return 6;
            else return 8;
        }

        private void freezeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FreezeAddress();
        }

        private int WORDGetLowerByte(int value)
        {
            return value / 256;
        }

        private int WORDGetUpperByte(int value)
        {
            return value >> 2;
        }

        private void FreezeAddress()
        {
            ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
            if (indexes.Count > 0)
            {
                switch (watchList[indexes[0]].type)
                {
                    case atype.BYTE:
                        Cheat c = new Cheat("", watchList[indexes[0]].address, (byte)watchList[indexes[0]].value,
                            true, Domain);
                        Global.MainForm.Cheats1.AddCheat(c);
                        break;
                    case atype.WORD:
                        {
                            byte low = (byte)(watchList[indexes[0]].value / 256);
                            byte high = (byte)(watchList[indexes[0]].value);
                            int a1 = watchList[indexes[0]].address;
                            int a2 = watchList[indexes[0]].address + 1;
                            if (watchList[indexes[0]].bigendian)
                            {
                                Cheat c1 = new Cheat("", a1, low, true, Domain);
                                Cheat c2 = new Cheat("", a2, high, true, Domain);
                                Global.MainForm.Cheats1.AddCheat(c1);
                                Global.MainForm.Cheats1.AddCheat(c2);
                            }
                            else
                            {
                                Cheat c1 = new Cheat("", a1, high, true, Domain);
                                Cheat c2 = new Cheat("", a2, low, true, Domain);
                                Global.MainForm.Cheats1.AddCheat(c1);
                                Global.MainForm.Cheats1.AddCheat(c2);
                            }
                        }
                        break;
                    case atype.DWORD:
                        {
                            byte HIWORDhigh = (byte)(watchList[indexes[0]].value / 0x1000000);
                            byte HIWORDlow = (byte)(watchList[indexes[0]].value / 0x10000);
                            byte LOWORDhigh = (byte)(watchList[indexes[0]].value / 0x100);
                            byte LOWORDlow = (byte)(watchList[indexes[0]].value);
                            int a1 = watchList[indexes[0]].address;
                            int a2 = watchList[indexes[0]].address + 1;
                            int a3 = watchList[indexes[0]].address + 2;
                            int a4 = watchList[indexes[0]].address + 3;
                            if (watchList[indexes[0]].bigendian)
                            {
                                Cheat c1 = new Cheat("", a1, HIWORDhigh, true, Domain);
                                Cheat c2 = new Cheat("", a2, HIWORDlow, true, Domain);
                                Cheat c3 = new Cheat("", a3, LOWORDhigh, true, Domain);
                                Cheat c4 = new Cheat("", a4, LOWORDlow, true, Domain);
                                Global.MainForm.Cheats1.AddCheat(c1);
                                Global.MainForm.Cheats1.AddCheat(c2);
                                Global.MainForm.Cheats1.AddCheat(c3);
                                Global.MainForm.Cheats1.AddCheat(c4);
                            }
                            else
                            {
                                Cheat c1 = new Cheat("", a1, LOWORDlow, true, Domain);
                                Cheat c2 = new Cheat("", a2, LOWORDhigh, true, Domain);
                                Cheat c3 = new Cheat("", a3, HIWORDlow, true, Domain);
                                Cheat c4 = new Cheat("", a4, HIWORDhigh, true, Domain);
                                Global.MainForm.Cheats1.AddCheat(c1);
                                Global.MainForm.Cheats1.AddCheat(c2);
                                Global.MainForm.Cheats1.AddCheat(c3);
                                Global.MainForm.Cheats1.AddCheat(c4);
                            }
                        }
                        break;
                }
            }
        }

        private void freezeAddressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FreezeAddress();
        }

        private void FreezetoolStripButton2_Click(object sender, EventArgs e)
        {
            FreezeAddress();
        }

        private void SetPlatformAndMemoryDomainLabel()
        {
            string memoryDomain = Domain.ToString();
            systemID = Global.Emulator.SystemId;
            MemDomainLabel.Text = systemID + " " + memoryDomain;
        }

        private void SetMemoryDomain(int pos)
        {
            if (pos < Global.Emulator.MemoryDomains.Count)  //Sanity check
            {
                Domain = Global.Emulator.MemoryDomains[pos];
            }
            SetPlatformAndMemoryDomainLabel();
            Update();
        }

        private void SetMemoryDomainMenu()
        {
            memoryDomainsToolStripMenuItem.DropDownItems.Clear();
            if (Global.Emulator.MemoryDomains.Count > 0)
            {
                for (int x = 0; x < Global.Emulator.MemoryDomains.Count; x++)
                {
                    string str = Global.Emulator.MemoryDomains[x].ToString();
                    var item = new ToolStripMenuItem();
                    item.Text = str;
                    {
                        int z = x;
                        item.Click += (o, ev) => SetMemoryDomain(z);
                    }
                    if (x == 0)
                    {
                        SetMemoryDomain(x);
                    }
                    memoryDomainsToolStripMenuItem.DropDownItems.Add(item);
                    domainMenuItems.Add(item);
                }
            }
            else
                memoryDomainsToolStripMenuItem.Enabled = false;
        }

        private void CheckDomainMenuItems()
        {
            for (int x = 0; x < domainMenuItems.Count; x++)
            {
                if (Domain.Name == domainMenuItems[x].Text)
                    domainMenuItems[x].Checked = true;
                else
                    domainMenuItems[x].Checked = false;
            }
        }

        private void memoryDomainsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            CheckDomainMenuItems();
        }
    }
}