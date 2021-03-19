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
			Paths.AddRange(new PathEntry[]
			{
				new(system, 0, "Base", Path.Combine(".", $"{system.RemoveInvalidFileSystemChars()}_INTERIM")),
				new(system, 1, "ROM", "."),
				new(system, 2, "Savestates", Path.Combine(".", "State")),
				new(system, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
				new(system, 4, "Screenshots", Path.Combine(".", "Screenshots")),
				new(system, 5, "Cheats", Path.Combine(".", "Cheats")),
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
			new("Global_NULL", 1, "Base", "."),
			new("Global_NULL", 2, "ROM", "."),
			new("Global_NULL", 3, "Firmware", Path.Combine(".", "Firmware")),
			new("Global_NULL", 4, "Movies", Path.Combine(".", "Movies")),
			new("Global_NULL", 5, "Movie backups", Path.Combine(".", "Movies", "backup")),
			new("Global_NULL", 6, "A/V Dumps", "."),
			new("Global_NULL", 7, "Tools", Path.Combine(".", "Tools")),
			new("Global_NULL", 8, "Lua", Path.Combine(".", "Lua")),
			new("Global_NULL", 9, "Watch (.wch)", Path.Combine(".", ".")),
			new("Global_NULL", 10, "Debug Logs", Path.Combine(".", "")),
			new("Global_NULL", 11, "Macros", Path.Combine(".", "Movies", "Macros")),
			new("Global_NULL", 12, "TAStudio states", Path.Combine(".", "Movies", "TAStudio states")),
			new("Global_NULL", 13, "Multi-Disk Bundles", Path.Combine(".", "")),
			new("Global_NULL", 14, "External Tools", Path.Combine(".", "ExternalTools")),
			new("Global_NULL", 15, "Temp Files", ""),

			new("INTV", 0, "Base", Path.Combine(".", "Intellivision")),
			new("INTV", 1, "ROM", "."),
			new("INTV", 2, "Savestates", Path.Combine(".", "State")),
			new("INTV", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("INTV", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("INTV", 5, "Cheats", Path.Combine(".", "Cheats")),
			new("INTV", 6, "Palettes", Path.Combine(".", "Palettes")),

			new("NES", 0, "Base", Path.Combine(".", "NES")),
			new("NES", 1, "ROM", "."),
			new("NES", 2, "Savestates", Path.Combine(".", "State")),
			new("NES", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("NES", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("NES", 5, "Cheats", Path.Combine(".", "Cheats")),
			new("NES", 6, "Palettes", Path.Combine(".", "Palettes")),

			new("SNES_SGB", 0, "Base", Path.Combine(".", "SNES")),
			new("SNES_SGB", 1, "ROM", "."),
			new("SNES_SGB", 2, "Savestates", Path.Combine(".", "State")),
			new("SNES_SGB", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("SNES_SGB", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("SNES_SGB", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("GBA", 0, "Base", Path.Combine(".", "GBA")),
			new("GBA", 1, "ROM", "."),
			new("GBA", 2, "Savestates", Path.Combine(".", "State")),
			new("GBA", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("GBA", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("GBA", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("SMS", 0, "Base", Path.Combine(".", "SMS")),
			new("SMS", 1, "ROM", "."),
			new("SMS", 2, "Savestates", Path.Combine(".", "State")),
			new("SMS", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("SMS", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("SMS", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("GG", 0, "Base", Path.Combine(".", "Game Gear")),
			new("GG", 1, "ROM", "."),
			new("GG", 2, "Savestates", Path.Combine(".", "State")),
			new("GG", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("GG", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("GG", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("SG", 0, "Base", Path.Combine(".", "SG-1000")),
			new("SG", 1, "ROM", "."),
			new("SG", 2, "Savestates", Path.Combine(".", "State")),
			new("SG", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("SG", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("SG", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("GEN", 0, "Base", Path.Combine(".", "Genesis")),
			new("GEN", 1, "ROM", "."),
			new("GEN", 2, "Savestates", Path.Combine(".", "State")),
			new("GEN", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("GEN", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("GEN", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("PCE_PCECD_SGX", 0, "Base", Path.Combine(".", "PC Engine")),
			new("PCE_PCECD_SGX", 1, "ROM", "."),
			new("PCE_PCECD_SGX", 2, "Savestates", Path.Combine(".", "State")),
			new("PCE_PCECD_SGX", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("PCE_PCECD_SGX", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("PCE_PCECD_SGX", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("GB_GBC", 0, "Base", Path.Combine(".", "Gameboy")),
			new("GB_GBC", 1, "ROM", "."),
			new("GB_GBC", 2, "Savestates", Path.Combine(".", "State")),
			new("GB_GBC", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("GB_GBC", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("GB_GBC", 5, "Cheats", Path.Combine(".", "Cheats")),
			new("GB_GBC", 6, "Palettes", Path.Combine(".", "Palettes")),

			new("DGB", 0, "Base", Path.Combine(".", "Dual Gameboy")),
			new("DGB", 1, "ROM", "."),
			new("DGB", 2, "Savestates", Path.Combine(".", "State")),
			new("DGB", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("DGB", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("DGB", 5, "Cheats", Path.Combine(".", "Cheats")),
			new("DGB", 6, "Palettes", Path.Combine(".", "Palettes")),

			new("TI83", 0, "Base", Path.Combine(".", "TI83")),
			new("TI83", 1, "ROM", "."),
			new("TI83", 2, "Savestates", Path.Combine(".", "State")),
			new("TI83", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("TI83", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("TI83", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("A26", 0, "Base", Path.Combine(".", "Atari 2600")),
			new("A26", 1, "ROM", "."),
			new("A26", 2, "Savestates", Path.Combine(".", "State")),
			new("A26", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("A26", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("A78", 0, "Base", Path.Combine(".", "Atari 7800")),
			new("A78", 1, "ROM", "."),
			new("A78", 2, "Savestates", Path.Combine(".", "State")),
			new("A78", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("A78", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("A78", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("C64", 0, "Base", Path.Combine(".", "C64")),
			new("C64", 1, "ROM", "."),
			new("C64", 2, "Savestates", Path.Combine(".", "State")),
			new("C64", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("C64", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("ZXSpectrum", 0, "Base", Path.Combine(".", "ZXSpectrum")),
			new("ZXSpectrum", 1, "ROM", "."),
			new("ZXSpectrum", 2, "Savestates", Path.Combine(".", "State")),
			new("ZXSpectrum", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("ZXSpectrum", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("AmstradCPC", 0, "Base", Path.Combine(".", "AmstradCPC")),
			new("AmstradCPC", 1, "ROM", "."),
			new("AmstradCPC", 2, "Savestates", Path.Combine(".", "State")),
			new("AmstradCPC", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("AmstradCPC", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("PSX", 0, "Base", Path.Combine(".", "PSX")),
			new("PSX", 1, "ROM", "."),
			new("PSX", 2, "Savestates", Path.Combine(".", "State")),
			new("PSX", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("PSX", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("PSX", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("Coleco", 0, "Base", Path.Combine(".", "Coleco")),
			new("Coleco", 1, "ROM", "."),
			new("Coleco", 2, "Savestates", Path.Combine(".", "State")),
			new("Coleco", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("Coleco", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("N64", 0, "Base", Path.Combine(".", "N64")),
			new("N64", 1, "ROM", "."),
			new("N64", 2, "Savestates", Path.Combine(".", "State")),
			new("N64", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("N64", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("N64", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("SAT", 0, "Base", Path.Combine(".", "Saturn")),
			new("SAT", 1, "ROM", "."),
			new("SAT", 2, "Savestates", Path.Combine(".", "State")),
			new("SAT", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("SAT", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("SAT", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("WSWAN", 0, "Base", Path.Combine(".", "WonderSwan")),
			new("WSWAN", 1, "ROM", "."),
			new("WSWAN", 2, "Savestates", Path.Combine(".", "State")),
			new("WSWAN", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("WSWAN", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("WSWAN", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("Lynx", 0, "Base", Path.Combine(".", "Lynx")),
			new("Lynx", 1, "ROM", "."),
			new("Lynx", 2, "Savestates", Path.Combine(".", "State")),
			new("Lynx", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("Lynx", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("Lynx", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("AppleII", 0, "Base", Path.Combine(".", "Apple II")),
			new("AppleII", 1, "ROM", "."),
			new("AppleII", 2, "Savestates", Path.Combine(".", "State")),
			new("AppleII", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("AppleII", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("Libretro", 0, "Base", Path.Combine(".", "Libretro")),
			new("Libretro", 1, "Cores", Path.Combine(".", "Cores")),
			new("Libretro", 2, "System", Path.Combine(".", "System")),
			new("Libretro", 3, "Savestates", Path.Combine(".", "State")),
			new("Libretro", 4, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("Libretro", 5, "Screenshots", Path.Combine(".", "Screenshots")),
			new("Libretro", 6, "Cheats", Path.Combine(".", "Cheats")),
			//It doesn't make much sense to have a ROM dir for libretro, but a lot of stuff is built around the assumption of a ROM dir existing
			//also, note, sometimes when path gets used, it's for opening a rom, which will be... loaded by... the default system for that rom, i.e. NOT libretro.
			//Really, "Open Rom" for instance doesn't make sense when you have a libretro core open.
			//Well, this is better than nothing.
			new("Libretro", 7, "ROM", "%recent%"),

			new("VB", 0, "Base", Path.Combine(".", "VB")),
			new("VB", 1, "ROM", "."),
			new("VB", 2, "Savestates", Path.Combine(".", "State")),
			new("VB", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("VB", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("VB", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("NGP", 0, "Base", Path.Combine(".", "NGP")),
			new("NGP", 1, "ROM", "."),
			new("NGP", 2, "Savestates", Path.Combine(".", "State")),
			new("NGP", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("NGP", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("NGP", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("PCFX", 0, "Base", Path.Combine(".", "PCFX")),
			new("PCFX", 1, "ROM", "."),
			new("PCFX", 2, "Savestates", Path.Combine(".", "State")),
			new("PCFX", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("PCFX", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("PCFX", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("ChannelF", 0, "Base", Path.Combine(".", "ZXSpectrum")),
			new("ChannelF", 1, "ROM", "."),
			new("ChannelF", 2, "Savestates", Path.Combine(".", "State")),
			new("ChannelF", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("ChannelF", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("GB3x", 0, "Base", Path.Combine(".", "GB3x")),
			new("GB3x", 1, "ROM", "."),
			new("GB3x", 2, "Savestates", Path.Combine(".", "State")),
			new("GB3x", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("GB3x", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("GB3x", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("GB4x", 0, "Base", Path.Combine(".", "GB4x")),
			new("GB4x", 1, "ROM", "."),
			new("GB4x", 2, "Savestates", Path.Combine(".", "State")),
			new("GB4x", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("GB4x", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("GB4x", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("VEC", 0, "Base", Path.Combine(".", "VEC")),
			new("VEC", 1, "ROM", "."),
			new("VEC", 2, "Savestates", Path.Combine(".", "State")),
			new("VEC", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("VEC", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("VEC", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("O2", 0, "Base", Path.Combine(".", "O2")),
			new("O2", 1, "ROM", "."),
			new("O2", 2, "Savestates", Path.Combine(".", "State")),
			new("O2", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("O2", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("O2", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("MSX", 0, "Base", Path.Combine(".", "MSX")),
			new("MSX", 1, "ROM", "."),
			new("MSX", 2, "Savestates", Path.Combine(".", "State")),
			new("MSX", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("MSX", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("MSX", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("UZE", 0, "Base", Path.Combine(".", "VEC")),
			new("UZE", 1, "ROM", "."),
			new("UZE", 2, "Savestates", Path.Combine(".", "State")),
			new("UZE", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("UZE", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("UZE", 5, "Cheats", Path.Combine(".", "Cheats")),

			new("NDS", 0, "Base", Path.Combine(".", "NDS")),
			new("NDS", 1, "ROM", "."),
			new("NDS", 2, "Savestates", Path.Combine(".", "State")),
			new("NDS", 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new("NDS", 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new("NDS", 5, "Cheats", Path.Combine(".", "Cheats")),
		};
	}
}
