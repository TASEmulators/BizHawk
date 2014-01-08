using System;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class CoreFileProvider : ICoreFileProvider
	{
		public string SubfileDirectory { get; set; }
		public FirmwareManager FirmwareManager { get; set; }

		private readonly Action<string> _showWarning;

		public CoreFileProvider(Action<string> showWarning)
		{
			_showWarning = showWarning;
		}

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

		public string DllPath()
		{
			return Path.Combine(PathManager.GetExeDirectoryAbsolute(), "dll");
		}

		#region EmuLoadHelper api

		private void FirmwareWarn(string sysID, string firmwareID, bool required, string msg = null)
		{
			if (required)
			{
				var fullmsg = String.Format(
					"Couldn't find required firmware \"{0}:{1}\".  This is fatal{2}", sysID, firmwareID, msg != null ? ": " + msg : ".");
				throw new Exception(fullmsg);
			}
			else
			{
				if (msg != null)
				{
					var fullmsg = String.Format(
						"Couldn't find firmware \"{0}:{1}\".  Will attempt to continue: {2}", sysID, firmwareID, msg);
					_showWarning(fullmsg);
				}
			}
		}

		public string GetFirmwarePath(string sysID, string firmwareID, bool required, string msg = null)
		{
			var path = FirmwareManager.Request(sysID, firmwareID);
			if (path != null && !File.Exists(path))
			{
				path = null;
			}

			if (path == null)
			{
				FirmwareWarn(sysID, firmwareID, required, msg);
			}

			return path;
		}

		public byte[] GetFirmware(string sysID, string firmwareID, bool required, string msg = null)
		{
			byte[] ret = null;
			var path = GetFirmwarePath(sysID, firmwareID, required, msg);
			if (path != null && File.Exists(path))
			{
				try
				{
					ret = File.ReadAllBytes(path);
				}
				catch (IOException) { }
			}

			if (ret == null && path != null)
			{
				FirmwareWarn(sysID, firmwareID, required, msg);
			}

			return ret;
		}

		#endregion

		public static void SyncCoreCommInputSignals(CoreComm target = null)
		{
			if (target == null)
			{
				target = Global.CoreComm;
			}

			var cfp = new CoreFileProvider(target.ShowMessage);
			target.CoreFileProvider = cfp;
			cfp.FirmwareManager = Global.FirmwareManager;
		}
	}
}