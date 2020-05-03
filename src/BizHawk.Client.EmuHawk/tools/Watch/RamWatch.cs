using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Common;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.ToolExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class RamWatch : ToolFormBase, IToolForm
	{
		private WatchList _watches;

		private int _defaultWidth;
		private int _defaultHeight;
		private string _sortedColumn;
		private bool _sortReverse;

		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }

		[RequiredService]
		private IEmulator Emu { get; set; }

		[OptionalService]
		private IDebuggable Debuggable { get; set; }

		public RamWatch()
		{
			InitializeComponent();
			Settings = new RamWatchSettings();

			WatchListView.QueryItemText += WatchListView_QueryItemText;
			WatchListView.QueryItemBkColor += WatchListView_QueryItemBkColor;
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

			_sortedColumn = "";
			_sortReverse = false;


			SetColumns();
		}

		private void SetColumns()
		{
			foreach (var column in Settings.Columns)
			{
				if (WatchListView.AllColumns[column.Name] == null)
				{
					WatchListView.AllColumns.Add(column);
				}
			}
		}

		[ConfigPersist]
		public RamWatchSettings Settings { get; set; }

		public class RamWatchSettings : ToolDialogSettings
		{
			public RamWatchSettings()
			{
				Columns = new List<RollColumn>
				{
					new RollColumn { Text = "Address", Name = WatchList.Address, Visible = true, UnscaledWidth = 60, Type = ColumnType.Text },
					new RollColumn { Text = "Value", Name = WatchList.Value, Visible = true, UnscaledWidth = 59, Type = ColumnType.Text },
					new RollColumn { Text = "Prev", Name = WatchList.Prev, Visible = false, UnscaledWidth = 59, Type = ColumnType.Text },
					new RollColumn { Text = "Changes", Name = WatchList.ChangesCol, Visible = true, UnscaledWidth = 60, Type = ColumnType.Text },
					new RollColumn { Text = "Diff", Name = WatchList.Diff, Visible = false, UnscaledWidth = 59, Type = ColumnType.Text },
					new RollColumn { Text = "Type", Name = WatchList.Type, Visible = false, UnscaledWidth = 55, Type = ColumnType.Text },
					new RollColumn { Text = "Domain", Name = WatchList.Domain, Visible = true, UnscaledWidth = 55, Type = ColumnType.Text },
					new RollColumn { Text = "Notes", Name = WatchList.Notes, Visible = true, UnscaledWidth = 128, Type = ColumnType.Text }
				};
			}

			public List<RollColumn> Columns { get; set; }
		}

		private IEnumerable<int> SelectedIndices => WatchListView.SelectedRows;
		private IEnumerable<Watch> SelectedItems => SelectedIndices.Select(index => _watches[index]);
		private IEnumerable<Watch> SelectedWatches => SelectedItems.Where(x => !x.IsSeparator);
		private IEnumerable<Watch> SelectedSeparators => SelectedItems.Where(x => x.IsSeparator);

		public IEnumerable<Watch> Watches => _watches.Where(x => !x.IsSeparator);

		#region API

		public void AddWatch(Watch watch)
		{
			_watches.Add(watch);
			WatchListView.RowCount = _watches.Count;
			GeneralUpdate();
			UpdateWatchCount();
			Changes();
		}

		public override bool AskSaveChanges()
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
						Config.RecentWatches.Add(_watches.CurrentFileName);
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
			var askResult = true;
			if (_watches.Changes)
			{
				askResult = AskSaveChanges();
			}

			if (askResult)
			{
				var loadResult = _watches.Load(path, append: false);
				if (!loadResult)
				{
					Config.RecentWatches.HandleLoadError(path);
				}
				else
				{
					Config.RecentWatches.Add(path);
					WatchListView.RowCount = _watches.Count;
					UpdateWatchCount();
					GeneralUpdate();
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
					WatchListView.RowCount = _watches.Count;
					UpdateWatchCount();
					Config.RecentWatches.Add(_watches.CurrentFileName);
					UpdateStatusBar();
					GeneralUpdate();
					PokeAddressToolBarItem.Enabled =
						FreezeAddressToolBarItem.Enabled =
						SelectedIndices.Any()
						&& SelectedWatches.All(w => w.Domain.Writable);
				}
			}
		}

		public void Restart()
		{
			if ((!IsHandleCreated || IsDisposed) && !Config.DisplayRamWatch)
			{
				return;
			}

			if (_watches != null
				&& !string.IsNullOrWhiteSpace(_watches.CurrentFileName)
				&& _watches.All(w => w.Domain == null || MemoryDomains.Select(m => m.Name).Contains(w.Domain.Name))
				&& (Config.RecentWatches.AutoLoad || (IsHandleCreated || !IsDisposed)))
			{
				_watches.RefreshDomains(MemoryDomains);
				_watches.Reload();
				GeneralUpdate();
				UpdateStatusBar();
			}
			else
			{
				_watches = new WatchList(MemoryDomains, Emu.SystemId);
				NewWatchList(true);
			}
		}

		public override void UpdateValues(ToolFormUpdateType type)
		{
			switch (type)
			{
				case ToolFormUpdateType.PostFrame:
				case ToolFormUpdateType.General:
					FrameUpdate();
					break;
				case ToolFormUpdateType.FastPostFrame:
					MinimalUpdate();
					break;
			}
		}

		#endregion

		#region Private Methods

		private void MinimalUpdate()
		{
			if ((!IsHandleCreated || IsDisposed) && !Config.DisplayRamWatch)
			{
				return;
			}

			if (_watches.Any())
			{
				_watches.UpdateValues();
				DisplayOnScreenWatches();
			}
		}

		private void FrameUpdate()
		{
			if ((!IsHandleCreated || IsDisposed) && !Config.DisplayRamWatch)
			{
				return;
			}

			GlobalWin.OSD.ClearRamWatches();
			if (_watches.Any())
			{
				_watches.UpdateValues();
				DisplayOnScreenWatches();

				if (!IsHandleCreated || IsDisposed)
				{
					return;
				}

				WatchListView.RowCount = _watches.Count;
			}
		}

		private void DisplayOnScreenWatches()
		{
			if (Config.DisplayRamWatch)
			{
				for (var i = 0; i < _watches.Count; i++)
				{
					var frozen = !_watches[i].IsSeparator && Global.CheatList.IsActive(_watches[i].Domain, _watches[i].Address);
					GlobalWin.OSD.AddRamWatch(
						_watches[i].ToDisplayString(),
						new MessagePosition
						{
							X = Config.RamWatches.X,
							Y = Config.RamWatches.Y + (i * 14),
							Anchor = Config.RamWatches.Anchor
						},
						Color.Black,
						frozen ? Color.Cyan : Color.White);
				}
			}
		}

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
					sb.AppendLine(watch.ToString());
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
					var watch = Watch.FromString(row, MemoryDomains);
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
			WatchListView.RowCount = _watches.Count;
			UpdateWatchCount();
			UpdateStatusBar();
			GeneralUpdate();
		}

		private void EditWatch(bool duplicate = false)
		{
			var indexes = SelectedIndices.ToList();

			if (SelectedWatches.Any())
			{
				foreach (var sw in SelectedWatches)
				{
					if (sw.Domain != SelectedWatches.First().Domain)
					{
						throw new InvalidOperationException("Can't edit multiple watches on varying memory domains");
					}
				}

				var we = new WatchEditor
				{
					InitialLocation = this.ChildPointToScreen(WatchListView),
					MemoryDomains = MemoryDomains
				};

				we.SetWatch(SelectedWatches.First().Domain, SelectedWatches, duplicate ? WatchEditor.Mode.Duplicate : WatchEditor.Mode.Edit);

				var result = we.ShowHawkDialog(this);
				if (result == DialogResult.OK)
				{
					Changes();
					if (duplicate)
					{
						_watches.AddRange(we.Watches);
						WatchListView.RowCount = _watches.Count;
						UpdateWatchCount();
					}
					else
					{
						for (var i = 0; i < we.Watches.Count; i++)
						{
							_watches[indexes[i]] = we.Watches[i];
						}
					}
				}

				GeneralUpdate();
			}
			else if (SelectedSeparators.Any() && !duplicate)
			{
				var inputPrompt = new InputPrompt
				{
					Text = "Edit Separator",
					StartLocation = this.ChildPointToScreen(WatchListView),
					Message = "Separator Text:",
					TextInputType = InputPrompt.InputType.Text
				};

				var result = inputPrompt.ShowHawkDialog();

				if (result == DialogResult.OK)
				{
					Changes();

					for (int i = 0; i < SelectedSeparators.Count(); i++)
					{
						var sep = SelectedSeparators.ToList()[i];
						sep.Notes = inputPrompt.PromptText;
						_watches[indexes[i]] = sep;
					}
				}

				GeneralUpdate();
			}
		}

		private string ComputeDisplayType(Watch w)
		{
			string s = w.Size == WatchSize.Byte ? "1" : (w.Size == WatchSize.Word ? "2" : "4");
			switch (w.Type)
			{
				case Common.DisplayType.Binary:
					s += "b";
					break;
				case Common.DisplayType.FixedPoint_12_4:
					s += "F";
					break;
				case Common.DisplayType.FixedPoint_16_16:
					s += "F6";
					break;
				case Common.DisplayType.FixedPoint_20_12:
					s += "F2";
					break;
				case Common.DisplayType.Float:
					s += "f";
					break;
				case Common.DisplayType.Hex:
					s += "h";
					break;
				case Common.DisplayType.Signed:
					s += "s";
					break;
				case Common.DisplayType.Unsigned:
					s += "u";
					break;
			}

			return s + (w.BigEndian ? "B" : "L");
		}

		private void LoadConfigSettings()
		{
			// Size and Positioning
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

			WatchListView.AllColumns.Clear();
			SetColumns();
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
				WatchListView.RowCount = _watches.Count;
				GeneralUpdate();
				UpdateWatchCount();
				UpdateStatusBar();
				_sortReverse = false;
				_sortedColumn = "";

				PokeAddressToolBarItem.Enabled =
					FreezeAddressToolBarItem.Enabled =
					SelectedIndices.Any() &&
					SelectedWatches.All(w => w.Domain.Writable);
			}
		}

		private void OrderColumn(RollColumn column)
		{
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
				Config.RecentWatches.Add(_watches.CurrentFileName);
			}
		}

		private void SaveConfigSettings()
		{
			Settings.Columns = WatchListView.AllColumns;

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
			CurrentDomain = MemoryDomains[name];
			Update();
		}

		private void UpdateStatusBar(bool saved = false)
		{
			var message = "";
			if (!string.IsNullOrWhiteSpace(_watches.CurrentFileName))
			{
				if (saved)
				{
					message = $"{Path.GetFileName(_watches.CurrentFileName)} saved.";
				}
				else
				{
					message = Path.GetFileName(_watches.CurrentFileName) + (_watches.Changes ? " *" : "");
				}
			}

			ErrorIconButton.Visible = _watches.Where(watch => !watch.IsSeparator).Any(watch => watch.Address >= watch.Domain.Size);

			MessageLabel.Text = message;
		}

		private void UpdateWatchCount()
		{
			WatchCountLabel.Text = _watches.WatchCount + (_watches.WatchCount == 1 ? " watch" : " watches");
		}

		private void WatchListView_QueryItemBkColor(int index, RollColumn column, ref Color color)
		{
			if (index >= _watches.Count)
			{
				return;
			}

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

		private void WatchListView_QueryItemText(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			text = "";
			if (index >= _watches.Count)
			{
				return;
			}

			if (_watches[index].IsSeparator)
			{
				if (column.Name == WatchList.Address)
				{
					text = _watches[index].Notes;
				}

				return;
			}

			switch (column.Name)
			{
				case WatchList.Address:
					text = _watches[index].AddressString;
					break;
				case WatchList.Value:
					text = _watches[index].ValueString;
					break;
				case WatchList.Prev:
					text = _watches[index].PreviousStr;
					break;
				case WatchList.ChangesCol:
					if (!_watches[index].IsSeparator)
					{
						text = _watches[index].ChangeCount.ToString();
					}

					break;
				case WatchList.Diff:
					text = _watches[index].Diff;
					break;
				case WatchList.Type:
					text = ComputeDisplayType(_watches[index]);
					break;
				case WatchList.Domain:
					text = _watches[index].Domain.Name;
					break;
				case WatchList.Notes:
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
					Config.RecentWatches.Add(_watches.CurrentFileName);
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
			RecentSubMenu.DropDownItems.AddRange(Config.RecentWatches.RecentMenu(LoadFileFromRecent, "Watches"));
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
				MoveTopMenuItem.Enabled =
				MoveBottomMenuItem.Enabled =
				SelectedIndices.Any();

			PokeAddressMenuItem.Enabled =
				FreezeAddressMenuItem.Enabled =
				SelectedIndices.Any() &&
				SelectedWatches.All(w => w.Domain.Writable);
		}

		private MemoryDomain _currentDomain;

		private MemoryDomain CurrentDomain
		{
			get => _currentDomain ?? MemoryDomains.MainMemory;
			set => _currentDomain = value;
		}

		private void MemoryDomainsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			MemoryDomainsSubMenu.DropDownItems.Clear();
			MemoryDomainsSubMenu.DropDownItems.AddRange(
				MemoryDomains.MenuItems(SetMemoryDomain, CurrentDomain.Name)
				.ToArray());
		}

		private void NewWatchMenuItem_Click(object sender, EventArgs e)
		{
			var we = new WatchEditor
			{
				InitialLocation = this.ChildPointToScreen(WatchListView),
				MemoryDomains = MemoryDomains
			};
			we.SetWatch(CurrentDomain);
			we.ShowHawkDialog(this);
			if (we.DialogResult == DialogResult.OK)
			{
				_watches.Add(we.Watches[0]);
				Changes();
				UpdateWatchCount();
				WatchListView.RowCount = _watches.Count;
				GeneralUpdate();
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

				WatchListView.RowCount = _watches.Count;
				GeneralUpdate();
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
				var poke = new RamPoke
				{
					InitialLocation = this.ChildPointToScreen(WatchListView)
				};

				poke.SetWatch(SelectedWatches);

				if (poke.ShowHawkDialog(this) == DialogResult.OK)
				{
					GeneralUpdate();
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

			WatchListView.RowCount = _watches.Count;
			Changes();
			UpdateWatchCount();
		}

		private void ClearChangeCountsMenuItem_Click(object sender, EventArgs e)
		{
			_watches.ClearChangeCounts();
			GeneralUpdate();
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

			var indices = indexes.Select(t => t - 1);

			WatchListView.DeselectAll();
			foreach (var t in indices)
			{
				WatchListView.SelectRow(t, true);
			}

			WatchListView.RowCount = _watches.Count;
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

			var newIndices = indices.Select(t => t + 1);

			WatchListView.DeselectAll();
			foreach (var t in newIndices)
			{
				WatchListView.SelectRow(t, true);
			}

			Changes();
			WatchListView.RowCount = _watches.Count;
		}

		private void MoveTopMenuItem_Click(object sender, EventArgs e)
		{
			var indexes = SelectedIndices.ToList();
			if (!indexes.Any())
			{
				return;
			}

			for (int i = 0; i < indexes.Count; i++)
			{
				var watch = _watches[indexes[i]];
				_watches.RemoveAt(indexes[i]);
				_watches.Insert(i, watch);
				indexes[i] = i;
			}

			Changes();

			WatchListView.DeselectAll();
			foreach (var t in indexes)
			{
				WatchListView.SelectRow(t, true);
			}

			WatchListView.RowCount = _watches.Count;
		}

		private void MoveBottomMenuItem_Click(object sender, EventArgs e)
		{
			var indices = SelectedIndices.ToList();
			if (indices.Count == 0)
			{
				return;
			}

			for (var i = 0; i < indices.Count; i++)
			{
				var watch = _watches[indices[i] - i];
				_watches.RemoveAt(indices[i] - i);
				_watches.Insert(_watches.Count, watch);
			}

			var newInd = new List<int>();
			for (int i = 0, x = _watches.Count - indices.Count; i < indices.Count; i++, x++)
			{
				newInd.Add(x);
			}

			WatchListView.DeselectAll();
			foreach (var t in newInd)
			{
				WatchListView.SelectRow(t, true);
			}

			Changes();
			WatchListView.RowCount = _watches.Count;
		}

		private void SelectAllMenuItem_Click(object sender, EventArgs e)
		{
			WatchListView.SelectAll();
		}

		#endregion

		#region Options

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			WatchesOnScreenMenuItem.Checked = Config.DisplayRamWatch;
			SaveWindowPositionMenuItem.Checked = Settings.SaveWindowPosition;
			AlwaysOnTopMenuItem.Checked = Settings.TopMost;
			FloatingWindowMenuItem.Checked = Settings.FloatingWindow;
		}

		private void DefinePreviousValueSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			PreviousFrameMenuItem.Checked = Config.RamWatchDefinePrevious == PreviousType.LastFrame;
			LastChangeMenuItem.Checked = Config.RamWatchDefinePrevious == PreviousType.LastChange;
			OriginalMenuItem.Checked = Config.RamWatchDefinePrevious == PreviousType.Original;
		}

		private void PreviousFrameMenuItem_Click(object sender, EventArgs e)
		{
			Config.RamWatchDefinePrevious = PreviousType.LastFrame;
		}

		private void LastChangeMenuItem_Click(object sender, EventArgs e)
		{
			Config.RamWatchDefinePrevious = PreviousType.LastChange;
		}

		private void OriginalMenuItem_Click(object sender, EventArgs e)
		{
			Config.RamWatchDefinePrevious = PreviousType.Original;
		}

		private void WatchesOnScreenMenuItem_Click(object sender, EventArgs e)
		{
			Config.DisplayRamWatch ^= true;

			if (!Config.DisplayRamWatch)
			{
				GlobalWin.OSD.ClearRamWatches();
			}
			else
			{
				GeneralUpdate();
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
					.First(x => x.Name == "GeneratedColumnsSubMenu"));

			RamWatchMenu.Items.Add(WatchListView.ToColumnsMenu(ColumnToggleCallback));

			Config.DisplayRamWatch = false;

			RefreshFloatingWindowControl(Settings.FloatingWindow);

			WatchListView.AllColumns.Clear();
			SetColumns();
		}

		#endregion

		#region Dialog, Context Menu, and ListView Events

		private void RamWatch_Load(object sender, EventArgs e)
		{
			// Hack for previous config settings
			if (Settings.Columns.Any(c => string.IsNullOrWhiteSpace(c.Text)))
			{
				Settings = new RamWatchSettings();
			}

			TopMost = Settings.TopMost;
			_watches = new WatchList(MemoryDomains, Emu.SystemId);
			LoadConfigSettings();
			RamWatchMenu.Items.Add(WatchListView.ToColumnsMenu(ColumnToggleCallback));
			UpdateStatusBar();
			PokeAddressToolBarItem.Enabled =
				FreezeAddressToolBarItem.Enabled =
				SelectedIndices.Any() &&
				SelectedWatches.All(w => w.Domain.Writable);
		}

		private void ColumnToggleCallback()
		{
			Settings.Columns = WatchListView.AllColumns;
		}

		private void RamWatch_DragDrop(object sender, DragEventArgs e)
		{
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (Path.GetExtension(filePaths[0]) == ".wch")
			{
				_watches.Load(filePaths[0], append: false);
				Config.RecentWatches.Add(_watches.CurrentFileName);
				WatchListView.RowCount = _watches.Count;
				GeneralUpdate();
			}
		}

		private void ListViewContextMenu_Opening(object sender, CancelEventArgs e)
		{
			var indexes = WatchListView.SelectedRows.ToList();

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
				MoveTopContextMenuItem.Visible =
				MoveBottomContextMenuItem.Visible =
				indexes.Count > 0;

			ReadBreakpointContextMenuItem.Visible =
			WriteBreakpointContextMenuItem.Visible =
			Separator6.Visible =
				SelectedWatches.Any() &&
				Debuggable != null &&
				Debuggable.MemoryCallbacksAvailable() &&
				SelectedWatches.All(w => w.Domain.Name == (MemoryDomains != null ? MemoryDomains.SystemBus.Name : ""));

			PokeContextMenuItem.Enabled =
				FreezeContextMenuItem.Visible =
				SelectedIndices.Any()
				&& SelectedWatches.All(w => w.Domain.Writable);

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
				Tools.Load<HexEditor>();

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
				var debugger = Tools.Load<GenericDebugger>();

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
				var debugger = Tools.Load<GenericDebugger>();

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
			PokeAddressToolBarItem.Enabled =
				FreezeAddressToolBarItem.Enabled =
				SelectedIndices.Any()
				&& SelectedWatches.All(w => w.Domain.Writable);
		}

		private void WatchListView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			EditWatch();
		}

		private void WatchListView_ColumnClick(object sender, InputRoll.ColumnClickEventArgs e)
		{
			OrderColumn(e.Column);
		}

		private void ErrorIconButton_Click(object sender, EventArgs e)
		{
			var items = _watches
				.Where(watch => watch.Address >= watch.Domain.Size)
				.ToList(); // enumerate because _watches is about to be changed

			foreach (var item in items)
			{
				_watches.Remove(item);
			}

			WatchListView.RowCount = _watches.Count;
			GeneralUpdate();
			UpdateWatchCount();
			UpdateStatusBar();
		}

		#endregion
		#endregion

		// Stupid designer
		protected void DragEnterWrapper(object sender, DragEventArgs e)
		{
			GenericDragEnter(sender, e);
		}
	}
}
