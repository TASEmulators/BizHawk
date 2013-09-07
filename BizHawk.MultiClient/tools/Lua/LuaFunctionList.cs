using System;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using BizHawk.MultiClient.tools;

namespace BizHawk.MultiClient
{
	public partial class LuaFunctionList : Form
	{
		private readonly Sorting ColumnSort = new Sorting();
		
		public LuaFunctionList()
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
			foreach (LuaDocumentation.LibraryFunction l in Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList)
			{
				ListViewItem item = new ListViewItem {Text = l.ReturnType};
				item.SubItems.Add(l.library + ".");
				item.SubItems.Add(l.name);
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
						Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList = Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList.OrderByDescending(x => x.ReturnType).ToList();
						break;
					case 1: //Library
						Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList = Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList.OrderByDescending(x => x.library).ToList();
						break;
					case 2: //Name
						Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList = Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList.OrderByDescending(x => x.name).ToList();
						break;
					case 3: //Parameters
						Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList = Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList.OrderByDescending(x => x.ParameterList).ToList();
						break;
				}
			}
			else
			{
				switch (column)
				{
					case 0: //Return
						Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList = Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList.OrderBy(x => x.ReturnType).ToList();
						break;
					case 1: //Library
						Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList = Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList.OrderBy(x => x.library).ToList();
						break;
					case 2: //Name
						Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList = Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList.OrderBy(x => x.name).ToList();
						break;
					case 3: //Parameters
						Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList = Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList.OrderBy(x => x.ParameterList).ToList();
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
						var library_function = Global.MainForm.LuaConsole1.LuaImp.docs.FunctionList[index];
						sb.Append(library_function.library).Append('.').Append(library_function.name).Append("()\n");
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
