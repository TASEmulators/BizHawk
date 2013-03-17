using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class TAStudio : Form
	{
		//TODO:
		//Slicer Section:
			//View clipboard - opens a pop-up with a listview showing the input
			//Save clipboard as macro - adds to the macro list (todo: macro list)
		//click & drag on list view should highlight rows
		//any event that changes highlighting of listview should update selection display
		//caret column and caret
		//When closing tastudio, don't write the movie file? AskSave() is acceptable however
		//If null emulator do a base virtualpad so getmnemonic doesn't fail
		//Right-click - Go to current frame
		//Clicking a frame should go there (currently set to double click)
		//Multiple timeline system
		//Macro listview
		//	Double click brings up a macro editing window
		//ensureVisible when recording
		//Allow hotkeys when TAStudio has focus
		//Reduce the memory footprint with compression and or dropping frames and rerunning them when requested.

		int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
		int defaultHeight;
		int stopOnFrame = 0;

		public bool Engaged; //When engaged the Client will listen to TAStudio for input

		//Movie header object - to have the main project header data
		//List<string> MacroFiles - list of .macro files (simply log files)
		//List<string> TimeLines - list of movie files
		//List<string> Bookmarks - list of savestate files

		public TAStudio()
		{
			InitializeComponent();
			Closing += (o, e) => SaveConfigSettings();
			TASView.QueryItemText += new QueryItemTextHandler(TASView_QueryItemText);
			TASView.QueryItemBkColor += new QueryItemBkColorHandler(TASView_QueryItemBkColor);
			TASView.VirtualMode = true;
		}


		//TODO: move me
		public class ClipboardEntry
		{
			public int frame;
			public string inputstr;
		}

		public List<ClipboardEntry> Clipboard = new List<ClipboardEntry>();

		public void UpdateValues()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;
			TASView.BlazingFast = true;
			if (Global.MovieSession.Movie.IsActive)
			{
				DisplayList();
			}
			else
			{
				TASView.ItemCount = 0;
			}

			if (Global.MovieSession.Movie.IsPlaying && !Global.MovieSession.Movie.IsFinished)
			{
				TASView.BlazingFast = false;
			}

			if (Global.Emulator.Frame < this.stopOnFrame)
			{
				Global.MainForm.PressFrameAdvance = true;
			}
		}

		//public string GetMnemonic()
		//{
		//    StringBuilder str = new StringBuilder("|"); //TODO: Control Command virtual pad

		//    //TODO: remove this hack with a nes controls pad 
		//    if (Global.Emulator.SystemId == "NES")
		//    {
		//        str.Append("0|");
		//    }

		//    for (int x = 0; x < Pads.Count; x++)
		//        str.Append(Pads[x].GetMnemonic());
		//    return str.ToString();
		//}

		private void TASView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (index == 0 && Global.MovieSession.Movie.StateFirstIndex == 0)
			{
				if (color != Color.LightGreen)
				{
					color = Color.LightGreen; //special case for frame 0. Normally we need to go back an extra frame, but for frame 0 we can reload the rom.
				}
			}
			else if (Global.MovieSession.Movie.FrameLagged(index))
			{
				if (color != Color.Pink)
				{
					color = Color.Pink;
				}
			}
			else if (index > Global.MovieSession.Movie.StateFirstIndex && index <= Global.MovieSession.Movie.StateLastIndex)
			{
				if (color != Color.LightGreen)
				{
					color = Color.LightGreen;
				}
			}
			if (index == Global.Emulator.Frame)
			{
				if (color != Color.LightBlue)
				{
					color = Color.LightBlue;
				}
			}
		}

		private void TASView_QueryItemText(int index, int column, out string text)
		{
			text = "";

			//If this is just for an actual frame and not just the list view cursor at the end
			if (Global.MovieSession.Movie.Frames != index)
			{
				if (column == 0)
					text = String.Format("{0:#,##0}", index);
				if (column == 1)
					text = Global.MovieSession.Movie.GetInput(index);
			}
		}

		private void DisplayList()
		{
			TASView.ItemCount = Global.MovieSession.Movie.RawFrames;
			if (Global.MovieSession.Movie.Frames == Global.Emulator.Frame && Global.MovieSession.Movie.StateLastIndex == Global.Emulator.Frame - 1)
			{
				//If we're at the end of the movie add one to show the cursor as a blank frame
				TASView.ItemCount++;
			}
			TASView.ensureVisible(Global.Emulator.Frame - 1);
		}

		public void Restart()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;
			TASView.Items.Clear();
			LoadTAStudio();
		}

		public void LoadTAStudio()
		{
			//TODO: don't engage until new/open project
			//
			Global.MainForm.PauseEmulator();
			Engaged = true;
			Global.OSD.AddMessage("TAStudio engaged");
			if (Global.MovieSession.Movie.IsActive)
			{
				Global.MovieSession.Movie.StateCapturing = true;
				ReadOnlyCheckBox.Checked = Global.MainForm.ReadOnly;
			}
			else
			{
				ReadOnlyCheckBox.Checked = false;
			}
			
			LoadConfigSettings();
			DisplayList();
		}

		private void TAStudio_Load(object sender, EventArgs e)
		{
			if (!MainForm.INTERIM)
			{
				newProjectToolStripMenuItem.Enabled = false;
				openProjectToolStripMenuItem.Enabled = false;
				saveProjectToolStripMenuItem.Enabled = false;
				saveProjectAsToolStripMenuItem.Enabled = false;
				recentToolStripMenuItem.Enabled = false;
				importTASFileToolStripMenuItem.Enabled = false;
			}

			LoadTAStudio();
		}

		private void LoadConfigSettings()
		{
			defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = Size.Height;

			if (Global.Config.TAStudioSaveWindowPosition && Global.Config.TASWndx >= 0 && Global.Config.TASWndy >= 0)
			{
				this.Location = new Point(Global.Config.TASWndx, Global.Config.TASWndy);
			}

			if (Global.Config.TASWidth >= 0 && Global.Config.TASHeight >= 0)
			{
				this.Size = new System.Drawing.Size(Global.Config.TASWidth, Global.Config.TASHeight);
			}

		}

		private void SaveConfigSettings()
		{
			Engaged = false;
			Global.Config.TASWndx = this.Location.X;
			Global.Config.TASWndy = this.Location.Y;
			Global.Config.TASWidth = this.Right - this.Left;
			Global.Config.TASHeight = this.Bottom - this.Top;
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void settingsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.TAStudioSaveWindowPosition;
			autoloadToolStripMenuItem.Checked = Global.Config.AutoloadTAStudio;
			updatePadsOnMovePlaybackToolStripMenuItem.Checked = Global.Config.TASUpdatePads;
		}

		private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TAStudioSaveWindowPosition ^= true;
		}

		private void restoreWindowToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Size = new System.Drawing.Size(defaultWidth, defaultHeight);
		}

		private void StopButton_Click(object sender, EventArgs e)
		{
			Global.MainForm.StopMovie();
			Restart();
		}

		private void FrameAdvanceButton_Click(object sender, EventArgs e)
		{
			Global.MainForm.PressFrameAdvance = true;
		}

		private void RewindButton_Click(object sender, EventArgs e)
		{
			this.stopOnFrame = 0;
			if (Global.MovieSession.Movie.IsFinished || !Global.MovieSession.Movie.IsActive)
			{
				Global.MainForm.Rewind(1);
				if (Global.Emulator.Frame <= Global.MovieSession.Movie.Frames)
				{
					Global.MovieSession.Movie.SwitchToPlay();
				}
			}
			else
			{
				Global.MovieSession.Movie.RewindToFrame(Global.Emulator.Frame - 1);
			}
			UpdateValues();
		}

		private void PauseButton_Click(object sender, EventArgs e)
		{
			Global.MainForm.TogglePause();
		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoloadTAStudio ^= true;
		}

		private void checkBox1_CheckedChanged(object sender, EventArgs e)
		{
			if (ReadOnlyCheckBox.Checked)
			{
				Global.MainForm.SetReadOnly(true);
				ReadOnlyCheckBox.BackColor = System.Drawing.SystemColors.Control;

				if (Global.MovieSession.Movie.IsActive)
				{
					Global.MovieSession.Movie.SwitchToPlay();
					toolTip1.SetToolTip(this.ReadOnlyCheckBox, "Currently Read-Only Mode");
				}
			}
			else
			{
				Global.MainForm.SetReadOnly(false);
				ReadOnlyCheckBox.BackColor = Color.LightCoral;
				if (Global.MovieSession.Movie.IsActive)
				{
					Global.MovieSession.Movie.SwitchToRecord();
					toolTip1.SetToolTip(this.ReadOnlyCheckBox, "Currently Read+Write Mode");
				}
			}
		} 

		private void RewindToBeginning_Click(object sender, EventArgs e)
		{
			Global.MainForm.Rewind(Global.Emulator.Frame);
			DisplayList();
		}

		private void FastForwardToEnd_Click(object sender, EventArgs e)
		{
			//TODO: adelikat: I removed the stop on frame feature, so this will keep playing into movie finished mode, need to rebuild that functionality

			this.FastFowardToEnd.Checked ^= true;
			Global.MainForm.FastForward = this.FastFowardToEnd.Checked;
			if (true == this.FastFowardToEnd.Checked)
			{
				this.FastForward.Checked = false;
				this.TurboFastForward.Checked = false;
			}
		}

		private void editToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (ReadOnlyCheckBox.Checked)
			{

				clearToolStripMenuItem2.Enabled = false;
				deleteFramesToolStripMenuItem.Enabled = false;
				cloneToolStripMenuItem.Enabled = false;
				insertFrameToolStripMenuItem.Enabled = false;
				insertNumFramesToolStripMenuItem.Enabled = false;
				truncateMovieToolStripMenuItem.Enabled = false;
				
			}
			else
			{
				clearToolStripMenuItem2.Enabled = true;
				deleteFramesToolStripMenuItem.Enabled = true;
				cloneToolStripMenuItem.Enabled = true;
				insertFrameToolStripMenuItem.Enabled = true;
				insertNumFramesToolStripMenuItem.Enabled = true;
				truncateMovieToolStripMenuItem.Enabled = true;
			}
		}

		private void insertFrameToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (ReadOnlyCheckBox.Checked)
			{
				return;
			}
			else
			{
				InsertFrames();
			}
		}

		private void updatePadsOnMovePlaybackToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TASUpdatePads ^= true;
		}

		private void newProjectToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.MainForm.RecordMovie();
		}

		private void openProjectToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.MainForm.PlayMovie();
		}

		private void saveProjectToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.MovieSession.Movie.WriteMovie();
		}

		private void saveProjectAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			string fileName = SaveRecordingAs();

			if ("" != fileName)
			{
				Global.MovieSession.Movie.Filename = fileName;
				Global.MovieSession.Movie.WriteMovie();
			}
		}

		private void FastForward_Click(object sender, EventArgs e)
		{
			this.FastForward.Checked ^= true;
			Global.MainForm.FastForward = this.FastForward.Checked;
			if (true == this.FastForward.Checked)
			{
				this.TurboFastForward.Checked = false;
				this.FastFowardToEnd.Checked = false;
			}
		}

		private void TurboFastForward_Click(object sender, EventArgs e)
		{
			Global.MainForm.TurboFastForward ^= true;
			this.TurboFastForward.Checked ^= true;
			if (true == this.TurboFastForward.Checked)
			{
				this.FastForward.Checked = false;
				this.FastFowardToEnd.Checked = false;
			}
		}

		private void TASView_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateSlicerDisplay();
		}

		private void TASView_DoubleClick(object sender, EventArgs e)
		{
			if (TASView.selectedItem <= Global.MovieSession.Movie.StateLastIndex)
			{
				this.stopOnFrame = 0;
				Global.MovieSession.Movie.RewindToFrame(TASView.selectedItem);
			}
			else
			{
				Global.MovieSession.Movie.RewindToFrame(Global.MovieSession.Movie.StateLastIndex);
				this.stopOnFrame = TASView.selectedItem;
				Global.MainForm.PressFrameAdvance = true;
			}

			UpdateValues();
		}

		private void Insert_Click(object sender, EventArgs e)
		{
			InsertFrames();
		}

		private void Delete_Click(object sender, EventArgs e)
		{
			DeleteFrames();
		}

		private static string SaveRecordingAs()
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.MoviesPath);
			sfd.DefaultExt = "." + Global.Config.MovieExtension;
			sfd.FileName = Global.MovieSession.Movie.Filename;
			string filter = "Movie Files (*." + Global.Config.MovieExtension + ")|*." + Global.Config.MovieExtension + "|Savestates|*.state|All Files|*.*";
			sfd.Filter = filter;

			Global.Sound.StopSound();
			var result = sfd.ShowDialog();
			Global.Sound.StartSound();
			if (result == DialogResult.OK)
			{
				return sfd.FileName;
			}
			return "";
		}

		private void TASView_MouseWheel(object sender, MouseEventArgs e)
		{

			//if ((Control.MouseButtons & MouseButtons.Middle) > 0) //adelikat: TODO: right-click + mouse wheel won't work because in this dialog, right-click freezes emulation in the main window.  Why? Hex Editor doesn't do this for instance
			if ((Control.ModifierKeys & Keys.Control) > 0)
			{
				this.stopOnFrame = 0;

				if (e.Delta > 0) //Scroll up
				{
					Global.MovieSession.Movie.RewindToFrame(Global.Emulator.Frame - 1);
				}
				else if (e.Delta < 0) //Scroll down
				{
					Global.MainForm.PressFrameAdvance = true;
				}

				UpdateValues();
			}
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			if (ReadOnlyCheckBox.Checked)
			{
				clearToolStripMenuItem3.Visible = false;
				ContextMenu_Delete.Visible = false;
				cloneToolStripMenuItem1.Visible = false;
				ContextMenu_Insert.Visible = false;
				insertFramesToolStripMenuItem.Visible = false;
				toolStripSeparator5.Visible = false;
				truncateMovieToolStripMenuItem1.Visible = false;
				toolStripSeparator9.Visible = false;
			}
			else
			{
				clearToolStripMenuItem3.Visible = true;
				ContextMenu_Delete.Visible = true;
				cloneToolStripMenuItem1.Visible = true;
				ContextMenu_Insert.Visible = true;
				insertFramesToolStripMenuItem.Visible = true;
				toolStripSeparator5.Visible = true;
				truncateMovieToolStripMenuItem1.Visible = true;
				toolStripSeparator9.Visible = true;
			}
		}

		private void cloneToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Clone();
		}

		private void cloneToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			Clone();
		}

		private void deleteFramesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DeleteFrames();
		}

		private void InsertFrames()
		{
			ListView.SelectedIndexCollection list = TASView.SelectedIndices;
			for (int index = 0; index < list.Count; index++)
			{
				Global.MovieSession.Movie.InsertBlankFrame(list[index]);
			}

			UpdateValues();
		}

		private void DeleteFrames()
		{
			ListView.SelectedIndexCollection list = TASView.SelectedIndices;
			for (int index = 0; index < list.Count; index++)
			{
				Global.MovieSession.Movie.DeleteFrame(list[0]); //TODO: this doesn't allow of non-continuous deletion, instead it should iterate from last to first and remove the iterated value
			}

			UpdateValues();
		}

		private void Clone()
		{
			ListView.SelectedIndexCollection list = TASView.SelectedIndices;
			for (int index = 0; index < list.Count; index++)
			{
				Global.MovieSession.Movie.InsertFrame(Global.MovieSession.Movie.GetInput(list[index]), list[index]);
			}

			UpdateValues();
		}

		private void ClearFrames()
		{
			ListView.SelectedIndexCollection list = TASView.SelectedIndices;
			for (int index = 0; index < list.Count; index++)
			{
				Global.MovieSession.Movie.ClearFrame(list[index]);
			}

			UpdateValues();
		}

		private void InsertNumFrames()
		{
			ListView.SelectedIndexCollection list = TASView.SelectedIndices;
			if (list.Count > 0)
			{
				InputPrompt prompt = new InputPrompt();
				prompt.TextInputType = InputPrompt.InputType.UNSIGNED;
				prompt.SetMessage("How many frames?");
				prompt.SetInitialValue("1");
				prompt.SetTitle("Insert new frames");
				prompt.ShowDialog();
				if (prompt.UserOK)
				{
					int frames = int.Parse(prompt.UserText);
					for (int i = 0; i < frames; i++)
					{
						Global.MovieSession.Movie.InsertBlankFrame(list[0] + i);
					}
				}
			}
			UpdateValues();
		}

		private void SelectAll()
		{
			for (int i = 0; i < TASView.ItemCount; i++)
			{
				TASView.SelectItem(i, true);
			}
		}

		private void clearToolStripMenuItem2_Click(object sender, EventArgs e)
		{
			ClearFrames();
		}

		private void insertNumFramesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			InsertNumFrames();
		}

		private void insertFramesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			InsertNumFrames();
		}

		private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SelectAll();
		}

		private void SelectAll_Click(object sender, EventArgs e)
		{
			SelectAll();
		}

		private void TruncateMovie()
		{
			ListView.SelectedIndexCollection list = TASView.SelectedIndices;
			if (list.Count > 0)
			{
				Global.MovieSession.Movie.TruncateMovie(list[0]);
				UpdateValues();
			}
		}

		private void truncateMovieToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TruncateMovie();
		}

		private void truncateMovieToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			TruncateMovie();
		}

		private void CopySelectionToClipBoard()
		{
			Clipboard.Clear();
			ListView.SelectedIndexCollection list = TASView.SelectedIndices;
			for (int i = 0; i < list.Count; i++)
			{
				ClipboardEntry entry = new ClipboardEntry();
				entry.frame = list[i];
				entry.inputstr = Global.MovieSession.Movie.GetInput(list[i]);
				Clipboard.Add(entry);
			}
			UpdateSlicerDisplay();
		}

		private void copyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CopySelectionToClipBoard();
		}

		private void UpdateSlicerDisplay()
		{
			ListView.SelectedIndexCollection list = TASView.SelectedIndices;
			if (list.Count > 0)
			{
				SelectionDisplay.Text = list.Count.ToString() + " row";
			}
			else
			{
				SelectionDisplay.Text = "none";
			}

			if (Clipboard.Count > 0)
			{
				ClipboardDisplay.Text = Clipboard.Count.ToString() + " row";
			}
			else
			{
				ClipboardDisplay.Text = "none";
			}
		}

		private void TASView_Click(object sender, EventArgs e)
		{
			UpdateSlicerDisplay();
		}

		private void PasteSelectionOnTop()
		{
			ListView.SelectedIndexCollection list = TASView.SelectedIndices;
			if (list.Count > 0)
			{
				for (int i = 0; i < Clipboard.Count; i++)
				{
					Global.MovieSession.Movie.ModifyFrame(Clipboard[i].inputstr, list[0] + i);
				}
			}
			UpdateValues();
		}

		private void PasteSelectionInsert()
		{
			ListView.SelectedIndexCollection list = TASView.SelectedIndices;
			if (list.Count > 0)
			{
				for (int i = 0; i < Clipboard.Count; i++)
				{
					Global.MovieSession.Movie.InsertFrame(Clipboard[i].inputstr, list[0] + i);
				}
			}
			UpdateValues();
		}

		private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PasteSelectionOnTop();
		}

		private void pasteInsertToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PasteSelectionInsert();
		}

		private void CutSelection()
		{
			ListView.SelectedIndexCollection list = TASView.SelectedIndices;
			if (list.Count > 0)
			{
				Clipboard.Clear();
				for (int i = 0; i < list.Count; i++)
				{
					ClipboardEntry entry = new ClipboardEntry();
					entry.frame = list[i];
					entry.inputstr = Global.MovieSession.Movie.GetInput(list[i]);
					Clipboard.Add(entry);
					Global.MovieSession.Movie.DeleteFrame(list[0]);
				}

				UpdateValues();
			}
		}

		private void cutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CutSelection();
		}

		private void TASView_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.Delete:
					DeleteFrames();
					break;
				case Keys.Insert:
					InsertFrames();
					break;
			}
		}

		private void TASView_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Middle)
			{
				Global.MainForm.TogglePause();
			}
		}

		private void TAStudio_KeyPress(object sender, KeyPressEventArgs e)
		{
			Global.MainForm.ProcessInput();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			//Do visualization
			DoVisualizerScan = true;
			VisualizerBox.Refresh();
			DoVisualizerScan = false;
		}

		private void VisualizerBox_Paint(object sender, PaintEventArgs e)
		{
			if (DoVisualizerScan)
			{
				StateVisualizer vizualizer = new StateVisualizer();

				for (int i = 0; i < vizualizer.TimeLineCount; i++)
				{

				}
				
				
				int x = 0;
				x++;
				int y = x;
				y++;
			}
		}

		private bool DoVisualizerScan = false;

		private void VisualizerBox_Enter(object sender, EventArgs e)
		{

		}
	}
}
