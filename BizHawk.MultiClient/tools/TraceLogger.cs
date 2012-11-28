using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace BizHawk.MultiClient
{
	public partial class TraceLogger : Form
	{
		//Refresh rate slider
		//Make faster, such as not saving to disk until the logging is stopped, dont' add to Instructions list every frame, etc
		//Remember window size

		List<string> Instructions = new List<string>();
		FileInfo LogFile;

		public TraceLogger()
		{
			InitializeComponent();
			
			TraceView.QueryItemText += new QueryItemTextHandler(TraceView_QueryItemText);
			TraceView.QueryItemBkColor += new QueryItemBkColorHandler(TraceView_QueryItemBkColor);
			TraceView.VirtualMode = true;

			Closing += (o, e) => SaveConfigSettings();
		}

		public void SaveConfigSettings()
		{
			Global.CoreInputComm.Tracer.Enabled = false;
			Global.Config.TraceLoggerWndx = this.Location.X;
			Global.Config.TraceLoggerWndy = this.Location.Y;
		}

		private void TraceView_QueryItemBkColor(int index, int column, ref Color color)
		{
			//TODO
		}

		private void TraceView_QueryItemText(int index, int column, out string text)
		{
			if (index < Instructions.Count)
			{
				text = Instructions[index];
			}
			else
			{
				text = "";
			}
		}

		private void TraceLogger_Load(object sender, EventArgs e)
		{
			if (Global.Config.TraceLoggerSaveWindowPosition && Global.Config.TraceLoggerWndx >= 0 && Global.Config.TraceLoggerWndy >= 0)
			{
				this.Location = new Point(Global.Config.TraceLoggerWndx, Global.Config.TraceLoggerWndy);
			}

			ClearList();
			LoggingEnabled.Checked = true;
			Global.CoreInputComm.Tracer.Enabled = true;
			SetTracerBoxTitle();
			Restart();
		}

		public void UpdateValues()
		{
			DoInstructions();
		}

		public void Restart()
		{
			if (!this.IsHandleCreated || this.IsDisposed)
			{
				return;
			}
			else
			{
				if (Global.Emulator.CoreOutputComm.CpuTraceAvailable)
				{
					ClearList();
					TraceView.Columns[0].Text = Global.Emulator.CoreOutputComm.TraceHeader;
				}
				else
				{
					this.Close();
				}
			}
		}

		private void ClearList()
		{
			Instructions.Clear();
			TraceView.ItemCount = 0;
			SetTracerBoxTitle();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void LoggingEnabled_CheckedChanged(object sender, EventArgs e)
		{
			Global.CoreInputComm.Tracer.Enabled = LoggingEnabled.Checked;
			SetTracerBoxTitle();
		}

		private void ClearButton_Click(object sender, EventArgs e)
		{
			ClearList();
		}

		private void DoInstructions()
		{
			if (ToWindowRadio.Checked)
			{
				LogToWindow();
				SetTracerBoxTitle();
			}
			else
			{
				LogToFile();
			}
		}

		private void LogToFile()
		{
			using (StreamWriter sw = new StreamWriter(LogFile.FullName, true))
			{
				sw.Write(Global.CoreInputComm.Tracer.TakeContents());
			}
		}

		private void LogToWindow()
		{
			string[] instructions = Global.CoreInputComm.Tracer.TakeContents().Split('\n');
			if (!String.IsNullOrWhiteSpace(instructions[0]))
			{
				foreach (string s in instructions)
				{
					Instructions.Add(s);
				}

				
			}
			if (Instructions.Count >= Global.Config.TraceLoggerMaxLines)
			{
				int x = Instructions.Count - Global.Config.TraceLoggerMaxLines;
				Instructions.RemoveRange(0, x);
			}

			TraceView.ItemCount = Instructions.Count;
		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TraceLoggerAutoLoad ^= true;
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			autoloadToolStripMenuItem.Checked = Global.Config.TraceLoggerAutoLoad;
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.TraceLoggerSaveWindowPosition;
		}

		private void CloseButton_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TraceLoggerSaveWindowPosition ^= true;
		}

		private Point GetPromptPoint()
		{
			Point p = new Point(TraceView.Location.X + 30, TraceView.Location.Y + 30);
			Point q = new Point();
			q = PointToScreen(p);
			return q;
		}

		private void setMaxWindowLinesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			InputPrompt p = new InputPrompt();
			p.SetMessage("Max lines to display in the window");
			p.SetInitialValue(Global.Config.TraceLoggerMaxLines.ToString());
			p.TextInputType = InputPrompt.InputType.UNSIGNED;
			p._Location = GetPromptPoint();
			DialogResult result =  p.ShowDialog();
			if (p.UserOK)
			{
				int x = int.Parse(p.UserText);
				if (x > 0)
				{
					Global.Config.TraceLoggerMaxLines = x;
				}
			}
		}

		private void SetTracerBoxTitle()
		{
			if (Global.CoreInputComm.Tracer.Enabled)
			{
				if (ToFileRadio.Checked)
				{
					TracerBox.Text = "Trace log - logging to file...";
				}
				else if (Instructions.Count > 0)
				{
					TracerBox.Text = "Trace log - logging - " + Instructions.Count.ToString() + " instructions";
				}
				else
				{
					TracerBox.Text = "Trace log - logging...";
				}
			}
			else
			{
				if (Instructions.Count > 0)
				{
					TracerBox.Text = "Trace log - " + Instructions.Count.ToString() + " instructions";
				}
				else
				{
					TracerBox.Text = "Trace log";
				}
			}
		}

		private void ToFileRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (ToFileRadio.Checked)
			{
				FileBox.Visible = true;
				BrowseBox.Visible = true;
				string name = PathManager.FilesystemSafeName(Global.Game);
				string filename = Path.Combine(PathManager.MakeAbsolutePath(Global.Config.LogPath, ""), name) + ".txt";
				LogFile = new FileInfo(filename);
				if (!LogFile.Directory.Exists)
				{
					LogFile.Directory.Create();
				}
				if (LogFile.Exists)
				{
					LogFile.Delete();
					LogFile.Create();
				}
				else
				{
					LogFile.Create();
				}

				FileBox.Text = LogFile.FullName;
			}
			else
			{
				CloseFile();
				FileBox.Visible = false;
				BrowseBox.Visible = false;

			}

			SetTracerBoxTitle();
		}

		private void CloseFile()
		{
			//TODO: save the remaining instructions in CoreComm
		}

		private void TraceView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Control && e.KeyCode == Keys.C)
			{
				ListView.SelectedIndexCollection indexes = TraceView.SelectedIndices;

				if (indexes.Count > 0)
				{
					StringBuilder blob = new StringBuilder();
					foreach (int x in indexes)
					{
						blob.Append(Instructions[x]);
						blob.Append("\r\n");
					}
					blob.Remove(blob.Length - 2, 2); //Lazy way to not have a line break at the end
					Clipboard.SetDataObject(blob.ToString());
				}
			}
		}

		private void BrowseBox_Click(object sender, EventArgs e)
		{
			var file = GetFileFromUser();
			if (file != null)
			{
				LogFile = file;
				FileBox.Text = LogFile.FullName;
			}
		}

		private FileInfo GetFileFromUser()
		{
			var sfd = new SaveFileDialog();
			if (LogFile == null)
			{
				string name = PathManager.FilesystemSafeName(Global.Game);
				sfd.FileName = name + ".txt";
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.LogPath, "");
			}
			else if (!String.IsNullOrWhiteSpace(LogFile.FullName))
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
				sfd.InitialDirectory = Path.GetDirectoryName(LogFile.FullName);
			}
			else
			{
				sfd.FileName = Path.GetFileNameWithoutExtension(LogFile.FullName);
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.LogPath, "");
			}

			sfd.Filter = "Text Files (*.txt)|*.txt|Log Files (*.log)|*.log|All Files|*.*";
			sfd.RestoreDirectory = true;
			Global.Sound.StopSound();

			var result = sfd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
			{
				return null;
			}
			else
			{
				return new FileInfo(sfd.FileName);
				
			}
		}

		private void saveLogToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var file = GetFileFromUser();
			if (file != null)
			{
				DumpListToDisk(file);
				Global.OSD.AddMessage("Log dumped to " + file.FullName);
			}
		}

		private void DumpListToDisk(FileInfo file)
		{
			using (StreamWriter sw = new StreamWriter(file.FullName))
			{
				foreach (string s in Instructions)
				{
					sw.WriteLine(s);
				}
			}
		}

		void CopyAllToClipboard()
		{
			StringBuilder sb = new StringBuilder();
			foreach (string s in Instructions)
				sb.AppendLine(s);
			string ss = sb.ToString();
			if (!string.IsNullOrEmpty(ss))
				Clipboard.SetText(sb.ToString(), TextDataFormat.Text);
		}

		private void TraceLogger_KeyDown(object sender, KeyEventArgs e)
		{
			if (ModifierKeys.HasFlag(Keys.Control) && e.KeyCode == Keys.C)
				CopyAllToClipboard();
		}

		private void copyAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CopyAllToClipboard();
		}
	}
}
