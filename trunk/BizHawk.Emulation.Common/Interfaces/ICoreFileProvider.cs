using System.IO;

namespace BizHawk.Emulation.Common
{
	public interface ICoreFileProvider
	{
		/// <summary>
		/// Opens a firmware according to the specified firmware ID key
		/// </summary>
		Stream OpenFirmware(string sysId, string key);

		/// <summary>
		/// Returns the path to a firmware according to the specified firmware ID key. Use OpenFirmware instead
		/// </summary>
		string PathFirmware(string sysId, string key);

		/// <summary>
		/// Produces a path to the requested file, expected to be parallel to the running rom. for example: cue+bin files or sfc+pcm (MSU-1 games)
		/// </summary>
		string PathSubfile(string fname);

		/// <summary>
		/// produces a path that contains emulation related dll and exe files
		/// </summary>
		/// <returns></returns>
		string DllPath();

		#region EmuLoadHelper api

		/// <summary>
		/// get path to a firmware
		/// </summary>
		/// <param name="sysID"></param>
		/// <param name="firmwareID"></param>
		/// <param name="required">if true, result is guaranteed to be valid; else null is possible if not foun</param>
		/// <param name="msg">message to show if fail to get</param>
		/// <returns></returns>
		string GetFirmwarePath(string sysID, string firmwareID, bool required, string msg = null);

		/// <summary>
		/// get a firmware as a byte array
		/// </summary>
		/// <param name="sysID"></param>
		/// <param name="firmwareID"></param>
		/// <param name="required">if true, result is guaranteed to be valid; else null is possible if not found</param>
		/// <param name="msg">message to show if fail to get</param>
		/// <returns></returns>
		byte[] GetFirmware(string sysID, string firmwareID, bool required, string msg = null);

		#endregion

	}
}
