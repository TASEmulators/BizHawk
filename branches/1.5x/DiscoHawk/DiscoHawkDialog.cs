using System;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using BizHawk.DiscSystem;

namespace BizHawk
{
	public partial class DiscoHawkDialog : Form
	{
		public DiscoHawkDialog()
		{
			InitializeComponent();
			PresetCanonical();
		}

		private class DiscRecord
		{
			public Disc Disc;
			public string BaseName;
		}

		private void btnAddDisc_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "CUE files (*.cue)|*.cue|ISO files (*.iso)|*.iso";
			ofd.CheckFileExists = true;
			ofd.CheckPathExists = true;
			ofd.Multiselect = false;
			if (ofd.ShowDialog() != DialogResult.OK)
				return;

			Disc disc = Disc.FromCuePath(ofd.FileName, new CueBinPrefs());

			string baseName = Path.GetFileName(ofd.FileName);
			ListViewItem lvi = new ListViewItem(baseName);
			DiscRecord dr = new DiscRecord();
			dr.Disc = disc;
			dr.BaseName = baseName;
			lvi.Tag = dr;
			lvDiscs.SelectedIndices.Clear();
			lvDiscs.Items.Add(lvi);
			lvDiscs.Items[lvDiscs.Items.Count-1].Selected = true;
		}

		private void lvDiscs_SelectedIndexChanged(object sender, EventArgs e)
		{
			UnbindDisc();
			if (lvDiscs.SelectedIndices.Count != 0)
			{
				DiscRecord dr = (DiscRecord) lvDiscs.SelectedItems[0].Tag;
				BindDisc(dr);
			}
		}

		void UnbindDisc()
		{
			btnExportCue.Enabled = false;
			lblSessions.Text = "";
			lblTracks.Text = "";
			lblSectors.Text = "";
			lblSize.Text = "";
		}

		Disc boundDisc;
		DiscRecord boundDiscRecord;
		void BindDisc(DiscRecord discRecord)
		{
			Disc disc = discRecord.Disc;
			boundDiscRecord = discRecord;

			DiscTOC toc = disc.ReadTOC();
			boundDisc = disc;
			lblSessions.Text = toc.Sessions.Count.ToString();
			lblTracks.Text = toc.Sessions.Sum((ses) => ses.Tracks.Count).ToString();
			lblSectors.Text = string.Format("{0} ({1})", toc.length_aba, toc.FriendlyLength.Value);
			lblSize.Text = string.Format("{0:0.00} MB", toc.BinarySize / 1024.0 / 1024.0);
			btnExportCue.Enabled = true;
			UpdateCue();
		}

		void UpdateCue()
		{
			if (boundDisc == null)
			{
				txtCuePreview.Text = "";
				return;
			}

			var cueBin = boundDisc.DumpCueBin(boundDiscRecord.BaseName, GetCuePrefs());
			txtCuePreview.Text = cueBin.cue.Replace("\n", "\r\n"); ;
		}

		CueBinPrefs GetCuePrefs()
		{
			var prefs = new CueBinPrefs();
			prefs.AnnotateCue = checkCueProp_Annotations.Checked;
			prefs.OneBlobPerTrack = checkCueProp_OneBlobPerTrack.Checked;
			prefs.ReallyDumpBin = false;
			prefs.SingleSession = true;
			return prefs;
		}

		private void btnPresetCanonical_Click(object sender, EventArgs e)
		{
			PresetCanonical();
		}
		void PresetCanonical()
		{
			checkCueProp_Annotations.Checked = false;
			checkCueProp_OneBlobPerTrack.Checked = false;
		}

		private void btnPresetDaemonTools_Click(object sender, EventArgs e)
		{
			PresetDaemonTools();
		}
		void PresetDaemonTools()
		{
			checkCueProp_Annotations.Checked = false;
		}

		private void checkCueProp_CheckedChanged(object sender, EventArgs e)
		{
			UpdateCue();
		}

		private void Form1_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
		{
			e.Action = DragAction.Continue;
		}

		private void handleDragEnter(object sender, DragEventArgs e)
		{
			List<string> files = validateDrop(e.Data);
			if (files.Count > 0)
				e.Effect = DragDropEffects.Link;
			else e.Effect = DragDropEffects.None;
		}

		private void handleDragDrop(object sender, DragEventArgs e)
		{
			List<string> files = validateDrop(e.Data);
			if (files.Count == 0) return;
			try
			{
				foreach (var file in files)
				{
					Disc disc = Disc.FromCuePath(file, new CueBinPrefs());
					string baseName = Path.GetFileNameWithoutExtension(file);
					baseName += "_hawked";
					var prefs = GetCuePrefs();
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

		List<string> validateDrop(IDataObject ido)
		{
			List<string> ret = new List<string>();
			string[] files = (string[])ido.GetData(System.Windows.Forms.DataFormats.FileDrop);
			if (files == null) return new List<string>();
			foreach (string str in files)
			{
				if (Path.GetExtension(str).ToUpper() != ".CUE")
				{
					return new List<string>();
				}
				ret.Add(str);
			}
			return ret;
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

		private void btnExportCue_Click(object sender, EventArgs e)
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "CUE files (*.cue)|*.cue";
			sfd.OverwritePrompt = true;
			if (sfd.ShowDialog() != DialogResult.OK)
				return;
			string baseName = Path.GetFileNameWithoutExtension(sfd.FileName);
			var prefs = GetCuePrefs();
			prefs.ReallyDumpBin = true;
			var cueBin = boundDisc.DumpCueBin(baseName, prefs);

			Dump(cueBin, Path.GetDirectoryName(sfd.FileName), prefs);
		}

		private void lblMagicDragArea_Click(object sender, EventArgs e)
		{

		}

	}
}
