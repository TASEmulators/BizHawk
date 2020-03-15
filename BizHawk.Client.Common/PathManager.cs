using System.IO;
using System.Reflection;
using BizHawk.Common.PathExtensions;

namespace BizHawk.Client.Common
{
	public static class PathManager
	{
		static PathManager()
		{
			var defaultIni = Path.Combine(PathUtils.GetExeDirectoryAbsolute(), "config.ini");
			SetDefaultIniPath(defaultIni);
		}

		/// <summary>
		/// The location of the default INI file
		/// </summary>
		public static string DefaultIniPath { get; private set; }

		public static void SetDefaultIniPath(string newDefaultIniPath)
		{
			DefaultIniPath = newDefaultIniPath;
		}
	}
}
