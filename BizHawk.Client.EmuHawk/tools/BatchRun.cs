using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using System.IO;

using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class BatchRun : Form
	{
		private Thread _thread;
		private List<BatchRunner.Result> _mostRecentResults;

		public BatchRun()
		{
			InitializeComponent();
		}

		private void listBox1_DragEnter(object sender, DragEventArgs e)
		{
			e.Set(DragDropEffects.Link);
		}

		private void SetCount()
		{
			label2.Text = $"Number of files: {listBox1.Items.Count}";
		}

		private void listBox1_DragDrop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				listBox1.Items.AddRange(files);
				SetCount();
			}
		}

		private void buttonClear_Click(object sender, EventArgs e)
		{
			listBox1.Items.Clear();
			SetCount();
		}

		private void buttonGo_Click(object sender, EventArgs e)
		{
			if (_thread != null)
			{
				MessageBox.Show("Old one still running!");
			}
			else
			{
				if (listBox1.Items.Count == 0)
				{
					MessageBox.Show("No files!");
				}
				else
				{
					label3.Text = "Status: Running...";
					int numFrames = (int)numericUpDownFrames.Value;
					List<string> files = new List<string>(listBox1.Items.Count);
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
				BatchRunner br = new BatchRunner(pp.Item2, pp.Item1);
				br.OnProgress += br_OnProgress;
				var results = br.Run();
				this.Invoke(() => { label3.Text = "Status: Finished!"; _mostRecentResults = results; });
			}
			catch (Exception e)
			{
				MessageBox.Show(e.ToString(), "The Whole Thing Died!");
				this.Invoke(() => label3.Text = "Deaded!");
			}
			this.Invoke(() => _thread = null);
		}

		void br_OnProgress(object sender, BatchRunner.ProgressEventArgs e)
		{
			this.Invoke(() => ProgressUpdate(e.Completed, e.Total));
			e.ShouldCancel = false;
		}

		private void BatchRun_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (_thread != null)
			{
				MessageBox.Show("Can't close while task is running!");
				e.Cancel = true;
			}
		}

		private void buttonDump_Click(object sender, EventArgs e)
		{
			if (_mostRecentResults != null)
			{
				using var sfd = new SaveFileDialog();
				var result = sfd.ShowDialog(this);
				if (result == DialogResult.OK)
				{
					using TextWriter tw = new StreamWriter(sfd.FileName);
					foreach (var r in _mostRecentResults)
					{
						r.DumpTo(tw);
					}
				}
			}
			else
			{
				MessageBox.Show("No results to save!");
			}
		}
	}
}
