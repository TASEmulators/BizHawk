using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using LuaInterface;

namespace BizHawk.MultiClient
{
    public partial class LuaConsole : Form
    {
        //TODO: remember column widths

        int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
        int defaultHeight;

        List<LuaFiles> luaList = new List<LuaFiles>();
        LuaImplementation LuaImp;
        string lastLuaFile = "";

        private List<LuaFiles> GetLuaFileList()
        {
            List<LuaFiles> l = new List<LuaFiles>();
            for (int x = 0; x < luaList.Count; x++)
                l.Add(new LuaFiles(luaList[x]));

            return l;
        }

        public LuaConsole get()
        {
            return this;
        }

        public void AddText(string s)
        {
            OutputBox.Text += s;
        }

        public LuaConsole()
        {
            InitializeComponent();
            LuaImp = new LuaImplementation(this);
            Closing += (o, e) => SaveConfigSettings();
            LuaListView.QueryItemText += new QueryItemTextHandler(LuaListView_QueryItemText);
            LuaListView.QueryItemBkColor += new QueryItemBkColorHandler(LuaListView_QueryItemBkColor);
            LuaListView.VirtualMode = true;
        }

        private void LuaListView_QueryItemBkColor(int index, int column, ref Color color)
        {
            if (luaList[index].IsSeparator)
                color = Color.DarkGray;
            else if (!luaList[index].Enabled)
                color = this.BackColor;
        }

        private void LuaListView_QueryItemText(int index, int column, out string text)
        {
            text = "";
        }

        private void LuaConsole_Load(object sender, EventArgs e)
        {
            LoadConfigSettings();
        }

        public void Restart()
        {
            //Stop all Lua scripts
            for (int x = 0; x < luaList.Count; x++)
                luaList[x].Enabled = false;
        }

        private void SaveConfigSettings()
        {
            Global.Config.LuaConsoleWndx = this.Location.X;
            Global.Config.LuaConsoleWndy = this.Location.Y;
            Global.Config.LuaConsoleWidth = this.Right - this.Left;
            Global.Config.LuaConsoleHeight = this.Bottom - this.Top;
        }

        private void LoadConfigSettings()
        {
            defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
            defaultHeight = Size.Height;

            if (Global.Config.LuaConsoleSaveWindowPosition && Global.Config.LuaConsoleWndx >= 0 && Global.Config.LuaConsoleWndy >= 0)
                Location = new Point(Global.Config.LuaConsoleWndx, Global.Config.LuaConsoleWndy);

            if (Global.Config.LuaConsoleWidth >= 0 && Global.Config.LuaConsoleHeight >= 0)
            {
                Size = new System.Drawing.Size(Global.Config.LuaConsoleWidth, Global.Config.LuaConsoleHeight);
            }

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void restoreWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Size = new System.Drawing.Size(defaultWidth, defaultHeight);
        }

        private FileInfo GetFileFromUser()
        {
            var ofd = new OpenFileDialog();
            if (lastLuaFile.Length > 0)
                ofd.FileName = Path.GetFileNameWithoutExtension(lastLuaFile);
            ofd.InitialDirectory = Global.Config.LuaPath;
            ofd.Filter = "Lua Scripts (*.lua)|*.lua|All Files|*.*";
            ofd.RestoreDirectory = true;

            Global.Sound.StopSound();
            var result = ofd.ShowDialog();
            Global.Sound.StartSound();
            if (result != DialogResult.OK)
                return null;
            var file = new FileInfo(ofd.FileName);
            return file;
        }

        private void LoadLuaFile(string path)
        {
            LuaFiles l = new LuaFiles("", path, true);
            luaList.Add(l);

            LuaImp.DoLuaFile(path);
        }

        private void OpenLuaFile()
        {
            var file = GetFileFromUser();
            if (file != null)
            {
                LoadLuaFile(file.FullName);
                //DisplayLuaList();
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenLuaFile();
        }

        private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            saveWindowPositionToolStripMenuItem.Checked = Global.Config.LuaConsoleSaveWindowPosition;
        }

        private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.LuaConsoleSaveWindowPosition ^= true;
        }

        private void Toggle()
        {
            ListView.SelectedIndexCollection indexes = LuaListView.SelectedIndices;
            if (indexes.Count > 0)
            {
                for (int x = 0; x < indexes.Count; x++)
                {
                    if (luaList[indexes[x]].Enabled)
                        luaList[indexes[x]].Enabled = false;
                    else
                        luaList[indexes[x]].Enabled = true;
                }
                LuaListView.Refresh();
            }
            UpdateNumberOfScripts();
        }

        private void UpdateNumberOfScripts()
        {
            string message = "";
            int active = 0;
            for (int x = 0; x < luaList.Count; x++)
            {
                if (luaList[x].Enabled)
                    active++;
            }

            int L = luaList.Count;
            if (L == 1)
                message += L.ToString() + " cheat (" + active.ToString() + " active)";
            else if (L == 0)
                message += L.ToString() + " cheats";
            else
                message += L.ToString() + " cheats (" + active.ToString() + " active)";

            NumberOfScripts.Text = message;
        }

        private void LuaListView_DoubleClick(object sender, EventArgs e)
        {
            Toggle();
        }
    }
}
