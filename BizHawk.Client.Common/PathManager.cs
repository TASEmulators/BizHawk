using System.IO;
using System.Reflection;

namespace BizHawk.Client.Common
{
	public static class PathManager
	{
		static PathManager()
		{
			var defaultIni = Path.Combine(GetExeDirectoryAbsolute(), "config.ini");
			SetDefaultIniPath(defaultIni);
		}

		public static string GetExeDirectoryAbsolute()
		{
			var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			if (path.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				path = path.Remove(path.Length - 1, 1);
			}

			return path;
		}

		// TODO: this always makes an absolute path!
		// Needs to be fixed, the intent was to turn an absolute path
		// into one relative to the exe
		// for instance: C:\BizHawk\Lua becomes .\Lua (if EmuHawk.Exe is in C:\BizHawk)
		/// <summary>
		/// Makes a path relative to the %exe% directory
		/// </summary>
		public static string MakeProgramRelativePath(string path)
		{
			return Path.Combine(GetExeDirectoryAbsolute(), path);
		}

		public static string GetDllDirectory()
		{
			return Path.Combine(GetExeDirectoryAbsolute(), "dll");
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
