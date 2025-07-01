using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;

using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Downloads FFmpeg
	/// </summary>
	public partial class FFmpegDownloaderForm : Form
	{
		public FFmpegDownloaderForm()
		{
			_path = FFmpegService.FFmpegPath;
			_url = FFmpegService.Url;

			InitializeComponent();

			txtLocation.Text = _path;
			txtUrl.Text = _url;

			if (OSTailoredCode.IsUnixHost) textBox1.Text = string.Join("\n", textBox1.Text.Split('\n').Take(3)) + "\n\n(Linux user: If installing manually, you can use a symlink.)";
		}

		private readonly string _path;
		private readonly string _url;

		private int _pct = 0;
		private bool _exiting = false;
		private bool _succeeded = false;
		private bool _failed = false;

		private void ThreadProc()
		{
			Download();
		}

		private void Download()
		{
			//the temp file is owned by this thread
			var fn = TempFileManager.GetTempFilename("ffmpeg_download", ".7z", false);

			try
			{
				DirectoryInfo parentDir = new(Path.GetDirectoryName(_path)!);
				if (!parentDir.Exists) parentDir.Create();
				// check writable before bothering with the download
				if (File.Exists(_path)) File.Delete(_path);
				using var fs = File.Create(_path);
				using (var evt = new ManualResetEvent(false))
				{
					using var client = new WebClient();
					ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
					client.DownloadFileAsync(new Uri(_url), fn);
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
				using (var hf = new HawkFile(fn))
				{
					using (var exe = OSTailoredCode.IsUnixHost ? hf.BindArchiveMember("ffmpeg") : hf.BindFirstOf(".exe"))
					{
						//last chance. exiting, don't dump the new ffmpeg file
						if (_exiting) return;
						exe!.GetStream().CopyTo(fs);
						fs.Dispose();
						if (OSTailoredCode.IsUnixHost)
						{
							OSTailoredCode.ConstructSubshell("chmod", $"+x {_path}", checkStdout: false).Start();
							Thread.Sleep(50); // Linux I/O flush idk
						}
					}
				}

				//make sure it worked
				if (!FFmpegService.QueryServiceAvailable()) throw new Exception("download failed");

				_succeeded = true;
			}
			catch (Exception e)
			{
				_failed = true;
				Util.DebugWriteLine($"FFmpeg download failed with:\n{e}");
			}
			finally
			{
				try
				{
					File.Delete(fn);
				}
				catch
				{
					// ignore
				}
			}
		}

		private void btnDownload_Click(object sender, EventArgs e)
		{
			btnDownload.Text = "Downloading...";
			btnDownload.Enabled = false;
			_failed = false;
			_succeeded = false;
			_pct = 0;
			var t = new Thread(ThreadProc);
			t.Start();
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
			Util.OpenUrlExternal(_url);
		}
	}
}
