using System;
using System.Linq;
using System.IO;
using System.Reflection;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Emulation.Cores.Nintendo.SNES;

namespace BizHawk.Client.Common
{
	public static class PathManager
	{
		public static string GetExeDirectoryAbsolute()
		{
			var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			if (path.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				path = path.Remove(path.Length - 1, 1);
			}

			return path;
		}

		/// <summary>
		/// Makes a path relative to the %exe% dir
		/// </summary>
		public static string MakeProgramRelativePath(string path) { return MakeAbsolutePath("%exe%/" + path, null); }

		public static string GetDllDirectory() { return Path.Combine(GetExeDirectoryAbsolute(), "dll"); }

		/// <summary>
		/// The location of the default INI file
		/// </summary>
		public static string DefaultIniPath
		{
			get
			{
				return MakeProgramRelativePath("config.ini");
			}
		}

		/// <summary>
		/// Gets absolute base as derived from EXE
		/// </summary>
		/// <returns></returns>
		public static string GetBasePathAbsolute()
		{
			if (Global.Config.PathEntries.GlobalBaseFragment.Length < 1) // If empty, then EXE path
			{
				return GetExeDirectoryAbsolute();
			}

			if (Global.Config.PathEntries.GlobalBaseFragment.Length >= 5
				&& Global.Config.PathEntries.GlobalBaseFragment.Substring(0, 5) == "%exe%")
			{
				return GetExeDirectoryAbsolute();
			}

			if (Global.Config.PathEntries.GlobalBaseFragment[0] == '.')
			{
				if (Global.Config.PathEntries.GlobalBaseFragment.Length == 1)
				{
					return GetExeDirectoryAbsolute();
				}

				if (Global.Config.PathEntries.GlobalBaseFragment.Length == 2 &&
					Global.Config.PathEntries.GlobalBaseFragment == ".\\")
				{
					return GetExeDirectoryAbsolute();
				}

				var tmp = Global.Config.PathEntries.GlobalBaseFragment.Remove(0, 1);
				tmp = tmp.Insert(0, GetExeDirectoryAbsolute());
				return tmp;
			}

			if (Global.Config.PathEntries.GlobalBaseFragment.Substring(0, 2) == "..")
			{
				return RemoveParents(Global.Config.PathEntries.GlobalBaseFragment, GetExeDirectoryAbsolute());
			}

			// In case of error, return EXE path
			return GetExeDirectoryAbsolute();
		}

		public static string GetPlatformBase(string system)
		{
			return Global.Config.PathEntries[system, "Base"].Path;
		}

		public static string StandardFirmwareName(string name)
		{
			return Path.Combine(MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPathFragment, null), name);
		}

		public static string MakeAbsolutePath(string path, string system)
		{
			// Hack
			if (system == "Global")
			{
				system = null;
			}

			// This function translates relative path and special identifiers in absolute paths

			if (path.Length < 1)
			{
				return GetBasePathAbsolute();
			}

			if (path == "%recent%")
			{
				return Environment.SpecialFolder.Recent.ToString();
			}

			if (path.Length >= 5 && path.Substring(0, 5) == "%exe%")
			{
				if (path.Length == 5)
				{
					return GetExeDirectoryAbsolute();
				}

				var tmp = path.Remove(0, 5);
				tmp = tmp.Insert(0, GetExeDirectoryAbsolute());
				return tmp;
			}

			if (path[0] == '.')
			{
				if (!string.IsNullOrWhiteSpace(system))
				{
					path = path.Remove(0, 1);
					path = path.Insert(0, GetPlatformBase(system));
				}

				if (path.Length == 1)
				{
					return GetBasePathAbsolute();
				}

				if (path[0] == '.')
				{
					path = path.Remove(0, 1);
					path = path.Insert(0, GetBasePathAbsolute());
				}

				return path;
			}

			// If begins wtih .. do alorithm to determine how many ..\.. combos and deal with accordingly, return drive letter only if too many ..
			if ((path[0] > 'A' && path[0] < 'Z') || (path[0] > 'a' && path[0] < 'z'))
			{
				// C:\
				if (path.Length > 2 && path[1] == ':' && path[2] == '\\')
				{
					return path;
				}

				// file:\ is an acceptable path as well, and what FileBrowserDialog returns
				if (path.Length >= 6 && path.Substring(0, 6) == "file:\\")
				{
					return path;
				}

				return GetExeDirectoryAbsolute(); // bad path
			}

			// all pad paths default to EXE
			return GetExeDirectoryAbsolute();
		}

