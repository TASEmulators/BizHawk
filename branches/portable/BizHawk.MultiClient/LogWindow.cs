using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

//todo - perks - pause, copy to clipboard, backlog length limiting

namespace BizHawk.MultiClient
{
	public partial class LogWindow : Form
	{
		int defaultWidth;
		int defaultHeight;

		public LogWindow()
		{
			InitializeComponent();
			Closing += (o, e) =>
			{
				Global.Config.ShowLogWindow = false;
				Global.MainForm.notifyLogWindowClosing();
				LogConsole.notifyLogWindowClosing();
				SaveConfigSettings();
			};
		}

		public void ShowReport(string title, string report)
		{
			Text = title;
			textBox1.Text = report;
			btnClear.Visible = false;
			ShowDialog();
		}

		public void SetLogText(string str)
		{
			textBox1.Text = str;
			textBox1.SelectionStart = str.Length;
			textBox1.ScrollToCaret();
			Refresh();
		}

		StringBuilder sbLog = new StringBuilder();
		public void Append(string str)
		{
			sbLog.Append(str);
			SetLogText(sbLog.ToString());
		}

		private void btnClear_Click(object sender, EventArgs e)
		{
			sbLog.Length = 0;
			SetLogText("");
			Refresh();
		}

		private void btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void LogWindow_Load(object sender, EventArgs e)
		{
			defaultWidth = this.Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = this.Size.Height;

			if (Global.Config.LogWindowSaveWindowPosition)
			{
				if (Global.Config.LogWindowSaveWindowPosition && Global.Config.LogWindowWndx >= 0 && Global.Config.LogWindowWndy >= 0)
					this.Location = new Point(Global.Config.LogWindowWndx, Global.Config.LogWindowWndy);

				if (Global.Config.LogWindowWidth >= 0 && Global.Config.LogWindowHeight >= 0)
				{
					this.Size = new System.Drawing.Size(Global.Config.LogWindowWidth, Global.Config.LogWindowHeight);
				}
			}
		}

		public void SaveConfigSettings()
		{
			if (Global.Config.LogWindowSaveWindowPosition)
			{
				Global.Config.LogWindowWndx = this.Location.X;
				Global.Config.LogWindowWndy = this.Location.Y;
				Global.Config.LogWindowWidth = this.Right - this.Left;
				Global.Config.LogWindowHeight = this.Bottom - this.Top;
			}
		}
	}
}
