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
    public partial class RamWatch : Form
    {
        //TODO: 
        //implement separator feature
        //Display value differently based on signed or hex, endian, type
        //Currently address is 4 digit hex, but at some point it needs to be smart enough to adjust size based on the emulator core used
        //Make Edit/Add/Duplicate Watch windows appear in relation to the listview box
        //Make a context menu for add/remove/Dup/etc, make the context menu & edit watch windows appear in relation to where they right clicked
        //TODO: Call AskSave in main client close function
        //TODO: make so that only 1 instance of ram watch can be open at once (so that ask save can be reliable, as well as the watch button in ram search dialogs
        int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
        int defaultHeight;
        List<Watch> watchList = new List<Watch>();
        string currentWatchFile = "";
        bool changes = false;

        public RamWatch()
        {
            InitializeComponent();
        }

        public int HowMany(string str, char c)  //Shouldn't something like this exist already? Counts how many times c in in str
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
            }
        }
        
        private  bool SaveWatchFile(string path)
        {
            var file = new FileInfo(path);
            //if (file.Exists == true) //TODO: prompt to overwrite

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
                //Update the number of watches
                WatchCountLabel.Text = count.ToString() + " watches";
            }

            return true;
        }

        void AddNewWatch()
        {
            RamWatchNewWatch r = new RamWatchNewWatch();
            r.ShowDialog();
            if (r.userSelected == true)
            {
                //TODO: check for duplicates before adding? All parameters would have to match, otherwise it should be allowed
                watchList.Add(r.watch);
                DisplayWatchList();
            }
        }

        void EditWatch()
        {
            ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
            RamWatchNewWatch r = new RamWatchNewWatch();
            int x = indexes[0];
            r.SetToEditWatch(watchList[x], "Edit Watch");
            r.ShowDialog();

            if (r.userSelected == true)
            {
                changes = true;
                watchList[x] = r.watch;
                DisplayWatchList();
            }
        }

        void RemoveWatch()
        {
            changes = true;
            ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
            foreach (int index in indexes)
            {
                watchList.Remove(watchList[index]);
            }
            DisplayWatchList();
        }

        void DuplicateWatch()
        {
            ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
            RamWatchNewWatch r = new RamWatchNewWatch();
            int x = indexes[0];
            r.SetToEditWatch(watchList[x], "Duplicate Watch");
            r.ShowDialog();

            if (r.userSelected == true)
            {
                changes = true;
                watchList.Add(watchList[x]);
                DisplayWatchList();
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
                changes = true; //Note: here it will get flagged many times redundantly potnetially, but this avoids it being flag falsely when the user did not select an index
            }
            DisplayWatchList();
           //TODO: Set highlighted items to be what the user had selected (in their new position)
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
                changes = true; //Note: here it will get flagged many times redundantly potnetially, but this avoids it being flag falsely when the user did not select an index
            }
            DisplayWatchList();
            //TODO: Set highlighted items to be what the user had selected (in their new position)
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
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

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
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
                SaveWatchFile(file.FullName);
            //TODO: inform the user (with using an annoying message box)
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
            changes = true;
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

        private void DisplayWatchList()
        {
            WatchListView.Items.Clear();
            for (int x = 0; x < watchList.Count; x++)
            {
                ListViewItem item = new ListViewItem(String.Format("{0:X4}", watchList[x].address));
                item.SubItems.Add(watchList[x].value.ToString());
                item.SubItems.Add(watchList[x].notes);
                WatchListView.Items.Add(item);
            }          
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
                    recentToolStripMenuItem.DropDownItems.Add(item); //TODO: truncate this to a nice size
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
            // Determine if label is changed by checking for null.
            if (e.Label == null)
            return;

   // ASCIIEncoding is used to determine if a number character has been entered.
   ASCIIEncoding AE = new ASCIIEncoding();
   // Convert the new label to a character array.
   char[] temp = e.Label.ToCharArray();

   // Check each character in the new label to determine if it is a number.
   for(int x=0; x < temp.Length; x++)
   {
      // Encode the character from the character array to its ASCII code.
      byte[] bc = AE.GetBytes(temp[x].ToString());

      // Determine if the ASCII code is within the valid range of numerical values.
      if(bc[0] > 47 && bc[0] < 58)
      {
         // Cancel the event and return the lable to its original state.
         e.CancelEdit = true;
         // Display a MessageBox alerting the user that numbers are not allowed.
         MessageBox.Show ("The text for the item cannot contain numerical values.");
         // Break out of the loop and exit.
         return;
      }
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

        private void WatchListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            //TODO: debug/testing
            ListView.SelectedIndexCollection i = this.WatchListView.SelectedIndices;
            i = WatchListView.SelectedIndices;
        }
    }
}
