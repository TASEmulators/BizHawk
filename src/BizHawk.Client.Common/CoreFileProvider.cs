using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class CoreFileProvider : ICoreFileProvider
	{
		private readonly FirmwareManager _firmwareManager;
		private readonly Action<string> _showWarning;
		private readonly PathEntryCollection _pathEntries;
		private readonly IDictionary<string, string> _firmwareUserSpecifications;

		public CoreFileProvider(
			Action<string> showWarning,
			FirmwareManager firmwareManager,
			PathEntryCollection pathEntries,
			IDictionary<string, string> firmwareUserSpecifications)
		{
			_showWarning = showWarning;
			_firmwareManager = firmwareManager;
			_pathEntries = pathEntries;
			_firmwareManager = firmwareManager;
			_firmwareUserSpecifications = firmwareUserSpecifications;
		}

		public string DllPath() => PathUtils.DllDirectoryPath;

		// Poop
		public string GetRetroSaveRAMDirectory(GameInfo game)
			=> _pathEntries.RetroSaveRamAbsolutePath(game, Global.MovieSession.Movie);

		// Poop
		public string GetRetroSystemPath(GameInfo game)
			=> _pathEntries.RetroSystemAbsolutePath(game);

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

		private byte[] GetFirmwareWithPath(string sysId, string firmwareId, bool required, string msg, out string path)
		{
			var firmwarePath = _firmwareManager.Request(
				_pathEntries,
				_firmwareUserSpecifications,
				sysId,
				firmwareId);

			if (firmwarePath == null || !File.Exists(firmwarePath))
			{
				path = null;
				FirmwareWarn(sysId, firmwareId, required, msg);
				return null;
			}

			try
			{
				var ret = File.ReadAllBytes(firmwarePath);
				path = firmwarePath;
				return ret;
			}
			catch (IOException)
			{
				path = null;
				FirmwareWarn(sysId, firmwareId, required, msg);
				return null;
			}
		}

		/// <exception cref="MissingFirmwareException">not found and <paramref name="required"/> is true</exception>
		public byte[] GetFirmware(string sysId, string firmwareId, bool required, string msg = null)
			=> GetFirmwareWithPath(sysId, firmwareId, required, msg, out _);

		/// <exception cref="MissingFirmwareException">not found and <paramref name="required"/> is true</exception>
		public byte[] GetFirmwareWithGameInfo(string sysId, string firmwareId, bool required, out GameInfo gi, string msg = null)
		{
			byte[] ret = GetFirmwareWithPath(sysId, firmwareId, required, msg, out var path);
			gi = ret != null && path != null
				? Database.GetGameInfo(ret, path)
				: null;

			return ret;
		}
	}
}