		public static string RemoveParents(string path, string workingpath)
		{
			// determines number of parents, then removes directories from working path, return absolute path result
			// Ex: "..\..\Bob\", "C:\Projects\Emulators\Bizhawk" will return "C:\Projects\Bob\" 
			int x = NumParentDirectories(path);
			if (x > 0)
			{
				int y = path.HowMany("..\\");
				int z = workingpath.HowMany("\\");
				if (y >= z)
				{
					//Return drive letter only, working path must be absolute?
				}

				return string.Empty;
			}

			return path;
		}

		public static int NumParentDirectories(string path)
		{
			// determine the number of parent directories in path and return result
			int x = path.HowMany('\\');
			if (x > 0)
			{
				return path.HowMany("..\\");
			}

			return 0;
		}

		public static bool IsRecent(string path)
		{
			return path == "%recent%";
		}

		public static string GetLuaPath()
		{
			return MakeAbsolutePath(Global.Config.PathEntries.LuaPathFragment, null);
		}

		// Decides if a path is non-empty, not . and not .\
		private static bool PathIsSet(string path)
		{
			if (!string.IsNullOrWhiteSpace(path))
			{
				return path != "." && path != ".\\";
			}

			return false;
		}

		public static string GetRomsPath(string sysID)
		{
			if (Global.Config.UseRecentForROMs)
			{
				return Environment.SpecialFolder.Recent.ToString();
			}

			var path = Global.Config.PathEntries[sysID, "ROM"];

			if (path == null || !PathIsSet(path.Path))
			{
				path = Global.Config.PathEntries["Global", "ROM"];

				if (path != null && PathIsSet(path.Path))
				{
					return MakeAbsolutePath(path.Path, null);
				}
			}

			return MakeAbsolutePath(path.Path, sysID);
		}

		public static string RemoveInvalidFileSystemChars(string name)
		{
			var newStr = name;
			var chars = Path.GetInvalidFileNameChars();
			return chars.Aggregate(newStr, (current, c) => current.Replace(c.ToString(), string.Empty));
		}

