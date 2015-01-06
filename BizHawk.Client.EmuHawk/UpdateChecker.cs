using System;
using System.IO;
using System.Net;
using System.Threading;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class UpdateChecker
	{
		private static readonly string _latestVersionInfoURL = "http://tasvideos.org/SystemBizHawkReleaseManager.html";
		private static readonly TimeSpan _minimumCheckDuration = TimeSpan.FromHours(8);

		private static bool AutoCheckEnabled
		{
			get { return Global.Config.Update_AutoCheckEnabled; }
			set { Global.Config.Update_AutoCheckEnabled = value; }
		}

		private static DateTime? LastCheckTimeUTC
		{
			get { return Global.Config.Update_LastCheckTimeUTC; }
			set { Global.Config.Update_LastCheckTimeUTC = value; }
		}

		private static string LatestVersion
		{
			get { return Global.Config.Update_LatestVersion; }
			set { Global.Config.Update_LatestVersion = value; }
		}

		private static string IgnoreVersion
		{
			get { return Global.Config.Update_IgnoreVersion; }
			set { Global.Config.Update_IgnoreVersion = value; }
		}

		public static void BeginCheck(bool skipCheck = false)
		{
			if (skipCheck || String.IsNullOrEmpty(_latestVersionInfoURL) || !AutoCheckEnabled || LastCheckTimeUTC > DateTime.UtcNow - _minimumCheckDuration)
			{
				OnCheckComplete();
				return;
			}

			ThreadPool.QueueUserWorkItem((s) => CheckInternal());
		}

		public static bool IsNewVersionAvailable
		{
			get
			{
				return AutoCheckEnabled &&
					LatestVersion != IgnoreVersion &&
					ParseVersion(VersionInfo.MAINVERSION) != 0 && // Avoid notifying if current version string is invalid
					ParseVersion(LatestVersion) > ParseVersion(VersionInfo.MAINVERSION);
			}
		}

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
				string latestVersionInfo = WebUtility.HtmlDecode(DownloadURLAsString(_latestVersionInfoURL));

				LatestVersion = GetVersionNumberFromVersionInfo(latestVersionInfo);
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
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.KeepAlive = false;
			using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
			using (StreamReader responseStream = new StreamReader(response.GetResponseStream()))
			{
				return responseStream.ReadToEnd();
			}
		}

		private static string GetVersionNumberFromVersionInfo(string info)
		{
			string versionNumber = GetTextFromTag(info, "VersionNumber");
			return (versionNumber != null && ParseVersion(versionNumber) != 0) ? versionNumber : "";
		}

		private static string GetTextFromTag(string info, string tagName)
		{
			string openTag = "[" + tagName + "]";
			string closeTag = "[/" + tagName + "]";
			int start = info.IndexOf(openTag, StringComparison.OrdinalIgnoreCase);
			if (start == -1) return null;
			start += openTag.Length;
			int end = info.IndexOf(closeTag, start, StringComparison.OrdinalIgnoreCase);
			if (end == -1) return null;
			return info.Substring(start, end - start).Trim();
		}

		// Major version goes in the first 16 bits, and so on, up to 4 parts
		private static ulong ParseVersion(string str)
		{
			string[] split = str.Split('.');
			if (split.Length > 4) return 0;
			ulong version = 0;
			for (int i = 0; i < split.Length; i++)
			{
				ushort versionPart;
				if (!UInt16.TryParse(split[i], out versionPart)) return 0;
				version |= (ulong)versionPart << (48 - (i * 16));
			}
			return version;
		}

		private static void OnCheckComplete()
		{
			CheckComplete(null, EventArgs.Empty);
		}

		public static event EventHandler CheckComplete = delegate { };
	}
}
