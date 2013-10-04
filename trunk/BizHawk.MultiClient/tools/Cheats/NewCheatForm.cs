using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class NewCheatForm : Form
	{
		public const string NAME = "NamesColumn";
		public const string ADDRESS = "AddressColumn";
		public const string VALUE = "ValueColumn";
		public const string COMPARE = "CompareColumn";
		public const string ON = "OnColumn";
		public const string DOMAIN = "DomainColumn";

		private readonly Dictionary<string, int> DefaultColumnWidths = new Dictionary<string, int>
		{
			{ NAME, 128 },
			{ ADDRESS, 60 },
			{ VALUE, 59 },
			{ COMPARE, 59 },
			{ ON, 28 },
			{ DOMAIN, 55 },
		};

		private int defaultWidth;
		private int defaultHeight;
		private string _sortedColumn = "";
		private bool _sortReverse = false;

		public NewCheatForm()
		{
			InitializeComponent();
			Closing += (o, e) => SaveConfigSettings();
			CheatListView.QueryItemText += CheatListView_QueryItemText;
			CheatListView.QueryItemBkColor += CheatListView_QueryItemBkColor;
			CheatListView.VirtualMode = true;

			_sortedColumn = "";
			_sortReverse = false;
			TopMost = Global.Config.CheatsAlwaysOnTop;
		}

		private void UpdateListView()
		{
			CheatListView.ItemCount = Global.CheatList2.Count;
			TotalLabel.Text = Global.CheatList2.CheatCount.ToString()
				+ (Global.CheatList2.CheatCount == 1 ? " cheat" : " cheats")
				+ Global.CheatList2.ActiveCheatCount.ToString() + " active";
		}

		public void LoadFileFromRecent(string path)
		{
			bool ask_result = true;
			if (Global.CheatList2.Changes)
			{
				ask_result = AskSave();
			}

			if (ask_result)
			{
				bool load_result = Global.CheatList2.Load(path, append: false);
				if (!load_result)
				{
					Global.Config.RecentWatches.HandleLoadError(path);
				}
				else
				{
					Global.Config.RecentWatches.Add(path);
					UpdateListView();
					UpdateMessageLabel();
				}
			}
		}

		private void UpdateMessageLabel(bool saved = false)
		{
			string message = String.Empty;
			if (!String.IsNullOrWhiteSpace(Global.CheatList2.CurrentFileName))
			{
				if (saved)
				{
					message = Path.GetFileName(Global.CheatList2.CurrentFileName) + " saved.";
				}
				else
				{
					message = Path.GetFileName(Global.CheatList2.CurrentFileName) + (Global.CheatList2.Changes ? " *" : String.Empty);
				}
			}

			MessageLabel.Text = message;
		}

		public bool AskSave()
		{
			if (Global.Config.SupressAskSave) //User has elected to not be nagged
			{
				return true;
			}

			if (Global.CheatList2.Changes)
			{
				Global.Sound.StopSound();
				DialogResult result = MessageBox.Show("Save Changes?", "Cheats", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
				Global.Sound.StartSound();
				if (result == DialogResult.Yes)
				{
					Global.CheatList2.Save();
				}
				else if (result == DialogResult.No)
				{
					return true;
				}
				else if (result == DialogResult.Cancel)
				{
					return false;
				}
			}

			return true;
		}

		private void LoadFile(FileInfo file, bool append)
		{
			if (file != null)
			{
				bool result = true;
				if (Global.CheatList2.Changes)
				{
					result = AskSave();
				}

				if (result)
				{
					Global.CheatList2.Load(file.FullName, append);
					UpdateListView();
					Global.Config.RecentCheats.Add(Global.CheatList2.CurrentFileName);
				}
			}
		}

		private void NewCheatForm_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
		}

		public void SaveConfigSettings()
		{
			/*TODO*/
		}

		private void LoadConfigSettings()
		{
			//Size and Positioning
			defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = Size.Height;

			if (Global.Config.CheatsSaveWindowPosition && Global.Config.CheatsWndx >= 0 && Global.Config.CheatsWndy >= 0)
			{
				Location = new Point(Global.Config.CheatsWndx, Global.Config.CheatsWndy);
			}

			if (Global.Config.CheatsWidth >= 0 && Global.Config.CheatsHeight >= 0)
			{
				Size = new Size(Global.Config.CheatsWidth, Global.Config.CheatsHeight);
			}

			LoadColumnInfo();
		}

		private void LoadColumnInfo()
		{
			CheatListView.Columns.Clear();
			ToolHelpers.AddColumn(CheatListView, NAME, true, GetColumnWidth(NAME));
			ToolHelpers.AddColumn(CheatListView, ADDRESS, true, GetColumnWidth(ADDRESS));
			ToolHelpers.AddColumn(CheatListView, VALUE, true, GetColumnWidth(VALUE));
			ToolHelpers.AddColumn(CheatListView, COMPARE, true, GetColumnWidth(COMPARE));
			ToolHelpers.AddColumn(CheatListView, ON, true, GetColumnWidth(ON));
			ToolHelpers.AddColumn(CheatListView, DOMAIN, true, GetColumnWidth(DOMAIN));
		}

		private int GetColumnWidth(string columnName)
		{
			var width = Global.Config.CheatsColumnWidths[columnName];
			if (width == -1)
			{
				width = DefaultColumnWidths[columnName];
			}

			return width;
		}

		private void CheatListView_QueryItemText(int index, int column, out string text)
		{
			text = "";
			if (index >= Global.CheatList2.Count || Global.CheatList2[index].IsSeparator)
			{
				return;
			}

			string columnName = CheatListView.Columns[column].Name;

			switch (columnName)
			{
				case NAME:
					text = Global.CheatList2[index].Name;
					break;
				case ADDRESS:
					text = Global.CheatList2[index].AddressStr;
					break;
				case VALUE:
					text = Global.CheatList2[index].ValueStr;
					break;
				case COMPARE:
					text = Global.CheatList2[index].CompareStr;
					break;
				case ON:
					text = Global.CheatList2[index].Enabled ? "*" : "";
					break;
				case DOMAIN:
					text = Global.CheatList2[index].Domain.Name;
					break;
			}
		}

		private void CheatListView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (index < Global.CheatList2.Count)
			{
				if (Global.CheatList2[index].IsSeparator)
				{
					color = BackColor;
				}
				else if (Global.CheatList2[index].Enabled)
				{
					color = Color.LightCyan;
				}
			}
		}

		private List<int> SelectedIndices
		{
			get
			{
				var selected = new List<int>();
				ListView.SelectedIndexCollection indices = CheatListView.SelectedIndices;
				foreach (int index in indices)
				{
					selected.Add(index);
				}
				return selected;
			}
		}
		private List<NewCheat> SelectedItems
		{
			get
			{
				var selected = new List<NewCheat>();
				if (SelectedIndices.Any())
				{
					foreach (int index in SelectedIndices)
					{
						if (!Global.CheatList2[index].IsSeparator)
						{
							selected.Add(Global.CheatList2[index]);
						}
					}
				}
				return selected;
			}
		}

		private List<NewCheat> SelectedCheats
		{
			get
			{
				return SelectedItems.Where(x => !x.IsSeparator).ToList();
			}
		}

		private void MoveUp()
		{
			var indices = CheatListView.SelectedIndices;
			if (indices.Count == 0 || indices[0] == 0)
			{
				return;
			}

			foreach (int index in indices)
			{
				var cheat = Global.CheatList2[index];
				Global.CheatList2.Remove(Global.CheatList2[index]);
				Global.CheatList2.Insert(index - 1, cheat);
			}

			UpdateMessageLabel();

			var newindices = new List<int>();
			for (int i = 0; i < indices.Count; i++)
			{
				newindices.Add(indices[i] - 1);
			}

			CheatListView.SelectedIndices.Clear();
			foreach (int newi in newindices)
			{
				CheatListView.SelectItem(newi, true);
			}

			UpdateListView();
		}

		private void MoveDown()
		{
			var indices = CheatListView.SelectedIndices;
			if (indices.Count == 0)
			{
				return;
			}

			foreach (int index in indices)
			{
				var cheat = Global.CheatList2[index];

				if (index < Global.CheatList2.Count - 1)
				{
					Global.CheatList2.Remove(Global.CheatList2[index]);
					Global.CheatList2.Insert(index + 1, cheat);
				}
			}

			UpdateMessageLabel();

			var newindices = new List<int>();
			for (int i = 0; i < indices.Count; i++)
			{
				newindices.Add(indices[i] + 1);
			}

			CheatListView.SelectedIndices.Clear();
			foreach (int newi in newindices)
			{
				CheatListView.SelectItem(newi, true);
			}

			UpdateListView();
		}

		private void Remove()
		{
			if (SelectedIndices.Any())
			{
				foreach (int index in SelectedIndices)
				{
					Global.CheatList2.Remove(Global.CheatList2[SelectedIndices[0]]); //SelectedIndices[0] used since each iteration will make this the correct list index
				}
				CheatListView.SelectedIndices.Clear();
			}

			UpdateListView();
		}

		#region Events

		#region File

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveMenuItem.Enabled = Global.CheatList2.Changes;
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(Global.Config.RecentCheats.GenerateRecentMenu(LoadFileFromRecent));
		}

		private void OpenMenuItem_Click(object sender, EventArgs e)
		{
			bool append = sender == AppendMenuItem;
			LoadFile(NewCheatList.GetFileFromUser(Global.CheatList2.CurrentFileName), append);
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		#region Cheats

		private void CheatsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RemoveCheatMenuItem.Enabled =
				MoveUpMenuItem.Enabled =
				MoveDownMenuItem.Enabled =
				SelectedIndices.Any();

			DisableAllCheatsMenuItem.Enabled = Global.CheatList2.ActiveCheatCount > 0;
		}

		private void RemoveCheatMenuItem_Click(object sender, EventArgs e)
		{
			Remove();
		}

		private void InsertSeparatorMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedIndices.Any())
			{
				Global.CheatList2.Insert(SelectedIndices.Max(), NewCheat.Separator);
			}
			else
			{
				Global.CheatList2.Add(NewCheat.Separator);
			}

			UpdateListView();
		}

		private void MoveUpMenuItem_Click(object sender, EventArgs e)
		{
			MoveUp();
		}

		private void MoveDownMenuItem_Click(object sender, EventArgs e)
		{
			MoveDown();
		}

		private void SelectAllMenuItem_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < Global.CheatList2.Count; i++)
			{
				CheatListView.SelectItem(i, true);
			}
		}

		private void DisableAllCheatsMenuItem_Click(object sender, EventArgs e)
		{
			Global.CheatList2.DisableAll();
		}

		#endregion

		#region Options

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AlwaysOnTopMenuItem.Checked = Global.Config.CheatsAlwaysOnTop;
			SaveWindowPositionMenuItem.Checked = Global.Config.CheatsSaveWindowPosition;
		}

		private void SaveWindowPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.CheatsSaveWindowPosition ^= true;
		}

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.CheatsAlwaysOnTop ^= true;
		}

		#endregion

		#region Columns

		private void ColumnsSubMenu_DropDownOpened(object sender, EventArgs e)
		{

		}

		#endregion

		#endregion
	}
}
