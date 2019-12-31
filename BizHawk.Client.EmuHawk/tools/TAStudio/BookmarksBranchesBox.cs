using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class BookmarksBranchesBox : UserControl
	{
		private const string BranchNumberColumnName = "BranchNumberColumn";
		private const string FrameColumnName = "FrameColumn";
		private const string UserTextColumnName = "TextColumn";

		private readonly ScreenshotForm _screenshot = new ScreenshotForm();

		private TasMovie Movie => Tastudio.CurrentTasMovie;
		private MainForm MainForm => Tastudio.MainForm;
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
					Width = 30
				},
				new RollColumn
				{
					Name = FrameColumnName,
					Text = "Frame",
					Width = 64
				},
				new RollColumn
				{
					Name = UserTextColumnName,
					Text = "UserText",
					Width = 90
				},
			});
		}

		#region Query callbacks

		private void QueryItemText(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			text = "";

			if (index >= Movie.Branches.Count)
			{
				return;
			}

			switch (column.Name)
			{
				case BranchNumberColumnName:
					text = index.ToString();
					break;
				case FrameColumnName:
					text = GetBranch(index).Frame.ToString();
					break;
				case UserTextColumnName:
					text = GetBranch(index).UserText;
					break;
			}
		}

		private void QueryItemBkColor(int index, RollColumn column, ref Color color)
		{
			TasBranch branch = GetBranch(index);
			if (branch != null)
			{
				var record = Movie[branch.Frame];
				if (index == Movie.CurrentBranch)
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

		#endregion

		#region Actions

		private TasBranch GetBranch(int index)
		{
			return Movie.GetBranch(index);
		}

		public void Branch()
		{
			TasBranch branch = CreateBranch();
			Movie.NewBranchText = ""; // reset every time it's used
			Movie.AddBranch(branch);
			BranchView.RowCount = Movie.Branches.Count;
			Movie.CurrentBranch = Movie.Branches.Count - 1;
			BranchView.ScrollToIndex(Movie.CurrentBranch);
			Select(Movie.CurrentBranch, true);
			BranchView.Refresh();
			Tastudio.RefreshDialog();
			MainForm.UpdateStatusSlots();
		}

		public TasBranch SelectedBranch => BranchView.AnyRowsSelected
			? GetBranch(BranchView.SelectedRows.First())
			: null;

		private TasBranch CreateBranch()
		{
			return new TasBranch
			{
				Frame = Tastudio.Emulator.Frame,
				CoreData = (byte[])(Tastudio.StatableEmulator.SaveStateBinary().Clone()),
				InputLog = Movie.CloneInput(),
				CoreFrameBuffer = MainForm.MakeScreenshotImage(),
				OSDFrameBuffer = MainForm.CaptureOSD(),
				ChangeLog = new TasMovieChangeLog(Movie),
				TimeStamp = DateTime.Now,
				Markers = Movie.Markers.DeepClone(),
				UserText = Movie.NewBranchText
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
			var stateInfo = new KeyValuePair<int, byte[]>(branch.Frame, branch.CoreData);
			Tastudio.LoadState(stateInfo);
			Movie.TasStateManager.Capture(true);
			QuickBmpFile.Copy(new BitmapBufferVideoProvider(branch.CoreFrameBuffer), Tastudio.VideoProvider);

			if (Tastudio.Settings.OldControlSchemeForBranches && Tastudio.TasPlaybackBox.RecordingMode)
				Movie.Truncate(branch.Frame);

			MainForm.PauseOnFrame = null;
			Tastudio.RefreshDialog();
		}

		private void UpdateBranch(TasBranch branch)
		{
			Movie.UpdateBranch(branch, CreateBranch());
			Tastudio.RefreshDialog();
		}

		private void LoadSelectedBranch()
		{
			if (SelectedBranch != null)
			{
				int index = BranchView.SelectedRows.First();
				Movie.CurrentBranch = index;
				LoadBranch(SelectedBranch);
				BranchView.Refresh();
				GlobalWin.OSD.AddMessage($"Loaded branch {Movie.CurrentBranch}");
			}
		}

		private void BranchesContextMenu_Opening(object sender, CancelEventArgs e)
		{
			UpdateBranchContextMenuItem.Enabled =
			RemoveBranchContextMenuItem.Enabled =
			LoadBranchContextMenuItem.Enabled =
			EditBranchTextContextMenuItem.Enabled =
			JumpToBranchContextMenuItem.Enabled = 
				SelectedBranch != null;
		}

		private void AddBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Branch();
			SavedCallback?.Invoke(Movie.Branches.Count - 1);
			GlobalWin.OSD.AddMessage($"Added branch {Movie.CurrentBranch}");
		}

		private void AddBranchWithTexToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Branch();
			EditBranchTextPopUp(Movie.CurrentBranch);
			SavedCallback?.Invoke(Movie.Branches.Count - 1);
			GlobalWin.OSD.AddMessage($"Added branch {Movie.CurrentBranch}");
		}

		private void LoadBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			_backupBranch = CreateBranch();

			var currentHashes = Movie.Branches.Select(b => b.UniqueIdentifier.GetHashCode()).ToList();
			do
			{
				_backupBranch.UniqueIdentifier = Guid.NewGuid();
			}
			while (currentHashes.Contains(_backupBranch.UniqueIdentifier.GetHashCode()));

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

			Movie.CurrentBranch = BranchView.SelectedRows.First();

			_backupBranch = SelectedBranch.Clone();
			UndoBranchToolStripMenuItem.Enabled = UndoBranchButton.Enabled = true;
			UndoBranchToolStripMenuItem.Text = "Undo Branch Update";
			toolTip1.SetToolTip(UndoBranchButton, "Undo Branch Update");
			_branchUndo = BranchUndo.Update;

			UpdateBranch(SelectedBranch);
			SavedCallback?.Invoke(Movie.CurrentBranch);
			GlobalWin.OSD.AddMessage($"Saved branch {Movie.CurrentBranch}");
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

				GlobalWin.OSD.AddMessage($"Edited branch {index}");
			}
		}

		private void JumpToBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedBranch == null)
			{
				return;
			}

			int index = BranchView.SelectedRows.First();
			TasBranch branch = Movie.GetBranch(index);
			Tastudio.GoToFrame(branch.Frame);
		}

		private void RemoveBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedBranch == null)
			{
				return;
			}

			int index = BranchView.SelectedRows.First();
			if (index == Movie.CurrentBranch)
			{
				Movie.CurrentBranch = -1;
			}
			else if (index < Movie.CurrentBranch)
			{
				Movie.CurrentBranch--;
			}

			_backupBranch = SelectedBranch.Clone();
			UndoBranchToolStripMenuItem.Enabled = UndoBranchButton.Enabled = true;
			UndoBranchToolStripMenuItem.Text = "Undo Branch Removal";
			toolTip1.SetToolTip(UndoBranchButton, "Undo Branch Removal");
			_branchUndo = BranchUndo.Remove;

			Movie.RemoveBranch(SelectedBranch);
			BranchView.RowCount = Movie.Branches.Count;

			if (index == Movie.Branches.Count)
			{
				BranchView.DeselectAll();
				Select(Movie.Branches.Count - 1, true);
			}

			RemovedCallback?.Invoke(index);
			Tastudio.RefreshDialog();
			GlobalWin.OSD.AddMessage($"Removed branch {index}");
			MainForm.UpdateStatusSlots();
		}

		private void UndoBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (_branchUndo == BranchUndo.Load)
			{
				LoadBranch(_backupBranch);
				LoadedCallback?.Invoke(Movie.Branches.IndexOf(_backupBranch));
				GlobalWin.OSD.AddMessage("Branch Load canceled");
			}
			else if (_branchUndo == BranchUndo.Update)
			{
				Movie.UpdateBranch(Movie.GetBranch(_backupBranch.UniqueIdentifier), _backupBranch);
				SavedCallback?.Invoke(Movie.Branches.IndexOf(_backupBranch));
				GlobalWin.OSD.AddMessage("Branch Update canceled");
			}
			else if (_branchUndo == BranchUndo.Text)
			{
				Movie.GetBranch(_backupBranch.UniqueIdentifier).UserText = _backupBranch.UserText;
				GlobalWin.OSD.AddMessage("Branch Text Edit canceled");
			}
			else if (_branchUndo == BranchUndo.Remove)
			{
				Movie.AddBranch(_backupBranch);
				BranchView.RowCount = Movie.Branches.Count;
				SavedCallback?.Invoke(Movie.Branches.IndexOf(_backupBranch));
				GlobalWin.OSD.AddMessage("Branch Removal canceled");
			}

			UndoBranchToolStripMenuItem.Enabled = UndoBranchButton.Enabled = false;
			BranchView.Refresh();
			Tastudio.RefreshDialog();
		}

		public void AddBranchExternal()
		{
			AddBranchToolStripMenuItem_Click(null, null);
			Select(Movie.CurrentBranch, true);
			BranchView.Refresh();
		}

		public void LoadBranchExternal(int slot = -1)
		{
			if (Tastudio.FloatEditingMode)
			{
				return;
			}

			if (slot != -1)
			{
				if (GetBranch(slot) != null)
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
			if (Tastudio.FloatEditingMode)
			{
				return;
			}

			if (slot != -1)
			{
				if (GetBranch(slot) != null)
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
			if (Tastudio.FloatEditingMode)
			{
				return;
			}

			if (GetBranch(slot) != null)
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
				Select(Movie.CurrentBranch, true);
				BranchView.Refresh();
				return;
			}

			int sel = BranchView.SelectedRows.First();
			if (next)
			{
				if (GetBranch(sel + 1) != null)
				{
					Select(sel, false);
					Select(sel + 1, true);
				}
			}
			else // previous
			{
				if (GetBranch(sel - 1) != null)
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
				SelectedBranch != null;
		}

		private void Select(int index, bool value)
		{
			BranchView.SelectRow(index, value);
			UpdateButtons();
		}

		public void NonExistentBranchMessage(int slot)
		{
			string binding = Global.Config.HotkeyBindings.First(x => x.DisplayName == "Add Branch").Bindings;
			GlobalWin.OSD.AddMessage($"Branch {slot} does not exist");
			GlobalWin.OSD.AddMessage($"Use {binding} to add branches");
		}

		public void UpdateValues()
		{
			BranchView.RowCount = Movie.Branches.Count;
		}

		public void Restart()
		{
			BranchView.RowCount = Movie.Branches.Count;

			if (BranchView.RowCount == 0)
			{
				SetupColumns();
			}

			BranchView.Refresh();
		}

		public void UpdateTextColumnWidth()
		{
			if (Movie.Branches.Any())
			{
				var longestBranchText = Movie.Branches
					.OrderBy(b => b.UserText?.Length ?? 0)
					.Last()
					.UserText;

				BranchView.ExpandColumnToFitText(UserTextColumnName, longestBranchText);
			}
		}

		public bool EditBranchTextPopUp(int index)
		{
			TasBranch branch = Movie.GetBranch(index);
			if (branch == null)
			{
				return false;
			}

			var i = new InputPrompt
			{
				Text = $"Text for branch {index}",
				TextInputType = InputPrompt.InputType.Text,
				Message = "Enter a message",
				InitialValue = branch.UserText
			};

			var point = Cursor.Position;
			point.Offset(i.Width / -2, i.Height / -2);

			var result = i.ShowHawkDialog(position: point);
			if (result == DialogResult.OK)
			{
				branch.UserText = i.PromptText;
				UpdateTextColumnWidth();
				UpdateValues();
				return true;
			}

			return false;
		}

		#endregion

		#region Events

		private void BranchView_MouseDown(object sender, MouseEventArgs e)
		{
			BranchesContextMenu.Close();
			UpdateButtons();

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
			if (e.NewCell.IsDataCell() && e.OldCell.RowIndex < Movie.Branches.Count)
			{
				var guid = Movie.BranchGuidByIndex(Movie.CurrentBranch);
				Movie.SwapBranches(e.OldCell.RowIndex.Value, e.NewCell.RowIndex.Value);
				int newIndex = Movie.BranchIndexByHash(guid);
				Movie.CurrentBranch = newIndex;
				Select(newIndex, true);
			}
		}

		private void BranchView_PointedCellChanged(object sender, InputRoll.CellEventArgs e)
		{
			if (e.NewCell?.RowIndex != null && e.NewCell.Column != null && e.NewCell.RowIndex < Movie.Branches.Count)
			{
				if (BranchView.CurrentCell.Column.Name == BranchNumberColumnName &&
					BranchView.CurrentCell.RowIndex.HasValue &&
					BranchView.CurrentCell.RowIndex < Movie.Branches.Count)
				{
					TasBranch branch = GetBranch(BranchView.CurrentCell.RowIndex.Value);
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

		#endregion
	}
}
