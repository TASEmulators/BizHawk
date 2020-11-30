using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Client.EmuHawk.Properties;

namespace BizHawk.Client.EmuHawk
{
	public partial class BookmarksBranchesBox : UserControl
	{
		private const string BranchNumberColumnName = "BranchNumberColumn";
		private const string FrameColumnName = "FrameColumn";
		private const string UserTextColumnName = "TextColumn";

		private readonly ScreenshotForm _screenshot = new ScreenshotForm();

		private ITasMovie Movie => Tastudio.CurrentTasMovie;
		private ITasBranchCollection Branches => Movie.Branches;

		private IMainFormForTools MainForm => Tastudio.MainForm;
		private TasBranch _backupBranch;
		private BranchUndo _branchUndo = BranchUndo.None;

		private enum BranchUndo
		{
			Load, Update, Text, Remove, None
		}

		public Action<int> LoadedCallback { get; set; }

		public Action<int> SavedCallback { get; set; }

		public Action<int> RemovedCallback { get; set; }

		public TAStudio Tastudio { get; set; }

		public int HoverInterval
		{
			get => BranchView.HoverInterval;
			set => BranchView.HoverInterval = value;
		}

		public BookmarksBranchesBox()
		{
			InitializeComponent();
			UndoBranchButton.Image = Resources.Undo;
			JumpToBranchButton.Image = Resources.JumpTo;
			UpdateBranchButton.Image = Resources.Reboot;
			AddWithTextBranchButton.Image = Resources.AddEdit;
			AddBranchButton.Image = Resources.Add;
			LoadBranchButton.Image = Resources.Debugger;
			AddBranchContextMenu.Image = Resources.Add;
			AddBranchWithTextContextMenuItem.Image = Resources.AddEdit;
			LoadBranchContextMenuItem.Image = Resources.Debugger;
			UpdateBranchContextMenuItem.Image = Resources.Reboot;
			EditBranchTextContextMenuItem.Image = Resources.Pencil;
			JumpToBranchContextMenuItem.Image = Resources.JumpTo;
			UndoBranchToolStripMenuItem.Image = Resources.Undo;
			RemoveBranchContextMenuItem.Image = Resources.Delete;
			SetupColumns();
			BranchView.QueryItemText += QueryItemText;
			BranchView.QueryItemBkColor += QueryItemBkColor;
		}

		private void SetupColumns()
		{
			BranchView.AllColumns.Clear();
			BranchView.AllColumns.AddRange(new[]
			{
				new RollColumn
				{
					Name = BranchNumberColumnName,
					Text = "#",
					UnscaledWidth = 30,
					Type = ColumnType.Text
				},
				new RollColumn
				{
					Name = FrameColumnName,
					Text = "Frame",
					UnscaledWidth = 64,
					Type = ColumnType.Text
				},
				new RollColumn
				{
					Name = UserTextColumnName,
					Text = "UserText",
					UnscaledWidth = 90,
					Type = ColumnType.Text
				}
			});
		}

		private void QueryItemText(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			text = "";

			// This could happen if the control is told to redraw while Tastudio is rebooting, as we would not have a TasMovie just yet
			if (Tastudio.CurrentTasMovie == null)
			{
				return;
			}

			text = column.Name switch
			{
				BranchNumberColumnName => index.ToString(),
				FrameColumnName => Branches[index].Frame.ToString(),
				UserTextColumnName => Branches[index].UserText,
				_ => text
			};
		}

		private void QueryItemBkColor(int index, RollColumn column, ref Color color)
		{
			// This could happen if the control is told to redraw while Tastudio is rebooting, as we would not have a TasMovie just yet
			if (Tastudio.CurrentTasMovie == null)
			{
				return;
			}

			var branch = Branches[index];
			if (branch != null)
			{
				var record = Movie[branch.Frame];
				if (index == Branches.Current)
				{
					color = TAStudio.CurrentFrame_InputLog;
				}
				else if (record.Lagged.HasValue)
				{
					color = record.Lagged.Value
						? TAStudio.LagZone_InputLog
						: TAStudio.GreenZone_InputLog;
				}
			}

			// Highlight the branch cell a little, if hovering over it
			if (BranchView.CurrentCell.IsDataCell()
				&& BranchView.CurrentCell.Column.Name == BranchNumberColumnName &&
				column.Name == BranchNumberColumnName && 
				index == BranchView.CurrentCell.RowIndex)
			{
				color = Color.FromArgb((byte)(color.A - 24), (byte)(color.R - 24), (byte)(color.G - 24), (byte)(color.B - 24));
			}
		}

