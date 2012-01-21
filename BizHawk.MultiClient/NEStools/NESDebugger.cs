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
	public partial class NESDebugger : Form
	{
		int defaultWidth;       //For saving the default size of the dialog, so the user can restore if desired
		int defaultHeight;

		public NESDebugger()
		{
			InitializeComponent();
			DebugView.QueryItemText += new QueryItemTextHandler(DebugView_QueryItemText);
			DebugView.QueryItemBkColor += new QueryItemBkColorHandler(DebugView_QueryItemBkColor);
			DebugView.VirtualMode = true;
			Closing += (o, e) => SaveConfigSettings();
		}

		public void Restart()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;
		}

		private void NESDebugger_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
		}

		private void LoadConfigSettings()
		{
			defaultWidth = this.Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = this.Size.Height;

			if (Global.Config.NESDebuggerSaveWindowPosition && Global.Config.NESDebuggerWndx >= 0 && Global.Config.NESDebuggerWndy >= 0)
				this.Location = new Point(Global.Config.NESDebuggerWndx, Global.Config.NESDebuggerWndy);

			if (Global.Config.NESDebuggerWidth >= 0 && Global.Config.NESDebuggerHeight >= 0)
			{
				this.Size = new System.Drawing.Size(Global.Config.NESDebuggerWidth, Global.Config.NESDebuggerHeight);
			}
		}

		public void SaveConfigSettings()
		{
			Global.Config.NESDebuggerWndx = this.Location.X;
			Global.Config.NESDebuggerWndy = this.Location.Y;
			Global.Config.NESDebuggerWidth = this.Right - this.Left;
			Global.Config.NESDebuggerHeight = this.Bottom - this.Top;
		}

		private void DebugView_QueryItemBkColor(int index, int column, ref Color color)
		{

		}

		void DebugView_QueryItemText(int index, int column, out string text)
		{
			text = "";
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoLoadNESDebugger ^= true;
		}

		private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.NESDebuggerSaveWindowPosition ^= true;
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			autoloadToolStripMenuItem.Checked = Global.Config.AutoLoadNESDebugger;
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.NESDebuggerSaveWindowPosition;
		}

		private void restoreOriginalSizeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Size = new System.Drawing.Size(defaultWidth, defaultHeight);
		}
	}
}
