using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;

using Newtonsoft.Json;

namespace BizHawk.Client.Common
{
	[JsonObject]
	public class PathEntryCollection : IEnumerable<PathEntry>
	{
		private static readonly string COMBINED_SYSIDS_GB = string.Join("_", VSystemID.Raw.GB, VSystemID.Raw.GBC, VSystemID.Raw.SGB);

		private static readonly string COMBINED_SYSIDS_PCE = string.Join("_", VSystemID.Raw.PCE, VSystemID.Raw.PCECD, VSystemID.Raw.SGX);

		public static readonly string GLOBAL = string.Join("_", "Global", VSystemID.Raw.NULL);

		private static readonly Dictionary<string, string> _displayNameLookup = new()
		{
			[GLOBAL] = "Global",
			[VSystemID.Raw.INTV] = "Intellivision",
			[VSystemID.Raw.NES] = "NES",
			[VSystemID.Raw.SNES] = "SNES",
			[VSystemID.Raw.GBA] = "GBA",
			[VSystemID.Raw.SMS] = "SMS",
			[VSystemID.Raw.GG] = "GG",
			[VSystemID.Raw.SG] = "SG",
			[VSystemID.Raw.GEN] = "Genesis",
			[COMBINED_SYSIDS_PCE] = "PC Engine",
			[COMBINED_SYSIDS_GB] = "Gameboy",
			[VSystemID.Raw.DGB] = "Dual Gameboy",
			[VSystemID.Raw.TI83] = "TI83",
			[VSystemID.Raw.A26] = "Atari 2600",
			[VSystemID.Raw.A78] = "Atari 7800",
			[VSystemID.Raw.C64] = "Commodore 64",
			[VSystemID.Raw.ZXSpectrum] = "Sinclair ZX Spectrum",
			[VSystemID.Raw.AmstradCPC] = "Amstrad CPC",
			[VSystemID.Raw.PSX] = "Playstation",
			[VSystemID.Raw.Coleco] = "Coleco",
			[VSystemID.Raw.N64] = "N64",
			[VSystemID.Raw.SAT] = "Saturn",
			[VSystemID.Raw.WSWAN] = "WonderSwan",
			[VSystemID.Raw.Lynx] = "Lynx",
			[VSystemID.Raw.AppleII] = "Apple II",
			[VSystemID.Raw.Libretro] = "Libretro",
			[VSystemID.Raw.VB] = "VB",
			[VSystemID.Raw.NGP] = "NGP",
			[VSystemID.Raw.PCFX] = "PCFX",
			[VSystemID.Raw.ChannelF] = "Fairchild Channel F",
			[VSystemID.Raw.GB3x] = "GB3x",
			[VSystemID.Raw.GB4x] = "GB4x",
			[VSystemID.Raw.VEC] = "VEC",
			[VSystemID.Raw.O2] = "O2",
			[VSystemID.Raw.MSX] = "MSX",
			[VSystemID.Raw.UZE] = "UZE",
			[VSystemID.Raw.NDS] = "NDS",
			[VSystemID.Raw.Sega32X] = "Sega 32X",
			[VSystemID.Raw.GGL] = "Dual Game Gear",
			[VSystemID.Raw.PS2] = "Playstation 2",
		};

		public static string GetDisplayNameFor(string sysID)
		{
			if (_displayNameLookup.TryGetValue(sysID, out var dispName)) return dispName;
			var newDispName = $"{sysID} (INTERIM)";
			_displayNameLookup[sysID] = newDispName;
			return newDispName;
		}

		public static bool InGroup(string sysID, string group)
			=> sysID == group || group.Split('_').Contains(sysID);

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

		[JsonIgnore]
		public string FirmwaresPathFragment => this[GLOBAL, "Firmware"].Path;

		[JsonIgnore]
		internal string TempFilesFragment => this[GLOBAL, "Temp Files"].Path;

