using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BizHawk.Common.PathExtensions;
using Newtonsoft.Json;

namespace BizHawk.Client.Common
{
	[JsonObject]
	public class PathEntryCollection : IEnumerable<PathEntry>
	{
		private static readonly Dictionary<string, string> _displayNameLookup = new()
		{
			["Global_NULL"] = "Global",
			["INTV"] = "Intellivision",
			["NES"] = "NES",
			["SNES_SGB"] = "SNES",
			["GBA"] = "GBA",
			["SMS"] = "SMS",
			["GG"] = "GG",
			["SG"] = "SG",
			["GEN"] = "Genesis",
			["PCE_PCECD_SGX"] = "PC Engine",
			["GB_GBC"] = "Gameboy",
			["DGB"] = "Dual Gameboy",
			["TI83"] = "TI83",
			["A26"] = "Atari 2600",
			["A78"] = "Atari 7800",
			["C64"] = "Commodore 64",
			["ZXSpectrum"] = "Sinclair ZX Spectrum",
			["AmstradCPC"] = "Amstrad CPC",
			["PSX"] = "Playstation",
			["Coleco"] = "Coleco",
			["N64"] = "N64",
			["SAT"] = "Saturn",
			["WSWAN"] = "WonderSwan",
			["Lynx"] = "Lynx",
			["AppleII"] = "Apple II",
			["Libretro"] = "Libretro",
			["VB"] = "VB",
			["NGP"] = "NGP",
			["PCFX"] = "PCFX",
			["ChannelF"] = "Fairchild Channel F",
			["GB3x"] = "GB3x",
			["GB4x"] = "GB4x",
			["VEC"] = "VEC",
			["O2"] = "O2",
			["MSX"] = "MSX",
			["UZE"] = "UZE",
			["NDS"] = "NDS",
		};

		public static string GetDisplayNameFor(string sysID)
		{
			if (_displayNameLookup.TryGetValue(sysID, out var dispName)) return dispName;
			var newDispName = $"{sysID} (INTERIM)";
			_displayNameLookup[sysID] = newDispName;
			return newDispName;
		}

		public List<PathEntry> Paths { get; }

		[JsonConstructor]
		public PathEntryCollection(List<PathEntry> paths)
		{
			Paths = paths;
		}

		public PathEntryCollection() : this(new List<PathEntry>(DefaultValues)) {}

		public bool UseRecentForRoms { get; set; }
		public string LastRomPath { get; set; } = ".";

		public IEnumerator<PathEntry> GetEnumerator() => Paths.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public PathEntry this[string system, string type] =>
			Paths.FirstOrDefault(p => p.IsSystem(system) && p.Type == type)
			?? TryGetDebugPath(system, type);

