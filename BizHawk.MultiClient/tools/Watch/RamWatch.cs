using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace BizHawk.MultiClient
{
	public partial class RamWatch : Form
	{
		public const string ADDRESS = "AddressColumn";
		public const string VALUE = "ValueColumn";
		public const string PREV = "PrevColumn";
		public const string CHANGES = "ChangesColumn";
		public const string DIFF = "DiffColumn";
		public const string DOMAIN = "DomainColumn";
		public const string NOTES = "NotesColumn";

		private readonly Dictionary<string, int> DefaultColumnWidths = new Dictionary<string, int>
			{
			{ ADDRESS, 60 },
			{ VALUE, 59 },
			{ PREV, 59 },
			{ CHANGES, 55 },
			{ DIFF, 59 },
			{ DOMAIN, 55 },
			{ NOTES, 128 },
		};

		private int defaultWidth;
		private int defaultHeight;
		private readonly WatchList Watches = new WatchList(Global.Emulator.MainMemory);
		private string _sortedColumn = "";
		private bool _sortReverse = false;

		public RamWatch()
		{
			InitializeComponent();
			WatchListView.QueryItemText += WatchListView_QueryItemText;
			WatchListView.QueryItemBkColor += WatchListView_QueryItemBkColor;
			WatchListView.VirtualMode = true;
			Closing += (o, e) => SaveConfigSettings();
			_sortedColumn = "";
			_sortReverse = false;

			TopMost = Global.Config.RamWatchAlwaysOnTop;
		}

		public void UpdateValues()
		{
			if ((!IsHandleCreated || IsDisposed) && !Global.Config.DisplayRamWatch)
			{
				return;
			}

			if (Watches.Any())
			{
				Watches.UpdateValues();

				if (Global.Config.DisplayRamWatch)
				{
					for (int x = 0; x < Watches.Count; x++)
					{
						bool alert = Watches[x].IsSeparator ? false : Global.CheatList.IsActiveCheat(Watches[x].Domain, Watches[x].Address.Value);
						Global.OSD.AddGUIText(
							Watches[x].ToString(),
							Global.Config.DispRamWatchx,
							(Global.Config.DispRamWatchy + (x * 14)),
							alert,
							Color.Black,
							Color.White,
							0
						);
					}
				}


				if (!IsHandleCreated || IsDisposed) return;

				WatchListView.BlazingFast = true;
				WatchListView.Refresh();
				WatchListView.BlazingFast = false;
			}
		}

		public void Restart()
		{
			if ((!IsHandleCreated || IsDisposed) && !Global.Config.DisplayRamWatch)
			{
				return;
			}

			if (!String.IsNullOrWhiteSpace(Watches.CurrentFileName))
			{
				Watches.Reload();
			}
			else
			{
				NewWatchList(true);
			}
		}

		public void AddWatch(Watch watch)
		{
			Watches.Add(watch);
			DisplayWatches();
			UpdateValues();
			UpdateWatchCount();
			Changes();
		}

		public void LoadWatchFile(FileInfo file, bool append)
		{
			if (file != null)
			{
				bool result = true;
				if (Watches.Changes)
				{
					result = AskSave();
				}

				if (result)
				{
					Watches.Load(file.FullName, append);
					DisplayWatches();
					UpdateMessageLabel();
					UpdateWatchCount();
					Global.Config.RecentWatches.Add(Watches.CurrentFileName);
					SetMemoryDomain(GetDomainPos(Watches.Domain.ToString()));
				}
			}
		}

		private int GetDomainPos(string name)
		{
			//Attempts to find the memory domain by name, if it fails, it defaults to index 0
			for (int i = 0; i < Global.Emulator.MemoryDomains.Count; i++)
			{
				if (Global.Emulator.MemoryDomains[i].Name == name)
				{
					return i;
				}
			}
			return 0;
		}

		public List<int> AddressList
		{
			get
			{
				return Watches.Where(x => !x.IsSeparator).Select(x => x.Address.Value).ToList();
			}
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

		public void SaveConfigSettings()
		{
			SaveColumnInfo();
			Global.Config.RamWatchWndx = Location.X;
			Global.Config.RamWatchWndy = Location.Y;
			Global.Config.RamWatchWidth = Right - Left;
			Global.Config.RamWatchHeight = Bottom - Top;
		}

		private void SaveColumnInfo()
		{
			if (WatchListView.Columns[ADDRESS] != null)
			{
				Global.Config.RamWatchColumnIndexes[ADDRESS] = WatchListView.Columns[ADDRESS].DisplayIndex;
				Global.Config.RamWatchColumnWidths[ADDRESS] = WatchListView.Columns[ADDRESS].Width;
			}

			if (WatchListView.Columns[VALUE] != null)
			{
				Global.Config.RamWatchColumnIndexes[VALUE] = WatchListView.Columns[VALUE].DisplayIndex;
				Global.Config.RamWatchColumnWidths[VALUE] = WatchListView.Columns[VALUE].Width;
			}

			if (WatchListView.Columns[PREV] != null)
			{
				Global.Config.RamWatchColumnIndexes[PREV] = WatchListView.Columns[PREV].DisplayIndex;
				Global.Config.RamWatchColumnWidths[PREV] = WatchListView.Columns[PREV].Width;
			}

			if (WatchListView.Columns[CHANGES] != null)
			{
				Global.Config.RamWatchColumnIndexes[CHANGES] = WatchListView.Columns[CHANGES].DisplayIndex;
				Global.Config.RamWatchColumnWidths[CHANGES] = WatchListView.Columns[CHANGES].Width;
			}

			if (WatchListView.Columns[DIFF] != null)
			{
				Global.Config.RamWatchColumnIndexes[DIFF] = WatchListView.Columns[DIFF].DisplayIndex;
				Global.Config.RamWatchColumnWidths[DIFF] = WatchListView.Columns[DIFF].Width;
			}

			if (WatchListView.Columns[DOMAIN] != null)
			{
				Global.Config.RamWatchColumnIndexes[DOMAIN] = WatchListView.Columns[DOMAIN].DisplayIndex;
				Global.Config.RamWatchColumnWidths[DOMAIN] = WatchListView.Columns[DOMAIN].Width;
			}

			if (WatchListView.Columns[NOTES] != null)
			{
				Global.Config.RamWatchColumnIndexes[NOTES] = WatchListView.Columns[NOTES].Index;
				Global.Config.RamWatchColumnWidths[NOTES] = WatchListView.Columns[NOTES].Width;
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (!AskSave())
				e.Cancel = true;
			base.OnClosing(e);
		}

		private int GetColumnWidth(string columnName)
		{
			var width = Global.Config.RamWatchColumnWidths[columnName];
			if (width == -1)
			{
				width = DefaultColumnWidths[columnName];
			}

			return width;
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

		private void WatchListView_QueryItemText(int index, int column, out string text)
		{
			text = "";

			if (index >= Watches.ItemCount || Watches[index].IsSeparator)
			{
				return;
			}
			string columnName = WatchListView.Columns[column].Name;

			switch (columnName)
			{
				case ADDRESS:
					text = Watches[index].AddressString;
					break;
				case VALUE:
					text = Watches[index].ValueString;
					break;
				case PREV:
					text = Watches[index].PreviousStr;
					break;
				case CHANGES:
					if (!Watches[index].IsSeparator)
					{
						text = Watches[index].ChangeCount.ToString();
					}
					break;
				case DIFF:
					text = Watches[index].Diff;
					break;
				case DOMAIN:
					text = Watches[index].Domain.Name;
					break;
				case NOTES:
					text = Watches[index].Notes;
					break;
			}
		}

		private void DisplayWatches()
		{
			WatchListView.ItemCount = Watches.ItemCount;
		}

		private void UpdateWatchCount()
		{
			WatchCountLabel.Text = Watches.WatchCount.ToString() + (Watches.WatchCount == 1 ? " watch" : " watches");
		}

		private void SetPlatformAndMemoryDomainLabel()
		{
			MemDomainLabel.Text = Global.Emulator.SystemId + " " + Watches.Domain.Name;
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
				UpdateMessageLabel();
				_sortReverse = false;
				_sortedColumn = String.Empty;
			}
		}

		public void LoadFileFromRecent(string path)
		{
			bool ask_result = true;
			if (Watches.Changes)
			{
				ask_result = AskSave();
			}

			if (ask_result)
			{
				bool load_result = Watches.Load(path, append: false);
				if (!load_result)
				{
					Global.Config.RecentWatches.HandleLoadError(path);
				}
				else
				{
					Global.Config.RecentWatches.Add(path);
					DisplayWatches();
					UpdateWatchCount();
					UpdateMessageLabel();
					Watches.Changes = false;
				}
			}
		}

		private void UpdateMessageLabel(bool saved = false)
		{
			string message = String.Empty;
			if (!String.IsNullOrWhiteSpace(Watches.CurrentFileName))
			{
				if (saved)
				{
					message = Path.GetFileName(Watches.CurrentFileName) + " saved.";
				}
				else
				{
					message = Path.GetFileName(Watches.CurrentFileName) + (Watches.Changes ? " *" : String.Empty);
				}
			}

			MessageLabel.Text = message;
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

		private void SelectAll()
		{
			for (int i = 0; i < Watches.Count; i++)
			{
				WatchListView.SelectItem(i, true);
			}
		}

		private void Changes()
		{
			Watches.Changes = true;
			UpdateMessageLabel();
		}

		private void MoveUp()
		{
			ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
			if (indexes.Count == 0 || indexes[0] == 0)
			{
				return;
			}

			foreach (int index in indexes)
			{
				var watch = Watches[index];
				Watches.Remove(Watches[index]);
				Watches.Insert(index - 1, watch);

				//Note: here it will get flagged many times redundantly potentially, 
				//but this avoids it being flagged falsely when the user did not select an index
				Changes();
			}

			var indices = new List<int>();
			for (int i = 0; i < indexes.Count; i++)
			{
				indices.Add(indexes[i] - 1);
			}

			WatchListView.SelectedIndices.Clear();
			foreach (int t in indices)
			{
				WatchListView.SelectItem(t, true);
			}

			DisplayWatches();
		}

		private void MoveDown()
		{
			var indexes = WatchListView.SelectedIndices;
			if (indexes.Count == 0)
			{
				return;
			}

			foreach (int index in indexes)
			{
				var watch = Watches[index];

				if (index < Watches.Count - 1)
				{
					Watches.Remove(Watches[index]);
					Watches.Insert(index + 1, watch);
				}

				//Note: here it will get flagged many times redundantly potentially, 
				//but this avoids it being flagged falsely when the user did not select an index
				Changes();
			}

			var indices = new List<int>();
			for (int i = 0; i < indexes.Count; i++)
			{
				indices.Add(indexes[i] + 1);
			}

			WatchListView.SelectedIndices.Clear();
			foreach (int t in indices)
			{
				WatchListView.SelectItem(t, true);
			}

			DisplayWatches();
		}

		private void InsertSeparator()
		{
			var indexes = WatchListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				Watches.Insert(indexes[0], SeparatorWatch.Instance);
			}
			else
			{
				Watches.Add(SeparatorWatch.Instance);
			}
			DisplayWatches();
			Changes();
			UpdateWatchCount();
		}

		private Point GetPromptPoint()
		{
			return PointToScreen(new Point(WatchListView.Location.X, WatchListView.Location.Y));
		}

		private void AddNewWatch()
		{
			WatchEditor we = new WatchEditor
			{
				InitialLocation = GetPromptPoint()
			};
			we.SetWatch(Watches.Domain);
			Global.Sound.StopSound();
			we.ShowDialog();
			Global.Sound.StartSound();

			if (we.DialogResult == DialogResult.OK)
			{
				Watches.Add(we.Watches[0]);
				Changes();
				UpdateWatchCount();
				DisplayWatches();
			}
		}

		private void EditWatch(bool duplicate = false)
		{
			var indexes = WatchListView.SelectedIndices;

			if (indexes.Count > 0)
			{
				WatchEditor we = new WatchEditor
				{
					InitialLocation = GetPromptPoint(),
				};

				if (!SelectedWatches.Any())
				{
					return;
				}

				we.SetWatch(Watches.Domain, SelectedWatches, duplicate ? WatchEditor.Mode.Duplicate : WatchEditor.Mode.Edit);
				Global.Sound.StopSound();
				var result = we.ShowDialog();
				if (result == DialogResult.OK)
				{
					Changes();
					if (duplicate)
					{
						Watches.AddRange(we.Watches);
						DisplayWatches();
					}
					else
					{
						for (int i = 0; i < we.Watches.Count; i++)
						{
							Watches[indexes[i]] = we.Watches[i];
						}
					}
				}

				Global.Sound.StartSound();
				UpdateValues();
			}
		}

		private void PokeAddress()
		{
			if (SelectedWatches.Any())
			{
				RamPoke poke = new RamPoke
					{
						InitialLocation = GetPromptPoint()
					};

				if (SelectedWatches.Any())
				{
					poke.SetWatch(SelectedWatches);
				}

				Global.Sound.StopSound();
				var result = poke.ShowDialog();
				if (result == DialogResult.OK)
				{
					UpdateValues();
				}
				Global.Sound.StartSound();
			}
		}

		private List<Watch> SelectedWatches
		{
			get
			{
				var selected = new List<Watch>();
				ListView.SelectedIndexCollection indices = WatchListView.SelectedIndices;
				if (indices.Count > 0)
				{
					foreach (int index in indices)
					{
						if (!Watches[index].IsSeparator)
						{
							selected.Add(Watches[index]);
						}
					}
				}
				return selected;
			}
		}

		private void ColumnPositions()
		{
			List<KeyValuePair<string, int>> Columns = 
				Global.Config.RamWatchColumnIndexes
					.Where(x => WatchListView.Columns.ContainsKey(x.Key))
					.OrderBy(x => x.Value).ToList();

			for (int i = 0; i < Columns.Count; i++)
			{
				if (WatchListView.Columns.ContainsKey(Columns[i].Key))
				{
					WatchListView.Columns[Columns[i].Key].DisplayIndex = i;
				}
			}
		}

		private void LoadConfigSettings()
		{
			//Size and Positioning
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

			LoadColumnInfo();
		}

		private void LoadColumnInfo()
		{
			WatchListView.Columns.Clear();
			ToolHelpers.AddColumn(WatchListView, ADDRESS, true, GetColumnWidth(ADDRESS));
			ToolHelpers.AddColumn(WatchListView, VALUE, true, GetColumnWidth(VALUE));
			ToolHelpers.AddColumn(WatchListView, PREV, Global.Config.RamWatchShowPrevColumn, GetColumnWidth(PREV));
			ToolHelpers.AddColumn(WatchListView, CHANGES, Global.Config.RamWatchShowChangeColumn, GetColumnWidth(CHANGES));
			ToolHelpers.AddColumn(WatchListView, DIFF, Global.Config.RamWatchShowDiffColumn, GetColumnWidth(DIFF));
			ToolHelpers.AddColumn(WatchListView, DOMAIN, Global.Config.RamWatchShowDomainColumn, GetColumnWidth(DOMAIN));
			ToolHelpers.AddColumn(WatchListView, NOTES, true, GetColumnWidth(NOTES));

			ColumnPositions();
		}

		private void RemoveWatch()
		{
			var indexes = WatchListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				foreach (int index in indexes)
				{
					Watches.Remove(Watches[indexes[0]]); //index[0] used since each iteration will make this the correct list index
				}
				indexes.Clear();
				DisplayWatches();
			}
			UpdateValues();
			UpdateWatchCount();
		}

		private string GetColumnValue(string name, int index)
		{
			switch (name)
			{
				default:
					return String.Empty;
				case ADDRESS:
					return Watches[index].AddressString;
				case VALUE:
					return Watches[index].ValueString;
				case PREV:
					return Watches[index].PreviousStr;
				case CHANGES:
					return Watches[index].ChangeCount.ToString();
				case DIFF:
					return Watches[index].Diff;
				case DOMAIN:
					return Watches[index].Domain.Name;
				case NOTES:
					return Watches[index].Notes;
			}
		}

		private void CopyWatchesToClipBoard()
		{
			var indexes = WatchListView.SelectedIndices;

			if (indexes.Count > 0)
			{
				StringBuilder sb = new StringBuilder();
				foreach (int index in indexes)
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

		private void OrderColumn(int index)
		{
			var column = WatchListView.Columns[index];
			if (column.Name != _sortedColumn)
			{
				_sortReverse = false;
			}

			Watches.OrderWatches(column.Name, _sortReverse);

			_sortedColumn = column.Name;
			_sortReverse ^= true;
			WatchListView.Refresh();
		}

		#region Winform Events

		private void NewRamWatch_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
		}

		private void NewRamWatch_Activated(object sender, EventArgs e)
		{
			WatchListView.Refresh();
		}

		private void NewRamWatch_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		private void NewRamWatch_DragDrop(object sender, DragEventArgs e)
		{
			string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (Path.GetExtension(filePaths[0]) == (".wch"))
			{
				Watches.Load(filePaths[0], append:false);
				DisplayWatches();
			}
		}

		private void NewRamWatch_Enter(object sender, EventArgs e)
		{
			WatchListView.Focus();
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
			LoadWatchFile(WatchList.GetFileFromUser(Watches.CurrentFileName), append);
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Watches.Save())
			{
				UpdateMessageLabel(saved: true);
			}
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			bool result = Watches.SaveAs();
			if (result)
			{
				UpdateMessageLabel(saved: true);
				Global.Config.RecentWatches.Add(Watches.CurrentFileName);
			}
		}

		private void recentToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			recentToolStripMenuItem.DropDownItems.Clear();
			recentToolStripMenuItem.DropDownItems.AddRange(Global.Config.RecentWatches.GenerateRecentMenu(LoadFileFromRecent));
			recentToolStripMenuItem.DropDownItems.Add(Global.Config.RecentWatches.GenerateAutoLoadItem());
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

		/*************Watches***********************/
		private void watchesToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
			if (indexes.Count > 0)
			{
				editWatchToolStripMenuItem.Enabled = true;
				duplicateWatchToolStripMenuItem.Enabled = true;
				removeWatchToolStripMenuItem.Enabled = true;
				moveUpToolStripMenuItem.Enabled = true;
				moveDownToolStripMenuItem.Enabled = true;
				pokeAddressToolStripMenuItem.Enabled = true;
				freezeAddressToolStripMenuItem.Enabled = true;
			}
			else
			{
				editWatchToolStripMenuItem.Enabled = false;
				duplicateWatchToolStripMenuItem.Enabled = false;
				removeWatchToolStripMenuItem.Enabled = false;
				moveUpToolStripMenuItem.Enabled = false;
				moveDownToolStripMenuItem.Enabled = false;
				pokeAddressToolStripMenuItem.Enabled = false;
				freezeAddressToolStripMenuItem.Enabled = false;
			}
		}

		private void memoryDomainsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			memoryDomainsToolStripMenuItem.DropDownItems.Clear();
			memoryDomainsToolStripMenuItem.DropDownItems.AddRange(ToolHelpers.GenerateMemoryDomainMenuItems(SetMemoryDomain, Watches.Domain.Name));
		}

		private void newWatchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			AddNewWatch();
		}

		private void editWatchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			EditWatch();
		}

		private void removeWatchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RemoveWatch();
		}

		private void duplicateWatchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			EditWatch(duplicate: true);
		}

		private void pokeAddressToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PokeAddress();
		}

		private void freezeAddressToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToolHelpers.FreezeAddress(SelectedWatches);
		}

		private void insertSeparatorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			InsertSeparator();
		}

		private void clearChangeCountsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Watches.ClearChangeCounts();
		}

		private void moveUpToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MoveUp();
		}

		private void moveDownToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MoveDown();
		}

		private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SelectAll();
		}

		/*************Columns***********************/
		private void ColumnsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ShowPreviousMenuItem.Checked = Global.Config.RamWatchShowPrevColumn;
			ShowChangesMenuItem.Checked = Global.Config.RamWatchShowChangeColumn;
			ShowDiffMenuItem.Checked = Global.Config.RamWatchShowDiffColumn;
			ShowDomainMenuItem.Checked = Global.Config.RamWatchShowDomainColumn;
		}

		private void showPreviousValueToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamWatchShowPrevColumn ^= true;
			SaveColumnInfo();
			LoadColumnInfo();
		}

		private void showChangeCountsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamWatchShowChangeColumn ^= true;

			SaveColumnInfo();
			LoadColumnInfo();
		}

		private void diffToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamWatchShowDiffColumn ^= true;

			SaveColumnInfo();
			LoadColumnInfo();
		}

		private void domainToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamWatchShowDomainColumn ^= true;

			SaveColumnInfo();
			LoadColumnInfo();
		}

		/*************Options***********************/
		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			displayWatchesOnScreenToolStripMenuItem.Checked = Global.Config.DisplayRamWatch;
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.RamWatchSaveWindowPosition;
			AlwaysOnTopMenuItem.Checked = Global.Config.RamWatchAlwaysOnTop;
		}

		private void definePreviousValueAsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			lastChangeToolStripMenuItem.Checked = false;
			previousFrameToolStripMenuItem.Checked = false;
			originalToolStripMenuItem.Checked = false;

			switch (Global.Config.RamWatchDefinePrevious)
			{
				default:
				case Watch.PreviousType.LastFrame:
					previousFrameToolStripMenuItem.Checked = true;
					break;
				case Watch.PreviousType.LastChange:
					lastChangeToolStripMenuItem.Checked = true;
					break;
				case Watch.PreviousType.Original:
					originalToolStripMenuItem.Checked = true;
					break;
			}
		}

		private void previousFrameToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamWatchDefinePrevious = Watch.PreviousType.LastFrame;
		}

		private void lastChangeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamWatchDefinePrevious = Watch.PreviousType.LastChange;
		}

		private void originalToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamWatchDefinePrevious = Watch.PreviousType.Original;
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

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamWatchAlwaysOnTop ^= true;
			TopMost = Global.Config.RamWatchAlwaysOnTop;
		}

		private void restoreWindowSizeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(defaultWidth, defaultHeight);

			Global.Config.RamWatchColumnIndexes = new Dictionary<string, int>
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

			Global.Config.RamWatchShowChangeColumn = true;
			Global.Config.RamWatchShowDomainColumn = true;
			Global.Config.RamWatchShowPrevColumn = false;
			Global.Config.RamWatchShowDiffColumn = false;

			WatchListView.Columns[ADDRESS].Width = DefaultColumnWidths[ADDRESS];
			WatchListView.Columns[VALUE].Width = DefaultColumnWidths[VALUE];
			//WatchListView.Columns[PREV].Width = DefaultColumnWidths[PREV];
			WatchListView.Columns[CHANGES].Width = DefaultColumnWidths[CHANGES];
			//WatchListView.Columns[DIFF].Width = DefaultColumnWidths[DIFF];
			WatchListView.Columns[DOMAIN].Width = DefaultColumnWidths[DOMAIN];
			WatchListView.Columns[NOTES].Width = DefaultColumnWidths[NOTES];

			Global.Config.DisplayRamWatch = false;
			Global.Config.RamWatchSaveWindowPosition = true;
			Global.Config.RamWatchAlwaysOnTop = TopMost = false;

			LoadColumnInfo();
		}

		/*************Context Menu***********************/
		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			ListView.SelectedIndexCollection indexes = WatchListView.SelectedIndices;
			EditContextMenuItem.Visible =
				RemoveContextMenuItem.Visible =
				DuplicateContextMenuItem.Visible =
				PokeContextMenuItem.Visible =
				FreezeContextMenuItem.Visible =
				Separator6.Visible =
				InsertSeperatorContextMenuItem.Visible =
				MoveUpContextMenuItem.Visible =
				MoveDownContextMenuItem.Visible =
				Separator6.Visible = 
				toolStripSeparator4.Visible = 
				indexes.Count > 0;


			bool allCheats = true;
			foreach (int i in indexes)
			{
				if (!Watches[i].IsSeparator)
				{
					if (!Global.CheatList.IsActiveCheat(Watches[i].Domain, Watches[i].Address.Value))
					{
						allCheats = false;
					}
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

			ShowChangeCountsContextMenuItem.Text = Global.Config.RamWatchShowChangeColumn ? "Hide change counts" : "Show change counts";
			ShowPreviousValueContextMenuItem.Text = Global.Config.RamWatchShowPrevColumn ? "Hide previous value" : "Show previous value";
			ShowDiffContextMenuItem.Text = Global.Config.RamWatchShowDiffColumn ? "Hide difference value" : "Show difference value";
			ShowDomainContextMenuItem.Text = Global.Config.RamWatchShowDomainColumn ? "Hide domain" : "Show domain";

			UnfreezeAllContextMenuItem.Visible = Global.CheatList.HasActiveCheats;

			ViewInHexEditorContextMenuItem.Visible = SelectedWatches.Count == 1;
		}

		private void UnfreezeAllContextMenuItem_Click(object sender, EventArgs e)
		{
			Global.MainForm.Cheats1.RemoveAllCheats();
			UpdateValues();
			Global.MainForm.RamSearch1.UpdateValues();
			Global.MainForm.HexEditor1.UpdateValues();
			Global.MainForm.Cheats_UpdateValues();
		}

		private void ViewInHexEditorContextMenuItem_Click(object sender, EventArgs e)
		{
			var selected = SelectedWatches;
			if (selected.Any())
			{
				Global.MainForm.LoadHexEditor();

				if (selected.Select(x => x.Domain).Distinct().Count() > 1)
				{
					ToolHelpers.ViewInHexEditor(selected[0].Domain, new List<int> { selected.First().Address.Value });
				}
				else
				{
					ToolHelpers.ViewInHexEditor(selected[0].Domain, selected.Select(x => x.Address.Value));
				}
			}
		}

		/*************ListView Events***********************/

		private void WatchListView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete && !e.Control && !e.Alt && !e.Shift)
			{
				RemoveWatch();
			}
			else if (e.KeyCode == Keys.A && e.Control && !e.Alt && !e.Shift) //Select All
			{
				for (int x = 0; x < Watches.Count; x++)
				{
					WatchListView.SelectItem(x, true);
				}
			}
			else if (e.KeyCode == Keys.C && e.Control && !e.Alt && !e.Shift) //Copy
			{
				CopyWatchesToClipBoard();
			}
			else if (e.KeyCode == Keys.Enter && !e.Control && !e.Alt && !e.Shift) //Enter
			{
				EditWatch();
			}
		}

		private void WatchListView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			EditWatch();
		}

		private void WatchListView_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			OrderColumn(e.Column);
		}

		private void WatchListView_ColumnReordered(object sender, ColumnReorderedEventArgs e)
		{
			Global.Config.RamWatchColumnIndexes[ADDRESS] = WatchListView.Columns[ADDRESS].DisplayIndex;
			Global.Config.RamWatchColumnIndexes[VALUE] = WatchListView.Columns[VALUE].DisplayIndex;
			Global.Config.RamWatchColumnIndexes[PREV] = WatchListView.Columns[ADDRESS].DisplayIndex;
			Global.Config.RamWatchColumnIndexes[CHANGES] = WatchListView.Columns[CHANGES].DisplayIndex;
			Global.Config.RamWatchColumnIndexes[DIFF] = WatchListView.Columns[DIFF].DisplayIndex;
			Global.Config.RamWatchColumnIndexes[DOMAIN] = WatchListView.Columns[DOMAIN].DisplayIndex;
			Global.Config.RamWatchColumnIndexes[NOTES] = WatchListView.Columns[NOTES].DisplayIndex;
		}

		#endregion
	}
}
