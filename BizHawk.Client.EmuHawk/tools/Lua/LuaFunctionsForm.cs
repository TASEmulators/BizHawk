using System;
using System.Linq;
using System.Windows.Forms;
using System.Text;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaFunctionsForm : Form
	{
		private readonly Sorting ColumnSort = new Sorting();
		
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
			foreach (LuaDocumentation.LibraryFunction l in GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList)
			{
				ListViewItem item = new ListViewItem {Text = l.ReturnType};
				item.SubItems.Add(l.Library + ".");
				item.SubItems.Add(l.Name);
				item.SubItems.Add(l.ParameterList);
				FunctionView.Items.Add(item);
			}
		}

		private void OrderColumn(int column)
		{
			ColumnSort.Column = column;
			if (ColumnSort.Descending)
			{
				switch (column)
				{
					case 0: //Return
						GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList.OrderByDescending(x => x.ReturnType).ToList();
						break;
					case 1: //Library
						GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList.OrderByDescending(x => x.Library).ToList();
						break;
					case 2: //Name
						GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList.OrderByDescending(x => x.Name).ToList();
						break;
					case 3: //Parameters
						GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList.OrderByDescending(x => x.ParameterList).ToList();
						break;
				}
			}
			else
			{
				switch (column)
				{
					case 0: //Return
						GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList.OrderBy(x => x.ReturnType).ToList();
						break;
					case 1: //Library
						GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList.OrderBy(x => x.Library).ToList();
						break;
					case 2: //Name
						GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList.OrderBy(x => x.Name).ToList();
						break;
					case 3: //Parameters
						GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList.OrderBy(x => x.ParameterList).ToList();
						break;
				}
			}
			PopulateListView();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void FunctionView_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			OrderColumn(e.Column);
		}

		public class Sorting
		{
			private bool desc;
			private int column = 1;

			public int Column
			{
				get
				{
					return column;
				}
				set
				{
					if (column == value)
					{
						desc ^= true;
					}
					column = value;
				}
			}

			public bool Descending
			{
				get
				{
					return desc;
				}
			}
		}

		private void FunctionView_SelectedIndexChanged(object sender, EventArgs e)
		{

		}

		private void FunctionView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.C && e.Control && !e.Alt && !e.Shift) //Copy
			{
				ListView.SelectedIndexCollection indexes = FunctionView.SelectedIndices;

				if (indexes.Count > 0)
				{
					StringBuilder sb = new StringBuilder();

					foreach (int index in indexes)
					{
						var library_function = GlobalWin.Tools.LuaConsole.LuaImp.Docs.FunctionList[index];
						sb.Append(library_function.Library).Append('.').Append(library_function.Name).Append("()\n");
					}

					if (sb.Length > 0)
					{
						Clipboard.SetDataObject((sb.ToString()));
					}
				}
			}
		}
	}
}
