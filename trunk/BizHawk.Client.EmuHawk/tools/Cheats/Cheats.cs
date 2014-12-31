using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Sega.Genesis;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class Cheats : Form, IToolForm
	{
		[RequiredService]
		private IMemoryDomains Core { get; set; }

		[RequiredService] // TODO: use of this property should be factored out
		private IEmulator Emu { get; set; }

		[ConfigPersist]
		public CheatsSettings Settings { get; set; }

		public class CheatsSettings : ToolDialogSettings
		{
			public CheatsSettings()
			{
				CheatsColumnWidths = new Dictionary<string, int>
				{
					{ "NamesColumn", 128 },
					{ "AddressColumn", 60 },
					{ "ValueColumn", 59 },
					{ "CompareColumn", 59 },
					{ "OnColumn", 28 },
					{ "DomainColumn", 55 },
					{ "SizeColumn", 55 },
					{ "EndianColumn", 55 },
					{ "DisplayTypeColumn", 55 },
				};

				CheatsColumnIndices = new Dictionary<string, int>
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

				CheatsColumnShow = new Dictionary<string, bool>
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
			}

			public Dictionary<string, int> CheatsColumnWidths { get; set; }
			public Dictionary<string, int> CheatsColumnIndices { get; set; }
			public Dictionary<string, bool> CheatsColumnShow { get; set; }
		}

		public const string NAME = "NamesColumn";
		public const string ADDRESS = "AddressColumn";
		public const string VALUE = "ValueColumn";
		public const string COMPARE = "CompareColumn";
		public const string ON = "OnColumn";
		public const string DOMAIN = "DomainColumn";
		public const string SIZE = "SizeColumn";
		public const string ENDIAN = "EndianColumn";
		public const string TYPE = "DisplayTypeColumn";

		private int _defaultWidth;
		private int _defaultHeight;
		private string _sortedColumn = string.Empty;
		private bool _sortReverse;

		public bool UpdateBefore { get { return false; } }

		public Cheats()
		{
			InitializeComponent();
			Settings = new CheatsSettings();

			Closing += (o, e) =>
			{
				if (AskSaveChanges())
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

			_sortedColumn = string.Empty;
			_sortReverse = false;
		}

		public void UpdateValues()
		{
			// Do nothing
		}

		public void FastUpdate()
		{
			// Do nothing
		}

		public void Restart()
		{
			CheatEditor.Core = Core;
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
			var askResult = !Global.CheatList.Changes || AskSaveChanges();
			if (askResult)
			{
				var loadResult = Global.CheatList.Load(path, append: false);
				if (!loadResult)
				{
					Global.Config.RecentWatches.HandleLoadError(path);
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
			MessageLabel.Text = saved 
				? Path.GetFileName(Global.CheatList.CurrentFileName) + " saved."
				: Path.GetFileName(Global.CheatList.CurrentFileName) + (Global.CheatList.Changes ? " *" : string.Empty);
		}

		public bool AskSaveChanges()
		{
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
					result = AskSaveChanges();
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

		private void Cheats_Load(object sender, EventArgs e)
		{
			TopMost = Settings.TopMost;
			CheatEditor.Core = Core;
			LoadConfigSettings();
			ToggleGameGenieButton();
			CheatEditor.SetAddEvent(AddCheat);
			CheatEditor.SetEditEvent(EditCheat);
			UpdateDialog();
		}

		private void ToggleGameGenieButton()
		{
			GameGenieToolbarSeparator.Visible =
				LoadGameGenieToolbarItem.Visible =
				   (Emu.SystemId == "NES")
				|| (Emu.SystemId == "GEN" && VersionInfo.DeveloperBuild)
				|| (Emu.SystemId == "GB")
				|| (Global.Game.System == "GG")
				|| (Emu is LibsnesCore);
		}

		private void AddCheat()
		{
			Global.CheatList.Add(CheatEditor.Cheat);
			UpdateDialog();
			UpdateMessageLabel();
		}

		private void EditCheat()
		{
			Global.CheatList.Remove(CheatEditor.OriginalCheat);
			AddCheat();
		}

		public void SaveConfigSettings()
		{
			SaveColumnInfo();
			Settings.Wndx = Location.X;
			Settings.Wndy = Location.Y;
			Settings.Width = Right - Left;
			Settings.Height = Bottom - Top;
		}

		private void LoadConfigSettings()
		{
			_defaultWidth = Size.Width;
			_defaultHeight = Size.Height;

			if (Settings.UseWindowPosition)
			{
				Location = Settings.WindowPosition;
			}

			if (Settings.UseWindowSize)
			{
				Size = Settings.WindowSize;
			}

			LoadColumnInfo();
		}

		private void LoadColumnInfo()
		{
			CheatListView.Columns.Clear();
			CheatListView.AddColumn(NAME, Settings.CheatsColumnShow[NAME], GetColumnWidth(NAME));
			CheatListView.AddColumn(ADDRESS, Settings.CheatsColumnShow[ADDRESS], GetColumnWidth(ADDRESS));
			CheatListView.AddColumn(VALUE, Settings.CheatsColumnShow[VALUE], GetColumnWidth(VALUE));
			CheatListView.AddColumn(COMPARE, Settings.CheatsColumnShow[COMPARE], GetColumnWidth(COMPARE));
			CheatListView.AddColumn(ON, Settings.CheatsColumnShow[ON], GetColumnWidth(ON));
			CheatListView.AddColumn(DOMAIN, Settings.CheatsColumnShow[DOMAIN], GetColumnWidth(DOMAIN));
			CheatListView.AddColumn(SIZE, Settings.CheatsColumnShow[SIZE], GetColumnWidth(SIZE));
			CheatListView.AddColumn(ENDIAN, Settings.CheatsColumnShow[ENDIAN], GetColumnWidth(ENDIAN));
			CheatListView.AddColumn(TYPE, Settings.CheatsColumnShow[TYPE], GetColumnWidth(TYPE));

			ColumnPositions();
		}

		private void ColumnPositions()
		{
			var columns = Settings.CheatsColumnIndices
					.Where(x => CheatListView.Columns.ContainsKey(x.Key))
					.OrderBy(x => x.Value)
					.ToList();

			for (var i = 0; i < columns.Count; i++)
			{
				if (CheatListView.Columns.ContainsKey(columns[i].Key))
				{
					CheatListView.Columns[columns[i].Key].DisplayIndex = i;
				}
			}
		}

		// TODO: delete me
		private int GetColumnWidth(string columnName)
		{
			return Settings.CheatsColumnWidths[columnName];
		}

		private void SaveColumnInfo()
		{
			if (CheatListView.Columns[NAME] != null)
			{
				Settings.CheatsColumnIndices[NAME] = CheatListView.Columns[NAME].DisplayIndex;
				Settings.CheatsColumnWidths[NAME] = CheatListView.Columns[NAME].Width;
			}

			if (CheatListView.Columns[ADDRESS] != null)
			{
				Settings.CheatsColumnIndices[ADDRESS] = CheatListView.Columns[ADDRESS].DisplayIndex;
				Settings.CheatsColumnWidths[ADDRESS] = CheatListView.Columns[ADDRESS].Width;
			}

			if (CheatListView.Columns[VALUE] != null)
			{
				Settings.CheatsColumnIndices[VALUE] = CheatListView.Columns[VALUE].DisplayIndex;
				Settings.CheatsColumnWidths[VALUE] = CheatListView.Columns[VALUE].Width;
			}

			if (CheatListView.Columns[COMPARE] != null)
			{
				Settings.CheatsColumnIndices[COMPARE] = CheatListView.Columns[COMPARE].DisplayIndex;
				Settings.CheatsColumnWidths[COMPARE] = CheatListView.Columns[COMPARE].Width;
			}

			if (CheatListView.Columns[ON] != null)
			{
				Settings.CheatsColumnIndices[ON] = CheatListView.Columns[ON].DisplayIndex;
				Settings.CheatsColumnWidths[ON] = CheatListView.Columns[ON].Width;
			}

			if (CheatListView.Columns[DOMAIN] != null)
			{
				Settings.CheatsColumnIndices[DOMAIN] = CheatListView.Columns[DOMAIN].DisplayIndex;
				Settings.CheatsColumnWidths[DOMAIN] = CheatListView.Columns[DOMAIN].Width;
			}

			if (CheatListView.Columns[SIZE] != null)
			{
				Settings.CheatsColumnIndices[SIZE] = CheatListView.Columns[SIZE].DisplayIndex;
				Settings.CheatsColumnWidths[SIZE] = CheatListView.Columns[SIZE].Width;
			}

			if (CheatListView.Columns[ENDIAN] != null)
			{
				Settings.CheatsColumnIndices[ENDIAN] = CheatListView.Columns[ENDIAN].DisplayIndex;
				Settings.CheatsColumnWidths[ENDIAN] = CheatListView.Columns[ENDIAN].Width;
			}

			if (CheatListView.Columns[TYPE] != null)
			{
				Settings.CheatsColumnIndices[TYPE] = CheatListView.Columns[TYPE].DisplayIndex;
				Settings.CheatsColumnWidths[TYPE] = CheatListView.Columns[TYPE].Width;
			}
		}

		private void DoColumnToggle(string column)
		{
			Settings.CheatsColumnShow[column] ^= true;
			SaveColumnInfo();
			LoadColumnInfo();
		}

		private void CheatListView_QueryItemText(int index, int column, out string text)
		{
			text = string.Empty;
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
					text = Global.CheatList[index].Enabled ? "*" : string.Empty;
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

		private void DoSelectedIndexChange()
		{
			if (!CheatListView.SelectAllInProgress)
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
		}

		private void StartNewList()
		{
			var result = !Global.CheatList.Changes || AskSaveChanges();
			if (result)
			{
				Global.CheatList.NewList(ToolManager.GenerateDefaultCheatFilename());
				UpdateDialog();
				UpdateMessageLabel();
				ToggleGameGenieButton();
			}
		}

		private void NewList()
		{
			var result = !Global.CheatList.Changes || AskSaveChanges();
			if (result)
			{
				StartNewList();
			}
		}

		private void RefreshFloatingWindowControl()
		{
			Owner = Settings.FloatingWindow ? null : GlobalWin.MainForm;
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
				Global.Config.RecentCheats.RecentMenu(LoadFileFromRecent));
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
				MoveUpMenuItem.Enabled =
				MoveDownMenuItem.Enabled =
				ToggleMenuItem.Enabled =
				SelectedIndices.Any();

			DisableAllCheatsMenuItem.Enabled = Global.CheatList.ActiveCount > 0;

			GameGenieSeparator.Visible =
				OpenGameGenieEncoderDecoderMenuItem.Visible = 
					   (Emu.SystemId == "NES") 
					|| (Emu is Genesis)
					|| (Emu.SystemId == "GB")
					|| (Global.Game.System == "GG")
					|| (Emu is LibsnesCore);
		}

		private void RemoveCheatMenuItem_Click(object sender, EventArgs e)
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

		private void MoveDownMenuItem_Click(object sender, EventArgs e)
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

		private void SelectAllMenuItem_Click(object sender, EventArgs e)
		{
			CheatListView.SelectAll();
		}

		private void ToggleMenuItem_Click(object sender, EventArgs e)
		{
			SelectedCheats.ToList().ForEach(x => x.Toggle());
		}

		private void DisableAllCheatsMenuItem_Click(object sender, EventArgs e)
		{
			Global.CheatList.DisableAll();
		}

		private void OpenGameGenieEncoderDecoderMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.LoadGameGenieEc();
		}

		#endregion

		#region Options

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AlwaysLoadCheatsMenuItem.Checked = Global.Config.LoadCheatFileByGame;
			AutoSaveCheatsMenuItem.Checked = Global.Config.CheatsAutoSaveOnClose;
			DisableCheatsOnLoadMenuItem.Checked = Global.Config.DisableCheatsOnLoad;
			AutoloadMenuItem.Checked = Global.Config.RecentCheats.AutoLoad;
			SaveWindowPositionMenuItem.Checked = Settings.SaveWindowPosition;
			AlwaysOnTopMenuItem.Checked = Settings.TopMost;
			FloatingWindowMenuItem.Checked = Settings.FloatingWindow;
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
			Settings.SaveWindowPosition ^= true;
		}

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			Settings.TopMost ^= true;
		}

		private void FloatingWindowMenuItem_Click(object sender, EventArgs e)
		{
			Settings.FloatingWindow ^= true;
			RefreshFloatingWindowControl();
		}

		private void RestoreWindowSizeMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(_defaultWidth, _defaultHeight);
			Settings = new CheatsSettings();

			Global.Config.DisableCheatsOnLoad = false;
			Global.Config.LoadCheatFileByGame = true;
			Global.Config.CheatsAutoSaveOnClose = true;

			RefreshFloatingWindowControl();
			ColumnPositions();
			LoadColumnInfo();
		}

		#endregion

		#region Columns

		private void ColumnsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ShowNameMenuItem.Checked = Settings.CheatsColumnShow[NAME];
			ShowAddressMenuItem.Checked = Settings.CheatsColumnShow[ADDRESS];
			ShowValueMenuItem.Checked = Settings.CheatsColumnShow[VALUE];
			ShowCompareMenuItem.Checked = Settings.CheatsColumnShow[COMPARE];
			ShowOnMenuItem.Checked = Settings.CheatsColumnShow[ON];
			ShowDomainMenuItem.Checked = Settings.CheatsColumnShow[DOMAIN];
			ShowSizeMenuItem.Checked = Settings.CheatsColumnShow[SIZE];
			ShowEndianMenuItem.Checked = Settings.CheatsColumnShow[ENDIAN];
			ShowDisplayTypeMenuItem.Checked = Settings.CheatsColumnShow[TYPE];
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

		private void CheatListView_DoubleClick(object sender, EventArgs e)
		{
			ToggleMenuItem_Click(sender, e);
		}

		private void CheatListView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete && !e.Control && !e.Alt && !e.Shift)
			{
				RemoveCheatMenuItem_Click(sender, e);
			}
			else if (e.KeyCode == Keys.A && e.Control && !e.Alt && !e.Shift)
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
			if (Path.GetExtension(filePaths[0]) == ".cht")
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

		private void CheatsContextMenu_Opening(object sender, CancelEventArgs e)
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
					ToolHelpers.ViewInHexEditor(selected[0].Domain, new List<int> { selected.First().Address ?? 0 }, selected.First().Size);
				}
				else
				{
					ToolHelpers.ViewInHexEditor(selected.First().Domain, selected.Select(x => x.Address ?? 0), selected.First().Size);
				}
			}
		}

		protected override void OnShown(EventArgs e)
		{
			RefreshFloatingWindowControl();
			base.OnShown(e);
		}

		#endregion

		#endregion
	}
}
