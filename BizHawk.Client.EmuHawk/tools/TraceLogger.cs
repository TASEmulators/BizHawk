using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		[ConfigPersist]
		private int FileSizeCap { get; set; }

		[ConfigPersist]
		private int DisasmColumnWidth { 
			get { return this.Disasm.Width; }
			set { this.Disasm.Width = value; }
		}

		[ConfigPersist]
		private int RegistersColumnWidth
		{
			get { return this.Registers.Width; }
			set { this.Registers.Width = value; }
		}

		private FileInfo _logFile;
		private FileInfo LogFile
		{
			get { return _logFile; }
			set
			{
				_logFile = value;
				_baseName = Path.ChangeExtension(value.FullName, null);
			}
		}

		private List<TraceInfo> _instructions = new List<TraceInfo>();
		private StreamWriter _streamWriter;
		private bool _splitFile;
		private string _baseName;
		private string _extension = ".log";
		private int _segmentCount;
		private ulong _currentSize;

		public TraceLogger()
		{
			InitializeComponent();

			TraceView.QueryItemText += TraceView_QueryItemText;
			TraceView.VirtualMode = true;

			Closing += (o, e) => SaveConfigSettings();

			MaxLines = 10000;
			FileSizeCap = 150; // make 1 frame of tracelog for n64/psx fit in
			_splitFile = FileSizeCap != 0;
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
			//Tracer.Enabled = LoggingEnabled.Checked;
		}

		private void TraceView_QueryItemText(int index, int column, out string text)
		{
			text = string.Empty;
			if (index < _instructions.Count)
			{
				switch (column)
				{
					case 0:
						text = _instructions[index].Disassembly.TrimEnd();
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
			OpenLogFile.Enabled = false;
			LoggingEnabled.Checked = false;
			Tracer.Sink = null;
			SetTracerBoxTitle();
		}

		class CallbackSink : ITraceSink
		{
			public void Put(TraceInfo info)
			{
				putter(info);
			}

			public Action<TraceInfo> putter;
		}

		public void UpdateValues() { }

		public void NewUpdate(ToolFormUpdateType type)
		{
			if (type == ToolFormUpdateType.PostFrame)
			{
				if (ToWindowRadio.Checked)
				{
					TraceView.VirtualListSize = _instructions.Count;
				}
				else
				{
					CloseFile();
				}
			}

			if (type == ToolFormUpdateType.PreFrame)
			{
				if (LoggingEnabled.Checked)
				{
					//connect tracer to sink for next frame
					if (ToWindowRadio.Checked)
					{
						//update listview with most recentr results
						TraceView.BlazingFast = !GlobalWin.MainForm.EmulatorPaused;

						Tracer.Sink = new CallbackSink()
						{
							putter = (info) =>
							{
								if (_instructions.Count >= MaxLines)
								{
									_instructions.RemoveRange(0, _instructions.Count - MaxLines);
								}
								_instructions.Add(info);
							}
						};
						_instructions.Clear();
					}
					else
					{
						if (_streamWriter == null)
						{
							StartLogFile(true);
						}
						Tracer.Sink = new CallbackSink {
							putter = (info) =>
							{
								//no padding supported. core should be doing this!
								var data = string.Format("{0} {1}", info.Disassembly, info.RegisterInfo);
								_streamWriter.WriteLine(data);
								_currentSize += (ulong)data.Length;
								if (_splitFile)
									CheckSplitFile();
							}
						};
					}
				}
				else Tracer.Sink = null;
			}
		}

		public void FastUpdate()
		{
		}

		public void Restart()
		{
			CloseFile();
			ClearList();
			LoggingEnabled.Checked = false;
			ToFileRadio.Checked = false;
			ToWindowRadio.Checked = true;
			OpenLogFile.Enabled = false;
			Tracer.Sink = null;
			SetTracerBoxTitle();
		}

		private void ClearList()
		{
			_instructions.Clear();
			TraceView.ItemCount = 0;
			SetTracerBoxTitle();
		}

		private void DumpToDisk()
		{
			foreach (var instruction in _instructions)
			{
				//no padding supported. core should be doing this!
				var data = string.Format("{0} {1}", instruction.Disassembly, instruction.RegisterInfo);
				_streamWriter.WriteLine(data);
				_currentSize += (ulong)data.Length;
				if (_splitFile)
					CheckSplitFile();
			}
		}

		private void LogToWindow()
		{
			if (_instructions.Count >= MaxLines)
			{
				_instructions.RemoveRange(0, _instructions.Count - MaxLines);
			}
			TraceView.ItemCount = _instructions.Count;
		}

		private void SetTracerBoxTitle()
		{
			if (LoggingEnabled.Checked)
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
			if (_streamWriter != null)
			{
				_streamWriter.Close();
				_streamWriter = null;
			}
		}

		private FileInfo GetFileFromUser()
		{
			var sfd = new SaveFileDialog();
			if (LogFile == null)
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game) + _extension;
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.LogPathFragment, null);
			}
			else if (!string.IsNullOrWhiteSpace(LogFile.FullName))
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
				sfd.InitialDirectory = Path.GetDirectoryName(LogFile.FullName);
			}
			else
			{
				sfd.FileName = Path.GetFileNameWithoutExtension(LogFile.FullName);
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.LogPathFragment, null);
			}

			sfd.Filter = "Log Files (*.log)|*.log|Text Files (*.txt)|*.txt|All Files|*.*";
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
			LogFile = GetFileFromUser();
			if (LogFile != null)
			{
				StartLogFile();
				DumpToDisk();
				GlobalWin.OSD.AddMessage("Log dumped to " + LogFile.FullName);
				CloseFile();
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
					blob.Append(string.Format("{0} {1}\n",
						_instructions[index].Disassembly,
						_instructions[index].RegisterInfo));
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

		private void SegmentSizeMenuItem_Click(object sender, EventArgs e)
		{
			var prompt = new InputPrompt
			{
				StartLocation = this.ChildPointToScreen(TraceView),
				TextInputType = InputPrompt.InputType.Unsigned,
				Message = "Log file segment size in megabytes\nSetting 0 disables segmentation",
				InitialValue = FileSizeCap.ToString()
			};

			var result = prompt.ShowHawkDialog();
			if (result == DialogResult.OK)
			{
				FileSizeCap = int.Parse(prompt.PromptText);
				_splitFile = FileSizeCap != 0;
			}
		}

		#endregion

		#region Dialog and ListView Events

		private void LoggingEnabled_CheckedChanged(object sender, EventArgs e)
		{
			//Tracer.Enabled = LoggingEnabled.Checked;
			SetTracerBoxTitle();

			if (LogFile != null && ToFileRadio.Checked)
			{
				OpenLogFile.Enabled = true;
				if (LoggingEnabled.Checked)
				{
					StartLogFile();
				}
				else
				{
					CloseFile();
				}
			}
		}

		private void StartLogFile(bool append = false)
		{
			var data = Tracer.Header;
			var segment = _segmentCount > 0 ? "_" + _segmentCount.ToString() : "";
			_streamWriter = new StreamWriter(_baseName + segment + _extension, append);
			_streamWriter.WriteLine(data);
			if (append)
			{
				_currentSize += (ulong)data.Length;
			}
			else
			{
				_currentSize = (ulong)data.Length;
			}
		}

		private void CheckSplitFile()
		{
			if (_currentSize / 1024 / 1024 >= (ulong)FileSizeCap)
			{
				_segmentCount++;
				CloseFile();
				StartLogFile();
			}
		}

		private void BrowseBox_Click(object sender, EventArgs e)
		{
			var file = GetFileFromUser();
			if (file != null)
			{
				LogFile = file;
				FileBox.Text = LogFile.FullName;
				_segmentCount = 0;
			}
		}

		private void ToFileRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (ToFileRadio.Checked)
			{
				FileBox.Visible = true;
				BrowseBox.Visible = true;
				var name = PathManager.FilesystemSafeName(Global.Game);
				var filename = Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathEntries.LogPathFragment, null), name) + _extension;
				LogFile = new FileInfo(filename);
				if (LogFile.Directory != null && !LogFile.Directory.Exists)
				{
					LogFile.Directory.Create();
				}

				// never delete, especially from ticking checkboxes
				// append = false is enough, and even that only happens when actually enabling logging
				//if (LogFile.Exists)
				//{
				//	LogFile.Delete();
				//}

				//using (LogFile.Create()) { } // created automatically

				FileBox.Text = LogFile.FullName;

				if (LoggingEnabled.Checked && LogFile != null)
				{
					StartLogFile();
					OpenLogFile.Enabled = true;
				}
			}
			else
			{
				_currentSize = 0;
				_segmentCount = 0;
				CloseFile();
				FileBox.Visible = false;
				BrowseBox.Visible = false;
				OpenLogFile.Enabled = false;
			}

			SetTracerBoxTitle();
		}

		#endregion

		#endregion

		private void ClearMenuItem_Click(object sender, EventArgs e)
		{
			ClearList();
		}

		private void OpenLogFile_Click(object sender, EventArgs e)
		{
			if (LogFile != null)
			{
				Process.Start(LogFile.FullName);
			}
		}
	}
}
