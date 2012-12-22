using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace BizHawk.MultiClient
{
	public static class PathManager
	{
		public static string GetExeDirectoryAbsolute()
		{
			//var uri = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
			//string module = uri.LocalPath + System.Web.HttpUtility.UrlDecode(uri.Fragment);
			//return Path.GetDirectoryName(module);
			//zero 21-dec-2012 - reuse code elsewhere and remove system.web dependency
			//return Assembly.GetEntryAssembly().GetDirectory();

			// no no no
			// this must be available entirely with multiclient code, as it's used to set up the AssemblyResolve event that loads util and emulation
			var asm = Assembly.GetEntryAssembly();
			string codeBase = asm.CodeBase;
			UriBuilder uri = new UriBuilder(codeBase);
			string path = Uri.UnescapeDataString(uri.Path);
			return Path.GetDirectoryName(path);
		}

		/// <summary>
		/// Makes a path relative to the %exe% dir
		/// </summary>
		public static string MakeProgramRelativePath(string path) { return MakeAbsolutePath("%exe%/" + path, ""); }

		/// <summary>
		/// The location of the default INI file
		/// </summary>
		public static string DefaultIniPath { get { return MakeProgramRelativePath("config.ini"); } }

		/// <summary>
		/// Gets absolute base as derived from EXE
		/// </summary>
		/// <returns></returns>
		public static string GetBasePathAbsolute()
		{
			if (Global.Config.BasePath.Length < 1) //If empty, then EXE path
				return GetExeDirectoryAbsolute();

			if (Global.Config.BasePath.Length >= 5 &&
				Global.Config.BasePath.Substring(0, 5) == "%exe%")
				return GetExeDirectoryAbsolute();
			if (Global.Config.BasePath[0] == '.')
			{
				if (Global.Config.BasePath.Length == 1)
					return GetExeDirectoryAbsolute();
				else
				{
					if (Global.Config.BasePath.Length == 2 &&
						Global.Config.BasePath == ".\\")
						return GetExeDirectoryAbsolute();
					else
					{
						string tmp = Global.Config.BasePath;
						tmp = tmp.Remove(0, 1);
						tmp = tmp.Insert(0, GetExeDirectoryAbsolute());
						return tmp;
					}
				}
			}

			if (Global.Config.BasePath.Substring(0, 2) == "..")
				return RemoveParents(Global.Config.BasePath, GetExeDirectoryAbsolute());

			//In case of error, return EXE path
			return GetExeDirectoryAbsolute();
		}

		public static string GetPlatformBase(string system)
		{
			switch (system)
			{
				case "C64":
					return Global.Config.BaseC64;
				case "PSX":
					return Global.Config.BasePSX;
				case "INTV":
					return Global.Config.BaseINTV;
				case "A26":
					return Global.Config.BaseAtari2600;
				case "A78":
					return Global.Config.BaseAtari7800;
				case "NES":
					return Global.Config.BaseNES;
				case "SG":
					return Global.Config.BaseSG;
				case "GG":
					return Global.Config.BaseGG;
				case "SMS":
					return Global.Config.BaseSMS;
				case "SGX":
				case "PCE":
				case "PCECD":
					return Global.Config.BasePCE;
				case "TI83":
					return Global.Config.BaseTI83;
				case "GEN":
					return Global.Config.BaseGenesis;
				case "GB":
					return Global.Config.BaseGameboy;
				case "SNES":
					return Global.Config.BaseSNES;
				case "Coleco":
					return Global.Config.BaseCOL;
				case "GBA":
					return Global.Config.BaseGBA;
				case "NULL":
				default:
					return "";
			}
		}

		public static string MakeAbsolutePath(string path, string system)
		{
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
				if (system.Length > 0)
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
			return MakeAbsolutePath(Global.Config.LuaPath, "");
		}

		public static string GetRomsPath(string sysID)
		{
			string path = "";

			if (Global.Config.UseRecentForROMs)
				return Environment.SpecialFolder.Recent.ToString();

			switch (sysID)
			{
				case "C64":
					path = PathManager.MakeAbsolutePath(Global.Config.PathC64ROMs, "C64");
					break;
				case "PSX":
					path = PathManager.MakeAbsolutePath(Global.Config.PathPSXROMs, "PSX");
					break;
				case "INTV":
					path = PathManager.MakeAbsolutePath(Global.Config.PathINTVROMs, "INTV");
					break;
				case "SNES":
					path = PathManager.MakeAbsolutePath(Global.Config.PathSNESROMs, "SNES");
					break;
				case "A26":
					path = PathManager.MakeAbsolutePath(Global.Config.PathAtari2600ROMs, "A26");
					break;
				case "A78":
					path = PathManager.MakeAbsolutePath(Global.Config.PathAtari7800ROMs, "A78");
					break;
				case "NES":
					path = PathManager.MakeAbsolutePath(Global.Config.PathNESROMs, "NES");
					break;
				case "SMS":
					path = PathManager.MakeAbsolutePath(Global.Config.PathSMSROMs, "SMS");
					break;
				case "SG":
					path = PathManager.MakeAbsolutePath(Global.Config.PathSGROMs, "SG");
					break;
				case "GG":
					path = PathManager.MakeAbsolutePath(Global.Config.PathGGROMs, "GG");
					break;
				case "GEN":
					path = PathManager.MakeAbsolutePath(Global.Config.PathGenesisROMs, "GEN");
					break;
				case "SFX":
				case "PCE":
				case "PCECD":
					path = PathManager.MakeAbsolutePath(Global.Config.PathPCEROMs, "PCE");
					break;
				case "GB":
					path = PathManager.MakeAbsolutePath(Global.Config.PathGBROMs, "GB");
					break;
				case "GBA":
					path = PathManager.MakeAbsolutePath(Global.Config.PathGBAROMs, "GBA");
					break;
				case "TI83":
					path = PathManager.MakeAbsolutePath(Global.Config.PathTI83ROMs, "TI83");
					break;
				case "Coleco":
					path = PathManager.MakeAbsolutePath(Global.Config.PathCOLROMs, "Coleco");
					break;
				default:
					path = PathManager.GetBasePathAbsolute();
					break;
			}

			return path;
		}

		public static string RemoveInvalidFileSystemChars(string name)
		{
			string newStr = name;
			char[] chars = Path.GetInvalidFileNameChars();
			foreach (char c in chars)
			{
				newStr = newStr.Replace(c.ToString(), "");
			}
			return newStr;
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

			switch (game.System)
			{
				case "INTV": return Path.Combine(MakeAbsolutePath(Global.Config.PathINTVSaveRAM, "INTV"), name + ".SaveRAM");
				case "SMS": return Path.Combine(MakeAbsolutePath(Global.Config.PathSMSSaveRAM, "SMS"), name + ".SaveRAM");
				case "GG": return Path.Combine(MakeAbsolutePath(Global.Config.PathGGSaveRAM, "GG"), name + ".SaveRAM");
				case "SG": return Path.Combine(MakeAbsolutePath(Global.Config.PathSGSaveRAM, "SG"), name + ".SaveRAM");
				case "SGX": return Path.Combine(MakeAbsolutePath(Global.Config.PathPCESaveRAM, "PCE"), name + ".SaveRAM");
				case "PCE": return Path.Combine(MakeAbsolutePath(Global.Config.PathPCESaveRAM, "PCE"), name + ".SaveRAM");
				case "PCECD": return Path.Combine(MakeAbsolutePath(Global.Config.PathPCESaveRAM, "PCE"), name + ".SaveRAM");
				case "GB": case "GBC": return Path.Combine(MakeAbsolutePath(Global.Config.PathGBSaveRAM, "GB"), name + ".SaveRAM");
				case "GBA": return Path.Combine(MakeAbsolutePath(Global.Config.PathGBASaveRAM, "GBA"), name + ".SaveRAM");
				case "GEN": return Path.Combine(MakeAbsolutePath(Global.Config.PathGenesisSaveRAM, "GEN"), name + ".SaveRAM");
				case "NES": return Path.Combine(MakeAbsolutePath(Global.Config.PathNESSaveRAM, "NES"), name + ".SaveRAM");
				case "TI83": return Path.Combine(MakeAbsolutePath(Global.Config.PathTI83SaveRAM, "TI83"), name + ".SaveRAM");
				case "A78": return Path.Combine(MakeAbsolutePath(Global.Config.PathAtari7800SaveRAM, "A78"), name + ".SaveRAM");
				case "SNES": return Path.Combine(MakeAbsolutePath(Global.Config.PathSNESSaveRAM, "SNES"), name + ".SaveRAM");
				case "PSX": return Path.Combine(MakeAbsolutePath(Global.Config.PathPSXSaveRAM, "PSX"), name + ".SaveRAM");
				default: return Path.Combine(GetBasePathAbsolute(), name + ".SaveRAM");
			}
		}

		public static string GetSaveStatePath(GameInfo game)
		{
			switch (game.System)
			{
				default: return GetRomsPath(game.System);
				case "INTV": return MakeAbsolutePath(Global.Config.PathINTVSavestates, "INTV");
				case "A26": return MakeAbsolutePath(Global.Config.PathAtari2600Savestates, "A26");
				case "A78": return MakeAbsolutePath(Global.Config.PathAtari7800Savestates, "A78");
				case "SMS": return MakeAbsolutePath(Global.Config.PathSMSSavestates, "SMS");
				case "GG": return MakeAbsolutePath(Global.Config.PathGGSavestates, "GG");
				case "SG": return MakeAbsolutePath(Global.Config.PathSGSavestates, "SG");
				case "SGX": return MakeAbsolutePath(Global.Config.PathPCESavestates, "PCE");
				case "PCE": return MakeAbsolutePath(Global.Config.PathPCESavestates, "PCE");
				case "PCECD": return MakeAbsolutePath(Global.Config.PathPCESavestates, "PCE");
				case "GB": case "GBC": return MakeAbsolutePath(Global.Config.PathGBSavestates, "GB");
				case "GBA": return MakeAbsolutePath(Global.Config.PathGBASavestates, "GBA");
				case "GEN": return MakeAbsolutePath(Global.Config.PathGenesisSavestates, "GEN");
				case "NES": return MakeAbsolutePath(Global.Config.PathNESSavestates, "NES");
				case "TI83": return MakeAbsolutePath(Global.Config.PathTI83Savestates, "TI83");
				case "SNES": return MakeAbsolutePath(Global.Config.PathSNESSavestates, "SNES");
				case "PSX": return MakeAbsolutePath(Global.Config.PathPSXSavestates, "PSX");
				case "C64": return MakeAbsolutePath(Global.Config.PathC64Savestates, "C64");
				case "Coleco": return MakeAbsolutePath(Global.Config.PathCOLSavestates, "Coleco");
			}
		}

		public static string SaveStatePrefix(GameInfo game)
		{
			string name = FilesystemSafeName(game);
			
			if (Global.Config.BindSavestatesToMovies && Global.MovieSession.Movie.IsActive)
			{
				name += "." + Path.GetFileNameWithoutExtension(Global.MovieSession.Movie.Filename);
			}
			
			switch (game.System)
			{
				case "INTV": return Path.Combine(MakeAbsolutePath(Global.Config.PathINTVSavestates, "INTV"), name);
				case "A26": return Path.Combine(MakeAbsolutePath(Global.Config.PathAtari2600Savestates, "A26"), name);
				case "A78": return Path.Combine(MakeAbsolutePath(Global.Config.PathAtari7800Savestates, "A78"), name);
				case "SMS": return Path.Combine(MakeAbsolutePath(Global.Config.PathSMSSavestates, "SMS"), name);
				case "GG": return Path.Combine(MakeAbsolutePath(Global.Config.PathGGSavestates, "GG"), name);
				case "SG": return Path.Combine(MakeAbsolutePath(Global.Config.PathSGSavestates, "SG"), name);
				case "SGX": return Path.Combine(MakeAbsolutePath(Global.Config.PathPCESavestates, "PCE"), name);
				case "PCE": return Path.Combine(MakeAbsolutePath(Global.Config.PathPCESavestates, "PCE"), name);
				case "PCECD": return Path.Combine(MakeAbsolutePath(Global.Config.PathPCESavestates, "PCE"), name);
				case "GB": case "GBC": return Path.Combine(MakeAbsolutePath(Global.Config.PathGBSavestates, "GB"), name);
				case "GBA": return Path.Combine(MakeAbsolutePath(Global.Config.PathGBASavestates, "GBA"), name);
				case "GEN": return Path.Combine(MakeAbsolutePath(Global.Config.PathGenesisSavestates, "GEN"), name);
				case "NES": return Path.Combine(MakeAbsolutePath(Global.Config.PathNESSavestates, "NES"), name);
				case "TI83": return Path.Combine(MakeAbsolutePath(Global.Config.PathTI83Savestates, "TI83"), name);
				case "SNES": return Path.Combine(MakeAbsolutePath(Global.Config.PathSNESSavestates, "SNES"), name);
				case "PSX": return Path.Combine(MakeAbsolutePath(Global.Config.PathPSXSavestates, "PSX"), name);
				case "C64": return Path.Combine(MakeAbsolutePath(Global.Config.PathC64Savestates, "C64"), name);
				case "Coleco": return Path.Combine(MakeAbsolutePath(Global.Config.PathCOLSavestates, "Coleco"), name);
			}
			return "";
		}

		public static string ScreenshotPrefix(GameInfo game)
		{
			string name = FilesystemSafeName(game);
			switch (game.System)
			{
				case "INTV": return Path.Combine(MakeAbsolutePath(Global.Config.PathINTVScreenshots, "INTV"), name);
				case "A26": return Path.Combine(MakeAbsolutePath(Global.Config.PathAtari2600Screenshots, "A26"), name);
				case "A78": return Path.Combine(MakeAbsolutePath(Global.Config.PathAtari7800Screenshots, "A78"), name);
				case "SMS": return Path.Combine(MakeAbsolutePath(Global.Config.PathSMSScreenshots, "SMS"), name);
				case "GG": return Path.Combine(MakeAbsolutePath(Global.Config.PathGGScreenshots, "GG"), name);
				case "SG": return Path.Combine(MakeAbsolutePath(Global.Config.PathSGScreenshots, "SG"), name);
				case "SGX": return Path.Combine(MakeAbsolutePath(Global.Config.PathPCEScreenshots, "PCE"), name);
				case "PCE": return Path.Combine(MakeAbsolutePath(Global.Config.PathPCEScreenshots, "PCE"), name);
				case "PCECD": return Path.Combine(MakeAbsolutePath(Global.Config.PathPCEScreenshots, "PCE"), name);
				case "GB": case "GBC": return Path.Combine(MakeAbsolutePath(Global.Config.PathGBScreenshots, "GB"), name);
				case "GBA": return Path.Combine(MakeAbsolutePath(Global.Config.PathGBAScreenshots, "GBA"), name);
				case "GEN": return Path.Combine(MakeAbsolutePath(Global.Config.PathGenesisScreenshots, "GEN"), name);
				case "NES": return Path.Combine(MakeAbsolutePath(Global.Config.PathNESScreenshots, "NES"), name);
				case "TI83": return Path.Combine(MakeAbsolutePath(Global.Config.PathTI83Screenshots, "TI83"), name);
				case "SNES": return Path.Combine(MakeAbsolutePath(Global.Config.PathSNESScreenshots, "SNES"), name);
				case "PSX": return Path.Combine(MakeAbsolutePath(Global.Config.PathPSXScreenshots, "PSX"), name);
				case "Coleco": return Path.Combine(MakeAbsolutePath(Global.Config.PathCOLScreenshots, "Coleco"), name);
			}
			return "";
		}
	}
}
