using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Newtonsoft.Json;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class RetroAchievements
	{
		private static DynamicLibraryImportResolver _resolver;
		private static Version _version;

		private static void AttachDll()
		{
			_resolver = new("RA_Integration-x64.dll", hasLimitedLifetime: true);
			RA = BizInvoker.GetInvoker<RAInterface>(_resolver, CallingConventionAdapters.Native);
			_version = new(Marshal.PtrToStringAnsi(RA.IntegrationVersion()));
			Console.WriteLine($"Loaded RetroAchievements v{_version}");
		}

		private static void DetachDll()
		{
			RA?.Shutdown();
			_resolver?.Dispose();
			_resolver = null;
			RA = null;
			_version = new(0, 0);
		}

		private static bool DownloadDll(string url)
		{
			if (url.StartsWith("http:"))
			{
				// force https
				url = url.Replace("http:", "https:");
			}

			using var downloadForm = new RAIntegrationDownloaderForm(url);
			downloadForm.ShowDialog();
			return downloadForm.DownloadSucceeded();
		}

		public static bool CheckUpdateRA(MainForm mainForm)
		{
			try
			{
				var http = new HttpCommunication(null, "https://retroachievements.org/dorequest.php?r=latestintegration", null);
				var info = JsonConvert.DeserializeObject<Dictionary<string, object>>(http.ExecGet());
				if (info.TryGetValue("Success", out var success) && (bool)success)
				{
					var lastestVer = new Version((string)info["LatestVersion"]);
					var minVer = new Version((string)info["MinimumVersion"]);

					if (_version < minVer)
					{
						if (mainForm.ShowMessageBox2(
							owner: null,
							text: "An update is required to use RetroAchievements. Do you want to download the update now?",
							caption: "Update",
							icon: EMsgBoxIcon.Question,
							useOKCancel: false))
						{
							DetachDll();
							var ret = DownloadDll((string)info["LatestVersionUrlX64"]);
							AttachDll();
							return ret;
						}
						else
						{
							return false;
						}
					}
					else if (_version < lastestVer)
					{
						if (mainForm.ShowMessageBox2(
							owner: null,
							text: "An optional update is available for RetroAchievements. Do you want to download the update now?",
							caption: "Update",
							icon: EMsgBoxIcon.Question,
							useOKCancel: false))
						{
							DetachDll();
							DownloadDll((string)info["LatestVersionUrlX64"]);
							AttachDll();
							return true; // even if this fails, should be OK to use the old dll
						}
						else
						{
							// don't have to update in this case
							return true;
						}
					}
					else
					{
						return true;
					}
				}
				else
				{
					mainForm.ShowMessageBox(
						owner: null,
						text: "Failed to fetch update information, cannot start RetroAchievements.",
						caption: "Error",
						icon: EMsgBoxIcon.Error);

					return false;
				}
			}
			catch (Exception ex)
			{
				// is this needed?
				mainForm.ShowMessageBox(
					owner: null,
					text: $"Exception {ex.Message} occurred when fetching update information, cannot start RetroAchievements.",
					caption: "Error",
					icon: EMsgBoxIcon.Error);

				DetachDll();
				AttachDll();
				return false;
			}
		}
	}
}
