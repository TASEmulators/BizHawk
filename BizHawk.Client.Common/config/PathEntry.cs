using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BizHawk.Client.Common
{
	public class PathEntry
	{
		public string SystemDisplayName { get; set; }
		public string Type { get; set; }
		public string Path { get; set; }
		public string System { get; set; }
		public int Ordinal { get; set; }

		public bool HasSystem(string systemID)
		{
			return systemID == System || System.Split('_').Contains(systemID);
		}
	}

	[Newtonsoft.Json.JsonObject]
	public class PathEntryCollection : IEnumerable<PathEntry>
	{
		public List<PathEntry> Paths { get; }

		public PathEntryCollection()
		{
			Paths = new List<PathEntry>();
			Paths.AddRange(DefaultValues);
		}

		[Newtonsoft.Json.JsonConstructor]
		public PathEntryCollection(List<PathEntry> paths)
		{
			Paths = paths;
		}

		public void Add(PathEntry p)
		{
			Paths.Add(p);
		}

		public IEnumerator<PathEntry> GetEnumerator() => Paths.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public PathEntry this[string system, string type] =>
			Paths.FirstOrDefault(p => p.HasSystem(system) && p.Type == type)
			?? TryGetDebugPath(system, type);

		private PathEntry TryGetDebugPath(string system, string type)
		{
			if (Paths.Any(p => p.HasSystem(system)))
			{
				// we have the system, but not the type.  don't attempt to add an unknown type
				return null;
			}

			// we don't have anything for the system in question.  add a set of stock paths
			var systemPath = $"{PathManager.RemoveInvalidFileSystemChars(system)}_INTERIM";
			var systemDisp = $"{system} (INTERIM)";

			Paths.AddRange(new[]
			{
				new PathEntry { System = system, SystemDisplayName = systemDisp, Type = "Base", Path = Path.Combine(".", systemPath), Ordinal = 0 },
				new PathEntry { System = system, SystemDisplayName = systemDisp, Type = "ROM", Path = ".", Ordinal = 1 },
				new PathEntry { System = system, SystemDisplayName = systemDisp, Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
				new PathEntry { System = system, SystemDisplayName = systemDisp, Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
				new PathEntry { System = system, SystemDisplayName = systemDisp, Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
				new PathEntry { System = system, SystemDisplayName = systemDisp, Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 }
			});

			return this[system, type];
		}

		public void ResolveWithDefaults()
		{
			// Add missing entries
			foreach (PathEntry defaultPath in DefaultValues)
			{
				var path = Paths.FirstOrDefault(p => p.System == defaultPath.System && p.Type == defaultPath.Type);
				if (path == null)
				{
					Paths.Add(defaultPath);
				}
			}

			var entriesToRemove = new List<PathEntry>();

			// Remove entries that no longer exist in defaults
			foreach (PathEntry pathEntry in Paths)
			{
				var path = DefaultValues.FirstOrDefault(p => p.System == pathEntry.System && p.Type == pathEntry.Type);
				if (path == null)
				{
					entriesToRemove.Add(pathEntry);
				}
			}

			foreach (PathEntry entry in entriesToRemove)
			{
				Paths.Remove(entry);
			}

			// Add missing display names
			var missingDisplayPaths = Paths.Where(p => p.SystemDisplayName == null);
			foreach (PathEntry path in missingDisplayPaths)
			{
				path.SystemDisplayName = DefaultValues.First(p => p.System == path.System).SystemDisplayName;
			}
		}

		private static string ResolveToolsPath(string subPath)
		{
			if (Path.IsPathRooted(subPath) || subPath.StartsWith("%"))
			{
				return subPath;
			}

			var toolsPath = Global.Config.PathEntries["Global", "Tools"].Path;

			// Hack for backwards compatibility, prior to 1.11.5, .wch files were in .\Tools, we don't want that to turn into .Tools\Tools
			if (subPath == "Tools")
			{
				return toolsPath;
			}

			return Path.Combine(toolsPath, subPath);
		}

		// Some frequently requested paths, made into a property for convenience
		public string ToolsPathFragment => Global.Config.PathEntries["Global", "Tools"].Path;

		public string WatchPathFragment => ResolveToolsPath(Global.Config.PathEntries["Global", "Watch (.wch)"].Path);

		public string MultiDiskBundlesFragment => ResolveToolsPath(Global.Config.PathEntries["Global", "Multi-Disk Bundles"].Path);

		public string LogPathFragment => ResolveToolsPath(Global.Config.PathEntries["Global", "Debug Logs"].Path);

		public string MoviesPathFragment => Global.Config.PathEntries["Global", "Movies"].Path;

		public string MoviesBackupsPathFragment => Global.Config.PathEntries["Global", "Movie backups"].Path;

		public string LuaPathFragment => Global.Config.PathEntries["Global", "Lua"].Path;

		public string FirmwaresPathFragment => Global.Config.PathEntries["Global", "Firmware"].Path;

		public string AvPathFragment => Global.Config.PathEntries["Global", "A/V Dumps"].Path;

		public string GlobalRomFragment => Global.Config.PathEntries["Global", "ROM"].Path;

		public string TempFilesFragment => Global.Config.PathEntries["Global", "Temp Files"].Path;

		// this one is special
		public string GlobalBaseFragment => Global.Config.PathEntries["Global", "Base"].Path;

		public static List<PathEntry> DefaultValues => new List<PathEntry>
		{
			new PathEntry { System = "Global_NULL", SystemDisplayName = "Global", Type = "Base", Path = ".", Ordinal = 1 },
			new PathEntry { System = "Global_NULL", SystemDisplayName = "Global", Type = "ROM", Path = ".", Ordinal = 2 },
			new PathEntry { System = "Global_NULL", SystemDisplayName = "Global", Type = "Firmware", Path = Path.Combine(".", "Firmware"), Ordinal = 3 },
			new PathEntry { System = "Global_NULL", SystemDisplayName = "Global", Type = "Movies", Path = Path.Combine(".", "Movies"), Ordinal = 4 },
			new PathEntry { System = "Global_NULL", SystemDisplayName = "Global", Type = "Movie backups", Path = Path.Combine(".", "Movies", "backup"), Ordinal = 5 },
			new PathEntry { System = "Global_NULL", SystemDisplayName = "Global", Type = "A/V Dumps", Path = ".", Ordinal = 6 },
			new PathEntry { System = "Global_NULL", SystemDisplayName = "Global", Type = "Tools", Path = Path.Combine(".", "Tools"), Ordinal = 7 },
			new PathEntry { System = "Global_NULL", SystemDisplayName = "Global", Type = "Lua", Path = Path.Combine(".", "Lua"), Ordinal = 8 },
			new PathEntry { System = "Global_NULL", SystemDisplayName = "Global", Type = "Watch (.wch)", Path = Path.Combine(".", "."), Ordinal = 9 },
			new PathEntry { System = "Global_NULL", SystemDisplayName = "Global", Type = "Debug Logs", Path = Path.Combine(".", ""), Ordinal = 10 },
			new PathEntry { System = "Global_NULL", SystemDisplayName = "Global", Type = "Macros", Path = Path.Combine(".", "Movies", "Macros"), Ordinal = 11 },
			new PathEntry { System = "Global_NULL", SystemDisplayName = "Global", Type = "TAStudio states", Path = Path.Combine(".", "Movies", "TAStudio states"), Ordinal = 12 },
			new PathEntry { System = "Global_NULL", SystemDisplayName = "Global", Type = "Multi-Disk Bundles", Path = Path.Combine(".", ""), Ordinal = 13 },
			new PathEntry { System = "Global_NULL", SystemDisplayName = "Global", Type = "External Tools", Path = Path.Combine(".", "ExternalTools"), Ordinal = 14 },
			new PathEntry { System = "Global_NULL", SystemDisplayName = "Global", Type = "Temp Files", Path = "", Ordinal = 15 },

			new PathEntry { System = "INTV", SystemDisplayName = "Intellivision", Type = "Base", Path = Path.Combine(".", "Intellivision"), Ordinal = 0 },
			new PathEntry { System = "INTV", SystemDisplayName = "Intellivision", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "INTV", SystemDisplayName = "Intellivision", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "INTV", SystemDisplayName = "Intellivision", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "INTV", SystemDisplayName = "Intellivision", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "INTV", SystemDisplayName = "Intellivision", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },
			new PathEntry { System = "INTV", SystemDisplayName = "Intellivision", Type = "Palettes", Path = Path.Combine(".", "Palettes"),  Ordinal = 6 },

			new PathEntry { System = "NES", SystemDisplayName = "NES", Type = "Base", Path = Path.Combine(".", "NES"), Ordinal = 0 },
			new PathEntry { System = "NES", SystemDisplayName = "NES", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "NES", SystemDisplayName = "NES", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "NES", SystemDisplayName = "NES", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "NES", SystemDisplayName = "NES", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "NES", SystemDisplayName = "NES", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },
			new PathEntry { System = "NES", SystemDisplayName = "NES", Type = "Palettes", Path = Path.Combine(".", "Palettes"),  Ordinal = 6 },

			new PathEntry { System = "SNES_SGB", SystemDisplayName = "SNES", Type = "Base", Path = Path.Combine(".", "SNES"), Ordinal = 0 },
			new PathEntry { System = "SNES_SGB", SystemDisplayName = "SNES", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "SNES_SGB", SystemDisplayName = "SNES", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "SNES_SGB", SystemDisplayName = "SNES", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "SNES_SGB", SystemDisplayName = "SNES", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "SNES_SGB", SystemDisplayName = "SNES", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "GBA", SystemDisplayName = "GBA", Type = "Base", Path = Path.Combine(".", "GBA"), Ordinal = 0 },
			new PathEntry { System = "GBA", SystemDisplayName = "GBA", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "GBA", SystemDisplayName = "GBA", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "GBA", SystemDisplayName = "GBA", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "GBA", SystemDisplayName = "GBA", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "GBA", SystemDisplayName = "GBA", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "SMS", SystemDisplayName = "SMS", Type = "Base", Path = Path.Combine(".", "SMS"), Ordinal = 0 },
			new PathEntry { System = "SMS", SystemDisplayName = "SMS", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "SMS", SystemDisplayName = "SMS", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "SMS", SystemDisplayName = "SMS", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "SMS", SystemDisplayName = "SMS", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "SMS", SystemDisplayName = "SMS", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "GG", SystemDisplayName = "GG", Type = "Base", Path = Path.Combine(".", "Game Gear"), Ordinal = 0 },
			new PathEntry { System = "GG", SystemDisplayName = "GG", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "GG", SystemDisplayName = "GG", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "GG", SystemDisplayName = "GG", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "GG", SystemDisplayName = "GG", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "GG", SystemDisplayName = "GG", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "SG", SystemDisplayName = "SG", Type = "Base", Path = Path.Combine(".", "SG-1000"), Ordinal = 0 },
			new PathEntry { System = "SG", SystemDisplayName = "SG", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "SG", SystemDisplayName = "SG", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "SG", SystemDisplayName = "SG", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "SG", SystemDisplayName = "SG", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "SG", SystemDisplayName = "SG", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "GEN", SystemDisplayName = "Genesis", Type = "Base", Path = Path.Combine(".", "Genesis"), Ordinal = 0 },
			new PathEntry { System = "GEN", SystemDisplayName = "Genesis", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "GEN", SystemDisplayName = "Genesis", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "GEN", SystemDisplayName = "Genesis", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "GEN", SystemDisplayName = "Genesis", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "GEN", SystemDisplayName = "Genesis", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "PCE_PCECD_SGX", SystemDisplayName = "PC Engine", Type = "Base", Path = Path.Combine(".", "PC Engine"), Ordinal = 0 },
			new PathEntry { System = "PCE_PCECD_SGX", SystemDisplayName = "PC Engine", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "PCE_PCECD_SGX", SystemDisplayName = "PC Engine", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "PCE_PCECD_SGX", SystemDisplayName = "PC Engine", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "PCE_PCECD_SGX", SystemDisplayName = "PC Engine", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "PCE_PCECD_SGX", SystemDisplayName = "PC Engine", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "GB_GBC", SystemDisplayName = "Gameboy", Type = "Base", Path = Path.Combine(".", "Gameboy"), Ordinal = 0 },
			new PathEntry { System = "GB_GBC", SystemDisplayName = "Gameboy", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "GB_GBC", SystemDisplayName = "Gameboy", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "GB_GBC", SystemDisplayName = "Gameboy", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "GB_GBC", SystemDisplayName = "Gameboy", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "GB_GBC", SystemDisplayName = "Gameboy", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },
			new PathEntry { System = "GB_GBC", SystemDisplayName = "Gameboy", Type = "Palettes", Path = Path.Combine(".", "Palettes"),  Ordinal = 6 },

			new PathEntry { System = "DGB", SystemDisplayName = "Dual Gameboy", Type = "Base", Path = Path.Combine(".", "Dual Gameboy"), Ordinal = 0 },
			new PathEntry { System = "DGB", SystemDisplayName = "Dual Gameboy", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "DGB", SystemDisplayName = "Dual Gameboy", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "DGB", SystemDisplayName = "Dual Gameboy", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "DGB", SystemDisplayName = "Dual Gameboy", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "DGB", SystemDisplayName = "Dual Gameboy", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },
			new PathEntry { System = "DGB", SystemDisplayName = "Dual Gameboy", Type = "Palettes", Path = Path.Combine(".", "Palettes"),  Ordinal = 6 },

			new PathEntry { System = "TI83", SystemDisplayName = "TI83", Type = "Base", Path = Path.Combine(".", "TI83"), Ordinal = 0 },
			new PathEntry { System = "TI83", SystemDisplayName = "TI83", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "TI83", SystemDisplayName = "TI83", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "TI83", SystemDisplayName = "TI83", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "TI83", SystemDisplayName = "TI83", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "TI83", SystemDisplayName = "TI83", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "A26", SystemDisplayName = "Atari 2600", Type = "Base", Path = Path.Combine(".", "Atari 2600"), Ordinal = 0 },
			new PathEntry { System = "A26", SystemDisplayName = "Atari 2600", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "A26", SystemDisplayName = "Atari 2600", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "A26", SystemDisplayName = "Atari 2600", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "A26", SystemDisplayName = "Atari 2600", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "A78", SystemDisplayName = "Atari 7800", Type = "Base", Path = Path.Combine(".", "Atari 7800"), Ordinal = 0 },
			new PathEntry { System = "A78", SystemDisplayName = "Atari 7800", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "A78", SystemDisplayName = "Atari 7800", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "A78", SystemDisplayName = "Atari 7800", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "A78", SystemDisplayName = "Atari 7800", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "A78", SystemDisplayName = "Atari 7800", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "C64", SystemDisplayName = "Commodore 64", Type = "Base", Path = Path.Combine(".", "C64"), Ordinal = 0 },
			new PathEntry { System = "C64", SystemDisplayName = "Commodore 64", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "C64", SystemDisplayName = "Commodore 64", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "C64", SystemDisplayName = "Commodore 64", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "C64", SystemDisplayName = "Commodore 64", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "ZXSpectrum", SystemDisplayName = "Sinclair ZX Spectrum", Type = "Base", Path = Path.Combine(".", "ZXSpectrum"), Ordinal = 0 },
			new PathEntry { System = "ZXSpectrum", SystemDisplayName = "Sinclair ZX Spectrum", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "ZXSpectrum", SystemDisplayName = "Sinclair ZX Spectrum", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "ZXSpectrum", SystemDisplayName = "Sinclair ZX Spectrum", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "ZXSpectrum", SystemDisplayName = "Sinclair ZX Spectrum", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "AmstradCPC", SystemDisplayName = "Amstrad CPC", Type = "Base", Path = Path.Combine(".", "AmstradCPC"), Ordinal = 0 },
			new PathEntry { System = "AmstradCPC", SystemDisplayName = "Amstrad CPC", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "AmstradCPC", SystemDisplayName = "Amstrad CPC", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "AmstradCPC", SystemDisplayName = "Amstrad CPC", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "AmstradCPC", SystemDisplayName = "Amstrad CPC", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "PSX", SystemDisplayName = "Playstation", Type = "Base", Path = Path.Combine(".", "PSX"), Ordinal = 0 },
			new PathEntry { System = "PSX", SystemDisplayName = "Playstation", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "PSX", SystemDisplayName = "Playstation", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "PSX", SystemDisplayName = "Playstation", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "PSX", SystemDisplayName = "Playstation", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "PSX", SystemDisplayName = "Playstation", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "Coleco", SystemDisplayName = "Coleco", Type = "Base", Path = Path.Combine(".", "Coleco"), Ordinal = 0 },
			new PathEntry { System = "Coleco", SystemDisplayName = "Coleco", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "Coleco", SystemDisplayName = "Coleco", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "Coleco", SystemDisplayName = "Coleco", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "Coleco", SystemDisplayName = "Coleco", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "N64", SystemDisplayName = "N64", Type = "Base", Path = Path.Combine(".", "N64"), Ordinal = 0 },
			new PathEntry { System = "N64", SystemDisplayName = "N64", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "N64", SystemDisplayName = "N64", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "N64", SystemDisplayName = "N64", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "N64", SystemDisplayName = "N64", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "N64", SystemDisplayName = "N64", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "SAT", SystemDisplayName = "Saturn", Type = "Base", Path = Path.Combine(".", "Saturn"), Ordinal = 0 },
			new PathEntry { System = "SAT", SystemDisplayName = "Saturn", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "SAT", SystemDisplayName = "Saturn", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "SAT", SystemDisplayName = "Saturn", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "SAT", SystemDisplayName = "Saturn", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "SAT", SystemDisplayName = "Saturn", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "WSWAN", SystemDisplayName = "WonderSwan", Type = "Base", Path = Path.Combine(".", "WonderSwan"), Ordinal = 0 },
			new PathEntry { System = "WSWAN", SystemDisplayName = "WonderSwan", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "WSWAN", SystemDisplayName = "WonderSwan", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "WSWAN", SystemDisplayName = "WonderSwan", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "WSWAN", SystemDisplayName = "WonderSwan", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "WSWAN", SystemDisplayName = "WonderSwan", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "Lynx", SystemDisplayName = "Lynx", Type = "Base", Path = Path.Combine(".", "Lynx"), Ordinal = 0 },
			new PathEntry { System = "Lynx", SystemDisplayName = "Lynx", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "Lynx", SystemDisplayName = "Lynx", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "Lynx", SystemDisplayName = "Lynx", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "Lynx", SystemDisplayName = "Lynx", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "Lynx", SystemDisplayName = "Lynx", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "AppleII", SystemDisplayName = "Apple II", Type = "Base", Path = Path.Combine(".", "Apple II"), Ordinal = 0 },
			new PathEntry { System = "AppleII", SystemDisplayName = "Apple II", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "AppleII", SystemDisplayName = "Apple II", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "AppleII", SystemDisplayName = "Apple II", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "AppleII", SystemDisplayName = "Apple II", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "Libretro", SystemDisplayName = "Libretro", Type = "Base", Path = Path.Combine(".", "Libretro"), Ordinal = 0 },
			new PathEntry { System = "Libretro", SystemDisplayName = "Libretro", Type = "Cores", Path = Path.Combine(".", "Cores"), Ordinal = 1 },
			new PathEntry { System = "Libretro", SystemDisplayName = "Libretro", Type = "System", Path = Path.Combine(".", "System"), Ordinal = 2 },
			new PathEntry { System = "Libretro", SystemDisplayName = "Libretro", Type = "Savestates", Path = Path.Combine(".", "State"), Ordinal = 3 },
			new PathEntry { System = "Libretro", SystemDisplayName = "Libretro", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 4 },
			new PathEntry { System = "Libretro", SystemDisplayName = "Libretro", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 5 },
			new PathEntry { System = "Libretro", SystemDisplayName = "Libretro", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 6 },

			new PathEntry { System = "VB", SystemDisplayName = "VB", Type = "Base", Path = Path.Combine(".", "VB"), Ordinal = 0 },
			new PathEntry { System = "VB", SystemDisplayName = "VB", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "VB", SystemDisplayName = "VB", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "VB", SystemDisplayName = "VB", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "VB", SystemDisplayName = "VB", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "VB", SystemDisplayName = "VB", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "NGP", SystemDisplayName = "NGP", Type = "Base", Path = Path.Combine(".", "NGP"), Ordinal = 0 },
			new PathEntry { System = "NGP", SystemDisplayName = "NGP", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "NGP", SystemDisplayName = "NGP", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "NGP", SystemDisplayName = "NGP", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "NGP", SystemDisplayName = "NGP", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "NGP", SystemDisplayName = "NGP", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "PCFX", SystemDisplayName = "PCFX", Type = "Base", Path = Path.Combine(".", "PCFX"), Ordinal = 0 },
			new PathEntry { System = "PCFX", SystemDisplayName = "PCFX", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "PCFX", SystemDisplayName = "PCFX", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "PCFX", SystemDisplayName = "PCFX", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "PCFX", SystemDisplayName = "PCFX", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "PCFX", SystemDisplayName = "PCFX", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "ChannelF", SystemDisplayName = "Fairchild Channel F", Type = "Base", Path = Path.Combine(".", "ZXSpectrum"), Ordinal = 0 },
			new PathEntry { System = "ChannelF", SystemDisplayName = "Fairchild Channel F", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "ChannelF", SystemDisplayName = "Fairchild Channel F", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "ChannelF", SystemDisplayName = "Fairchild Channel F", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "ChannelF", SystemDisplayName = "Fairchild Channel F", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "GB3x", SystemDisplayName = "GB3x", Type = "Base", Path = Path.Combine(".", "GB3x"), Ordinal = 0 },
			new PathEntry { System = "GB3x", SystemDisplayName = "GB3x", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "GB3x", SystemDisplayName = "GB3x", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "GB3x", SystemDisplayName = "GB3x", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "GB3x", SystemDisplayName = "GB3x", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "GB3x", SystemDisplayName = "GB3x", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "GB4x", SystemDisplayName = "GB4x", Type = "Base", Path = Path.Combine(".", "GB4x"), Ordinal = 0 },
			new PathEntry { System = "GB4x", SystemDisplayName = "GB4x", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "GB4x", SystemDisplayName = "GB4x", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "GB4x", SystemDisplayName = "GB4x", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "GB4x", SystemDisplayName = "GB4x", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "GB4x", SystemDisplayName = "GB4x", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "VEC", SystemDisplayName = "VEC", Type = "Base", Path = Path.Combine(".", "VEC"), Ordinal = 0 },
			new PathEntry { System = "VEC", SystemDisplayName = "VEC", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "VEC", SystemDisplayName = "VEC", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "VEC", SystemDisplayName = "VEC", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "VEC", SystemDisplayName = "VEC", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "VEC", SystemDisplayName = "VEC", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "O2", SystemDisplayName = "O2", Type = "Base", Path = Path.Combine(".", "O2"), Ordinal = 0 },
			new PathEntry { System = "O2", SystemDisplayName = "O2", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "O2", SystemDisplayName = "O2", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "O2", SystemDisplayName = "O2", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "O2", SystemDisplayName = "O2", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "O2", SystemDisplayName = "O2", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "MSX", SystemDisplayName = "MSX", Type = "Base", Path = Path.Combine(".", "MSX"), Ordinal = 0 },
			new PathEntry { System = "MSX", SystemDisplayName = "MSX", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "MSX", SystemDisplayName = "MSX", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "MSX", SystemDisplayName = "MSX", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "MSX", SystemDisplayName = "MSX", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "MSX", SystemDisplayName = "MSX", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },
		};
	}

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

		public static string GlobalBaseAsAbsolute(this PathEntryCollection collection)
		{
			var globalBase = collection.GlobalBaseFragment;

			// if %exe% prefixed then substitute exe path and repeat
			if (globalBase.StartsWith("%exe%", StringComparison.InvariantCultureIgnoreCase))
			{
				globalBase = PathManager.GetExeDirectoryAbsolute() + globalBase.Substring(5);
			}

			// rooted paths get returned without change
			// (this is done after keyword substitution to avoid problems though)
			if (Path.IsPathRooted(globalBase))
			{
				return globalBase;
			}

			// not-rooted things are relative to exe path
			globalBase = Path.Combine(PathManager.GetExeDirectoryAbsolute(), globalBase);
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
				?? collection["Global", "Base"];
		}

		/// <summary>
		/// Returns an absolute path for the given relative path.
		/// If provided, the systemId will be used to generate the path.
		/// Wildcards are supported.
		/// Logic will fallback until an absolute path is found,
		/// using Global Base as a last resort
		/// </summary>
		public static string AbsolutePathFor(this PathEntryCollection collection,  string path, string systemId)
		{
			// Hack
			if (systemId == "Global")
			{
				systemId = null;
			}

			// This function translates relative path and special identifiers in absolute paths
			if (path.Length < 1)
			{
				return collection.GlobalBaseAsAbsolute();
			}

			if (path == "%recent%")
			{
				return Environment.SpecialFolder.Recent.ToString();
			}

			if (path.StartsWith("%exe%"))
			{
				return PathManager.GetExeDirectoryAbsolute() + path.Substring(5);
			}

			if (path.StartsWith("%rom%"))
			{
				return Global.Config.LastRomPath + path.Substring(5);
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
					return collection.GlobalBaseAsAbsolute();
				}

				if (path[0] == '.')
				{
					path = path.Remove(0, 1);
					path = path.Insert(0, collection.GlobalBaseAsAbsolute());
				}

				return path;
			}

			if (Path.IsPathRooted(path))
			{
				return path;
			}

			//handling of initial .. was removed (Path.GetFullPath can handle it)
			//handling of file:// or file:\\ was removed  (can Path.GetFullPath handle it? not sure)

			// all bad paths default to EXE
			return PathManager.GetExeDirectoryAbsolute();
		}
	}
}
