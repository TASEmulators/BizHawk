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

		public string PathSubfile(string fname)
		{
			return Path.Combine(SubfileDirectory ?? String.Empty, fname);
		}

		public string DllPath()
		{
			return Path.Combine(PathManager.GetExeDirectoryAbsolute(), "dll");
		}

		public string GetSaveRAMPath()
		{
			return PathManager.SaveRamPath(Global.Game);
		}


		public string GetRetroSaveRAMDirectory()
		{
			return PathManager.RetroSaveRAMDirectory(Global.Game);
		}

		public string GetRetroSystemPath()
		{
			return PathManager.RetroSystemPath(Global.Game);
		}

		public string GetGameBasePath()
		{
			return PathManager.GetGameBasePath(Global.Game);
		}

		#region EmuLoadHelper api

		private void FirmwareWarn(string sysID, string firmwareID, bool required, string msg = null)
		{
			if (required)
			{
				var fullmsg = String.Format(
					"Couldn't find required firmware \"{0}:{1}\".  This is fatal{2}", sysID, firmwareID, msg != null ? ": " + msg : ".");
				throw new MissingFirmwareException(fullmsg);
			}

			if (msg != null)
			{
				var fullmsg = String.Format(
					"Couldn't find firmware \"{0}:{1}\".  Will attempt to continue: {2}", sysID, firmwareID, msg);
				_showWarning(fullmsg);
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

		private byte[] GetFirmwareWithPath(string sysID, string firmwareID, bool required, string msg, out string path)
		{
			byte[] ret = null;
			var path_ = GetFirmwarePath(sysID, firmwareID, required, msg);
			if (path_ != null && File.Exists(path_))
			{
				try
				{
					ret = File.ReadAllBytes(path_);
				}
				catch (IOException) { }
			}

			if (ret == null && path_ != null)
			{
				FirmwareWarn(sysID, firmwareID, required, msg);
			}

			path = path_;
			return ret;
		}

		public byte[] GetFirmware(string sysID, string firmwareID, bool required, string msg = null)
		{
			string unused;
			return GetFirmwareWithPath(sysID, firmwareID, required, msg, out unused);
		}

		public byte[] GetFirmwareWithGameInfo(string sysID, string firmwareID, bool required, out GameInfo gi, string msg = null)
		{
			string path;
			byte[] ret = GetFirmwareWithPath(sysID, firmwareID, required, msg, out path);
			if (ret != null && path != null)
			{
				gi = Database.GetGameInfo(ret, path);
			}
			else
			{
				gi = null;
			}
			return ret;
		}

		#endregion

		// this should go away now
		public static void SyncCoreCommInputSignals(CoreComm target)
		{
			string superhack = null;
			if (target.CoreFileProvider != null && target.CoreFileProvider is CoreFileProvider)
				superhack = ((CoreFileProvider)target.CoreFileProvider ).SubfileDirectory;
			var cfp = new CoreFileProvider(target.ShowMessage);
			cfp.SubfileDirectory = superhack;
			target.CoreFileProvider = cfp;
			cfp.FirmwareManager = Global.FirmwareManager;
		}
	}
}