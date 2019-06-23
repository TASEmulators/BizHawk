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

using BizHawk.Common.StringExtensions;
using BizHawk.Common.NumberExtensions;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;
using BizHawk.Client.EmuHawk.ToolExtensions;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// A form designed to search through ram values
	/// </summary>
	public partial class RamSearch : ToolFormBase, IToolForm
	{
		// TODO: DoSearch grabs the state of widgets and passes it to the engine before running, so rip out code that is attempting to keep the state up to date through change events
		private string _currentFileName = "";

		private RamSearchEngine _searches;
		private RamSearchEngine.Settings _settings;

		private int _defaultWidth;
		private int _defaultHeight;
		private string _sortedColumn = "";
		private bool _sortReverse;
		private bool _forcePreviewClear;
		private bool _autoSearch;

		private bool _dropdownDontfire; // Used as a hack to get around lame .net dropdowns, there's no way to set their index without firing the selectedindexchanged event!

		private const int MaxDetailedSize = 1024 * 1024; // 1mb, semi-arbituary decision, sets the size to check for and automatically switch to fast mode for the user
		private const int MaxSupportedSize = 1024 * 1024 * 64; // 64mb, semi-arbituary decision, sets the maximum size RAM Search will support (as it will crash beyond this)

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

			_sortedColumn = "";
			_sortReverse = false;

			Settings = new RamSearchSettings();
		}

		[RequiredService]
		public IMemoryDomains MemoryDomains { get; set; }

		[RequiredService]
		public IEmulator Emu { get; set; }

		[OptionalService]
		public IInputPollable InputPollableCore { get; set; }

		[ConfigPersist]
		public RamSearchSettings Settings { get; set; }

		public bool AskSaveChanges()
		{
			return true;
		}

		public bool UpdateBefore => false;

		private void HardSetDisplayTypeDropDown(BizHawk.Client.Common.DisplayType type)
		{
			foreach (var item in DisplayTypeDropdown.Items)
			{
				if (Watch.DisplayTypeToString(type) == item.ToString())
				{
					DisplayTypeDropdown.SelectedItem = item;
				}
			}
		}

		private void HardSetSizeDropDown(WatchSize size)
		{
			switch (size)
			{
				case WatchSize.Byte:
					SizeDropdown.SelectedIndex = 0;
					break;
				case WatchSize.Word:
					SizeDropdown.SelectedIndex = 1;
					break;
				case WatchSize.DWord:
					SizeDropdown.SelectedIndex = 2;
					break;
			}
		}

		private void ColumnToggleCallback()
		{
			SaveColumnInfo(WatchListView, Settings.Columns);
			LoadColumnInfo(WatchListView, Settings.Columns);
		}

		private void RamSearch_Load(object sender, EventArgs e)
		{
			TopMost = Settings.TopMost;

			RamSearchMenu.Items.Add(Settings.Columns.GenerateColumnsMenu(ColumnToggleCallback));

			_settings = new RamSearchEngine.Settings(MemoryDomains);
			_searches = new RamSearchEngine(_settings, MemoryDomains);

			ErrorIconButton.Visible = false;
			_dropdownDontfire = true;
			LoadConfigSettings();
			SpecificValueBox.ByteSize = _settings.Size;
			SpecificValueBox.Type = _settings.Type;
			DifferentByBox.Type = Common.DisplayType.Unsigned;
			DifferenceBox.Type = Common.DisplayType.Unsigned;

			MessageLabel.Text = "";
			SpecificAddressBox.MaxLength = (MemoryDomains.MainMemory.Size - 1).NumHexDigits();
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

		private void OutOfRangeCheck()
		{
			ErrorIconButton.Visible = _searches.OutOfRangeAddress.Any();
		}

		private void ListView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (column == 0)
			{
				if (_searches.Count > 0 && column == 0)
				{
					var nextColor = Color.White;

					var isCheat = Global.CheatList.IsActive(_settings.Domain, _searches[index].Address);
					var isWeeded = Settings.PreviewMode && !_forcePreviewClear && _searches.Preview(_searches[index].Address);

					if (_searches[index].Address >= _searches[index].Domain.Size)
					{
						nextColor = Color.PeachPuff;
					}
					else if (isCheat)
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
		}

		private void ListView_QueryItemText(int index, int column, out string text)
		{
			text = "";

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

			if (Settings.UseWindowPosition && IsOnScreen(Settings.TopLeft))
			{
				Location = Settings.WindowPosition;
			}

			if (Settings.UseWindowSize)
			{
				Size = Settings.WindowSize;
			}

			TopMost = Settings.TopMost;

			LoadColumnInfo(WatchListView, Settings.Columns);
		}

		#endregion

		#region Public

		/// <summary>
		/// This should be called anytime the search list changes
		/// </summary>
		private void UpdateList()
		{
			WatchListView.ItemCount = _searches.Count;
			SetTotal();
		}

		public void NewUpdate(ToolFormUpdateType type)
		{
		}

		/// <summary>
		/// This should only be called when the values of the list need an update such as after a poke or emulation occurred
		/// </summary>
		public void UpdateValues()
		{
			if (_searches.Count > 0)
			{
				_searches.Update();

				if (_autoSearch)
				{
					if (InputPollableCore != null && Settings.AutoSearchTakeLagFramesIntoAccount && InputPollableCore.IsLagFrame)
					{
						// Do nothing
					}
					else
					{
						DoSearch();
					}
				}

				_forcePreviewClear = false;
				WatchListView.BlazingFast = true;
				WatchListView.Invalidate();
				WatchListView.BlazingFast = false;
			}
		}

		public void FastUpdate()
		{
			if (_searches.Count > 0)
			{
				_searches.Update();

				if (_autoSearch)
				{
					DoSearch();
				}
			}
		}

		public void Restart()
		{
			_settings = new RamSearchEngine.Settings(MemoryDomains);
			_searches = new RamSearchEngine(_settings, MemoryDomains);
			MessageLabel.Text = "Search restarted";
			DoDomainSizeCheck();
			NewSearch();
			SetSize(_settings.Size);
			HardSetDisplayTypeDropDown(_settings.Type);
		}

		private void SaveConfigSettings()
		{
			SaveColumnInfo(WatchListView, Settings.Columns);

			if (WindowState == FormWindowState.Normal)
			{
				Settings.Wndx = Location.X;
				Settings.Wndy = Location.Y;
				Settings.Width = Right - Left;
				Settings.Height = Bottom - Top;
			}
		}

		public void NewSearch()
		{
			var compareTo = _searches.CompareTo;
			var compareVal = _searches.CompareValue;
			var differentBy = _searches.DifferentBy;

			_searches = new RamSearchEngine(_settings, MemoryDomains, compareTo, compareVal, differentBy);
			_searches.Start();
			if (Settings.AlwaysExcludeRamWatch)
			{
				RemoveRamWatchesFromList();
			}

			UpdateList();
			ToggleSearchDependentToolBarItems();
			SetReboot(false);
			MessageLabel.Text = "";
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

		private void ToggleSearchDependentToolBarItems()
		{
			DoSearchToolButton.Enabled =
				CopyValueToPrevToolBarItem.Enabled =
				_searches.Count > 0;

			UpdateUndoToolBarButtons();
			OutOfRangeCheck();

			PokeAddressToolBarItem.Enabled =
				FreezeAddressToolBarItem.Enabled =
				SelectedIndices.Any() &&
				_searches.Domain.CanPoke();
		}

		private long? CompareToValue
		{
			get
			{
				if (PreviousValueRadio.Checked)
				{
					return null;
				}

				if (SpecificValueRadio.Checked)
				{
					return (long)SpecificValueBox.ToRawInt() & 0x00000000FFFFFFFF;
				}

				if (SpecificAddressRadio.Checked)
				{
					return SpecificAddressBox.ToRawInt();
				}

				if (NumberOfChangesRadio.Checked)
				{
					return NumberOfChangesBox.ToRawInt();
				}

				if (DifferenceRadio.Checked)
				{
					return DifferenceBox.ToRawInt();
				}

				return null;
			}
		}

		private int? DifferentByValue => DifferentByRadio.Checked ? DifferentByBox.ToRawInt() : null;

		private RamSearchEngine.ComparisonOperator Operator
		{
			get
			{
				if (NotEqualToRadio.Checked)
				{
					return RamSearchEngine.ComparisonOperator.NotEqual;
				}

				if (LessThanRadio.Checked)
				{
					return RamSearchEngine.ComparisonOperator.LessThan;
				}

				if (GreaterThanRadio.Checked)
				{
					return RamSearchEngine.ComparisonOperator.GreaterThan;
				}

				if (LessThanOrEqualToRadio.Checked)
				{
					return RamSearchEngine.ComparisonOperator.LessThanEqual;
				}

				if (GreaterThanOrEqualToRadio.Checked)
				{
					return RamSearchEngine.ComparisonOperator.GreaterThanEqual;
				}

				if (DifferentByRadio.Checked)
				{
					return RamSearchEngine.ComparisonOperator.DifferentBy;
				}

				return RamSearchEngine.ComparisonOperator.Equal;
			}
		}

		private RamSearchEngine.Compare Compare
		{
			get
			{
				if (SpecificValueRadio.Checked)
				{
					return RamSearchEngine.Compare.SpecificValue;
				}

				if (SpecificAddressRadio.Checked)
				{
					return RamSearchEngine.Compare.SpecificAddress;
				}

				if (NumberOfChangesRadio.Checked)
				{
					return RamSearchEngine.Compare.Changes;
				}

				if (DifferenceRadio.Checked)
				{
					return RamSearchEngine.Compare.Difference;
				}

				return RamSearchEngine.Compare.Previous;
			}
		}

		public void DoSearch()
		{
			_searches.CompareValue = CompareToValue;
			_searches.DifferentBy = DifferentByValue;
			_searches.Operator = Operator;
			_searches.CompareTo = Compare;

			var removed = _searches.DoSearch();
			UpdateList();
			SetRemovedMessage(removed);
			ToggleSearchDependentToolBarItems();
			_forcePreviewClear = true;
		}

		private IEnumerable<int> SelectedIndices => WatchListView.SelectedIndices.Cast<int>();

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
			MessageLabel.Text = $"{val} {(val == 1 ? "address" : "addresses")} removed";
		}

		private void SetTotal()
		{
			TotalSearchLabel.Text = $"{_searches.Count:n0} addresses";
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
				Settings.RecentSearches.HandleLoadError(path);
			}
			else
			{
				LoadWatchFile(file, append: false);
			}
		}

		private void SetMemoryDomain(string name)
		{
			_settings.Domain = MemoryDomains[name];
			SetReboot(true);
			SpecificAddressBox.MaxLength = (_settings.Domain.Size - 1).NumHexDigits();
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

		private void DoDisplayTypeClick(Client.Common.DisplayType type)
		{
			if (_settings.Type != type)
			{
				if (!string.IsNullOrEmpty(SpecificValueBox.Text))
				{
					SpecificValueBox.Text = "0";
				}

				if (!string.IsNullOrEmpty(DifferenceBox.Text))
				{
					DifferenceBox.Text = "0";
				}

				if (!string.IsNullOrEmpty(DifferentByBox.Text))
				{
					DifferentByBox.Text = "0";
				}
			}

			SpecificValueBox.Type = _settings.Type = type;
			DifferenceBox.Type = type;
			DifferentByBox.Type = type;
			_searches.SetType(type);

			_dropdownDontfire = true;
			DisplayTypeDropdown.SelectedItem = Watch.DisplayTypeToString(type);
			_dropdownDontfire = false;
			WatchListView.Refresh();
		}

		private void SetPreviousStype(PreviousType type)
		{
			_settings.PreviousType = type;
			_searches.SetPreviousType(type);
		}

		private void SetSize(WatchSize size)
		{
			_settings.Size = size;
			SpecificValueBox.ByteSize = size;
			if (!string.IsNullOrEmpty(SpecificAddressBox.Text))
			{
				SpecificAddressBox.Text = "0";
			}

			if (!string.IsNullOrEmpty(SpecificValueBox.Text))
			{
				SpecificValueBox.Text = "0";
			}

			bool isTypeCompatible = false;
			switch (size)
			{
				case WatchSize.Byte:
					isTypeCompatible = ByteWatch.ValidTypes.Any(t => t == _settings.Type);
					SizeDropdown.SelectedIndex = 0;
					break;

				case WatchSize.Word:
					isTypeCompatible = WordWatch.ValidTypes.Any(t => t == _settings.Type);
					SizeDropdown.SelectedIndex = 1;
					break;

				case WatchSize.DWord:
					isTypeCompatible = DWordWatch.ValidTypes.Any(t => t == _settings.Type);
					SizeDropdown.SelectedIndex = 2;
					break;
			}

			if (!isTypeCompatible)
			{
				_settings.Type = Client.Common.DisplayType.Unsigned;
			}

			_dropdownDontfire = true;

			PopulateTypeDropDown();
			_dropdownDontfire = false;
			SpecificValueBox.Type = _settings.Type;
			SetReboot(true);
			NewSearch();
		}

		private void PopulateTypeDropDown()
		{
			var previous = DisplayTypeDropdown.SelectedItem?.ToString() ?? "";
			var next = "";

			DisplayTypeDropdown.Items.Clear();

			IEnumerable<Client.Common.DisplayType> types = null;
			switch (_settings.Size)
			{
				case WatchSize.Byte:
					types = ByteWatch.ValidTypes;
					break;

				case WatchSize.Word:
					types = WordWatch.ValidTypes;
					break;

				case WatchSize.DWord:
					types = DWordWatch.ValidTypes;
					break;
			}
			
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
			WatchListView.Columns[WatchList.CHANGES].Width = Settings.Columns[WatchList.CHANGES].Width;
			SetReboot(true);
		}

		private void SetToFastMode()
		{
			_settings.Mode = RamSearchEngine.Settings.SearchMode.Fast;

			if (_settings.PreviousType == PreviousType.LastFrame || _settings.PreviousType == PreviousType.LastChange)
			{
				SetPreviousStype(PreviousType.LastSearch);
			}

			NumberOfChangesRadio.Enabled = false;
			NumberOfChangesBox.Enabled = false;
			NumberOfChangesBox.Text = "";
			ClearChangeCountsToolBarItem.Enabled = false;

			if (NumberOfChangesRadio.Checked || DifferenceRadio.Checked)
			{
				PreviousValueRadio.Checked = true;
			}

			Settings.Columns[WatchList.CHANGES].Width = WatchListView.Columns[WatchList.CHANGES].Width;
			WatchListView.Columns[WatchList.CHANGES].Width = 0;
			SetReboot(true);
		}

		private void RemoveAddresses()
		{
			var indices = SelectedIndices.ToList();
			if (indices.Any())
			{
				SetRemovedMessage(indices.Count);
				_searches.RemoveRange(indices);

				UpdateList();
				WatchListView.SelectedIndices.Clear();
				ToggleSearchDependentToolBarItems();
			}
		}

		private void LoadWatchFile(FileInfo file, bool append, bool truncate = false)
		{
			if (file != null)
			{
				if (!truncate)
				{
					_currentFileName = file.FullName;
				}

				var watches = new WatchList(MemoryDomains, Emu.SystemId);
				watches.Load(file.FullName, append);
				Settings.RecentSearches.Add(watches.CurrentFileName);

				var watchList = watches.Where(x => !x.IsSeparator);
				var addresses = watchList.Select(x => x.Address).ToList();

				if (truncate)
				{
					SetRemovedMessage(addresses.Count);
					_searches.RemoveSmallWatchRange(watchList);
				}
				else
				{
					_searches.AddRange(addresses, append);
					MessageLabel.Text = $"{file.Name} loaded";
				}

				UpdateList();
				Settings.RecentSearches.Add(file.FullName);

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
				if (Settings.AlwaysExcludeRamWatch)
				{
					RemoveRamWatchesFromList();
				}
			}
		}

		private void PokeAddress()
		{
			if (SelectedIndices.Any())
			{
				var poke = new RamPoke
				{
					InitialLocation = this.ChildPointToScreen(WatchListView)
				};

				poke.SetWatch(SelectedIndices.Select(t => _searches[t]));
				poke.ShowHawkDialog();

				UpdateList();
			}
		}

		private void RemoveRamWatchesFromList()
		{
			if (GlobalWin.Tools.Has<RamWatch>())
			{
				_searches.RemoveSmallWatchRange(GlobalWin.Tools.RamWatch.Watches);
				UpdateList();
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
					return "";
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

		private void GoToSpecifiedAddress()
		{
			WatchListView.SelectedIndices.Clear();
			var prompt = new InputPrompt
			{
				Text = "Go to Address",
				StartLocation = this.ChildPointToScreen(WatchListView),
				Message = "Enter a hexadecimal value"
			};

			while (prompt.ShowHawkDialog() == DialogResult.OK)
			{
				try
				{
					var addr = int.Parse(prompt.PromptText, NumberStyles.HexNumber);
					for (int index = 0; index < _searches.Count; index++)
					{
						if (_searches[index].Address == addr)
						{
							WatchListView.SelectItem(index, true);
							WatchListView.ensureVisible();
							return; // Don't re-show dialog on success
						}
					}
					//TODO add error text to dialog?
					// Re-show dialog if the address isn't found
				}
				catch (FormatException e)
				{
					// Re-show dialog if given invalid text (shouldn't happen)
				}
				catch (OverflowException e)
				{
					//TODO add error text to dialog?
					// Re-show dialog if the address isn't valid
				}
			}
		}

		#endregion

		public class RamSearchSettings : ToolDialogSettings
		{
			public RamSearchSettings()
			{
				Columns = new ColumnList
				{
					new Column { Name = WatchList.ADDRESS, Visible = true, Index = 0, Width = 60 },
					new Column { Name = WatchList.VALUE, Visible = true, Index = 1, Width = 59 },
					new Column { Name = WatchList.PREV, Visible = true, Index = 2, Width = 59 },
					new Column { Name = WatchList.CHANGES, Visible = true, Index = 3, Width = 55 },
					new Column { Name = WatchList.DIFF, Visible = false, Index = 4, Width = 59 },
				};

				PreviewMode = true;
				RecentSearches = new RecentFiles(8);
				AutoSearchTakeLagFramesIntoAccount = true;
			}

			public ColumnList Columns { get; }
			public bool PreviewMode { get; set; }
			public bool AlwaysExcludeRamWatch { get; set; }
			public bool AutoSearchTakeLagFramesIntoAccount { get; set; }

			public RecentFiles RecentSearches { get; set; }
		}

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
				Settings.RecentSearches.RecentMenu(LoadFileFromRecent));
		}

		private void OpenMenuItem_Click(object sender, EventArgs e)
		{
			LoadWatchFile(
				GetWatchFileFromUser(""),
				sender == AppendFileMenuItem,
				sender == TruncateFromFileMenuItem);
		}

		private void SaveMenuItem_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(_currentFileName))
			{
				var watches = new WatchList(MemoryDomains, Emu.SystemId) { CurrentFileName = _currentFileName };
				for (var i = 0; i < _searches.Count; i++)
				{
					watches.Add(_searches[i]);
				}

				if (!string.IsNullOrWhiteSpace(watches.CurrentFileName))
				{
					if (watches.Save())
					{
						_currentFileName = watches.CurrentFileName;
						MessageLabel.Text = $"{Path.GetFileName(_currentFileName)} saved";
						Settings.RecentSearches.Add(watches.CurrentFileName);
					}
				}
				else
				{
					var result = watches.SaveAs(GetWatchSaveFileFromUser(watches.CurrentFileName));
					if (result)
					{
						MessageLabel.Text = $"{Path.GetFileName(_currentFileName)} saved";
						Settings.RecentSearches.Add(watches.CurrentFileName);
					}
				}
			}
		}

		private void SaveAsMenuItem_Click(object sender, EventArgs e)
		{
			var watches = new WatchList(MemoryDomains, Emu.SystemId) { CurrentFileName = _currentFileName };
			for (var i = 0; i < _searches.Count; i++)
			{
				watches.Add(_searches[i]);
			}

			if (watches.SaveAs(GetWatchSaveFileFromUser(watches.CurrentFileName)))
			{
				_currentFileName = watches.CurrentFileName;
				MessageLabel.Text = $"{Path.GetFileName(_currentFileName)} saved";
				Settings.RecentSearches.Add(watches.CurrentFileName);
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
			MemoryDomainsSubMenu.DropDownItems.AddRange(
				MemoryDomains.MenuItems(SetMemoryDomain, _searches.Domain.Name, MaxSupportedSize)
				.ToArray());
		}

		private void SizeSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ByteMenuItem.Checked = _settings.Size == WatchSize.Byte;
			WordMenuItem.Checked = _settings.Size == WatchSize.Word;
			DWordMenuItem.Checked = _settings.Size == WatchSize.DWord;
		}

		private void DisplayTypeSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DisplayTypeSubMenu.DropDownItems.Clear();

			IEnumerable<Client.Common.DisplayType> types = null;
			switch (_settings.Size)
			{
				case WatchSize.Byte:
					types = ByteWatch.ValidTypes;
					break;

				case WatchSize.Word:
					types = WordWatch.ValidTypes;
					break;

				case WatchSize.DWord:
					types = DWordWatch.ValidTypes;
					break;
			}

			foreach (var type in types)
			{
				var item = new ToolStripMenuItem
					{
						Name = $"{type}ToolStripMenuItem",
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
			Previous_LastChangeMenuItem.Checked = false;

			switch (_settings.PreviousType)
			{
				default:
				case PreviousType.LastSearch:
					Previous_LastSearchMenuItem.Checked = true;
					break;
				case PreviousType.LastFrame:
					PreviousFrameMenuItem.Checked = true;
					break;
				case PreviousType.Original:
					Previous_OriginalMenuItem.Checked = true;
					break;
				case PreviousType.LastChange:
					Previous_LastChangeMenuItem.Checked = true;
					break;
			}

			PreviousFrameMenuItem.Enabled = _settings.Mode != RamSearchEngine.Settings.SearchMode.Fast;
			Previous_LastChangeMenuItem.Enabled = _settings.Mode != RamSearchEngine.Settings.SearchMode.Fast;
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
			SetSize(WatchSize.Byte);
		}

		private void WordMenuItem_Click(object sender, EventArgs e)
		{
			SetSize(WatchSize.Word);
		}

		private void DWordMenuItem_Click_Click(object sender, EventArgs e)
		{
			SetSize(WatchSize.DWord);
		}

		private void CheckMisalignedMenuItem_Click(object sender, EventArgs e)
		{
			_settings.CheckMisAligned ^= true;
			SetReboot(true);
		}

		private void Previous_LastFrameMenuItem_Click(object sender, EventArgs e)
		{
			SetPreviousStype(PreviousType.LastFrame);
		}

		private void Previous_LastSearchMenuItem_Click(object sender, EventArgs e)
		{
			SetPreviousStype(PreviousType.LastSearch);
		}

		private void Previous_OriginalMenuItem_Click(object sender, EventArgs e)
		{
			SetPreviousStype(PreviousType.Original);
		}

		private void Previous_LastChangeMenuItem_Click(object sender, EventArgs e)
		{
			SetPreviousStype(PreviousType.LastChange);
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
				SelectedIndices.Any();

			PokeAddressMenuItem.Enabled =
				FreezeAddressMenuItem.Enabled =
				SelectedIndices.Any() &&
				SelectedWatches.All(w => w.Domain.CanPoke());

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
				int restoredCount = _searches.Undo();
				UpdateList();
				ToggleSearchDependentToolBarItems();
				_forcePreviewClear = true;
				UpdateUndoToolBarButtons();
				MessageLabel.Text = $"{restoredCount} {(restoredCount == 1 ? "address" : "addresses")} restored";
			}
		}

		private void RedoMenuItem_Click(object sender, EventArgs e)
		{
			if (_searches.CanRedo)
			{
				int restoredCount = _searches.Redo();
				UpdateList();
				ToggleSearchDependentToolBarItems();
				_forcePreviewClear = true;
				UpdateUndoToolBarButtons();
				MessageLabel.Text = $"{restoredCount} {(restoredCount == 1 ? "address" : "addresses")} removed";
			}
		}

		private void CopyValueToPrevMenuItem_Click(object sender, EventArgs e)
		{
			_searches.SetPreviousToCurrent();
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
			var allCheats = SelectedWatches.All(x => Global.CheatList.IsActive(x.Domain, x.Address));
			if (allCheats)
			{
				SelectedWatches.UnfreezeAll();
			}
			else
			{
				SelectedWatches.FreezeAll();
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
			AutoloadDialogMenuItem.Checked = Settings.AutoLoad;
			SaveWinPositionMenuItem.Checked = Settings.SaveWindowPosition;
			ExcludeRamWatchMenuItem.Checked = Settings.AlwaysExcludeRamWatch;
			UseUndoHistoryMenuItem.Checked = _searches.UndoEnabled;
			PreviewModeMenuItem.Checked = Settings.PreviewMode;
			AlwaysOnTopMenuItem.Checked = Settings.TopMost;
			FloatingWindowMenuItem.Checked = Settings.FloatingWindow;
			AutoSearchMenuItem.Checked = _autoSearch;
			AutoSearchAccountForLagMenuItem.Checked = Settings.AutoSearchTakeLagFramesIntoAccount;
		}

		private void PreviewModeMenuItem_Click(object sender, EventArgs e)
		{
			Settings.PreviewMode ^= true;
		}

		private void AutoSearchMenuItem_Click(object sender, EventArgs e)
		{
			_autoSearch ^= true;
			AutoSearchCheckBox.Checked = _autoSearch;
			DoSearchToolButton.Enabled =
				SearchButton.Enabled =
				!_autoSearch;
		}

		private void AutoSearchAccountForLagMenuItem_Click(object sender, EventArgs e)
		{
			Settings.AutoSearchTakeLagFramesIntoAccount ^= true;
		}

		private void ExcludeRamWatchMenuItem_Click(object sender, EventArgs e)
		{
			Settings.AlwaysExcludeRamWatch ^= true;
			if (Settings.AlwaysExcludeRamWatch)
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
			Settings.AutoLoad ^= true;
		}

		private void SaveWinPositionMenuItem_Click(object sender, EventArgs e)
		{
			Settings.SaveWindowPosition ^= true;
		}

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			TopMost = Settings.TopMost ^= true;
		}

		private void FloatingWindowMenuItem_Click(object sender, EventArgs e)
		{
			Settings.FloatingWindow ^= true;
			RefreshFloatingWindowControl(Settings.FloatingWindow);
		}

		private void RestoreDefaultsMenuItem_Click(object sender, EventArgs e)
		{
			var recentFiles = Settings.RecentSearches; // We don't want to wipe recent files when restoring

			Settings = new RamSearchSettings { RecentSearches = recentFiles };

			Size = new Size(_defaultWidth, _defaultHeight);

			RamSearchMenu.Items.Remove(
				RamSearchMenu.Items
					.OfType<ToolStripMenuItem>()
					.First(x => x.Name == "GeneratedColumnsSubMenu"));

			RamSearchMenu.Items.Add(Settings.Columns.GenerateColumnsMenu(ColumnToggleCallback));


			_settings = new RamSearchEngine.Settings(MemoryDomains);
			if (_settings.Mode == RamSearchEngine.Settings.SearchMode.Fast)
			{
				SetToFastMode();
			}

			RefreshFloatingWindowControl(Settings.FloatingWindow);
			LoadColumnInfo(WatchListView, Settings.Columns);
		}

		#endregion

		#region ContextMenu and Toolbar

		private void ListViewContextMenu_Opening(object sender, CancelEventArgs e)
		{
			DoSearchContextMenuItem.Enabled = _searches.Count > 0;

			RemoveContextMenuItem.Visible =
				AddToRamWatchContextMenuItem.Visible =
				FreezeContextMenuItem.Visible =
				ContextMenuSeparator2.Visible =
				ViewInHexEditorContextMenuItem.Visible =
				SelectedIndices.Any();

			PokeContextMenuItem.Enabled =
				FreezeContextMenuItem.Visible =
				SelectedIndices.Any() &&
				SelectedWatches.All(w => w.Domain.CanPoke());

			UnfreezeAllContextMenuItem.Visible = Global.CheatList.ActiveCount > 0;

			ContextMenuSeparator3.Visible = SelectedIndices.Any() || (Global.CheatList.ActiveCount > 0);

			var allCheats = true;
			foreach (var index in SelectedIndices)
			{
				if (!Global.CheatList.IsActive(_settings.Domain, _searches[index].Address))
				{
					allCheats = false;
				}
			}

			if (allCheats)
			{
				FreezeContextMenuItem.Text = "&Unfreeze Address";
				FreezeContextMenuItem.Image = Properties.Resources.Unfreeze;
			}
			else
			{
				FreezeContextMenuItem.Text = "&Freeze Address";
				FreezeContextMenuItem.Image = Properties.Resources.Freeze;
			}
		}

		private void UnfreezeAllContextMenuItem_Click(object sender, EventArgs e)
		{
			Global.CheatList.RemoveAll();
		}

		private void ViewInHexEditorContextMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedWatches.Any())
			{
				ViewInHexEditor(_searches.Domain, SelectedWatches.Select(x => x.Address), SelectedSize);
			}
		}

		private void ClearPreviewContextMenuItem_Click(object sender, EventArgs e)
		{
			_forcePreviewClear = true;
			WatchListView.Refresh();
		}

		private WatchSize SelectedSize
		{
			get
			{
				switch (SizeDropdown.SelectedIndex)
				{
					default:
					case 0:
						return WatchSize.Byte;
					case 1:
						return WatchSize.Word;
					case 2:
						return WatchSize.DWord;
				}
			}
		}

		private void SizeDropdown_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_dropdownDontfire)
			{
				return;
			}

			SetSize(SelectedSize);
		}

		private void DisplayTypeDropdown_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!_dropdownDontfire)
			{
				DoDisplayTypeClick(Watch.StringToDisplayType(DisplayTypeDropdown.SelectedItem.ToString()));
			}
		}

		private void ErrorIconButton_Click(object sender, EventArgs e)
		{
			var outOfRangeAddresses = _searches.OutOfRangeAddress.ToList();

			SetRemovedMessage(outOfRangeAddresses.Count);

			UpdateList();
			ToggleSearchDependentToolBarItems();
		}

		private void CopyWatchesToClipBoard()
		{
			if (SelectedItems.Any())
			{
				var sb = new StringBuilder();
				foreach (var watch in SelectedItems)
				{
					sb.AppendLine(watch.ToString());
				}

				if (sb.Length > 0)
				{
					Clipboard.SetDataObject(sb.ToString());
				}
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
				CopyWatchesToClipBoard();
			}
			else if (e.KeyCode == Keys.Escape && !e.Control && !e.Alt && !e.Shift)
			{
				WatchListView.SelectedIndices.Clear();
			}
		}

		private void WatchListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (WatchListView.SelectAllInProgress)
			{
				return;
			}

			RemoveToolBarItem.Enabled =
				AddToRamWatchToolBarItem.Enabled =
				SelectedIndices.Any();

			PokeAddressToolBarItem.Enabled =
				FreezeAddressToolBarItem.Enabled =
				SelectedIndices.Any() &&
				_searches.Domain.CanPoke();
		}

		private void WatchListView_VirtualItemsSelectionRangeChanged(object sender, ListViewVirtualItemsSelectionRangeChangedEventArgs e)
		{
			WatchListView_SelectedIndexChanged(sender, e);
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
			if (SelectedIndices.Any())
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
			RefreshFloatingWindowControl(Settings.FloatingWindow);
			base.OnShown(e);
		}

		// Stupid designer
		protected void DragEnterWrapper(object sender, DragEventArgs e)
		{
			base.GenericDragEnter(sender, e);
		}

		#endregion

		#endregion
	}
}
