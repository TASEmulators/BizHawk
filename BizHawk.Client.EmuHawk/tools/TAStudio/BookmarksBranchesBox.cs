using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class BookmarksBranchesBox : UserControl
	{
		private const string BranchNumberColumnName = "BranchNumberColumn";
		private const string FrameColumnName = "FrameColumn";
		private const string UserTextColumnName = "TextColumn";

		private readonly PlatformFrameRates FrameRates = new PlatformFrameRates();
		private TasMovie Movie { get { return Tastudio.CurrentTasMovie; } }
		public TAStudio Tastudio { get; set; }

		public int HoverInterval {
			get { return BranchView.HoverInterval; }
			set { BranchView.HoverInterval = value; }
		}

		private TasBranch GetBranch(int id)
		{
			return Tastudio.CurrentTasMovie.GetBranch(id);
		}

		public BookmarksBranchesBox()
		{
			InitializeComponent();

			BranchView.AllColumns.AddRange(new InputRoll.RollColumn[]
			{
				new InputRoll.RollColumn
				{
					Name = BranchNumberColumnName,
					Text = "#",
					Width = 30
				},
				new InputRoll.RollColumn
				{
					Name = FrameColumnName,
					Text = "Frame",
					Width = 64
				},
				new InputRoll.RollColumn
				{
					Name = UserTextColumnName,
					Text = "UserText",
					Width = 90
				},
			});

			BranchView.QueryItemText += QueryItemText;
			BranchView.QueryItemBkColor += QueryItemBkColor;
		}

		private void QueryItemText(int index, InputRoll.RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			text = string.Empty;

			if (index >= Movie.BranchCount)
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
					//text = GetBranch(index).TimeStamp.ToString(@"hh\:mm\:ss\.ff");
					text = GetBranch(index).UserText;
					break;
			}
		}

		private void QueryItemBkColor(int index, InputRoll.RollColumn column, ref Color color)
		{
			TasBranch branch = GetBranch(index);
			if (branch != null)
			{
				var record = Tastudio.CurrentTasMovie[branch.Frame];
				if (index == Movie.CurrentBranch)
					color = TAStudio.CurrentFrame_InputLog; // SystemColors.HotTrack;
				else if (record.Lagged.HasValue)
				{
					if (record.Lagged.Value)
					{
						color = TAStudio.LagZone_InputLog;
					}
					else
					{
						color = TAStudio.GreenZone_InputLog;
					}
				}
			}

			// Highlight the branch cell a little, if hovering over it
			if (BranchView.CurrentCellIsDataCell &&
				BranchView.CurrentCell.Column.Name == BranchNumberColumnName &&
				column.Name == BranchNumberColumnName && 
				index == BranchView.CurrentCell.RowIndex)
			{
				color = Color.FromArgb((byte)(color.A - 24), (byte)(color.R - 24), (byte)(color.G - 24), (byte)(color.B - 24));
			}
		}

		public void Branch()
		{
			TasBranch branch = CreateBranch();
			Movie.NewBranchText = ""; // reset every time it's used
			Movie.AddBranch(branch);
			BranchView.RowCount = Movie.BranchCount;
			Movie.CurrentBranch = Movie.BranchCount - 1;
			BranchView.Refresh();
			Tastudio.RefreshDialog();
		}

		public TasBranch SelectedBranch
		{
			get
			{
				if (BranchView.AnyRowsSelected)
				{
					return GetBranch(BranchView.SelectedRows.First());
				}

				return null;
			}
		}

		private TasBranch CreateBranch()
		{
			return new TasBranch
			{
				Frame = Tastudio.Emulator.Frame,
				CoreData = (byte[])(Tastudio.StatableEmulator.SaveStateBinary().Clone()),
				InputLog = Movie.InputLog.Clone(),
				OSDFrameBuffer = GlobalWin.MainForm.CaptureOSD(),
				LagLog = Movie.TasLagLog.Clone(),
				ChangeLog = new TasMovieChangeLog(Movie),
				TimeStamp = DateTime.Now,
				Markers = Movie.Markers.DeepClone(),
				UserText = Movie.NewBranchText
			};
		}

		private void LoadBranch(TasBranch branch)
		{
			Tastudio.CurrentTasMovie.LoadBranch(branch);
			var stateInfo = new KeyValuePair<int, byte[]>(branch.Frame, branch.CoreData);
			Tastudio.LoadState(stateInfo);
			QuickBmpFile.Copy(new BitmapBufferVideoProvider(branch.OSDFrameBuffer), Tastudio.VideoProvider);
			//GlobalWin.MainForm.PauseEmulator();
			GlobalWin.MainForm.PauseOnFrame = null;
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
				//if (Movie.CurrentBranch == index) // if the current branch was edited, we should allow loading it. some day there might be a proper check
				//	return;
				Movie.CurrentBranch = index;
				LoadBranch(SelectedBranch);
				BranchView.Refresh();
				GlobalWin.OSD.AddMessage("Loaded branch " + Movie.CurrentBranch.ToString());
			}
		}

		private void BranchesContextMenu_Opening(object sender, CancelEventArgs e)
		{
			UpdateBranchContextMenuItem.Enabled =
			RemoveBranchContextMenuItem.Enabled =
			LoadBranchContextMenuItem.Enabled =
			EditBranchTextContextMenuItem.Enabled =
				SelectedBranch != null;
		}

		private void AddBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Branch();
			GlobalWin.OSD.AddMessage("Added branch " + Movie.CurrentBranch.ToString());
		}

		private void AddBranchWithTexToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Branch();
			EditBranchTextPopUp(Movie.CurrentBranch);
			GlobalWin.OSD.AddMessage("Added branch " + Movie.CurrentBranch.ToString());
		}

		private void LoadBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadSelectedBranch();
		}

		private void UpdateBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedBranch != null)
			{
				Movie.CurrentBranch = BranchView.SelectedRows.First();
				UpdateBranch(SelectedBranch);
				GlobalWin.OSD.AddMessage("Saved branch " + Movie.CurrentBranch.ToString());
			}
		}

		private void EditBranchTextToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedBranch != null)
			{
				int index = BranchView.SelectedRows.First();
				EditBranchTextPopUp(index);
				GlobalWin.OSD.AddMessage("Edited branch " + index.ToString());
			}
		}

		private void RemoveBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedBranch != null)
			{
				int index = BranchView.SelectedRows.First();
				if (index == Movie.CurrentBranch)
				{
					Movie.CurrentBranch = -1;
				}
				else if (index < Movie.CurrentBranch)
				{
					Movie.CurrentBranch--;
				}

				Movie.RemoveBranch(SelectedBranch);
				BranchView.RowCount = Movie.BranchCount;

				if (index == Movie.BranchCount)
				{
					BranchView.ClearSelectedRows();
					BranchView.SelectRow(Movie.BranchCount - 1, true);
				}

				//BranchView.Refresh();
				Tastudio.RefreshDialog();
				GlobalWin.OSD.AddMessage("Removed branch " + index.ToString());
			}
		}

		public void AddBranchExternal()
		{
			AddBranchToolStripMenuItem_Click(null, null);
			BranchView.SelectRow(Movie.CurrentBranch, true);
			BranchView.Refresh();
		}

		public void LoadBranchExternal(int slot = -1)
		{
			if (Tastudio.FloatEditingMode)
				return;

			if (slot != -1)
			{
				if (GetBranch(slot) != null)
				{
					BranchView.SelectRow(slot, true);
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
				return;

			if (slot != -1)
			{
				if (GetBranch(slot) != null)
				{
					BranchView.SelectRow(slot, true);
				}
				else
				{
					NonExistentBranchMessage(slot);
					return;
				}
			}
			UpdateBranchToolStripMenuItem_Click(null, null);
		}

		public void RemoveBranchExtrenal()
		{
			RemoveBranchToolStripMenuItem_Click(null, null);
		}

		public void SelectBranchExternal(int slot)
		{
			if (Tastudio.FloatEditingMode)
				return;

			if (GetBranch(slot) != null)
			{
				BranchView.SelectRow(slot, true);
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
				BranchView.SelectRow(Movie.CurrentBranch, true);
				BranchView.Refresh();
				return;
			}
			int sel = BranchView.SelectedRows.First();
			if (next)
			{
				if (GetBranch(sel + 1) != null)
				{
					BranchView.SelectRow(sel, false);
					BranchView.SelectRow(sel + 1, true);
				}
			}
			else // previous
			{
				if (GetBranch(sel - 1) != null)
				{
					BranchView.SelectRow(sel, false);
					BranchView.SelectRow(sel - 1, true);
				}
			}
			BranchView.Refresh();
		}

		public void NonExistentBranchMessage(int slot)
		{
			string binding = Global.Config.HotkeyBindings.Where(x => x.DisplayName == "Add Branch").FirstOrDefault().Bindings;
			GlobalWin.OSD.AddMessage("Branch " + slot.ToString() + " does not exist");
			GlobalWin.OSD.AddMessage("Use " + binding + " to add branches");
		}

		public void UpdateValues()
		{
			BranchView.RowCount = Movie.BranchCount;
			BranchView.Refresh();
		}

		public void Restart()
		{
			BranchView.DeselectAll();
			BranchView.RowCount = Movie.BranchCount;
			BranchView.Refresh();
		}

		public void EditBranchTextPopUp(int index)
		{
			TasBranch branch = Movie.GetBranch(index);
			if (branch == null)
				return;

			InputPrompt i = new InputPrompt
			{
				Text = "Text for branch " + index,
				TextInputType = InputPrompt.InputType.Text,
				Message = "Enter a message",
				InitialValue = branch.UserText
			};

			var result = i.ShowHawkDialog();

			if (result == DialogResult.OK)
			{
				branch.UserText = i.PromptText;
				UpdateValues();
			}
		}

		private void ScreenShotPopUp(TasBranch branch, int index)
		{
			Point locationOnForm = this.FindForm().PointToClient(
				this.Parent.PointToScreen(this.Location));

			int x = locationOnForm.X - Tastudio.ScreenshotControl.Width;
			int y = locationOnForm.Y; // keep consistent height, helps when conparing screenshots

			if (x < 1) x = 1;

			Tastudio.ScreenshotControl.Location = new Point(x, y);
			Tastudio.ScreenshotControl.Visible = true;
			Tastudio.ScreenshotControl.Branch = branch;
			Tastudio.ScreenshotControl.RecalculateHeight();
			Tastudio.ScreenshotControl.Refresh();
		}

		private void CloseScreenShotPopUp()
		{
			Tastudio.ScreenshotControl.Visible = false;
		}

		private void BranchView_MouseDown(object sender, MouseEventArgs e)
		{
			UpdateBranchButton.Enabled =
			RemoveBranchButton.Enabled =
			LoadBranchButton.Enabled =
			EditBranchTextButton.Enabled =
				SelectedBranch != null;

			BranchesContextMenu.Close();

			if (e.Button == MouseButtons.Left)
			{
				if (BranchView.CurrentCell != null && BranchView.CurrentCell.IsDataCell
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
			LoadSelectedBranch();
		}

		private void BranchView_MouseMove(object sender, MouseEventArgs e)
		{
			if (BranchView.CurrentCell == null || !BranchView.CurrentCell.RowIndex.HasValue || BranchView.CurrentCell.Column == null)
			{
				CloseScreenShotPopUp();
			}
			else if (BranchView.CurrentCell.Column.Name == BranchNumberColumnName)
			{
				BranchView.Refresh();
			}
		}

		private void BranchView_MouseLeave(object sender, EventArgs e)
		{
			// Tastudio.ScreenshotControl.Visible = false;
		}

		private void BranchView_CellHovered(object sender, InputRoll.CellEventArgs e)
		{
			if (e.NewCell != null && e.NewCell.RowIndex.HasValue && e.NewCell.Column != null && e.NewCell.RowIndex < Movie.BranchCount)
			{
				if (e.NewCell.Column.Name == BranchNumberColumnName)
				{
					ScreenShotPopUp(GetBranch(e.NewCell.RowIndex.Value), e.NewCell.RowIndex.Value);
				}
				else
				{
					CloseScreenShotPopUp();
				}
			}
			else
			{
				CloseScreenShotPopUp();
			}
		}

		private void BranchView_CellDropped(object sender, InputRoll.CellEventArgs e)
		{
			if (e.NewCell != null && e.NewCell.IsDataCell && e.OldCell.RowIndex.Value < Movie.BranchCount)
			{
				Movie.SwapBranches(e.OldCell.RowIndex.Value, e.NewCell.RowIndex.Value);
			}
		}
	}
}
