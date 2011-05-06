using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
    public partial class LuaConsole : Form
    {
        int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
        int defaultHeight;
        int defaultNameWidth;
        int defaultAddressWidth;

        List<LuaFiles> luaList = new List<LuaFiles>();
        string lastLuaFile = "";

        private List<LuaFiles> GetLuaFileList()
        {
            List<LuaFiles> l = new List<LuaFiles>();
            for (int x = 0; x < luaList.Count; x++)
                l.Add(new LuaFiles(luaList[x]));

            return l;
        }

        public LuaConsole()
        {
            InitializeComponent();
            Closing += (o, e) => SaveConfigSettings();
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
    }
}
