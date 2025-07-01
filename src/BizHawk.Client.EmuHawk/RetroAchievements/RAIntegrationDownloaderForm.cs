using System.IO;

using BizHawk.Common;
using BizHawk.Common.PathExtensions;

namespace BizHawk.Client.EmuHawk
{
	public sealed class RAIntegrationDownloaderForm : DownloaderForm
	{
		protected override string ComponentName
			=> "RAIntegration";

		protected override string DownloadTemp { get; }
			= TempFileManager.GetTempFilename("RAIntegration_download", ".dll", delete: false);

		public RAIntegrationDownloaderForm(string downloadFrom)
		{
			Description = string.Empty;
			DownloadFrom = downloadFrom;
			DownloadTo = Path.Combine(PathUtils.DataDirectoryPath, "dll", "RA_Integration-x64.dll");
		}

		protected override Stream GetExtractionStream(HawkFile downloaded)
			=> downloaded.GetStream();
	}
}
