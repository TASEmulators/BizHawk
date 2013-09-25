using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Globalization;

namespace BizHawk.MultiClient
{
	/// <summary>
	/// A winform designed to search through ram values
	/// </summary>
	public partial class NewRamSearch : Form
	{
		public const string ADDRESS = "AddressColumn";
		public const string VALUE = "ValueColumn";
		public const string PREV = "PrevColumn";
		public const string CHANGES = "ChangesColumn";
		public const string DIFF = "DiffColumn";

		private readonly Dictionary<string, int> DefaultColumnWidths = new Dictionary<string, int>
		{
			{ ADDRESS, 60 },
			{ VALUE, 59 },
			{ PREV, 59 },
			{ CHANGES, 55 },
			{ DIFF, 59 },
		};

		private string CurrentFileName = String.Empty;

		private RamSearchEngine Searches;
		private RamSearchEngine.Settings Settings;

		private int defaultWidth;       //For saving the default size of the dialog, so the user can restore if desired
		private int defaultHeight;
		private string _sortedColumn = "";
		private bool _sortReverse = false;
		private bool forcePreviewClear = false;
		private bool autoSearch = false;

		#region Initialize, Load, and Save
		
		public NewRamSearch()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			InitializeComponent();
			WatchListView.QueryItemText += ListView_QueryItemText;
			WatchListView.QueryItemBkColor += ListView_QueryItemBkColor;
			WatchListView.VirtualMode = true;
			Closing += (o, e) => SaveConfigSettings();

			_sortedColumn = "";
			_sortReverse = false;

			Settings = new RamSearchEngine.Settings();
			Searches = new RamSearchEngine(Settings);

			TopMost = Global.Config.RamSearchAlwaysOnTop;
		}

