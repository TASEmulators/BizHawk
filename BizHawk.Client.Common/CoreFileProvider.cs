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

		public string DllPath()
		{
			return Path.Combine(PathManager.GetExeDirectoryAbsolute(), "dll");
		}

		public string GetRetroSaveRAMDirectory()
		{
			return PathManager.RetroSaveRAMDirectory(Global.Game);
		}

		public string GetRetroSystemPath()
		{
			return PathManager.RetroSystemPath(Global.Game);
		}

		#region EmuLoadHelper api

		private void FirmwareWarn(string sysID, string firmwareID, bool required, string msg = null)
		{
			if (required)
			{
				var fullMsg = $"Couldn't find required firmware \"{sysID}:{firmwareID}\".  This is fatal{(msg != null ? $": {msg}" : ".")}";
				throw new MissingFirmwareException(fullMsg);
			}

			if (msg != null)
			{
				var fullMsg = $"Couldn't find firmware \"{sysID}:{firmwareID}\".  Will attempt to continue: {msg}";
				_showWarning(fullMsg);
			}
		}

		/// <exception cref="MissingFirmwareException">not found and <paramref name="required"/> is true</exception>
		private string GetFirmwarePath(string sysId, string firmwareId, bool required, string msg = null)
		{
			var path = FirmwareManager.Request(Global.Config.PathEntries.FirmwaresPathFragment, Global.Config.FirmwareUserSpecifications, sysId, firmwareId);
			if (path != null && !File.Exists(path))
			{
				path = null;
			}

			if (path == null)
			{
				FirmwareWarn(sysId, firmwareId, required, msg);
			}

			return path;
		}

		private byte[] GetFirmwareWithPath(string sysId, string firmwareId, bool required, string msg, out string path)
		{
			byte[] ret = null;
			var path_ = GetFirmwarePath(sysId, firmwareId, required, msg);
			if (path_ != null && File.Exists(path_))
			{
				try
				{
					ret = File.ReadAllBytes(path_);
				}
				catch (IOException)
				{
				}
			}

			if (ret == null && path_ != null)
			{
				FirmwareWarn(sysId, firmwareId, required, msg);
			}

			path = path_;
			return ret;
		}

		/// <exception cref="MissingFirmwareException">not found and <paramref name="required"/> is true</exception>
		public byte[] GetFirmware(string sysId, string firmwareId, bool required, string msg = null)
		{
			string unused;
			return GetFirmwareWithPath(sysId, firmwareId, required, msg, out unused);
		}

		/// <exception cref="MissingFirmwareException">not found and <paramref name="required"/> is true</exception>
		public byte[] GetFirmwareWithGameInfo(string sysId, string firmwareId, bool required, out GameInfo gi, string msg = null)
		{
			byte[] ret = GetFirmwareWithPath(sysId, firmwareId, required, msg, out var path);
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
			if (target.CoreFileProvider is CoreFileProvider)
			{
				superhack = ((CoreFileProvider)target.CoreFileProvider).SubfileDirectory;
			}

			var cfp = new CoreFileProvider(target.ShowMessage) { SubfileDirectory = superhack };
			target.CoreFileProvider = cfp;
			cfp.FirmwareManager = Global.FirmwareManager;
		}
	}
}