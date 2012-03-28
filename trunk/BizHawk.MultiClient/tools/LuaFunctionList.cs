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
			FunctionBox.Text = "";

			foreach (string l in Global.MainForm.LuaConsole1.LuaImp.LuaLibraryList)
			{
				FunctionBox.Text += l + "\n";
			}
		}

		private void OK_Click(object sender, EventArgs e)
		{
			this.Close();
		}
	}
}
