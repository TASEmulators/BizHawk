using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Sega.Genesis;

namespace BizHawk.Client.EmuHawk
{
	public partial class Cheats : Form, IToolForm
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

		private readonly Dictionary<string, int> _defaultColumnWidths = new Dictionary<string, int>
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

		private int _defaultWidth;
		private int _defaultHeight;
		private string _sortedColumn = String.Empty;
		private bool _sortReverse;

		public bool UpdateBefore { get { return false; } }

		public Cheats()
		{
			InitializeComponent();
			Closing += (o, e) =>
			{
				if (AskSave())
				{
					SaveConfigSettings();
				}
				else
				{
					e.Cancel = true;
				}
			};

			CheatListView.QueryItemText += CheatListView_QueryItemText;
			CheatListView.QueryItemBkColor += CheatListView_QueryItemBkColor;
			CheatListView.VirtualMode = true;

			_sortedColumn = String.Empty;
			_sortReverse = false;
			TopMost = Global.Config.CheatsAlwaysOnTop;
		}

		public void UpdateValues()
		{
			//Do nothing;
		}

		public void Restart()
		{
			StartNewList();
		}

		/// <summary>
		/// Tools that want to refresh the cheats list should call this, not UpdateValues
		/// </summary>
		public void UpdateDialog()
		{
			CheatListView.ItemCount = Global.CheatList.Count;
			TotalLabel.Text = Global.CheatList.CheatCount
				+ (Global.CheatList.CheatCount == 1 ? " cheat " : " cheats ")
				+ Global.CheatList.ActiveCount + " active";
		}

		public void LoadFileFromRecent(string path)
		{
			var ask_result = true;
			if (Global.CheatList.Changes)
			{
				ask_result = AskSave();
			}

			if (ask_result)
			{
				var load_result = Global.CheatList.Load(path, append: false);
				if (!load_result)
				{
					ToolHelpers.HandleLoadError(Global.Config.RecentWatches, path);
				}
				else
				{
					Global.Config.RecentWatches.Add(path);
					UpdateDialog();
					UpdateMessageLabel();
				}
			}
		}

		private void UpdateMessageLabel(bool saved = false)
		{
			string message;
			
			if (saved)
			{
				message = Path.GetFileName(Global.CheatList.CurrentFileName) + " saved.";
			}
			else
			{
				message = Path.GetFileName(Global.CheatList.CurrentFileName) + (Global.CheatList.Changes ? " *" : String.Empty);
			}

			MessageLabel.Text = message;
		}

		public bool AskSave()
		{
			if (Global.Config.SupressAskSave)
			{
				return true;
			}

			if (Global.CheatList.Changes)
			{
				GlobalWin.Sound.StopSound();
				var result = MessageBox.Show("Save Changes?", "Cheats", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
				GlobalWin.Sound.StartSound();
				if (result == DialogResult.Yes)
				{
					Global.CheatList.Save();
				}
				else if (result == DialogResult.No)
				{
					Global.CheatList.Changes = false;
					return true;
				}
				else if (result == DialogResult.Cancel)
				{
					return false;
				}
			}

			return true;
		}

		private void LoadFile(FileSystemInfo file, bool append)
		{
			if (file != null)
			{
				var result = true;
				if (Global.CheatList.Changes)
				{
					result = AskSave();
				}

				if (result)
				{
					Global.CheatList.Load(file.FullName, append);
					UpdateDialog();
					UpdateMessageLabel();
					Global.Config.RecentCheats.Add(Global.CheatList.CurrentFileName);
				}
			}
		}

		private static bool SaveAs()
		{
			var file = ToolHelpers.GetCheatSaveFileFromUser(Global.CheatList.CurrentFileName);
			return file != null && Global.CheatList.SaveFile(file.FullName);
		}

		private void NewCheatForm_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
			ToggleGameGenieButton();
			CheatEditor.SetAddEvent(AddCheat);
			CheatEditor.SetEditEvent(AddCheat); //CheatList.Add is already an upsert, so there is nothing different to handle here
			UpdateDialog();
		}

		private void ToggleGameGenieButton()
		{
			GameGenieToolbarSeparator.Visible =
				LoadGameGenieToolbarItem.Visible =
				((Global.Emulator is NES)
				|| (Global.Emulator is Genesis)
				|| (Global.Emulator.SystemId == "GB")
				|| (Global.Game.System == "GG")
				|| (Global.Emulator is LibsnesCore));
		}

		private void AddCheat()
		{
			Global.CheatList.Add(CheatEditor.Cheat);
			UpdateDialog();
			UpdateMessageLabel();
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
			_defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
			_defaultHeight = Size.Height;

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
				width = _defaultColumnWidths[columnName];
			}

