using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaRegisteredFunctionsList : Form
	{
		public Point StartLocation { get; set; } = new Point(0, 0);
		public LuaRegisteredFunctionsList()
		{
			InitializeComponent();
		}

		public void NewUpdate(ToolFormUpdateType type) { }

		public void UpdateValues()
		{
			if (GlobalWin.Tools.LuaConsole.LuaImp.GetRegisteredFunctions().Any())
			{
				PopulateListView();
			}
			else
			{
				Close();
			}
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
			
			var nlfs = GlobalWin.Tools.LuaConsole.LuaImp.GetRegisteredFunctions().OrderBy(x => x.Event).ThenBy(x => x.Name);
			foreach (var nlf in nlfs)
			{
				var item = new ListViewItem { Text = nlf.Event };
				item.SubItems.Add(nlf.Name);
				item.SubItems.Add(nlf.Guid.ToString());
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
			var indices = FunctionView.SelectedIndices;
			if (indices.Count > 0)
			{
				foreach (int index in indices)
				{
					var guid = FunctionView.Items[index].SubItems[2].Text;
					GlobalWin.Tools.LuaConsole.LuaImp.GetRegisteredFunctions()[guid].Call();
				}
			}
		}

		private void RemoveFunctionButton()
		{
			var indices = FunctionView.SelectedIndices;
			if (indices.Count > 0)
			{
				foreach (int index in indices)
				{
					var guid = FunctionView.Items[index].SubItems[2].Text;
					var nlf = GlobalWin.Tools.LuaConsole.LuaImp.GetRegisteredFunctions()[guid];
					GlobalWin.Tools.LuaConsole.LuaImp.GetRegisteredFunctions().Remove(nlf);
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
			GlobalWin.Tools.LuaConsole.LuaImp.GetRegisteredFunctions().ClearAll();
			PopulateListView();
		}

		private void DoButtonsStatus()
		{
			var indexes = FunctionView.SelectedIndices;
			CallButton.Enabled = indexes.Count > 0;
			RemoveButton.Enabled = indexes.Count > 0;
			RemoveAllBtn.Enabled = GlobalWin.Tools.LuaConsole.LuaImp.GetRegisteredFunctions().Any();
		}

		private void FunctionView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete && !e.Control && !e.Alt && !e.Shift) // Delete
			{
				RemoveFunctionButton();
			}
			else if (e.KeyCode == Keys.Space && !e.Control && !e.Alt && !e.Shift) // Space
			{
				CallFunction();
			}
			else if (e.KeyCode == Keys.Enter && !e.Control && !e.Alt && !e.Shift) // Enter
			{
				CallFunction();
			}
		}
	}
}
