using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;
using BizHawk.Client.EmuHawk.ToolExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class RamWatch : ToolFormBase, IToolForm
	{
		private WatchList _watches;

		private int _defaultWidth;
		private int _defaultHeight;
		private string _sortedColumn = string.Empty;
		private bool _sortReverse;
		private bool _paused = false;

		[RequiredService]
		private IMemoryDomains _memoryDomains { get; set; }

		[RequiredService]
		private IEmulator _emu { get; set; }

		[OptionalService]
		private IDebuggable _debuggable { get; set; }

		[ConfigPersist]
		public RamWatchSettings Settings { get; set; }

		public class RamWatchSettings : ToolDialogSettings
		{
			public RamWatchSettings()
			{
				Columns = new ColumnList
				{
					new Column { Name = WatchList.ADDRESS, Visible = true, Index = 0, Width = 60 },
					new Column { Name = WatchList.VALUE, Visible = true, Index = 1, Width = 59 },
					new Column { Name = WatchList.PREV, Visible = false, Index = 2, Width = 59 },
					new Column { Name = WatchList.CHANGES, Visible = true, Index = 3, Width = 55 },
					new Column { Name = WatchList.DIFF, Visible = false, Index = 4, Width = 59 },
					new Column { Name = WatchList.DOMAIN, Visible = true, Index = 5, Width = 55 },
					new Column { Name = WatchList.NOTES, Visible = true, Index = 6, Width = 128 },
				};
			}

			public ColumnList Columns { get; set; }
		}

		public RamWatch()
		{
			InitializeComponent();
			Settings = new RamWatchSettings();

			WatchListView.QueryItemText += WatchListView_QueryItemText;
			WatchListView.QueryItemBkColor += WatchListView_QueryItemBkColor;
			WatchListView.VirtualMode = true;
			Closing += (o, e) =>
			{
				if (AskSaveChanges())
				{
					SaveConfigSettings();
				}
				else
				{
					e.Cancel = true;
				}
			};

			_sortedColumn = string.Empty;
			_sortReverse = false;
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

		#region Properties

		public IEnumerable<Watch> Watches
		{
			get
			{
				return _watches.Where(x => !x.IsSeparator);
			}
		}

		public bool UpdateBefore
		{
			get { return false; }
		}

		#endregion

		#region API

		public void AddWatch(Watch watch)
		{
			_watches.Add(watch);
			WatchListView.ItemCount = _watches.Count;
			UpdateValues();
			UpdateWatchCount();
			Changes();
		}

		public bool AskSaveChanges()
		{
			if (_watches.Changes)
			{
				GlobalWin.Sound.StopSound();
				var result = MessageBox.Show("Save Changes?", "RAM Watch", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
				GlobalWin.Sound.StartSound();
				if (result == DialogResult.Yes)
				{
					if (string.IsNullOrWhiteSpace(_watches.CurrentFileName))
					{
						SaveAs();
					}
					else
					{
						_watches.Save();
						Global.Config.RecentWatches.Add(_watches.CurrentFileName);
					}
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

		public void LoadFileFromRecent(string path)
		{
			var ask_result = true;
			if (_watches.Changes)
			{
				ask_result = AskSaveChanges();
			}

			if (ask_result)
			{
				var load_result = _watches.Load(path, append: false);
				if (!load_result)
				{
					Global.Config.RecentWatches.HandleLoadError(path);
				}
				else
				{
					Global.Config.RecentWatches.Add(path);
					WatchListView.ItemCount = _watches.Count;
					UpdateWatchCount();
					UpdateStatusBar();
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
					result = AskSaveChanges();
				}

				if (result)
				{
					_watches.Load(file.FullName, append);
					WatchListView.ItemCount = _watches.Count;
					UpdateWatchCount();
					Global.Config.RecentWatches.Add(_watches.CurrentFileName);
					UpdateStatusBar();

					PokeAddressToolBarItem.Enabled =
						FreezeAddressToolBarItem.Enabled =
						SelectedIndices.Any() &&
						SelectedWatches.All(w => w.Domain.CanPoke());
				}
			}
		}

		public void Restart()
		{
			if ((!IsHandleCreated || IsDisposed) && !Global.Config.DisplayRamWatch)
			{
				return;
			}

			if (_watches != null
				&& !string.IsNullOrWhiteSpace(_watches.CurrentFileName)
				&& _watches.All(w => _memoryDomains.Contains(w.Domain)))
			{
				_watches.RefreshDomains(_memoryDomains);
				_watches.Reload();
				UpdateStatusBar();

				
			}
			else
			{
				_watches = new WatchList(_memoryDomains, _emu.SystemId);
				NewWatchList(true);
			}
		}

		public void NewUpdate(ToolFormUpdateType type) { }

		public void UpdateValues()
		{
			if (_paused)
			{
				return;
			}

			if ((!IsHandleCreated || IsDisposed) && !Global.Config.DisplayRamWatch)
			{
				return;
			}

			if (_watches.Any())
			{
				_watches.UpdateValues();

				if (Global.Config.DisplayRamWatch)
				{
					for (var i = 0; i < _watches.Count; i++)
					{
						var frozen = !_watches[i].IsSeparator && Global.CheatList.IsActive(_watches[i].Domain, _watches[i].Address);
						GlobalWin.OSD.AddGUIText(
							_watches[i].ToDisplayString(),
							Global.Config.DispRamWatchx,
							Global.Config.DispRamWatchy + (i * 14),
							Color.Black,
							frozen ? Color.Cyan : Color.White,
							0
						);
					}
				}

				if (!IsHandleCreated || IsDisposed)
				{
					return;
				}

				WatchListView.BlazingFast = true;
				WatchListView.UseCustomBackground = NeedsBackground;
				WatchListView.Invalidate();
				WatchListView.BlazingFast = false;
			}
		}

		private bool NeedsBackground
		{
			get
			{
				foreach (var watch in _watches)
				{
					if (watch.IsSeparator)
					{
						return true;
					}

					if (Global.CheatList.IsActive(watch.Domain, watch.Address))
					{
						return true;
					}

					if (watch.IsOutOfRange)
					{
						return true;
					}
				}

				return false;
			}
		}

		public void FastUpdate()
		{
			if (_paused)
			{
				return;
			}

			if ((!IsHandleCreated || IsDisposed) && !Global.Config.DisplayRamWatch)
			{
				return;
			}

			if (_watches.Any())
			{
				_watches.UpdateValues();

				if (Global.Config.DisplayRamWatch)
				{
					for (var i = 0; i < _watches.Count; i++)
					{
						var frozen = !_watches[i].IsSeparator && Global.CheatList.IsActive(_watches[i].Domain, _watches[i].Address);
						GlobalWin.OSD.AddGUIText(
							_watches[i].ToDisplayString(),
							Global.Config.DispRamWatchx,
							Global.Config.DispRamWatchy + (i * 14),
							Color.Black,
							frozen ? Color.Cyan : Color.White,
							0
						);
					}
				}
			}
		}

		#endregion

		#region Private Methods

		private void Changes()
		{
			_watches.Changes = true;
			UpdateStatusBar();
		}

		private void CopyWatchesToClipBoard()
		{
			if (SelectedItems.Any())
			{
				var sb = new StringBuilder();
				foreach (var watch in SelectedItems)
				{
					sb.AppendLine(ToString());
				}

				if (sb.Length > 0)
				{
					Clipboard.SetDataObject(sb.ToString());
				}
			}
		}

		private void PasteWatchesToClipBoard()
		{
			var data = Clipboard.GetDataObject();

			if (data != null && data.GetDataPresent(DataFormats.Text))
			{
				var clipboardRows = ((string)data.GetData(DataFormats.Text)).Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var row in clipboardRows)
				{
					var watch = Watch.FromString(row, _memoryDomains);
					if ((object)watch != null)
					{
						_watches.Add(watch);
					}
				}

				FullyUpdateWatchList();
			}
		}

		private void FullyUpdateWatchList()
		{
			WatchListView.ItemCount = _watches.Count;
			UpdateWatchCount();
			UpdateStatusBar();
			UpdateValues();
		}

		private void EditWatch(bool duplicate = false)
		{
			var indexes = SelectedIndices.ToList();

			if (SelectedWatches.Any())
			{
				foreach (var sw in SelectedWatches)
					if (sw.Domain != SelectedWatches.First().Domain)
						throw new InvalidOperationException("Can't edit multiple watches on varying memorydomains");

				var we = new WatchEditor
				{
					InitialLocation = this.ChildPointToScreen(WatchListView),
					MemoryDomains = _memoryDomains
				};

				we.SetWatch(SelectedWatches.First().Domain, SelectedWatches, duplicate ? WatchEditor.Mode.Duplicate : WatchEditor.Mode.Edit);

				var result = we.ShowHawkDialog(this);
				if (result == DialogResult.OK)
				{
					Changes();
					if (duplicate)
					{
						_watches.AddRange(we.Watches);
						WatchListView.ItemCount = _watches.Count;
					}
					else
					{
						for (var i = 0; i < we.Watches.Count; i++)
						{
							_watches[indexes[i]] = we.Watches[i];
						}
					}
				}

				UpdateValues();
			}
		}

		private string GetColumnValue(string name, int index)
		{
			switch (name)
			{
				default:
					return string.Empty;
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

		private void LoadConfigSettings()
		{
			// Size and Positioning
			_defaultWidth = Size.Width;
			_defaultHeight = Size.Height;

			if (Settings.UseWindowPosition)
			{
				Location = Settings.WindowPosition;
			}

			if (Settings.UseWindowSize)
			{
				Size = Settings.WindowSize;
			}

			LoadColumnInfo(WatchListView, Settings.Columns);
		}

		private void NewWatchList(bool suppressAsk)
		{
			var result = true;
			if (_watches.Changes)
			{
				result = AskSaveChanges();
			}

			if (result || suppressAsk)
			{
				_watches.Clear();
				WatchListView.ItemCount = _watches.Count;
				UpdateWatchCount();
				UpdateStatusBar();
				_sortReverse = false;
				_sortedColumn = string.Empty;

				PokeAddressToolBarItem.Enabled =
					FreezeAddressToolBarItem.Enabled =
					SelectedIndices.Any() &&
					SelectedWatches.All(w => w.Domain.CanPoke());
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

		private void SaveAs()
		{
			var result = _watches.SaveAs(GetWatchSaveFileFromUser(_watches.CurrentFileName));
			if (result)
			{
				UpdateStatusBar(saved: true);
				Global.Config.RecentWatches.Add(_watches.CurrentFileName);
			}
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

		private void SetMemoryDomain(string name)
		{
			CurrentDomain = _memoryDomains[name];
			Update();
		}

		private void UpdateStatusBar(bool saved = false)
		{
			var message = string.Empty;
			if (!string.IsNullOrWhiteSpace(_watches.CurrentFileName))
			{
				if (saved)
				{
					message = Path.GetFileName(_watches.CurrentFileName) + " saved.";
				}
				else
				{
					message = Path.GetFileName(_watches.CurrentFileName) + (_watches.Changes ? " *" : string.Empty);
				}
			}

			ErrorIconButton.Visible = _watches.Where(watch => !watch.IsSeparator).Any(watch => (watch.Address) >= watch.Domain.Size);

			MessageLabel.Text = message;
		}

		private void UpdateWatchCount()
		{
			WatchCountLabel.Text = _watches.WatchCount + (_watches.WatchCount == 1 ? " watch" : " watches");
		}

		private void WatchListView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (index >= _watches.Count)
			{
				return;
			}

			if (column == 0)
			{
				if (_watches[index].IsSeparator)
				{
					color = BackColor;
				}
				else if (_watches[index].Address >= _watches[index].Domain.Size)
				{
					color = Color.PeachPuff;
				}
				else if (Global.CheatList.IsActive(_watches[index].Domain, _watches[index].Address))
				{
					color = Color.LightCyan;
				}
			}
		}

		private void WatchListView_QueryItemText(int index, int column, out string text)
		{
			text = string.Empty;

			if (index >= _watches.Count || _watches[index].IsSeparator)
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

		#region File Menu

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveMenuItem.Enabled = _watches.Changes;
		}

		private void NewListMenuItem_Click(object sender, EventArgs e)
		{
			NewWatchList(false);
		}

		private void OpenMenuItem_Click(object sender, EventArgs e)
		{
			var append = sender == AppendMenuItem;
			LoadWatchFile(GetWatchFileFromUser(_watches.CurrentFileName), append);
		}

		private void SaveMenuItem_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(_watches.CurrentFileName))
			{
				if (_watches.Save())
				{
					Global.Config.RecentWatches.Add(_watches.CurrentFileName);
					UpdateStatusBar(saved: true);
				}
			}
			else
			{
				SaveAs();
			}
		}

		private void SaveAsMenuItem_Click(object sender, EventArgs e)
		{
			SaveAs();
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(
				Global.Config.RecentWatches.RecentMenu(LoadFileFromRecent, true));
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		#region Watch

		private void WatchesSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			EditWatchMenuItem.Enabled =
				DuplicateWatchMenuItem.Enabled =
				RemoveWatchMenuItem.Enabled =
				MoveUpMenuItem.Enabled =
				MoveDownMenuItem.Enabled =
				SelectedIndices.Any();

			PokeAddressMenuItem.Enabled =
				FreezeAddressMenuItem.Enabled =
				SelectedIndices.Any() &&
				SelectedWatches.All(w => w.Domain.CanPoke());

			PauseMenuItem.Text = _paused ? "Unpause" : "Pause";
		}

		private MemoryDomain _currentDomain = null;

		private MemoryDomain CurrentDomain
		{
			get { return _currentDomain ?? _memoryDomains.MainMemory; }
			set { _currentDomain = value; }
		}

		private void MemoryDomainsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			MemoryDomainsSubMenu.DropDownItems.Clear();
			MemoryDomainsSubMenu.DropDownItems.AddRange(
				_memoryDomains.MenuItems(SetMemoryDomain, CurrentDomain.Name)
				.ToArray());
		}

		private void NewWatchMenuItem_Click(object sender, EventArgs e)
		{
			var we = new WatchEditor
			{
				InitialLocation = this.ChildPointToScreen(WatchListView),
				MemoryDomains = _memoryDomains
			};
			we.SetWatch(CurrentDomain);
			we.ShowHawkDialog(this);
			if (we.DialogResult == DialogResult.OK)
			{
				_watches.Add(we.Watches[0]);
				Changes();
				UpdateWatchCount();
				WatchListView.ItemCount = _watches.Count;
				UpdateValues();
			}
		}

		private void EditWatchMenuItem_Click(object sender, EventArgs e)
		{
			EditWatch();
		}

		private void RemoveWatchMenuItem_Click(object sender, EventArgs e)
		{
			var items = SelectedItems.ToList();
			if (items.Any())
			{
				foreach (var item in items)
				{
					_watches.Remove(item);
				}

				WatchListView.ItemCount = _watches.Count;
				UpdateValues();
				UpdateWatchCount();
			}
		}

		private void DuplicateWatchMenuItem_Click(object sender, EventArgs e)
		{
			EditWatch(duplicate: true);
		}

		private void PokeAddressMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedWatches.Any())
			{
				RamPoke poke = new RamPoke
				{
					InitialLocation = this.ChildPointToScreen(WatchListView)
				};

				poke.SetWatch(SelectedWatches);

				if (poke.ShowHawkDialog(this) == DialogResult.OK)
				{
					UpdateValues();
				}
			}
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

		private void InsertSeparatorMenuItem_Click(object sender, EventArgs e)
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

			WatchListView.ItemCount = _watches.Count;
			Changes();
			UpdateWatchCount();
		}

		private void ClearChangeCountsMenuItem_Click(object sender, EventArgs e)
		{
			_watches.ClearChangeCounts();
		}

		private void MoveUpMenuItem_Click(object sender, EventArgs e)
		{
			var indexes = SelectedIndices.ToList();
			if (!indexes.Any() || indexes[0] == 0)
			{
				return;
			}

			foreach (var index in indexes)
			{
				var watch = _watches[index];
				_watches.RemoveAt(index);
				_watches.Insert(index - 1, watch);
			}

			Changes();

			var indices = indexes.Select(t => t - 1).ToList();

			WatchListView.SelectedIndices.Clear();
			foreach (var t in indices)
			{
				WatchListView.SelectItem(t, true);
			}

			WatchListView.ItemCount = _watches.Count;
		}

		private void MoveDownMenuItem_Click(object sender, EventArgs e)
		{
			var indices = SelectedIndices.ToList();
			if (indices.Count == 0 || indices.Last() == _watches.Count - 1)
			{
				return;
			}

			for (var i = indices.Count - 1; i >= 0; i--)
			{
				var watch = _watches[indices[i]];
				_watches.RemoveAt(indices[i]);
				_watches.Insert(indices[i] + 1, watch);
			}

			var newindices = indices.Select(t => t + 1).ToList();

			WatchListView.SelectedIndices.Clear();
			foreach (var t in newindices)
			{
				WatchListView.SelectItem(t, true);
			}

			Changes();
			WatchListView.ItemCount = _watches.Count;
		}

		private void SelectAllMenuItem_Click(object sender, EventArgs e)
		{
			WatchListView.SelectAll();
		}

		private void PauseMenuItem_Click(object sender, EventArgs e)
		{
			_paused ^= true;
		}

		#endregion

		#region Options

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			WatchesOnScreenMenuItem.Checked = Global.Config.DisplayRamWatch;
			SaveWindowPositionMenuItem.Checked = Settings.SaveWindowPosition;
			AlwaysOnTopMenuItem.Checked = Settings.TopMost;
			FloatingWindowMenuItem.Checked = Settings.FloatingWindow;
		}

		private void DefinePreviousValueSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			PreviousFrameMenuItem.Checked = Global.Config.RamWatchDefinePrevious == PreviousType.LastFrame;
			LastChangeMenuItem.Checked = Global.Config.RamWatchDefinePrevious == PreviousType.LastChange;
			OriginalMenuItem.Checked = Global.Config.RamWatchDefinePrevious == PreviousType.Original;
		}

		private void PreviousFrameMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamWatchDefinePrevious = PreviousType.LastFrame;
		}

		private void LastChangeMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamWatchDefinePrevious = PreviousType.LastChange;
		}

		private void OriginalMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RamWatchDefinePrevious = PreviousType.Original;
		}

		private void WatchesOnScreenMenuItem_Click(object sender, EventArgs e)
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

		private void SaveWindowPositionMenuItem_Click(object sender, EventArgs e)
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
			Settings = new RamWatchSettings();
			Size = new Size(_defaultWidth, _defaultHeight);

			RamWatchMenu.Items.Remove(
				RamWatchMenu.Items
					.OfType<ToolStripMenuItem>()
					.First(x => x.Name == "GeneratedColumnsSubMenu")
			);

			RamWatchMenu.Items.Add(Settings.Columns.GenerateColumnsMenu(ColumnToggleCallback));

			Global.Config.DisplayRamWatch = false;

			RefreshFloatingWindowControl(Settings.FloatingWindow);
			LoadColumnInfo(WatchListView, Settings.Columns);
		}

		#endregion

		#region Dialog, Context Menu, and ListView Events

		private void NewRamWatch_Load(object sender, EventArgs e)
		{
			TopMost = Settings.TopMost;
			_watches = new WatchList(_memoryDomains, _emu.SystemId);
			LoadConfigSettings();
			RamWatchMenu.Items.Add(Settings.Columns.GenerateColumnsMenu(ColumnToggleCallback));
			UpdateStatusBar();

			PokeAddressToolBarItem.Enabled =
				FreezeAddressToolBarItem.Enabled =
				SelectedIndices.Any() &&
				SelectedWatches.All(w => w.Domain.CanPoke());
		}

		private void ColumnToggleCallback()
		{
			SaveColumnInfo(WatchListView, Settings.Columns);
			LoadColumnInfo(WatchListView, Settings.Columns);
		}

		private void NewRamWatch_Activated(object sender, EventArgs e)
		{
			WatchListView.Refresh();
		}

		private void NewRamWatch_DragDrop(object sender, DragEventArgs e)
		{
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (Path.GetExtension(filePaths[0]) == ".wch")
			{
				_watches.Load(filePaths[0], append: false);
				Global.Config.RecentWatches.Add(_watches.CurrentFileName);
				WatchListView.ItemCount = _watches.Count;
			}
		}

		private void NewRamWatch_Enter(object sender, EventArgs e)
		{
			WatchListView.Focus();
		}

		private void ListViewContextMenu_Opening(object sender, CancelEventArgs e)
		{
			var indexes = WatchListView.SelectedIndices;

			EditContextMenuItem.Visible =
				RemoveContextMenuItem.Visible =
				DuplicateContextMenuItem.Visible =
				PokeContextMenuItem.Visible =
				FreezeContextMenuItem.Visible =
				Separator4.Visible =
				ReadBreakpointContextMenuItem.Visible =
				WriteBreakpointContextMenuItem.Visible =
				Separator6.Visible =
				InsertSeperatorContextMenuItem.Visible =
				MoveUpContextMenuItem.Visible =
				MoveDownContextMenuItem.Visible =
				indexes.Count > 0;

			ReadBreakpointContextMenuItem.Visible =
			WriteBreakpointContextMenuItem.Visible =
			Separator6.Visible =
				SelectedWatches.Any() &&
				_debuggable != null &&
				_debuggable.MemoryCallbacksAvailable() &&
				SelectedWatches.All(w => w.Domain.Name == (_memoryDomains != null ? _memoryDomains.SystemBus.Name : ""));

			PokeContextMenuItem.Enabled =
				FreezeContextMenuItem.Visible =
				SelectedIndices.Any() &&
				SelectedWatches.All(w => w.Domain.CanPoke());

			var allCheats = SelectedWatches.All(x => Global.CheatList.IsActive(x.Domain, x.Address));

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

			UnfreezeAllContextMenuItem.Visible = Global.CheatList.ActiveCount > 0;

			ViewInHexEditorContextMenuItem.Visible = SelectedWatches.Count() == 1;

			newToolStripMenuItem.Visible = indexes.Count == 0;
		}

		private void UnfreezeAllContextMenuItem_Click(object sender, EventArgs e)
		{
			Global.CheatList.RemoveAll();
		}

		private void ViewInHexEditorContextMenuItem_Click(object sender, EventArgs e)
		{
			var selected = SelectedWatches.ToList();
			if (selected.Any())
			{
				GlobalWin.Tools.Load<HexEditor>();

				if (selected.Select(x => x.Domain).Distinct().Count() > 1)
				{
					ViewInHexEditor(selected[0].Domain, new List<long> { selected.First().Address }, selected.First().Size);
				}
				else
				{
					ViewInHexEditor(selected.First().Domain, selected.Select(x => x.Address), selected.First().Size);
				}
			}
		}

		private void ReadBreakpointContextMenuItem_Click(object sender, EventArgs e)
		{
			var selected = SelectedWatches.ToList();

			if (selected.Any())
			{
				var debugger = GlobalWin.Tools.Load<GenericDebugger>();

				foreach (var watch in selected)
				{
					debugger.AddBreakpoint((uint)watch.Address, 0xFFFFFFFF, MemoryCallbackType.Read);
				}
			}
		}

		private void WriteBreakpointContextMenuItem_Click(object sender, EventArgs e)
		{
			var selected = SelectedWatches.ToList();

			if (selected.Any())
			{
				var debugger = GlobalWin.Tools.Load<GenericDebugger>();

				foreach (var watch in selected)
				{
					debugger.AddBreakpoint((uint)watch.Address, 0xFFFFFFFF, MemoryCallbackType.Write);
				}
			}
		}

		private void WatchListView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete && !e.Control && !e.Alt && !e.Shift)
			{
				RemoveWatchMenuItem_Click(sender, e);
			}
			else if (e.KeyCode == Keys.C && e.Control && !e.Alt && !e.Shift) // Ctrl+C
			{
				CopyWatchesToClipBoard();
			}
			else if (e.KeyCode == Keys.V && e.Control && !e.Alt && !e.Shift) // Ctrl+V
			{
				PasteWatchesToClipBoard();
			}
			else if (e.KeyCode == Keys.Enter && !e.Control && !e.Alt && !e.Shift) // Enter
			{
				EditWatch();
			}
		}

		private void WatchListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (WatchListView.SelectAllInProgress)
			{
				return;
			}

			PokeAddressToolBarItem.Enabled =
				FreezeAddressToolBarItem.Enabled =
				SelectedIndices.Any() &&
				SelectedWatches.All(w => w.Domain.CanPoke());
		}

		private void WatchListView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			EditWatch();
		}

		private void WatchListView_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			OrderColumn(e.Column);
		}

		private void ErrorIconButton_Click(object sender, EventArgs e)
		{
			var items = _watches
				.Where(watch => (watch.Address) >= watch.Domain.Size)
				.ToList();

			foreach (var item in items)
			{
				_watches.Remove(item);
			}

			WatchListView.ItemCount = _watches.Count;
			UpdateValues();
			UpdateWatchCount();
			UpdateStatusBar();
		}

		#endregion
		#endregion


		private void WatchListView_VirtualItemsSelectionRangeChanged(object sender, ListViewVirtualItemsSelectionRangeChangedEventArgs e)
		{
			PokeAddressToolBarItem.Enabled =
				FreezeAddressToolBarItem.Enabled =
				SelectedIndices.Any() &&
				SelectedWatches.All(w => w.Domain.CanPoke());
		}

		// Stupid designer
		protected void DragEnterWrapper(object sender, DragEventArgs e)
		{
			base.GenericDragEnter(sender, e);
		}
	}
}
