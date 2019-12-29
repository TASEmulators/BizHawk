using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;

//todo - perks - pause, copy to clipboard, backlog length limiting

namespace BizHawk.Client.EmuHawk
{
	public partial class LogWindow : Form
	{
		//TODO: only show add to game db when this is a Rom details dialog
		//Let user decide what type (instead of always adding it as a good dump)
		private readonly List<string> _lines = new List<string>();
		private readonly MainForm _mainForm;

		public LogWindow(MainForm mainForm)
		{
			_mainForm = mainForm;
			InitializeComponent();
			Closing += (o, e) =>
			{
				Global.Config.ShowLogWindow = false;
				mainForm.NotifyLogWindowClosing();
				LogConsole.NotifyLogWindowClosing();
				SaveConfigSettings();
			};
			ListView_ClientSizeChanged(null, null);
		}

		public static void ShowReport(string title, string report, MainForm parent)
		{
			using var dlg = new LogWindow(parent);
			var ss = report.Split('\n');
			foreach (var s in ss)
			{
				dlg._lines.Add(s.TrimEnd('\r'));
			}

			dlg.virtualListView1.VirtualListSize = ss.Length;
			dlg.Text = title;
			dlg.btnClear.Visible = false;
			dlg.ShowDialog(parent);
		}

		public void Append(string str)
		{
			var ss = str.Split('\n');
			foreach (var s in ss)
			{
				if (!string.IsNullOrWhiteSpace(s))
				{
					_lines.Add(s.TrimEnd('\r'));
					virtualListView1.VirtualListSize++;
				}
			}
		}

		private void btnClear_Click(object sender, EventArgs e)
		{
			_lines.Clear();
			virtualListView1.VirtualListSize = 0;
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

			HideShowGameDbButton();
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

		private void ListView_QueryItemText(object sender, RetrieveVirtualItemEventArgs e)
		{
			e.Item = new ListViewItem(_lines[e.ItemIndex]);
		}

		private void ListView_ClientSizeChanged(object sender, EventArgs e)
		{
			virtualListView1.Columns[0].Width = virtualListView1.ClientSize.Width;
		}

		private void ListView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.C && e.Control && !e.Alt && !e.Shift)
			{
				ButtonCopy_Click(null, null);
			}
		}

		private void ButtonCopy_Click(object sender, EventArgs e)
		{
			var sb = new StringBuilder();
			foreach (int i in virtualListView1.SelectedIndices)
				sb.AppendLine(_lines[i]);
			if (sb.Length > 0)
				Clipboard.SetText(sb.ToString(), TextDataFormat.Text);
		}

		private void ButtonCopyAll_Click(object sender, EventArgs e)
		{
			var sb = new StringBuilder();
			foreach (var s in _lines)
				sb.AppendLine(s);
			if (sb.Length > 0)
				Clipboard.SetText(sb.ToString(), TextDataFormat.Text);
		}

		private void HideShowGameDbButton()
		{
			AddToGameDbBtn.Visible = Global.Emulator.CanGenerateGameDBEntries()
				&& (Global.Game.Status == RomStatus.Unknown || Global.Game.Status == RomStatus.NotInDatabase);
		}

		private void AddToGameDbBtn_Click(object sender, EventArgs e)
		{
			using var picker = new RomStatusPicker();
			var result = picker.ShowDialog();
			if (result == DialogResult.OK)
			{
				var gameDbEntry = Global.Emulator.AsGameDBEntryGenerator().GenerateGameDbEntry();
				var userDb = Path.Combine(PathManager.GetExeDirectoryAbsolute(), "gamedb", "gamedb_user.txt");
				Global.Game.Status = gameDbEntry.Status = picker.PickedStatus;
				Database.SaveDatabaseEntry(userDb, gameDbEntry);
				_mainForm.UpdateDumpIcon();
				HideShowGameDbButton();
			}
		}
	}
}
