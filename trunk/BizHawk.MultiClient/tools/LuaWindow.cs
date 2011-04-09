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

namespace BizHawk.MultiClient.tools
{
    public partial class LuaWindow : Form
    {
        //TODO: main form should run save config settings, however, multiple lua consoles should be able to be opened at once, think about the logic of this

        int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
        int defaultHeight;
        LuaImplementation LuaImp;

        public LuaWindow()
        {
            InitializeComponent();
             LuaImp = new LuaImplementation(this);
             Closing += (o, e) => SaveConfigSettings(); 
        }
        public LuaWindow get()
        {
            return this;
        }

        private FileInfo GetFileFromUser()
        {
            var ofd = new OpenFileDialog();
            if (IDT_SCRIPTFILE.Text.Length > 0)
                ofd.FileName = Path.GetFileNameWithoutExtension(IDT_SCRIPTFILE.Text);
            ofd.InitialDirectory = Global.Config.LastRomPath;
            ofd.Filter = "Watch Files (*.lua)|*.lua|All Files|*.*";
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

        private void OpenLuaScript()
        {
            var file = GetFileFromUser();
            if (file != null)
            {
                IDT_SCRIPTFILE.Text = file.FullName;
            }
        }

        private void IDB_BROWSE_Click(object sender, EventArgs e)
        {
            OpenLuaScript();
        }
        public void AddText(string s)
        {
            IDT_OUTPUT.Text += s;
        }

        private void IDB_RUN_Click(object sender, EventArgs e)
        {
            LuaImp.DoLuaFile(IDT_SCRIPTFILE.Text);
        }

        private void LuaWindow_Load(object sender, EventArgs e)
        {
            LoadConfigSettings();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenLuaScript();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.LuaConsoleSaveWindowPosition ^= true;
        }

        private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            saveWindowPositionToolStripMenuItem.Checked = Global.Config.LuaConsoleSaveWindowPosition;
        }

        private void LoadConfigSettings()
        {
            defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
            defaultHeight = Size.Height;

            if (Global.Config.LuaConsoleSaveWindowPosition && Global.Config.LuaConsoleWndx >= 0 && Global.Config.LuaConsoleWndy >= 0)
                Location = new Point(Global.Config.LuaConsoleWndx, Global.Config.LuaConsoleWndy);

            if (Global.Config.LuaConsoleWidth >= 0 && Global.Config.LuaConsoleHeight >= 0)
                Size = new System.Drawing.Size(Global.Config.LuaConsoleWidth, Global.Config.LuaConsoleHeight);
        }

        public void SaveConfigSettings()
        {
            Global.Config.LuaConsoleWndx = this.Location.X;
            Global.Config.LuaConsoleWndy = this.Location.Y;
            Global.Config.LuaConsoleWidth = this.Right - this.Left;
            Global.Config.LuaConsoleHeight = this.Bottom - this.Top;
        }

        private void restoreWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Size = new System.Drawing.Size(defaultWidth, defaultHeight);
        }

        private void IDB_EDIT_Click(object sender, EventArgs e)
        {
            
        }

        private void IDB_STOP_Click(object sender, EventArgs e)
        {

        }
    }
}
