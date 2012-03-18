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
	public partial class LuaFunctionList : Form
	{
		public LuaFunctionList()
		{
			InitializeComponent();
		}

		private void LuaFunctionList_Load(object sender, EventArgs e)
		{
#if WINDOWS
			FunctionBox.Text = Global.MainForm.LuaConsole1.LuaImp.LuaLibraryList;
#endif
		}

		private void OK_Click(object sender, EventArgs e)
		{
			this.Close();
		}
	}
}
