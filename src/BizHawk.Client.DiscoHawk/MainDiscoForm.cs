using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Client.DiscoHawk
{
	public partial class MainDiscoForm : Form
	{
		// Release TODO:
		// An input (queue) list
		// An outputted list showing new file name
		// Progress bar should show file being converted
		// Add disc button, which puts it on the progress cue (converts it)
		public MainDiscoForm()
		{
			InitializeComponent();
			var icoStream = typeof(MainDiscoForm).Assembly.GetManifestResourceStream("BizHawk.Client.DiscoHawk.discohawk.ico");
			if (icoStream != null) Icon = new Icon(icoStream);
			else Console.WriteLine("couldn't load .ico EmbeddedResource?");
		}

		private void MainDiscoForm_Load(object sender, EventArgs e)
		{
			lvCompareTargets.Columns[0].Width = lvCompareTargets.ClientSize.Width;
		}

		private void ExitButton_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void lblMagicDragArea_DragDrop(object sender, DragEventArgs e)
		{
			lblMagicDragArea.AllowDrop = false;
			Cursor = Cursors.WaitCursor;
			var outputFormat = DiscoHawkLogic.HawkedFormats.CCD;
			if (ccdOutputButton.Checked) outputFormat = DiscoHawkLogic.HawkedFormats.CCD;
			if (chdOutputButton.Checked) outputFormat = DiscoHawkLogic.HawkedFormats.CHD;
			try
			{
				foreach (var file in ValidateDrop(e.Data))
				{
					var success = DiscoHawkLogic.HawkAndWriteFile(
						inputPath: file,
						errorCallback: err => MessageBox.Show(err, "Error loading disc"),
						hawkedFormat: outputFormat);
					if (!success) break;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "Error loading disc");
				throw;
			}
			finally
			{
				lblMagicDragArea.AllowDrop = true;
				Cursor = Cursors.Default;
			}
		}

#if false // API has changed
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
#endif

		private void LblMagicDragArea_DragEnter(object sender, DragEventArgs e)
		{
			var files = ValidateDrop(e.Data);
			e.Effect = files.Count > 0
				? DragDropEffects.Link
				: DragDropEffects.None;
		}

		private static List<string> ValidateDrop(IDataObject ido)
		{
			var ret = new List<string>();
			var files = (string[])ido.GetData(DataFormats.FileDrop);
			if (files == null) return new();
			foreach (var str in files)
			{
				var ext = Path.GetExtension(str) ?? string.Empty;
				if (!Disc.IsValidExtension(ext))
				{
					return new();
				}

				ret.Add(str);
			}

			return ret;
		}

		private void LblMp3ExtractMagicArea_DragDrop(object sender, DragEventArgs e)
		{
			if (!FFmpegService.QueryServiceAvailable())
			{
#if true
				MessageBox.Show(
					caption: "FFmpeg missing",
					text: "This function requires FFmpeg, but it doesn't appear to have been downloaded.\n"
						+ "EmuHawk can automatically download it: you just need to set up A/V recording with the FFmpeg writer.");
				return;
#else
				using EmuHawk.FFmpegDownloaderForm dialog = new(); // builds fine when <Compile Include/>'d, but the .resx won't load even if it's also included
				dialog.ShowDialog(owner: this);
				if (!FFmpegService.QueryServiceAvailable()) return;
#endif
			}
			lblMp3ExtractMagicArea.AllowDrop = false;
			Cursor = Cursors.WaitCursor;
			try
			{
				var files = ValidateDrop(e.Data);
				if (files.Count == 0) return;
				foreach (var file in files)
				{
					using var disc = Disc.LoadAutomagic(file);
					var (path, filename, _) = file.SplitPathToDirFileAndExt();
					static bool? PromptForOverwrite(string mp3Path)
						=> MessageBox.Show(
							$"Do you want to overwrite existing files? Choosing \"No\" will simply skip those. You could also \"Cancel\" the extraction entirely.\n\ncaused by file: {mp3Path}",
							"File to extract already exists",
							MessageBoxButtons.YesNoCancel) switch
						{
							DialogResult.Yes => true,
							DialogResult.No => false,
							_ => null
						};
					AudioExtractor.Extract(disc, path, filename, PromptForOverwrite);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "Error loading disc");
				throw;
			}
			finally
			{
				lblMp3ExtractMagicArea.AllowDrop = true;
				Cursor = Cursors.Default;
			}
		}

		private void BtnAbout_Click(object sender, EventArgs e)
		{
			new About().ShowDialog();
		}
	}
}