			return width;
		}

		private void CheatListView_QueryItemText(int index, int column, out string text)
		{
			text = String.Empty;
			if (index >= Global.CheatList.Count || Global.CheatList[index].IsSeparator)
			{
				return;
			}

			var columnName = CheatListView.Columns[column].Name;

			switch (columnName)
			{
				case NAME:
					text = Global.CheatList[index].Name;
					break;
				case ADDRESS:
					text = Global.CheatList[index].AddressStr;
					break;
				case VALUE:
					text = Global.CheatList[index].ValueStr;
					break;
				case COMPARE:
					text = Global.CheatList[index].CompareStr;
					break;
				case ON:
					text = Global.CheatList[index].Enabled ? "*" : String.Empty;
					break;
				case DOMAIN:
					text = Global.CheatList[index].Domain.Name;
					break;
				case SIZE:
					text = Global.CheatList[index].Size.ToString();
					break;
				case ENDIAN:
					text = (Global.CheatList[index].BigEndian ?? false) ? "Big" : "Little";
					break;
				case TYPE:
					text = Watch.DisplayTypeToString(Global.CheatList[index].Type);
					break;
			}
		}

		private void CheatListView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (index < Global.CheatList.Count)
			{
				if (Global.CheatList[index].IsSeparator)
				{
					color = BackColor;
				}
				else if (Global.CheatList[index].Enabled)
				{
					color = Color.LightCyan;
				}
			}
		}

		private IEnumerable<int> SelectedIndices
		{
			get { return CheatListView.SelectedIndices.Cast<int>(); }
		}

		private IEnumerable<Cheat> SelectedItems
		{
			get { return SelectedIndices.Select(index => Global.CheatList[index]); }
		}

		private IEnumerable<Cheat> SelectedCheats
		{
			get { return SelectedItems.Where(x => !x.IsSeparator); }
		}

		private void MoveUp()
		{
			var indices = SelectedIndices.ToList();
			if (indices.Count == 0 || indices[0] == 0)
			{
				return;
			}

			foreach (var index in indices)
			{
				var cheat = Global.CheatList[index];
				Global.CheatList.Remove(cheat);
				Global.CheatList.Insert(index - 1, cheat);
			}

			var newindices = indices.Select(t => t - 1).ToList();

			CheatListView.SelectedIndices.Clear();
			foreach (var newi in newindices)
			{
				CheatListView.SelectItem(newi, true);
			}

			UpdateMessageLabel();
			UpdateDialog();
		}

		private void MoveDown()
		{
			var indices = SelectedIndices.ToList();
			if (indices.Count == 0 || indices.Last() == Global.CheatList.Count - 1)
			{
				return;
			}

			for (var i = indices.Count - 1; i >= 0; i--)
			{
				var cheat = Global.CheatList[indices[i]];
				Global.CheatList.Remove(cheat);
				Global.CheatList.Insert(indices[i] + 1, cheat);
			}

			UpdateMessageLabel();

			var newindices = indices.Select(t => t + 1).ToList();

			CheatListView.SelectedIndices.Clear();
			foreach (var newi in newindices)
			{
				CheatListView.SelectItem(newi, true);
			}

			UpdateDialog();
		}

		private void Remove()
		{
			var items = SelectedItems.ToList();
			if (items.Any())
			{
				foreach (var item in items)
				{
					Global.CheatList.Remove(item);
				}

				CheatListView.SelectedIndices.Clear();
				UpdateDialog();
			}
		}

		private void Toggle()
		{
			SelectedCheats.ToList().ForEach(x => x.Toggle());
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
			if (SelectedCheats.Any())
			{
				var cheat = SelectedCheats.First();
				CheatEditor.SetCheat(cheat);
				CheatGroupBox.Text = "Editing Cheat " + cheat.Name + " - " + cheat.AddressStr;
			}
			else
			{
				CheatEditor.ClearForm();
				CheatGroupBox.Text = "New Cheat";
			}
		}

		private void StartNewList()
		{
			Global.CheatList.NewList(GlobalWin.MainForm.GenerateDefaultCheatFilename());
			UpdateDialog();
			UpdateMessageLabel();
			ToggleGameGenieButton();
		}

		private void NewList()
		{
			var result = true;
			if (Global.CheatList.Changes)
			{
				result = AskSave();
			}

			if (result)
			{
				StartNewList();
			}
		}

		public string GenerateDefaultCheatFilename()
		{
			var pathEntry = Global.Config.PathEntries[Global.Emulator.SystemId, "Cheats"] ??
			                      Global.Config.PathEntries[Global.Emulator.SystemId, "Base"];
			var path = PathManager.MakeAbsolutePath(pathEntry.Path, Global.Emulator.SystemId);

			var f = new FileInfo(path);
			if (f.Directory != null && f.Directory.Exists == false)
			{
				f.Directory.Create();
			}

			return Path.Combine(path, PathManager.FilesystemSafeName(Global.Game) + ".cht");
		}

		#region Events

		#region File

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveMenuItem.Enabled = Global.CheatList.Changes;
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(
				ToolHelpers.GenerateRecentMenu(Global.Config.RecentCheats, LoadFileFromRecent)
			);
		}

		private void NewMenuItem_Click(object sender, EventArgs e)
		{
			NewList();
		}

		private void OpenMenuItem_Click(object sender, EventArgs e)
		{
			var append = sender == AppendMenuItem;
			LoadFile(ToolHelpers.GetCheatFileFromUser(Global.CheatList.CurrentFileName), append);
		}

		private void SaveMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.CheatList.Changes)
			{
				if (Global.CheatList.Save())
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
			if (SaveAs())
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

			DisableAllCheatsMenuItem.Enabled = Global.CheatList.ActiveCount > 0;

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
					Global.CheatList.Add(new Cheat(Global.CheatList[index]));
				}
			}

			UpdateDialog();
			UpdateMessageLabel();
		}

		private void InsertSeparatorMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedIndices.Any())
			{
				Global.CheatList.Insert(SelectedIndices.Max(), Cheat.Separator);
			}
			else
			{
				Global.CheatList.Add(Cheat.Separator);
			}
			
			UpdateDialog();
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
			for (var i = 0; i < Global.CheatList.Count; i++)
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
			Global.CheatList.DisableAll();
		}

		private void OpenGameGenieEncoderDecoderMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.MainForm.LoadGameGenieEc();
		}

		#endregion

		#region Options

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AlwaysLoadCheatsMenuItem.Checked = Global.Config.LoadCheatFileByGame;
			AutoSaveCheatsMenuItem.Checked = Global.Config.CheatsAutoSaveOnClose;
			DisableCheatsOnLoadMenuItem.Checked = Global.Config.DisableCheatsOnLoad;
			AutoloadMenuItem.Checked = Global.Config.RecentCheats.AutoLoad;
			SaveWindowPositionMenuItem.Checked = Global.Config.CheatsSaveWindowPosition;
			AlwaysOnTopMenuItem.Checked = Global.Config.CheatsAlwaysOnTop;
		}

		private void AlwaysLoadCheatsMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.LoadCheatFileByGame ^= true;
		}

		private void AutoSaveCheatsMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.CheatsAutoSaveOnClose ^= true;
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
			Size = new Size(_defaultWidth, _defaultHeight);
			Global.Config.CheatsSaveWindowPosition = true;
			Global.Config.CheatsAlwaysOnTop = TopMost = false;
			Global.Config.DisableCheatsOnLoad = false;
			Global.Config.LoadCheatFileByGame = true;
			Global.Config.CheatsAutoSaveOnClose = true;

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

			Global.Config.CheatsColumnShow = new Dictionary<string, bool>
				{
				{ "NamesColumn", true },
				{ "AddressColumn", true },
				{ "ValueColumn", true },
				{ "CompareColumn", true },
				{ "OnColumn", false },
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

			Global.CheatList.Sort(column.Name, _sortReverse);

			_sortedColumn = column.Name;
			_sortReverse ^= true;
			UpdateDialog();
		}

		private void NewCheatForm_DragDrop(object sender, DragEventArgs e)
		{
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (Path.GetExtension(filePaths[0]) == (".cht"))
			{
				LoadFile(new FileInfo(filePaths[0]), append: false);
				UpdateDialog();
				UpdateMessageLabel();
			}
		}

		private void NewCheatForm_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			ToggleContextMenuItem.Enabled =
				RemoveContextMenuItem.Enabled =
				SelectedCheats.Any();

			DisableAllContextMenuItem.Enabled = Global.CheatList.ActiveCount > 0;
		}

		private void ViewInHexEditorContextMenuItem_Click(object sender, EventArgs e)
		{
			var selected = SelectedCheats.ToList();
			if (selected.Any())
			{
				GlobalWin.Tools.Load<HexEditor>();

				if (selected.Select(x => x.Domain).Distinct().Count() > 1)
				{
					ToolHelpers.ViewInHexEditor(selected[0].Domain, new List<int> { selected.First().Address ?? 0 });
				}
				else
				{
					ToolHelpers.ViewInHexEditor(selected[0].Domain, selected.Select(x => x.Address ?? 0));
				}
			}
		}

		#endregion

		#endregion
	}
}
