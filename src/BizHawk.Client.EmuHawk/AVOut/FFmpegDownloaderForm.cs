using System.IO;

using BizHawk.Common;
using BizHawk.Common.IOExtensions;

namespace BizHawk.Client.EmuHawk
{
	public sealed class FFmpegDownloaderForm : DownloaderForm
	{
		protected override string ComponentName
			=> "FFmpeg";

		protected override string DownloadTemp { get; }
			= TempFileManager.GetTempFilename("ffmpeg_download", ".7z", delete: false);

		public FFmpegDownloaderForm()
		{
			Description = "BizHawk relies on a specific version of FFmpeg. No other version will do. The wrong version will be ignored. There is no way to override this behavior."
				+ "\n\nThe required version could not be found."
				+ (OSTailoredCode.IsUnixHost
					? "\n\n(Linux user: If installing manually, you can use a symlink.)"
					: "\n\nUse this dialog to download it automatically, or download it yourself from the URL below and place it in the specified location.");
			DownloadFrom = FFmpegService.Url;
			DownloadTo = FFmpegService.FFmpegPath;
		}

		protected override Stream GetExtractionStream(HawkFile downloaded)
			=> (OSTailoredCode.IsUnixHost
				? downloaded.BindArchiveMember("ffmpeg")!
				: downloaded.BindFirstOf(".exe")).GetStream();

		protected override bool PostChmodCheck()
			=> FFmpegService.QueryServiceAvailable();

		protected override bool PreChmodCheck(FileStream extracted)
			=> SHA256Checksum.ComputeDigestHex(extracted.ReadAllBytes()) == FFmpegService.DownloadSHA256Checksum;
	}
}
