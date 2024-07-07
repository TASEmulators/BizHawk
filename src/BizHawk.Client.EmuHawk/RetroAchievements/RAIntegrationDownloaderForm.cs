using System.IO;
using System.Threading;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Common.PathExtensions;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Downloads RAIntegration (largely a copy paste of ffmpeg's downloader)
	/// </summary>
	public partial class RAIntegrationDownloaderForm : Form
	{
		public RAIntegrationDownloaderForm(string url)
		{
			_path = Path.Combine(PathUtils.DataDirectoryPath, "dll", "RA_Integration-x64.dll");
			_url = url;

			InitializeComponent();

			txtLocation.Text = _path;
			txtUrl.Text = _url;
		}

		private readonly string _path;
		private readonly string _url;

		private int _pct = 0;
		private bool _exiting = false;
		private bool _succeeded = false;
		private bool _failed = false;
		private Thread _thread;

		public bool DownloadSucceeded()
		{
			// block until the thread dies
			while (_thread?.IsAlive ?? false)
			{
				Thread.Sleep(1);
			}

			return _succeeded;
		}

		private void ThreadProc()
		{
			Download();
		}

		private void Download()
		{
			//the temp file is owned by this thread
			var fn = TempFileManager.GetTempFilename("RAIntegration_download", ".dll", false);

			try
			{
				using (var evt = new ManualResetEvent(false))
				{
					using var client = new System.Net.WebClient();
					System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
					client.DownloadFileAsync(new Uri(_url), fn);
					client.DownloadProgressChanged += (object sender, System.Net.DownloadProgressChangedEventArgs e) =>
					{
						_pct = e.ProgressPercentage;
					};
					client.DownloadFileCompleted += (object sender, System.ComponentModel.AsyncCompletedEventArgs e) =>
					{
							//we don't really need a status. we'll just try to unzip it when it's done
						evt.Set();
					};

					for (; ; )
					{
						if (evt.WaitOne(10))
							break;

						//if the gui thread ordered an exit, cancel the download and wait for it to acknowledge
						if (_exiting)
						{
							client.CancelAsync();
							evt.WaitOne();
							break;
						}
					}
				}

				//throw new Exception("test of download failure");

				//if we were ordered to exit, bail without wasting any more time
				if (_exiting)
					return;

				//try acquiring file
				using (var dll = new HawkFile(fn))
				{
					var data = dll!.ReadAllBytes();

					//last chance. exiting, don't dump the new RAIntegration file
					if (_exiting)
						return;

					DirectoryInfo parentDir = new(Path.GetDirectoryName(_path)!);
					if (!parentDir.Exists) parentDir.Create();
					if (File.Exists(_path)) File.Delete(_path);
					File.WriteAllBytes(_path, data);
				}

				_succeeded = true;
			}
			catch
			{
				_failed = true;
			}
			finally
			{
				try { File.Delete(fn); }
				catch { }
			}
		}

		private void btnDownload_Click(object sender, EventArgs e)
		{
			btnDownload.Text = "Downloading...";
			btnDownload.Enabled = false;
			_failed = false;
			_succeeded = false;
			_pct = 0;
			_thread = new Thread(ThreadProc);
			_thread.Start();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		protected override void OnClosed(EventArgs e)
		{
			//inform the worker thread that it needs to try terminating without doing anything else
			//(it will linger on in background for a bit til it can service this)
			_exiting = true;
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			//if it's done, close the window. the user will be smart enough to reopen it
			if (_succeeded)
				Close();
			if (_failed)
			{
				_failed = false;
				_pct = 0;
				btnDownload.Text = "FAILED - Download Again";
				btnDownload.Enabled = true;
			}
			progressBar1.Value = _pct;
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start(_url);
		}
	}
}

