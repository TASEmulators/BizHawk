using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class BatchRun : Form, IDialogParent
	{
		private readonly Config _config;

		private readonly Func<CoreComm> _createCoreComm;

		private List<BatchRunner.Result> _mostRecentResults;

		private Thread _thread;

		public IDialogController DialogController { get; }

		public BatchRun(IDialogController dialogController, Config config, Func<CoreComm> createCoreComm)
		{
			_config = config;
			_createCoreComm = createCoreComm;
			DialogController = dialogController;
			InitializeComponent();
		}

		private void ListBox1_DragEnter(object sender, DragEventArgs e)
		{
			e.Set(DragDropEffects.Link);
		}

		private void SetCount()
		{
			label2.Text = $"Number of files: {listBox1.Items.Count}";
		}

		private void ListBox1_DragDrop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				listBox1.Items.AddRange(files.Cast<object>().ToArray());
				SetCount();
			}
		}

		private void ButtonClear_Click(object sender, EventArgs e)
		{
			listBox1.Items.Clear();
			SetCount();
		}

		private void ButtonGo_Click(object sender, EventArgs e)
		{
			if (_thread != null)
			{
				DialogController.ShowMessageBox("Old one still running!");
			}
			else
			{
				if (listBox1.Items.Count == 0)
				{
					DialogController.ShowMessageBox("No files!");
				}
				else
				{
					label3.Text = "Status: Running...";
					int numFrames = (int)numericUpDownFrames.Value;
					var files = new List<string>(listBox1.Items.Count);
					foreach (string s in listBox1.Items)
					{
						files.Add(s);
					}

					_thread = new Thread(ThreadProc);
					_thread.Start(new Tuple<int, List<string>>(numFrames, files));
				}
			}
		}

		private void ProgressUpdate(int curr, int max)
		{
			progressBar1.Maximum = max;
			progressBar1.Value = curr;
		}

		private void ThreadProc(object o)
		{
			try
			{
				var pp = (Tuple<int, List<string>>)o;
				BatchRunner br = new BatchRunner(_config, _createCoreComm(), pp.Item2, pp.Item1);
				br.OnProgress += BrOnProgress;
				var results = br.Run();
				this.Invoke(() => { label3.Text = "Status: Finished!"; _mostRecentResults = results; });
			}
			catch (Exception e)
			{
				DialogController.ShowMessageBox(e.ToString(), "The Whole Thing Died!");
				this.Invoke(() => label3.Text = "Deaded!");
			}
			this.Invoke(() => _thread = null);
		}

		private void BrOnProgress(object sender, BatchRunner.ProgressEventArgs e)
		{
			this.Invoke(() => ProgressUpdate(e.Completed, e.Total));
			e.ShouldCancel = false;
		}

		private void BatchRun_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (_thread != null)
			{
				DialogController.ShowMessageBox("Can't close while task is running!");
				e.Cancel = true;
			}
		}

		private void ButtonDump_Click(object sender, EventArgs e)
		{
			if (_mostRecentResults is null)
			{
				DialogController.ShowMessageBox("No results to save!");
				return;
			}
			var result = this.ShowFileSaveDialog(initDir: _config.PathEntries.ToolsAbsolutePath());
			if (result is null) return;
			using TextWriter tw = new StreamWriter(result);
			foreach (var r in _mostRecentResults)
			{
				r.DumpTo(tw);
			}
		}
	}
}
