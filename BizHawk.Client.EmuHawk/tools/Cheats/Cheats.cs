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
using BizHawk.Client.EmuHawk.WinFormExtensions;

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
			CheatListView.VirtualMode = true;

			_sortedColumn = "";
			_sortReverse = false;
		}

		[RequiredService]
		private IMemoryDomains Core { get; set; }

		[ConfigPersist]
		public CheatsSettings Settings { get; set; }

		public bool UpdateBefore => false;

		public void NewUpdate(ToolFormUpdateType type) { }

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
			CheatEditor.MemoryDomains = Core;
			CheatEditor.Restart();
		}

		/// <summary>
		/// Tools that want to refresh the cheats list should call this, not UpdateValues
		/// </summary>
		public void UpdateDialog()
		{
			CheatListView.ItemCount = Global.CheatList.Count;
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
					Global.Config.RecentCheats.HandleLoadError(path);
				}
				else
				{
					Global.Config.RecentCheats.Add(path);
					UpdateDialog();
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

		public bool AskSaveChanges()
		{
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
			var file = SaveFileDialog(
				Global.CheatList.CurrentFileName,
				PathManager.GetCheatsPath(Global.Game),
				"Cheat Files",
				"cht");

			return file != null && Global.CheatList.SaveFile(file.FullName);
		}

		private void Cheats_Load(object sender, EventArgs e)
		{
			TopMost = Settings.TopMost;
			CheatEditor.MemoryDomains = Core;
			LoadConfigSettings();
			ToggleGameGenieButton();
			CheatEditor.SetAddEvent(AddCheat);
			CheatEditor.SetEditEvent(EditCheat);
			UpdateDialog();

			CheatsMenu.Items.Add(Settings.Columns.GenerateColumnsMenu(ColumnToggleCallback));
		}

		private void ColumnToggleCallback()
		{
			SaveColumnInfo(CheatListView, Settings.Columns);
			LoadColumnInfo(CheatListView, Settings.Columns);
		}

		private void ToggleGameGenieButton()
		{
			GameGenieToolbarSeparator.Visible =
				LoadGameGenieToolbarItem.Visible =
				GlobalWin.Tools.IsAvailable<GameShark>();
		}

		private void AddCheat()
		{
			Global.CheatList.Add(CheatEditor.GetCheat());
			UpdateDialog();
			UpdateMessageLabel();
		}

		private void EditCheat()
		{
			var newCheat = CheatEditor.GetCheat();

			if (!newCheat.IsSeparator) // If a separator comes from the cheat editor something must have been invalid
			{
				Global.CheatList.Exchange(CheatEditor.OriginalCheat, newCheat);
				UpdateDialog();
				UpdateMessageLabel();
			}
		}

		private void SaveConfigSettings()
		{
			SaveColumnInfo(CheatListView, Settings.Columns);

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

			LoadColumnInfo(CheatListView, Settings.Columns);
		}

		private void CheatListView_QueryItemText(int index, int column, out string text)
		{
			text = "";
			if (index >= Global.CheatList.Count || Global.CheatList[index].IsSeparator)
			{
				return;
			}

			var columnName = CheatListView.Columns[column].Name;

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
					switch (Global.CheatList[index].ComparisonType)
					{
						case Cheat.CompareType.None:
							text = "";
							break;
						case Cheat.CompareType.Equal:
							text = "=";
							break;
						case Cheat.CompareType.GreaterThan:
							text = ">";
							break;
						case Cheat.CompareType.GreaterThanOrEqual:
							text = ">=";
							break;
						case Cheat.CompareType.LessThan:
							text = "<";
							break;
						case Cheat.CompareType.LessThanOrEqual:
							text = "<=";
							break;
						case Cheat.CompareType.NotEqual:
							text = "!=";
							break;
						default:
							text = "";
							break;
					}
					
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

		private IEnumerable<int> SelectedIndices => CheatListView.SelectedIndices.Cast<int>();

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
					CheatGroupBox.Text = $"Editing Cheat {cheat.Name} - {cheat.AddressStr}";
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
			var file = OpenFileDialog(
				Global.CheatList.CurrentFileName,
				PathManager.GetCheatsPath(Global.Game),
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
				GlobalWin.Tools.IsAvailable<GameShark>();
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

			var newindices = indices.Select(t => t - 1);

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

			var newindices = indices.Select(t => t + 1);

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

			CheatsMenu.Items.Add(Settings.Columns.GenerateColumnsMenu(ColumnToggleCallback));

			Global.Config.DisableCheatsOnLoad = false;
			Global.Config.LoadCheatFileByGame = true;
			Global.Config.CheatsAutoSaveOnClose = true;

			RefreshFloatingWindowControl(Settings.FloatingWindow);
			LoadColumnInfo(CheatListView, Settings.Columns);
		}

		#endregion

		#region ListView and Dialog Events

		private void CheatListView_Click(object sender, EventArgs e)
		{
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

		#endregion

		#endregion

		public class CheatsSettings : ToolDialogSettings
		{
			public CheatsSettings()
			{
				Columns = new ColumnList
				{
					new Column { Name = NameColumn, Visible = true, Index = 0, Width = 128 },
					new Column { Name = AddressColumn, Visible = true, Index = 1, Width = 60 },
					new Column { Name = ValueColumn, Visible = true, Index = 2, Width = 59 },
					new Column { Name = CompareColumn, Visible = true, Index = 3, Width = 59 },
					new Column { Name = ComparisonTypeColumn, Visible = true, Index = 4, Width = 60 },
					new Column { Name = OnColumn, Visible = false, Index = 5, Width = 28 },
					new Column { Name = DomainColumn, Visible = true, Index = 6, Width = 55 },
					new Column { Name = SizeColumn, Visible = true, Index = 7, Width = 55 },
					new Column { Name = EndianColumn, Visible = false, Index = 8, Width = 55 },
					new Column { Name = TypeColumn, Visible = false, Index = 9, Width = 55 }
				};
			}

			public ColumnList Columns { get; set; }
		}
	}
}
