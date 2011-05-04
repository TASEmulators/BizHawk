using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

namespace BizHawk.MultiClient
{
    public partial class PathConfig : Form
    {
        //TODO:
        // `exe` should be valid notation to mean path that the .exe is in ex: `exe`/NES
        // ./  and ../ are always always relative to base path
        // ./ and ../ in the base path are always relative to EXE path
        // `recent` notation for most recently used path
        //If "always use recent path for roms" is checked then base path of each platorm should be disabled
        //Path text boxes shoudl be anchored L + R and the remaining widgets anchored R
        //Find a way for base to always be absolute
        //Make all base path text boxes not allow  %recent%
        //All path text boxes should do some kind of error checking

        public PathConfig()
        {
            InitializeComponent();
        }

        private void PathConfig_Load(object sender, EventArgs e)
        {
            WatchBox.Text = Global.Config.WatchPath;
        }

        private void SaveSettings()
        {
            Global.Config.BasePath = BasePathBox.Text;
            Global.Config.WatchPath = WatchBox.Text;
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OK_Click(object sender, EventArgs e)
        {
            SaveSettings();
            this.Close();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //TODO: make base text box Controls[0] so this will focus on it
            //tabControl1.TabPages[tabControl1.SelectedIndex].Controls[0].Focus(); 
        }

        private void RecentForROMs_CheckedChanged(object sender, EventArgs e)
        {
            if (RecentForROMs.Checked)
            {
                NESROMsBox.Enabled = false;
                BrowseNESROMs.Enabled = false;
                Sega8ROMsBox.Enabled = false;
                Sega8BrowseROMs.Enabled = false;
                GenesisROMsBox.Enabled = false;
                GenesisBrowseROMs.Enabled = false;
                PCEROMsBox.Enabled = false;
                PCEBrowseROMs.Enabled = false;
                GBROMsBox.Enabled = false;
                GBBrowseROMs.Enabled = false;
                TI83ROMsBox.Enabled = false;
                TI83BrowseROMs.Enabled = false;     
            }
            else
            {
                NESROMsBox.Enabled = true;
                BrowseNESROMs.Enabled = true;
                Sega8ROMsBox.Enabled = true;
                Sega8BrowseROMs.Enabled = true;
                GenesisROMsBox.Enabled = true;
                GenesisBrowseROMs.Enabled = true;
                PCEROMsBox.Enabled = true;
                PCEBrowseROMs.Enabled = true;
                GBROMsBox.Enabled = true;
                GBBrowseROMs.Enabled = true;
                TI83ROMsBox.Enabled = true;
                TI83BrowseROMs.Enabled = true;
            }
        }

        private void BrowseFolder(TextBox box, string Name)
        {
            FolderBrowserDialog f = new FolderBrowserDialog();
            f.Description = "Set the directory for " + Name;
            f.SelectedPath = PathManager.MakeAbsolutePath(box.Text);
            DialogResult result = f.ShowDialog();
            if (result == DialogResult.OK)
                box.Text = f.SelectedPath;
        }

        private void BrowseWatch_Click(object sender, EventArgs e)
        {
            BrowseFolder(WatchBox, WatchDescription.Text);
        }

        private void BrowseBase_Click(object sender, EventArgs e)
        {
            BrowseFolder(BasePathBox, BaseDescription.Text);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }
    }
}
