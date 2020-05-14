using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TraceLogger : ToolFormBase, IToolFormAutoConfig
	{
		[RequiredService]
		private ITraceable Tracer { get; set; }

		[ConfigPersist]
		private int MaxLines { get; set; }

		[ConfigPersist]
		private int FileSizeCap { get; set; }

		[ConfigPersist]
		private List<RollColumn> Columns
		{
			get => TraceView.AllColumns;
			set
			{
				TraceView.AllColumns.Clear();
				foreach (var column in value)
				{
					TraceView.AllColumns.Add(column);
				}

				TraceView.AllColumns.ColumnsChanged();
			}
		}

		private FileInfo _logFile;
		private FileInfo LogFile
		{
			get => _logFile;
			set
			{
				_logFile = value;
				_baseName = Path.ChangeExtension(value.FullName, null);
			}
		}

		private readonly List<TraceInfo> _instructions = new List<TraceInfo>();
		private StreamWriter _streamWriter;
		private bool _splitFile;
		private string _baseName;
		private string _extension = ".log";
		private int _segmentCount;
		private ulong _currentSize;

		private const string DisasmColumnName = "Disasm";
		private const string RegistersColumnName = "Registers";
		public TraceLogger()
		{
			InitializeComponent();

			TraceView.QueryItemText += TraceView_QueryItemText;

			Closing += (o, e) =>
			{
				SaveConfigSettings();
				Tracer.Sink = null;
				CloseFile();
			};

			MaxLines = 10000;
			FileSizeCap = 150; // make 1 frame of trace log for n64/psx fit in
			_splitFile = FileSizeCap != 0;

			TraceView.AllColumns.Clear();
			TraceView.AllColumns.Add(new RollColumn
			{
				Name = DisasmColumnName,
				Text = DisasmColumnName,
				UnscaledWidth = 239,
				Type = ColumnType.Text
			});
			TraceView.AllColumns.Add(new RollColumn
			{
				Name = RegistersColumnName,
				Text = RegistersColumnName,
				UnscaledWidth = 357,
				Type = ColumnType.Text
			});
		}

		private void SaveConfigSettings()
		{
			//Tracer.Enabled = LoggingEnabled.Checked;
		}

		private void TraceView_QueryItemText(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			text = "";
			if (index < _instructions.Count)
			{
				text = column.Name switch
				{
					DisasmColumnName => _instructions[index].Disassembly.TrimEnd(),
					RegistersColumnName => _instructions[index].RegisterInfo,
					_ => text
				};
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

		private class CallbackSink : ITraceSink
		{
			public void Put(TraceInfo info)
			{
				Putter(info);
			}

			public Action<TraceInfo> Putter { get; set; }
		}

		public override void UpdateValues(ToolFormUpdateType type)
		{
			if (type == ToolFormUpdateType.PostFrame)
			{
				if (ToWindowRadio.Checked)
				{
					TraceView.RowCount = _instructions.Count;
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
						Tracer.Sink = new CallbackSink
						{
							Putter = info =>
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
							Putter = (info) =>
							{
								//no padding supported. core should be doing this!
								var data = $"{info.Disassembly} {info.RegisterInfo}";
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
			TraceView.RowCount = 0;
			SetTracerBoxTitle();
		}

		private void DumpToDisk()
		{
			foreach (var instruction in _instructions)
			{
				//no padding supported. core should be doing this!
				var data = $"{instruction.Disassembly} {instruction.RegisterInfo}";
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
			TraceView.RowCount = _instructions.Count;
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
					TracerBox.Text = $"Trace log - logging - {_instructions.Count} instructions";
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
					TracerBox.Text = $"Trace log - {_instructions.Count} instructions";
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
			using var sfd = new SaveFileDialog();
			if (LogFile == null)
			{
				sfd.FileName = Global.Game.FilesystemSafeName() + _extension;
				sfd.InitialDirectory = Config.PathEntries.LogAbsolutePath();
			}
			else if (!string.IsNullOrWhiteSpace(LogFile.FullName))
			{
				sfd.FileName = Global.Game.FilesystemSafeName();
				sfd.InitialDirectory = Path.GetDirectoryName(LogFile.FullName);
			}
			else
			{
				sfd.FileName = Path.GetFileNameWithoutExtension(LogFile.FullName);
				sfd.InitialDirectory = Config.PathEntries.LogAbsolutePath();
			}

			sfd.Filter = new FilesystemFilterSet(
				new FilesystemFilter("Log Files", new[] { "log" }),
				FilesystemFilter.TextFiles
			).ToString();
			sfd.RestoreDirectory = true;
			var result = sfd.ShowHawkDialog();
			return result.IsOk() ? new FileInfo(sfd.FileName) : null;
		}





		private void SaveLogMenuItem_Click(object sender, EventArgs e)
		{
			LogFile = GetFileFromUser();
			if (LogFile != null)
			{
				StartLogFile();
				DumpToDisk();
				MainForm.AddOnScreenMessage($"Log dumped to {LogFile.FullName}");
				CloseFile();
			}
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void CopyMenuItem_Click(object sender, EventArgs e)
		{
			var indices = TraceView.SelectedRows.ToList();

			if (indices.Count > 0)
			{
				var blob = new StringBuilder();
				foreach (int index in indices)
				{
					blob.Append($"{_instructions[index].Disassembly} {_instructions[index].RegisterInfo}\n");
				}
				Clipboard.SetDataObject(blob.ToString());
			}
		}

		private void SelectAllMenuItem_Click(object sender, EventArgs e)
		{
			for (var i = 0; i < _instructions.Count; i++)
			{
				TraceView.SelectRow(i, true);
			}
		}

		private void MaxLinesMenuItem_Click(object sender, EventArgs e)
		{
			using var prompt = new InputPrompt
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
			using var prompt = new InputPrompt
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
			_streamWriter = new StreamWriter(
				string.Concat(_baseName, _segmentCount == 0 ? string.Empty : $"_{_segmentCount}", _extension),
				append);
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
				var name = Global.Game.FilesystemSafeName();
				var filename = Path.Combine(Config.PathEntries.LogAbsolutePath(), name) + _extension;
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