		public static string FilesystemSafeName(GameInfo game)
		{
			var filesystemSafeName = game.Name
				.Replace("|", "+")
				.Replace(":", " -") // adelikat - Path.GetFileName scraps everything to the left of a colon unfortunately, so we need this hack here
				.Replace("\"", ""); // adelikat - Ivan Ironman Stewart's Super Off-Road has quotes in game name

			// zero 06-nov-2015 - regarding the below, i changed my mind. for libretro i want subdirectories here.
			var filesystemDir = Path.GetDirectoryName(filesystemSafeName);
			filesystemSafeName = Path.GetFileName(filesystemSafeName);

			filesystemSafeName = RemoveInvalidFileSystemChars(filesystemSafeName);

			// zero 22-jul-2012 - i dont think this is used the same way it used to. game.Name shouldnt be a path, so this stuff is illogical.
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

		public static string SaveRamPath(GameInfo game)
		{
			var name = FilesystemSafeName(game);
			if (Global.MovieSession.Movie.IsActive)
			{
				name += "." + Path.GetFileNameWithoutExtension(Global.MovieSession.Movie.Filename);
			}

			var pathEntry = Global.Config.PathEntries[game.System, "Save RAM"] ??
							Global.Config.PathEntries[game.System, "Base"];

			return Path.Combine(MakeAbsolutePath(pathEntry.Path, game.System), name) + ".SaveRAM";
		}

		public static string RetroSaveRAMDirectory(GameInfo game)
		{
			//hijinx here to get the core name out of the game name
			var name = FilesystemSafeName(game);
			name = Path.GetDirectoryName(name);
			if (name == "") name = FilesystemSafeName(game);

			if (Global.MovieSession.Movie.IsActive)
			{
				name = Path.Combine(name, "movie-" + Path.GetFileNameWithoutExtension(Global.MovieSession.Movie.Filename));
			}

			var pathEntry = Global.Config.PathEntries[game.System, "Save RAM"] ??
							Global.Config.PathEntries[game.System, "Base"];

			return Path.Combine(MakeAbsolutePath(pathEntry.Path, game.System), name);
		}


		public static string RetroSystemPath(GameInfo game)
		{
			//hijinx here to get the core name out of the game name
			var name = FilesystemSafeName(game);
			name = Path.GetDirectoryName(name);
			if(name == "") name = FilesystemSafeName(game);

			var pathEntry = Global.Config.PathEntries[game.System, "System"] ??
							Global.Config.PathEntries[game.System, "Base"];

			return Path.Combine(MakeAbsolutePath(pathEntry.Path, game.System), name);
		}

		public static string GetGameBasePath(GameInfo game)
		{
			var name = FilesystemSafeName(game);

			var pathEntry = Global.Config.PathEntries[game.System, "Base"];
			return MakeAbsolutePath(pathEntry.Path, game.System);
		}

		public static string GetSaveStatePath(GameInfo game)
		{
			var pathEntry = Global.Config.PathEntries[game.System, "Savestates"] ??
							Global.Config.PathEntries[game.System, "Base"];

			return MakeAbsolutePath(pathEntry.Path, game.System);
		}

		public static string SaveStatePrefix(GameInfo game)
		{
			var name = FilesystemSafeName(game);

			// Neshawk and Quicknes have incompatible savestates, store the name to keep them separate
			if (Global.Emulator.SystemId == "NES")
			{
				name += "." + Global.Emulator.Attributes().CoreName;
			}

			// Bsnes profiles have incompatible savestates so save the profile name
			if (Global.Emulator is LibsnesCore)
			{
				name += "." + (Global.Emulator as LibsnesCore).CurrentProfile;
			}

			if (Global.Emulator.SystemId == "GBA")
			{
				name += "." + Global.Emulator.Attributes().CoreName;
			}

			if (Global.MovieSession.Movie.IsActive)
			{
				name += "." + Path.GetFileNameWithoutExtension(Global.MovieSession.Movie.Filename);
			}

			var pathEntry = Global.Config.PathEntries[game.System, "Savestates"] ??
							Global.Config.PathEntries[game.System, "Base"];

			return Path.Combine(MakeAbsolutePath(pathEntry.Path, game.System), name);
		}

		public static string GetCheatsPath(GameInfo game)
		{
			var pathEntry = Global.Config.PathEntries[game.System, "Cheats"] ??
							Global.Config.PathEntries[game.System, "Base"];

			return MakeAbsolutePath(pathEntry.Path, game.System);
		}

		public static string GetPathType(string system, string type)
		{
			var path = PathManager.GetPathEntryWithFallback(type, system).Path;
			return MakeAbsolutePath(path, system);
		}

		public static string ScreenshotPrefix(GameInfo game)
		{
			var name = FilesystemSafeName(game);

			var pathEntry = Global.Config.PathEntries[game.System, "Screenshots"] ??
							Global.Config.PathEntries[game.System, "Base"];

			return Path.Combine(MakeAbsolutePath(pathEntry.Path, game.System), name);
		}

		/// <summary>
		/// Takes an absolute path and attempts to convert it to a relative, based on the system, 
		/// or global base if no system is supplied, if it is not a subfolder of the base, it will return the path unaltered
		/// </summary>
		/// <param name="absolutePath"></param>
		/// <param name="system"></param>
		/// <returns></returns>
		public static string TryMakeRelative(string absolutePath, string system = null)
		{
			var parentPath = string.IsNullOrWhiteSpace(system) ?
				GetBasePathAbsolute() :
				MakeAbsolutePath(GetPlatformBase(system), system);

			if (IsSubfolder(parentPath, absolutePath))
			{
				return absolutePath.Replace(parentPath, ".");
			}

			return absolutePath;
		}

		public static string MakeRelativeTo(string absolutePath, string basePath)
		{
			if (IsSubfolder(basePath, absolutePath))
			{
				return absolutePath.Replace(basePath, ".");
			}

			return absolutePath;
		}

		//http://stackoverflow.com/questions/3525775/how-to-check-if-directory-1-is-a-subdirectory-of-dir2-and-vice-versa
		public static bool IsSubfolder(string parentPath, string childPath)
		{
			var parentUri = new Uri(parentPath);

			var childUri = new DirectoryInfo(childPath).Parent;

			while (childUri != null)
			{
				if (new Uri(childUri.FullName) == parentUri)
				{
					return true;
				}

				childUri = childUri.Parent;
			}

			return false;
		}

		/// <summary>
		/// Don't only valid system ids to system ID, pathType is ROM, Screenshot, etc
		/// Returns the desired path, if does not exist, returns platform base, else it returns base
		/// </summary>
		/// <param name="pathType"></param>
		/// <param name="systemID"></param>
		public static PathEntry GetPathEntryWithFallback(string pathType, string systemID)
		{
			var entry = Global.Config.PathEntries[systemID, pathType];
			if (entry == null)
			{
				entry = Global.Config.PathEntries[systemID, "Base"];
			}

			if (entry == null)
			{
				entry = Global.Config.PathEntries["Global", "Base"];
			}

			return entry;
		}
	}
}