		/// <summary>
		/// Add a new branch.
		/// </summary>
		public void Branch()
		{
			var branch = CreateBranch();
			Movie.Branches.NewBranchText = ""; // reset every time it's used
			Branches.Add(branch);
			BranchView.RowCount = Branches.Count;
			Branches.Current = Branches.Count - 1;
			Movie.TasSession.UpdateValues(Tastudio.Emulator.Frame, Branches.Current);
			BranchView.ScrollToIndex(Branches.Current);
			BranchView.DeselectAll();
			Select(Branches.Current, true);
			BranchView.Refresh();
			Tastudio.RefreshDialog();
			MainForm.UpdateStatusSlots();
		}

		public TasBranch SelectedBranch => BranchView.AnyRowsSelected
			? Branches[BranchView.SelectedRows.First()]
			: null;

		private TasBranch CreateBranch()
		{
			return new TasBranch
			{
				Frame = Tastudio.Emulator.Frame,
				CoreData = Tastudio.StatableEmulator.CloneSavestate(),
				InputLog = Movie.GetLogEntries().Clone(),
				CoreFrameBuffer = MainForm.MakeScreenshotImage(),
				OSDFrameBuffer = MainForm.CaptureOSD(),
				ChangeLog = new TasMovieChangeLog(Movie),
				TimeStamp = DateTime.Now,
				Markers = Movie.Markers.DeepClone(),
				UserText = Movie.Branches.NewBranchText
			};
		}

		private void LoadBranch(TasBranch branch)
		{
			if (Tastudio.Settings.OldControlSchemeForBranches && !Tastudio.TasPlaybackBox.RecordingMode)
			{
				JumpToBranchToolStripMenuItem_Click(null, null);
				return;
			}

			Movie.LoadBranch(branch);
			Tastudio.LoadState(new KeyValuePair<int, Stream>(branch.Frame, new MemoryStream(branch.CoreData, false)));
			Movie.TasStateManager.Capture(Tastudio.Emulator.Frame, Tastudio.Emulator.AsStatable());
			QuickBmpFile.Copy(new BitmapBufferVideoProvider(branch.CoreFrameBuffer), Tastudio.VideoProvider);

			if (Tastudio.Settings.OldControlSchemeForBranches && Tastudio.TasPlaybackBox.RecordingMode)
				Movie.Truncate(branch.Frame);

			MainForm.PauseOnFrame = null;
			Tastudio.RefreshDialog();
		}

		private void LoadSelectedBranch()
		{
			if (SelectedBranch != null)
			{
				int index = BranchView.SelectedRows.First();
				Branches.Current = index;
				LoadBranch(SelectedBranch);
				BranchView.Refresh();
				Tastudio.MainForm.AddOnScreenMessage($"Loaded branch {Branches.Current}");
			}
		}

		private void BranchesContextMenu_Opening(object sender, CancelEventArgs e)
		{
			RemoveBranchContextMenuItem.Enabled = SelectedBranch != null;

			UpdateBranchContextMenuItem.Enabled =
			LoadBranchContextMenuItem.Enabled =
			EditBranchTextContextMenuItem.Enabled =
			JumpToBranchContextMenuItem.Enabled = 
				BranchView.SelectedRows.Count() == 1;
		}

		private void AddBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Branch();
			SavedCallback?.Invoke(Branches.Count - 1);
			Tastudio.MainForm.AddOnScreenMessage($"Added branch {Branches.Current}");
		}

