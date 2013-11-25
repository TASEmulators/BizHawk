using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class RamWatch : Form, IToolForm
	{
		private readonly Dictionary<string, int> _defaultColumnWidths = new Dictionary<string, int>
			{
			{ WatchList.ADDRESS, 60 },
			{ WatchList.VALUE, 59 },
			{ WatchList.PREV, 59 },
			{ WatchList.CHANGES, 55 },
			{ WatchList.DIFF, 59 },
			{ WatchList.DOMAIN, 55 },
			{ WatchList.NOTES, 128 },
		};

		private int _defaultWidth;
		private int _defaultHeight;
		private string _sortedColumn = String.Empty;
		private bool _sortReverse;

		private readonly WatchList _watches = new WatchList(Global.Emulator.MemoryDomains.MainMemory);

		#region API

		public void AddWatch(Watch watch)
		{
			_watches.Add(watch);
			WatchListView.ItemCount = _watches.ItemCount;
			UpdateValues();
			UpdateWatchCount();
			Changes();
		}

		public bool AskSave()
		{
			if (Global.Config.SupressAskSave) //User has elected to not be nagged
			{
				return true;
			}

			if (_watches.Changes)
			{
				GlobalWin.Sound.StopSound();
				var result = MessageBox.Show("Save Changes?", "Ram Watch", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
				GlobalWin.Sound.StartSound();
				if (result == DialogResult.Yes)
				{
					_watches.Save();
				}
				else if (result == DialogResult.No)
				{
					_watches.Changes = false;
					return true;
				}
				else if (result == DialogResult.Cancel)
				{
					return false;
				}
			}

			return true;
		}

		public IEnumerable<int> AddressList
		{
			get
			{
				return _watches.Where(x => !x.IsSeparator).Select(x => x.Address ?? 0).ToList();
			}
		}

		public void LoadFileFromRecent(string path)
		{
			var ask_result = true;
			if (_watches.Changes)
			{
				ask_result = AskSave();
			}

			if (ask_result)
			{
				var load_result = _watches.Load(path, append: false);
				if (!load_result)
				{
					ToolHelpers.HandleLoadError(Global.Config.RecentWatches, path);
				}
				else
				{
					Global.Config.RecentWatches.Add(path);
					WatchListView.ItemCount = _watches.ItemCount;
					UpdateWatchCount();
					UpdateMessageLabel();
					_watches.Changes = false;
				}
			}
		}

		public void LoadWatchFile(FileInfo file, bool append)
		{
			if (file != null)
			{
				var result = true;
				if (_watches.Changes)
				{
					result = AskSave();
				}

				if (result)
				{
					_watches.Load(file.FullName, append);
					WatchListView.ItemCount = _watches.ItemCount;
					UpdateMessageLabel();
					UpdateWatchCount();
					Global.Config.RecentWatches.Add(_watches.CurrentFileName);
					SetMemoryDomain(_watches.Domain.ToString());
				}
			}
		}

		public RamWatch()
		{
			InitializeComponent();
			WatchListView.QueryItemText += WatchListView_QueryItemText;
			WatchListView.QueryItemBkColor += WatchListView_QueryItemBkColor;
			WatchListView.VirtualMode = true;
			Closing += (o, e) =>
			{
				if (AskSave())
				{
					SaveConfigSettings();
				}
				else
				{
					e.Cancel = true;
				}
			};
			_sortedColumn = String.Empty;
			_sortReverse = false;

			TopMost = Global.Config.RamWatchAlwaysOnTop;
		}

		public void Restart()
		{
			if ((!IsHandleCreated || IsDisposed) && !Global.Config.DisplayRamWatch)
			{
				return;
			}

			if (!String.IsNullOrWhiteSpace(_watches.CurrentFileName))
			{
				_watches.Reload();
			}
			else
			{
				NewWatchList(true);
			}
		}

		public bool UpdateBefore { get { return true; } }

		public void UpdateValues()
		{
			if ((!IsHandleCreated || IsDisposed) && !Global.Config.DisplayRamWatch)
			{
				return;
			}

			if (_watches.Any())
			{
				_watches.UpdateValues();

				if (Global.Config.DisplayRamWatch)
				{
					for (var x = 0; x < _watches.Count; x++)
					{
						var alert = !_watches[x].IsSeparator && Global.CheatList.IsActive(_watches[x].Domain, (_watches[x].Address ?? 0));
						GlobalWin.OSD.AddGUIText(
							_watches[x].ToString(),
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

		#endregion

		#region Private Methods

		private void AddNewWatch()
		{
			var we = new WatchEditor
			{
				InitialLocation = GetPromptPoint()
			};
			we.SetWatch(_watches.Domain);
			GlobalWin.Sound.StopSound();
			we.ShowDialog();
			GlobalWin.Sound.StartSound();

			if (we.DialogResult == DialogResult.OK)
			{
				_watches.Add(we.Watches[0]);
				Changes();
				UpdateWatchCount();
				WatchListView.ItemCount = _watches.ItemCount;
				UpdateValues();
			}
		}

		private void Changes()
		{
			_watches.Changes = true;
			UpdateMessageLabel();
		}

		private void ColumnPositions()
		{
			var Columns = Global.Config.RamWatchColumnIndexes
					.Where(x => WatchListView.Columns.ContainsKey(x.Key))
					.OrderBy(x => x.Value).ToList();

			for (var i = 0; i < Columns.Count; i++)
			{
				if (WatchListView.Columns.ContainsKey(Columns[i].Key))
				{
					WatchListView.Columns[Columns[i].Key].DisplayIndex = i;
				}
			}
		}

		private void CopyWatchesToClipBoard()
		{
			var indexes = SelectedIndices.ToList();

			if (indexes.Any())
			{
				var sb = new StringBuilder();
				foreach (var index in indexes)
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

		private void EditWatch(bool duplicate = false)
		{
			var indexes = SelectedIndices.ToList();

			if (SelectedWatches.Any())
			{
				var we = new WatchEditor
				{
					InitialLocation = GetPromptPoint(),
				};

				we.SetWatch(_watches.Domain, 
					SelectedWatches, duplicate ? WatchEditor.Mode.Duplicate : WatchEditor.Mode.Edit
				);
				
				GlobalWin.Sound.StopSound();
				var result = we.ShowDialog();
				if (result == DialogResult.OK)
				{
					Changes();
					if (duplicate)
					{
						_watches.AddRange(we.Watches);
						WatchListView.ItemCount = _watches.ItemCount;
					}
					else
					{
						for (var i = 0; i < we.Watches.Count; i++)
						{
							_watches[indexes[i]] = we.Watches[i];
						}
					}
				}

				GlobalWin.Sound.StartSound();
				UpdateValues();
			}
		}

		private string GetColumnValue(string name, int index)
		{
			switch (name)
			{
				default:
					return String.Empty;
				case WatchList.ADDRESS:
					return _watches[index].AddressString;
				case WatchList.VALUE:
					return _watches[index].ValueString;
				case WatchList.PREV:
					return _watches[index].PreviousStr;
				case WatchList.CHANGES:
					return _watches[index].ChangeCount.ToString();
				case WatchList.DIFF:
					return _watches[index].Diff;
				case WatchList.DOMAIN:
					return _watches[index].Domain.Name;
				case WatchList.NOTES:
					return _watches[index].Notes;
			}
		}

		private int GetColumnWidth(string columnName)
		{
			var width = Global.Config.RamWatchColumnWidths[columnName];
			if (width == -1)
			{
				width = _defaultColumnWidths[columnName];
			}

			return width;
		}

		private Point GetPromptPoint()
		{
			return PointToScreen(new Point(WatchListView.Location.X, WatchListView.Location.Y));
		}

		private void InsertSeparator()
		{
			var indexes = SelectedIndices.ToList();
			if (indexes.Any())
			{
				_watches.Insert(indexes[0], SeparatorWatch.Instance);
			}
			else
			{
				_watches.Add(SeparatorWatch.Instance);
			}
			WatchListView.ItemCount = _watches.ItemCount;
			Changes();
			UpdateWatchCount();
		}

		private void LoadColumnInfo()
		{
			WatchListView.Columns.Clear();
			ToolHelpers.AddColumn(WatchListView, WatchList.ADDRESS, true, GetColumnWidth(WatchList.ADDRESS));
			ToolHelpers.AddColumn(WatchListView, WatchList.VALUE, true, GetColumnWidth(WatchList.VALUE));
			ToolHelpers.AddColumn(WatchListView, WatchList.PREV, Global.Config.RamWatchShowPrevColumn, GetColumnWidth(WatchList.PREV));
			ToolHelpers.AddColumn(WatchListView, WatchList.CHANGES, Global.Config.RamWatchShowChangeColumn, GetColumnWidth(WatchList.CHANGES));
			ToolHelpers.AddColumn(WatchListView, WatchList.DIFF, Global.Config.RamWatchShowDiffColumn, GetColumnWidth(WatchList.DIFF));
			ToolHelpers.AddColumn(WatchListView, WatchList.DOMAIN, Global.Config.RamWatchShowDomainColumn, GetColumnWidth(WatchList.DOMAIN));
			ToolHelpers.AddColumn(WatchListView, WatchList.NOTES, true, GetColumnWidth(WatchList.NOTES));

			ColumnPositions();
		}

		private void LoadConfigSettings()
		{
			//Size and Positioning
			_defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
			_defaultHeight = Size.Height;

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

		private void MoveDown()
		{
			var indices = SelectedIndices.ToList();
			if (indices.Count == 0 || indices.Last() == _watches.Count - 1)
			{
				return;
			}

			for (var i = indices.Count - 1; i >= 0; i--)
			{
				var watch = _watches[indices[i]];
				_watches.Remove(watch);
				_watches.Insert(indices[i] + 1, watch);
			}

			var newindices = indices.Select(t => t + 1).ToList();

			WatchListView.SelectedIndices.Clear();
			foreach (var t in newindices)
			{
				WatchListView.SelectItem(t, true);
			}

			Changes();
			WatchListView.ItemCount = _watches.ItemCount;
		}

		private void MoveUp()
		{
			var indexes = SelectedIndices.ToList();
			if (!indexes.Any() || indexes[0] == 0)
			{
				return;
			}

			foreach (var index in indexes)
			{
				var watch = _watches[index];
				_watches.Remove(watch);
				_watches.Insert(index - 1, watch);
			}

			Changes();
			
			var indices = indexes.Select(t => t - 1).ToList();

			WatchListView.SelectedIndices.Clear();
			foreach (var t in indices)
			{
				WatchListView.SelectItem(t, true);
			}

			WatchListView.ItemCount = _watches.ItemCount;
		}

		private void NewWatchList(bool suppressAsk)
		{
			var result = true;
			if (_watches.Changes)
			{
				result = AskSave();
			}

			if (result || suppressAsk)
			{
				_watches.Clear();
				WatchListView.ItemCount = _watches.ItemCount;
				UpdateWatchCount();
				UpdateMessageLabel();
				_sortReverse = false;
				_sortedColumn = String.Empty;
			}
		}

		private void OrderColumn(int index)
		{
			var column = WatchListView.Columns[index];
			if (column.Name != _sortedColumn)
			{
				_sortReverse = false;
			}

			_watches.OrderWatches(column.Name, _sortReverse);

			_sortedColumn = column.Name;
			_sortReverse ^= true;
			WatchListView.Refresh();
		}

		private void PokeAddress()
		{
			if (SelectedWatches.Any())
			{
				var poke = new RamPoke
				{
					InitialLocation = GetPromptPoint()
				};

				if (SelectedWatches.Any())
				{
					poke.SetWatch(SelectedWatches);
				}

				GlobalWin.Sound.StopSound();
				var result = poke.ShowDialog();
				if (result == DialogResult.OK)
				{
					UpdateValues();
				}
				GlobalWin.Sound.StartSound();
			}
		}

		private void RemoveWatch()
		{
			if (SelectedItems.Any())
			{
				foreach (var item in SelectedItems)
				{
					_watches.Remove(item);
				}

				WatchListView.ItemCount = _watches.ItemCount;
				UpdateValues();
				UpdateWatchCount();
			}
		}

		private void SaveAs()
		{
			var result = _watches.SaveAs(ToolHelpers.GetWatchSaveFileFromUser(_watches.CurrentFileName));
			if (result)
			{
				UpdateMessageLabel(saved: true);
				Global.Config.RecentWatches.Add(_watches.CurrentFileName);
			}
		}

		private void SaveColumnInfo()
		{
			if (WatchListView.Columns[WatchList.ADDRESS] != null)
			{
				Global.Config.RamWatchColumnIndexes[WatchList.ADDRESS] = WatchListView.Columns[WatchList.ADDRESS].DisplayIndex;
				Global.Config.RamWatchColumnWidths[WatchList.ADDRESS] = WatchListView.Columns[WatchList.ADDRESS].Width;
			}

			if (WatchListView.Columns[WatchList.VALUE] != null)
			{
				Global.Config.RamWatchColumnIndexes[WatchList.VALUE] = WatchListView.Columns[WatchList.VALUE].DisplayIndex;
				Global.Config.RamWatchColumnWidths[WatchList.VALUE] = WatchListView.Columns[WatchList.VALUE].Width;
			}

			if (WatchListView.Columns[WatchList.PREV] != null)
			{
				Global.Config.RamWatchColumnIndexes[WatchList.PREV] = WatchListView.Columns[WatchList.PREV].DisplayIndex;
				Global.Config.RamWatchColumnWidths[WatchList.PREV] = WatchListView.Columns[WatchList.PREV].Width;
			}

			if (WatchListView.Columns[WatchList.CHANGES] != null)
			{
				Global.Config.RamWatchColumnIndexes[WatchList.CHANGES] = WatchListView.Columns[WatchList.CHANGES].DisplayIndex;
				Global.Config.RamWatchColumnWidths[WatchList.CHANGES] = WatchListView.Columns[WatchList.CHANGES].Width;
			}

			if (WatchListView.Columns[WatchList.DIFF] != null)
			{
				Global.Config.RamWatchColumnIndexes[WatchList.DIFF] = WatchListView.Columns[WatchList.DIFF].DisplayIndex;
				Global.Config.RamWatchColumnWidths[WatchList.DIFF] = WatchListView.Columns[WatchList.DIFF].Width;
			}

			if (WatchListView.Columns[WatchList.DOMAIN] != null)
			{
				Global.Config.RamWatchColumnIndexes[WatchList.DOMAIN] = WatchListView.Columns[WatchList.DOMAIN].DisplayIndex;
				Global.Config.RamWatchColumnWidths[WatchList.DOMAIN] = WatchListView.Columns[WatchList.DOMAIN].Width;
			}

			if (WatchListView.Columns[WatchList.NOTES] != null)
			{
				Global.Config.RamWatchColumnIndexes[WatchList.NOTES] = WatchListView.Columns[WatchList.NOTES].Index;
				Global.Config.RamWatchColumnWidths[WatchList.NOTES] = WatchListView.Columns[WatchList.NOTES].Width;
			}
		}

		private void SaveConfigSettings()
		{
			SaveColumnInfo();
			Global.Config.RamWatchWndx = Location.X;
			Global.Config.RamWatchWndy = Location.Y;
			Global.Config.RamWatchWidth = Right - Left;
			Global.Config.RamWatchHeight = Bottom - Top;
		}

		private void SelectAll()
		{
			for (var i = 0; i < _watches.Count; i++)
			{
				WatchListView.SelectItem(i, true);
			}
		}

		private IEnumerable<int> SelectedIndices
		{
			get { return WatchListView.SelectedIndices.Cast<int>(); }
		}

		private IEnumerable<Watch> SelectedItems
		{
			get { return SelectedIndices.Select(index => _watches[index]); }
		}

		private IEnumerable<Watch> SelectedWatches
		{
			get { return SelectedItems.Where(x => !x.IsSeparator); }
		}

		private void SetMemoryDomain(string name)
		{
			_watches.Domain = Global.Emulator.MemoryDomains[name];
			SetPlatformAndMemoryDomainLabel();
			Update();
		}

		private void SetPlatformAndMemoryDomainLabel()
		{
			MemDomainLabel.Text = Global.Emulator.SystemId + " " + _watches.Domain.Name;
		}

		private void UpdateMessageLabel(bool saved = false)
		{
			var message = String.Empty;
			if (!String.IsNullOrWhiteSpace(_watches.CurrentFileName))
			{
				if (saved)
				{
					message = Path.GetFileName(_watches.CurrentFileName) + " saved.";
				}
				else
				{
					message = Path.GetFileName(_watches.CurrentFileName) + (_watches.Changes ? " *" : String.Empty);
				}
			}

			MessageLabel.Text = message;
		}

		private void UpdateWatchCount()
		{
			WatchCountLabel.Text = _watches.WatchCount + (_watches.WatchCount == 1 ? " watch" : " watches");
		}

		private void WatchListView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (index >= _watches.ItemCount)
			{
				return;
			}

			if (column == 0)
			{
				if (_watches[index].IsSeparator)
				{
					color = BackColor;
				}
				else if (Global.CheatList.IsActive(_watches.Domain, _watches[index].Address ?? 0))
				{
					color = Color.LightCyan;
				}
			}
		}

		private void WatchListView_QueryItemText(int index, int column, out string text)
		{
			text = String.Empty;

			if (index >= _watches.ItemCount || _watches[index].IsSeparator)
			{
				return;
			}
			var columnName = WatchListView.Columns[column].Name;

			switch (columnName)
			{
				case WatchList.ADDRESS:
					text = _watches[index].AddressString;
					break;
				case WatchList.VALUE:
					text = _watches[index].ValueString;
					break;
				case WatchList.PREV:
					text = _watches[index].PreviousStr;
					break;
				case WatchList.CHANGES:
					if (!_watches[index].IsSeparator)
					{
						text = _watches[index].ChangeCount.ToString();
					}
					break;
				case WatchList.DIFF:
					text = _watches[index].Diff;
					break;
				case WatchList.DOMAIN:
					text = _watches[index].Domain.Name;
					break;
				case WatchList.NOTES:
					text = _watches[index].Notes;
					break;
			}
		}

		#endregion

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
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (Path.GetExtension(filePaths[0]) == (".wch"))
			{
				_watches.Load(filePaths[0], append:false);
				WatchListView.ItemCount = _watches.ItemCount;
			}
		}

		private void NewRamWatch_Enter(object sender, EventArgs e)
		{
			WatchListView.Focus();
		}

		/*************File***********************/
		private void filesToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			saveToolStripMenuItem.Enabled = _watches.Changes;
		}

		private void newListToolStripMenuItem_Click(object sender, EventArgs e)
		{
			NewWatchList(false);
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var append = sender == appendFileToolStripMenuItem;
			LoadWatchFile(ToolHelpers.GetWatchFileFromUser(_watches.CurrentFileName), append);
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!String.IsNullOrWhiteSpace(_watches.CurrentFileName))
			{
				if (_watches.Save())
				{
					UpdateMessageLabel(saved: true);
				}
			}
			else
			{
				SaveAs();
			}
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveAs();
		}

		private void recentToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			recentToolStripMenuItem.DropDownItems.Clear();
			recentToolStripMenuItem.DropDownItems.AddRange(
				ToolHelpers.GenerateRecentMenu(Global.Config.RecentWatches, LoadFileFromRecent)
			);
			recentToolStripMenuItem.DropDownItems.Add(
				ToolHelpers.GenerateAutoLoadItem(Global.Config.RecentWatches)
			);
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
			editWatchToolStripMenuItem.Enabled =
				duplicateWatchToolStripMenuItem.Enabled =
				removeWatchToolStripMenuItem.Enabled =
				moveUpToolStripMenuItem.Enabled =
				moveDownToolStripMenuItem.Enabled =
				pokeAddressToolStripMenuItem.Enabled =
				freezeAddressToolStripMenuItem.Enabled =
				SelectedIndices.Any();
		}

		private void memoryDomainsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			memoryDomainsToolStripMenuItem.DropDownItems.Clear();
			memoryDomainsToolStripMenuItem.DropDownItems.AddRange(ToolHelpers.GenerateMemoryDomainMenuItems(SetMemoryDomain, _watches.Domain.Name).ToArray());
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
			var allCheats = SelectedWatches.All(watch => !Global.CheatList.IsActive(watch.Domain, watch.Address ?? 0));
			if (allCheats)
			{
				ToolHelpers.UnfreezeAddress(SelectedWatches);
			}
			else
			{
				ToolHelpers.FreezeAddress(SelectedWatches);
			}
		}

		private void insertSeparatorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			InsertSeparator();
		}

		private void clearChangeCountsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			_watches.ClearChangeCounts();
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
				GlobalWin.OSD.ClearGUIText();
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
			Size = new Size(_defaultWidth, _defaultHeight);

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

			WatchListView.Columns[WatchList.ADDRESS].Width = _defaultColumnWidths[WatchList.ADDRESS];
			WatchListView.Columns[WatchList.VALUE].Width = _defaultColumnWidths[WatchList.VALUE];
			//WatchListView.Columns[WatchList.PREV].Width = DefaultColumnWidths[WatchList.PREV];
			WatchListView.Columns[WatchList.CHANGES].Width = _defaultColumnWidths[WatchList.CHANGES];
			//WatchListView.Columns[WatchList.DIFF].Width = DefaultColumnWidths[WatchList.DIFF];
			WatchListView.Columns[WatchList.DOMAIN].Width = _defaultColumnWidths[WatchList.DOMAIN];
			WatchListView.Columns[WatchList.NOTES].Width = _defaultColumnWidths[WatchList.NOTES];

			Global.Config.DisplayRamWatch = false;
			Global.Config.RamWatchSaveWindowPosition = true;
			Global.Config.RamWatchAlwaysOnTop = TopMost = false;

			LoadColumnInfo();
		}

		/*************Context Menu***********************/
		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			var indexes = WatchListView.SelectedIndices;

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


			var allCheats = _watches.All(x => Global.CheatList.IsActive(x.Domain, x.Address ?? 0));

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

			UnfreezeAllContextMenuItem.Visible = Global.CheatList.ActiveCount > 0;

			ViewInHexEditorContextMenuItem.Visible = SelectedWatches.Count() == 1;
		}

		private void UnfreezeAllContextMenuItem_Click(object sender, EventArgs e)
		{
			Global.CheatList.DisableAll();
		}

		private void ViewInHexEditorContextMenuItem_Click(object sender, EventArgs e)
		{
			var selected = SelectedWatches.ToList();
			if (selected.Any())
			{
				GlobalWin.Tools.Load<HexEditor>();

				if (selected.Select(x => x.Domain).Distinct().Count() > 1)
				{
					ToolHelpers.ViewInHexEditor(selected[0].Domain, new List<int> { selected.First().Address ?? 0 });
				}
				else
				{
					ToolHelpers.ViewInHexEditor(selected[0].Domain, selected.Select(x => x.Address ?? 0));
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
				for (var x = 0; x < _watches.Count; x++)
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
			Global.Config.RamWatchColumnIndexes[WatchList.ADDRESS] = WatchListView.Columns[WatchList.ADDRESS].DisplayIndex;
			Global.Config.RamWatchColumnIndexes[WatchList.VALUE] = WatchListView.Columns[WatchList.VALUE].DisplayIndex;
			Global.Config.RamWatchColumnIndexes[WatchList.PREV] = WatchListView.Columns[WatchList.ADDRESS].DisplayIndex;
			Global.Config.RamWatchColumnIndexes[WatchList.CHANGES] = WatchListView.Columns[WatchList.CHANGES].DisplayIndex;
			Global.Config.RamWatchColumnIndexes[WatchList.DIFF] = WatchListView.Columns[WatchList.DIFF].DisplayIndex;
			Global.Config.RamWatchColumnIndexes[WatchList.DOMAIN] = WatchListView.Columns[WatchList.DOMAIN].DisplayIndex;
			Global.Config.RamWatchColumnIndexes[WatchList.NOTES] = WatchListView.Columns[WatchList.NOTES].DisplayIndex;
		}

		#endregion
	}
}
