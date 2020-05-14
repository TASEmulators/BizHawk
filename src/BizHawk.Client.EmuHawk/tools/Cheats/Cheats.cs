using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.ToolExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class Cheats : ToolFormBase, IToolForm
	{
		private const string NameColumn = "NamesColumn";
		private const string AddressColumn = "AddressColumn";
		private const string ValueColumn = "ValueColumn";
		private const string CompareColumn = "CompareColumn";
		private const string OnColumn = "OnColumn";
		private const string DomainColumn = "DomainColumn";
		private const string SizeColumn = "SizeColumn";
		private const string EndianColumn = "EndianColumn";
		private const string TypeColumn = "DisplayTypeColumn";
		private const string ComparisonTypeColumn = "ComparisonTypeColumn";

		private int _defaultWidth;
		private int _defaultHeight;
		private string _sortedColumn;
		private bool _sortReverse;

		public Cheats()
		{
			InitializeComponent();
			Settings = new CheatsSettings();

			Closing += (o, e) =>
			{
				SaveConfigSettings();
			};

			CheatListView.QueryItemText += CheatListView_QueryItemText;
			CheatListView.QueryItemBkColor += CheatListView_QueryItemBkColor;

			_sortedColumn = "";
			_sortReverse = false;
		}

		[RequiredService]
		private IMemoryDomains Core { get; set; }

		[ConfigPersist]
		public CheatsSettings Settings { get; set; }

		public void Restart()
		{
			CheatEditor.MemoryDomains = Core;
			CheatEditor.Restart();
		}

		/// <summary>
		/// Tools that want to refresh the cheats list should call this, not UpdateValues
		/// </summary>
		protected override void GeneralUpdate()
		{
			CheatListView.RowCount = Global.CheatList.Count;
			TotalLabel.Text = $"{Global.CheatList.CheatCount} {(Global.CheatList.CheatCount == 1 ? "cheat" : "cheats")} {Global.CheatList.ActiveCount} active";
		}

		private void LoadFileFromRecent(string path)
		{
			var askResult = !Global.CheatList.Changes || AskSaveChanges();
			if (askResult)
			{
				var loadResult = Global.CheatList.Load(path, append: false);
				if (!loadResult)
				{
					Config.RecentCheats.HandleLoadError(path);
				}
				else
				{
					Config.RecentCheats.Add(path);
					GeneralUpdate();
					UpdateMessageLabel();
				}
			}
		}

		private void UpdateMessageLabel(bool saved = false)
		{
			MessageLabel.Text = saved
				? $"{Path.GetFileName(Global.CheatList.CurrentFileName)} saved."
				: Global.CheatList.Changes
					? $"{Path.GetFileName(Global.CheatList.CurrentFileName)} *"
					: Path.GetFileName(Global.CheatList.CurrentFileName);
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
					GeneralUpdate();
					UpdateMessageLabel();
					Config.RecentCheats.Add(Global.CheatList.CurrentFileName);
				}
			}
		}

		private bool SaveAs()
		{
			var file = SaveFileDialog(
				Global.CheatList.CurrentFileName,
				Config.PathEntries.CheatsAbsolutePath(Global.Game.System),
				"Cheat Files",
				"cht");

			return file != null && Global.CheatList.SaveFile(file.FullName);
		}

		private void Cheats_Load(object sender, EventArgs e)
		{
			// Hack for previous config settings
			if (Settings.Columns.Any(c => string.IsNullOrWhiteSpace(c.Text)))
			{
				Settings = new CheatsSettings();
			}

			TopMost = Settings.TopMost;
			CheatEditor.MemoryDomains = Core;
			LoadConfigSettings();
			CheatsMenu.Items.Add(CheatListView.ToColumnsMenu(ColumnToggleCallback));
			ToggleGameGenieButton();
			CheatEditor.SetAddEvent(AddCheat);
			CheatEditor.SetEditEvent(EditCheat);
			GeneralUpdate();
		}

		private void SetColumns()
		{
			foreach (var column in Settings.Columns)
			{
				if (CheatListView.AllColumns[column.Name] == null)
				{
					CheatListView.AllColumns.Add(column);
				}
			}
		}

		private void ColumnToggleCallback()
		{
			Settings.Columns = CheatListView.AllColumns;
		}

		private void ToggleGameGenieButton()
		{
			GameGenieToolbarSeparator.Visible =
				LoadGameGenieToolbarItem.Visible =
				Tools.IsAvailable<GameShark>();
		}

		private void AddCheat()
		{
			Global.CheatList.Add(CheatEditor.GetCheat());
			GeneralUpdate();
			UpdateMessageLabel();
		}

		private void EditCheat()
		{
			var newCheat = CheatEditor.GetCheat();

			if (!newCheat.IsSeparator) // If a separator comes from the cheat editor something must have been invalid
			{
				Global.CheatList.Exchange(CheatEditor.OriginalCheat, newCheat);
				GeneralUpdate();
				UpdateMessageLabel();
			}
		}

		private void SaveConfigSettings()
		{
			Settings.Columns = CheatListView.AllColumns;

			if (WindowState == FormWindowState.Normal)
			{
				Settings.Wndx = Location.X;
				Settings.Wndy = Location.Y;
				Settings.Width = Right - Left;
				Settings.Height = Bottom - Top;
			}
		}

		private void LoadConfigSettings()
		{
			_defaultWidth = Size.Width;
			_defaultHeight = Size.Height;

			if (Settings.UseWindowPosition && IsOnScreen(Settings.TopLeft))
			{
				Location = Settings.WindowPosition;
			}

			if (Settings.UseWindowSize)
			{
				Size = Settings.WindowSize;
			}

			CheatListView.AllColumns.Clear();
			SetColumns();
		}

		private void CheatListView_QueryItemText(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			text = "";
			if (index >= Global.CheatList.Count || Global.CheatList[index].IsSeparator)
			{
				return;
			}

			var columnName = column.Name;

			switch (columnName)
			{
				case NameColumn:
					text = Global.CheatList[index].Name;
					break;
				case AddressColumn:
					text = Global.CheatList[index].AddressStr;
					break;
				case ValueColumn:
					text = Global.CheatList[index].ValueStr;
					break;
				case CompareColumn:
					text = Global.CheatList[index].CompareStr;
					break;
				case OnColumn:
					text = Global.CheatList[index].Enabled ? "*" : "";
					break;
				case DomainColumn:
					text = Global.CheatList[index].Domain.Name;
					break;
				case SizeColumn:
					text = Global.CheatList[index].Size.ToString();
					break;
				case EndianColumn:
					text = (Global.CheatList[index].BigEndian ?? false) ? "Big" : "Little";
					break;
				case TypeColumn:
					text = Watch.DisplayTypeToString(Global.CheatList[index].Type);
					break;
				case ComparisonTypeColumn:
					text = Global.CheatList[index].ComparisonType switch
						{
							Cheat.CompareType.None => "",
							Cheat.CompareType.Equal => "=",
							Cheat.CompareType.GreaterThan => ">",
							Cheat.CompareType.GreaterThanOrEqual => ">=",
							Cheat.CompareType.LessThan => "<",
							Cheat.CompareType.LessThanOrEqual => "<=",
							Cheat.CompareType.NotEqual => "!=",
							_ => ""
						};

					break;
			}
		}

		private void CheatListView_QueryItemBkColor(int index, RollColumn column, ref Color color)
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

		private IEnumerable<int> SelectedIndices => CheatListView.SelectedRows;

		private IEnumerable<Cheat> SelectedItems => SelectedIndices.Select(index => Global.CheatList[index]);

		private IEnumerable<Cheat> SelectedCheats => SelectedItems.Where(x => !x.IsSeparator);

		private void DoSelectedIndexChange()
		{
			if (SelectedCheats.Any())
			{
				var cheat = SelectedCheats.First();
				CheatEditor.SetCheat(cheat);
				CheatGroupBox.Text = $"Editing Cheat {cheat.Name} - {cheat.AddressStr}";
			}
			else
			{
				CheatEditor.ClearForm();
				CheatGroupBox.Text = "New Cheat";
			}
		}

		private void StartNewList()
		{
			var result = !Global.CheatList.Changes || AskSaveChanges();
			if (result)
			{
				Global.CheatList.NewList(Tools.GenerateDefaultCheatFilename());
				GeneralUpdate();
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





		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveMenuItem.Enabled = Global.CheatList.Changes;
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(Config.RecentCheats.RecentMenu(LoadFileFromRecent, "Cheats", noAutoload: true));
		}

		private void NewMenuItem_Click(object sender, EventArgs e)
		{
			NewList();
		}

		private void OpenMenuItem_Click(object sender, EventArgs e)
		{
			var file = OpenFileDialog(
				Global.CheatList.CurrentFileName,
				Config.PathEntries.CheatsAbsolutePath(Global.Game.System),
				"Cheat Files",
				"cht");

			LoadFile(file, append: sender == AppendMenuItem);
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





		private void CheatsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RemoveCheatMenuItem.Enabled =
				MoveUpMenuItem.Enabled =
				MoveDownMenuItem.Enabled =
				ToggleMenuItem.Enabled =
				SelectedIndices.Any();

			// Always leave enabled even if no cheats enabled. This way the hotkey will always work however a new cheat is enabled
			// DisableAllCheatsMenuItem.Enabled = Global.CheatList.ActiveCount > 0;

			GameGenieSeparator.Visible =
				OpenGameGenieEncoderDecoderMenuItem.Visible =
				Tools.IsAvailable<GameShark>();
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

				CheatListView.DeselectAll();
				GeneralUpdate();
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
			
			GeneralUpdate();
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

			var newIndices = indices.Select(t => t - 1);

			CheatListView.DeselectAll();
			foreach (var index in newIndices)
			{
				CheatListView.SelectRow(index, true);
			}

			UpdateMessageLabel();
			GeneralUpdate();
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

			var newIndices = indices.Select(t => t + 1);

			CheatListView.DeselectAll();
			foreach (var index in newIndices)
			{
				CheatListView.SelectRow(index, true);
			}

			GeneralUpdate();
		}

		private void SelectAllMenuItem_Click(object sender, EventArgs e)
		{
			CheatListView.SelectAll();
		}

		private void ToggleMenuItem_Click(object sender, EventArgs e)
		{
			foreach (var x in SelectedCheats)
			{
				x.Toggle();
			}
			CheatListView.Refresh();
		}

		private void DisableAllCheatsMenuItem_Click(object sender, EventArgs e)
		{	
			Global.CheatList.DisableAll();
		}

		private void OpenGameGenieEncoderDecoderMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<GameShark>();
		}





		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AlwaysLoadCheatsMenuItem.Checked = Config.LoadCheatFileByGame;
			AutoSaveCheatsMenuItem.Checked = Config.CheatsAutoSaveOnClose;
			DisableCheatsOnLoadMenuItem.Checked = Config.DisableCheatsOnLoad;
			AutoloadMenuItem.Checked = Config.RecentCheats.AutoLoad;
			SaveWindowPositionMenuItem.Checked = Settings.SaveWindowPosition;
			AlwaysOnTopMenuItem.Checked = Settings.TopMost;
			FloatingWindowMenuItem.Checked = Settings.FloatingWindow;
		}

		private void AlwaysLoadCheatsMenuItem_Click(object sender, EventArgs e)
		{
			Config.LoadCheatFileByGame ^= true;
		}

		private void AutoSaveCheatsMenuItem_Click(object sender, EventArgs e)
		{
			Config.CheatsAutoSaveOnClose ^= true;
		}

		private void CheatsOnOffLoadMenuItem_Click(object sender, EventArgs e)
		{
			Config.DisableCheatsOnLoad ^= true;
		}

		private void AutoloadMenuItem_Click(object sender, EventArgs e)
		{
			Config.RecentCheats.AutoLoad ^= true;
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
			RefreshFloatingWindowControl(Settings.FloatingWindow);
		}

		private void RestoreDefaultsMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(_defaultWidth, _defaultHeight);
			Settings = new CheatsSettings();

			CheatsMenu.Items.Remove(
				CheatsMenu.Items
					.OfType<ToolStripMenuItem>()
					.First(x => x.Name == "GeneratedColumnsSubMenu"));

			CheatsMenu.Items.Add(CheatListView.ToColumnsMenu(ColumnToggleCallback));

			Config.DisableCheatsOnLoad = false;
			Config.LoadCheatFileByGame = true;
			Config.CheatsAutoSaveOnClose = true;

			RefreshFloatingWindowControl(Settings.FloatingWindow);
			CheatListView.AllColumns.Clear();
			SetColumns();
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

		private void CheatListView_ColumnClick(object sender, InputRoll.ColumnClickEventArgs e)
		{
			var column = e.Column;
			if (column.Name != _sortedColumn)
			{
				_sortReverse = false;
			}

			Global.CheatList.Sort(column.Name, _sortReverse);

			_sortedColumn = column.Name;
			_sortReverse ^= true;
			GeneralUpdate();
		}

		private void NewCheatForm_DragDrop(object sender, DragEventArgs e)
		{
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (Path.GetExtension(filePaths[0]) == ".cht")
			{
				LoadFile(new FileInfo(filePaths[0]), append: false);
				GeneralUpdate();
				UpdateMessageLabel();
			}
		}

		private void NewCheatForm_DragEnter(object sender, DragEventArgs e)
		{
			e.Set(DragDropEffects.Copy);
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
				Tools.Load<HexEditor>();

				if (selected.Select(x => x.Domain).Distinct().Count() > 1)
				{
					ViewInHexEditor(selected[0].Domain, new List<long> { selected.First().Address ?? 0 }, selected.First().Size);
				}
				else
				{
					ViewInHexEditor(selected.First().Domain, selected.Select(x => x.Address ?? 0), selected.First().Size);
				}
			}
		}

		protected override void OnShown(EventArgs e)
		{
			RefreshFloatingWindowControl(Settings.FloatingWindow);
			base.OnShown(e);
		}





		public class CheatsSettings : ToolDialogSettings
		{
			public CheatsSettings()
			{
				Columns = new List<RollColumn>
				{
					new RollColumn { Text = "Names", Name = NameColumn, Visible = true, UnscaledWidth = 128, Type = ColumnType.Text },
					new RollColumn { Text = "Address", Name = AddressColumn, Visible = true, UnscaledWidth = 60, Type = ColumnType.Text },
					new RollColumn { Text = "Value", Name = ValueColumn, Visible = true, UnscaledWidth = 59, Type = ColumnType.Text },
					new RollColumn { Text = "Compare", Name = CompareColumn, Visible = true, UnscaledWidth = 63, Type = ColumnType.Text },
					new RollColumn { Text = "Compare Type", Name = ComparisonTypeColumn, Visible = true, UnscaledWidth = 98, Type = ColumnType.Text },
					new RollColumn { Text = "On", Name = OnColumn, Visible = false, UnscaledWidth = 28, Type = ColumnType.Text },
					new RollColumn { Text = "Size", Name = SizeColumn, Visible = true, UnscaledWidth = 55, Type = ColumnType.Text },
					new RollColumn { Text = "Endian", Name = EndianColumn, Visible = false, UnscaledWidth = 55, Type = ColumnType.Text },
					new RollColumn { Text = "Display Type", Name = TypeColumn, Visible = false, UnscaledWidth = 88, Type = ColumnType.Text }
				};
			}

			public List<RollColumn> Columns { get; set; }
		}
	}
}
