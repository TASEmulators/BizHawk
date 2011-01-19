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
        //TODO: Recent files & autoload
        //Keep track of changes to watch list in order to prompt the user to save changes
        //TODO: implement separator feature

        List<Watch> watchList = new List<Watch>();   
        
        public RamWatch()
        {
            InitializeComponent();
        }

        //Debug
        void TempDisplayWatchInTempList(Watch watch)
        {
            string temp = watch.address + " " + watch.value + " " + watch.notes;
            listBox1.Items.Add(temp);
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

        bool LoadWatchFile(string path)
        {
            int y, z;
            var file = new FileInfo(path);
            if (file.Exists == false) return false;

            using (StreamReader sr = file.OpenText())
            {
                int count = 0;
                string s = "";
                string temp = "";
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
                        s = s.Substring(y, s.Length - y - 1);   //5 digit value representing the watch position number
                    }
                    else if (z != 4) 
                        continue;   //If not 4, something is wrong with this line, ignore it
                    count++;
                    Watch w = new Watch();

                    temp = s.Substring(0, s.IndexOf('\t'));
                    w.address = int.Parse(temp, NumberStyles.HexNumber);

                    y = s.IndexOf('\t') + 1;
                    s = s.Substring(y, s.Length - y - 1);   //Type
                    w.SetTypeByChar(s[0]);

                    y = s.IndexOf('\t') + 1;
                    s = s.Substring(y, s.Length - y - 1);   //Signed
                    w.SetSignedByChar(s[0]);

                    y = s.IndexOf('\t') + 1;
                    s = s.Substring(y, s.Length - y - 1);   //Endian
                    y = Int16.Parse(s[0].ToString());
                    if (y == 0)
                        w.bigendian = false;
                    else
                        w.bigendian = true;
                                        
                    w.notes =  s.Substring(2, s.Length - 2);   //User notes

                    watchList.Add(w);
                }

                //Update the number of watches
                listBox1.Items.Clear();
                WatchCountLabel.Text = count.ToString() + " watches";
            }

            return true;
        }

        void AddNewWatch()
        {
        }

        void EditWatch()
        {
        }

        void RemoveWatch()
        {
        }

        void DuplicateWatch()
        {
        }

        void MoveUp()
        {
        }

        void MoveDown()
        {
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void newListToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.InitialDirectory = Global.Config.LastRomPath;
            ofd.Filter = "Watch Files (*.wch)|*.wch|All Files|*.*";
            ofd.RestoreDirectory = true;

            Global.Sound.StopSound();
            var result = ofd.ShowDialog();
            Global.Sound.StartSound();
            if (result != DialogResult.OK)
                return;
            var file = new FileInfo(ofd.FileName);
            Global.Config.LastRomPath = file.DirectoryName;
            LoadWatchFile(file.FullName);

            //Debug
            for (int x = 0; x < watchList.Count; x++)
                TempDisplayWatchInTempList(watchList[x]);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void appendFileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void autoLoadToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
        {

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
            //TODO: Debug
            Watch watch1 = new Watch();
            watch1.notes = "Test1";
            watchList.Add(watch1);
                               
            ListViewItem item1 = new ListViewItem(watch1.address.ToString(), 0);
            WatchListView.Items.Add(item1);

            item1 = new ListViewItem(watch1.value.ToString(), 0);
            WatchListView.Items.Add(item1);

            item1 = new ListViewItem(watch1.notes, 0);
            WatchListView.Items.Add(item1);

            //Debug
            for (int x = 0; x < watchList.Count; x++)
                TempDisplayWatchInTempList(watchList[x]);
        }

        private void filesToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            if (Global.Config.AutoLoadRamWatch == true)
                autoLoadToolStripMenuItem.Checked = true;
            else
                autoLoadToolStripMenuItem.Checked = false;
        }
    }
}