		public static List<PathEntry> DefaultValues => new List<PathEntry>
		{
			new(GLOBAL, 1, "Base", "."),
			new(GLOBAL, 2, "ROM", "."),
			new(GLOBAL, 3, "Firmware", Path.Combine(".", "Firmware")),
			new(GLOBAL, 4, "Movies", Path.Combine(".", "Movies")),
			new(GLOBAL, 5, "Movie backups", Path.Combine(".", "Movies", "backup")),
			new(GLOBAL, 6, "A/V Dumps", "."),
			new(GLOBAL, 7, "Tools", Path.Combine(".", "Tools")),
			new(GLOBAL, 8, "Lua", Path.Combine(".", "Lua")),
			new(GLOBAL, 9, "Watch (.wch)", Path.Combine(".", ".")),
			new(GLOBAL, 10, "Debug Logs", Path.Combine(".", "")),
			new(GLOBAL, 11, "Macros", Path.Combine(".", "Movies", "Macros")),
			new(GLOBAL, 12, "TAStudio states", Path.Combine(".", "Movies", "TAStudio states")),
			new(GLOBAL, 13, "Multi-Disk Bundles", Path.Combine(".", "")),
			new(GLOBAL, 14, "External Tools", Path.Combine(".", "ExternalTools")),
			new(GLOBAL, 15, "Temp Files", ""),

			new(VSystemID.Raw.INTV, 0, "Base", Path.Combine(".", "Intellivision")),
			new(VSystemID.Raw.INTV, 1, "ROM", "."),
			new(VSystemID.Raw.INTV, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.INTV, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.INTV, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.INTV, 5, "Cheats", Path.Combine(".", "Cheats")),
			new(VSystemID.Raw.INTV, 6, "Palettes", Path.Combine(".", "Palettes")),

			new(VSystemID.Raw.NES, 0, "Base", Path.Combine(".", "NES")),
			new(VSystemID.Raw.NES, 1, "ROM", "."),
			new(VSystemID.Raw.NES, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.NES, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.NES, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.NES, 5, "Cheats", Path.Combine(".", "Cheats")),
			new(VSystemID.Raw.NES, 6, "Palettes", Path.Combine(".", "Palettes")),

			new(VSystemID.Raw.SNES, 0, "Base", Path.Combine(".", "SNES")),
			new(VSystemID.Raw.SNES, 1, "ROM", "."),
			new(VSystemID.Raw.SNES, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.SNES, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.SNES, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.SNES, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.GBA, 0, "Base", Path.Combine(".", "GBA")),
			new(VSystemID.Raw.GBA, 1, "ROM", "."),
			new(VSystemID.Raw.GBA, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.GBA, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.GBA, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.GBA, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.SMS, 0, "Base", Path.Combine(".", "SMS")),
			new(VSystemID.Raw.SMS, 1, "ROM", "."),
			new(VSystemID.Raw.SMS, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.SMS, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.SMS, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.SMS, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.GG, 0, "Base", Path.Combine(".", "Game Gear")),
			new(VSystemID.Raw.GG, 1, "ROM", "."),
			new(VSystemID.Raw.GG, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.GG, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.GG, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.GG, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.SG, 0, "Base", Path.Combine(".", "SG-1000")),
			new(VSystemID.Raw.SG, 1, "ROM", "."),
			new(VSystemID.Raw.SG, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.SG, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.SG, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.SG, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.GEN, 0, "Base", Path.Combine(".", "Genesis")),
			new(VSystemID.Raw.GEN, 1, "ROM", "."),
			new(VSystemID.Raw.GEN, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.GEN, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.GEN, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.GEN, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(COMBINED_SYSIDS_PCE, 0, "Base", Path.Combine(".", "PC Engine")),
			new(COMBINED_SYSIDS_PCE, 1, "ROM", "."),
			new(COMBINED_SYSIDS_PCE, 2, "Savestates", Path.Combine(".", "State")),
			new(COMBINED_SYSIDS_PCE, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(COMBINED_SYSIDS_PCE, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(COMBINED_SYSIDS_PCE, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(COMBINED_SYSIDS_GB, 0, "Base", Path.Combine(".", "Gameboy")),
			new(COMBINED_SYSIDS_GB, 1, "ROM", "."),
			new(COMBINED_SYSIDS_GB, 2, "Savestates", Path.Combine(".", "State")),
			new(COMBINED_SYSIDS_GB, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(COMBINED_SYSIDS_GB, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(COMBINED_SYSIDS_GB, 5, "Cheats", Path.Combine(".", "Cheats")),
			new(COMBINED_SYSIDS_GB, 6, "Palettes", Path.Combine(".", "Palettes")),

			new(VSystemID.Raw.DGB, 0, "Base", Path.Combine(".", "Dual Gameboy")),
			new(VSystemID.Raw.DGB, 1, "ROM", "."),
			new(VSystemID.Raw.DGB, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.DGB, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.DGB, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.DGB, 5, "Cheats", Path.Combine(".", "Cheats")),
			new(VSystemID.Raw.DGB, 6, "Palettes", Path.Combine(".", "Palettes")),

			new(VSystemID.Raw.TI83, 0, "Base", Path.Combine(".", "TI83")),
			new(VSystemID.Raw.TI83, 1, "ROM", "."),
			new(VSystemID.Raw.TI83, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.TI83, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.TI83, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.TI83, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.A26, 0, "Base", Path.Combine(".", "Atari 2600")),
			new(VSystemID.Raw.A26, 1, "ROM", "."),
			new(VSystemID.Raw.A26, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.A26, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.A26, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.A78, 0, "Base", Path.Combine(".", "Atari 7800")),
			new(VSystemID.Raw.A78, 1, "ROM", "."),
			new(VSystemID.Raw.A78, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.A78, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.A78, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.A78, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.C64, 0, "Base", Path.Combine(".", "C64")),
			new(VSystemID.Raw.C64, 1, "ROM", "."),
			new(VSystemID.Raw.C64, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.C64, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.C64, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.ZXSpectrum, 0, "Base", Path.Combine(".", "ZXSpectrum")),
			new(VSystemID.Raw.ZXSpectrum, 1, "ROM", "."),
			new(VSystemID.Raw.ZXSpectrum, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.ZXSpectrum, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.ZXSpectrum, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.AmstradCPC, 0, "Base", Path.Combine(".", "AmstradCPC")),
			new(VSystemID.Raw.AmstradCPC, 1, "ROM", "."),
			new(VSystemID.Raw.AmstradCPC, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.AmstradCPC, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.AmstradCPC, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.PSX, 0, "Base", Path.Combine(".", "PSX")),
			new(VSystemID.Raw.PSX, 1, "ROM", "."),
			new(VSystemID.Raw.PSX, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.PSX, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.PSX, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.PSX, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.Coleco, 0, "Base", Path.Combine(".", "Coleco")),
			new(VSystemID.Raw.Coleco, 1, "ROM", "."),
			new(VSystemID.Raw.Coleco, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.Coleco, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.Coleco, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.N64, 0, "Base", Path.Combine(".", "N64")),
			new(VSystemID.Raw.N64, 1, "ROM", "."),
			new(VSystemID.Raw.N64, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.N64, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.N64, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.N64, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.SAT, 0, "Base", Path.Combine(".", "Saturn")),
			new(VSystemID.Raw.SAT, 1, "ROM", "."),
			new(VSystemID.Raw.SAT, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.SAT, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.SAT, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.SAT, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.WSWAN, 0, "Base", Path.Combine(".", "WonderSwan")),
			new(VSystemID.Raw.WSWAN, 1, "ROM", "."),
			new(VSystemID.Raw.WSWAN, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.WSWAN, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.WSWAN, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.WSWAN, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.Lynx, 0, "Base", Path.Combine(".", "Lynx")),
			new(VSystemID.Raw.Lynx, 1, "ROM", "."),
			new(VSystemID.Raw.Lynx, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.Lynx, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.Lynx, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.Lynx, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.AppleII, 0, "Base", Path.Combine(".", "Apple II")),
			new(VSystemID.Raw.AppleII, 1, "ROM", "."),
			new(VSystemID.Raw.AppleII, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.AppleII, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.AppleII, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.Libretro, 0, "Base", Path.Combine(".", "Libretro")),
			new(VSystemID.Raw.Libretro, 1, "Cores", Path.Combine(".", "Cores")),
			new(VSystemID.Raw.Libretro, 2, "System", Path.Combine(".", "System")),
			new(VSystemID.Raw.Libretro, 3, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.Libretro, 4, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.Libretro, 5, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.Libretro, 6, "Cheats", Path.Combine(".", "Cheats")),
			//It doesn't make much sense to have a ROM dir for libretro, but a lot of stuff is built around the assumption of a ROM dir existing
			//also, note, sometimes when path gets used, it's for opening a rom, which will be... loaded by... the default system for that rom, i.e. NOT libretro.
			//Really, "Open Rom" for instance doesn't make sense when you have a libretro core open.
			//Well, this is better than nothing.
			new(VSystemID.Raw.Libretro, 7, "ROM", "%recent%"),

			new(VSystemID.Raw.VB, 0, "Base", Path.Combine(".", "VB")),
			new(VSystemID.Raw.VB, 1, "ROM", "."),
			new(VSystemID.Raw.VB, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.VB, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.VB, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.VB, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.NGP, 0, "Base", Path.Combine(".", "NGP")),
			new(VSystemID.Raw.NGP, 1, "ROM", "."),
			new(VSystemID.Raw.NGP, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.NGP, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.NGP, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.NGP, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.PCFX, 0, "Base", Path.Combine(".", "PCFX")),
			new(VSystemID.Raw.PCFX, 1, "ROM", "."),
			new(VSystemID.Raw.PCFX, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.PCFX, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.PCFX, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.PCFX, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.ChannelF, 0, "Base", Path.Combine(".", "Channel F")),
			new(VSystemID.Raw.ChannelF, 1, "ROM", "."),
			new(VSystemID.Raw.ChannelF, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.ChannelF, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.ChannelF, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.GB3x, 0, "Base", Path.Combine(".", "GB3x")),
			new(VSystemID.Raw.GB3x, 1, "ROM", "."),
			new(VSystemID.Raw.GB3x, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.GB3x, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.GB3x, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.GB3x, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.GB4x, 0, "Base", Path.Combine(".", "GB4x")),
			new(VSystemID.Raw.GB4x, 1, "ROM", "."),
			new(VSystemID.Raw.GB4x, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.GB4x, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.GB4x, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.GB4x, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.VEC, 0, "Base", Path.Combine(".", "VEC")),
			new(VSystemID.Raw.VEC, 1, "ROM", "."),
			new(VSystemID.Raw.VEC, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.VEC, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.VEC, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.VEC, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.O2, 0, "Base", Path.Combine(".", "O2")),
			new(VSystemID.Raw.O2, 1, "ROM", "."),
			new(VSystemID.Raw.O2, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.O2, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.O2, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.O2, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.MSX, 0, "Base", Path.Combine(".", "MSX")),
			new(VSystemID.Raw.MSX, 1, "ROM", "."),
			new(VSystemID.Raw.MSX, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.MSX, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.MSX, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.MSX, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.UZE, 0, "Base", Path.Combine(".", "Uzebox")),
			new(VSystemID.Raw.UZE, 1, "ROM", "."),
			new(VSystemID.Raw.UZE, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.UZE, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.UZE, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.UZE, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.NDS, 0, "Base", Path.Combine(".", "NDS")),
			new(VSystemID.Raw.NDS, 1, "ROM", "."),
			new(VSystemID.Raw.NDS, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.NDS, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.NDS, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.NDS, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.Sega32X, 0, "Base", Path.Combine(".", "32X")),
			new(VSystemID.Raw.Sega32X, 1, "ROM", "."),
			new(VSystemID.Raw.Sega32X, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.Sega32X, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.Sega32X, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.Sega32X, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.GGL, 0, "Base", Path.Combine(".", "Dual Game Gear")),
			new(VSystemID.Raw.GGL, 1, "ROM", "."),
			new(VSystemID.Raw.GGL, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.GGL, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.GGL, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.GGL, 5, "Cheats", Path.Combine(".", "Cheats")),

			new(VSystemID.Raw.PS2, 0, "Base", Path.Combine(".", "PS2")),
			new(VSystemID.Raw.PS2, 1, "ROM", "."),
			new(VSystemID.Raw.PS2, 2, "Savestates", Path.Combine(".", "State")),
			new(VSystemID.Raw.PS2, 3, "Save RAM", Path.Combine(".", "SaveRAM")),
			new(VSystemID.Raw.PS2, 4, "Screenshots", Path.Combine(".", "Screenshots")),
			new(VSystemID.Raw.PS2, 5, "Cheats", Path.Combine(".", "Cheats")),
		};
	}
}
