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
		int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
		int defaultHeight;

		public TAStudio()
		{
			InitializeComponent();
			Closing += (o, e) => SaveConfigSettings();
			TASView.QueryItemText += new QueryItemTextHandler(TASView_QueryItemText);
			TASView.QueryItemBkColor += new QueryItemBkColorHandler(TASView_QueryItemBkColor);
			TASView.VirtualMode = true;
		}

		public void UpdateValues()
		{
			DisplayList();
		}

		private void TASView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (index == Global.Emulator.Frame)
				color = Color.LightGreen;
		}

		private void TASView_QueryItemText(int index, int column, out string text)
		{
			text = "";
			if (column == 0)
				text = String.Format("{0:#,##0}", index);
			if (column == 1)
				text = Global.MainForm.UserMovie.GetInputFrame(index);
		}

		private void DisplayList()
		{
			TASView.ItemCount = Global.MainForm.UserMovie.GetMovieLength();
		}

		private void TAStudio_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
			ReadOnlyCheckBox.Checked = Global.MainForm.ReadOnly;
			DisplayList();
		}

		private void LoadConfigSettings()
		{
			defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = Size.Height;
		}

		private void SaveConfigSettings()
		{
			Global.Config.TASWndx = this.Location.X;
			Global.Config.TASWndy = this.Location.Y;
			Global.Config.TASWidth = this.Right - this.Left;
			Global.Config.TASHeight = this.Bottom - this.Top;
		}

		public void Restart()
		{

		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void settingsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.TAStudioSaveWindowPosition;
			autoloadToolStripMenuItem.Checked = Global.Config.AutoloadTAStudio;
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
			Global.MainForm.StopUserMovie();
		}

		private void FrameAdvanceButton_Click(object sender, EventArgs e)
		{
			Global.MainForm.PressFrameAdvance = true;
		}

		private void RewindButton_Click(object sender, EventArgs e)
		{
			Global.MainForm.PressRewind = true;
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
			Global.MainForm.ToggleReadOnly();
			if (ReadOnlyCheckBox.Checked)
			{
				ReadOnlyCheckBox.BackColor = System.Drawing.SystemColors.Control;
				//TODO: set tooltip text to "In Read-Only Mode)
			}
			else
			{
				ReadOnlyCheckBox.BackColor = Color.LightCoral;
				//TOD: set tooltip text to "In Read+Write Mode"
			}
		}

		private void toolStripButton1_Click(object sender, EventArgs e)
		{
			Global.MainForm.PlayMovieFromBeginning();
		}
	}
}
