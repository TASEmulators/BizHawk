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

		private RamSearchEngine Searches = new RamSearchEngine(Global.Emulator.MainMemory);

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
		}

		private void RamSearch_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
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
		#endregion

		#region Public

		public void UpdateValues()
		{
			Searches.Update();
			WatchListView.Refresh();
		}

		public void SaveConfigSettings()
		{
			//TODO
		}

		public void Restart()
		{
			//TODO
		}

		#endregion

		#region Private

		private void NewSearch()
		{
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
				Searches = new RamSearchEngine(Global.Emulator.MemoryDomains[pos]); //We have to start a new search
				Searches.Start();
			}

			SetPlatformAndMemoryDomainLabel();
			Update();
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

		#endregion

		#region Winform Events

		/*************File***********************/
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

		/*************Search***********************/
		private void SearchSubMenu_DropDownOpened(object sender, EventArgs e)
		{

		}

		private void NewSearchMenuMenuItem_Click(object sender, EventArgs e)
		{
			NewSearch();
		}

		/*************Options***********************/
		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AutoloadDialogMenuItem.Checked = Global.Config.RecentSearches.AutoLoad;
		}

		private void MemoryDomainsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			MemoryDomainsSubMenu.DropDownItems.Clear();
			MemoryDomainsSubMenu.DropDownItems.AddRange(ToolHelpers.GenerateMemoryDomainMenuItems(SetMemoryDomain, Searches.DomainName));
		}

		private void AutoloadDialogMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RecentSearches.AutoLoad ^= true;
		}

		#endregion
	}
}
