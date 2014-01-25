using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaFunctionsForm : Form
	{
		private readonly Sorting _columnSort = new Sorting();
		
		public LuaFunctionsForm()
		{
			InitializeComponent();
		}

		private void LuaFunctionList_Load(object sender, EventArgs e)
		{
			PopulateListView();
		}

		private void PopulateListView()
		{
			FunctionView.Items.Clear();
			foreach (var libraryFunction in GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList)
			{
				var item = new ListViewItem { Text = libraryFunction.ReturnType };
				item.SubItems.Add(libraryFunction.Library + ".");
				item.SubItems.Add(libraryFunction.Name);
				item.SubItems.Add(libraryFunction.ParameterList);
				item.SubItems.Add(libraryFunction.Description);
				FunctionView.Items.Add(item);
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
						GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList.OrderByDescending(x => x.ReturnType).ToList();
						break;
					case 1: // Library
						GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList.OrderByDescending(x => x.Library).ToList();
						break;
					case 2: // Name
						GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList.OrderByDescending(x => x.Name).ToList();
						break;
					case 3: // Parameters
						GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList.OrderByDescending(x => x.ParameterList).ToList();
						break;
					case 4: // Description
						GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList.OrderByDescending(x => x.Description).ToList();
						break;
				}
			}
			else
			{
				switch (column)
				{
					case 0: // Return
						GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList.OrderBy(x => x.ReturnType).ToList();
						break;
					case 1: // Library
						GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList.OrderBy(x => x.Library).ToList();
						break;
					case 2: // Name
						GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList.OrderBy(x => x.Name).ToList();
						break;
					case 3: // Parameters
						GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList.OrderBy(x => x.ParameterList).ToList();
						break;
					case 4: // Description
						GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList.OrderBy(x => x.Description).ToList();
						break;
				}
			}

			PopulateListView();
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
						var libraryFunction = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList[index];
						sb.Append(libraryFunction.Library).Append('.').Append(libraryFunction.Name).Append("()\n");
					}

					if (sb.Length > 0)
					{
						Clipboard.SetDataObject(sb.ToString());
					}
				}
			}
		}
	}
}
