using System;
using System.Linq;
using System.IO;
using System.Reflection;

using BizHawk.Common;
using BizHawk.Emulation.Common;

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

		// Decides if a path is non-empty, not . and not .\
		public static bool PathIsSet(string path)
		{
			if (!string.IsNullOrWhiteSpace(path))
			{
				return path != "." && path != ".\\";
			}

			return false;
		}

		public static string RemoveInvalidFileSystemChars(string name)
		{
			var newStr = name;
			var chars = Path.GetInvalidFileNameChars();
			return chars.Aggregate(newStr, (current, c) => current.Replace(c.ToString(), ""));
		}

		public static string FilesystemSafeName(GameInfo game)
		{
			var filesystemSafeName = game.Name
				.Replace("|", "+")
				.Replace(":", " -") // Path.GetFileName scraps everything to the left of a colon unfortunately, so we need this hack here
				.Replace("\"", "")  // Ivan IronMan Stewart's Super Off-Road has quotes in game name
				.Replace("/", "+"); // Mario Bros / Duck hunt has a slash in the name which GetDirectoryName and GetFileName treat as if it were a folder

			// zero 06-nov-2015 - regarding the below, i changed my mind. for libretro i want subdirectories here.
			var filesystemDir = Path.GetDirectoryName(filesystemSafeName);
			filesystemSafeName = Path.GetFileName(filesystemSafeName);

			filesystemSafeName = RemoveInvalidFileSystemChars(filesystemSafeName);

			// zero 22-jul-2012 - i don't think this is used the same way it used to. game.Name shouldn't be a path, so this stuff is illogical.
			// if game.Name is a path, then someone should have made it not-a-path already.
			// return Path.Combine(Path.GetDirectoryName(filesystemSafeName), Path.GetFileNameWithoutExtension(filesystemSafeName));

			// adelikat:
			// This hack is to prevent annoying things like Super Mario Bros..bk2
			if (filesystemSafeName.EndsWith("."))
			{
				filesystemSafeName = filesystemSafeName.Remove(filesystemSafeName.Length - 1, 1);
			}

			return Path.Combine(filesystemDir, filesystemSafeName);
		}

		public static string MakeRelativeTo(string absolutePath, string basePath)
		{
			if (IsSubfolder(basePath, absolutePath))
			{
				return absolutePath.Replace(basePath, ".");
			}

			return absolutePath;
		}

		/// <remarks>Algorithm for Windows taken from https://stackoverflow.com/a/7710620/7467292</remarks>
		public static bool IsSubfolder(string parentPath, string childPath)
		{
			if (OSTailoredCode.IsUnixHost)
			{
#if true
				return OSTailoredCode.SimpleSubshell("realpath", $"-L \"{childPath}\"", $"invalid path {childPath} or missing realpath binary")
					.StartsWith(OSTailoredCode.SimpleSubshell("realpath", $"-L \"{parentPath}\"", $"invalid path {parentPath} or missing realpath binary"));
#else // written for Unix port but may be useful for Windows when moving to .NET Core
				var parentUriPath = new Uri(parentPath.TrimEnd('.')).AbsolutePath.TrimEnd('/');
				try
				{
					for (var childUri = new DirectoryInfo(childPath).Parent; childUri != null; childUri = childUri?.Parent)
					{
						if (new Uri(childUri.FullName).AbsolutePath.TrimEnd('/') == parentUriPath) return true;
					}
				}
				catch
				{
					// ignored
				}
				return false;
#endif
			}
			var parentUri = new Uri(parentPath);
			for (var childUri = new DirectoryInfo(childPath).Parent; childUri != null; childUri = childUri?.Parent)
			{
				if (new Uri(childUri.FullName) == parentUri) return true;
			}
			return false;
		}
	}
}
