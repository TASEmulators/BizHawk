using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

//todo - perks - pause, copy to clipboard, backlog length limiting

namespace BizHawk.MultiClient
{
	public partial class LogWindow : Form
	{
		private readonly List<string> Lines = new List<string>();

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
			virtualListView1_ClientSizeChanged(null, null);
		}

		public static void ShowReport(string title, string report, IWin32Window parent)
		{
			using (var dlg = new LogWindow())
			{
				var ss = report.Split('\n');
				foreach (var s in ss)
					dlg.Lines.Add(s);
				dlg.virtualListView1.ItemCount = ss.Length;
				dlg.Text = title;
				dlg.btnClear.Visible = false;
				dlg.ShowDialog(parent);
			}
		}

		public void Append(string str)
		{
			var ss = str.Split('\n');
			foreach (var s in ss)
			{
				if (!string.IsNullOrWhiteSpace(s))
				{
					Lines.Add(s);
					virtualListView1.ItemCount++;
				}
			}
		}

		private void btnClear_Click(object sender, EventArgs e)
		{
			Lines.Clear();
			virtualListView1.ItemCount = 0;
			virtualListView1.SelectedIndices.Clear();
		}

		private void btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void LogWindow_Load(object sender, EventArgs e)
		{
			if (Global.Config.LogWindowSaveWindowPosition)
			{
				if (Global.Config.LogWindowSaveWindowPosition && Global.Config.LogWindowWndx >= 0 && Global.Config.LogWindowWndy >= 0)
					Location = new Point(Global.Config.LogWindowWndx, Global.Config.LogWindowWndy);

				if (Global.Config.LogWindowWidth >= 0 && Global.Config.LogWindowHeight >= 0)
				{
					Size = new Size(Global.Config.LogWindowWidth, Global.Config.LogWindowHeight);
				}
			}
		}

		public void SaveConfigSettings()
		{
			if (Global.Config.LogWindowSaveWindowPosition)
			{
				Global.Config.LogWindowWndx = Location.X;
				Global.Config.LogWindowWndy = Location.Y;
				Global.Config.LogWindowWidth = Right - Left;
				Global.Config.LogWindowHeight = Bottom - Top;
			}
		}

		private void virtualListView1_QueryItemText(int item, int subItem, out string text)
		{
			text = Lines[item];
		}

		private void virtualListView1_ClientSizeChanged(object sender, EventArgs e)
		{
			virtualListView1.Columns[0].Width = virtualListView1.ClientSize.Width;
		}

		private void buttonCopy_Click(object sender, EventArgs e)
		{
			var sb = new StringBuilder();
			foreach (int i in virtualListView1.SelectedIndices)
				sb.AppendLine(Lines[i]);
			if (sb.Length > 0)
				Clipboard.SetText(sb.ToString(), TextDataFormat.Text);
		}

		private void buttonCopyAll_Click(object sender, EventArgs e)
		{
			var sb = new StringBuilder();
			foreach (var s in Lines)
				sb.Append(s);
			if (sb.Length > 0)
				Clipboard.SetText(sb.ToString(), TextDataFormat.Text);
		}
	}
}
