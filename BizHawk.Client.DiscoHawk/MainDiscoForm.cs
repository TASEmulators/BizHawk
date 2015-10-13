using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Client.DiscoHawk
{
	public partial class MainDiscoForm : Form
	{
		//Release TODO:
		//An input (queue) list 
		//An outputted list showing new file name
		//Progress bar should show file being converted
		//Add disc button, which puts it on the progress cue (converts it)

		public MainDiscoForm()
		{
			InitializeComponent();
		}

		private void MainDiscoForm_Load(object sender, EventArgs e)
		{
			lvCompareTargets.Columns[0].Width = lvCompareTargets.ClientSize.Width;
		}

		private void ExitButton_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void lblMagicDragArea_DragDrop(object sender, DragEventArgs e)
		{
			List<string> files = validateDrop(e.Data);
			if (files.Count == 0) return;
			try
			{
				this.Cursor = Cursors.WaitCursor;
				foreach (var file in files)
				{
					var job = new DiscMountJob { IN_FromPath = file };
					job.Run();
					var disc = job.OUT_Disc;
					if (job.OUT_ErrorLevel)
					{
						System.Windows.Forms.MessageBox.Show(job.OUT_Log, "Error loading disc");
						break;
					}

					string baseName = Path.GetFileNameWithoutExtension(file);
					baseName += "_hawked";
					string outfile = Path.Combine(Path.GetDirectoryName(file), baseName) + ".ccd";
					CCD_Format.Dump(disc, outfile);
				}
				this.Cursor = Cursors.Default;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "Error loading disc");
				throw;
			}
		}

		//bool Dump(CueBin cueBin, string directoryTo, CueBinPrefs prefs)
		//{
		//  ProgressReport pr = new ProgressReport();
		//  Thread workThread = new Thread(() =>
		//  {
		//    cueBin.Dump(directoryTo, prefs, pr);
		//  });

		//  ProgressDialog pd = new ProgressDialog(pr);
		//  pd.Show(this);
		//  this.Enabled = false;
		//  workThread.Start();
		//  for (; ; )
		//  {
		//    Application.DoEvents();
		//    Thread.Sleep(10);
		//    if (workThread.ThreadState != ThreadState.Running)
		//      break;
		//    pd.Update();
		//  }
		//  this.Enabled = true;
		//  pd.Dispose();
		//  return !pr.CancelSignal;
		//}

		private void lblMagicDragArea_DragEnter(object sender, DragEventArgs e)
		{
			List<string> files = validateDrop(e.Data);
			if (files.Count > 0)
				e.Effect = DragDropEffects.Link;
			else e.Effect = DragDropEffects.None;
		}

		List<string> validateDrop(IDataObject ido)
		{
			List<string> ret = new List<string>();
			string[] files = (string[])ido.GetData(System.Windows.Forms.DataFormats.FileDrop);
			if (files == null) return new List<string>();
			foreach (string str in files)
			{
				string ext = Path.GetExtension(str).ToUpper();
				if(!ext.In(new string[]{".CUE",".ISO",".CCD"}))
				{
					return new List<string>();
				}
				ret.Add(str);
			}
			return ret;
		}

		private void lblMp3ExtractMagicArea_DragDrop(object sender, DragEventArgs e)
		{
			var files = validateDrop(e.Data);
			if (files.Count == 0) return;
			foreach (var file in files)
			{
				using (var disc = Disc.LoadAutomagic(file))
				{
					var path = Path.GetDirectoryName(file);
					var filename = Path.GetFileNameWithoutExtension(file);
					AudioExtractor.Extract(disc, path, filename);
				}
			}
		}

		private void btnAbout_Click(object sender, EventArgs e)
		{
			new About().ShowDialog();
		}

	}
}
