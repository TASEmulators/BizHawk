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
		public string GetRetroSaveRAMDirectory(IGameInfo game)
			=> _pathEntries.RetroSaveRamAbsolutePath(game);

		// Poop
		public string GetRetroSystemPath(IGameInfo game)
			=> _pathEntries.RetroSystemAbsolutePath(game);

		private void FirmwareWarn(FirmwareID id, bool required, string msg = null)
		{
			if (required)
			{
				var fullMsg = $"Couldn't find required firmware {id}.  This is fatal{(msg != null ? $": {msg}" : ".")}";
				throw new MissingFirmwareException(fullMsg);
			}

			if (msg != null)
			{
				var fullMsg = $"Couldn't find firmware {id}.  Will attempt to continue: {msg}";
				_showWarning(fullMsg);
			}
		}

		private byte[] GetFirmwareWithPath(FirmwareID id, bool required, string msg, out string path)
		{
			var firmwarePath = _firmwareManager.Request(_pathEntries, _firmwareUserSpecifications, id);

			if (firmwarePath == null || !File.Exists(firmwarePath))
			{
				path = null;
				FirmwareWarn(id, required, msg);
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
				FirmwareWarn(id, required, msg);
				return null;
			}
		}

		/// <exception cref="MissingFirmwareException">not found and <paramref name="required"/> is true</exception>
		public byte[] GetFirmware(FirmwareID id, bool required, string msg = null)
			=> GetFirmwareWithPath(id, required, msg, out _);

		/// <exception cref="MissingFirmwareException">not found and <paramref name="required"/> is true</exception>
		public byte[] GetFirmwareWithGameInfo(FirmwareID id, bool required, out GameInfo gi, string msg = null)
		{
			var ret = GetFirmwareWithPath(id, required, msg, out var path);
			gi = ret != null && path != null
				? Database.GetGameInfo(ret, path)
				: null;

			return ret;
		}
	}
}