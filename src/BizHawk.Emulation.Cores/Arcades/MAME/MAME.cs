using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	[PortedCore(CoreNames.MAME, "MAMEDev", "0.249", "https://github.com/mamedev/mame.git", isReleased: false)]
	public partial class MAME : IRomInfo
	{
		[CoreConstructor(VSystemID.Raw.Arcade)]
		public MAME(CoreLoadParameters<object, MAMESyncSettings> lp)
		{
			_gameFileName = Path.GetFileName(lp.Roms[0].RomPath);

			ServiceProvider = new BasicServiceProvider(this);

			_syncSettings = lp.SyncSettings ?? new();

			DeterministicEmulation = !_syncSettings.RTCSettings.UseRealTime || lp.DeterministicEmulationRequested;

			_logCallback = MAMELogCallback;
			_baseTimeCallback = MAMEBaseTimeCallback;
			_filenameCallback = name => _nvramFilenames.Add(name);

			_exe = new(new()
			{
				Filename = "libmamearcade.wbx",
				Path = lp.Comm.CoreFileProvider.DllPath(),
				SbrkHeapSizeKB = 128 * 1024,
				InvisibleHeapSizeKB = 4,
				MmapHeapSizeKB = 1024 * 1024,
				PlainHeapSizeKB = 4,
				SealedHeapSizeKB = 4,
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});

			using (_exe.EnterExit())
			{
				_adapter = CallingConventionAdapters.MakeWaterbox(new Delegate[] { _logCallback, _baseTimeCallback, _filenameCallback }, _exe);
				_core = BizInvoker.GetInvoker<LibMAME>(_exe, _exe, _adapter);
				StartMAME(lp.Roms);
			}

			if (_loadFailure != string.Empty)
			{
				Dispose();
				throw new Exception("\n\n" + _loadFailure);
			}

			RomDetails = _gameFullName + "\r\n" + string.Join("\r\n", _romHashes.Select(static r => $"{r.Key} - {r.Value}"));

			// concat all SHA1 hashes together (unprefixed), then hash that
			var hashes = string.Concat(_romHashes
				.Select(static r => r.Value.Split(' ')
				.First(static s => s.StartsWith("SHA:"))
				.RemovePrefix("SHA:")));

			lp.Game.Name = _gameFullName;
			lp.Game.Hash = SHA1Checksum.ComputeDigestHex(Encoding.ASCII.GetBytes(hashes));
			lp.Game.Status = RomStatus.GoodDump;

			_exe.Seal();
		}

		private readonly LibMAME _core;
		private readonly WaterboxHost _exe;
		private readonly ICallingConventionAdapter _adapter;

		private readonly LibMAME.LogCallbackDelegate _logCallback;
		private readonly LibMAME.BaseTimeCallbackDelegate _baseTimeCallback;

		public string RomDetails { get; }

		private readonly string _gameFileName;
		private string _gameFullName = "Arcade";
		private string _gameShortName = "arcade";
		private string _loadFailure = string.Empty;

		private void StartMAME(List<IRomAsset> roms)
		{
			_core.mame_set_log_callback(_logCallback);
			_core.mame_set_base_time_callback(_baseTimeCallback);

			var gameName = _gameFileName.Split('.')[0];

			// mame expects chd files in a folder of the game name
			string MakeFileName(IRomAsset rom)
				=> rom.Extension == ".chd"
					? gameName + '/' + Path.GetFileNameWithoutExtension(rom.RomPath) + rom.Extension
					: Path.GetFileNameWithoutExtension(rom.RomPath) + rom.Extension;

			foreach (var rom in roms)
			{
				_exe.AddReadonlyFile(rom.FileData, MakeFileName(rom));
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
				, "-nvram_directory",               ""  // path to nvram from
				, "-artpath",                      "?"  // path to artwork
				, "-diff_directory",               "?"  // path to hdd diffs
				, "-cfg_directory",                "?"  // path to config
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
				out var value))
			{
				args.AddRange(new[] { "-bios", value });
			}

			if (_core.mame_launch(args.Count, args.ToArray()) == 0)
			{
				CheckVersions();
				UpdateGameName();
				UpdateVideo();
				UpdateAspect();
				UpdateFramerate();
				InitMemoryDomains();
				GetNVRAMFilenames();
				GetInputFields();
				GetROMsInfo();
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
				if (rom.Extension != ".chd")
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

			if (!_core.mame_lua_free_string(ptr))
			{
				Console.WriteLine("LibMAME ERROR: string buffer wasn't freed");
			}

			return ret;
		}

		private void UpdateGameName()
		{
			_gameFullName = MameGetString(MAMELuaCommand.GetGameFullName);
			_gameShortName = MameGetString(MAMELuaCommand.GetGameShortName);
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
			public const string Exit = "manager.machine:exit()";

			// getters
			public const string GetVersion = "return emu.app_version()";
			public const string GetGameShortName = "return manager.machine.system.name";
			public const string GetGameFullName = "return manager.machine.system.description";
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
				"local b = manager.machine.render.ui_target.current_view.bounds " +
				"return b.x1-b.x0";
			public const string GetBoundY =
				"local b = manager.machine.render.ui_target.current_view.bounds " +
				"return b.y1-b.y0";
			public const string GetROMsInfo =
				"local final = {} " +
				"for __, r in pairs(manager.machine.devices[\":\"].roms) do " +
					"if (r:hashdata() ~= \"\") then " +
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
		}
	}
}
