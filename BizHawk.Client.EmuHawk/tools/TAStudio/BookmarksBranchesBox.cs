using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class BookmarksBranchesBox : UserControl
	{
		private readonly PlatformFrameRates FrameRates = new PlatformFrameRates();
		public TAStudio Tastudio { get; set; }

		public TasBranchCollection Branches
		{
			get { return Tastudio.CurrentTasMovie.TasBranches; }
		}

		public BookmarksBranchesBox()
		{
			InitializeComponent();
			BranchView.QueryItemText += QueryItemText;
			BranchView.QueryItemBkColor += QueryItemBkColor;
		}

		public TasBranch SelectedBranch
		{
			get
			{
				if (BranchView.SelectedIndices.Count > 0)
				{
					return Branches[BranchView.SelectedIndices[0]];
				}

				return null;
			}
		}

		private void QueryItemText(int index, int column, out string text)
		{
			text = string.Empty;

			var columnName = BranchView.Columns[column].Name;

			if (index >= Tastudio.CurrentTasMovie.TasBranches.Count)
			{
				return;
			}

			switch (column)
			{
				case 0: // BranchNumberColumn
					text = index.ToString();
					break;
				case 1: // FrameColumn
					text = Branches[index].Frame.ToString();
					break;
				case 2: // TimeColumn
					text = MovieTime(Branches[index].Frame).ToString(@"hh\:mm\:ss\.fff");
					break;
			}
		}

		private void QueryItemBkColor(int index, int column, ref Color color)
		{
			
		}

		private void AddContextMenu_Click(object sender, EventArgs e)
		{
			// TODO: don't use Global.Emulator
			var branch = new TasBranch
			{
				Frame = Global.Emulator.Frame,
				CoreData = (byte[])((Global.Emulator as IStatable).SaveStateBinary().Clone()),
				InputLog = Tastudio.CurrentTasMovie.InputLog.ToList(),
				//OSDFrameBuffer = GlobalWin.MainForm.CurrentFrameBuffer(captureOSD: true),
				OSDFrameBuffer = (int[])(Global.Emulator.VideoProvider().GetVideoBuffer().Clone()),
				LagLog = Tastudio.CurrentTasMovie.TasLagLog.Clone()
			};

			Branches.Add(branch);
			BranchView.ItemCount = Branches.Count;
		}

		private void BranchView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (SelectedBranch != null)
			{
				LoadBranch(SelectedBranch);
			}
		}

		private void LoadBranchContextMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedBranch != null)
			{
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
				Branches.Remove(SelectedBranch);
				BranchView.ItemCount = Branches.Count;
			}
		}

		private void Temp(int[] framebuffer)
		{
			var buff = Global.Emulator.VideoProvider().GetVideoBuffer();
			for (int i = 0; i < buff.Length; i++)
			{
				buff[i] = framebuffer[i];
			}
		}

		private void LoadBranch(TasBranch branch)
		{
			Tastudio.CurrentTasMovie.LoadBranch(branch);
			GlobalWin.DisplayManager.NeedsToPaint = true;
			var stateInfo = new KeyValuePair<int, byte[]>(branch.Frame, branch.CoreData);
			Tastudio.LoadState(stateInfo);
			//SavestateManager.PopulateFramebuffer(branch.OSDFrameBuffer);
			Temp(branch.OSDFrameBuffer);
			GlobalWin.MainForm.PauseEmulator();
			GlobalWin.MainForm.PauseOnFrame = null;
			Tastudio.RefreshDialog();
		}

		// TODO: copy pasted from PLatformFrameRates

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
			var movie = Tastudio.CurrentTasMovie;
			var system = movie.HeaderEntries[HeaderKeys.PLATFORM];
			var pal = movie.HeaderEntries.ContainsKey(HeaderKeys.PAL) &&
				movie.HeaderEntries[HeaderKeys.PAL] == "1";

			return FrameRates[system, pal];
		}
		// ***************************
	}
}
