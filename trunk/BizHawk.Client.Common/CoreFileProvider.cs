using System;
using System.IO;

namespace BizHawk.Client.Common
{
	public class CoreFileProvider : ICoreFileProvider
	{
		public string SubfileDirectory;
		public FirmwareManager FirmwareManager;

		public Stream OpenFirmware(string sysId, string key)
		{
			var fn = PathFirmware(sysId, key);
			return new FileStream(fn, FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		public string PathFirmware(string sysId, string key)
		{
			return FirmwareManager.Request(sysId, key);
		}

		public string PathSubfile(string fname)
		{
			return Path.Combine(Path.GetDirectoryName(SubfileDirectory) ?? String.Empty, fname);
		}
	}
}