		private void AddBranchWithTexToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Branch();
			EditBranchTextPopUp(Branches.Current);
			SavedCallback?.Invoke(Branches.Count - 1);
			Tastudio.MainForm.AddOnScreenMessage($"Added branch {Branches.Current}");
		}

		private void LoadBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			_backupBranch = CreateBranch();

			var currentHashes = Branches.Select(b => b.Uuid.GetHashCode()).ToList();
			do
			{
				_backupBranch.Uuid = Guid.NewGuid();
			}
			while (currentHashes.Contains(_backupBranch.Uuid.GetHashCode()));

			UndoBranchToolStripMenuItem.Enabled = UndoBranchButton.Enabled = true;
			UndoBranchToolStripMenuItem.Text = "Undo Branch Load";
			toolTip1.SetToolTip(UndoBranchButton, "Undo Branch Load");
			_branchUndo = BranchUndo.Load;

			if (BranchView.AnyRowsSelected)
			{
				LoadSelectedBranch();
				LoadedCallback?.Invoke(BranchView.SelectedRows.First());
			}
		}

		private void UpdateBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedBranch == null)
			{
				return;
			}

			Branches.Current = BranchView.SelectedRows.First();

			_backupBranch = SelectedBranch.Clone();
			UndoBranchToolStripMenuItem.Enabled = UndoBranchButton.Enabled = true;
			UndoBranchToolStripMenuItem.Text = "Undo Branch Update";
			toolTip1.SetToolTip(UndoBranchButton, "Undo Branch Update");
			_branchUndo = BranchUndo.Update;

			Branches.Replace(SelectedBranch, CreateBranch());
			Tastudio.RefreshDialog();
			SavedCallback?.Invoke(Branches.Current);
			Tastudio.MainForm.AddOnScreenMessage($"Saved branch {Branches.Current}");
		}

		private void EditBranchTextToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedBranch == null)
			{
				return;
			}

			int index = BranchView.SelectedRows.First();
			string oldText = SelectedBranch.UserText;

			if (EditBranchTextPopUp(index))
			{
				_backupBranch = SelectedBranch.Clone();
				_backupBranch.UserText = oldText;
				UndoBranchToolStripMenuItem.Enabled = UndoBranchButton.Enabled = true;
				UndoBranchToolStripMenuItem.Text = "Undo Branch Text Edit";
				toolTip1.SetToolTip(UndoBranchButton, "Undo Branch Text Edit");
				_branchUndo = BranchUndo.Text;

				Tastudio.MainForm.AddOnScreenMessage($"Edited branch {index}");
			}
		}

		private void JumpToBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!BranchView.AnyRowsSelected)
			{
				return;
			}

			int index = BranchView.SelectedRows.First();
			var branch = Branches[index];
			Tastudio.GoToFrame(branch.Frame);
		}

		private void RemoveBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!BranchView.AnyRowsSelected)
			{
				return;
			}

			var indices = BranchView.SelectedRows.ToList();
			var branches = Branches.ToList();
			foreach (var index in indices)
			{
				_backupBranch =  branches[index].Clone();
				Branches.Remove(branches[index]);
				RemovedCallback?.Invoke(index);
				Tastudio.MainForm.AddOnScreenMessage($"Removed branch {index}");

				if (index == Branches.Current)
				{
					Branches.Current = -1;
				}
				else if (index < Branches.Current)
				{
					Branches.Current--;
				}
			}

			UndoBranchToolStripMenuItem.Enabled = UndoBranchButton.Enabled = true;
			UndoBranchToolStripMenuItem.Text = "Undo Branch Removal";
			toolTip1.SetToolTip(UndoBranchButton, "Undo Branch Removal");
			_branchUndo = BranchUndo.Remove;

			BranchView.RowCount = Branches.Count;
			Tastudio.RefreshDialog(refreshBranches: false);
			MainForm.UpdateStatusSlots();
		}

		private void UndoBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (_branchUndo == BranchUndo.Load)
			{
				LoadBranch(_backupBranch);
				LoadedCallback?.Invoke(Branches.IndexOf(_backupBranch));
				Tastudio.MainForm.AddOnScreenMessage("Branch Load canceled");
			}
			else if (_branchUndo == BranchUndo.Update)
			{
				var branch = Branches.SingleOrDefault(b => b.Uuid == _backupBranch.Uuid);
				if (branch != null)
				{
					Branches.Replace(branch, _backupBranch);
					SavedCallback?.Invoke(Branches.IndexOf(_backupBranch));
					Tastudio.MainForm.AddOnScreenMessage("Branch Update canceled");
				}
			}
			else if (_branchUndo == BranchUndo.Text)
			{
				var branch = Branches.SingleOrDefault(b => b.Uuid == _backupBranch.Uuid);
				if (branch != null)
				{
					branch.UserText = _backupBranch.UserText;
				}

				Tastudio.MainForm.AddOnScreenMessage("Branch Text Edit canceled");
			}
			else if (_branchUndo == BranchUndo.Remove)
			{
				Branches.Add(_backupBranch);
				BranchView.RowCount = Branches.Count;
				SavedCallback?.Invoke(Branches.IndexOf(_backupBranch));
				Tastudio.MainForm.AddOnScreenMessage("Branch Removal canceled");
			}

			UndoBranchToolStripMenuItem.Enabled = UndoBranchButton.Enabled = false;
			BranchView.Refresh();
			Tastudio.RefreshDialog();
		}

		public void AddBranchExternal()
		{
			AddBranchToolStripMenuItem_Click(null, null);
		}

		public void LoadBranchExternal(int slot = -1)
		{
			if (Tastudio.AxisEditingMode)
			{
				return;
			}

			if (slot != -1)
			{
				if (Branches[slot] != null)
				{
					Select(slot, true);
				}
				else
				{
					NonExistentBranchMessage(slot);
					return;
				}
			}

			LoadBranchToolStripMenuItem_Click(null, null);
		}

		public void UpdateBranchExternal(int slot = -1)
		{
			if (Tastudio.AxisEditingMode)
			{
				return;
			}

			if (slot != -1)
			{
				if (Branches[slot] != null)
				{
					Select(slot, true);
				}
				else
				{
					//NonExistentBranchMessage(slot); // some people can't get used to creating branches explicitly with unusual hotkeys
					AddBranchExternal(); // so just make a new branch, even though the index may be wrong
					return;
				}
			}

			UpdateBranchToolStripMenuItem_Click(null, null);
		}

		public void RemoveBranchExternal()
		{
			RemoveBranchToolStripMenuItem_Click(null, null);
		}

		public void SelectBranchExternal(int slot)
		{
			if (Tastudio.AxisEditingMode)
			{
				return;
			}

			if (Branches[slot] != null)
			{
				Select(slot, true);
				BranchView.Refresh();
			}
			else
			{
				NonExistentBranchMessage(slot);
			}
		}

		public void SelectBranchExternal(bool next)
		{
			if (SelectedBranch == null)
			{
				Select(Branches.Current, true);
				BranchView.Refresh();
				return;
			}

			int sel = BranchView.SelectedRows.First();
			if (next)
			{
				if (Branches[sel + 1] != null)
				{
					Select(sel, false);
					Select(sel + 1, true);
				}
			}
			else // previous
			{
				if (Branches[sel - 1] != null)
				{
					Select(sel, false);
					Select(sel - 1, true);
				}
			}

			BranchView.Refresh();
		}

		private void UpdateButtons()
		{
			UpdateBranchButton.Enabled =
			LoadBranchButton.Enabled =
			JumpToBranchButton.Enabled =
				BranchView.SelectedRows.Count() == 1;
		}

		private void Select(int index, bool value)
		{
			BranchView.SelectRow(index, value);
			UpdateButtons();
		}

		public void NonExistentBranchMessage(int slot)
		{
			string binding = Tastudio.Config.HotkeyBindings.First(x => x.DisplayName == "Add Branch").Bindings;
			Tastudio.MainForm.AddOnScreenMessage($"Branch {slot} does not exist");
			Tastudio.MainForm.AddOnScreenMessage($"Use {binding} to add branches");
		}

		public void UpdateValues()
		{
			BranchView.RowCount = Branches.Count;
		}

		public void Restart()
		{
			BranchView.RowCount = Branches.Count;

			if (BranchView.RowCount == 0)
			{
				SetupColumns();
			}

			BranchView.Refresh();
		}

		public void UpdateTextColumnWidth()
		{
			if (Branches.Any())
			{
				var longestBranchText = Branches
					.OrderBy(b => b.UserText?.Length ?? 0)
					.Last()
					.UserText;

				BranchView.ExpandColumnToFitText(UserTextColumnName, longestBranchText);
			}
		}

		public bool EditBranchTextPopUp(int index)
		{
			var branch = Branches[index];
			if (branch == null)
			{
				return false;
			}

			var i = new InputPrompt(MainForm)
			{
				Text = $"Text for branch {index}",
				TextInputType = InputPrompt.InputType.Text,
				Message = "Enter a message",
				InitialValue = branch.UserText
			};

			var point = Cursor.Position;
			point.Offset(i.Width / -2, i.Height / -2);

			var result = i.ShowHawkDialog(this, position: point);
			if (result.IsOk())
			{
				branch.UserText = i.PromptText;
				UpdateTextColumnWidth();
				UpdateValues();
				return true;
			}

			return false;
		}

		private void BranchView_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (BranchView.CurrentCell.IsDataCell()
					&& BranchView.CurrentCell.Column.Name == BranchNumberColumnName)
				{
					BranchView.DragCurrentCell();
				}
			}
		}

		private void BranchView_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				BranchView.ReleaseCurrentCell();
			}
		}

		private void BranchView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (Tastudio.Settings.LoadBranchOnDoubleClick)
			{
				LoadBranchToolStripMenuItem_Click(null, null);
			}
		}

		private void BranchView_MouseMove(object sender, MouseEventArgs e)
		{
			if (BranchView.CurrentCell?.RowIndex == null || BranchView.CurrentCell.Column == null)
			{
				_screenshot.FadeOut();
			}
			else if (BranchView.CurrentCell.Column.Name == BranchNumberColumnName)
			{
				BranchView.Refresh();
			}
		}

		private void BranchView_MouseLeave(object sender, EventArgs e)
		{
			_screenshot.FadeOut();
		}

		private void BranchView_CellDropped(object sender, InputRoll.CellEventArgs e)
		{
			if (e.NewCell.IsDataCell() && e.OldCell.RowIndex < Branches.Count)
			{
				var guid = Branches.Current > Branches.Count
					? Guid.Empty
					: Branches[Branches.Current].Uuid;

				Branches.Swap(e.OldCell.RowIndex.Value, e.NewCell.RowIndex.Value);
				int newIndex = Branches.IndexOfHash(guid);
				Branches.Current = newIndex;
				Select(newIndex, true);
			}
		}

		private void BranchView_PointedCellChanged(object sender, InputRoll.CellEventArgs e)
		{
			if (e.NewCell?.RowIndex != null && e.NewCell.Column != null && e.NewCell.RowIndex < Branches.Count)
			{
				if (BranchView.CurrentCell.Column.Name == BranchNumberColumnName &&
					BranchView.CurrentCell.RowIndex.HasValue &&
					BranchView.CurrentCell.RowIndex < Branches.Count)
				{
					var branch = Branches[BranchView.CurrentCell.RowIndex.Value];
					Point location = PointToScreen(Location);
					int width = branch.OSDFrameBuffer.Width;
					int height = branch.OSDFrameBuffer.Height;
					location.Offset(-width, 0);

					if (location.X < 0)
					{
						location.Offset(width + Width, 0);
					}

					_screenshot.UpdateValues(branch, location, width, height,
						(int)Graphics.FromHwnd(Handle).MeasureString(
							branch.UserText, _screenshot.Font, width).Height);

					_screenshot.FadeIn();
				}
				else
				{
					_screenshot.FadeOut();
				}
			}
			else
			{
				_screenshot.FadeOut();
			}
		}

		private void BranchView_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateButtons();
		}
	}
}
