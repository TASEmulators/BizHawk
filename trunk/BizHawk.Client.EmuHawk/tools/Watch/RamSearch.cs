using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// A form designed to search through ram values
	/// </summary>
	public partial class RamSearch : Form, IToolForm
	{
		// TODO: DoSearch grabs the state of widgets and passes it to the engine before running, so rip out code that is attempting to keep the state up to date through change events
		private readonly Dictionary<string, int> _defaultColumnWidths = new Dictionary<string, int>
		{
			{ WatchList.ADDRESS, 60 },
			{ WatchList.VALUE, 59 },
			{ WatchList.PREV, 59 },
			{ WatchList.CHANGES, 55 },
			{ WatchList.DIFF, 59 },
		};

		private string _currentFileName = string.Empty;

		private RamSearchEngine _searches;
		private RamSearchEngine.Settings _settings;

		private int _defaultWidth;
		private int _defaultHeight;
		private string _sortedColumn = string.Empty;
		private bool _sortReverse;
		private bool _forcePreviewClear;
		private bool _autoSearch;

		private bool _dropdownDontfire; // Used as a hack to get around lame .net dropdowns, there's no way to set their index without firing the selectedindexchanged event!

		public const int MaxDetailedSize = 1024 * 1024; // 1mb, semi-arbituary decision, sets the size to check for and automatically switch to fast mode for the user
		public const int MaxSupportedSize = 1024 * 1024 * 64; // 64mb, semi-arbituary decision, sets the maximum size ram search will support (as it will crash beyond this)

		public bool AskSave()
		{
			return true;
		}

		public bool UpdateBefore
		{
			get { return false; }
		}

		#region Initialize, Load, and Save

		public RamSearch()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			InitializeComponent();
			WatchListView.QueryItemText += ListView_QueryItemText;
			WatchListView.QueryItemBkColor += ListView_QueryItemBkColor;
			WatchListView.VirtualMode = true;
			Closing += (o, e) => SaveConfigSettings();

			_sortedColumn = string.Empty;
			_sortReverse = false;

			_settings = new RamSearchEngine.Settings();
			_searches = new RamSearchEngine(_settings);

			TopMost = Global.Config.RamSearchSettings.TopMost;
		}

		private void HardSetDisplayTypeDropDown(Watch.DisplayType type)
		{
			foreach (var item in DisplayTypeDropdown.Items)
			{
				if (Watch.DisplayTypeToString(type) == item.ToString())
				{
					DisplayTypeDropdown.SelectedItem = item;
				}
			}
		}

		private void HardSetSizeDropDown(Watch.WatchSize size)
		{
			switch (size)
			{
				case Watch.WatchSize.Byte:
					SizeDropdown.SelectedIndex = 0;
					break;
				case Watch.WatchSize.Word:
					SizeDropdown.SelectedIndex = 1;
					break;
				case Watch.WatchSize.DWord:
					SizeDropdown.SelectedIndex = 2;
					break;
			}
		}

		private void RamSearch_Load(object sender, EventArgs e)
		{
			_dropdownDontfire = true;
			LoadConfigSettings();
			SpecificValueBox.ByteSize = _settings.Size;
			SpecificValueBox.Type = _settings.Type;

			MessageLabel.Text = string.Empty;
			SpecificAddressBox.MaxLength = IntHelpers.GetNumDigits(Global.Emulator.MemoryDomains.MainMemory.Size);
			HardSetSizeDropDown(_settings.Size);
			PopulateTypeDropDown();
			HardSetDisplayTypeDropDown(_settings.Type);
			DoDomainSizeCheck();
			SetReboot(false);

			SpecificAddressBox.SetHexProperties(_settings.Domain.Size);
			SpecificValueBox.ResetText();
			SpecificAddressBox.ResetText();
			NumberOfChangesBox.ResetText();
			DifferenceBox.ResetText();
			DifferentByBox.ResetText();

			_dropdownDontfire = false;

			if (_settings.Mode == RamSearchEngine.Settings.SearchMode.Fast)
			{
				SetToFastMode();
			}

			NewSearch();
		}

		private void ListView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (_searches.Count > 0 && column == 0)
			{
				var nextColor = Color.White;

				var isCheat = Global.CheatList.IsActive(_settings.Domain, _searches[index].Address ?? 0);
				var isWeeded = Global.Config.RamSearchPreviewMode && !_forcePreviewClear && _searches.Preview(_searches[index].Address ?? 0);

				if (isCheat)
				{
					nextColor = isWeeded ? Color.Lavender : Color.LightCyan;
				}
				else
				{
					if (isWeeded)
					{
						nextColor = Color.Pink;
					}
				}

				color = nextColor;
			}
		}

		private void ListView_QueryItemText(int index, int column, out string text)
		{
			text = string.Empty;

			if (index >= _searches.Count)
			{
				return;
			}

			var columnName = WatchListView.Columns[column].Name;
			switch (columnName)
			{
				case WatchList.ADDRESS:
					text = _searches[index].AddressString;
					break;
				case WatchList.VALUE:
					text = _searches[index].ValueString;
					break;
				case WatchList.PREV:
					text = _searches[index].PreviousStr;
					break;
				case WatchList.CHANGES:
					text = _searches[index].ChangeCount.ToString();
					break;
				case WatchList.DIFF:
					text = _searches[index].Diff;
					break;
			}
		}

		private void LoadConfigSettings()
		{
			_defaultWidth = Size.Width;
			_defaultHeight = Size.Height;

			if (Global.Config.RamSearchSettings.UseWindowPosition)
			{
				Location = Global.Config.RamSearchSettings.WindowPosition;
			}

			if (Global.Config.RamSearchSettings.UseWindowSize)
			{
				Size = Global.Config.RamSearchSettings.WindowSize;
			}

			TopMost = Global.Config.RamSearchSettings.TopMost;

			LoadColumnInfo();
		}

		#endregion

		#region Public

		public void UpdateValues()
		{
			if (_searches.Count > 0)
			{
				_searches.Update();

				if (_autoSearch)
				{
					DoSearch();
				}

				_forcePreviewClear = false;
				WatchListView.Refresh();
			}
		}

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			_settings.Domain = Global.Emulator.MemoryDomains.MainMemory;
			MessageLabel.Text = "Search restarted";
			DoDomainSizeCheck();
			NewSearch();
		}

		public void SaveConfigSettings()
		{
			SaveColumnInfo();

			Global.Config.RamSearchSettings.Wndx = Location.X;
			Global.Config.RamSearchSettings.Wndy = Location.Y;
			Global.Config.RamSearchSettings.Width = Right - Left;
			Global.Config.RamSearchSettings.Height = Bottom - Top;
		}

		public void NewSearch()
		{
			var compareTo = _searches.CompareTo;
			var compareVal = _searches.CompareValue;
			var differentBy = _searches.DifferentBy;

			_searches = new RamSearchEngine(_settings, compareTo, compareVal, differentBy);
			_searches.Start();
			if (Global.Config.RamSearchAlwaysExcludeRamWatch)
			{
				RemoveRamWatchesFromList();
			}

			SetTotal();
			WatchListView.ItemCount = _searches.Count;
			ToggleSearchDependentToolBarItems();
			SetReboot(false);
			MessageLabel.Text = string.Empty;
			SetDomainLabel();
		}

		public void NextCompareTo(bool reverse = false)
		{
			var radios = CompareToBox.Controls
				.OfType<RadioButton>()
				.Select(control => control)
				.OrderBy(x => x.TabIndex)
				.ToList();

			var selected = radios.FirstOrDefault(x => x.Checked);
			var index = radios.IndexOf(selected);

			if (reverse)
			{
				if (index == 0)
				{
					index = radios.Count - 1;
				}
				else
				{
					index--;
				}
			}
			else
			{
				index++;
				if (index >= radios.Count)
				{
					index = 0;
				}
			}

			radios[index].Checked = true;
			var mi = radios[index].GetType().GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic);
			mi.Invoke(radios[index], new object[] { new EventArgs() });
		}

		public void NextOperator(bool reverse = false)
		{
			var radios = ComparisonBox.Controls
				.OfType<RadioButton>()
				.Select(control => control)
				.OrderBy(x => x.TabIndex)
				.ToList();

			var selected = radios.FirstOrDefault(x => x.Checked);
			var index = radios.IndexOf(selected);

			if (reverse)
			{
				if (index == 0)
				{
					index = radios.Count - 1;
				}
				else
				{
					index--;
				}
			}
			else
			{
				index++;
				if (index >= radios.Count)
				{
					index = 0;
				}
			}

			radios[index].Checked = true;
			var mi = radios[index].GetType().GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic);
			mi.Invoke(radios[index], new object[] { new EventArgs() });
		}

		#endregion

		#region Private

		private void RefreshFloatingWindowControl()
		{
			Owner = Global.Config.RamSearchSettings.FloatingWindow ? null : GlobalWin.MainForm;
		}

		private void ToggleSearchDependentToolBarItems()
		{
			DoSearchToolButton.Enabled =
				CopyValueToPrevToolBarItem.Enabled =
				_searches.Count > 0;
			UpdateUndoToolBarButtons();
		}

		private int? CompareToValue
		{
			get
			{
				if (PreviousValueRadio.Checked)
				{
					return null;
				}
				else if (SpecificValueRadio.Checked)
				{
					return SpecificValueBox.ToRawInt();
				}
				else if (SpecificAddressRadio.Checked)
				{
					return SpecificAddressBox.ToRawInt();
				}
				else if (NumberOfChangesRadio.Checked)
				{
					return NumberOfChangesBox.ToRawInt();
				}
				else if (DifferenceRadio.Checked)
				{
					return DifferenceBox.ToRawInt();
				}
				else
				{
					return null;
				}
			}
		}

		private int? DifferentByValue
		{
			get
			{
				return DifferentByRadio.Checked ? DifferentByBox.ToRawInt() : null;
			}
		}

		private RamSearchEngine.ComparisonOperator Operator
		{
			get
			{
				if (NotEqualToRadio.Checked) return RamSearchEngine.ComparisonOperator.NotEqual;
				else if (LessThanRadio.Checked) return RamSearchEngine.ComparisonOperator.LessThan;
				else if (GreaterThanRadio.Checked) return RamSearchEngine.ComparisonOperator.GreaterThan;
				else if (LessThanOrEqualToRadio.Checked) return RamSearchEngine.ComparisonOperator.LessThanEqual;
				else if (GreaterThanOrEqualToRadio.Checked) return RamSearchEngine.ComparisonOperator.GreaterThanEqual;
				else if (DifferentByRadio.Checked) return RamSearchEngine.ComparisonOperator.DifferentBy;
				else return RamSearchEngine.ComparisonOperator.Equal;
			}
		}

		private RamSearchEngine.Compare Compare
		{
			get
			{
				if (SpecificValueRadio.Checked) return RamSearchEngine.Compare.SpecificValue;
				else if (SpecificAddressRadio.Checked) return RamSearchEngine.Compare.SpecificAddress;
				else if (NumberOfChangesRadio.Checked) return RamSearchEngine.Compare.Changes;
				else if (DifferenceRadio.Checked) return RamSearchEngine.Compare.Difference;
				else return RamSearchEngine.Compare.Previous;
			}
		}

		public void DoSearch()
		{
			_searches.CompareValue = CompareToValue;
			_searches.DifferentBy = DifferentByValue;
			_searches.Operator = Operator;
			_searches.CompareTo = Compare;

			var removed = _searches.DoSearch();
			SetTotal();
			WatchListView.ItemCount = _searches.Count;
			SetRemovedMessage(removed);
			ToggleSearchDependentToolBarItems();
			_forcePreviewClear = true;
		}

		private IList<int> SelectedIndices
		{
			get { return WatchListView.SelectedIndices.Cast<int>().ToList(); }
		}

		private IEnumerable<Watch> SelectedItems
		{
			get { return SelectedIndices.Select(index => _searches[index]); }
		}

		private IEnumerable<Watch> SelectedWatches
		{
			get { return SelectedItems.Where(x => !x.IsSeparator); }
		}

		private void SetRemovedMessage(int val)
		{
			MessageLabel.Text = val + " address" + (val != 1 ? "es" : string.Empty) + " removed";
		}

		private void SetTotal()
		{
			TotalSearchLabel.Text = string.Format("{0:n0}", _searches.Count) + " addresses";
		}

		private void SetDomainLabel()
		{
			MemDomainLabel.Text = _searches.Domain.Name;
		}

		private void LoadFileFromRecent(string path)
		{
			var file = new FileInfo(path);

			if (!file.Exists)
			{
				ToolHelpers.HandleLoadError(Global.Config.RecentSearches, path);
			}
			else
			{
				LoadWatchFile(file, append: false);
			}
		}

		private void SetMemoryDomain(string name)
		{
			_settings.Domain = Global.Emulator.MemoryDomains[name];
			SetReboot(true);
			SpecificAddressBox.MaxLength = IntHelpers.GetNumDigits(_settings.Domain.Size);
			DoDomainSizeCheck();
		}

		private void DoDomainSizeCheck()
		{
			if (_settings.Domain.Size >= MaxDetailedSize 
				&& _settings.Mode == RamSearchEngine.Settings.SearchMode.Detailed)
			{
				_settings.Mode = RamSearchEngine.Settings.SearchMode.Fast;
				SetReboot(true);
				MessageLabel.Text = "Large domain, switching to fast mode";
			}
		}

		private void LoadColumnInfo()
		{
			WatchListView.Columns.Clear();
			ToolHelpers.AddColumn(WatchListView, WatchList.ADDRESS, true, GetColumnWidth(WatchList.ADDRESS));
			ToolHelpers.AddColumn(WatchListView, WatchList.VALUE, true, GetColumnWidth(WatchList.VALUE));
			ToolHelpers.AddColumn(WatchListView, WatchList.PREV, Global.Config.RamSearchShowPrevColumn, GetColumnWidth(WatchList.PREV));
			ToolHelpers.AddColumn(WatchListView, WatchList.CHANGES, Global.Config.RamSearchShowChangeColumn, GetColumnWidth(WatchList.CHANGES));
			ToolHelpers.AddColumn(WatchListView, WatchList.DIFF, Global.Config.RamSearchShowDiffColumn, GetColumnWidth(WatchList.DIFF));

			ColumnPositions();
		}

		private void ColumnPositions()
		{
			var columns = Global.Config.RamSearchColumnIndexes
					.Where(x => WatchListView.Columns.ContainsKey(x.Key))
					.OrderBy(x => x.Value).ToList();

			for (var i = 0; i < columns.Count; i++)
			{
				WatchListView.Columns[columns[i].Key].DisplayIndex = i;
			}
		}

		private void SaveColumnInfo()
		{
			if (WatchListView.Columns[WatchList.ADDRESS] != null)
			{
				Global.Config.RamSearchColumnIndexes[WatchList.ADDRESS] = WatchListView.Columns[WatchList.ADDRESS].DisplayIndex;
				Global.Config.RamSearchColumnWidths[WatchList.ADDRESS] = WatchListView.Columns[WatchList.ADDRESS].Width;
			}

			if (WatchListView.Columns[WatchList.VALUE] != null)
			{
				Global.Config.RamSearchColumnIndexes[WatchList.VALUE] = WatchListView.Columns[WatchList.VALUE].DisplayIndex;
				Global.Config.RamSearchColumnWidths[WatchList.VALUE] = WatchListView.Columns[WatchList.VALUE].Width;
			}

			if (WatchListView.Columns[WatchList.PREV] != null)
			{
				Global.Config.RamSearchColumnIndexes[WatchList.PREV] = WatchListView.Columns[WatchList.PREV].DisplayIndex;
				Global.Config.RamSearchColumnWidths[WatchList.PREV] = WatchListView.Columns[WatchList.PREV].Width;
			}

			if (WatchListView.Columns[WatchList.CHANGES] != null)
			{
				Global.Config.RamSearchColumnIndexes[WatchList.CHANGES] = WatchListView.Columns[WatchList.CHANGES].DisplayIndex;
				Global.Config.RamSearchColumnWidths[WatchList.CHANGES] = WatchListView.Columns[WatchList.CHANGES].Width;
			}

			if (WatchListView.Columns[WatchList.DIFF] != null)
			{
				Global.Config.RamSearchColumnIndexes[WatchList.DIFF] = WatchListView.Columns[WatchList.DIFF].DisplayIndex;
				Global.Config.RamSearchColumnWidths[WatchList.DIFF] = WatchListView.Columns[WatchList.DIFF].Width;
			}
		}

		private int GetColumnWidth(string columnName)
		{
			var width = Global.Config.RamSearchColumnWidths[columnName];
			if (width == -1)
			{
				width = _defaultColumnWidths[columnName];
			}

			return width;
		}

		private void DoDisplayTypeClick(Watch.DisplayType type)
		{
			if (_settings.Type != type && !string.IsNullOrEmpty(SpecificValueBox.Text))
			{
				SpecificValueBox.Text = "0";
			}

			SpecificValueBox.Type = _settings.Type = type;
			_searches.SetType(type);

			_dropdownDontfire = true;
			DisplayTypeDropdown.SelectedItem = Watch.DisplayTypeToString(type);
			_dropdownDontfire = false;
			SpecificValueBox.Type = type;
			WatchListView.Refresh();
		}

		private void SetPreviousStype(Watch.PreviousType type)
		{
			_settings.PreviousType = type;
			_searches.SetPreviousType(type);
		}

		private void SetSize(Watch.WatchSize size)
		{
			SpecificValueBox.ByteSize = _settings.Size = size;
			if (!string.IsNullOrEmpty(SpecificAddressBox.Text))
			{
				SpecificAddressBox.Text = "0";
			}

			if (!string.IsNullOrEmpty(SpecificValueBox.Text))
			{
				SpecificValueBox.Text = "0";
			}

			if (!Watch.AvailableTypes(size).Contains(_settings.Type))
			{
				_settings.Type = Watch.AvailableTypes(size)[0];
			}

			_dropdownDontfire = true;
			switch (size)
			{
				case Watch.WatchSize.Byte:
					SizeDropdown.SelectedIndex = 0;
					break;
				case Watch.WatchSize.Word:
					SizeDropdown.SelectedIndex = 1;
					break;
				case Watch.WatchSize.DWord:
					SizeDropdown.SelectedIndex = 2;
					break;
			}

			PopulateTypeDropDown();
			_dropdownDontfire = false;
			SpecificValueBox.Type = _settings.Type;
			SetReboot(true);
		}

		private void PopulateTypeDropDown()
		{
			var previous = DisplayTypeDropdown.SelectedItem != null ? DisplayTypeDropdown.SelectedItem.ToString() : string.Empty;
			var next = string.Empty;

			DisplayTypeDropdown.Items.Clear();
			var types = Watch.AvailableTypes(_settings.Size);
			foreach (var type in types)
			{
				var typeStr = Watch.DisplayTypeToString(type);
				DisplayTypeDropdown.Items.Add(typeStr);
				if (previous == typeStr)
				{
					next = typeStr;
				}
			}

			if (!string.IsNullOrEmpty(next))
			{
				DisplayTypeDropdown.SelectedItem = next;
			}
			else
			{
				DisplayTypeDropdown.SelectedIndex = 0;
			}
		}

		private void SetComparisonOperator(RamSearchEngine.ComparisonOperator op)
		{
			_searches.Operator = op;
			WatchListView.Refresh();
		}

		private void SetCompareTo(RamSearchEngine.Compare comp)
		{
			_searches.CompareTo = comp;
			WatchListView.Refresh();
		}

		private void SetCompareValue(int? value)
		{
			_searches.CompareValue = value;
			WatchListView.Refresh();
		}

		private void SetReboot(bool rebootNeeded)
		{
			RebootToolBarSeparator.Visible =
					RebootToolbarButton.Visible =
					rebootNeeded;
		}

		private void SetToDetailedMode()
		{
			_settings.Mode = RamSearchEngine.Settings.SearchMode.Detailed;
			NumberOfChangesRadio.Enabled = true;
			NumberOfChangesBox.Enabled = true;
			DifferenceRadio.Enabled = true;
			DifferentByBox.Enabled = true;
			ClearChangeCountsToolBarItem.Enabled = true;
			WatchListView.Columns[WatchList.CHANGES].Width = Global.Config.RamSearchColumnWidths[WatchList.CHANGES];
			SetReboot(true);
		}

		private void SetToFastMode()
		{
			_settings.Mode = RamSearchEngine.Settings.SearchMode.Fast;

			if (_settings.PreviousType == Watch.PreviousType.LastFrame || _settings.PreviousType == Watch.PreviousType.LastChange)
			{
				SetPreviousStype(Watch.PreviousType.LastSearch);
			}

			NumberOfChangesRadio.Enabled = false;
			NumberOfChangesBox.Enabled = false;
			NumberOfChangesBox.Text = string.Empty;
			ClearChangeCountsToolBarItem.Enabled = false;

			if (NumberOfChangesRadio.Checked || DifferenceRadio.Checked)
			{
				PreviousValueRadio.Checked = true;
			}

			Global.Config.RamSearchColumnWidths[WatchList.CHANGES] = WatchListView.Columns[WatchList.CHANGES].Width;
			WatchListView.Columns[WatchList.CHANGES].Width = 0;
			SetReboot(true);
		}

		private void RemoveAddresses()
		{
			if (SelectedIndices.Count > 0)
			{
				SetRemovedMessage(SelectedIndices.Count);

				var addresses = SelectedIndices.Select(index => _searches[index].Address ?? 0).ToList();
				_searches.RemoveRange(addresses);

				WatchListView.ItemCount = _searches.Count;
				SetTotal();
				WatchListView.SelectedIndices.Clear();
				ToggleSearchDependentToolBarItems();
			}
		}

		public void LoadWatchFile(FileInfo file, bool append, bool truncate = false)
		{
			if (file != null)
			{
				if (!truncate)
				{
					_currentFileName = file.FullName;
				}

				var watches = new WatchList(_settings.Domain);
				watches.Load(file.FullName, append);
				var addresses = watches.Where(x => !x.IsSeparator).Select(x => x.Address ?? 0).ToList();

				if (truncate)
				{
					SetRemovedMessage(addresses.Count);
					_searches.RemoveRange(addresses);
				}
				else
				{
					_searches.AddRange(addresses, append);
					MessageLabel.Text = file.Name + " loaded";
				}

				WatchListView.ItemCount = _searches.Count;
				SetTotal();
				Global.Config.RecentSearches.Add(file.FullName);

				if (!append && !truncate)
				{
					_searches.ClearHistory();
				}

				ToggleSearchDependentToolBarItems();
			}
		}

		private void AddToRamWatch()
		{
			var watches = SelectedWatches.ToList();
			if (watches.Any())
			{
				GlobalWin.Tools.LoadRamWatch(true);
				watches.ForEach(GlobalWin.Tools.RamWatch.AddWatch);
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
				var poke = new RamPoke();
				var watches = SelectedIndices.Select(t => _searches[t]).ToList();
				poke.SetWatch(watches);
				poke.InitialLocation = GetPromptPoint();
				poke.ShowHawkDialog();
				UpdateValues();
			}
		}

		private void RemoveRamWatchesFromList()
		{
			if (GlobalWin.Tools.Has<RamWatch>())
			{
				_searches.RemoveRange(GlobalWin.Tools.RamWatch.AddressList);
				WatchListView.ItemCount = _searches.Count;
				SetTotal();
			}
		}

		private void UpdateUndoToolBarButtons()
		{
			UndoToolBarButton.Enabled = _searches.CanUndo;
			RedoToolBarItem.Enabled = _searches.CanRedo;
		}

		private string GetColumnValue(string name, int index)
		{
			switch (name)
			{
				default:
					return string.Empty;
				case WatchList.ADDRESS:
					return _searches[index].AddressString;
				case WatchList.VALUE:
					return _searches[index].ValueString;
				case WatchList.PREV:
					return _searches[index].PreviousStr;
				case WatchList.CHANGES:
					return _searches[index].ChangeCount.ToString();
				case WatchList.DIFF:
					return _searches[index].Diff;
			}
		}

		private void ToggleAutoSearch()
		{
			_autoSearch ^= true;
			AutoSearchCheckBox.Checked = _autoSearch;
			DoSearchToolButton.Enabled =
				SearchButton.Enabled =
				!_autoSearch;
		}

		private void GoToSpecifiedAddress()
		{
			WatchListView.SelectedIndices.Clear();
			var prompt = new InputPrompt { Text = "Go to Address", _Location = GetPromptPoint() };
			prompt.SetMessage("Enter a hexadecimal value");
			prompt.ShowHawkDialog();

			if (prompt.UserOK)
			{
				if (InputValidate.IsHex(prompt.UserText))
				{
					var addr = int.Parse(prompt.UserText, NumberStyles.HexNumber);
					WatchListView.SelectItem(addr, true);
					WatchListView.ensureVisible();
				}
			}
		}

		#endregion

		#region Winform Events

		#region File

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveMenuItem.Enabled = !string.IsNullOrWhiteSpace(_currentFileName);
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(
				ToolHelpers.GenerateRecentMenu(Global.Config.RecentSearches, LoadFileFromRecent)
			);
		}

		private void OpenMenuItem_Click(object sender, EventArgs e)
		{
			LoadWatchFile(
				ToolHelpers.GetWatchFileFromUser(string.Empty),
				sender == AppendFileMenuItem,
				sender == TruncateFromFileMenuItem
				);
		}

		private void SaveMenuItem_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(_currentFileName))
			{
				var watches = new WatchList(_settings.Domain) { CurrentFileName = _currentFileName };
				for (var i = 0; i < _searches.Count; i++)
				{
					watches.Add(_searches[i]);
				}

				if (!string.IsNullOrWhiteSpace(watches.CurrentFileName))
				{
					if (watches.Save())
					{
						_currentFileName = watches.CurrentFileName;
						MessageLabel.Text = Path.GetFileName(_currentFileName) + " saved";
					}
				}
				else
				{
					var result = watches.SaveAs(ToolHelpers.GetWatchSaveFileFromUser(watches.CurrentFileName));
					if (result)
					{
						MessageLabel.Text = Path.GetFileName(_currentFileName) + " saved";
						Global.Config.RecentWatches.Add(watches.CurrentFileName);
					}
				}
			}
		}

		private void SaveAsMenuItem_Click(object sender, EventArgs e)
		{
			var watches = new WatchList(_settings.Domain) { CurrentFileName = _currentFileName };
			for (var i = 0; i < _searches.Count; i++)
			{
				watches.Add(_searches[i]);
			}

			if (watches.SaveAs(ToolHelpers.GetWatchSaveFileFromUser(watches.CurrentFileName)))
			{
				_currentFileName = watches.CurrentFileName;
				MessageLabel.Text = Path.GetFileName(_currentFileName) + " saved";
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
			CheckMisalignedMenuItem.Checked = _settings.CheckMisAligned;
			BigEndianMenuItem.Checked = _settings.BigEndian;
		}

		private void ModeSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DetailedMenuItem.Checked = _settings.Mode == RamSearchEngine.Settings.SearchMode.Detailed;
			FastMenuItem.Checked = _settings.Mode == RamSearchEngine.Settings.SearchMode.Fast;
		}

		private void MemoryDomainsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			MemoryDomainsSubMenu.DropDownItems.Clear();
			MemoryDomainsSubMenu.DropDownItems.AddRange(ToolHelpers.GenerateMemoryDomainMenuItems(SetMemoryDomain, _searches.Domain.Name, MaxSupportedSize).ToArray());
		}

		private void SizeSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ByteMenuItem.Checked = _settings.Size == Watch.WatchSize.Byte;
			WordMenuItem.Checked = _settings.Size == Watch.WatchSize.Word;
			DWordMenuItem.Checked = _settings.Size == Watch.WatchSize.DWord;
		}

		private void DisplayTypeSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DisplayTypeSubMenu.DropDownItems.Clear();

			foreach (var type in Watch.AvailableTypes(_settings.Size))
			{
				var item = new ToolStripMenuItem
					{
					Name = type + "ToolStripMenuItem",
					Text = Watch.DisplayTypeToString(type),
					Checked = _settings.Type == type,
				};
				var type1 = type;
				item.Click += (o, ev) => DoDisplayTypeClick(type1);

				DisplayTypeSubMenu.DropDownItems.Add(item);
			}
		}

		private void DefinePreviousValueSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			Previous_LastSearchMenuItem.Checked = false;
			PreviousFrameMenuItem.Checked = false;
			Previous_OriginalMenuItem.Checked = false;

			switch (_settings.PreviousType)
			{
				default:
				case Watch.PreviousType.LastSearch:
					Previous_LastSearchMenuItem.Checked = true;
					break;
				case Watch.PreviousType.LastFrame:
					PreviousFrameMenuItem.Checked = true;
					break;
				case Watch.PreviousType.Original:
					Previous_OriginalMenuItem.Checked = true;
					break;
			}

			PreviousFrameMenuItem.Enabled = _settings.Mode != RamSearchEngine.Settings.SearchMode.Fast;
		}

		private void DetailedMenuItem_Click(object sender, EventArgs e)
		{
			SetToDetailedMode();
		}

		private void FastMenuItem_Click(object sender, EventArgs e)
		{
			SetToFastMode();
		}

		private void ByteMenuItem_Click(object sender, EventArgs e)
		{
			SetSize(Watch.WatchSize.Byte);
		}

		private void WordMenuItem_Click(object sender, EventArgs e)
		{
			SetSize(Watch.WatchSize.Word);
		}

		private void DWordMenuItem_Click_Click(object sender, EventArgs e)
		{
			SetSize(Watch.WatchSize.DWord);
		}

		private void CheckMisalignedMenuItem_Click(object sender, EventArgs e)
		{
			_settings.CheckMisAligned ^= true;
			SetReboot(true);
		}

		private void Previous_LastFrameMenuItem_Click(object sender, EventArgs e)
		{
			SetPreviousStype(Watch.PreviousType.LastFrame);
		}

		private void Previous_LastSearchMenuItem_Click(object sender, EventArgs e)
		{
			SetPreviousStype(Watch.PreviousType.LastSearch);
		}

		private void Previous_OriginalMenuItem_Click(object sender, EventArgs e)
		{
			SetPreviousStype(Watch.PreviousType.Original);
		}

		private void BigEndianMenuItem_Click(object sender, EventArgs e)
		{
			_settings.BigEndian ^= true;
			_searches.SetEndian(_settings.BigEndian);
		}

		#endregion

		#region Search

		private void SearchSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ClearChangeCountsMenuItem.Enabled = _settings.Mode == RamSearchEngine.Settings.SearchMode.Detailed;

			RemoveMenuItem.Enabled =
				AddToRamWatchMenuItem.Enabled =
				PokeAddressMenuItem.Enabled =
				FreezeAddressMenuItem.Enabled =
				SelectedIndices.Any();

			UndoMenuItem.Enabled =
				ClearUndoMenuItem.Enabled =
				_searches.CanUndo;

			RedoMenuItem.Enabled = _searches.CanRedo;
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
			if (_searches.CanUndo)
			{
				_searches.Undo();
				SetTotal();
				WatchListView.ItemCount = _searches.Count;
				ToggleSearchDependentToolBarItems();
				_forcePreviewClear = true;
				UpdateUndoToolBarButtons();
			}
		}

		private void RedoMenuItem_Click(object sender, EventArgs e)
		{
			if (_searches.CanRedo)
			{
				_searches.Redo();
				SetTotal();
				WatchListView.ItemCount = _searches.Count;
				ToggleSearchDependentToolBarItems();
				_forcePreviewClear = true;
				UpdateUndoToolBarButtons();
			}
		}

		private void CopyValueToPrevMenuItem_Click(object sender, EventArgs e)
		{
			_searches.SetPrevousToCurrent();
			WatchListView.Refresh();
		}

		private void ClearChangeCountsMenuItem_Click(object sender, EventArgs e)
		{
			_searches.ClearChangeCounts();
			WatchListView.Refresh();
		}

		private void RemoveMenuItem_Click(object sender, EventArgs e)
		{
			RemoveAddresses();
		}

		private void GoToAddressMenuItem_Click(object sender, EventArgs e)
		{
			GoToSpecifiedAddress();
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
			var allCheats = SelectedWatches.All(x => Global.CheatList.IsActive(x.Domain, x.Address ?? 0));
			if (allCheats)
			{
				ToolHelpers.UnfreezeAddress(SelectedWatches);
			}
			else
			{
				ToolHelpers.FreezeAddress(SelectedWatches);
			}
		}

		private void ClearUndoMenuItem_Click(object sender, EventArgs e)
		{
			_searches.ClearHistory();
			UpdateUndoToolBarButtons();
		}

		#endregion

		#region Options

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AutoloadDialogMenuItem.Checked = Global.Config.RecentSearches.AutoLoad;
			SaveWinPositionMenuItem.Checked = Global.Config.RamSearchSettings.SaveWindowPosition;
			ExcludeRamWatchMenuItem.Checked = Global.Config.RamSearchAlwaysExcludeRamWatch;
			UseUndoHistoryMenuItem.Checked = _searches.UndoEnabled;
			PreviewModeMenuItem.Checked = Global.Config.RamSearchPreviewMode;
			AlwaysOnTopMenuItem.Checked = Global.Config.RamSearchSettings.TopMost;
			FloatingWindowMenuItem.Checked = Global.Config.RamSearchSettings.FloatingWindow;
			AutoSearchMenuItem.Checked = _autoSearch;
		}

		private void PreviewModeMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchPreviewMode ^= true;
		}

		private void AutoSearchMenuItem_Click(object sender, EventArgs e)
		{
			ToggleAutoSearch();
		}

		private void ExcludeRamWatchMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchAlwaysExcludeRamWatch ^= true;
			if (Global.Config.RamSearchAlwaysExcludeRamWatch)
			{
				RemoveRamWatchesFromList();
			}
		}

		private void UseUndoHistoryMenuItem_Click(object sender, EventArgs e)
		{
			_searches.UndoEnabled ^= true;
		}

		private void AutoloadDialogMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RecentSearches.AutoLoad ^= true;
		}

		private void SaveWinPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchSettings.SaveWindowPosition ^= true;
		}

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchSettings.TopMost ^= true;
			TopMost = Global.Config.RamSearchSettings.TopMost;
		}

		private void FloatingWindowMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchSettings.FloatingWindow ^= true;
			RefreshFloatingWindowControl();
		}

		private void RestoreDefaultsMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(_defaultWidth, _defaultHeight);

			Global.Config.RamSearchColumnIndexes = new Dictionary<string, int>
				{
					{ "AddressColumn", 0 },
					{ "ValueColumn", 1 },
					{ "PrevColumn", 2 },
					{ "ChangesColumn", 3 },
					{ "DiffColumn", 4 },
					{ "DomainColumn", 5 },
					{ "NotesColumn", 6 },
				};

			ColumnPositions();

			Global.Config.RamSearchShowChangeColumn = true;
			Global.Config.RamSearchShowPrevColumn = true;
			Global.Config.RamSearchShowDiffColumn = false;

			WatchListView.Columns[WatchList.ADDRESS].Width = _defaultColumnWidths[WatchList.ADDRESS];
			WatchListView.Columns[WatchList.VALUE].Width = _defaultColumnWidths[WatchList.VALUE];
			WatchListView.Columns[WatchList.CHANGES].Width = _defaultColumnWidths[WatchList.CHANGES];

			Global.Config.RamSearchSettings.SaveWindowPosition = true;
			Global.Config.RamSearchSettings.TopMost = TopMost = false;
			Global.Config.RamSearchSettings.FloatingWindow = false;

			Global.Config.RamSearchColumnWidths = new Dictionary<string, int>
				{
					{ "AddressColumn", -1 },
					{ "ValueColumn", -1 },
					{ "PrevColumn", -1 },
					{ "ChangesColumn", -1 },
					{ "DiffColumn", -1 },
				};

			LoadColumnInfo();

			_settings = new RamSearchEngine.Settings();
			if (_settings.Mode == RamSearchEngine.Settings.SearchMode.Fast)
			{
				SetToFastMode();
			}

			RefreshFloatingWindowControl();
		}

		#endregion

		#region Columns

		private void ColumnsMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			ShowPreviousMenuItem.Checked = Global.Config.RamSearchShowPrevColumn;
			ShowChangesMenuItem.Checked = Global.Config.RamSearchShowChangeColumn;
			ShowDiffMenuItem.Checked = Global.Config.RamSearchShowDiffColumn;
		}

		private void ShowPreviousMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchShowPrevColumn ^= true;
			SaveColumnInfo();
			LoadColumnInfo();
		}

		private void ShowChangesMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchShowChangeColumn ^= true;
			SaveColumnInfo();
			LoadColumnInfo();
		}

		private void ShowDiffMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamSearchShowDiffColumn ^= true;
			SaveColumnInfo();
			LoadColumnInfo();
		}

		#endregion

		#region ContextMenu and Toolbar

		private void ListViewContextMenu_Opening(object sender, CancelEventArgs e)
		{
			DoSearchContextMenuItem.Enabled = _searches.Count > 0;

			RemoveContextMenuItem.Visible =
				AddToRamWatchContextMenuItem.Visible =
				PokeContextMenuItem.Visible =
				FreezeContextMenuItem.Visible =
				ContextMenuSeparator2.Visible =

				ViewInHexEditorContextMenuItem.Visible =
				SelectedIndices.Count > 0;

			UnfreezeAllContextMenuItem.Visible = Global.CheatList.ActiveCount > 0;

			ContextMenuSeparator3.Visible = SelectedIndices.Any() || (Global.CheatList.ActiveCount > 0);

			var allCheats = true;
			foreach (var index in SelectedIndices)
			{
				if (!Global.CheatList.IsActive(_settings.Domain, _searches[index].Address ?? 0))
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
			Global.CheatList.DisableAll();
		}

		private void ViewInHexEditorContextMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedWatches.Any())
			{
				ToolHelpers.ViewInHexEditor(_searches.Domain, SelectedWatches.Select(x => x.Address ?? 0));
			}
		}

		private void ClearPreviewContextMenuItem_Click(object sender, EventArgs e)
		{
			_forcePreviewClear = true;
			WatchListView.Refresh();
		}

		private void SizeDropdown_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_dropdownDontfire)
			{
				return;
			}

			switch (SizeDropdown.SelectedIndex)
			{
				case 0:
					SetSize(Watch.WatchSize.Byte);
					break;
				case 1:
					SetSize(Watch.WatchSize.Word);
					break;
				case 2:
					SetSize(Watch.WatchSize.DWord);
					break;
			}
		}

		private void DisplayTypeDropdown_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!_dropdownDontfire)
			{
				DoDisplayTypeClick(Watch.StringToDisplayType(DisplayTypeDropdown.SelectedItem.ToString()));
			}
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
			if (string.IsNullOrWhiteSpace(SpecificValueBox.Text))
			{
				SpecificAddressBox.ResetText();
			}

			_searches.CompareValue = SpecificValueBox.ToRawInt();

			if (Focused)
			{
				SpecificValueBox.Focus();
			}
			
			SpecificAddressBox.Enabled = false;
			NumberOfChangesBox.Enabled = false;
			DifferenceBox.Enabled = false;
			SetCompareTo(RamSearchEngine.Compare.SpecificValue);
		}

		private void SpecificAddressRadio_Click(object sender, EventArgs e)
		{
			SpecificValueBox.Enabled = false;
			SpecificAddressBox.Enabled = true;
			if (string.IsNullOrWhiteSpace(SpecificAddressBox.Text))
			{
				SpecificAddressBox.ResetText();
			}

			_searches.CompareValue = SpecificAddressBox.ToRawInt();

			if (Focused)
			{
				SpecificAddressBox.Focus();
			}

			NumberOfChangesBox.Enabled = false;
			DifferenceBox.Enabled = false;
			SetCompareTo(RamSearchEngine.Compare.SpecificAddress);
		}

		private void NumberOfChangesRadio_Click(object sender, EventArgs e)
		{
			SpecificValueBox.Enabled = false;
			SpecificAddressBox.Enabled = false;
			NumberOfChangesBox.Enabled = true;
			if (string.IsNullOrWhiteSpace(NumberOfChangesBox.Text))
			{
				NumberOfChangesBox.ResetText();
			}

			_searches.CompareValue = NumberOfChangesBox.ToRawInt();

			if (Focused)
			{
				NumberOfChangesBox.Focus();
			}

			DifferenceBox.Enabled = false;
			SetCompareTo(RamSearchEngine.Compare.Changes);
		}

		private void DifferenceRadio_Click(object sender, EventArgs e)
		{
			SpecificValueBox.Enabled = false;
			SpecificAddressBox.Enabled = false;
			NumberOfChangesBox.Enabled = false;
			DifferenceBox.Enabled = true;
			if (string.IsNullOrWhiteSpace(DifferenceBox.Text))
			{
				DifferenceBox.ResetText();
			}

			_searches.CompareValue = DifferenceBox.ToRawInt();

			if (Focused)
			{
				DifferenceBox.Focus();
			}

			SetCompareTo(RamSearchEngine.Compare.Difference);
		}

		private void CompareToValue_TextChanged(object sender, EventArgs e)
		{
			SetCompareValue((sender as INumberBox).ToRawInt());
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
			
			if (string.IsNullOrWhiteSpace(DifferentByBox.Text))
			{
				DifferentByBox.ResetText();
			}

			_searches.DifferentBy = DifferenceBox.ToRawInt();

			if (Focused)
			{
				DifferentByBox.Focus();
			}

			SetComparisonOperator(RamSearchEngine.ComparisonOperator.DifferentBy);
		}

		private void DifferentByBox_TextChanged(object sender, EventArgs e)
		{
			_searches.DifferentBy = !string.IsNullOrWhiteSpace(DifferentByBox.Text) ? DifferentByBox.ToRawInt() : null;
			WatchListView.Refresh();
		}

		#endregion

		#region ListView Events

		private void WatchListView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete && !e.Control && !e.Alt && !e.Shift)
			{
				RemoveAddresses();
			}
			else if (e.KeyCode == Keys.C && e.Control && !e.Alt && !e.Shift) // Copy
			{
				if (SelectedIndices.Count > 0)
				{
					var sb = new StringBuilder();
					foreach (var index in SelectedIndices)
					{
						foreach (ColumnHeader column in WatchListView.Columns)
						{
							sb.Append(GetColumnValue(column.Name, index)).Append('\t');
						}

						sb.Remove(sb.Length - 1, 1);
						sb.AppendLine();
					}

					if (sb.Length > 0)
					{
						Clipboard.SetDataObject(sb.ToString());
					}
				}
			}
			else if (e.KeyCode == Keys.Escape && !e.Control && !e.Alt && !e.Shift)
			{
				WatchListView.SelectedIndices.Clear();
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

		private void WatchListView_ColumnReordered(object sender, ColumnReorderedEventArgs e)
		{
			Global.Config.RamSearchColumnIndexes[WatchList.ADDRESS] = WatchListView.Columns[WatchList.ADDRESS].DisplayIndex;
			Global.Config.RamSearchColumnIndexes[WatchList.VALUE] = WatchListView.Columns[WatchList.VALUE].DisplayIndex;
			Global.Config.RamSearchColumnIndexes[WatchList.PREV] = WatchListView.Columns[WatchList.ADDRESS].DisplayIndex;
			Global.Config.RamSearchColumnIndexes[WatchList.CHANGES] = WatchListView.Columns[WatchList.CHANGES].DisplayIndex;
			Global.Config.RamSearchColumnIndexes[WatchList.DIFF] = WatchListView.Columns[WatchList.DIFF].DisplayIndex;
		}

		private void WatchListView_Enter(object sender, EventArgs e)
		{
			WatchListView.Refresh();
		}

		private void WatchListView_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			var column = WatchListView.Columns[e.Column];
			if (column.Name != _sortedColumn)
			{
				_sortReverse = false;
			}

			_searches.Sort(column.Name, _sortReverse);

			_sortedColumn = column.Name;
			_sortReverse ^= true;
			WatchListView.Refresh();
		}

		private void WatchListView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (SelectedIndices.Count > 0)
			{
				AddToRamWatch();
			}
		}

		#endregion

		#region Dialog Events

		private void NewRamSearch_Activated(object sender, EventArgs e)
		{
			WatchListView.Refresh();
		}

		private void NewRamSearch_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		private void NewRamSearch_DragDrop(object sender, DragEventArgs e)
		{
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (Path.GetExtension(filePaths[0]) == ".wch")
			{
				var file = new FileInfo(filePaths[0]);
				if (file.Exists)
				{
					LoadWatchFile(file, false);
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
