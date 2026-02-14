using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaRegisteredFunctionsList : Form
	{
		private List<LuaFile> _scriptList;

		private IEnumerable<NamedLuaFunction> AllFunctions
		{
			get => _scriptList.SelectMany(lf => lf.Functions);
		}

		public LuaRegisteredFunctionsList(List<LuaFile> scripts)
		{
			_scriptList = scripts;
			InitializeComponent();
			Icon = Properties.Resources.TextDocIcon;
		}

		public Point StartLocation { get; set; } = new Point(0, 0);

		public void UpdateValues(List<LuaFile> scripts)
		{
			_scriptList = scripts;
			PopulateListView();
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

			var functions = AllFunctions
				.OrderBy(f => f.Event)
				.ThenBy(f => f.Name);
			foreach (var nlf in functions)
			{
				var item = new ListViewItem { Text = nlf.Event };
				item.SubItems.Add(nlf.Name);
				item.SubItems.Add(nlf.GuidStr);
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
					Guid.TryParseExact(guid, format: "D", out var parsed);
					AllFunctions.First(nlf => nlf.Guid == parsed).Call();
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
					Guid.TryParseExact(guid, format: "D", out var parsed);
					foreach (LuaFile file in _scriptList)
					{
						var nlf = AllFunctions.FirstOrDefault(nlf => nlf.Guid == parsed);
						if (nlf is not null)
							file.Functions.Remove(nlf);
					}
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
			foreach (LuaFile file in _scriptList)
				file.Functions.Clear();
			PopulateListView();
		}

		private void DoButtonsStatus()
		{
			var indexes = FunctionView.SelectedIndices;
			CallButton.Enabled = indexes.Count > 0;
			RemoveButton.Enabled = indexes.Count > 0;
			RemoveAllBtn.Enabled = AllFunctions.Any();
		}

		private void FunctionView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.IsPressed(Keys.Delete))
			{
				RemoveFunctionButton();
			}
			else if (e.IsPressed(Keys.Space))
			{
				CallFunction();
			}
			else if (e.IsPressed(Keys.Enter))
			{
				CallFunction();
			}
		}
	}
}
