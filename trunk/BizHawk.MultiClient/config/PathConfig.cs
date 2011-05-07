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
        //Paths should default to their platform specific base before the main base! This will have to be done by specifically calling methods for each platform type
        //TODO config path under base, config will default to %exe%

        public PathConfig()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void PathConfig_Load(object sender, EventArgs e)
        {
            BasePathBox.Text = Global.Config.BasePath;
            
            NESBaseBox.Text = Global.Config.BaseNES;
            NESROMsBox.Text = Global.Config.PathNESROMs;
            NESSavestatesBox.Text = Global.Config.PathNESSavestates;
            NESSaveRAMBox.Text = Global.Config.PathNESSaveRAM;
            NESScreenshotsBox.Text = Global.Config.PathNESScreenshots;
            NESCheatsBox.Text = Global.Config.PathNESCheats;
            
            Sega8BaseBox.Text = Global.Config.BaseSMS;
            Sega8ROMsBox.Text = Global.Config.PathSMSROMs;
            Sega8SavestatesBox.Text = Global.Config.PathSMSSavestates;
            Sega8SaveRAM.Text = Global.Config.PathSMSSaveRAM;
            Sega8ScreenshotsBox.Text = Global.Config.PathSMSScreenshots;
            Sega8CheatsBox.Text = Global.Config.PathSMSCheats;

            PCEBaseBox.Text = Global.Config.BasePCE;
            PCEROMsBox.Text = Global.Config.PathPCEROMs;
            PCESavestatesBox.Text = Global.Config.PathPCESavestates;
            PCESaveRAMBox.Text = Global.Config.PathPCESaveRAM;
            PCEScreenshotsBox.Text = Global.Config.PathPCEScreenshots;
            PCECheatsBox.Text = Global.Config.PathPCECheats;

            GenesisBaseBox.Text = Global.Config.BaseGenesis;
            GenesisROMsBox.Text = Global.Config.PathGenesisROMs;
            GenesisSavestatesBox.Text = Global.Config.PathGenesisScreenshots;
            GenesisSaveRAMBox.Text = Global.Config.PathGenesisSaveRAM;
            GenesisScreenshotsBox.Text = Global.Config.PathGenesisScreenshots;
            GenesisCheatsBox.Text = Global.Config.PathGenesisCheats;

            GBBaseBox.Text = Global.Config.BaseGameboy;
            GBROMsBox.Text = Global.Config.PathGBROMs;
            GBSavestatesBox.Text = Global.Config.PathGBSavestates;
            GBSaveRAMBox.Text = Global.Config.PathGBSaveRAM;
            GBScreenshotsBox.Text = Global.Config.PathGBScreenshots;
            GBCheatsBox.Text = Global.Config.PathGBCheats;

            TI83BaseBox.Text = Global.Config.BaseTI83;
            TI83ROMsBox.Text = Global.Config.PathTI83ROMs;
            TI83SavestatesBox.Text = Global.Config.PathTI83Savestates;
            TI83SaveRAMBox.Text = Global.Config.PathTI83SaveRAM;
            TI83ScreenshotsBox.Text = Global.Config.PathTI83Screenshots;
            TI83CheatsBox.Text = Global.Config.PathTI83Cheats;

            MoviesBox.Text = Global.Config.MoviesPath;
            LuaBox.Text = Global.Config.LuaPath;
            WatchBox.Text = Global.Config.WatchPath;
            AVIBox.Text = Global.Config.AVIPath;
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
            f.SelectedPath = PathManager.MakeAbsolutePath(box.Text, "");
            DialogResult result = f.ShowDialog();
            if (result == DialogResult.OK)
                box.Text = f.SelectedPath;
        }

        private void BrowseFolder(TextBox box, string Name, string System)
        {
            FolderBrowserDialog f = new FolderBrowserDialog();
            f.Description = "Set the directory for " + Name;
            f.SelectedPath = PathManager.MakeAbsolutePath(box.Text, System);
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
        
        private void BrowseAVI_Click(object sender, EventArgs e)
        {
            BrowseFolder(AVIBox, AVIDescription.Text);
        }

        private void BrowseLua_Click(object sender, EventArgs e)
        {
            BrowseFolder(LuaBox, LuaDescription.Text);
        }

        private void BrowseMovies_Click(object sender, EventArgs e)
        {
            BrowseFolder(MoviesBox, MoviesDescription.Text);
        }

        private void BrowseNESBase_Click(object sender, EventArgs e)
        {
            BrowseFolder(NESBaseBox, NESBaseDescription.Text);
        }

        private void BrowseNESROMs_Click(object sender, EventArgs e)
        {
            BrowseFolder(NESROMsBox, NESROMsDescription.Text, "NES");
        }

        private void BrowseNESSavestates_Click(object sender, EventArgs e)
        {
            BrowseFolder(NESSavestatesBox, NESSavestatesDescription.Text, "NES");
        }

        private void BrowseNESSaveRAM_Click(object sender, EventArgs e)
        {
            BrowseFolder(NESSaveRAMBox, NESSaveRAMDescription.Text, "NES");
        }

        private void BrowseNESScreenshots_Click(object sender, EventArgs e)
        {
            BrowseFolder(NESScreenshotsBox, NESScreenshotsDescription.Text, "NES");
        }

        private void NESBrowseCheats_Click(object sender, EventArgs e)
        {
            BrowseFolder(NESCheatsBox, NESCheatsDescription.Text, "NES");
        }
    }
}
