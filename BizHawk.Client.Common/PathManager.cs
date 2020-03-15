using System;
using System.Linq;
using System.IO;
using System.Reflection;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Nintendo.SNES9X;

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
				.Replace(":", " -") // adelikat - Path.GetFileName scraps everything to the left of a colon unfortunately, so we need this hack here
				.Replace("\"", "")  // adelikat - Ivan Ironman Stewart's Super Off-Road has quotes in game name
				.Replace("/", "+"); // Narry - Mario Bros / Duck hunt has a slash in the name which GetDirectoryName and GetFileName treat as if it were a folder

			// zero 06-nov-2015 - regarding the below, i changed my mind. for libretro i want subdirectories here.
			var filesystemDir = Path.GetDirectoryName(filesystemSafeName);
			filesystemSafeName = Path.GetFileName(filesystemSafeName);

			filesystemSafeName = RemoveInvalidFileSystemChars(filesystemSafeName);

			// zero 22-jul-2012 - i don't think this is used the same way it used to. game.Name shouldn't be a path, so this stuff is illogical.
			// if game.Name is a path, then someone shouldve made it not-a-path already.
			// return Path.Combine(Path.GetDirectoryName(filesystemSafeName), Path.GetFileNameWithoutExtension(filesystemSafeName));

			// adelikat:
			// This hack is to prevent annoying things like Super Mario Bros..bk2
			if (filesystemSafeName.EndsWith("."))
			{
				filesystemSafeName = filesystemSafeName.Remove(filesystemSafeName.Length - 1, 1);
			}

			return Path.Combine(filesystemDir, filesystemSafeName);
		}

		public static string SaveStatePrefix(GameInfo game)
		{
			var name = FilesystemSafeName(game);

			// Neshawk and Quicknes have incompatible savestates, store the name to keep them separate
			if (Global.Emulator.SystemId == "NES")
			{
				name += $".{Global.Emulator.Attributes().CoreName}";
			}

			// Gambatte and GBHawk have incompatible savestates, store the name to keep them separate
			if (Global.Emulator.SystemId == "GB")
			{
				name += $".{Global.Emulator.Attributes().CoreName}";
			}

			if (Global.Emulator is Snes9x) // Keep snes9x savestate away from libsnes, we want to not be too tedious so bsnes names will just have the profile name not the core name
			{
				name += $".{Global.Emulator.Attributes().CoreName}";
			}

			// Bsnes profiles have incompatible savestates so save the profile name
			if (Global.Emulator is LibsnesCore)
			{
				name += $".{((LibsnesCore)Global.Emulator).CurrentProfile}";
			}

			if (Global.Emulator.SystemId == "GBA")
			{
				name += $".{Global.Emulator.Attributes().CoreName}";
			}

			if (Global.MovieSession.Movie.IsActive())
			{
				name += $".{Path.GetFileNameWithoutExtension(Global.MovieSession.Movie.Filename)}";
			}

			var pathEntry = Global.Config.PathEntries[game.System, "Savestates"] ??
							Global.Config.PathEntries[game.System, "Base"];

			return Path.Combine(Global.Config.PathEntries.AbsolutePathFor(pathEntry.Path, game.System), name);
		}

		/// <summary>
		/// Takes an absolute path and attempts to convert it to a relative, based on the system, 
		/// or global base if no system is supplied, if it is not a subfolder of the base, it will return the path unaltered
		/// </summary>
		public static string TryMakeRelative(string absolutePath, string system = null)
		{
			var parentPath = string.IsNullOrWhiteSpace(system)
				? Global.Config.PathEntries.GlobalBaseAsAbsolute()
				: Global.Config.PathEntries.AbsolutePathFor(Global.Config.PathEntries.BaseFor(system), system);
#if true
			if (!IsSubfolder(parentPath, absolutePath)) return absolutePath;

			return OSTailoredCode.IsUnixHost
				? "./" + OSTailoredCode.SimpleSubshell("realpath", $"--relative-to=\"{parentPath}\" \"{absolutePath}\"", $"invalid path {absolutePath} or missing realpath binary")
				: absolutePath.Replace(parentPath, ".");
#else // written for Unix port but may be useful for .NET Core
			if (!IsSubfolder(parentPath, absolutePath))
			{
				return OSTailoredCode.IsUnixHost && parentPath.TrimEnd('.') == $"{absolutePath}/" ? "." : absolutePath;
			}

			return OSTailoredCode.IsUnixHost
				? absolutePath.Replace(parentPath.TrimEnd('.'), "./")
				: absolutePath.Replace(parentPath, ".");
#endif
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

		/// <summary>
		/// Puts the currently configured temp path into the environment for use as actual temp directory
		/// </summary>
		public static void RefreshTempPath()
		{
			if (Global.Config.PathEntries.TempFilesFragment != "")
			{
				//TODO - BUG - needs to route through PathManager.MakeAbsolutePath or something similar, but how?
				string target = Global.Config.PathEntries.TempFilesFragment;
				BizHawk.Common.TempFileManager.HelperSetTempPath(target);
			}
		}
	}
}
