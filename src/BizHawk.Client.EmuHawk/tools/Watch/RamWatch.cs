using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Common;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	public partial class RamWatch : ToolFormBase, IToolFormAutoConfig
	{
		public static Icon ToolIcon
			=> Resources.WatchIcon;

		private WatchList _watches;

		private string _sortedColumn;
		private bool _sortReverse;

		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }

		[RequiredService]
		private IEmulator Emu { get; set; }

		[OptionalService]
		private IDebuggable Debuggable { get; set; }

		protected override string WindowTitleStatic => "RAM Watch";

		public RamWatch()
		{
			InitializeComponent();
			newToolStripMenuItem.Image = Resources.AddWatch;
			EditContextMenuItem.Image = Resources.Cut;
			RemoveContextMenuItem.Image = Resources.Delete;
			DuplicateContextMenuItem.Image = Resources.Duplicate;
			SplitContextMenuItem.Image = Resources.Split;
			PokeContextMenuItem.Image = Resources.Poke;
			FreezeContextMenuItem.Image = Resources.Freeze;
			UnfreezeAllContextMenuItem.Image = Resources.Unfreeze;
			InsertSeperatorContextMenuItem.Image = Resources.InsertSeparator;
			MoveUpContextMenuItem.Image = Resources.MoveUp;
			MoveDownContextMenuItem.Image = Resources.MoveDown;
			MoveTopContextMenuItem.Image = Resources.MoveTop;
			MoveBottomContextMenuItem.Image = Resources.MoveBottom;
			ErrorIconButton.Image = Resources.ExclamationRed;
			newToolStripButton.Image = Resources.NewFile;
			openToolStripButton.Image = Resources.OpenFile;
			saveToolStripButton.Image = Resources.SaveAs;
			newWatchToolStripButton.Image = Resources.AddWatch;
			editWatchToolStripButton.Image = Resources.Cut;
			cutToolStripButton.Image = Resources.Delete;
			clearChangeCountsToolStripButton.Image = Resources.Placeholder;
			duplicateWatchToolStripButton.Image = Resources.Duplicate;
			SplitWatchToolStripButton.Image = Resources.Split;
			PokeAddressToolBarItem.Image = Resources.Poke;
			FreezeAddressToolBarItem.Image = Resources.Freeze;
			seperatorToolStripButton.Image = Resources.InsertSeparator;
			moveUpToolStripButton.Image = Resources.MoveUp;
			moveDownToolStripButton.Image = Resources.MoveDown;
			NewListMenuItem.Image = Resources.NewFile;
			OpenMenuItem.Image = Resources.OpenFile;
			SaveMenuItem.Image = Resources.SaveAs;
			RecentSubMenu.Image = Resources.Recent;
			NewWatchMenuItem.Image = Resources.Find;
			EditWatchMenuItem.Image = Resources.Cut;
			RemoveWatchMenuItem.Image = Resources.Delete;
			DuplicateWatchMenuItem.Image = Resources.Duplicate;
			SplitWatchMenuItem.Image = Resources.Split;
			PokeAddressMenuItem.Image = Resources.Poke;
			FreezeAddressMenuItem.Image = Resources.Freeze;
			InsertSeparatorMenuItem.Image = Resources.InsertSeparator;
			MoveUpMenuItem.Image = Resources.MoveUp;
			MoveDownMenuItem.Image = Resources.MoveDown;
			MoveTopMenuItem.Image = Resources.MoveTop;
			MoveBottomMenuItem.Image = Resources.MoveBottom;
			Icon = ToolIcon;

			ToolStripMenuItemEx deduperMenuItem = new() { Text = "Purge Duplicate Watches" };
			deduperMenuItem.Click += (_, _) =>
			{
				if (!this.ModalMessageBox2(
					caption: "Purge duplicates?",
					text: "This will go through each watch starting from the top,\n"
						+ "removing it if its (domain & addr & size) has been seen before.\n"
						+ "Neither the endianness nor display type are considered.\n"
						+ "Overlapping watches also aren't considered.",
					useOKCancel: true))
				{
					return;
				}
				var seen = new SortedList<(string Domain, long Addr)>[] { null, new(), new(), null, new() };
				//TODO check if it's faster to build up a toRemove list and then batch Remove (by ref) at end
				var i = 0;
				Watch w;
				while (i < _watches.Count)
				{
					w = _watches[i];
					if (w.IsSeparator)
					{
						i++;
						continue;
					}
					var l = seen[(int) w.Size];
					var tuple = (w.Domain.Name, w.Address);
					if (l.Contains(tuple))
					{
						_watches.RemoveAt(i);
						continue;
					}
					l.Add(tuple);
					i++;
				}
			};
			WatchesSubMenu.DropDownItems.Insert(11, deduperMenuItem);

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

		public override bool IsActive => Config!.DisplayRamWatch || base.IsActive;
		public override bool IsLoaded => base.IsActive;

		private void SetColumns()
		{
			WatchListView.AllColumns.AddRange(Settings.Columns);
			WatchListView.Refresh();
		}

		[ConfigPersist]
		public RamWatchSettings Settings { get; set; }

		public class RamWatchSettings
		{
			public RamWatchSettings()
			{
				Columns = new List<RollColumn>
				{
					new(name: WatchList.Address, widthUnscaled: 60, text: "Address"),
					new(name: WatchList.Value, widthUnscaled: 59, text: "Value"),
					new(name: WatchList.Prev, widthUnscaled: 59, text: "Prev") { Visible = false },
					new(name: WatchList.ChangesCol, widthUnscaled: 60, text: "Changes"),
					new(name: WatchList.Diff, widthUnscaled: 59, text: "Diff") { Visible = false },
					new(name: WatchList.Type, widthUnscaled: 55, text: "Type") { Visible = false },
					new(name: WatchList.Domain, widthUnscaled: 55, text: "Domain"),
					new(name: WatchList.Notes, widthUnscaled: 128, text: "Notes"),
				};
			}

			public List<RollColumn> Columns { get; set; }

			public bool DoubleClickToPoke { get; set; } = true;
		}

		private IEnumerable<int> SelectedIndices => WatchListView.SelectedRows;
		private IEnumerable<Watch> SelectedItems => SelectedIndices.Select(index => _watches[index]);
		private IEnumerable<Watch> SelectedWatches => SelectedItems.Where(x => !x.IsSeparator);
		private IEnumerable<Watch> SelectedSeparators => SelectedItems.Where(x => x.IsSeparator);

		private bool MayPokeAllSelected
			=> SelectedWatches.Any() && SelectedWatches.All(static w => w.Domain.Writable);

		public IEnumerable<Watch> Watches => _watches.Where(x => !x.IsSeparator);

		protected override void GeneralUpdate() => FrameUpdate();

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
			if (!_watches.Changes) return true;
			var result = DialogController.DoWithTempMute(() => this.ModalMessageBox3(
				caption: "Closing with Unsaved Changes",
				icon: EMsgBoxIcon.Question,
				text: "Save watch file?"));
			if (result is null) return false;
			if (result.Value)
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
			else
			{
				_watches.Changes = false;
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
					Config.RecentWatches.HandleLoadError(MainForm, path);
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
							MayPokeAllSelected;
				}
			}
		}

		public override void Restart()
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
				_watches.RefreshDomains(MemoryDomains, Config.RamWatchDefinePrevious);
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

		private void MinimalUpdate()
		{
			if ((!IsHandleCreated || IsDisposed) && !Config.DisplayRamWatch)
			{
				return;
			}

			if (_watches.Any())
			{
				_watches.UpdateValues(Config.RamWatchDefinePrevious);
				DisplayOnScreenWatches();
			}
		}

		private void FrameUpdate()
		{
			if ((!IsHandleCreated || IsDisposed) && !Config.DisplayRamWatch)
			{
				return;
			}

			DisplayManager.OSD.ClearRamWatches();
			if (_watches.Any())
			{
				_watches.UpdateValues(Config.RamWatchDefinePrevious);
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
				DisplayManager.OSD.ClearRamWatches();
				for (var i = 0; i < _watches.Count; i++)
				{
					var frozen = !_watches[i].IsSeparator && MainForm.CheatList.IsActive(_watches[i].Domain, _watches[i].Address);
					DisplayManager.OSD.AddRamWatch(
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

		private void PasteWatchesFromClipBoard()
		{
			var data = Clipboard.GetDataObject();

			if (data != null && data.GetDataPresent(DataFormats.Text))
			{
				var clipboardRows = ((string)data.GetData(DataFormats.Text)).Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var row in clipboardRows)
				{
					var watch = Watch.FromString(row, MemoryDomains);
					if (watch is not null)
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

		/// <summary>
		/// Open Edit or Poke window for selected watch depending on user setting
		/// </summary>
		private void OpenWatch()
		{
			if (Settings.DoubleClickToPoke)
			{
				PokeAddress();
			}
			else
			{
				EditWatch();
			}
		}

		private void EditWatch(bool duplicate = false)
		{
			var indexes = SelectedIndices.ToList();

			if (SelectedWatches.Any())
			{
				var we = new WatchEditor
				{
					InitialLocation = this.ChildPointToScreen(WatchListView),
					MemoryDomains = MemoryDomains
				};

				we.SetWatch(SelectedWatches.First().Domain, SelectedWatches, duplicate ? WatchEditor.Mode.Duplicate : WatchEditor.Mode.Edit);

				if (this.ShowDialogWithTempMute(we).IsOk())
				{
					if (duplicate)
					{
						_watches.InsertRange(WatchListView.SelectionEndIndex!.Value + 1, we.Watches); //TODO should be able to reduce the number of enumerations of selected rows if necessary --yoshi
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
					Changes();
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

				if (this.ShowDialogWithTempMute(inputPrompt).IsOk())
				{
					Changes();

					var selection = SelectedSeparators.ToList();
					for (var i = 0; i < selection.Count; i++)
					{
						var sep = selection[i];
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
				case Common.WatchDisplayType.Binary:
					s += "b";
					break;
				case Common.WatchDisplayType.FixedPoint_12_4:
					s += "F";
					break;
				case Common.WatchDisplayType.FixedPoint_16_16:
					s += "F6";
					break;
				case Common.WatchDisplayType.FixedPoint_20_12:
					s += "F2";
					break;
				case Common.WatchDisplayType.Float:
					s += "f";
					break;
				case Common.WatchDisplayType.Hex:
					s += "h";
					break;
				case Common.WatchDisplayType.Signed:
					s += "s";
					break;
				case Common.WatchDisplayType.Unsigned:
					s += "u";
					break;
			}

			return s + (w.BigEndian ? "B" : "L");
		}

		private void LoadConfigSettings()
		{
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
						MayPokeAllSelected;
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
			_sortReverse = !_sortReverse;
			WatchListView.Refresh();
		}

		private string CurrentFileName()
		{
			return !string.IsNullOrWhiteSpace(_watches.CurrentFileName)
				? _watches.CurrentFileName
				: Game.FilesystemSafeName();
		}

		private void SaveAs()
		{
			var result = _watches.SaveAs(GetWatchSaveFileFromUser(CurrentFileName()));
			if (result)
			{
				UpdateStatusBar(saved: true);
				Config.RecentWatches.Add(_watches.CurrentFileName);
			}
		}

		private void SaveConfigSettings()
		{
			Settings.Columns = WatchListView.AllColumns;
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

			ErrorIconButton.Visible = _watches.Any(static watch => !watch.IsSeparator && !watch.IsValid);

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
			else if (!_watches[index].IsValid)
			{
				color = Color.PeachPuff;
			}
			else if (MainForm.CheatList.IsActive(_watches[index].Domain, _watches[index].Address))
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
			=> RecentSubMenu.ReplaceDropDownItems(Config!.RecentWatches.RecentMenu(this, LoadFileFromRecent, "Watches"));

		private void WatchesSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			EditWatchMenuItem.Enabled =
				DuplicateWatchMenuItem.Enabled =
				SplitWatchMenuItem.Enabled =
				RemoveWatchMenuItem.Enabled =
				MoveUpMenuItem.Enabled =
				MoveDownMenuItem.Enabled =
				MoveTopMenuItem.Enabled =
				MoveBottomMenuItem.Enabled =
					WatchListView.AnyRowsSelected;

			SplitWatchMenuItem.Enabled = MaySplitAllSelected;

			PokeAddressMenuItem.Enabled =
				FreezeAddressMenuItem.Enabled =
					MayPokeAllSelected;
		}

		private MemoryDomain _currentDomain;

		private MemoryDomain CurrentDomain
		{
			get => _currentDomain ?? MemoryDomains.MainMemory;
			set => _currentDomain = value;
		}

		private void MemoryDomainsSubMenu_DropDownOpened(object sender, EventArgs e)
			=> MemoryDomainsSubMenu.ReplaceDropDownItems(MemoryDomains.MenuItems(SetMemoryDomain, CurrentDomain.Name).ToArray());

		private void NewWatchMenuItem_Click(object sender, EventArgs e)
		{
			var we = new WatchEditor
			{
				InitialLocation = this.ChildPointToScreen(WatchListView),
				MemoryDomains = MemoryDomains
			};
			we.SetWatch(CurrentDomain);
			if (!this.ShowDialogWithTempMute(we).IsOk()) return;
			_watches.Add(we.Watches[0]);
			Changes();
			UpdateWatchCount();
			WatchListView.RowCount = _watches.Count;
			GeneralUpdate();
		}

		private void EditWatchMenuItem_Click(object sender, EventArgs e)
		{
			EditWatch();
		}

		private void RemoveWatchMenuItem_Click(object sender, EventArgs e)
		{
			if (!WatchListView.AnyRowsSelected) return;
			foreach (var index in SelectedIndices.OrderDescending().ToList()) _watches.RemoveAt(index);
			WatchListView.RowCount = _watches.Count;
			GeneralUpdate();
			UpdateWatchCount();
		}

		private void DuplicateWatchMenuItem_Click(object sender, EventArgs e)
		{
			EditWatch(duplicate: true);
		}

		private static (Watch A, Watch B) SplitWatch(Watch ab)
		{
			var newSize = ab.Size switch
			{
				WatchSize.DWord => WatchSize.Word,
				WatchSize.Word => WatchSize.Byte,
				_ => throw new InvalidOperationException()
			};
			var a = Watch.GenerateWatch(ab.Domain, ab.Address, newSize, ab.Type, ab.BigEndian, ab.Notes);
			var b = Watch.GenerateWatch(ab.Domain, ab.Address + (int) newSize, newSize, ab.Type, ab.BigEndian, ab.Notes);
			return ab.BigEndian ? (a, b) : (b, a);
		}

		private void SplitWatchAt(int index)
		{
			var ab = _watches[index];
			if (!ab.IsSplittable) return;
			var (a, b) = SplitWatch(ab);
			_watches[index] = a;
			_watches.Insert(index + 1, b);
		}

		private void SplitWatchMenuItem_Click(object sender, EventArgs e)
		{
			var indices = SelectedIndices.ToList();
			for (var indexIndex = indices.Count - 1; indexIndex >= 0; indexIndex--) SplitWatchAt(indices[indexIndex]);
			Changes();
			UpdateWatchCount();
			WatchListView.RowCount = _watches.Count;
			GeneralUpdate();
		}

		private void PokeAddressMenuItem_Click(object sender, EventArgs e)
		{
			PokeAddress();
		}

		private void PokeAddress()
		{
			if (SelectedWatches.Any())
			{
				var poke = new RamPoke(DialogController, SelectedWatches, MainForm.CheatList)
				{
					InitialLocation = this.ChildPointToScreen(WatchListView)
				};

				if (this.ShowDialogWithTempMute(poke).IsOk())
				{
					GeneralUpdate();
				}
			}
		}

		private void FreezeAddressMenuItem_Click(object sender, EventArgs e)
		{
			var allCheats = SelectedWatches.All(x => MainForm.CheatList.IsActive(x.Domain, x.Address));
			if (allCheats)
			{
				MainForm.CheatList.RemoveRange(SelectedWatches);
			}
			else
			{
				MainForm.CheatList.AddRange(
					SelectedWatches.Select(w => new Cheat(w, w.Value)));
			}
		}

		private void InsertSeparatorMenuItem_Click(object sender, EventArgs e)
		{
			_watches.Insert(WatchListView.SelectionStartIndex ?? _watches.Count, SeparatorWatch.Instance);
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
			if (indices.Count == 0
				|| indices[indices.Count - 1] == _watches.Count - 1) // at end already
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
			=> WatchListView.ToggleSelectAll();

		private void SettingsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			WatchesOnScreenMenuItem.Checked = Config.DisplayRamWatch;
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
			Config.DisplayRamWatch = !Config.DisplayRamWatch;
			if (!Config.DisplayRamWatch)
			{
				DisplayManager.OSD.ClearRamWatches();
			}
			else
			{
				GeneralUpdate();
			}
		}

		private void DoubleClickActionSubMenu_DropDownOpening(object sender, EventArgs e)
		{
			DoubleClickToEditMenuItem.Checked = !Settings.DoubleClickToPoke;
			DoubleClickToPokeMenuItem.Checked = Settings.DoubleClickToPoke;
		}

		private void DoubleClickToEditMenuItem_Click(object sender, EventArgs e)
		{
			Settings.DoubleClickToPoke = false;
		}

		private void DoubleClickToPokeMenuItem_Click(object sender, EventArgs e)
		{
			Settings.DoubleClickToPoke = true;
		}

		[RestoreDefaults]
		private void RestoreDefaultsMenuItem()
		{
			Settings = new RamWatchSettings();

			RamWatchMenu.Items.Remove(
				RamWatchMenu.Items
					.OfType<ToolStripMenuItem>()
					.First(x => x.Name == "GeneratedColumnsSubMenu"));

			RamWatchMenu.Items.Add(WatchListView.ToColumnsMenu(ColumnToggleCallback));
			Config.DisplayRamWatch = false;
			WatchListView.AllColumns.Clear();
			SetColumns();
			WatchListView.Refresh();
		}

		private void RamWatch_Load(object sender, EventArgs e)
		{
			_watches = new WatchList(MemoryDomains, Emu.SystemId);
			LoadConfigSettings();
			RamWatchMenu.Items.Add(WatchListView.ToColumnsMenu(ColumnToggleCallback));
			UpdateStatusBar();
			PokeAddressToolBarItem.Enabled =
				FreezeAddressToolBarItem.Enabled =
					MayPokeAllSelected;
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

		private bool MaySplitAllSelected
			=> SelectedWatches.Any() && SelectedWatches.All(static w => w.IsSplittable);

		private void ListViewContextMenu_Opening(object sender, CancelEventArgs e)
		{
			EditContextMenuItem.Visible =
				RemoveContextMenuItem.Visible =
				DuplicateContextMenuItem.Visible =
				SplitContextMenuItem.Visible =
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
					WatchListView.AnyRowsSelected;

			var sysBusName = MemoryDomains?.SystemBus?.Name ?? string.Empty;
			ReadBreakpointContextMenuItem.Visible = WriteBreakpointContextMenuItem.Visible
				= Separator6.Visible
					= Debuggable?.MemoryCallbacksAvailable() is true
						&& SelectedWatches.Any() && SelectedWatches.All(w => w.Domain.Name == sysBusName);

			DuplicateContextMenuItem.Enabled = SelectedWatches.Any();

			SplitContextMenuItem.Enabled = MaySplitAllSelected;

			PokeContextMenuItem.Enabled =
				FreezeContextMenuItem.Visible =
					MayPokeAllSelected;

			var allCheats = SelectedWatches.All(x => MainForm.CheatList.IsActive(x.Domain, x.Address));

			if (allCheats)
			{
				FreezeContextMenuItem.Text = "&Unfreeze Address";
				FreezeContextMenuItem.Image = Resources.Unfreeze;
			}
			else
			{
				FreezeContextMenuItem.Text = "&Freeze Address";
				FreezeContextMenuItem.Image = Resources.Freeze;
			}

			UnfreezeAllContextMenuItem.Visible = MainForm.CheatList.AnyActive;

			ViewInHexEditorContextMenuItem.Visible = SelectedWatches.CountIsExactly(1);

			newToolStripMenuItem.Visible = !WatchListView.AnyRowsSelected;
		}

		private void UnfreezeAllContextMenuItem_Click(object sender, EventArgs e)
		{
			MainForm.CheatList.Clear();
		}

		private void ViewInHexEditorContextMenuItem_Click(object sender, EventArgs e)
		{
			var selected = SelectedWatches.ToList();
			if (selected.Any())
			{
				Tools.Load<HexEditor>();
				ViewInHexEditor(
					selected[0].Domain,
					selected.Select(static x => x.Domain).Distinct().CountIsAtLeast(2)
						? new[] { selected[0].Address }
						: selected.Select(static x => x.Address),
					selected[0].Size);
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
			if (e.IsPressed(Keys.Delete))
			{
				RemoveWatchMenuItem_Click(sender, e);
			}
			else if (e.IsCtrl(Keys.C))
			{
				CopyWatchesToClipBoard();
			}
			else if (e.IsCtrl(Keys.V))
			{
				PasteWatchesFromClipBoard();
			}
			else if (e.IsPressed(Keys.Enter))
			{
				OpenWatch();
			}
		}

		private void WatchListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			PokeAddressToolBarItem.Enabled =
				FreezeAddressToolBarItem.Enabled =
					MayPokeAllSelected;
		}

		private void WatchListView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			OpenWatch();
		}

		private void WatchListView_ColumnClick(object sender, InputRoll.ColumnClickEventArgs e)
			=> OrderColumn(e.Column!);

		private void ErrorIconButton_Click(object sender, EventArgs e)
		{
			var items = _watches
				.Where(watch => !watch.IsValid)
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

		// Stupid designer
		protected void DragEnterWrapper(object sender, DragEventArgs e)
		{
			GenericDragEnter(sender, e);
		}
	}
}
