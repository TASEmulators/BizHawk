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

using BizHawk.Emulation.Consoles.Nintendo.SNES;
using BizHawk.Emulation.Consoles.Nintendo;
using BizHawk.Emulation.Consoles.Sega;

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
		public const string SIZE = "SizeColumn";
		public const string ENDIAN = "EndianColumn";
		public const string TYPE = "DisplayTypeColumn";

		private readonly Dictionary<string, int> DefaultColumnWidths = new Dictionary<string, int>
		{
			{ NAME, 128 },
			{ ADDRESS, 60 },
			{ VALUE, 59 },
			{ COMPARE, 59 },
			{ ON, 28 },
			{ DOMAIN, 55 },
			{ SIZE, 55 },
			{ ENDIAN, 55 },
			{ TYPE, 55 },
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
				+ (Global.CheatList2.CheatCount == 1 ? " cheat " : " cheats ")
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
			
			if (saved)
			{
				message = Path.GetFileName(Global.CheatList2.CurrentFileName) + " saved.";
			}
			else
			{
				message = Path.GetFileName(Global.CheatList2.CurrentFileName) + (Global.CheatList2.Changes ? " *" : String.Empty);
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
					UpdateMessageLabel();
					Global.Config.RecentCheats.Add(Global.CheatList2.CurrentFileName);
				}
			}
		}

		private void NewCheatForm_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();

			if ((Global.Emulator is NES) || (Global.Emulator is Genesis) || (Global.Emulator.SystemId == "GB") || (Global.Game.System == "GG") || (Global.Emulator is LibsnesCore))

				GameGenieToolbarSeparator.Visible =
					LoadGameGenieToolbarItem.Visible =
					((Global.Emulator is NES)
					|| (Global.Emulator is Genesis)
					|| (Global.Emulator.SystemId == "GB")
					|| (Global.Game.System == "GG")
					|| (Global.Emulator is LibsnesCore));

			CheatEditor.SetAddEvent(AddCheat);
			CheatEditor.SetEditEvent(EditCheat);
		}

		private void AddCheat()
		{
			Global.CheatList2.Add(CheatEditor.Cheat);
			UpdateListView();
			UpdateMessageLabel();
		}

		private void EditCheat()
		{
			MessageBox.Show("Edit clicked");
		}

		public void SaveConfigSettings()
		{
			SaveColumnInfo();
			Global.Config.CheatsWndx = Location.X;
			Global.Config.CheatsWndy = Location.Y;
			Global.Config.CheatsWidth = Right - Left;
			Global.Config.CheatsHeight = Bottom - Top;
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
			ToolHelpers.AddColumn(CheatListView, NAME, Global.Config.CheatsColumnShow[NAME], GetColumnWidth(NAME));
			ToolHelpers.AddColumn(CheatListView, ADDRESS, Global.Config.CheatsColumnShow[ADDRESS], GetColumnWidth(ADDRESS));
			ToolHelpers.AddColumn(CheatListView, VALUE, Global.Config.CheatsColumnShow[VALUE], GetColumnWidth(VALUE));
			ToolHelpers.AddColumn(CheatListView, COMPARE, Global.Config.CheatsColumnShow[COMPARE], GetColumnWidth(COMPARE));
			ToolHelpers.AddColumn(CheatListView, ON, Global.Config.CheatsColumnShow[ON], GetColumnWidth(ON));
			ToolHelpers.AddColumn(CheatListView, DOMAIN, Global.Config.CheatsColumnShow[DOMAIN], GetColumnWidth(DOMAIN));
			ToolHelpers.AddColumn(CheatListView, SIZE, Global.Config.CheatsColumnShow[SIZE], GetColumnWidth(SIZE));
			ToolHelpers.AddColumn(CheatListView, ENDIAN, Global.Config.CheatsColumnShow[ENDIAN], GetColumnWidth(ENDIAN));
			ToolHelpers.AddColumn(CheatListView, TYPE, Global.Config.CheatsColumnShow[TYPE], GetColumnWidth(TYPE));
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
				case SIZE:
					text = Global.CheatList2[index].Size.ToString();
					break;
				case ENDIAN:
					text = Global.CheatList2[index].BigEndian.Value ? "Big" : "Little";
					break;
				case TYPE:
					text = Watch.DisplayTypeToString(Global.CheatList2[index].Type);
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

		private void Toggle()
		{
			SelectedCheats.ForEach(x => x.Toggle());
			Global.CheatList2.FlagChanges();
			UpdateListView();
		}

		private void SaveColumnInfo()
		{
			if (CheatListView.Columns[NAME] != null)
			{
				Global.Config.CheatsColumnIndices[NAME] = CheatListView.Columns[NAME].DisplayIndex;
				Global.Config.CheatsColumnWidths[NAME] = CheatListView.Columns[NAME].Width;
			}

			if (CheatListView.Columns[ADDRESS] != null)
			{
				Global.Config.CheatsColumnIndices[ADDRESS] = CheatListView.Columns[ADDRESS].DisplayIndex;
				Global.Config.CheatsColumnWidths[ADDRESS] = CheatListView.Columns[ADDRESS].Width;
			}

			if (CheatListView.Columns[VALUE] != null)
			{
				Global.Config.CheatsColumnIndices[VALUE] = CheatListView.Columns[VALUE].DisplayIndex;
				Global.Config.CheatsColumnWidths[VALUE] = CheatListView.Columns[VALUE].Width;
			}

			if (CheatListView.Columns[COMPARE] != null)
			{
				Global.Config.CheatsColumnIndices[COMPARE] = CheatListView.Columns[COMPARE].DisplayIndex;
				Global.Config.CheatsColumnWidths[COMPARE] = CheatListView.Columns[COMPARE].Width;
			}

			if (CheatListView.Columns[ON] != null)
			{
				Global.Config.CheatsColumnIndices[ON] = CheatListView.Columns[ON].DisplayIndex;
				Global.Config.CheatsColumnWidths[ON] = CheatListView.Columns[ON].Width;
			}

			if (CheatListView.Columns[DOMAIN] != null)
			{
				Global.Config.CheatsColumnIndices[DOMAIN] = CheatListView.Columns[DOMAIN].DisplayIndex;
				Global.Config.CheatsColumnWidths[DOMAIN] = CheatListView.Columns[DOMAIN].Width;
			}

			if (CheatListView.Columns[SIZE] != null)
			{
				Global.Config.CheatsColumnIndices[SIZE] = CheatListView.Columns[SIZE].DisplayIndex;
				Global.Config.CheatsColumnWidths[SIZE] = CheatListView.Columns[SIZE].Width;
			}

			if (CheatListView.Columns[ENDIAN] != null)
			{
				Global.Config.CheatsColumnIndices[ENDIAN] = CheatListView.Columns[ENDIAN].DisplayIndex;
				Global.Config.CheatsColumnWidths[ENDIAN] = CheatListView.Columns[ENDIAN].Width;
			}

			if (CheatListView.Columns[TYPE] != null)
			{
				Global.Config.CheatsColumnIndices[TYPE] = CheatListView.Columns[TYPE].DisplayIndex;
				Global.Config.CheatsColumnWidths[TYPE] = CheatListView.Columns[TYPE].Width;
			}
		}

		private void DoColumnToggle(string column)
		{
			Global.Config.CheatsColumnShow[column] ^= true;
			SaveColumnInfo();
			LoadColumnInfo();
		}

		private void DoSelectedIndexChange()
		{
			if (SelectedIndices.Any())
			{
				CheatEditor.SetCheat(Global.CheatList2[SelectedIndices[0]]);
			}
			else
			{
				CheatEditor.ClearForm();
			}
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

		private void NewMenuItem_Click(object sender, EventArgs e)
		{
			bool result = true;
			if (Global.CheatList2.Changes)
			{
				result = AskSave();
			}

			if (result)
			{
				Global.CheatList2.NewList();
				UpdateListView();
				UpdateMessageLabel();
			}
		}

		private void OpenMenuItem_Click(object sender, EventArgs e)
		{
			bool append = sender == AppendMenuItem;
			LoadFile(NewCheatList.GetFileFromUser(Global.CheatList2.CurrentFileName), append);
		}

		private void SaveMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.CheatList2.Changes)
			{
				if (Global.CheatList2.Save())
				{
					UpdateMessageLabel(saved: true);
				}
			}
			else
			{
				SaveAsMenuItem_Click(sender, e);
			}
		}

		private void SaveAsMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.CheatList2.SaveAs())
			{
				UpdateMessageLabel(saved: true);
			}
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
				DuplicateMenuItem.Enabled =
				MoveUpMenuItem.Enabled =
				MoveDownMenuItem.Enabled =
				ToggleMenuItem.Enabled =
				SelectedIndices.Any();

			DisableAllCheatsMenuItem.Enabled = Global.CheatList2.ActiveCheatCount > 0;

			GameGenieSeparator.Visible =
				OpenGameGenieEncoderDecoderMenuItem.Visible = 
				((Global.Emulator is NES) 
					|| (Global.Emulator is Genesis)
					|| (Global.Emulator.SystemId == "GB")
					|| (Global.Game.System == "GG")
					|| (Global.Emulator is LibsnesCore));
		}

		private void RemoveCheatMenuItem_Click(object sender, EventArgs e)
		{
			Remove();
		}

		private void DuplicateMenuItem_Click(object sender, EventArgs e)
		{
			if (CheatListView.SelectedIndices.Count > 0)
			{
				foreach (int index in CheatListView.SelectedIndices)
				{
					Global.CheatList2.Add(new NewCheat(Global.CheatList2[index]));
				}
			}

			UpdateListView();
			UpdateMessageLabel();
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
			UpdateMessageLabel();
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

		private void ToggleMenuItem_Click(object sender, EventArgs e)
		{
			Toggle();
		}

		private void DisableAllCheatsMenuItem_Click(object sender, EventArgs e)
		{
			Global.CheatList2.DisableAll();
		}

		private void OpenGameGenieEncoderDecoderMenuItem_Click(object sender, EventArgs e)
		{
			Global.MainForm.LoadGameGenieEC();
		}

		#endregion

		#region Options

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DisableCheatsOnLoadMenuItem.Checked = Global.Config.DisableCheatsOnLoad;
			AutoloadMenuItem.Checked = Global.Config.RecentCheats.AutoLoad;
			SaveWindowPositionMenuItem.Checked = Global.Config.CheatsSaveWindowPosition;
			AlwaysOnTopMenuItem.Checked = Global.Config.CheatsAlwaysOnTop;
		}

		private void CheatsOnOffLoadMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DisableCheatsOnLoad ^= true;
		}
		private void AutoloadMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RecentCheats.AutoLoad ^= true;
		}

		private void SaveWindowPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.CheatsSaveWindowPosition ^= true;
		}

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.CheatsAlwaysOnTop ^= true;
		}

		private void RestoreWindowSizeMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(defaultWidth, defaultHeight);
			Global.Config.CheatsSaveWindowPosition = true;
			Global.Config.CheatsAlwaysOnTop = TopMost = false;
			Global.Config.DisableCheatsOnLoad = false;

			Global.Config.CheatsColumnIndices = new Dictionary<string, int>
			{
				{ "NamesColumn", 0 },
				{ "AddressColumn", 1 },
				{ "ValueColumn", 2 },
				{ "CompareColumn", 3 },
				{ "OnColumn", 4 },
				{ "DomainColumn", 5 },
				{ "SizeColumn", 6 },
				{ "EndianColumn", 7 },
				{ "DisplayTypeColumn", 8 },
			};

			Global.Config.CheatsColumnIndices = new Dictionary<string, int>
			{
				{ "NamesColumn", 0 },
				{ "AddressColumn", 1 },
				{ "ValueColumn", 2 },
				{ "CompareColumn", 3 },
				{ "OnColumn", 4 },
				{ "DomainColumn", 5 },
				{ "SizeColumn", 6 },
				{ "EndianColumn", 7 },
				{ "DisplayTypeColumn", 8 },
			};

			Global.Config.CheatsColumnShow = new Dictionary<string, bool>()
			{
				{ "NamesColumn", true },
				{ "AddressColumn", true },
				{ "ValueColumn", true },
				{ "CompareColumn", true },
				{ "OnColumn", true },
				{ "DomainColumn", true },
				{ "SizeColumn", true },
				{ "EndianColumn", false },
				{ "DisplayTypeColumn", false },
			};

			LoadColumnInfo();
		}

		#endregion

		#region Columns

		private void ColumnsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ShowNameMenuItem.Checked = Global.Config.CheatsColumnShow[NAME];
			ShowAddressMenuItem.Checked = Global.Config.CheatsColumnShow[ADDRESS];
			ShowValueMenuItem.Checked = Global.Config.CheatsColumnShow[VALUE];
			ShowCompareMenuItem.Checked = Global.Config.CheatsColumnShow[COMPARE];
			ShowOnMenuItem.Checked = Global.Config.CheatsColumnShow[ON];
			ShowDomainMenuItem.Checked = Global.Config.CheatsColumnShow[DOMAIN];
			ShowSizeMenuItem.Checked = Global.Config.CheatsColumnShow[SIZE];
			ShowEndianMenuItem.Checked = Global.Config.CheatsColumnShow[ENDIAN];
			ShowDisplayTypeMenuItem.Checked = Global.Config.CheatsColumnShow[TYPE];
		}

		private void ShowNameMenuItem_Click(object sender, EventArgs e)
		{
			DoColumnToggle(NAME);
		}

		private void ShowAddressMenuItem_Click(object sender, EventArgs e)
		{
			DoColumnToggle(ADDRESS);
		}

		private void ShowValueMenuItem_Click(object sender, EventArgs e)
		{
			DoColumnToggle(VALUE);
		}

		private void ShowCompareMenuItem_Click(object sender, EventArgs e)
		{
			DoColumnToggle(COMPARE);
		}

		private void ShowOnMenuItem_Click(object sender, EventArgs e)
		{
			DoColumnToggle(ON);
		}

		private void ShowDomainMenuItem_Click(object sender, EventArgs e)
		{
			DoColumnToggle(DOMAIN);
		}

		private void ShowSizeMenuItem_Click(object sender, EventArgs e)
		{
			DoColumnToggle(SIZE);
		}

		private void ShowEndianMenuItem_Click(object sender, EventArgs e)
		{
			DoColumnToggle(ENDIAN);
		}

		private void ShowDisplayTypeMenuItem_Click(object sender, EventArgs e)
		{
			DoColumnToggle(TYPE);
		}

		#endregion

		#region ListView and Dialog Events

		private void CheatListView_Click(object sender, EventArgs e)
		{
			DoSelectedIndexChange();
		}

		private void CheatListView_ColumnReordered(object sender, ColumnReorderedEventArgs e)
		{

			Global.Config.CheatsColumnIndices[NAME] = CheatListView.Columns[NAME].DisplayIndex;
			Global.Config.CheatsColumnIndices[ADDRESS] = CheatListView.Columns[ADDRESS].DisplayIndex;
			Global.Config.CheatsColumnIndices[VALUE] = CheatListView.Columns[VALUE].DisplayIndex;
			Global.Config.CheatsColumnIndices[COMPARE] = CheatListView.Columns[COMPARE].DisplayIndex;
			Global.Config.CheatsColumnIndices[ON] = CheatListView.Columns[ON].DisplayIndex;
			Global.Config.CheatsColumnIndices[DOMAIN] = CheatListView.Columns[DOMAIN].DisplayIndex;
			Global.Config.CheatsColumnIndices[SIZE] = CheatListView.Columns[SIZE].DisplayIndex;
			Global.Config.CheatsColumnIndices[ENDIAN] = CheatListView.Columns[ENDIAN].DisplayIndex;
			Global.Config.CheatsColumnIndices[TYPE] = CheatListView.Columns[TYPE].DisplayIndex;
		}

		private void CheatListView_DoubleClick(object sender, EventArgs e)
		{
			Toggle();
		}

		private void CheatListView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete && !e.Control && !e.Alt && !e.Shift)
			{
				Remove();
			}
			else if (e.KeyCode == Keys.A && e.Control && !e.Alt && !e.Shift) //Select All
			{
				SelectAllMenuItem_Click(null, null);
			}
		}

		private void CheatListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			DoSelectedIndexChange();
		}

		private void CheatListView_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			var column = CheatListView.Columns[e.Column];
			if (column.Name != _sortedColumn)
			{
				_sortReverse = false;
			}

			Global.CheatList2.Sort(column.Name, _sortReverse);

			_sortedColumn = column.Name;
			_sortReverse ^= true;
			UpdateListView();
		}

		private void NewCheatForm_DragDrop(object sender, DragEventArgs e)
		{
			string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (Path.GetExtension(filePaths[0]) == (".cht"))
			{
				LoadFile(new FileInfo(filePaths[0]), append: false);
				UpdateListView();
				UpdateMessageLabel();
			}
		}

		private void NewCheatForm_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		#endregion

		#endregion
	}
}
