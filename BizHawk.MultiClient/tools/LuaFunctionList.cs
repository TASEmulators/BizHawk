using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.MultiClient.tools;

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
			FunctionView.Items.Clear();
			foreach (LuaDocumentation.LibraryFunction l in Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList)
			{
				ListViewItem item = new ListViewItem();
				item.Text = l.ReturnType;
				item.SubItems.Add(l.library + ".");
				item.SubItems.Add(l.name);
				item.SubItems.Add(l.ParameterList);
				FunctionView.Items.Add(item);
			}
		}

		private void OK_Click(object sender, EventArgs e)
		{
			this.Close();
		}
	}
}
