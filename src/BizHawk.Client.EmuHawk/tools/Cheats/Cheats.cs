using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class Cheats : ToolFormBase, IToolFormAutoConfig
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

		private static readonly FilesystemFilterSet CheatsFSFilterSet = new(new FilesystemFilter("Cheat Files", new[] { "cht" }));

		public static Icon ToolIcon
			=> Resources.FreezeIcon;

		private string _sortedColumn;
		private bool _sortReverse;

		protected override string WindowTitleStatic => "Cheats";

		public Cheats()
		{
			InitializeComponent();
			Icon = ToolIcon;
			ToggleContextMenuItem.Image = Resources.Refresh;
			RemoveContextMenuItem.Image = Resources.Delete;
			DisableAllContextMenuItem.Image = Resources.Stop;
			NewMenuItem.Image = Resources.NewFile;
			OpenMenuItem.Image = Resources.OpenFile;
			SaveMenuItem.Image = Resources.SaveAs;
			RecentSubMenu.Image = Resources.Recent;
			RemoveCheatMenuItem.Image = Resources.Delete;
			InsertSeparatorMenuItem.Image = Resources.InsertSeparator;
			MoveUpMenuItem.Image = Resources.MoveUp;
			MoveDownMenuItem.Image = Resources.MoveDown;
			ToggleMenuItem.Image = Resources.Refresh;
			DisableAllCheatsMenuItem.Image = Resources.Stop;
			NewToolBarItem.Image = Resources.NewFile;
			OpenToolBarItem.Image = Resources.OpenFile;
			SaveToolBarItem.Image = Resources.SaveAs;
			RemoveToolbarItem.Image = Resources.Delete;
			SeparatorToolbarItem.Image = Resources.InsertSeparator;
			MoveUpToolbarItem.Image = Resources.MoveUp;
			MoveDownToolbarItem.Image = Resources.MoveDown;
			LoadGameGenieToolbarItem.Image = Resources.Placeholder;
			Settings = new CheatsSettings();

			Closing += (o, e) =>
			{
				Settings.Columns = CheatListView.AllColumns;
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

		public override void Restart()
		{
			CheatEditor.MemoryDomains = Core;
			CheatEditor.Restart();
		}

		/// <summary>
		/// Tools that want to refresh the cheats list should call this, not UpdateValues
		/// </summary>
		protected override void GeneralUpdate()
		{
			CheatListView.RowCount = MainForm.CheatList.Count;
			TotalLabel.Text = $"{MainForm.CheatList.CheatCount} {(MainForm.CheatList.CheatCount == 1 ? "cheat" : "cheats")} {MainForm.CheatList.ActiveCount} active";
		}

		private void LoadFileFromRecent(string path)
		{
			var askResult = !MainForm.CheatList.Changes || AskSaveChanges();
			if (askResult)
			{
				var loadResult = MainForm.CheatList.Load(Core, path, append: false);
				if (!loadResult)
				{
					Config.Cheats.Recent.HandleLoadError(MainForm, path);
				}
				else
				{
					GeneralUpdate();
					UpdateMessageLabel();
				}
			}
		}

		private void UpdateMessageLabel(bool saved = false)
		{
			MessageLabel.Text = saved
				? $"{Path.GetFileName(MainForm.CheatList.CurrentFileName)} saved."
				: MainForm.CheatList.Changes
					? $"{Path.GetFileName(MainForm.CheatList.CurrentFileName)} *"
					: Path.GetFileName(MainForm.CheatList.CurrentFileName);
		}

		private void LoadFile(FileSystemInfo file, bool append)
		{
			if (file != null)
			{
				var result = true;
				if (MainForm.CheatList.Changes)
				{
					result = AskSaveChanges();
				}

				if (result)
				{
					MainForm.CheatList.Load(Core, file.FullName, append);
					GeneralUpdate();
					UpdateMessageLabel();
				}
			}
		}

		private bool SaveAs()
		{
			var fileName = MainForm.CheatList.CurrentFileName;
			if (string.IsNullOrWhiteSpace(fileName))
			{
				fileName = Game.FilesystemSafeName();
			}

			var file = SaveFileDialog(
				currentFile: fileName,
				path: Config!.PathEntries.CheatsAbsolutePath(Game.System),
				CheatsFSFilterSet,
				this);

			return file != null && MainForm.CheatList.SaveFile(file.FullName);
		}

		private void Cheats_Load(object sender, EventArgs e)
		{
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
			CheatListView.AllColumns.AddRange(Settings.Columns);
			CheatListView.Refresh();
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
			MainForm.CheatList.Add(CheatEditor.GetCheat());
			GeneralUpdate();
			UpdateMessageLabel();
		}

		private void EditCheat()
		{
			var newCheat = CheatEditor.GetCheat();

			if (!newCheat.IsSeparator) // If a separator comes from the cheat editor something must have been invalid
			{
				MainForm.CheatList.Exchange(CheatEditor.OriginalCheat, newCheat);
				GeneralUpdate();
				UpdateMessageLabel();
			}
		}

		private void LoadConfigSettings()
		{
			CheatListView.AllColumns.Clear();
			SetColumns();
		}

		private void CheatListView_QueryItemText(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			text = "";
			if (index >= MainForm.CheatList.Count || MainForm.CheatList[index].IsSeparator)
			{
				return;
			}

			var columnName = column.Name;

			switch (columnName)
			{
				case NameColumn:
					text = MainForm.CheatList[index].Name;
					break;
				case AddressColumn:
					text = MainForm.CheatList[index].AddressStr;
					break;
				case ValueColumn:
					text = MainForm.CheatList[index].ValueStr;
					break;
				case CompareColumn:
					text = MainForm.CheatList[index].CompareStr;
					break;
				case OnColumn:
					text = MainForm.CheatList[index].Enabled ? "*" : "";
					break;
				case DomainColumn:
					text = MainForm.CheatList[index].Domain.Name;
					break;
				case SizeColumn:
					text = MainForm.CheatList[index].Size.ToString();
					break;
				case EndianColumn:
					text = (MainForm.CheatList[index].BigEndian ?? false) ? "Big" : "Little";
					break;
				case TypeColumn:
					text = Watch.DisplayTypeToString(MainForm.CheatList[index].Type);
					break;
				case ComparisonTypeColumn:
					text = MainForm.CheatList[index].ComparisonType switch
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
			if (index < MainForm.CheatList.Count)
			{
				if (MainForm.CheatList[index].IsSeparator)
				{
					color = BackColor;
				}
				else if (MainForm.CheatList[index].Enabled)
				{
					color = Color.LightCyan;
				}
			}
		}

		private IEnumerable<int> SelectedIndices => CheatListView.SelectedRows;

		private IEnumerable<Cheat> SelectedItems => SelectedIndices.Select(index => MainForm.CheatList[index]);

		private IEnumerable<Cheat> SelectedCheats => SelectedItems.Where(x => !x.IsSeparator);

		private void DoSelectedIndexChange()
		{
			var selected = SelectedCheats.Take(2).ToList(); // is this saving that much overhead by not enumerating the whole selection? could display the row count if we did
			if (selected.Count is 0)
			{
				CheatEditor.ClearForm();
				CheatGroupBox.Text = "New Cheat";
				CheatGroupBox.Enabled = true;
			}
			else if (selected.Count is 1)
			{
				CheatEditor.SetCheat(selected[0]);
				CheatGroupBox.Text = $"Editing Cheat {selected[0].Name} - {selected[0].AddressStr}";
				CheatGroupBox.Enabled = true;
			}
			else
			{
				CheatGroupBox.Enabled = false;
				CheatEditor.ClearForm();
				CheatGroupBox.Text = "Multiple Cheats Selected";
			}
		}

		private void StartNewList()
		{
			var result = !MainForm.CheatList.Changes || AskSaveChanges();
			if (result)
			{
				MainForm.CheatList.NewList(Tools.GenerateDefaultCheatFilename());
				GeneralUpdate();
				UpdateMessageLabel();
				ToggleGameGenieButton();
			}
		}

		private void NewList()
		{
			var result = !MainForm.CheatList.Changes || AskSaveChanges();
			if (result)
			{
				StartNewList();
			}
		}

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveMenuItem.Enabled = MainForm.CheatList.Changes;
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
			=> RecentSubMenu.ReplaceDropDownItems(Config!.Cheats.Recent.RecentMenu(this, LoadFileFromRecent, "Cheats"));

		private void NewMenuItem_Click(object sender, EventArgs e)
		{
			NewList();
		}

		private void OpenMenuItem_Click(object sender, EventArgs e)
		{
			var file = OpenFileDialog(
				currentFile: MainForm.CheatList.CurrentFileName,
				path: Config!.PathEntries.CheatsAbsolutePath(Game.System),
				CheatsFSFilterSet);

			LoadFile(file, append: sender == AppendMenuItem);
		}

		private void SaveMenuItem_Click(object sender, EventArgs e)
		{
			if (MainForm.CheatList.Changes)
			{
				if (MainForm.CheatList.Save())
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

		private void CheatsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RemoveCheatMenuItem.Enabled =
				MoveUpMenuItem.Enabled =
				MoveDownMenuItem.Enabled =
				ToggleMenuItem.Enabled =
					CheatListView.AnyRowsSelected;

#if false // Always leave enabled even if no cheats enabled. This way the hotkey will always work, even if a new cheat is enabled without also refreshing the menu
			DisableAllCheatsMenuItem.Enabled = MainForm.CheatList.AnyActive;
#endif

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
					MainForm.CheatList.Remove(item);
				}

				CheatListView.DeselectAll();
				GeneralUpdate();
			}
		}

		private void InsertSeparatorMenuItem_Click(object sender, EventArgs e)
		{
			MainForm.CheatList.Insert(CheatListView.SelectionStartIndex ?? MainForm.CheatList.Count, Cheat.Separator);
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
				var cheat = MainForm.CheatList[index];
				MainForm.CheatList.Remove(cheat);
				MainForm.CheatList.Insert(index - 1, cheat);
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
			if (indices.Count == 0
				|| indices[indices.Count - 1] == MainForm.CheatList.Count - 1) // at end already
			{
				return;
			}

			for (var i = indices.Count - 1; i >= 0; i--)
			{
				var cheat = MainForm.CheatList[indices[i]];
				MainForm.CheatList.Remove(cheat);
				MainForm.CheatList.Insert(indices[i] + 1, cheat);
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
			=> CheatListView.ToggleSelectAll();

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
			MainForm.CheatList.DisableAll();
		}

		private void OpenGameGenieEncoderDecoderMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<GameShark>();
		}

		private void SettingsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AlwaysLoadCheatsMenuItem.Checked = Config.Cheats.LoadFileByGame;
			AutoSaveCheatsMenuItem.Checked = Config.Cheats.AutoSaveOnClose;
			DisableCheatsOnLoadMenuItem.Checked = Config.Cheats.DisableOnLoad;
		}

		private void AlwaysLoadCheatsMenuItem_Click(object sender, EventArgs e)
			=> Config.Cheats.LoadFileByGame = !Config.Cheats.LoadFileByGame;

		private void AutoSaveCheatsMenuItem_Click(object sender, EventArgs e)
			=> Config.Cheats.AutoSaveOnClose = !Config.Cheats.AutoSaveOnClose;

		private void CheatsOnOffLoadMenuItem_Click(object sender, EventArgs e)
			=> Config.Cheats.DisableOnLoad = !Config.Cheats.DisableOnLoad;

		[RestoreDefaults]
		private void RestoreDefaults()
		{
			Settings = new CheatsSettings();

			CheatsMenu.Items.Remove(
				CheatsMenu.Items
					.OfType<ToolStripMenuItem>()
					.First(x => x.Name == "GeneratedColumnsSubMenu"));

			CheatsMenu.Items.Add(CheatListView.ToColumnsMenu(ColumnToggleCallback));

			Config.Cheats.DisableOnLoad = false;
			Config.Cheats.LoadFileByGame = true;
			Config.Cheats.AutoSaveOnClose = true;

			CheatListView.AllColumns.Clear();
			SetColumns();
		}

		private void CheatListView_DoubleClick(object sender, EventArgs e)
		{
			ToggleMenuItem_Click(sender, e);
		}

		private void CheatListView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.IsPressed(Keys.Delete))
			{
				RemoveCheatMenuItem_Click(sender, e);
			}
			else if (e.IsCtrl(Keys.A))
			{
				SelectAllMenuItem_Click(null, null);
			}

			DoSelectedIndexChange();
		}

		private void CheatListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			DoSelectedIndexChange();
		}

		private void CheatListView_ColumnClick(object sender, InputRoll.ColumnClickEventArgs e)
		{
			var column = e.Column!;
			if (column.Name != _sortedColumn)
			{
				_sortReverse = false;
			}

			MainForm.CheatList.Sort(column.Name, _sortReverse);

			_sortedColumn = column.Name;
			_sortReverse = !_sortReverse;
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

			DisableAllContextMenuItem.Enabled = MainForm.CheatList.AnyActive;
		}

		private void ViewInHexEditorContextMenuItem_Click(object sender, EventArgs e)
		{
			var selected = SelectedCheats.ToList();
			if (selected.Any())
			{
				Tools.Load<HexEditor>();

				ViewInHexEditor(
					selected[0].Domain,
					selected.Select(static x => x.Domain).Distinct().CountIsAtLeast(2)
						? new[] { selected[0].Address ?? 0 }
						: selected.Select(static x => x.Address ?? 0),
					selected[0].Size);
			}
		}

		public class CheatsSettings
		{
			public CheatsSettings()
			{
				Columns = new List<RollColumn>
				{
					new(name: NameColumn, widthUnscaled: 128, text: "Names"),
					new(name: AddressColumn, widthUnscaled: 60, text: "Address"),
					new(name: ValueColumn, widthUnscaled: 59, text: "Value"),
					new(name: CompareColumn, widthUnscaled: 63, text: "Compare"),
					new(name: ComparisonTypeColumn, widthUnscaled: 98, text: "Compare Type"),
					new(name: OnColumn, widthUnscaled: 28, text: "On") { Visible = false },
					new(name: SizeColumn, widthUnscaled: 55, text: "Size"),
					new(name: EndianColumn, widthUnscaled: 55, text: "Endian") { Visible = false },
					new(name: TypeColumn, widthUnscaled: 88, text: "Display Type") { Visible = false },
				};
			}

			public List<RollColumn> Columns { get; set; }
		}
	}
}