		private void RamSearch_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
			SpecificValueBox.ByteSize = Settings.Size;
			SpecificValueBox.Type = Settings.Type;
			MessageLabel.Text = String.Empty;
		}

		private void ListView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (Searches.Count > 0 && column == 0)
			{
				Color nextColor = Color.White;

				bool isCheat = Global.CheatList.IsActiveCheat(Settings.Domain, Searches[index].Address.Value);
				bool isWeeded = Global.Config.RamSearchPreviewMode && Searches.Preview(Searches[index].Address.Value) && !forcePreviewClear;

				if (isCheat)
				{
					if (isWeeded)
					{
						nextColor = Color.Lavender;
					}
					else
					{
						nextColor = Color.LightCyan;
					}
				}
				else
				{
					if (isWeeded)
					{
						nextColor = Color.Pink;
					}
				}

				if (color != nextColor)
				{
					color = nextColor;
				}
			}
		}

		private void ListView_QueryItemText(int index, int column, out string text)
		{
			text = "";

			if (index >= Searches.Count)
			{
				return;
			}

			string columnName = WatchListView.Columns[column].Name;
			switch (columnName)
			{
				case ADDRESS:
					text = Searches[index].AddressString;
					break;
				case VALUE:
					text = Searches[index].ValueString;
					break;
				case PREV:
					text = Searches[index].PreviousStr;
					break;
				case CHANGES:
					if (Searches[index] is IWatchDetails)
					{
						text = (Searches[index] as IWatchDetails).ChangeCount.ToString();
					}
					break;
				case DIFF:
					if (Searches[index] is IWatchDetails)
					{
						text = (Searches[index] as IWatchDetails).Diff;
					}
					break;
			}
		}

		private void LoadConfigSettings()
		{
			//Size and Positioning
			defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = Size.Height;

			if (Global.Config.RamSearchSaveWindowPosition && Global.Config.RamSearchWndx >= 0 && Global.Config.RamSearchWndy >= 0)
			{
				Location = new Point(Global.Config.RamSearchWndx, Global.Config.RamSearchWndy);
			}

			if (Global.Config.RamSearchWidth >= 0 && Global.Config.RamSearchHeight >= 0)
			{
				Size = new Size(Global.Config.RamSearchWidth, Global.Config.RamSearchHeight);
			}

			TopMost = Global.Config.RamSearchAlwaysOnTop;

			LoadColumnInfo();
		}

		#endregion

		#region Public

		public void UpdateValues()
		{
			//TODO: autosearch logic
			if (Searches.Count > 0)
			{
				Searches.Update();
				WatchListView.Refresh();
			}
		}

		public void Restart()
		{
			//TODO
			if (!IsHandleCreated || IsDisposed) return;
		}

		public void SaveConfigSettings()
		{
			//TODO: columns

			Global.Config.RamSearchWndx = Location.X;
			Global.Config.RamSearchWndy = Location.Y;
			Global.Config.RamSearchWidth = Right - Left;
			Global.Config.RamSearchHeight = Bottom - Top;
		}

		#endregion

		#region Private

		private void NewSearch()
		{
			Searches = new RamSearchEngine(Settings);
			Searches.Start();
			if (Global.Config.RamSearchAlwaysExcludeRamWatch)
			{
				RemoveRamWatchesFromList();
			}

			SetTotal();
			WatchListView.ItemCount = Searches.Count;
			ToggleSearchDependentToolBarItems();
		}

		private void ToggleSearchDependentToolBarItems()
		{
			DoSearchToolButton.Enabled =
				CopyValueToPrevToolBarItem.Enabled =
				Searches.Count > 0;
			UpdateUndoToolBarButtons();
		}

		private void DoSearch()
		{
			int removed = Searches.DoSearch();
			SetTotal();
			WatchListView.ItemCount = Searches.Count;
			SetRemovedMessage(removed);
			ToggleSearchDependentToolBarItems();
		}

		private List<int> SelectedIndices
		{
			get
			{
				var indices = new List<int>();
				foreach (int index in WatchListView.SelectedIndices)
				{
					indices.Add(index);
				}
				return indices;
			}
		}

		private void SetRemovedMessage(int val)
		{
			MessageLabel.Text = val.ToString() + " address" + (val != 1 ? "es" : String.Empty) + " removed";
		}

		private void SetTotal()
		{
			TotalSearchLabel.Text = String.Format("{0:n0}", Searches.Count) + " addresses";
		}

		private void SetDomainLabel()
		{
			MemDomainLabel.Text = Searches.DomainName;
		}

		private void LoadFileFromRecent(string path)
		{
			FileInfo file = new FileInfo(path);

			if (!file.Exists)
			{
				Global.Config.RecentSearches.HandleLoadError(path);
			}
			else
			{
				LoadWatchFile(file, append: false);
			}
		}

		private void SetPlatformAndMemoryDomainLabel()
		{
			MemDomainLabel.Text = Global.Emulator.SystemId + " " + Searches.DomainName;
		}

		private void SetMemoryDomain(int pos)
		{
			if (pos < Global.Emulator.MemoryDomains.Count)  //Sanity check
			{
				Settings.Domain = Global.Emulator.MemoryDomains[pos];
				SetDomainLabel();
			}
		}

		private void LoadColumnInfo()
		{
			WatchListView.Columns.Clear();
			AddColumn(ADDRESS, true); //TODO: make things configurable
			AddColumn(VALUE, true);
			AddColumn(PREV, true);
			AddColumn(CHANGES, true);
			AddColumn(DIFF, true);

			//ColumnPositions(); //TODO
		}

		private void AddColumn(string columnName, bool enabled)
		{
			if (enabled)
			{
				if (WatchListView.Columns[columnName] == null)
				{
					ColumnHeader column = new ColumnHeader
					{
						Name = columnName,
						Text = columnName.Replace("Column", ""),
						Width = 50, //TODO: GetColumnWidth(columnName),
					};
					
					WatchListView.Columns.Add(column);
				}
			}
		}

		private void DoDisplayTypeClick(Watch.DisplayType type)
		{
			SpecificValueBox.Type = Settings.Type = type;
			Searches.SetType(type);
		}

		private void SetPreviousStype(Watch.PreviousType type)
		{
			Settings.PreviousType = type;
			Searches.SetPreviousType(type);
		}

		private void SetSize(Watch.WatchSize size)
		{
			SpecificValueBox.ByteSize = Settings.Size = size;
		}

		private void SetComparisonOperator(RamSearchEngine.ComparisonOperator op)
		{
			Searches.Operator = op;
		}

		private void SetCompareTo(RamSearchEngine.Compare comp)
		{
			Searches.CompareTo = comp;
		}

		private void SetCompareValue(int? value)
		{
			Searches.CompareValue = value;
		}

		private void SetToDetailedMode()
		{
			Settings.Mode = RamSearchEngine.Settings.SearchMode.Detailed;
			NumberOfChangesRadio.Enabled = true;
			NumberOfChangesBox.Enabled = true;
			DifferenceRadio.Enabled = true;
			DifferentByBox.Enabled = true;
			ClearChangeCountsToolBarItem.Enabled = true;
		}

		private void SetToFastMode()
		{
			Settings.Mode = RamSearchEngine.Settings.SearchMode.Fast;

			if (Settings.PreviousType == Watch.PreviousType.LastFrame || Settings.PreviousType == Watch.PreviousType.LastChange)
			{
				SetPreviousStype(Watch.PreviousType.LastSearch);
			}

			NumberOfChangesRadio.Enabled = false;
			NumberOfChangesBox.Enabled = false;
			NumberOfChangesBox.Text = String.Empty;
			ClearChangeCountsToolBarItem.Enabled = false;

			if (NumberOfChangesRadio.Checked || DifferenceRadio.Checked)
			{
				PreviousValueRadio.Checked = true;
			}
		}

		private void RemoveAddresses()
		{
			if (SelectedIndices.Count > 0)
			{
				SetRemovedMessage(SelectedIndices.Count);

				var addresses = new List<int>();
				foreach (int index in SelectedIndices)
				{
					addresses.Add(Searches[index].Address.Value);
				}
				Searches.RemoveRange(addresses);

				WatchListView.ItemCount = Searches.Count;
				SetTotal();
				WatchListView.SelectedIndices.Clear();
			}
		}

		public void LoadWatchFile(FileInfo file, bool append, bool truncate = false)
		{
			if (file != null)
			{
				if (!truncate)
				{
					CurrentFileName = file.FullName;
				}

				WatchList watches = new WatchList(Settings.Domain);
				watches.Load(file.FullName, false, append);
				List<int> addresses = watches.Where(x => !x.IsSeparator).Select(x => x.Address.Value).ToList();

				if (truncate)
				{
					SetRemovedMessage(addresses.Count);
					Searches.RemoveRange(addresses);
				}
				else
				{
					Searches.AddRange(addresses, append);
					MessageLabel.Text = file.Name + " loaded";
				}

				WatchListView.ItemCount = Searches.Count;
				SetTotal();
				Global.Config.RecentSearches.Add(file.FullName);
			}
		}

		private void AddToRamWatch()
		{
			if (SelectedIndices.Count > 0)
			{
				Global.MainForm.LoadRamWatch(true);
				for (int x = 0; x < SelectedIndices.Count; x++)
				{
					Global.MainForm.NewRamWatch1.AddWatch(Searches[SelectedIndices[x]]);
				}

				if (Global.Config.RamSearchAlwaysExcludeRamWatch)
				{
					RemoveRamWatchesFromList();
				}
			}
		}

		private Point GetPromptPoint()
		{
			return PointToScreen(new Point(WatchListView.Location.X, WatchListView.Location.Y));
		}

		private void PokeAddress()
		{
			if (SelectedIndices.Count > 0)
			{
				Global.Sound.StopSound();
				var poke = new RamPoke();

				var watches = new List<Watch>();
				for (int i = 0; i < SelectedIndices.Count; i++)
				{
					watches.Add(Searches[SelectedIndices[i]]);
				}

				poke.SetWatch(watches);
				poke.InitialLocation = GetPromptPoint();
				poke.ShowDialog();
				UpdateValues();
				Global.Sound.StartSound();
			}
		}

		private List<Watch> SelectedWatches
		{
			get
			{
				var selected = new List<Watch>();
				ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
				if (indexes.Count > 0)
				{
					foreach (int index in indexes)
					{
						if (!Searches[index].IsSeparator)
						{
							selected.Add(Searches[index]);
						}
					}
				}
				return selected;
			}
		}

		private void FreezeAddress()
		{
			ToolHelpers.FreezeAddress(SelectedWatches);
		}

		private void RemoveRamWatchesFromList()
		{
			Searches.RemoveRange(Global.MainForm.NewRamWatch1.AddressList);
			SetTotal();
		}

		private void UpdateUndoToolBarButtons()
		{
			UndoToolBarButton.Enabled = Searches.CanUndo;
			RedoToolBarItem.Enabled = Searches.CanRedo;
		}

		#endregion

		#region Winform Events

		#region File
		
		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveMenuItem.Enabled = !String.IsNullOrWhiteSpace(CurrentFileName);
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(Global.Config.RecentSearches.GenerateRecentMenu(LoadFileFromRecent));
		}

		private void OpenMenuItem_Click(object sender, EventArgs e)
		{
			LoadWatchFile(
				WatchList.GetFileFromUser(String.Empty),
				sender == AppendFileMenuItem,
				sender == TruncateFromFileMenuItem
				);
		}

		private void SaveMenuItem_Click(object sender, EventArgs e)
		{
			if (!String.IsNullOrWhiteSpace(CurrentFileName))
			{
				WatchList watches = new WatchList(Settings.Domain);
				watches.CurrentFileName = CurrentFileName;
				for (int i = 0; i < Searches.Count; i++)
				{
					watches.Add(Searches[i]);
				}

				if (watches.Save())
				{
					CurrentFileName = watches.CurrentFileName;
					MessageLabel.Text = Path.GetFileName(CurrentFileName) + " saved";
				}
			}
		}

		private void SaveAsMenuItem_Click(object sender, EventArgs e)
		{
			WatchList watches = new WatchList(Settings.Domain);
			watches.CurrentFileName = CurrentFileName;
			for(int i = 0; i < Searches.Count; i++)
			{
				watches.Add(Searches[i]);
			}

			if (watches.SaveAs())
			{
				CurrentFileName = watches.CurrentFileName;
				MessageLabel.Text = Path.GetFileName(CurrentFileName) + " saved";
				Global.Config.RecentSearches.Add(watches.CurrentFileName);
			}
		}

		private void CloseMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}
		
		#endregion

		#region Settings

		private void SettingsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			CheckMisalignedMenuItem.Checked = Settings.CheckMisAligned;
			BigEndianMenuItem.Checked = Settings.BigEndian;
		}

		private void ModeSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DetailedMenuItem.Checked = Settings.Mode == RamSearchEngine.Settings.SearchMode.Detailed;
			FastMenuItem.Checked = Settings.Mode == RamSearchEngine.Settings.SearchMode.Fast;
		}

		private void MemoryDomainsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			MemoryDomainsSubMenu.DropDownItems.Clear();
			MemoryDomainsSubMenu.DropDownItems.AddRange(ToolHelpers.GenerateMemoryDomainMenuItems(SetMemoryDomain, Searches.DomainName));
		}

		private void SizeSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			_1ByteMenuItem.Checked = Settings.Size == Watch.WatchSize.Byte;
			_2ByteMenuItem.Checked = Settings.Size == Watch.WatchSize.Word;
			_4ByteMenuItem.Checked = Settings.Size == Watch.WatchSize.DWord;
		}

		private void DisplayTypeSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DisplayTypeSubMenu.DropDownItems.Clear();

			foreach (var type in Watch.AvailableTypes(Settings.Size))
			{
				var item = new ToolStripMenuItem()
				{
					Name = type.ToString() + "ToolStripMenuItem",
					Text = Watch.DisplayTypeToString(type),
					Checked = Settings.Type == type,
				};
				item.Click += (o, ev) => DoDisplayTypeClick(type);

				DisplayTypeSubMenu.DropDownItems.Add(item);
			}
		}

		private void DefinePreviousValueSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			Previous_LastSearchMenuItem.Checked = false;
			Previous_LastChangeMenuItem.Checked = false;
			PreviousFrameMenuItem.Checked = false;
			Previous_OriginalMenuItem.Checked = false;

			switch (Settings.PreviousType)
			{
				default:
				case Watch.PreviousType.LastSearch:
					Previous_LastSearchMenuItem.Checked = true;
					break;
				case Watch.PreviousType.LastChange:
					Previous_LastChangeMenuItem.Checked = true;
					break;
				case Watch.PreviousType.LastFrame:
					PreviousFrameMenuItem.Checked = true;
					break;
				case Watch.PreviousType.Original:
					Previous_OriginalMenuItem.Checked = true;
					break;
			}

			if (Settings.Mode == RamSearchEngine.Settings.SearchMode.Fast)
			{
				Previous_LastChangeMenuItem.Enabled = false;
				PreviousFrameMenuItem.Enabled = false;
			}
			else
			{
				Previous_LastChangeMenuItem.Enabled = true;
				PreviousFrameMenuItem.Enabled = true;
			}
		}

		private void DetailedMenuItem_Click(object sender, EventArgs e)
		{
			SetToDetailedMode();
		}

		private void FastMenuItem_Click(object sender, EventArgs e)
		{
			SetToFastMode();
		}

		private void _1ByteMenuItem_Click(object sender, EventArgs e)
		{
			SetSize(Watch.WatchSize.Byte);
		}

		private void _2ByteMenuItem_Click(object sender, EventArgs e)
		{
			SetSize(Watch.WatchSize.Word);
		}

		private void _4ByteMenuItem_Click(object sender, EventArgs e)
		{
			SetSize(Watch.WatchSize.DWord);
		}

		private void CheckMisalignedMenuItem_Click(object sender, EventArgs e)
		{
			Settings.CheckMisAligned = CheckMisalignedMenuItem.Checked;
		}

		private void Previous_LastFrameMenuItem_Click(object sender, EventArgs e)
		{
			SetPreviousStype(Watch.PreviousType.LastFrame);
		}

		private void Previous_LastSearchMenuItem_Click(object sender, EventArgs e)
		{
			SetPreviousStype(Watch.PreviousType.LastSearch);
		}

		private void Previous_LastChangeMenuItem_Click(object sender, EventArgs e)
		{
			SetPreviousStype(Watch.PreviousType.LastChange);
		}

		private void Previous_OriginalMenuItem_Click(object sender, EventArgs e)
		{
			SetPreviousStype(Watch.PreviousType.Original);
		}

		private void BigEndianMenuItem_Click(object sender, EventArgs e)
		{
			Settings.BigEndian = BigEndianMenuItem.Checked;
			Searches.SetEndian(BigEndianMenuItem.Checked);
		}

		#endregion

		#region Search

		private void SearchSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ClearChangeCountsMenuItem.Enabled = Settings.Mode == RamSearchEngine.Settings.SearchMode.Detailed;

			RemoveMenuItem.Enabled =
				AddToRamWatchMenuItem.Enabled =
				PokeAddressMenuItem.Enabled =
				FreezeAddressMenuItem.Enabled =
				SelectedIndices.Any();

			UndoMenuItem.Enabled =
				ClearUndoMenuItem.Enabled =
				Searches.CanUndo;

			RedoMenuItem.Enabled = Searches.CanRedo;
		}

		private void NewSearchMenuMenuItem_Click(object sender, EventArgs e)
		{
			NewSearch();
		}

		private void SearchMenuItem_Click(object sender, EventArgs e)
		{
			DoSearch();
		}

		private void UndoMenuItem_Click(object sender, EventArgs e)
		{
			if (Searches.CanUndo)
			{
				Searches.Undo();
				UpdateUndoToolBarButtons();
			}
		}

		private void RedoMenuItem_Click(object sender, EventArgs e)
		{
			if (Searches.CanRedo)
			{
				Searches.Redo();
				UpdateUndoToolBarButtons();
			}
		}

		private void CopyValueToPrevMenuItem_Click(object sender, EventArgs e)
		{
			Searches.SetPrevousToCurrent();
			WatchListView.Refresh();
		}

		private void ClearChangeCountsMenuItem_Click(object sender, EventArgs e)
		{
			Searches.ClearChangeCounts();
			WatchListView.Refresh();
		}

		private void RemoveMenuItem_Click(object sender, EventArgs e)
		{
			RemoveAddresses();
		}

		private void AddToRamWatchMenuItem_Click(object sender, EventArgs e)
		{
			AddToRamWatch();
		}

		private void PokeAddressMenuItem_Click(object sender, EventArgs e)
		{
			PokeAddress();
		}

		private void FreezeAddressMenuItem_Click(object sender, EventArgs e)
		{
			FreezeAddress();
		}

		private void ClearUndoMenuItem_Click(object sender, EventArgs e)
		{
			Searches.ClearHistory();
			UpdateUndoToolBarButtons();
		}

		#endregion

		#region Options

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AutoloadDialogMenuItem.Checked = Global.Config.RecentSearches.AutoLoad;
			SaveWinPositionMenuItem.Checked = Global.Config.RamSearchSaveWindowPosition;
			ExcludeRamWatchMenuItem.Checked = Global.Config.RamSearchAlwaysExcludeRamWatch;
			PreviewModeMenuItem.Checked = Global.Config.RamSearchPreviewMode;
			AlwaysOnTopMenuItem.Checked = Global.Config.RamSearchAlwaysOnTop;
			AutoSearchMenuItem.Checked = autoSearch;
		}

		private void PreviewModeMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchPreviewMode ^= true;
		}

		private void AutoSearchMenuItem_Click(object sender, EventArgs e)
		{
			autoSearch ^= true;
		}

		private void ExcludeRamWatchMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchAlwaysExcludeRamWatch ^= true;
			if (Global.Config.RamSearchAlwaysExcludeRamWatch)
			{
				RemoveRamWatchesFromList();
			}
		}

		private void AutoloadDialogMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RecentSearches.AutoLoad ^= true;
		}

		private void SaveWinPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchSaveWindowPosition ^= true;
		}

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchAlwaysOnTop ^= true;
			TopMost = Global.Config.RamSearchAlwaysOnTop;
		}

		private void RestoreDefaultsMenuItem_Click(object sender, EventArgs e)
		{
			//TODO: finish

			Global.Config.RamSearchAlwaysOnTop = TopMost = false;
			Size = new Size(defaultWidth, defaultHeight);
		}

		#endregion

		#region ContextMenu

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			DoSearchContextMenuItem.Enabled = Searches.Count > 0;
			
			RemoveContextMenuItem.Visible =
				AddToRamWatchContextMenuItem.Visible =
				PokeContextMenuItem.Visible =
				FreezeContextMenuItem.Visible =
				ContextMenuSeparator2.Visible =
				
				ViewInHexEditorContextMenuItem.Visible = 
				SelectedIndices.Count > 0;

			UnfreezeAllContextMenuItem.Visible = Global.CheatList.Any();

			ContextMenuSeparator3.Visible = (SelectedIndices.Count > 0) || (Global.CheatList.Any());

			bool allCheats = true;
			foreach (int index in SelectedIndices)
			{
				if (!Global.CheatList.IsActiveCheat(Settings.Domain, Searches[index].Address.Value))
				{
					allCheats = false;
				}
			}

			if (allCheats)
			{
				FreezeContextMenuItem.Text = "&Unfreeze address";
				FreezeContextMenuItem.Image = Properties.Resources.Unfreeze;
			}
			else
			{
				FreezeContextMenuItem.Text = "&Freeze address";
				FreezeContextMenuItem.Image = Properties.Resources.Freeze;
			}
		}

		private void UnfreezeAllContextMenuItem_Click(object sender, EventArgs e)
		{
			ToolHelpers.UnfreezeAll();
		}

		private void ViewInHexEditorContextMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedIndices.Count > 0)
			{
				Global.MainForm.LoadHexEditor();
				Global.MainForm.HexEditor1.SetDomain(Settings.Domain);
				Global.MainForm.HexEditor1.GoToAddress(Searches[SelectedIndices[0]].Address.Value);

				//TODO: secondary highlighted on remaining indexes
			}
		}

		private void ClearPreviewContextMenuItem_Click(object sender, EventArgs e)
		{
			forcePreviewClear = true;
			WatchListView.Refresh();
		}

		#endregion

		#region Compare To Box

		private void PreviousValueRadio_Click(object sender, EventArgs e)
		{
			SpecificValueBox.Enabled = false;
			SpecificAddressBox.Enabled = false;
			NumberOfChangesBox.Enabled = false;
			DifferenceBox.Enabled = false;
			SetCompareTo(RamSearchEngine.Compare.Previous);
		}

		private void SpecificValueRadio_Click(object sender, EventArgs e)
		{
			SpecificValueBox.Enabled = true;
			SpecificValueBox.Focus();
			SpecificAddressBox.Enabled = false;
			NumberOfChangesBox.Enabled = false;
			DifferenceBox.Enabled = false;
			SetCompareTo(RamSearchEngine.Compare.SpecificValue);
		}

		private void SpecificAddressRadio_Click(object sender, EventArgs e)
		{
			SpecificValueBox.Enabled = false;
			SpecificAddressBox.Enabled = true;
			SpecificAddressBox.Focus();
			NumberOfChangesBox.Enabled = false;
			DifferenceBox.Enabled = false;
			SetCompareTo(RamSearchEngine.Compare.SpecificAddress);
		}

		private void NumberOfChangesRadio_Click(object sender, EventArgs e)
		{
			SpecificValueBox.Enabled = false;
			SpecificAddressBox.Enabled = false;
			NumberOfChangesBox.Enabled = true;
			NumberOfChangesBox.Focus();
			DifferenceBox.Enabled = false;
			SetCompareTo(RamSearchEngine.Compare.Changes);
		}

		private void DifferenceRadio_Click(object sender, EventArgs e)
		{
			SpecificValueBox.Enabled = false;
			SpecificAddressBox.Enabled = false;
			NumberOfChangesBox.Enabled = false;
			DifferenceBox.Enabled = true;
			DifferenceBox.Focus();
			SetCompareTo(RamSearchEngine.Compare.Difference);
		}

		private void CompareToValue_TextChanged(object sender, EventArgs e)
		{
			SetCompareValue((sender as INumberBox).ToInt());
		}

		#endregion

		#region Comparison Operator Box

		private void EqualToRadio_Click(object sender, EventArgs e)
		{
			DifferentByBox.Enabled = false;
			SetComparisonOperator(RamSearchEngine.ComparisonOperator.Equal);
		}

		private void NotEqualToRadio_Click(object sender, EventArgs e)
		{
			DifferentByBox.Enabled = false;
			SetComparisonOperator(RamSearchEngine.ComparisonOperator.NotEqual);
		}

		private void LessThanRadio_Click(object sender, EventArgs e)
		{
			DifferentByBox.Enabled = false;
			SetComparisonOperator(RamSearchEngine.ComparisonOperator.LessThan);
		}

		private void GreaterThanRadio_Click(object sender, EventArgs e)
		{
			DifferentByBox.Enabled = false;
			SetComparisonOperator(RamSearchEngine.ComparisonOperator.GreaterThan);
		}

		private void LessThanOrEqualToRadio_Click(object sender, EventArgs e)
		{
			DifferentByBox.Enabled = false;
			SetComparisonOperator(RamSearchEngine.ComparisonOperator.LessThanEqual);
		}

		private void GreaterThanOrEqualToRadio_Click(object sender, EventArgs e)
		{
			DifferentByBox.Enabled = false;
			SetComparisonOperator(RamSearchEngine.ComparisonOperator.GreaterThanEqual);
		}

		private void DifferentByRadio_Click(object sender, EventArgs e)
		{
			DifferentByBox.Enabled = true;
			SetComparisonOperator(RamSearchEngine.ComparisonOperator.DifferentBy);
			if (String.IsNullOrWhiteSpace(DifferentByBox.Text))
			{
				DifferentByBox.Text = "0";
			}
			DifferentByBox.Focus();
		}

		private void DifferentByBox_TextChanged(object sender, EventArgs e)
		{
			if (!String.IsNullOrWhiteSpace(DifferentByBox.Text))
			{
				Searches.DifferentBy = DifferentByBox.ToInt();
			}
			else
			{
				Searches.DifferentBy = null;
			}
		}

		#endregion

		#region ListView Events

		private void WatchListView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete && !e.Control && !e.Alt && !e.Shift)
			{
				RemoveAddresses();
			}
		}

		private void WatchListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			RemoveToolBarItem.Enabled =
				AddToRamWatchToolBarItem.Enabled =
				PokeAddressToolBarItem.Enabled =
				FreezeAddressToolBarItem.Enabled =
				SelectedIndices.Any();
		}

		#endregion

		#endregion
	}
}
