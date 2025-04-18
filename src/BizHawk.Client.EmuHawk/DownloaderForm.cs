using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class DownloaderForm : Form
	{
		protected virtual string ComponentName { get; } = null!;

		public string Description
		{
			get => textBox1.Text;
			init => textBox1.Text = value;
		}

		public string DownloadFrom
		{
			get => txtUrl.Text;
			init => txtUrl.Text = value;
		}

		protected virtual string DownloadTemp { get; } = null!;

		public string DownloadTo
		{
			get => txtLocation.Text;
			init => txtLocation.Text = value;
		}

		public DownloaderForm()
		{
			InitializeComponent();
			Load += (_, _) => Text = $"Download {ComponentName ?? "COMPONENT UNSET"}";
		}

		private int _pct = 0;
		private bool _exiting = false;
		private bool _succeeded = false;
		private bool _failed = false;

		private Thread/*?*/ _thread = null;

		public bool DownloadSucceeded()
		{
			// block until the thread dies
			while (_thread is { IsAlive: true })
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
			try
			{
				DirectoryInfo parentDir = new(Path.GetDirectoryName(DownloadTo)!);
				if (!parentDir.Exists) parentDir.Create();
				// check writable before bothering with the download
				if (File.Exists(DownloadTo)) File.Delete(DownloadTo);
				using var fs = File.Create(DownloadTo);
				using (var evt = new ManualResetEvent(false))
				{
					using var client = new WebClient();
					ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
					client.DownloadFileAsync(new Uri(DownloadFrom), DownloadTemp);
					client.DownloadProgressChanged += (_, progressArgs) => _pct = progressArgs.ProgressPercentage;
					client.DownloadFileCompleted += (_, _) => evt.Set(); // we don't really need a status, we'll just try to unzip it when it's done

					while (true)
					{
						if (evt.WaitOne(10)) break;

						//if the gui thread ordered an exit, cancel the download and wait for it to acknowledge
						if (_exiting)
						{
							client.CancelAsync();
							evt.WaitOne();
							break;
						}
					}
				}

//				throw new Exception("test of download failure");

				//if we were ordered to exit, bail without wasting any more time
				if (_exiting) return;

				//try acquiring file
				using (HawkFile hf = new(DownloadTemp))
				{
					using var exStream = GetExtractionStream(hf);
					//last chance. exiting, don't dump the new file
					if (_exiting) return;
					exStream.CopyTo(fs);
					fs.Position = 0L;
					if (!PreChmodCheck(fs)) throw new Exception("download failed (pre-chmod validation)");
					fs.Dispose();
					if (OSTailoredCode.IsUnixHost)
					{
						OSTailoredCode.ConstructSubshell("chmod", $"+x {DownloadTo}", checkStdout: false).Start();
						Thread.Sleep(50); // Linux I/O flush idk
					}
				}

				//make sure it worked
				if (!PostChmodCheck()) throw new Exception("download failed (post-chmod validation)");

				_succeeded = true;
			}
			catch (Exception e)
			{
				_failed = true;
				Util.DebugWriteLine($"{ComponentName} download failed with:\n{e}");
			}
			finally
			{
				try
				{
					File.Delete(DownloadTemp);
				}
				catch
				{
					// ignore
				}
			}
		}

		protected virtual Stream GetExtractionStream(HawkFile downloaded)
			=> throw new NotImplementedException();

		protected virtual bool PostChmodCheck()
			=> true;

		protected virtual bool PreChmodCheck(FileStream extracted)
			=> true;

		private void btnDownload_Click(object sender, EventArgs e)
		{
			btnDownload.Text = "Downloading...";
			btnDownload.Enabled = false;
			_failed = false;
			_succeeded = false;
			_pct = 0;
			_thread = new(ThreadProc);
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
			if (_succeeded) Close();
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
			Process.Start(DownloadFrom);
		}
	}
}
