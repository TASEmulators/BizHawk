using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaFunctionsForm : Form
	{
		private readonly Sorting _columnSort = new Sorting();

		private List<LibraryFunction> FunctionList = new List<LibraryFunction>();

		private List<LibraryFunction> _filteredList = new List<LibraryFunction>();

		private void GenerateFilteredList()
		{
			if (!string.IsNullOrWhiteSpace(FilterBox.Text))
			{
				_filteredList = FunctionList
					.Where(f => $"{f.Library}.{f.Name}".ToLowerInvariant().Contains(FilterBox.Text.ToLowerInvariant()))
					.ToList();
			}
			else
			{
				_filteredList = FunctionList.ToList();
			}
		}

		public LuaFunctionsForm()
		{
			InitializeComponent();
			FunctionView.RetrieveVirtualItem += FunctionView_QueryItemText;
		}

		private void LuaFunctionList_Load(object sender, EventArgs e)
		{
			FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs
				.OrderBy(l => l.Library)
				.ThenBy(l => l.Name)
				.ToList();
			UpdateList();
			FilterBox.Focus();

			ToWikiMarkupButton.Visible = VersionInfo.DeveloperBuild;
		}

		private void FunctionView_QueryItemText(object sender, RetrieveVirtualItemEventArgs e)
		{
			var entry = _filteredList[e.ItemIndex];
			e.Item = new ListViewItem(entry.ReturnType);
			e.Item.SubItems.Add(entry.Library);
			e.Item.SubItems.Add(entry.Name);
			e.Item.SubItems.Add(entry.ParameterList);
			e.Item.SubItems.Add(entry.Description);
		}

		private void OrderColumn(int column)
		{
			_columnSort.Column = column;
			if (_columnSort.Descending)
			{
				switch (column)
				{
					case 0: // Return
						FunctionList = FunctionList.OrderByDescending(x => x.ReturnType).ToList();
						break;
					case 1: // Library
						FunctionList = FunctionList.OrderByDescending(x => x.Library).ToList();
						break;
					case 2: // Name
						FunctionList = FunctionList.OrderByDescending(x => x.Name).ToList();
						break;
					case 3: // Parameters
						FunctionList = FunctionList.OrderByDescending(x => x.ParameterList).ToList();
						break;
					case 4: // Description
						FunctionList = FunctionList.OrderByDescending(x => x.Description).ToList();
						break;
				}
			}
			else
			{
				switch (column)
				{
					case 0: // Return
						FunctionList = FunctionList.OrderBy(x => x.ReturnType).ToList();
						break;
					case 1: // Library
						FunctionList = FunctionList.OrderBy(x => x.Library).ToList();
						break;
					case 2: // Name
						FunctionList = FunctionList.OrderBy(x => x.Name).ToList();
						break;
					case 3: // Parameters
						FunctionList = FunctionList.OrderBy(x => x.ParameterList).ToList();
						break;
					case 4: // Description
						FunctionList = FunctionList.OrderBy(x => x.Description).ToList();
						break;
				}
			}

			UpdateList();
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void FunctionView_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			OrderColumn(e.Column);
		}

		private class Sorting
		{
			private int _column = 1;

			public int Column
			{
				get => _column;

				set
				{
					if (_column == value)
					{
						Descending ^= true;
					}

					_column = value;
				}
			}

			public bool Descending { get; private set; }
		}

		private void FunctionView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.C && e.Control && !e.Alt && !e.Shift) // Copy
			{
				var indexes = FunctionView.SelectedIndices;

				//TODO - duplicated code with FunctionView_Copy
				//also -- this list is more compact (the examples would fill space)
				//it isn't clear whether we should copy the examples here. So maybe this should stay distinct (and more compact?)

				if (indexes.Count > 0)
				{
					var sb = new StringBuilder();

					foreach (int index in indexes)
					{
						var libraryFunction = GlobalWin.Tools.LuaConsole.LuaImp.Docs[index];
						sb.Append(libraryFunction.Library).Append('.').Append(libraryFunction.Name).Append("()\n");
					}

					if (sb.Length > 0)
						Clipboard.SetDataObject(sb.ToString());
				}
			}
		}

		//FREVBHFYL?
		private void FunctionView_Copy(object sender, EventArgs e)
		{
			if (FunctionView.SelectedIndices.Count == 0)
			{
				return;
			}

			var itm = _filteredList[FunctionView.SelectedIndices[0]];
			var sb = new StringBuilder($"//{itm.Library}.{itm.Name}{itm.ParameterList}"); //comment style not an accident: the 'declaration' is not legal lua, so use of -- to comment it shouldn't suggest it. right?
			if (itm.Example != null)
			{
				sb.AppendLine();
				sb.Append(itm.Example);
			}

			if (sb.Length > 0)
			{
				Clipboard.SetText(sb.ToString());
			}
		}
		
		private void UpdateList()
		{
			GenerateFilteredList();
			FunctionView.VirtualListSize = _filteredList.Count;
		}

		private void FilterBox_KeyUp(object sender, KeyEventArgs e)
		{
			UpdateList();
		}

		private void ToWikiMarkupButton_Click(object sender, EventArgs e)
		{
			Clipboard.SetDataObject(GlobalWin.Tools.LuaConsole.LuaImp.Docs.ToTASVideosWikiMarkup());
		}
	}
}
