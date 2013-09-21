using System;
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

		private RamSearchEngine Searches;
		private RamSearchEngine.Settings Settings;

		private int defaultWidth;       //For saving the default size of the dialog, so the user can restore if desired
		private int defaultHeight;

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

			Settings = new RamSearchEngine.Settings();
			Searches = new RamSearchEngine(Settings);
		}

		private void RamSearch_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
			SpecificValueBox.ByteSize = Settings.Size;
			SpecificValueBox.Type = Settings.Type;
		}

		private void ListView_QueryItemBkColor(int index, int column, ref Color color)
		{
			//TODO
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

		private void SaveConfigSettings()
		{
			//TODO
		}

		#endregion

		#region Public

		public void UpdateValues()
		{
			if (Searches.Count > 0)
			{
				Searches.Update();
				WatchListView.Refresh();
			}
		}

		public void Restart()
		{
			//TODO
		}

		#endregion

		#region Private

		private void NewSearch()
		{
			Searches = new RamSearchEngine(Settings);
			Searches.Start();
			SetTotal();
			WatchListView.ItemCount = Searches.Count;
		}

		private void SetTotal()
		{
			TotalSearchLabel.Text = String.Format("{0:n0}", Searches.Count) + " addresses";
		}

		private void LoadFileFromRecent(string path)
		{
			//bool load_result = Watches.Load(path, details: true, append: false);
			bool load_result = true; //TODO
			if (!load_result)
			{
				Global.Config.RecentSearches.HandleLoadError(path);
			}
			else
			{
				Global.Config.RecentSearches.Add(path);
				
				//TODO: update listview and refresh things
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

			LoadColumnInfo();
		}

		private void LoadColumnInfo()
		{
			WatchListView.Columns.Clear();
			AddColumn(ADDRESS, true); //TODO: make things configurable
			AddColumn(VALUE, true);
			AddColumn(PREV, true);
			AddColumn(CHANGES, true);
			AddColumn(DIFF, false);

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

		#endregion

		#region Winform Events

		#region File
		
		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{

		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(Global.Config.RecentSearches.GenerateRecentMenu(LoadFileFromRecent));
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
			Settings.Mode = RamSearchEngine.Settings.SearchMode.Detailed;
		}

		private void FastMenuItem_Click(object sender, EventArgs e)
		{
			Settings.Mode = RamSearchEngine.Settings.SearchMode.Fast;

			if (Settings.PreviousType == Watch.PreviousType.LastFrame || Settings.PreviousType == Watch.PreviousType.LastChange)
			{
				SetPreviousStype(Watch.PreviousType.LastSearch);
			}
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

		}

		private void NewSearchMenuMenuItem_Click(object sender, EventArgs e)
		{
			NewSearch();
		}

		private void SearchMenuItem_Click(object sender, EventArgs e)
		{
			Searches.DoSearch();
			SetTotal();
			WatchListView.ItemCount = Searches.Count;
		}

		#endregion

		#region Options

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AutoloadDialogMenuItem.Checked = Global.Config.RecentSearches.AutoLoad;
		}

		private void AutoloadDialogMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RecentSearches.AutoLoad ^= true;
		}

		#endregion

		#region Compare To Box

		private void PreviousValueRadio_Click(object sender, EventArgs e)
		{
			SpecificValueBox.Enabled = false;
			SpecificAddressBox.Enabled = false;
			NumberOfChangesBox.Enabled = false;
			SetCompareTo(RamSearchEngine.Compare.Previous);
		}

		private void SpecificValueRadio_Click(object sender, EventArgs e)
		{
			SpecificValueBox.Enabled = true;
			SpecificAddressBox.Enabled = false;
			NumberOfChangesBox.Enabled = false;
			SetCompareTo(RamSearchEngine.Compare.SpecificValue);
		}

		private void SpecificAddressRadio_Click(object sender, EventArgs e)
		{
			SpecificValueBox.Enabled = false;
			SpecificAddressBox.Enabled = true;
			NumberOfChangesBox.Enabled = false;
			SetCompareTo(RamSearchEngine.Compare.SpecificAddress);
		}

		private void NumberOfChangesRadio_Click(object sender, EventArgs e)
		{
			SpecificValueBox.Enabled = false;
			SpecificAddressBox.Enabled = false;
			NumberOfChangesBox.Enabled = true;
			SetCompareTo(RamSearchEngine.Compare.Changes);
		}

		private void DifferenceRadio_Click(object sender, EventArgs e)
		{
			SpecificValueBox.Enabled = false;
			SpecificAddressBox.Enabled = false;
			NumberOfChangesBox.Enabled = false;
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

		#endregion
	}
}