		private PathEntry TryGetDebugPath(string system, string type)
		{
			if (Paths.Any(p => p.IsSystem(system)))
			{
				// we have the system, but not the type.  don't attempt to add an unknown type
				return null;
			}

			// we don't have anything for the system in question.  add a set of stock paths
			Paths.AddRange(new[]
			{
				new PathEntry { System = system, Type = "Base", Path = Path.Combine(".", $"{system.RemoveInvalidFileSystemChars()}_INTERIM"), Ordinal = 0 },
				new PathEntry { System = system, Type = "ROM", Path = ".", Ordinal = 1 },
				new PathEntry { System = system, Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
				new PathEntry { System = system, Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
				new PathEntry { System = system, Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
				new PathEntry { System = system, Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 }
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
		}

		public string FirmwaresPathFragment => this["Global", "Firmware"].Path;

		internal string TempFilesFragment => this["Global", "Temp Files"].Path;

		public static List<PathEntry> DefaultValues => new List<PathEntry>
		{
			new PathEntry { System = "Global_NULL", Type = "Base", Path = ".", Ordinal = 1 },
			new PathEntry { System = "Global_NULL", Type = "ROM", Path = ".", Ordinal = 2 },
			new PathEntry { System = "Global_NULL", Type = "Firmware", Path = Path.Combine(".", "Firmware"), Ordinal = 3 },
			new PathEntry { System = "Global_NULL", Type = "Movies", Path = Path.Combine(".", "Movies"), Ordinal = 4 },
			new PathEntry { System = "Global_NULL", Type = "Movie backups", Path = Path.Combine(".", "Movies", "backup"), Ordinal = 5 },
			new PathEntry { System = "Global_NULL", Type = "A/V Dumps", Path = ".", Ordinal = 6 },
			new PathEntry { System = "Global_NULL", Type = "Tools", Path = Path.Combine(".", "Tools"), Ordinal = 7 },
			new PathEntry { System = "Global_NULL", Type = "Lua", Path = Path.Combine(".", "Lua"), Ordinal = 8 },
			new PathEntry { System = "Global_NULL", Type = "Watch (.wch)", Path = Path.Combine(".", "."), Ordinal = 9 },
			new PathEntry { System = "Global_NULL", Type = "Debug Logs", Path = Path.Combine(".", ""), Ordinal = 10 },
			new PathEntry { System = "Global_NULL", Type = "Macros", Path = Path.Combine(".", "Movies", "Macros"), Ordinal = 11 },
			new PathEntry { System = "Global_NULL", Type = "TAStudio states", Path = Path.Combine(".", "Movies", "TAStudio states"), Ordinal = 12 },
			new PathEntry { System = "Global_NULL", Type = "Multi-Disk Bundles", Path = Path.Combine(".", ""), Ordinal = 13 },
			new PathEntry { System = "Global_NULL", Type = "External Tools", Path = Path.Combine(".", "ExternalTools"), Ordinal = 14 },
			new PathEntry { System = "Global_NULL", Type = "Temp Files", Path = "", Ordinal = 15 },

			new PathEntry { System = "INTV", Type = "Base", Path = Path.Combine(".", "Intellivision"), Ordinal = 0 },
			new PathEntry { System = "INTV", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "INTV", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "INTV", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "INTV", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "INTV", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },
			new PathEntry { System = "INTV", Type = "Palettes", Path = Path.Combine(".", "Palettes"),  Ordinal = 6 },

			new PathEntry { System = "NES", Type = "Base", Path = Path.Combine(".", "NES"), Ordinal = 0 },
			new PathEntry { System = "NES", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "NES", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "NES", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "NES", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "NES", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },
			new PathEntry { System = "NES", Type = "Palettes", Path = Path.Combine(".", "Palettes"),  Ordinal = 6 },

			new PathEntry { System = "SNES_SGB", Type = "Base", Path = Path.Combine(".", "SNES"), Ordinal = 0 },
			new PathEntry { System = "SNES_SGB", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "SNES_SGB", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "SNES_SGB", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "SNES_SGB", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "SNES_SGB", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "GBA", Type = "Base", Path = Path.Combine(".", "GBA"), Ordinal = 0 },
			new PathEntry { System = "GBA", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "GBA", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "GBA", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "GBA", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "GBA", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "SMS", Type = "Base", Path = Path.Combine(".", "SMS"), Ordinal = 0 },
			new PathEntry { System = "SMS", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "SMS", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "SMS", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "SMS", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "SMS", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "GG", Type = "Base", Path = Path.Combine(".", "Game Gear"), Ordinal = 0 },
			new PathEntry { System = "GG", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "GG", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "GG", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "GG", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "GG", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "SG", Type = "Base", Path = Path.Combine(".", "SG-1000"), Ordinal = 0 },
			new PathEntry { System = "SG", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "SG", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "SG", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "SG", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "SG", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "GEN", Type = "Base", Path = Path.Combine(".", "Genesis"), Ordinal = 0 },
			new PathEntry { System = "GEN", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "GEN", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "GEN", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "GEN", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "GEN", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "PCE_PCECD_SGX", Type = "Base", Path = Path.Combine(".", "PC Engine"), Ordinal = 0 },
			new PathEntry { System = "PCE_PCECD_SGX", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "PCE_PCECD_SGX", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "PCE_PCECD_SGX", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "PCE_PCECD_SGX", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "PCE_PCECD_SGX", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "GB_GBC", Type = "Base", Path = Path.Combine(".", "Gameboy"), Ordinal = 0 },
			new PathEntry { System = "GB_GBC", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "GB_GBC", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "GB_GBC", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "GB_GBC", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "GB_GBC", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },
			new PathEntry { System = "GB_GBC", Type = "Palettes", Path = Path.Combine(".", "Palettes"),  Ordinal = 6 },

			new PathEntry { System = "DGB", Type = "Base", Path = Path.Combine(".", "Dual Gameboy"), Ordinal = 0 },
			new PathEntry { System = "DGB", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "DGB", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "DGB", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "DGB", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "DGB", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },
			new PathEntry { System = "DGB", Type = "Palettes", Path = Path.Combine(".", "Palettes"),  Ordinal = 6 },

			new PathEntry { System = "TI83", Type = "Base", Path = Path.Combine(".", "TI83"), Ordinal = 0 },
			new PathEntry { System = "TI83", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "TI83", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "TI83", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "TI83", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "TI83", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "A26", Type = "Base", Path = Path.Combine(".", "Atari 2600"), Ordinal = 0 },
			new PathEntry { System = "A26", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "A26", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "A26", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "A26", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "A78", Type = "Base", Path = Path.Combine(".", "Atari 7800"), Ordinal = 0 },
			new PathEntry { System = "A78", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "A78", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "A78", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "A78", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "A78", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "C64", Type = "Base", Path = Path.Combine(".", "C64"), Ordinal = 0 },
			new PathEntry { System = "C64", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "C64", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "C64", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "C64", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "ZXSpectrum", Type = "Base", Path = Path.Combine(".", "ZXSpectrum"), Ordinal = 0 },
			new PathEntry { System = "ZXSpectrum", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "ZXSpectrum", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "ZXSpectrum", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "ZXSpectrum", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "AmstradCPC", Type = "Base", Path = Path.Combine(".", "AmstradCPC"), Ordinal = 0 },
			new PathEntry { System = "AmstradCPC", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "AmstradCPC", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "AmstradCPC", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "AmstradCPC", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "PSX", Type = "Base", Path = Path.Combine(".", "PSX"), Ordinal = 0 },
			new PathEntry { System = "PSX", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "PSX", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "PSX", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "PSX", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "PSX", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "Coleco", Type = "Base", Path = Path.Combine(".", "Coleco"), Ordinal = 0 },
			new PathEntry { System = "Coleco", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "Coleco", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "Coleco", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "Coleco", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "N64", Type = "Base", Path = Path.Combine(".", "N64"), Ordinal = 0 },
			new PathEntry { System = "N64", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "N64", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "N64", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "N64", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "N64", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "SAT", Type = "Base", Path = Path.Combine(".", "Saturn"), Ordinal = 0 },
			new PathEntry { System = "SAT", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "SAT", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "SAT", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "SAT", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "SAT", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "WSWAN", Type = "Base", Path = Path.Combine(".", "WonderSwan"), Ordinal = 0 },
			new PathEntry { System = "WSWAN", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "WSWAN", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "WSWAN", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "WSWAN", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "WSWAN", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "Lynx", Type = "Base", Path = Path.Combine(".", "Lynx"), Ordinal = 0 },
			new PathEntry { System = "Lynx", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "Lynx", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "Lynx", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "Lynx", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "Lynx", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "AppleII", Type = "Base", Path = Path.Combine(".", "Apple II"), Ordinal = 0 },
			new PathEntry { System = "AppleII", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "AppleII", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "AppleII", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "AppleII", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "Libretro", Type = "Base", Path = Path.Combine(".", "Libretro"), Ordinal = 0 },
			new PathEntry { System = "Libretro", Type = "Cores", Path = Path.Combine(".", "Cores"), Ordinal = 1 },
			new PathEntry { System = "Libretro", Type = "System", Path = Path.Combine(".", "System"), Ordinal = 2 },
			new PathEntry { System = "Libretro", Type = "Savestates", Path = Path.Combine(".", "State"), Ordinal = 3 },
			new PathEntry { System = "Libretro", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 4 },
			new PathEntry { System = "Libretro", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 5 },
			new PathEntry { System = "Libretro", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 6 },
			//It doesn't make much sense to have a ROM dir for libretro, but a lot of stuff is built around the assumption of a ROM dir existing
			//also, note, sometimes when path gets used, it's for opening a rom, which will be... loaded by... the default system for that rom, i.e. NOT libretro.
			//Really, "Open Rom" for instance doesn't make sense when you have a libretro core open.
			//Well, this is better than nothing.
			new PathEntry { System = "Libretro", Type = "ROM", Path = "%recent%", Ordinal = 7 },

			new PathEntry { System = "VB", Type = "Base", Path = Path.Combine(".", "VB"), Ordinal = 0 },
			new PathEntry { System = "VB", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "VB", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "VB", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "VB", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "VB", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "NGP", Type = "Base", Path = Path.Combine(".", "NGP"), Ordinal = 0 },
			new PathEntry { System = "NGP", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "NGP", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "NGP", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "NGP", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "NGP", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "PCFX", Type = "Base", Path = Path.Combine(".", "PCFX"), Ordinal = 0 },
			new PathEntry { System = "PCFX", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "PCFX", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "PCFX", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "PCFX", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "PCFX", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "ChannelF", Type = "Base", Path = Path.Combine(".", "ZXSpectrum"), Ordinal = 0 },
			new PathEntry { System = "ChannelF", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "ChannelF", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "ChannelF", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "ChannelF", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "GB3x", Type = "Base", Path = Path.Combine(".", "GB3x"), Ordinal = 0 },
			new PathEntry { System = "GB3x", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "GB3x", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "GB3x", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "GB3x", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "GB3x", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "GB4x", Type = "Base", Path = Path.Combine(".", "GB4x"), Ordinal = 0 },
			new PathEntry { System = "GB4x", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "GB4x", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "GB4x", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "GB4x", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "GB4x", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "VEC", Type = "Base", Path = Path.Combine(".", "VEC"), Ordinal = 0 },
			new PathEntry { System = "VEC", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "VEC", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "VEC", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "VEC", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "VEC", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "O2", Type = "Base", Path = Path.Combine(".", "O2"), Ordinal = 0 },
			new PathEntry { System = "O2", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "O2", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "O2", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "O2", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "O2", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "MSX", Type = "Base", Path = Path.Combine(".", "MSX"), Ordinal = 0 },
			new PathEntry { System = "MSX", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "MSX", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "MSX", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "MSX", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "MSX", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "UZE", Type = "Base", Path = Path.Combine(".", "VEC"), Ordinal = 0 },
			new PathEntry { System = "UZE", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "UZE", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "UZE", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "UZE", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "UZE", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

			new PathEntry { System = "NDS", Type = "Base", Path = Path.Combine(".", "NDS"), Ordinal = 0 },
			new PathEntry { System = "NDS", Type = "ROM", Path = ".", Ordinal = 1 },
			new PathEntry { System = "NDS", Type = "Savestates",  Path = Path.Combine(".", "State"), Ordinal = 2 },
			new PathEntry { System = "NDS", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
			new PathEntry { System = "NDS", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
			new PathEntry { System = "NDS", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 }
		};
	}
}
