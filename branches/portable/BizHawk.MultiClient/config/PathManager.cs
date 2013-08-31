using System;
using System.Linq;
using System.IO;
using System.Reflection;

namespace BizHawk.MultiClient
{
	public static class PathManager
	{
		public static string GetExeDirectoryAbsolute()
		{
			var uri = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
			string module = uri.LocalPath + System.Web.HttpUtility.UrlDecode(uri.Fragment);
			return Path.GetDirectoryName(module);
		}

		/// <summary>
		/// Makes a path relative to the %exe% dir
		/// </summary>
		public static string MakeProgramRelativePath(string path) { return MakeAbsolutePath("%exe%/" + path, null); }

		/// <summary>
		/// The location of the default INI file
		/// </summary>
		public static string DefaultIniPath
		{
			get 
			{
				string blah = MakeProgramRelativePath("config.ini");
				return blah;
			} 
		}

		/// <summary>
		/// Gets absolute base as derived from EXE
		/// </summary>
		/// <returns></returns>
		public static string GetBasePathAbsolute()
		{
			if (Global.Config.PathEntries.GlobalBase.Length < 1) //If empty, then EXE path
				return GetExeDirectoryAbsolute();

			if (Global.Config.PathEntries.GlobalBase.Length >= 5 &&
				Global.Config.PathEntries.GlobalBase.Substring(0, 5) == "%exe%")
				return GetExeDirectoryAbsolute();
			if (Global.Config.PathEntries.GlobalBase[0] == '.')
			{
				if (Global.Config.PathEntries.GlobalBase.Length == 1)
					return GetExeDirectoryAbsolute();
				else
				{
					if (Global.Config.PathEntries.GlobalBase.Length == 2 &&
						Global.Config.PathEntries.GlobalBase == "."+Path.DirectorySeparatorChar)
						return GetExeDirectoryAbsolute();
					else
					{
						string tmp = Global.Config.PathEntries.GlobalBase;
						tmp = tmp.Remove(0, 1);
						tmp = tmp.Insert(0, GetExeDirectoryAbsolute());
						return tmp;
					}
				}
			}

			if (Global.Config.PathEntries.GlobalBase.Substring(0, 2) == "..")
				return RemoveParents(Global.Config.PathEntries.GlobalBase, GetExeDirectoryAbsolute());

			//In case of error, return EXE path
			return GetExeDirectoryAbsolute();
		}

		public static string GetPlatformBase(string system)
		{
			return Global.Config.PathEntries[system, "Base"].Path;
		}

		public static string StandardFirmwareName(string name)
		{
			return Path.Combine(MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPath, null), name);
		}

		public static string MakeAbsolutePath(string path, string system)
		{
			//Hack
			if (system == "Global")
			{
				system = null;
			}

			//This function translates relative path and special identifiers in absolute paths

			if (path.Length < 1)
				return GetBasePathAbsolute();

			if (path == "%recent%")
			{
				return Environment.SpecialFolder.Recent.ToString();
			}

			if (path.Length >= 5 && path.Substring(0, 5) == "%exe%")
			{
				if (path.Length == 5)
					return GetExeDirectoryAbsolute();
				else
				{
					string tmp = path.Remove(0, 5);
					tmp = tmp.Insert(0, GetExeDirectoryAbsolute());
					return tmp;
				}
			}

			if (path[0] == '.')
			{
				if (!String.IsNullOrWhiteSpace(system))
				{
					path = path.Remove(0, 1);
					path = path.Insert(0, GetPlatformBase(system));
				}
				if (path.Length == 1)
					return GetBasePathAbsolute();
				else
				{
					if (path[0] == '.')
					{
						path = path.Remove(0, 1);
						path = path.Insert(0, GetBasePathAbsolute());
					}

					return path;
				}
			}

			//If begins wtih .. do alorithm to determine how many ..\.. combos and deal with accordingly, return drive letter only if too many ..

#if WINDOWS
			if ((path[0] > 'A' && path[0] < 'Z') || (path[0] > 'a' && path[0] < 'z'))
			{
				//C:\
				if (path.Length > 2 && path[1] == ':' && path[2] == Path.DirectorySeparatorChar)
					return path;
				else
				{
					//file:\ is an acceptable path as well, and what FileBrowserDialog returns
					if (path.Length >= 6 && path.Substring(0, 6) == "file:"+Path.DirectorySeparatorChar)
						return path;
					else
						return GetExeDirectoryAbsolute(); //bad path
				}
			}
#else
			if(path[0] == Path.DirectorySeparatorChar)
				return path; //If it starts with /, it's probably a valid Unix absolute path.
#endif

			//all pad paths default to EXE
			return GetExeDirectoryAbsolute();
		}

