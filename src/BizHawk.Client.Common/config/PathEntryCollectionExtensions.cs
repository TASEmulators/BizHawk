using System.IO;
using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public static class PathEntryExtensions
	{
		/// <summary>
		/// Returns the base path of the given system.
		/// If the system can not be found, an empty string is returned
		/// </summary>
		public static string BaseFor(this PathEntryCollection collection, string systemId)
		{
			return string.IsNullOrWhiteSpace(systemId)
				? ""
				: collection[systemId, "Base"]?.Path ?? "";
		}

		public static string GlobalBaseAbsolutePath(this PathEntryCollection collection)
		{
			var globalBase = collection[PathEntryCollection.GLOBAL, "Base"].Path;

			// if %exe% prefixed then substitute exe path and repeat
			if (globalBase.StartsWith("%exe%", StringComparison.InvariantCultureIgnoreCase))
			{
				globalBase = PathUtils.ExeDirectoryPath + globalBase.Substring(5);
			}

			// absolute paths get returned without change
			// (this is done after keyword substitution to avoid problems though)
			if (globalBase.IsAbsolute())
			{
				return globalBase;
			}

			// non-absolute things are relative to exe path
			globalBase = Path.Combine(PathUtils.ExeDirectoryPath, globalBase);
			return globalBase;
		}

		/// <summary>
		/// Returns an entry for the given system and pathType (ROM, screenshot, etc)
		/// but falls back to the base system or global system if it fails
		/// to find pathType or systemId
		/// </summary>
		public static PathEntry EntryWithFallback(this PathEntryCollection collection, string pathType, string systemId)
		{
			return (collection[systemId, pathType]
				?? collection[systemId, "Base"])
				?? collection[PathEntryCollection.GLOBAL, "Base"];
		}

		public static string AbsolutePathForType(this PathEntryCollection collection, string systemId, string type)
		{
			var path = collection.EntryWithFallback(type, systemId).Path;
			return collection.AbsolutePathFor(path, systemId);
		}

		/// <summary>
		/// Returns an absolute path for the given relative path.
		/// If provided, the systemId will be used to generate the path.
		/// Wildcards are supported.
		/// Logic will fallback until an absolute path is found,
		/// using Global Base as a last resort
		/// </summary>
		public static string AbsolutePathFor(this PathEntryCollection collection, string path, string systemId)
		{
			// warning: supposedly Path.GetFullPath accesses directories (and needs permissions)
			// if this poses a problem, we need to paste code from .net or mono sources and fix them to not pose problems, rather than homebrew stuff
			return Path.GetFullPath(collection.AbsolutePathForInner(path, systemId));
		}

		private static string AbsolutePathForInner(this PathEntryCollection collection,  string path, string systemId)
		{
			// Hack
			if (systemId == "Global")
			{
				return collection.AbsolutePathForInner(path, systemId: null);
			}

			// This function translates relative path and special identifiers in absolute paths
			if (path.Length < 1)
			{
				return collection.GlobalBaseAbsolutePath();
			}

			if (path == "%recent%") return PathUtils.SpecialRecentsDir;

			if (path.StartsWithOrdinal("%exe%"))
			{
				return PathUtils.ExeDirectoryPath + path.Substring(5);
			}

			if (path.StartsWithOrdinal("%rom%"))
			{
				return collection.LastRomPath + path.Substring(5);
			}

			if (path[0] == '.')
			{
				if (!string.IsNullOrWhiteSpace(systemId))
				{
					path = path.Remove(0, 1);
					path = path.Insert(0, collection.BaseFor(systemId));
				}

				if (path.Length == 1)
				{
					return collection.GlobalBaseAbsolutePath();
				}

				if (path[0] == '.')
				{
					path = path.Remove(0, 1);
					path = path.Insert(0, collection.GlobalBaseAbsolutePath());
				}

				return path;
			}

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
			bool isAbsolute = path.IsAbsolute();
#else
			bool isAbsolute;
			try
			{
				isAbsolute = path.IsAbsolute();
			}
			catch
			{
				isAbsolute = false;
			}
#endif

			//handling of initial .. was removed (Path.GetFullPath can handle it)
			//handling of file:// or file:\\ was removed  (can Path.GetFullPath handle it? not sure)

			// all bad paths default to EXE
			return isAbsolute ? path : PathUtils.ExeDirectoryPath;
		}

		public static string MovieAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection[PathEntryCollection.GLOBAL, "Movies"].Path;
			return collection.AbsolutePathFor(path, null);
		}

		public static string MovieBackupsAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection[PathEntryCollection.GLOBAL, "Movie backups"].Path;
			return collection.AbsolutePathFor(path, null);
		}

		public static string AvAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection[PathEntryCollection.GLOBAL, "A/V Dumps"].Path;
			return collection.AbsolutePathFor(path, null);
		}

		public static string LuaAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection[PathEntryCollection.GLOBAL, "Lua"].Path;
			return collection.AbsolutePathFor(path, null);
		}

		public static string FirmwareAbsolutePath(this PathEntryCollection collection)
		{
			return collection.AbsolutePathFor(collection.FirmwarePathFragment, null);
		}

		public static string LogAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection.ResolveToolsPath(collection[PathEntryCollection.GLOBAL, "Debug Logs"].Path);
			return collection.AbsolutePathFor(path, null);
		}

		public static string WatchAbsolutePath(this PathEntryCollection collection)
		{
			var path = 	collection.ResolveToolsPath(collection[PathEntryCollection.GLOBAL, "Watch (.wch)"].Path);
			return collection.AbsolutePathFor(path, null);
		}

		public static string ToolsAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection[PathEntryCollection.GLOBAL, "Tools"].Path;
			return collection.AbsolutePathFor(path, null);
		}

		public static string ExternalToolsAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection[PathEntryCollection.GLOBAL, "External Tools"].Path;
			return collection.AbsolutePathFor(path, null);
		}

		public static string MultiDiskAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection.ResolveToolsPath(collection[PathEntryCollection.GLOBAL, "Multi-Disk Bundles"].Path);
			return collection.AbsolutePathFor(path, null);
		}

		public static string RomAbsolutePath(this PathEntryCollection collection, string systemId = null)
		{
			if (string.IsNullOrWhiteSpace(systemId))
			{
				return collection.AbsolutePathFor(collection[PathEntryCollection.GLOBAL, "ROM"].Path, PathEntryCollection.GLOBAL);
			}

			if (collection.UseRecentForRoms) return /*PathUtils.SpecialRecentsDir*/string.Empty; // instructs OpenFileDialog to use the dir of the most recently-opened file, a behaviour consistent with previous versions, even though it may never have been intended; this system will be overhauled when adding #1574

			var path = collection[systemId, "ROM"];

			if (!path.Path.PathIsSet())
			{
				path = collection[PathEntryCollection.GLOBAL, "ROM"];

				if (path.Path.PathIsSet())
				{
					return collection.AbsolutePathFor(path.Path, null);
				}
			}

			return collection.AbsolutePathFor(path.Path, systemId);
		}

		public static string SaveRamAbsolutePath(this PathEntryCollection collection, IGameInfo game, IMovie movie)
		{
			var name = game.FilesystemSafeName();
			if (movie.IsActive())
			{
				name += $".{Path.GetFileNameWithoutExtension(movie.Filename)}";
			}

			var pathEntry = collection[game.System, "Save RAM"]
				?? collection[game.System, "Base"];

			return $"{Path.Combine(collection.AbsolutePathFor(pathEntry.Path, game.System), name)}.SaveRAM";
		}

		// Shenanigans
		public static string RetroSaveRamAbsolutePath(this PathEntryCollection collection, IGameInfo game)
		{
			var name = game.FilesystemSafeName();
			name = Path.GetDirectoryName(name);
			if (name == "")
			{
				name = game.FilesystemSafeName();
			}

			name ??= "";

			var pathEntry = collection[game.System, "Save RAM"]
				?? collection[game.System, "Base"];

			return Path.Combine(collection.AbsolutePathFor(pathEntry.Path, game.System), name);
		}

		// Shenanigans
		public static string RetroSystemAbsolutePath(this PathEntryCollection collection, IGameInfo game)
		{
			var name = game.FilesystemSafeName();
			name = Path.GetDirectoryName(name);
			if (string.IsNullOrEmpty(name))
			{
				name = game.FilesystemSafeName();
			}

			var pathEntry = collection[game.System, "System"]
				?? collection[game.System, "Base"];

			return Path.Combine(collection.AbsolutePathFor(pathEntry.Path, game.System), name);
		}

		public static string AutoSaveRamAbsolutePath(this PathEntryCollection collection, IGameInfo game, IMovie movie)
		{
			var path = collection.SaveRamAbsolutePath(game, movie);
			return path.Insert(path.Length - 8, ".AutoSaveRAM");
		}

		public static string CheatsAbsolutePath(this PathEntryCollection collection, string systemId)
		{
			var pathEntry = collection[systemId, "Cheats"]
				?? collection[systemId, "Base"];

			return collection.AbsolutePathFor(pathEntry.Path,systemId);
		}

		public static string SaveStateAbsolutePath(this PathEntryCollection collection, string systemId)
		{
			var pathEntry = collection[systemId, "Savestates"]
				?? collection[systemId, "Base"];

			return collection.AbsolutePathFor(pathEntry.Path, systemId);
		}

		public static string ScreenshotAbsolutePathFor(this PathEntryCollection collection, string systemId)
		{
			var entry = collection[systemId, "Screenshots"]
				?? collection[systemId, "Base"];

			return collection.AbsolutePathFor(entry.Path, systemId);
		}

		public static string PalettesAbsolutePathFor(this PathEntryCollection collection, string systemId)
		{
			return collection.AbsolutePathFor(collection[systemId, "Palettes"].Path, systemId);
		}

		public static string UserAbsolutePathFor(this PathEntryCollection collection, string systemId)
		{
			return collection.AbsolutePathFor(collection[systemId, "User"].Path, systemId);
		}

		/// <summary>
		/// Takes an absolute path and attempts to convert it to a relative, based on the system,
		/// or global base if no system is supplied, if it is not a subfolder of the base, it will return the path unaltered
		/// </summary>
		public static string TryMakeRelative(this PathEntryCollection collection, string absolutePath, string system = null) => absolutePath.MakeRelativeTo(
			string.IsNullOrWhiteSpace(system)
				? collection.GlobalBaseAbsolutePath()
				: collection.AbsolutePathFor(collection.BaseFor(system), system)
		);

		/// <summary>
		/// Puts the currently configured temp path into the environment for use as actual temp directory
		/// </summary>
		public static void RefreshTempPath(this PathEntryCollection collection)
		{
			if (string.IsNullOrWhiteSpace(collection.TempFilesFragment))
				return;
			var path = collection.AbsolutePathFor(collection.TempFilesFragment, null);
			TempFileManager.HelperSetTempPath(path);
		}

		private static string ResolveToolsPath(this PathEntryCollection collection, string subPath)
		{
			if (subPath.IsAbsolute() || subPath.StartsWith('%')) return subPath;

			var toolsPath = collection[PathEntryCollection.GLOBAL, "Tools"].Path;

			// Hack for backwards compatibility, prior to 1.11.5, .wch files were in .\Tools, we don't want that to turn into .Tools\Tools
			if (subPath == "Tools")
			{
				return toolsPath;
			}

			return Path.Combine(toolsPath, subPath);
		}
	}
}
