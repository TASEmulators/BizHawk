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
	public partial class LuaRegisteredFunctionsList : Form
	{
		public LuaRegisteredFunctionsList()
		{
			InitializeComponent();
		}

		private void LuaRegisteredFunctionsList_Load(object sender, EventArgs e)
		{
			PopulateListView();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void PopulateListView()
		{
			FunctionView.Items.Clear();
			
			List<NamedLuaFunction> nlfs = Global.MainForm.LuaConsole1.LuaImp.RegisteredFunctions.OrderBy(x => x.Event).ThenBy(x => x.Name).ToList();
			foreach (NamedLuaFunction nlf in nlfs)
			{
				ListViewItem item = new ListViewItem { Text = nlf.Event };
				item.SubItems.Add(nlf.Name);
				item.SubItems.Add(nlf.GUID.ToString());
				FunctionView.Items.Add(item);
			}
		}

		private void CallButton_Click(object sender, EventArgs e)
		{
			CallFunction();
		}

		private void RemoveButton_Click(object sender, EventArgs e)
		{
			RemoveFunctionButton();
		}

		private void CallFunction()
		{
			ListView.SelectedIndexCollection indexes = FunctionView.SelectedIndices;
			if (indexes.Count > 0)
			{
				Global.MainForm.LuaConsole1.LuaImp.RegisteredFunctions[indexes[0]].Call();
			}
		}

		private void RemoveFunctionButton()
		{
			ListView.SelectedIndexCollection indexes = FunctionView.SelectedIndices;
			if (indexes.Count > 0)
			{
				NamedLuaFunction nlf = Global.MainForm.LuaConsole1.LuaImp.RegisteredFunctions[indexes[0]];
				Global.MainForm.LuaConsole1.LuaImp.RegisteredFunctions.Remove(nlf);
				PopulateListView();
			}
		}

		private void FunctionView_SelectedIndexChanged(object sender, EventArgs e)
		{
			ListView.SelectedIndexCollection indexes = FunctionView.SelectedIndices;
			CallButton.Enabled = indexes.Count > 0;
			RemoveButton.Enabled = indexes.Count > 0;
		}
	}
}
