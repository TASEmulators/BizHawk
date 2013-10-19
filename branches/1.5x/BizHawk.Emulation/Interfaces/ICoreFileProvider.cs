using System.IO;

namespace BizHawk
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
	}
}
