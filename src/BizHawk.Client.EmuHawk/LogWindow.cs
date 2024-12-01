using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Common.StringExtensions;

// todo - perks - pause, copy to clipboard, backlog length limiting

namespace BizHawk.Client.EmuHawk
{
	public partial class LogWindow : ToolFormBase, IToolFormAutoConfig
	{
		public static Icon ToolIcon
			=> Properties.Resources.CommandWindow;

		// TODO: only show add to game db when this is a Rom details dialog
		// Let user decide what type (instead of always adding it as a good dump)
		private readonly List<string> _lines = new List<string>();
		private LogWriter _logWriter;

		[RequiredService]
		private IEmulator Emulator { get; set; }

		private string _windowTitle = "Log Window";

		protected override string WindowTitle => _windowTitle;

		protected override string WindowTitleStatic => "Log Window";

		public LogWindow()
		{
			InitializeComponent();
			Icon = ToolIcon;
			AddToGameDbBtn.Image = Properties.Resources.Add;
			Closing += (o, e) =>
			{
				Detach();
			};
			ListView_ClientSizeChanged(null, null);
			Attach();
		}

		private void Attach()
		{
			_logWriter = new LogWriter();
			Console.SetOut(_logWriter);
			_logWriter.Emit = appendInvoked;
		}

		private void Detach()
		{
			Console.SetOut(new StreamWriter(Console.OpenStandardOutput())
			{
				AutoFlush = true
			});
			_logWriter.Close();
			_logWriter = null;
		}

		public void ShowReport(string title, string report)
		{
			var ss = report.Split('\n');
			
			lock (_lines)
				foreach (var s in ss)
				{
					_lines.Add(s.TrimEnd('\r'));
				}

			virtualListView1.VirtualListSize = ss.Length;
			_windowTitle = title;
			UpdateWindowTitle();
			btnClear.Visible = false;
		}

		private void append(string str, bool invoked)
		{
			var ss = str.Split('\n');
			foreach (var s in ss)
			{
				if (!string.IsNullOrWhiteSpace(s))
				{
					lock (_lines)
					{
						_lines.Add(s.TrimEnd('\r'));
						if (invoked)
						{
							//basically an easy way to post an update message which should hopefully happen before anything else happens (redraw or user interaction)
							BeginInvoke(doUpdateListSize);
						}
						else
							doUpdateListSize();
					}
				}
			}
		}

		private void doUpdateListSize()
		{
			virtualListView1.VirtualListSize = _lines.Count;
			virtualListView1.EnsureVisible(_lines.Count - 1);
		}

		private void appendInvoked(string str)
		{
			append(str, true);
		}

		private void BtnClear_Click(object sender, EventArgs e)
		{
			lock (_lines)
			{
				_lines.Clear();
				virtualListView1.VirtualListSize = 0;
				virtualListView1.SelectedIndices.Clear();
			}
		}

		private void BtnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void LogWindow_Load(object sender, EventArgs e)
		{
			HideShowGameDbButton();
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
			if (e.IsCtrl(Keys.C))
			{
				ButtonCopy_Click(null, null);
			}
		}

		private void ButtonCopy_Click(object sender, EventArgs e)
		{
			string s;
			lock (_lines)
			{
				if (virtualListView1.SelectedIndices.Count > 1)
				{
					StringBuilder sb = new();
					foreach (int i in virtualListView1.SelectedIndices) sb.AppendLine(_lines[i]);
					s = sb.ToString();
				}
				else if (virtualListView1.SelectedIndices.Count is 1)
				{
					s = _lines[virtualListView1.SelectedIndices[0]]
						.RemovePrefix(SHA1Checksum.PREFIX + ":")
						.RemovePrefix(MD5Checksum.PREFIX + ":");
				}
				else
				{
					return;
				}
			}
			s = s.Trim();
			if (!string.IsNullOrWhiteSpace(s)) Clipboard.SetText(s, TextDataFormat.Text);
		}

		private void ButtonCopyAll_Click(object sender, EventArgs e)
		{
			var sb = new StringBuilder();
			lock(_lines)
				foreach (var s in _lines)
					sb.AppendLine(s);
			if (sb.Length > 0)
				Clipboard.SetText(sb.ToString(), TextDataFormat.Text);
		}

		private void HideShowGameDbButton()
		{
			AddToGameDbBtn.Visible = Emulator.CanGenerateGameDBEntries()
				&& (Game.Status == RomStatus.Unknown || Game.Status == RomStatus.NotInDatabase);
		}

		private void AddToGameDbBtn_Click(object sender, EventArgs e)
		{
			using var picker = new RomStatusPicker();
			var result = picker.ShowDialog();
			if (result.IsOk())
			{
				var gameDbEntry = Emulator.AsGameDBEntryGenerator().GenerateGameDbEntry();
				gameDbEntry.Status = picker.PickedStatus;
				Database.SaveDatabaseEntry(gameDbEntry);
				MainForm.UpdateDumpInfo(gameDbEntry.Status);
				HideShowGameDbButton();
			}
		}

		private class LogWriter : TextWriter
		{
			public override void Write(char[] buffer, int offset, int count)
			{
				Emit(new string(buffer, offset, count));
			}

			public override Encoding Encoding => Encoding.Unicode;

			public Action<string> Emit;
		}
	}
}
