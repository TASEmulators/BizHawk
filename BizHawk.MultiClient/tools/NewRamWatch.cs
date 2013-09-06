using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace BizHawk.MultiClient
{
	public partial class NewRamWatch : Form
	{
        private int defaultWidth;
        private int defaultHeight;
        private WatchList Watches = new WatchList();
		private string systemID = "NULL";
		private string sortedCol = "";
		private bool sortReverse = false;

		public NewRamWatch()
		{
			InitializeComponent();
			WatchListView.QueryItemText += WatchListView_QueryItemText;
			WatchListView.QueryItemBkColor += WatchListView_QueryItemBkColor;
			WatchListView.VirtualMode = true;
            Closing += (o, e) => SaveConfigSettings();
            sortedCol = "";
            sortReverse = false;
		}

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!AskSave())
                e.Cancel = true;
            base.OnClosing(e);
        }

		private void WatchListView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (index >= Watches.ItemCount)
			{
				return;
			}

			if (column == 0)
			{
				if (Watches[index].IsSeparator)
				{
					color = BackColor;
				}
				else if (Global.CheatList.IsActiveCheat(Watches.Domain, Watches[index].Address.Value))
				{
					color = Color.LightCyan;
				}
			}
		}

		void WatchListView_QueryItemText(int index, int column, out string text)
		{
			text = "";

			if (index >= Watches.ItemCount || Watches[index].IsSeparator)
			{
				return;
			}

			switch (column)
			{
				case 0: // address
					text = Watches[index].AddressString;
					break;
				case 1: // value
					text = Watches[index].ValueString;
					break;
				case 2: // prev
					if (Watches[index] is iWatchEntryDetails)
					{
						text = (Watches[index] as iWatchEntryDetails).PreviousStr;
					}
					break;
				case 3: // changes
					if (Watches[index] is iWatchEntryDetails)
					{
						text = (Watches[index] as iWatchEntryDetails).ChangeCount.ToString();
					}
					break;
				case 4: // diff
					if (Watches[index] is iWatchEntryDetails)
					{
						text = (Watches[index] as iWatchEntryDetails).Diff;
					}
					break;
				case 5: // domain
					text = Watches[index].DomainName;
					break;
				case 6: // notes
					if (Watches[index] is iWatchEntryDetails)
					{
						text = (Watches[index] as iWatchEntryDetails).Notes;
					}
					break;
			}
		}

		public void UpdateValues()
		{
			if ((!IsHandleCreated || IsDisposed) && !Global.Config.DisplayRamWatch)
			{
				return;
			}
			Watches.UpdateValues();

			if (Global.Config.DisplayRamWatch)
			{
				/* TODO
				for (int x = 0; x < Watches.Count; x++)
				{
					bool alert = Global.CheatList.IsActiveCheat(Domain, Watches[x].Address);
					Global.OSD.AddGUIText(Watches[x].ToString(),
						Global.Config.DispRamWatchx, (Global.Config.DispRamWatchy + (x * 14)), alert, Color.Black, Color.White, 0);
				}
				*/
			}

			if (!IsHandleCreated || IsDisposed) return;

			WatchListView.BlazingFast = true;
			WatchListView.Refresh();
			WatchListView.BlazingFast = false;
		}

		public bool AskSave()
		{
			if (Global.Config.SupressAskSave) //User has elected to not be nagged
			{
				return true;
			}

			if (Watches.Changes)
			{
				Global.Sound.StopSound();
				DialogResult result = MessageBox.Show("Save Changes?", "Ram Watch", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
				Global.Sound.StartSound();
				if (result == DialogResult.Yes)
				{
					Watches.Save();
				}
				else if (result == DialogResult.No)
				{
					return true;
				}
				else if (result == DialogResult.Cancel)
				{
					return false;
				}
			}

			return true;
		}

		private void DisplayWatches()
		{
			WatchListView.ItemCount = Watches.ItemCount;
		}

		private void UpdateWatchCount()
		{
			int count = Watches.WatchCount;
			WatchCountLabel.Text = count.ToString() + (count == 1 ? " watch" : " watches");
		}

		private void SetMemoryDomain(int pos)
		{
			if (pos < Global.Emulator.MemoryDomains.Count)  //Sanity check
			{
				Watches.Domain = Global.Emulator.MemoryDomains[pos];
			}
			SetPlatformAndMemoryDomainLabel();
			Update();
		}

		private void SetPlatformAndMemoryDomainLabel()
		{
			string memoryDomain = Watches.Domain.ToString();
			systemID = Global.Emulator.SystemId;
			MemDomainLabel.Text = systemID + " " + memoryDomain;
		}

		private void NewWatchList(bool suppressAsk)
		{
			bool result = true;
			if (Watches.Changes)
			{
				result = AskSave();
			}

			if (result || suppressAsk)
			{
				Watches.Clear();
				DisplayWatches();
				UpdateWatchCount();
				MessageLabel.Text = "";
				sortReverse = false;
				sortedCol = "";
			}
		}

		public void LoadWatchFromRecent(string file)
		{
			bool ask_result = true;
			if (Watches.Changes)
			{
				ask_result = AskSave();
			}

			if (ask_result)
			{
				bool load_result = Watches.Load(file, details: true, append: false);
				if (!load_result)
				{
					DialogResult result = MessageBox.Show("Could not open " + file + "\nRemove from list?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
					if (result == DialogResult.Yes)
						Global.Config.RecentWatches.Remove(file);
				}

				DisplayWatches();
				Watches.Changes = false;
			}
		}

        public void SaveConfigSettings()
        {
            Global.Config.RamWatchAddressWidth = WatchListView.Columns[Global.Config.RamWatchAddressIndex].Width;
            Global.Config.RamWatchValueWidth = WatchListView.Columns[Global.Config.RamWatchValueIndex].Width;
            Global.Config.RamWatchPrevWidth = WatchListView.Columns[Global.Config.RamWatchPrevIndex].Width;
            Global.Config.RamWatchChangeWidth = WatchListView.Columns[Global.Config.RamWatchChangeIndex].Width;
            Global.Config.RamWatchDiffWidth = WatchListView.Columns[Global.Config.RamWatchDiffIndex].Width;
            Global.Config.RamWatchDomainWidth = WatchListView.Columns[Global.Config.RamWatchDomainIndex].Width;
            Global.Config.RamWatchNotesWidth = WatchListView.Columns[Global.Config.RamWatchNotesIndex].Width;

            Global.Config.RamWatchWndx = Location.X;
            Global.Config.RamWatchWndy = Location.Y;
            Global.Config.RamWatchWidth = Right - Left;
            Global.Config.RamWatchHeight = Bottom - Top;
        }

		#region Winform Events

        private void NewRamWatch_Load(object sender, EventArgs e)
        {
            defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
            defaultHeight = Size.Height;

            if (Global.Config.RamWatchSaveWindowPosition && Global.Config.RamWatchWndx >= 0 && Global.Config.RamWatchWndy >= 0)
            {
                Location = new Point(Global.Config.RamWatchWndx, Global.Config.RamWatchWndy);
            }

            if (Global.Config.RamWatchWidth >= 0 && Global.Config.RamWatchHeight >= 0)
            {
                Size = new Size(Global.Config.RamWatchWidth, Global.Config.RamWatchHeight);
            }
        }

        /*************File***********************/
        private void filesToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            saveToolStripMenuItem.Enabled = Watches.Changes;
        }

		private void newListToolStripMenuItem_Click(object sender, EventArgs e)
		{
			NewWatchList(false);
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
            bool append = sender == appendFileToolStripMenuItem;
            var file = WatchList.GetFileFromUser(Watches.CurrentFileName);
			if (file != null)
			{
				bool result = true;
				if (Watches.Changes)
				{
					result = AskSave();
				}

				if (result)
				{
					Watches.Load(file.FullName, true, append);
					DisplayWatches();
                    MessageLabel.Text = Path.GetFileNameWithoutExtension(Watches.CurrentFileName) + (Watches.Changes ? " *" : String.Empty);
					UpdateWatchCount();
					Global.Config.RecentWatches.Add(Watches.CurrentFileName);
					SetMemoryDomain(WatchCommon.GetDomainPos(Watches.Domain.ToString()));
				}
			}
		}

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool result = Watches.Save();
            if (result)
            {
                MessageLabel.Text = Path.GetFileName(Watches.CurrentFileName) + " saved.";
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool result = Watches.SaveAs();
            if (result)
            {
                MessageLabel.Text = Path.GetFileName(Watches.CurrentFileName) + " saved.";
                Global.Config.RecentWatches.Add(Watches.CurrentFileName);
            }
        }

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!AskSave())
			{
				return;
			}
			else
			{
				Close();
			}
		}

        /*************Options***********************/
        private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            displayWatchesOnScreenToolStripMenuItem.Checked = Global.Config.DisplayRamWatch;
            saveWindowPositionToolStripMenuItem.Checked = Global.Config.RamWatchSaveWindowPosition;
        }

        private void displayWatchesOnScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.DisplayRamWatch ^= true;

            if (!Global.Config.DisplayRamWatch)
            {
                Global.OSD.ClearGUIText();
            }
            else
            {
                UpdateValues();
            }
        }

        private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.RamWatchSaveWindowPosition ^= true;
        }

        private void restoreWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Size = new Size(defaultWidth, defaultHeight);

            Global.Config.RamWatchAddressIndex = 0;
            Global.Config.RamWatchValueIndex = 1;
            Global.Config.RamWatchPrevIndex = 2;
            Global.Config.RamWatchChangeIndex = 3;
            Global.Config.RamWatchDiffIndex = 4;
            Global.Config.RamWatchNotesIndex = 5;

            showPreviousValueToolStripMenuItem.Checked = false;
            Global.Config.RamWatchShowPrevColumn = false;
            showChangeCountsToolStripMenuItem.Checked = true;
            Global.Config.RamWatchShowChangeColumn = true;
            Global.Config.RamWatchShowDiffColumn = false;
            Global.Config.RamWatchShowDomainColumn = true;
            WatchListView.Columns[0].Width = 60;
            WatchListView.Columns[1].Width = 59;
            WatchListView.Columns[2].Width = 0;
            WatchListView.Columns[3].Width = 55;
            WatchListView.Columns[4].Width = 0;
            WatchListView.Columns[5].Width = 55;
            WatchListView.Columns[6].Width = 128;
            Global.Config.DisplayRamWatch = false;
            Global.Config.RamWatchSaveWindowPosition = true;
        }

		#endregion
	}
}
