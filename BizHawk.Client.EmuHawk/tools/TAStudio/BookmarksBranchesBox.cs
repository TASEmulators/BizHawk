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

		public TasBranchCollection Branches
		{
			get { return Tastudio.CurrentTasMovie.TasBranches; }
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
					Text = "Length",
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
				if (BranchView.SelectedRows.Any())
				{
					return Branches[BranchView.SelectedRows.First()];
				}

				return null;
			}
		}

		private int CurrentBranch = -1;

		private void QueryItemText(int index, InputRoll.RollColumn column, out string text)
		{
			text = string.Empty;

			if (index >= Tastudio.CurrentTasMovie.TasBranches.Count)
			{
				return;
			}

			switch (column.Name)
			{
				case BranchNumberColumnName:
					text = index.ToString();
					break;
				case FrameColumnName:
					text = Branches[index].Frame.ToString();
					break;
				case TimeColumnName:
					text = MovieTime(Branches[index].Frame).ToString(@"hh\:mm\:ss\.fff");
					break;
			}
		}

		private void QueryItemBkColor(int index, InputRoll.RollColumn column, ref Color color)
		{
			if (index == CurrentBranch)
				color = SystemColors.HotTrack;

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
				CurrentBranch = BranchView.SelectedRows.First();
				BranchView.Refresh();
				LoadBranch(SelectedBranch);
			}
		}

		private void BranchesContextMenu_Opening(object sender, CancelEventArgs e)
		{
			RemoveBranchContextMenuItem.Enabled =
				LoadBranchContextMenuItem.Enabled =
				SelectedBranch != null;
		}

		private void RemoveBranchContextMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedBranch != null)
			{
				if (Branches.IndexOf(SelectedBranch) == CurrentBranch)
				{
					CurrentBranch = -1;
				}

				Branches.Remove(SelectedBranch);
				BranchView.RowCount = Branches.Count;
				BranchView.Refresh();
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

		// TODO: copy pasted from PlatformFrameRates

		private TimeSpan MovieTime(int frameCount)
		{
			var dblseconds = GetSeconds(frameCount);
			var seconds = (int)(dblseconds % 60);
			var days = seconds / 86400;
			var hours = seconds / 3600;
			var minutes = (seconds / 60) % 60;
			var milliseconds = (int)((dblseconds - seconds) * 1000);
			return new TimeSpan(days, hours, minutes, seconds, milliseconds);
		}

		private double GetSeconds(int frameCount)
		{
			double frames = frameCount;

			if (frames < 1)
			{
				return 0;
			}

			return frames / Fps();
		}

		private double Fps()
		{
			TasMovie movie = Tastudio.CurrentTasMovie;
			string system = movie.HeaderEntries[HeaderKeys.PLATFORM];
			bool pal = movie.HeaderEntries.ContainsKey(HeaderKeys.PAL) &&
				movie.HeaderEntries[HeaderKeys.PAL] == "1";

			return FrameRates[system, pal];
		}
		// ***************************

		public void UpdateValues()
		{
			BranchView.RowCount = Branches.Count;
		}

		public void Branch()
		{
			// TODO: don't use Global.Emulator
			TasBranch branch = new TasBranch
			{
				Frame = Global.Emulator.Frame,
				CoreData = (byte[])((Global.Emulator as IStatable).SaveStateBinary().Clone()),
				InputLog = Tastudio.CurrentTasMovie.InputLog.ToList(),
				OSDFrameBuffer = GlobalWin.MainForm.CaptureOSD(),
				//OSDFrameBuffer = (int[])(Global.Emulator.VideoProvider().GetVideoBuffer().Clone()),
				LagLog = Tastudio.CurrentTasMovie.TasLagLog.Clone(),
				ChangeLog = new TasMovieChangeLog(Tastudio.CurrentTasMovie)
			};

			Branches.Add(branch);
			BranchView.RowCount = Branches.Count;
			BranchView.Refresh();
		}

		private void BranchView_CellHovered(object sender, InputRoll.CellEventArgs e)
		{
			if (e.NewCell != null && e.NewCell.RowIndex.HasValue && e.NewCell.Column != null && e.NewCell.RowIndex < Branches.Count)
			{
				if (e.NewCell.Column.Name == BranchNumberColumnName)
				{
					ScreenShotPopUp(Branches[e.NewCell.RowIndex.Value], e.NewCell.RowIndex.Value);
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
			Tastudio.ScreenshotControl.Visible = false;
		}

		private void ScreenShotPopUp(TasBranch branch, int index)
		{
			int x = this.Location.X - Tastudio.ScreenshotControl.Width;
			int y = this.Location.Y + (BranchView.RowHeight * index);

			Tastudio.ScreenshotControl.Location = new Point(x, y);

			Tastudio.ScreenshotControl.Visible = true;
			Tastudio.ScreenshotControl.Branch = branch;
			Tastudio.ScreenshotControl.Refresh();
		}
	}
}
