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
		//Save to file - saves what's on screen to disk (defaults to the current log file)
		//Show file that is being logged to
		//Browse button to set file
		//Refresh rate slider

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

		private void setMaxWindowLinesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			InputPrompt p = new InputPrompt();
			p.SetMessage("Max lines to display in the window");
			p.SetInitialValue(Global.Config.TraceLoggerMaxLines.ToString());
			p.TextInputType = InputPrompt.InputType.UNSIGNED;
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
			}
			else
			{
				CloseFile();
			}

			SetTracerBoxTitle();
		}

		private void CloseFile()
		{
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
						blob.Append('\n');
					}
					blob.Remove(blob.Length - 1, 1); //Lazy way to not have a line break at the end
					Clipboard.SetDataObject(blob.ToString());
				}
			}
		}
	}
}
