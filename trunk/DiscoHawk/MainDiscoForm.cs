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

using BizHawk.DiscSystem;

namespace BizHawk
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

		private class DiscRecord
		{
			public Disc Disc;
			public string BaseName;
		}

		private void MainDiscoForm_Load(object sender, EventArgs e)
		{

		}

		private void ExitButton_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		CueBinPrefs GetCuePrefs()
		{
			var prefs = new DiscSystem.CueBinPrefs();
			prefs.AnnotateCue = true; // TODO? checkCueProp_Annotations.Checked;
			prefs.OneBlobPerTrack = false; //TODO? checkCueProp_OneBlobPerTrack.Checked;
			prefs.ReallyDumpBin = false;
			prefs.SingleSession = true;
			prefs.ExtensionAware = true;
			return prefs;
		}

		private void lblMagicDragArea_DragDrop(object sender, DragEventArgs e)
		{
			List<string> files = validateDrop(e.Data);
			if (files.Count == 0) return;
			try
			{
				foreach (var file in files)
				{
					var prefs = GetCuePrefs();
					string ext = Path.GetExtension(file).ToUpper();
					Disc disc = null;
					if (ext == ".ISO")
						disc = Disc.FromIsoPath(file);
					else if(ext == ".CUE")
						disc = Disc.FromCuePath(file, prefs);
					string baseName = Path.GetFileNameWithoutExtension(file);
					baseName += "_hawked";
					prefs.ReallyDumpBin = true;
					var cueBin = disc.DumpCueBin(baseName, GetCuePrefs());
					Dump(cueBin, Path.GetDirectoryName(file), prefs);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "oops! error");
				throw;
			}
		}

		bool Dump(CueBin cueBin, string directoryTo, CueBinPrefs prefs)
		{
			ProgressReport pr = new ProgressReport();
			Thread workThread = new Thread(() =>
			{
				cueBin.Dump(directoryTo, prefs, pr);
			});

			ProgressDialog pd = new ProgressDialog(pr);
			pd.Show(this);
			this.Enabled = false;
			workThread.Start();
			for (; ; )
			{
				Application.DoEvents();
				Thread.Sleep(10);
				if (workThread.ThreadState != ThreadState.Running)
					break;
				pd.Update();
			}
			this.Enabled = true;
			pd.Dispose();
			return !pr.CancelSignal;
		}

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
				if (Path.GetExtension(str).ToUpper() != ".CUE" && Path.GetExtension(str).ToUpper() != ".ISO")
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
				using (var disc = Disc.FromCuePath(file, new CueBinPrefs()))
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
