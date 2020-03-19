using System;
using System.IO;
using System.Net;
using System.Threading;

using Newtonsoft.Json.Linq;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class UpdateChecker
	{
		private static readonly string _latestVersionInfoURL = "https://api.github.com/repos/TASVideos/BizHawk/releases/latest";
		private static readonly TimeSpan _minimumCheckDuration = TimeSpan.FromHours(8);

		private static bool AutoCheckEnabled
		{
			get => Global.Config.UpdateAutoCheckEnabled;
			set => Global.Config.UpdateAutoCheckEnabled = value;
		}

		private static DateTime? LastCheckTimeUTC
		{
			get => Global.Config.UpdateLastCheckTimeUtc;
			set => Global.Config.UpdateLastCheckTimeUtc = value;
		}

		private static string LatestVersion
		{
			get => Global.Config.UpdateLatestVersion;
			set => Global.Config.UpdateLatestVersion = value;
		}

		private static string IgnoreVersion
		{
			get => Global.Config.UpdateIgnoreVersion;
			set => Global.Config.UpdateIgnoreVersion = value;
		}

		public static void BeginCheck(bool skipCheck = false)
		{
			if (skipCheck || string.IsNullOrEmpty(_latestVersionInfoURL) || !AutoCheckEnabled || LastCheckTimeUTC > DateTime.UtcNow - _minimumCheckDuration)
			{
				OnCheckComplete();
				return;
			}

			ThreadPool.QueueUserWorkItem((s) => CheckInternal());
		}

		public static bool IsNewVersionAvailable =>
			AutoCheckEnabled
			&& LatestVersion != IgnoreVersion
			&& ParseVersion(VersionInfo.MainVersion) != 0 // Avoid notifying if current version string is invalid
			&& ParseVersion(LatestVersion) > ParseVersion(VersionInfo.MainVersion);

		public static void IgnoreNewVersion()
		{
			IgnoreVersion = LatestVersion;
		}

		public static void ResetHistory()
		{
			LastCheckTimeUTC = null;
			LatestVersion = "";
			IgnoreVersion = "";
		}

		private static void CheckInternal()
		{
			try
			{
				JObject response = JObject.Parse(DownloadURLAsString(_latestVersionInfoURL));

				LatestVersion = ValidateVersionNumberString((string)response["name"]);
			}
			catch
			{
				// Ignore errors, and fall through to set the last check time to avoid requesting too frequently from the web server
			}

			LastCheckTimeUTC = DateTime.UtcNow;

			OnCheckComplete();
		}

		private static string DownloadURLAsString(string url)
		{
			var request = (HttpWebRequest)WebRequest.Create(url);
			request.UserAgent = "BizHawk";
			request.KeepAlive = false;
			using var response = (HttpWebResponse)request.GetResponse();
			using var responseStream = new StreamReader(response.GetResponseStream());
			return responseStream.ReadToEnd();
		}

		private static string ValidateVersionNumberString(string versionNumber)
		{
			return versionNumber != null && ParseVersion(versionNumber) != 0 ? versionNumber : "";
		}

		// Major version goes in the first 16 bits, and so on, up to 4 parts
		private static ulong ParseVersion(string str)
		{
			string[] split = str.Split('.');
			if (split.Length > 4) return 0;
			ulong version = 0;
			for (int i = 0; i < split.Length; i++)
			{
				if (!UInt16.TryParse(split[i], out var versionPart)) return 0;
				version |= (ulong)versionPart << (48 - (i * 16));
			}
			return version;
		}

		private static void OnCheckComplete()
		{
			CheckComplete?.Invoke(null, EventArgs.Empty);
		}

		public static event EventHandler CheckComplete;
	}
}
