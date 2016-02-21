using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class TraceLogger : Form, IToolFormAutoConfig
	{
		[RequiredService]
		private ITraceable Tracer { get; set; }

		[ConfigPersist]
		private int MaxLines { get; set; }

		private readonly List<TraceInfo> _instructions = new List<TraceInfo>();
		
		private FileInfo _logFile;

		public TraceLogger()
		{
			InitializeComponent();

			TraceView.QueryItemText += TraceView_QueryItemText;
			TraceView.VirtualMode = true;

			Closing += (o, e) => SaveConfigSettings();

			MaxLines = 10000;
		}

		public bool UpdateBefore
		{
			get { return false; }
		}

		public bool AskSaveChanges()
		{
			return true;
		}

		private void SaveConfigSettings()
		{
			Tracer.Enabled = LoggingEnabled.Checked;
		}

		private void TraceView_QueryItemText(int index, int column, out string text)
		{
			text = string.Empty;
			if (index < _instructions.Count)
			{
				switch (column)
				{
					case 0:
						text = _instructions[index].Disassembly;
						break;
					case 1:
						text = _instructions[index].RegisterInfo;
						break;

				}
			}
		}

		private void TraceLogger_Load(object sender, EventArgs e)
		{
			ClearList();
			Tracer.Enabled = LoggingEnabled.Checked = false;
			SetTracerBoxTitle();
		}

		public void UpdateValues()
		{
			TraceView.BlazingFast = !GlobalWin.MainForm.EmulatorPaused;
			if (ToWindowRadio.Checked)
			{
				LogToWindow();
			}
			else
			{
				LogToFile();
			}
		}

		public void FastUpdate()
		{
			// TODO: get instructions, but don't draw on screen
			UpdateValues();
		}

		public void Restart()
		{
			ClearList();
			Tracer.Enabled = LoggingEnabled.Checked = false;
			SetTracerBoxTitle();
		}

		private void ClearList()
		{
			_instructions.Clear();
			TraceView.ItemCount = 0;
			SetTracerBoxTitle();
		}

		// TODO: LogToFile and DumpListTODisk have a lot of repeated code
		private void LogToFile()
		{
			var todo = Tracer.TakeContents();
			if (todo.Any())
			{
				using (var sw = new StreamWriter(_logFile.FullName, true))
				{
					int pad = todo.Max(i => i.Disassembly.Length) + 4;

					foreach (var instruction in todo)
					{
						sw.WriteLine(instruction.Disassembly.PadRight(pad)
							+ instruction.RegisterInfo
						);
					}

					sw.Write(Tracer.TakeContents());
				}
			}
		}

		private void DumpListToDisk(FileSystemInfo file)
		{
			using (var sw = new StreamWriter(file.FullName))
			{
				int pad = _instructions.Max(i => i.Disassembly.Length) + 4;

				foreach (var instruction in _instructions)
				{
					sw.WriteLine(instruction.Disassembly.PadRight(pad)
						+ instruction.RegisterInfo
					);
				}
			}
		}

		private void LogToWindow()
		{
			_instructions.AddRange(Tracer.TakeContents());
			

			if (_instructions.Count >= MaxLines)
			{
				_instructions.RemoveRange(0, _instructions.Count - MaxLines);
			}

			TraceView.ItemCount = _instructions.Count;
		}

		private void SetTracerBoxTitle()
		{
			if (Tracer.Enabled)
			{
				if (ToFileRadio.Checked)
				{
					TracerBox.Text = "Trace log - logging to file...";
				}
				else if (_instructions.Any())
				{
					TracerBox.Text = "Trace log - logging - " + _instructions.Count + " instructions";
				}
				else
				{
					TracerBox.Text = "Trace log - logging...";
				}
			}
			else
			{
				if (_instructions.Any())
				{
					TracerBox.Text = "Trace log - " + _instructions.Count + " instructions";
				}
				else
				{
					TracerBox.Text = "Trace log";
				}
			}
		}

		private void CloseFile()
		{
			// TODO: save the remaining instructions in CoreComm
		}

		private FileInfo GetFileFromUser()
		{
			var sfd = new SaveFileDialog();
			if (_logFile == null)
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game) + ".txt";
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.LogPathFragment, null);
			}
			else if (!string.IsNullOrWhiteSpace(_logFile.FullName))
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
				sfd.InitialDirectory = Path.GetDirectoryName(_logFile.FullName);
			}
			else
			{
				sfd.FileName = Path.GetFileNameWithoutExtension(_logFile.FullName);
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.LogPathFragment, null);
			}

			sfd.Filter = "Text Files (*.txt)|*.txt|Log Files (*.log)|*.log|All Files|*.*";
			sfd.RestoreDirectory = true;
			var result = sfd.ShowHawkDialog();
			if (result == DialogResult.OK)
			{
				return new FileInfo(sfd.FileName);
			}
			else
			{
				return null;
			}
		}

		#region Events

		#region Menu Items

		private void SaveLogMenuItem_Click(object sender, EventArgs e)
		{
			var file = GetFileFromUser();
			if (file != null)
			{
				DumpListToDisk(file);
				GlobalWin.OSD.AddMessage("Log dumped to " + file.FullName);
			}
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void CopyMenuItem_Click(object sender, EventArgs e)
		{
			var indices = TraceView.SelectedIndices;

			if (indices.Count > 0)
			{
				var blob = new StringBuilder();
				foreach (int index in indices)
				{
					int pad = _instructions.Max(m => m.Disassembly.Length) + 4;
					blob.Append(_instructions[index].Disassembly.PadRight(pad))
						.Append(_instructions[index].RegisterInfo)
						.AppendLine();
				}
				Clipboard.SetDataObject(blob.ToString());
			}
		}

		private void SelectAllMenuItem_Click(object sender, EventArgs e)
		{
			for (var i = 0; i < _instructions.Count; i++)
			{
				TraceView.SelectItem(i, true);
			}
		}

		private void MaxLinesMenuItem_Click(object sender, EventArgs e)
		{
			var prompt = new InputPrompt
			{
				StartLocation = this.ChildPointToScreen(TraceView),
				TextInputType = InputPrompt.InputType.Unsigned,
				Message = "Max lines to display in the window",
				InitialValue = MaxLines.ToString()
			};

			var result = prompt.ShowHawkDialog();
			if (result == DialogResult.OK)
			{
				var max = int.Parse(prompt.PromptText);
				if (max > 0)
				{
					MaxLines = max;
				}
			}
		}

		#endregion

		#region Dialog and ListView Events

		private void LoggingEnabled_CheckedChanged(object sender, EventArgs e)
		{
			Tracer.Enabled = LoggingEnabled.Checked;
			SetTracerBoxTitle();
		}

		private void ClearButton_Click(object sender, EventArgs e)
		{
			ClearList();
		}

		private void BrowseBox_Click(object sender, EventArgs e)
		{
			var file = GetFileFromUser();
			if (file != null)
			{
				_logFile = file;
				FileBox.Text = _logFile.FullName;
			}
		}

		private void ToFileRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (ToFileRadio.Checked)
			{
				FileBox.Visible = true;
				BrowseBox.Visible = true;
				var name = PathManager.FilesystemSafeName(Global.Game);
				var filename = Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathEntries.LogPathFragment, null), name) + ".txt";
				_logFile = new FileInfo(filename);
				if (_logFile.Directory != null && !_logFile.Directory.Exists)
				{
					_logFile.Directory.Create();
				}

				if (_logFile.Exists)
				{
					_logFile.Delete();
				}
			
				using (_logFile.Create()) { }

				FileBox.Text = _logFile.FullName;
			}
			else
			{
				CloseFile();
				FileBox.Visible = false;
				BrowseBox.Visible = false;
			}

			SetTracerBoxTitle();
		}

		#endregion

		#endregion
	}
}
