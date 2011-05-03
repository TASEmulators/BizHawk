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

        string EXEPath; //TODO: public variable in main, populated at run time

        public PathConfig()
        {
            InitializeComponent();
        }

        private void PathConfig_Load(object sender, EventArgs e)
        {
            EXEPath = GetExePathAbsolute();
            WatchBox.Text = Global.Config.WatchPath;
        }

        //--------------------------------------
        //TODO: Move these to Main (or util if EXE path is same
        private string GetExePathAbsolute()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
        }

        private string GetBasePathAbsolute()
        {
            //Gets absolute base as derived from EXE

            if (Global.Config.BasePath.Length < 1) //If empty, then EXE path
                return GetExePathAbsolute();
            
            if (Global.Config.BasePath.Substring(0,5) == "%exe%")
                return GetExePathAbsolute();
            if (Global.Config.BasePath[0] == '.')
            {
                if (Global.Config.BasePath.Length == 1)
                    return GetExePathAbsolute();
                else
                {
                    if (Global.Config.BasePath.Length == 2 &&
                        Global.Config.BasePath == ".\\")
                        return GetExePathAbsolute();
                    else
                    {
                        string tmp = Global.Config.BasePath;
                        tmp = tmp.Remove(0, 1);
                        tmp = tmp.Insert(0, GetExePathAbsolute());
                    }
                }
            }
            
            if (Global.Config.BasePath.Substring(0, 2) == "..")
                return RemoveParents(Global.Config.BasePath, GetExePathAbsolute());   
            
            //In case of error, return EXE path
            return GetExePathAbsolute();
        }
        

        private string MakeAbsolutePath(string path)
        {
            //This function translates relative path and special identifiers in absolute paths
            
            if (path.Length < 1)
                return GetBasePathAbsolute();

            if (path == "%recent%")
            {
                //return last used directory (environment path)
            }

            if (path.Substring(0, 5) == "%exe%")
            {
                if (path.Length == 5)
                    return GetExePathAbsolute();
                else
                {
                    string tmp = path.Remove(0, 5);
                    tmp = tmp.Insert(0, GetExePathAbsolute());
                    return tmp;
                }
            }

            if (path[0] == '.')
            {
                if (path.Length == 1)
                    return GetBasePathAbsolute();
                else
                {
                    string tmp = path.Remove(0, 1);
                    tmp = tmp.Insert(0, GetBasePathAbsolute());
                    return tmp;
                }
            }
            
            //If begins wtih .. do alorithm to determine how many ..\.. combos and deal with accordingly, return drive letter only if too many ..

            if ((path[0] > 'A' && path[0] < 'Z') || (path[0] > 'a' && path[0] < 'z'))
            {
                if (path.Length > 2 && path[1] == ':' && path[2] == '\\')
                    return path;
                else
                    return GetExePathAbsolute(); //bad path
            }

            //all pad paths default to EXE
            return GetExePathAbsolute();
        }

        private string RemoveParents(string path, string workingpath)
        {
            //determines number of parents, then removes directories from working path, return absolute path result
            //Ex: "..\..\Bob\", "C:\Projects\Emulators\Bizhawk" will return "C:\Projects\Bob\" 
            int x = NumParentDirectories(path);
            if (x > 0)
            {
                int y = HowMany(path, "..\\");
                int z = HowMany(workingpath, "\\");
                if (y >= z)
                {
                    //Return drive letter only, working path must be absolute?
                }
                return "";
            }
            else return path;            
        }

        private int NumParentDirectories(string path)
        {
            //determine the number of parent directories in path and return result
            int x = HowMany(path, '\\');
            if (x > 0)
            {
                return HowMany(path, "..\\");
            }
            return 0;
        }

        public int HowMany(string str, string s)
        {
            int count = 0;
            for (int x = 0; x < (str.Length - s.Length); x++)
            {
                if (str.Substring(x, s.Length) == s)
                    count++;
            }
            return count;
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

        //-------------------------------------------------------

        private void SaveSettings()
        {
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

        private void BrowseWatch_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog f = new FolderBrowserDialog();
            f.Description = "Set the directory for Watch (.wch) files";
            f.SelectedPath = "C:\\Repos";
            //TODO: find a way to set root folder to base
            DialogResult result = f.ShowDialog();
            if (result == DialogResult.OK)
                WatchBox.Text = f.SelectedPath;
        }
    }
}
