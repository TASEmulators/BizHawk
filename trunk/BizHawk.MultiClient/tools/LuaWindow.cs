using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
 

namespace BizHawk.MultiClient.tools
{
    
    public partial class LuaWindow : Form
    {        
        public LuaWindow()
        {
            InitializeComponent();
        }

        private void IDB_BROWSE_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "Open Lua Script";
            fdlg.InitialDirectory = @"c:\"; //Switch this to a better default directory
            fdlg.Filter = "Lua files (*.lua)|*.lua|All files (*.*)|*.*";
            fdlg.FilterIndex = 1;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog(this) == DialogResult.OK)
            {
                IDT_SCRIPTFILE.Text = fdlg.FileName;
            }
        }
    }
}
