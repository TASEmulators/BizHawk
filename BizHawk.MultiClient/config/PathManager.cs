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
		public static string MakeProgramRelativePath(string path) { return MakeAbsolutePath("%exe%/" + path); }

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
						Global.Config.PathEntries.GlobalBase == ".\\")
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
			if (system == "SGX" || system == "PCECD")
			{
				system = "PCE";
			}
			return Global.Config.PathEntries[system, "Base"].Path;
		}

		public static string StandardFirmwareName(string name)
		{
			return Path.Combine(MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPath), name);
		}

		public static string MakeAbsolutePath(string path, string system = null)
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

			if ((path[0] > 'A' && path[0] < 'Z') || (path[0] > 'a' && path[0] < 'z'))
			{
				//C:\
				if (path.Length > 2 && path[1] == ':' && path[2] == '\\')
					return path;
				else
				{
					//file:\ is an acceptable path as well, and what FileBrowserDialog returns
					if (path.Length >= 6 && path.Substring(0, 6) == "file:\\")
						return path;
					else
						return GetExeDirectoryAbsolute(); //bad path
				}
			}

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
				int y = StringHelpers.HowMany(path, "..\\");
				int z = StringHelpers.HowMany(workingpath, "\\");
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
			int x = StringHelpers.HowMany(path, '\\');
			if (x > 0)
			{
				return StringHelpers.HowMany(path, "..\\");
			}
			return 0;
		}

		public static bool IsRecent(string path)
		{
			if (path == "%recent%")
				return true;
			else
				return false;
		}

		public static string GetLuaPath()
		{
			return MakeAbsolutePath(Global.Config.PathEntries.LuaPath);
		}

		public static string GetRomsPath(string sysID)
		{
			if (Global.Config.UseRecentForROMs)
			{
				return Environment.SpecialFolder.Recent.ToString();
			}

			if (sysID == "SGX" || sysID == "PCECD") //Yucky
			{
				sysID = "PCE";
			}
			else if (sysID == "NULL")
			{
				sysID = "Global";
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

			string sysId = "";
			switch (game.System)
			{
				case "SGX":
				case "PCECD":
					sysId = "PCE";
					break;
				case "NULL":
					sysId = "Global";
					break;
				default:
					sysId = game.System;
					break;
			}

			PathEntry pathEntry = Global.Config.PathEntries[sysId, "Save RAM"];

			if (pathEntry == null)
			{
				pathEntry = Global.Config.PathEntries[game.System, "Base"];
			}

			return Path.Combine(MakeAbsolutePath(pathEntry.Path), name);
		}

		public static string GetSaveStatePath(GameInfo game)
		{
			string sysId = "";
			switch (game.System)
			{
				case "SGX":
				case "PCECD":
					sysId = "PCE";
					break;
				case "NULL":
					sysId = "Global";
					break;
				default:
					sysId = game.System;
					break;
			}

			PathEntry pathEntry = Global.Config.PathEntries[sysId, "Savestates"];

			if (pathEntry == null)
			{
				pathEntry = Global.Config.PathEntries[game.System, "Base"];
			}

			return MakeAbsolutePath(pathEntry.Path, sysId == "Global" ? null : sysId);
		}

		public static string SaveStatePrefix(GameInfo game)
		{
			string name = FilesystemSafeName(game);
			
			if (Global.Config.BindSavestatesToMovies && Global.MovieSession.Movie.IsActive)
			{
				name += "." + Path.GetFileNameWithoutExtension(Global.MovieSession.Movie.Filename);
			}

			string sysId = "";
			switch (game.System)
			{
				case "SGX":
				case "PCECD":
					sysId = "PCE";
					break;
				case "NULL":
					sysId = "Global";
					break;
				default:
					sysId = game.System;
					break;
			}

			PathEntry pathEntry = Global.Config.PathEntries[sysId, "Savestates"];

			if (pathEntry == null)
			{
				pathEntry = Global.Config.PathEntries[sysId, "Base"];
			}

			return Path.Combine(MakeAbsolutePath(pathEntry.Path, sysId == "Global" ? null : sysId), name);
		}

		public static string ScreenshotPrefix(GameInfo game)
		{
			string name = FilesystemSafeName(game);

			string sysId = "";
			switch (game.System)
			{
				case "SGX":
				case "PCECD":
					sysId = "PCE";
					break;
				case "NULL":
					sysId = "Global";
					break;
				default:
					sysId = game.System;
					break;
			}

			PathEntry pathEntry = Global.Config.PathEntries[sysId, "Screenshots"];

			if (pathEntry == null)
			{
				pathEntry = Global.Config.PathEntries[game.System, "Base"];
			}

			return Path.Combine(MakeAbsolutePath(pathEntry.Path), name);
		}
	}
}
