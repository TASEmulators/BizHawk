using System;
using System.Collections.Generic;
using System.Drawing;
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

		private List<LibraryFunction> FilteredList
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(FilterBox.Text))
				{
					return FunctionList
						.Where(f => (f.Library + "." + f.Name).Contains(FilterBox.Text))
						.ToList();
				}

				return FunctionList;
			}
		}

		public LuaFunctionsForm()
		{
			InitializeComponent();
			FunctionView.QueryItemText += FunctionView_QueryItemText;
			FunctionView.QueryItemBkColor += FunctionView_QueryItemBkColor;
		}

		private void LuaFunctionList_Load(object sender, EventArgs e)
		{
			FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs
				.OrderBy(x => x.Library)
				.ThenBy(x => x.Name)
				.ToList();
			UpdateList();
			FilterBox.Focus();

			ToWikiMarkupButton.Visible = VersionInfo.DeveloperBuild;
		}

		private void FunctionView_QueryItemBkColor(int index, int column, ref Color color)
		{
			
		}

		private void FunctionView_QueryItemText(int index, int column, out string text)
		{
			text = string.Empty;

			try
			{ 
				if (FilteredList.Any() && index < FilteredList.Count)
				{
					switch (column)
					{
						case 0:
							text = FilteredList[index].ReturnType;
							break;
						case 1:
							text = FilteredList[index].Library;
							break;
						case 2:
							text = FilteredList[index].Name;
							break;
						case 3:
							text = FilteredList[index].ParameterList;
							break;
						case 4:
							text = FilteredList[index].Description;
							break;
					}
				}
			}
			catch
			{
				/* Eat it*/
			}
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

		public class Sorting
		{
			private bool _desc;
			private int _column = 1;

			public int Column
			{
				get
				{
					return _column;
				}

				set
				{
					if (_column == value)
					{
						_desc ^= true;
					}

					_column = value;
				}
			}

			public bool Descending
			{
				get { return _desc; }
			}
		}

		private void FunctionView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.C && e.Control && !e.Alt && !e.Shift) // Copy
			{
				var indexes = FunctionView.SelectedIndices;

				if (indexes.Count > 0)
				{
					var sb = new StringBuilder();

					foreach (int index in indexes)
					{
						var libraryFunction = GlobalWin.Tools.LuaConsole.LuaImp.Docs[index];
						sb.Append(libraryFunction.Library).Append('.').Append(libraryFunction.Name).Append("()\n");
					}

					if (sb.Length > 0)
					{
						Clipboard.SetDataObject(sb.ToString());
					}
				}
			}
		}

		private void UpdateList()
		{
			FunctionView.ItemCount = FilteredList.Count;
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
