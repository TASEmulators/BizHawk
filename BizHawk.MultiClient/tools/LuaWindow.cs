using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LuaInterface;

namespace BizHawk.MultiClient.tools
{
    public partial class LuaWindow : Form
    {
        LuaImplementation LuaImp;
        public LuaWindow()
        {
            InitializeComponent();
             LuaImp = new LuaImplementation(this);
        }
        public LuaWindow get()
        {
            return this;
        }
        private void IDB_BROWSE_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "Open Lua Script";
            fdlg.InitialDirectory = @".\"; //Switch this to a better default directory
            fdlg.Filter = "Lua files (*.lua)|*.lua|All files (*.*)|*.*";
            fdlg.FilterIndex = 1;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog(this) == DialogResult.OK)
            {
                IDT_SCRIPTFILE.Text = fdlg.FileName;
            }
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

        }

    }
}
