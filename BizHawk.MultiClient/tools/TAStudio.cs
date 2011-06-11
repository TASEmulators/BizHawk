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

        private void TASView_QueryItemBkColor(int index, int column, ref Color color)
        {

        }

        private void TASView_QueryItemText(int index, int column, out string text)
        {
            text = "";
        }

        private void TAStudio_Load(object sender, EventArgs e)
        {
            LoadConfigSettings();
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
    }
}
