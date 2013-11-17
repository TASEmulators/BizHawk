using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaRegisteredFunctionsList : Form
	{
		public Point StartLocation = new Point(0, 0);
		public LuaRegisteredFunctionsList()
		{
			InitializeComponent();
		}

		private void LuaRegisteredFunctionsList_Load(object sender, EventArgs e)
		{
			if (StartLocation.X > 0 && StartLocation.Y > 0)
			{
				Location = StartLocation;
			}
			PopulateListView();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void PopulateListView()
		{
			FunctionView.Items.Clear();
			
			List<NamedLuaFunction> nlfs = GlobalWin.Tools.LuaConsole.LuaImp.RegisteredFunctions.OrderBy(x => x.Event).ThenBy(x => x.Name).ToList();
			foreach (NamedLuaFunction nlf in nlfs)
			{
				ListViewItem item = new ListViewItem { Text = nlf.Event };
				item.SubItems.Add(nlf.Name);
				item.SubItems.Add(nlf.GUID.ToString());
				FunctionView.Items.Add(item);
			}

			DoButtonsStatus();
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
			ListView.SelectedIndexCollection indices = FunctionView.SelectedIndices;
			if (indices.Count > 0)
			{
				foreach (int index in indices)
				{
					GlobalWin.Tools.LuaConsole.LuaImp.RegisteredFunctions[index].Call();
				}
			}
		}

		private void RemoveFunctionButton()
		{
			ListView.SelectedIndexCollection indices = FunctionView.SelectedIndices;
			if (indices.Count > 0)
			{
				foreach (int index in indices)
				{
					NamedLuaFunction nlf = GlobalWin.Tools.LuaConsole.LuaImp.RegisteredFunctions[index];
					GlobalWin.Tools.LuaConsole.LuaImp.RegisteredFunctions.RemoveFunction(nlf);
				}
				PopulateListView();
			}
		}

		private void FunctionView_SelectedIndexChanged(object sender, EventArgs e)
		{
			DoButtonsStatus();
		}

		private void FunctionView_DoubleClick(object sender, EventArgs e)
		{
			CallFunction();
		}

		private void RemoveAllBtn_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.LuaConsole.LuaImp.RegisteredFunctions.ClearAll();
			PopulateListView();
		}

		private void DoButtonsStatus()
		{
			ListView.SelectedIndexCollection indexes = FunctionView.SelectedIndices;
			CallButton.Enabled = indexes.Count > 0;
			RemoveButton.Enabled = indexes.Count > 0;
			RemoveAllBtn.Enabled = GlobalWin.Tools.LuaConsole.LuaImp.RegisteredFunctions.Any();
		}

		private void FunctionView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete && !e.Control && !e.Alt && !e.Shift) //Delete
			{
				RemoveFunctionButton();
			}
			else if (e.KeyCode == Keys.Space && !e.Control && !e.Alt && !e.Shift) //Space
			{
				CallFunction();
			}
			else if (e.KeyCode == Keys.Enter && !e.Control && !e.Alt && !e.Shift) //Enter
			{
				CallFunction();
			}
		}
	}
}
