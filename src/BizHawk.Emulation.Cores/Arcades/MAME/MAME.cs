using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Text;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Common.PathExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	[PortedCore(CoreNames.MAME, "MAMEDev", "0.252", "https://github.com/mamedev/mame.git")]
	public partial class MAME : IRomInfo
	{
		[CoreConstructor(VSystemID.Raw.Arcade)]
		public MAME(CoreLoadParameters<object, MAMESyncSettings> lp)
		{
			_gameFileName = Path.GetFileName(lp.Roms[0].RomPath).ToLowerInvariant();
			_syncSettings = lp.SyncSettings ?? new();

			ServiceProvider = new BasicServiceProvider(this);
			DeterministicEmulation = !_syncSettings.RTCSettings.UseRealTime || lp.DeterministicEmulationRequested;

			_logCallback = MAMELogCallback;
			_baseTimeCallback = MAMEBaseTimeCallback;
			_inputPollCallback = InputCallbacks.Call;
			_filenameCallback = name => _nvramFilenames.Add(name);
			_infoCallback = info =>
			{
				var text = info.Replace(". ", "\n").Replace("\n\n", "\n");
				lp.Comm.Notify(text, 4 * Regex.Matches(text, "\n").Count);
				RomDetails =
					$"Full Name:          { _gameFullName }\r\n" +
					$"Short Name:         { _gameShortName }\r\n" +
					$"Resolution:         { BufferWidth }x{ BufferHeight }\r\n" +
					$"Aspect Ratio:       { _wAspect }:{ _hAspect }\r\n" +
					$"Framerate:          { (float)VsyncNumerator / VsyncDenominator } " +
					$"({ VsyncNumerator } / { VsyncDenominator })\r\n" +
					$"Driver Source File: { _driverSourceFile.RemovePrefix("src")}\r\n\r\n" +
					text + (text == "" ? "" : "\r\n") +
					string.Join("\r\n", _romHashes.Select(static r => $"{r.Value} - {r.Key}"));

				if (text.Contains("imperfect", StringComparison.OrdinalIgnoreCase))
				{
					lp.Game.Status = RomStatus.Imperfect;
				}

				if (text.Contains("unemulated", StringComparison.OrdinalIgnoreCase))
				{
					lp.Game.Status = RomStatus.Unimplemented;
				}

				if (text.Contains("doesn't work", StringComparison.OrdinalIgnoreCase))
				{
					lp.Game.Status = RomStatus.NotWorking;
				}

			};

			_exe = new(new()
			{
				Filename = "libmamearcade.wbx",
				Path = PathUtils.DllDirectoryPath,
				SbrkHeapSizeKB = 512 * 1024,
				InvisibleHeapSizeKB = 4,
				MmapHeapSizeKB = 1024 * 1024,
				PlainHeapSizeKB = 4,
				SealedHeapSizeKB = 4,
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});

			using (_exe.EnterExit())
			{
				_adapter = CallingConventionAdapters.MakeWaterbox(new Delegate[] { _logCallback, _baseTimeCallback, _inputPollCallback, _filenameCallback, _infoCallback }, _exe);
				_core = BizInvoker.GetInvoker<LibMAME>(_exe, _exe, _adapter);
				StartMAME(lp.Roms);
			}

			if (_loadFailure != string.Empty)
			{
				Dispose();
				throw new Exception("\n\n" + _loadFailure);
			}

			// concat all SHA1 hashes together (unprefixed), then hash that
			var hashes = string.Concat(_romHashes.Values
				.Where(static s => s.Contains("SHA:"))
				.Select(static s => s.Split(' ')
				.First(static s => s.StartsWithOrdinal("SHA:"))
				.RemovePrefix("SHA:")));

			lp.Game.Name = _gameFullName;
			lp.Game.Hash = SHA1Checksum.ComputeDigestHex(Encoding.ASCII.GetBytes(hashes));

			if (_romHashes.Values.Any(static s => s is "NO GOOD DUMP KNOWN"))
			{
				lp.Game.Status = RomStatus.Unknown;
			}
			else if (_romHashes.Keys.Any(static s => s.Contains("BAD DUMP")))
			{
				lp.Game.Status = RomStatus.BadDump;
			}
			else
			{
				lp.Game.Status = RomStatus.GoodDump;
			}

			_core.mame_info_get_warnings_string(_infoCallback);
			_infoCallback = null;

			_exe.Seal();
		}

		private readonly LibMAME _core;
		private readonly WaterboxHost _exe;
		private readonly ICallingConventionAdapter _adapter;

		private readonly LibMAME.LogCallbackDelegate _logCallback;
		private readonly LibMAME.BaseTimeCallbackDelegate _baseTimeCallback;
		private readonly LibMAME.InputPollCallbackDelegate _inputPollCallback;
		private readonly LibMAME.InfoCallbackDelegate _infoCallback;

		public string RomDetails { get; set; }

		private readonly string _gameFileName;
		private string _gameFullName = "Arcade";
		private string _gameShortName = "arcade";
		private string _driverSourceFile = "";
		private string _loadFailure = string.Empty;
		private readonly SortedList<string, string> _romHashes = new();

		private void StartMAME(List<IRomAsset> roms)
		{
			_core.mame_set_log_callback(_logCallback);
			_core.mame_set_base_time_callback(_baseTimeCallback);

			var gameName = _gameFileName.Split('.')[0];

			static byte[] MakeRomData(IRomAsset rom)
			{
				if (rom.Extension.ToLowerInvariant() is ".zip")
				{
					// if this is deflate, unzip the zip, and rezip it without compression
					// this is to get around some zlib bug?
					using var ret = new MemoryStream();
					ret.Write(rom.FileData, 0, rom.FileData.Length);
					using (var zip = new ZipArchive(ret, ZipArchiveMode.Update, leaveOpen: true))
					{
						foreach (var entryName in zip.Entries.Select(e => e.FullName).ToList())
						{
							try // TODO: this is a bad way to detect deflate (although it works I guess)
							{
								var oldEntry = zip.GetEntry(entryName)!;
								using var oldEntryStream = oldEntry.Open(); // if this isn't deflate, this throws InvalidDataException
								var contents = oldEntryStream.ReadAllBytes();
								oldEntryStream.Dispose();
								oldEntry.Delete();
								var newEntry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
								using var newEntryStream = newEntry.Open();
								newEntryStream.Write(contents, 0, contents.Length);
							}
							catch (InvalidDataException)
							{
								// ignored
							}
						}
					}

					// ZipArchive's Dispose() is what actually modifies the backing stream
					return ret.ToArray();
				}

				return rom.FileData;
			}

			// mame expects chd files in a folder of the game name
			string MakeFileName(IRomAsset rom)
				=> rom.Extension.ToLowerInvariant() is ".chd"
					? gameName + '/' + Path.GetFileNameWithoutExtension(rom.RomPath).ToLowerInvariant() + rom.Extension.ToLowerInvariant()
					: Path.GetFileNameWithoutExtension(rom.RomPath).ToLowerInvariant() + rom.Extension.ToLowerInvariant();

			foreach (var rom in roms)
			{
				_exe.AddReadonlyFile(MakeRomData(rom), MakeFileName(rom));
			}

			// https://docs.mamedev.org/commandline/commandline-index.html
			var args = new List<string>
			{
				 "mame"                                 // dummy, internally discarded by index, so has to go first
				, _gameFileName                         // no dash for rom names
				, "-noreadconfig"                       // forbid reading ini files
				, "-nowriteconfig"                      // forbid writing ini files
				, "-norewind"                           // forbid rewind savestates (captured upon frame advance)
				, "-skip_gameinfo"                      // forbid this blocking screen that requires user input
				, "-nothrottle"                         // forbid throttling to "real" speed of the device
				, "-update_in_pause"                    // ^ including frame-advancing
				, "-rompath",                       ""  // mame doesn't load roms from full paths, only from dirs to scan
				, "-joystick_contradictory"             // allow L+R/U+D on digital joystick
				, "-nvram_directory",               ""  // path to nvram
				, "-artpath",                       ""  // path to artwork
				, "-diff_directory",                ""  // path to hdd diffs
				, "-cfg_directory",                 ""  // path to config
				, "-volume",                     "-32"  // lowest attenuation means mame osd remains silent
				, "-output",                 "console"  // print everything to hawk console
				, "-samplerate", _sampleRate.ToString() // match hawk samplerate
				, "-sound",                     "none"  // forbid osd sound driver
				, "-video",                     "none"  // forbid mame window altogether
				, "-keyboardprovider",          "none"
				, "-mouseprovider",             "none"
				, "-lightgunprovider",          "none"
				, "-joystickprovider",          "none"
			};

			if (_syncSettings.DriverSettings.TryGetValue(
				MAMELuaCommand.MakeLookupKey(gameName, LibMAME.BIOS_LUA_CODE),
				out var biosValue))
			{
				args.AddRange(new[] { "-bios", biosValue });
			}

			if (_syncSettings.DriverSettings.TryGetValue(
				MAMELuaCommand.MakeLookupKey(gameName, LibMAME.VIEW_LUA_CODE),
				out var viewValue))
			{
				args.AddRange(new[] { "-snapview", viewValue });
			}

			if (_core.mame_launch(args.Count, args.ToArray()) == 0)
			{
				CheckVersions();
				UpdateGameName();
				UpdateAspect();
				UpdateVideo();
				UpdateFramerate();
				InitMemoryDomains();
				GetNVRAMFilenames();
				GetInputFields();
				GetROMsInfo();
				GetViewsInfo();
				FetchDefaultGameSettings();
				OverrideGameSettings();

				// advance to the first periodic callback while paused (to ensure no emulation is done)
				_core.mame_lua_execute(MAMELuaCommand.Pause);
				_core.mame_coswitch();
				_core.mame_lua_execute(MAMELuaCommand.Unpause);
			}
			else if (_loadFailure == string.Empty)
			{
				_loadFailure = "Unknown load error occurred???";
			}

			foreach (var rom in roms)
			{
				// only close non-chd files
				if (rom.Extension.ToLowerInvariant() != ".chd")
				{
					_exe.RemoveReadonlyFile(MakeFileName(rom));
				}
			}
		}

		private string MameGetString(string command)
		{
			var ptr = _core.mame_lua_get_string(command, out var lengthInBytes);

			if (ptr == IntPtr.Zero)
			{
				Console.WriteLine("LibMAME ERROR: string buffer pointer is null");
				return string.Empty;
			}

			var ret = Marshal.PtrToStringAnsi(ptr, lengthInBytes);
			_core.mame_lua_free_string(ptr);
			return ret;
		}

		private void UpdateGameName()
		{
			_gameFullName = MameGetString(MAMELuaCommand.GetGameFullName);
			_gameShortName = MameGetString(MAMELuaCommand.GetGameShortName);
			_driverSourceFile = MameGetString(MAMELuaCommand.GetDriverSourceFile);
		}

		private void CheckVersions()
		{
			var mameVersion = MameGetString(MAMELuaCommand.GetVersion);
			var version = ((PortedCoreAttribute) this.Attributes()).PortedVersion;
			Debug.Assert(version == mameVersion,
				"MAME versions desync!\n\n" +
				$"MAME is { mameVersion }\n" +
				$"MAMEHawk is { version }");
		}
		
		private void MAMELogCallback(LibMAME.OutputChannel channel, int size, string data)
		{
			if (data.Contains("NOT FOUND") && channel == LibMAME.OutputChannel.ERROR)
			{
				_loadFailure = data;
			}

			if (data.Contains("Fatal error"))
			{
				_loadFailure += data;
			}

			// mame sends osd_output_channel casted to int, we implicitly cast it back
			if (!data.Contains("pause = "))
			{
				Console.WriteLine(
					$"[MAME { channel }] " +
					$"{ data.Replace('\n', ' ') }");
			}
		}

		private static readonly DateTime _epoch = new(1970, 1, 1, 0, 0, 0);

		private long MAMEBaseTimeCallback()
		{
			var start = DeterministicEmulation ? _syncSettings.RTCSettings.InitialTime : DateTime.Now;
			return (long)(start - _epoch).TotalSeconds;
		}

		private static class MAMELuaCommand
		{
			// commands
			public const string Step = "emu.step()";
			public const string Pause = "emu.pause()";
			public const string Unpause = "emu.unpause()";
			public const string Reset = "manager.machine:soft_reset()";
			public const string Exit = "manager.machine:exit()";

			// getters
			public const string GetVersion = "return emu.app_version()";
			public const string GetGameShortName = "return manager.machine.system.name";
			public const string GetGameFullName = "return manager.machine.system.description";
			public const string GetDriverSourceFile = "return manager.machine.system.source_file";
			public const string GetWidth = "return (select(1, manager.machine.video:snapshot_size()))";
			public const string GetHeight = "return (select(2, manager.machine.video:snapshot_size()))";
			public const string GetPixels = "return manager.machine.video:snapshot_pixels()";
			public const string GetSamples = "return manager.machine.sound:get_samples()";
			public const string GetMainCPUName = "return manager.machine.devices[\":maincpu\"].shortname";

			// memory space
			public const string GetSpace = "return manager.machine.devices[\":maincpu\"].spaces[\"program\"]";
			public const string GetSpaceAddressMask = "return manager.machine.devices[\":maincpu\"].spaces[\"program\"].address_mask";
			public const string GetSpaceAddressShift = "return manager.machine.devices[\":maincpu\"].spaces[\"program\"].shift";
			public const string GetSpaceDataWidth = "return manager.machine.devices[\":maincpu\"].spaces[\"program\"].data_width";
			public const string GetSpaceEndianness = "return manager.machine.devices[\":maincpu\"].spaces[\"program\"].endianness";
			public const string GetSpaceMapCount = "return #manager.machine.devices[\":maincpu\"].spaces[\"program\"].map.entries";
			public const string SpaceMap = "manager.machine.devices[\":maincpu\"].spaces[\"program\"].map.entries";

			// complex stuff
			public const string GetFrameNumber =
				"for k,v in pairs(manager.machine.screens) do " +
					"return v:frame_number() " +
				"end";
			public const string GetRefresh =
				"for k,v in pairs(manager.machine.screens) do " +
					"return v.refresh_attoseconds " +
				"end";
			public const string GetBoundX =
				"return manager.machine.video.snapshot_target.current_view.bounds.width";
			public const string GetBoundY =
				"return manager.machine.video.snapshot_target.current_view.bounds.height";
			public const string GetROMsInfo =
				"local final = {} " +
				"for __, r in pairs(manager.machine.devices[\":\"].roms) do " +
					"if (r:hashdata() == \"!\") then " +
						"table.insert(final, string.format(\"%s~%s~%s;\", r:name(), \"NO GOOD DUMP KNOWN\", r:flags())) " +
					"elseif (r:hashdata() ~= \"\") then " +
						"table.insert(final, string.format(\"%s~%s~%s;\", r:name(), r:hashdata(), r:flags())) " +
					"end " +
				"end " +
				"table.sort(final) " +
				"return table.concat(final)";
			public const string GetButtonFields =
				"local final = {} " +
				"for tag, _ in pairs(manager.machine.ioport.ports) do " +
					"for name, field in pairs(manager.machine.ioport.ports[tag].fields) do " +
						"if field.type_class ~= \"dipswitch\" and not field.is_analog then " +
							"table.insert(final, string.format(\"%s,%s;\", tag, name)) " +
						"end " +
					"end " +
				"end " +
				"table.sort(final) " +
				"return table.concat(final)";
			public const string GetAnalogFields =
				"local final = {} " +
				"for tag, _ in pairs(manager.machine.ioport.ports) do " +
					"for name, field in pairs(manager.machine.ioport.ports[tag].fields) do " +
						"if field.type_class ~= \"dipswitch\" and field.is_analog then " +
							"table.insert(final, string.format(\"%s,%s,%d,%d,%d;\", tag, name, field.defvalue, field.minvalue, field.maxvalue)) " +
						"end " +
					"end " +
				"end " +
				"table.sort(final) " +
				"return table.concat(final)";
			public const string GetDIPSwitchTags =
				"local final = {} " +
				"for tag, _ in pairs(manager.machine.ioport.ports) do " +
					"for name, field in pairs(manager.machine.ioport.ports[tag].fields) do " +
						"if field.type_class == \"dipswitch\" then " +
							"table.insert(final, tag..\";\") " +
							"break " +
						"end " +
					"end " +
				"end " +
				"table.sort(final) " +
				"return table.concat(final)";
			public const string GetViewsInfo =
				"local final = {} " +
				"for index, name in pairs(manager.machine.video.snapshot_target.view_names) do " +
					"table.insert(final, string.format(\"%s,%s;\", index, name)) " +
				"end " +
				"table.sort(final) " +
				"return table.concat(final)";

			public static string GetFramerateDenominator(int frequency) =>
				"for k,v in pairs(manager.machine.screens) do " +
					$"return emu.attotime(0, v.refresh_attoseconds):as_ticks({ frequency }) " +
				"end";
			public static string MakeLookupKey(string gameName, string luaCode) =>
				$"[{ gameName }] { luaCode }";
			public static string InputField(string tag, string fieldName) =>
				$"manager.machine.ioport.ports[\"{ tag }\"].fields[\"{ fieldName }\"]";
			public static string GetDIPSwitchFields(string tag) =>
				"local final = { } " +
				$"for name, field in pairs(manager.machine.ioport.ports[\"{ tag }\"].fields) do " +
					"if field.type_class == \"dipswitch\" then " +
						"table.insert(final, field.name..\"^\") " +
					 "end " +
				"end " +
				"table.sort(final) " +
				"return table.concat(final)";
			public static string GetDIPSwitchOptions(string tag, string fieldName) =>
				"local final = { } " +
				$"for value, description in pairs(manager.machine.ioport.ports[\"{ tag }\"].fields[\"{ fieldName }\"].settings) do " +
					"table.insert(final, string.format(\"%d~%s@\", value, description)) " +
				"end " +
				"table.sort(final) " +
				"return table.concat(final)";
			public static string GetViewName(string index) =>
				$"return manager.machine.video.snapshot_target.view_names[{ index }]";
		}
	}
}