		public static string RemoveParents(string path, string workingpath)
		{
			//determines number of parents, then removes directories from working path, return absolute path result
			//Ex: "..\..\Bob\", "C:\Projects\Emulators\Bizhawk" will return "C:\Projects\Bob\" 
			int x = NumParentDirectories(path);
			if (x > 0)
			{
				int y = StringHelpers.HowMany(path, ".."+Path.DirectorySeparatorChar);
				int z = StringHelpers.HowMany(workingpath, Path.DirectorySeparatorChar);
				if (y >= z)
				{
					//Return drive letter only, working path must be absolute?
				}
				return "";
			}
			else return path;
		}

		public static int NumParentDirectories(string path)
		{
			//determine the number of parent directories in path and return result
			int x = StringHelpers.HowMany(path, Path.DirectorySeparatorChar);
			if (x > 0)
			{
				return StringHelpers.HowMany(path, ".."+Path.DirectorySeparatorChar);
			}
			return 0;
		}

		public static bool IsRecent(string path)
		{
			return path == "%recent%";
		}

		public static string GetLuaPath()
		{
			return MakeAbsolutePath(Global.Config.PathEntries.LuaPath, null);
		}

		public static string GetRomsPath(string sysID)
		{
			if (Global.Config.UseRecentForROMs)
			{
				return Environment.SpecialFolder.Recent.ToString();
			}

			PathEntry path = Global.Config.PathEntries[sysID, "ROM"];

			if (path == null)
			{
				path = Global.Config.PathEntries[sysID, "Base"];
			}

			return MakeAbsolutePath(path.Path, sysID);
		}

		public static string RemoveInvalidFileSystemChars(string name)
		{
			string newStr = name;
			char[] chars = Path.GetInvalidFileNameChars();
			return chars.Aggregate(newStr, (current, c) => current.Replace(c.ToString(), ""));
		}

		public static string FilesystemSafeName(GameInfo game)
		{
			string filesystemSafeName = game.Name.Replace("|", "+");
			filesystemSafeName = RemoveInvalidFileSystemChars(filesystemSafeName);
			//zero 22-jul-2012 - i dont think this is used the same way it used to. game.Name shouldnt be a path, so this stuff is illogical.
			//if game.Name is a path, then someone shouldve made it not-a-path already.
			//return Path.Combine(Path.GetDirectoryName(filesystemSafeName), Path.GetFileNameWithoutExtension(filesystemSafeName));
			return filesystemSafeName;
		}

		public static string SaveRamPath(GameInfo game)
		{
			string name = FilesystemSafeName(game);
			if (Global.MovieSession.Movie.IsActive)
			{
				name += "." + Path.GetFileNameWithoutExtension(Global.MovieSession.Movie.Filename);
			}

			PathEntry pathEntry = Global.Config.PathEntries[game.System, "Save RAM"];

			if (pathEntry == null)
			{
				pathEntry = Global.Config.PathEntries[game.System, "Base"];
			}

			return Path.Combine(MakeAbsolutePath(pathEntry.Path, game.System), name) + ".SaveRAM";
		}

		public static string GetSaveStatePath(GameInfo game)
		{
			PathEntry pathEntry = Global.Config.PathEntries[game.System, "Savestates"];

			if (pathEntry == null)
			{
				pathEntry = Global.Config.PathEntries[game.System, "Base"];
			}

			return MakeAbsolutePath(pathEntry.Path, game.System);
		}

		public static string SaveStatePrefix(GameInfo game)
		{
			string name = FilesystemSafeName(game);
			
			if (Global.Config.BindSavestatesToMovies && Global.MovieSession.Movie.IsActive)
			{
				name += "." + Path.GetFileNameWithoutExtension(Global.MovieSession.Movie.Filename);
			}

			PathEntry pathEntry = Global.Config.PathEntries[game.System, "Savestates"];

			if (pathEntry == null)
			{
				pathEntry = Global.Config.PathEntries[game.System, "Base"];
			}

			return Path.Combine(MakeAbsolutePath(pathEntry.Path, game.System), name);
		}

		public static string ScreenshotPrefix(GameInfo game)
		{
			string name = FilesystemSafeName(game);

			PathEntry pathEntry = Global.Config.PathEntries[game.System, "Screenshots"];

			if (pathEntry == null)
			{
				pathEntry = Global.Config.PathEntries[game.System, "Base"];
			}

			return Path.Combine(MakeAbsolutePath(pathEntry.Path, game.System), name);
		}

		/// <summary>
		/// Takes an absolute path and attempts to convert it to a relative, based on the system, 
		/// or global base if no system is supplied, if it is not a subfolder of the base, it will return the path unaltered
		/// </summary>
		/// <param name="absolute_path"></param>
		/// <param name="system"></param>
		/// <returns></returns>
		public static string TryMakeRelative(string absolute_path, string system = null)
		{
			string parent_path;
			if (String.IsNullOrWhiteSpace(system))
			{
				parent_path = GetBasePathAbsolute();
			}
			else
			{
				parent_path = MakeAbsolutePath(GetPlatformBase(system), system);
			}

			if (IsSubfolder(parent_path, absolute_path))
			{
				return absolute_path.Replace(parent_path, ".");
			}
			else
			{
				return absolute_path;
			}
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
	}
}
