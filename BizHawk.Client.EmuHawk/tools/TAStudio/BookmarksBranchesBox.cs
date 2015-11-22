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

namespace BizHawk.Client.EmuHawk
{
	public partial class BookmarksBranchesBox : UserControl
	{
		private const string BranchNumberColumnName = "BranchNumberColumn";
		private const string FrameColumnName = "FrameColumn";
		private const string TimeColumnName = "TimeColumn";

		private readonly PlatformFrameRates FrameRates = new PlatformFrameRates();
		public TAStudio Tastudio { get; set; }
		private TasMovie Movie { get { return Tastudio.CurrentTasMovie; } }

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
					Name = TimeColumnName,
					Text = "TimeStamp",
					Width = 90
				},
			});

			BranchView.QueryItemText += QueryItemText;
			BranchView.QueryItemBkColor += QueryItemBkColor;
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
				case TimeColumnName:
					text = GetBranch(index).TimeStamp.ToString(@"hh\:mm\:ss\.ff");
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

		private void AddContextMenu_Click(object sender, EventArgs e)
		{
			Branch();
		}

		private void BranchView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			LoadSelectedBranch();
		}

		private void LoadBranchContextMenuItem_Click(object sender, EventArgs e)
		{
			LoadSelectedBranch();
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
			}
		}

		private void BranchesContextMenu_Opening(object sender, CancelEventArgs e)
		{
			UpdateBranchContextMenuItem.Enabled =
			RemoveBranchContextMenuItem.Enabled =
				LoadBranchContextMenuItem.Enabled =
				SelectedBranch != null;
		}

		private void RemoveBranchContextMenuItem_Click(object sender, EventArgs e)
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

				if (index == BranchView.SelectedRows.FirstOrDefault())
				{
					BranchView.ClearSelectedRows();
				}

				BranchView.Refresh();
				Tastudio.RefreshDialog();
			}
		}

		private void LoadBranch(TasBranch branch)
		{
			Tastudio.CurrentTasMovie.LoadBranch(branch);
			GlobalWin.DisplayManager.NeedsToPaint = true;
			var stateInfo = new KeyValuePair<int, byte[]>(branch.Frame, branch.CoreData);
			Tastudio.LoadState(stateInfo);
			QuickBmpFile.Copy(new BitmapBufferVideoProvider(branch.OSDFrameBuffer), Global.Emulator.VideoProvider());
			GlobalWin.MainForm.PauseEmulator();
			GlobalWin.MainForm.PauseOnFrame = null;
			Tastudio.RefreshDialog();
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

		public void Branch()
		{
			TasBranch branch = CreateBranch();
			Movie.AddBranch(branch);
			BranchView.RowCount = Movie.BranchCount;
			Movie.CurrentBranch = Movie.BranchCount - 1;
			BranchView.Refresh();
			Tastudio.RefreshDialog();
		}

		private TasBranch CreateBranch()
		{
			// TODO: don't use Global.Emulator
			return new TasBranch
			{
				Frame = Global.Emulator.Frame,
				CoreData = (byte[])((Global.Emulator as IStatable).SaveStateBinary().Clone()),
				InputLog = Movie.InputLog.Clone(),
				OSDFrameBuffer = GlobalWin.MainForm.CaptureOSD(),
				LagLog = Movie.TasLagLog.Clone(),
				ChangeLog = new TasMovieChangeLog(Movie),
				TimeStamp = DateTime.Now,
				Markers = Movie.Markers.DeepClone()
			};
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

		private void CloseScreenShotPopUp()
		{
			Tastudio.ScreenshotControl.Visible = false;
		}

		private void BranchView_MouseLeave(object sender, EventArgs e)
		{
			// Tastudio.ScreenshotControl.Visible = false;
		}

		private void ScreenShotPopUp(TasBranch branch, int index)
		{
			Point locationOnForm = this.FindForm().PointToClient(
				this.Parent.PointToScreen(this.Location));

			int x = locationOnForm.X - Tastudio.ScreenshotControl.Width;
			int y = locationOnForm.Y; // keep consistent height, helps when conparing screenshots

			if (x < 0) x = 0;

			Tastudio.ScreenshotControl.Location = new Point(x, y);

			Tastudio.ScreenshotControl.Visible = true;
			Tastudio.ScreenshotControl.Branch = branch;
			Tastudio.ScreenshotControl.Refresh();
		}

		private void UpdateBranchContextMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedBranch != null)
			{
				UpdateBranch(SelectedBranch);
				Movie.CurrentBranch = BranchView.SelectedRows.First();
			}
		}

		private void UpdateBranch(TasBranch branch)
		{
			Movie.UpdateBranch(branch, CreateBranch());
			BranchView.Refresh();
			Tastudio.RefreshDialog();
		}

		private void BranchView_MouseDown(object sender, MouseEventArgs e)
		{
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

		private void BranchView_CellDropped(object sender, InputRoll.CellEventArgs e)
		{
			if (e.NewCell != null && e.NewCell.IsDataCell && e.OldCell.RowIndex.Value < Movie.BranchCount)
			{
				Movie.SwapBranches(e.OldCell.RowIndex.Value, e.NewCell.RowIndex.Value);
			}
		}
	}
}
