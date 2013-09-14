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
		RamSearchEngine Searches = new RamSearchEngine(Global.Emulator.MainMemory);

		#region Initialize, Load, and Save
		
		public NewRamSearch()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			InitializeComponent();
			SearchListView.QueryItemText += ListView_QueryItemText;
			SearchListView.QueryItemBkColor += ListView_QueryItemBkColor;
			SearchListView.VirtualMode = true;
			Closing += (o, e) => SaveConfigSettings();
		}

		private void RamSearch_Load(object sender, EventArgs e)
		{

		}

		private void ListView_QueryItemBkColor(int index, int column, ref Color color)
		{
			//TODO
		}

		private void ListView_QueryItemText(int index, int column, out string text)
		{
			//TODO
			text = "";
		}
		#endregion

		#region Public

		public void SaveConfigSettings()
		{
			//TODO
		}

		#endregion

		#region Private

		private void NewSearch()
		{
			//TODO
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
			}

			SetPlatformAndMemoryDomainLabel();
			Update();
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
