using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaFunctionsForm : Form
	{
		private readonly LuaDocumentation _docs;
		private readonly Sorting _columnSort = new Sorting();

		private List<LibraryFunction> _functionList = new List<LibraryFunction>();
		private List<LibraryFunction> _filteredList = new List<LibraryFunction>();

		public LuaFunctionsForm(LuaDocumentation docs)
		{
			_docs = docs;
			InitializeComponent();
			Icon = Properties.Resources.TextDocIcon;
			FunctionView.RetrieveVirtualItem += FunctionView_QueryItemText;
		}

		private void GenerateFilteredList()
		{
			if (!string.IsNullOrWhiteSpace(FilterBox.Text))
			{
				_filteredList = _functionList
					.Where(f => $"{f.Library}.{f.Name}".Contains(FilterBox.Text, StringComparison.OrdinalIgnoreCase)
						|| f.Description.Contains(FilterBox.Text, StringComparison.OrdinalIgnoreCase))
					.ToList();
			}
			else
			{
				_filteredList = _functionList.ToList();
			}
		}

		private void LuaFunctionList_Load(object sender, EventArgs e)
		{
			_functionList = _docs
				.OrderBy(l => l.Library)
				.ThenBy(l => l.Name)
				.ToList();
			UpdateList();
			FilterBox.Select();

			ToWikiMarkupButton.Visible = VersionInfo.DeveloperBuild;
		}

		private void FunctionView_QueryItemText(object sender, RetrieveVirtualItemEventArgs e)
		{
			var entry = _filteredList[e.ItemIndex];
			e.Item = new ListViewItem(entry.ReturnType);
			e.Item.SubItems.Add(entry.Library);

			var deprecated = entry.IsDeprecated ? "[Deprecated] " : "";
			e.Item.SubItems.Add(deprecated + entry.Name);
			e.Item.SubItems.Add(entry.ParameterList);
			e.Item.SubItems.Add(entry.Description);
		}

		private void OrderColumn(int column)
		{
			_columnSort.Column = column;

			_functionList = column switch
			{
				0 => _functionList.OrderBy(x => x.ReturnType, _columnSort.Descending).ToList(),
				1 => _functionList.OrderBy(x => x.Library, _columnSort.Descending).ToList(),
				2 => _functionList.OrderBy(x => x.Name, _columnSort.Descending).ToList(),
				3 => _functionList.OrderBy(x => x.ParameterList, _columnSort.Descending).ToList(),
				4 => _functionList.OrderBy(x => x.Description, _columnSort.Descending).ToList(),
				_ => _functionList
			};

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
					if (_column == value) Descending = !Descending;
					_column = value;
				}
			}

			public bool Descending { get; private set; }
		}

		private void FunctionView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.IsCtrl(Keys.C))
			{
				FunctionView_Copy(null, null);
			}
		}

		private void FunctionView_Copy(object sender, EventArgs e)
		{
			if (FunctionView.SelectedIndices.Count == 0)
			{
				return;
			}

			var sb = new StringBuilder();
			foreach (int index in FunctionView.SelectedIndices)
			{
				var itm = _filteredList[index];
				sb.Append($"//{itm.Library}.{itm.Name}{itm.ParameterList}"); // comment style not an accident: the 'declaration' is not legal lua, so use of -- to comment it shouldn't suggest it. right?
				if (itm.Example != null)
				{
					sb.AppendLine();
					sb.AppendLine(itm.Example);
				}
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
			FunctionView.Refresh();
		}

		private void FilterBox_KeyUp(object sender, KeyEventArgs e)
		{
			UpdateList();
		}

		private void ToWikiMarkupButton_Click(object sender, EventArgs e)
		{
			Clipboard.SetDataObject(_docs.ToTASVideosWikiMarkup());
		}
	}
}
