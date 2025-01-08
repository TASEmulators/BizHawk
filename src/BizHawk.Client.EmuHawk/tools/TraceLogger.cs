using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
		public static Icon ToolIcon
			=> Properties.Resources.PencilIcon;

		private static readonly FilesystemFilterSet LogFilesFSFilterSet = new(
			new FilesystemFilter("Log Files", new[] { "log" }),
			FilesystemFilter.TextFiles);

		[RequiredService]
		public ITraceable _tracerCore { get; set; }

		private ITraceable Tracer
			=> _tracerCore!;

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
				if (value != null) { _baseName = Path.ChangeExtension(value.FullName, null); }
				else { _baseName = null; }
			}
		}

		private readonly List<TraceInfo> _instructions = new List<TraceInfo>();
		private StreamWriter _streamWriter;
		private bool _splitFile;
		private string _baseName;
		private readonly string _extension = ".log";
		private int _segmentCount;
		private ulong _currentSize;

		private const string DisasmColumnName = "Disasm";
		private const string RegistersColumnName = "Registers";

		protected override string WindowTitleStatic => "Trace Logger";

		public TraceLogger()
		{
			InitializeComponent();
			Icon = ToolIcon;
			SaveLogMenuItem.Image = Properties.Resources.SaveAs;

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
			TraceView.AllColumns.Add(new(name: DisasmColumnName, widthUnscaled: 239, text: DisasmColumnName));
			TraceView.AllColumns.Add(new(name: RegistersColumnName, widthUnscaled: 357, text: RegistersColumnName));
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

		public override void Restart()
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
			string initDir;
			string initFileName;
			if (LogFile == null)
			{
				initFileName = Game.FilesystemSafeName() + _extension;
				initDir = Config!.PathEntries.LogAbsolutePath();
			}
			else if (!string.IsNullOrWhiteSpace(LogFile.FullName))
			{
				initFileName = Game.FilesystemSafeName();
				initDir = Path.GetDirectoryName(LogFile.FullName) ?? string.Empty;
			}
			else
			{
				initFileName = Path.GetFileNameWithoutExtension(LogFile.FullName);
				initDir = Config!.PathEntries.LogAbsolutePath();
			}
			var result = this.ShowFileSaveDialog(
				discardCWDChange: true,
				filter: LogFilesFSFilterSet,
				initDir: initDir,
				initFileName: initFileName);
			return result is not null ? new FileInfo(result) : null;
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

		private void CopyMenuItem_Click(object sender, EventArgs e)
		{
			if (!TraceView.AnyRowsSelected) return;
			StringBuilder blob = new();
			foreach (var info in TraceView.SelectedRows.Select(index => _instructions[index]))
			{
				blob.AppendFormat("{0} {1}\n", info.Disassembly, info.RegisterInfo);
			}
			Clipboard.SetDataObject(blob.ToString());
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
			if (!this.ShowDialogWithTempMute(prompt).IsOk()) return;
			var max = int.Parse(prompt.PromptText);
			if (max > 0) MaxLines = max;
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
			if (!this.ShowDialogWithTempMute(prompt).IsOk()) return;
			FileSizeCap = int.Parse(prompt.PromptText);
			_splitFile = FileSizeCap != 0;
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
			LoggingEnabled.Text = LoggingEnabled.Checked ? "Stop &logging" : "Start &logging";
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
				var name = Game.FilesystemSafeName();
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
