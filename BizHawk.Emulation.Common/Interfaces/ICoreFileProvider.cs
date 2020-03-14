using System;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Defines the means by which firmware, bios and other necessary files are provided to a core that needs them
	/// </summary>
	public interface ICoreFileProvider
	{
		/// <summary>
		/// produces a path that contains emulation related DLL and exe files
		/// </summary>
		string DllPath();

		/// <summary>
		/// produces a path that contains saveram... because libretro cores need it
		/// </summary>
		string GetRetroSaveRAMDirectory();

		/// <summary>
		/// produces a path for use as a libretro system path (different for each core)
		/// </summary>
		string GetRetroSystemPath();

		#region EmuLoadHelper api

		/// <summary>
		/// Get a firmware as a byte array
		/// </summary>
		/// <param name="sysId">the core systemID</param>
		/// <param name="firmwareId">the firmware id</param>
		/// <param name="required">if true, result is guaranteed to be valid; else null is possible if not found</param>
		/// <param name="msg">message to show if fail to get</param>
		byte[] GetFirmware(string sysId, string firmwareId, bool required, string msg = null);

		byte[] GetFirmwareWithGameInfo(string sysId, string firmwareId, bool required, out GameInfo gi, string msg = null);

		#endregion
	}
}
