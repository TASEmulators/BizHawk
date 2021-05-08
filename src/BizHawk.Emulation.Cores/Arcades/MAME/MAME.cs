/*

                    Build command

make SUBTARGET=arcade NO_USE_PORTAUDIO=1 DONT_USE_NETWORK=1 NO_USE_MIDI=1 MAIN_SHARED_LIB=1 BIN_DIR="..\somewhere\BizHawk\output\dll" OPTIMIZE=3 PTR64=1 REGENIE=1 -j8


                    FrameAdvance()

MAME fires the periodic callback on every video and debugger update,
which happens every VBlank and also repeatedly at certain time
intervals while paused. Since MAME's luaengine runs in a separate
thread, it's only safe to update everything we need per frame during
this callback, when it's explicitly waiting for further lua commands.

If we disable throttling and pass -update_in_pause, there will be no
delay between video updates. This allows to run at full speed while
frame-stepping.

MAME only captures new frame data once per VBlank, while unpaused.
But it doesn't have an exclusive VBlank callback we could attach to.
It has a LUA_ON_FRAME_DONE callback, but that fires even more
frequently and updates all sorts of other non-video stuff, and we
need none of that here.

So we filter out all the calls that happen while paused (non-VBlank
updates). Then, when Hawk asks us to advance a frame, we virtually
unpause and declare the new frame unfinished. This informs MAME that
it should advance one frame internally. Hawk starts waiting for the
MAME thread to complete the request.

After MAME's done advancing, it fires the periodic callback again.
That's when we update everything and declare the new frame finished,
filtering out any further updates again. Then we allow Hawk to
complete frame-advancing.


                    Memory access

All memory access needs to be done while we're inside a callback,
otherwise we get crashes inside SOL (as described above).

We can't know in advance how many addresses we'll be reading (bulkread
in hawk is too complicated to fully implement), but we can assume
that when a new FrameAdvance() request arrives, all the reading requests
have ended for that frame.

So once the first memory read is requested, we put this whole callback
on hold and just wait for FrameAdvance(). This allows for as many memreads
as one may dream of, without letting MAME to execute anything in its main
thread.

Upon new FrameAdvance(), we wait for the current memread to complete,
then we immeditely let go of the callback, without executing any further
logic. Only when MAME fires the callback once more, we consider it safe to
process FrameAdvance() further.


                      Strings

MAME's luaengine uses lua strings to return C strings as well as
binary buffers. You're meant to know which you're going to get and
handle that accordingly.

When we want to get a C string, we Marshal.PtrToStringAnsi().
With buffers, we Marshal.Copy() to our new buffer.
MameGetString() only covers the former because it's the same steps
every time, while buffers use to need aditional logic.

In both cases MAME wants us to manually free the string buffer. It's
made that way to make the buffer persist actoss C API calls.

*/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Dynamic;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	[PortedCore(CoreNames.MAME, "MAMEDev", "0.230", "https://github.com/mamedev/mame.git", isReleased: false)]
	public partial class MAME : IEmulator, IVideoProvider, ISoundProvider, ISettable<object, MAME.SyncSettings>, IStatable, IInputPollable
	{
		public MAME(string dir, string file, MAME.SyncSettings syncSettings, out string gamename)
		{
			OSTailoredCode.LinkedLibManager.FreeByPtr(OSTailoredCode.LinkedLibManager.LoadOrThrow(LibMAME.dll)); // don't bother if the library is missing

			ServiceProvider = new BasicServiceProvider(this);

			_gameDirectory = dir;
			_gameFilename = file;
			_mameThread = new Thread(ExecuteMAMEThread);

			AsyncLaunchMAME();

			_syncSettings = (SyncSettings)syncSettings ?? new SyncSettings();
			_syncSettings.ExpandoSettings = new ExpandoObject();
			var dynamicObject = (IDictionary<string, object>)_syncSettings.ExpandoSettings;
			dynamicObject.Add("OKAY", 1);
			gamename = _gameName;

			if (_loadFailure != "")
			{
				Dispose();
				throw new Exception("\n\n" + _loadFailure);
			}
		}

		private string _gameName = "Arcade";
		private readonly string _gameDirectory;
		private readonly string _gameFilename;
		private string _loadFailure = "";
		private readonly Thread _mameThread;
		private readonly ManualResetEvent _mameStartupComplete = new ManualResetEvent(false);
		private readonly ManualResetEvent _mameFrameComplete = new ManualResetEvent(false);
		private readonly ManualResetEvent _memoryAccessComplete = new ManualResetEvent(false);
		private readonly AutoResetEvent _mamePeriodicComplete = new AutoResetEvent(false);
		private LibMAME.PeriodicCallbackDelegate _periodicCallback;
		private LibMAME.SoundCallbackDelegate _soundCallback;
		private LibMAME.BootCallbackDelegate _bootCallback;
		private LibMAME.LogCallbackDelegate _logCallback;

		private void AsyncLaunchMAME()
		{
			_mameThread.Start();
			_mameStartupComplete.WaitOne();
		}

		private void ExecuteMAMEThread()
		{
			// dodge GC
			_periodicCallback = MAMEPeriodicCallback;
			_soundCallback = MAMESoundCallback;
			_bootCallback = MAMEBootCallback;
			_logCallback = MAMELogCallback;

			LibMAME.mame_set_periodic_callback(_periodicCallback);
			LibMAME.mame_set_sound_callback(_soundCallback);
			LibMAME.mame_set_boot_callback(_bootCallback);
			LibMAME.mame_set_log_callback(_logCallback);

			// https://docs.mamedev.org/commandline/commandline-index.html
			string[] args =
			{
				 "mame"                                 // dummy, internally discarded by index, so has to go first
				, _gameFilename                         // no dash for rom names
				, "-noreadconfig"                       // forbid reading ini files
				, "-nowriteconfig"                      // forbid writing ini files
				, "-norewind"                           // forbid rewind savestates (captured upon frame advance)
				, "-skip_gameinfo"                      // forbid this blocking screen that requires user input
				, "-nothrottle"                         // forbid throttling to "real" speed of the device
				, "-update_in_pause"                    // ^ including frame-advancing
				, "-rompath",            _gameDirectory // mame doesn't load roms from full paths, only from dirs to scan
				, "-joystick_contradictory"             // allow L+R/U+D on digital joystick
				, "-nonvram_save"                       // prevent dumping non-volatile ram to disk
				, "-artpath",          "mame\\artwork"  // path to load artowrk from
				, "-diff_directory",      "mame\\diff"  // hdd diffs, whenever stuff is written back to an image
				, "-cfg_directory",                ":"  // send invalid path to prevent cfg handling
				, "-volume",                     "-32"  // lowest attenuation means mame osd remains silent
				, "-output",                 "console"  // print everything to hawk console
				, "-samplerate", _sampleRate.ToString() // match hawk samplerate
				, "-video",                     "none"  // forbid mame window altogether
				, "-keyboardprovider",          "none"
				, "-mouseprovider",             "none"
				, "-lightgunprovider",          "none"
				, "-joystickprovider",          "none"
			//	, "-debug"                              // launch mame debugger (because we can)
			};

			LibMAME.mame_launch(args.Length, args);
		}

		private static string MameGetString(string command)
		{
			IntPtr ptr = LibMAME.mame_lua_get_string(command, out var lengthInBytes);

			if (ptr == IntPtr.Zero)
			{
				Console.WriteLine("LibMAME ERROR: string buffer pointer is null");
				return "";
			}

			var ret = Marshal.PtrToStringAnsi(ptr, lengthInBytes);

			if (!LibMAME.mame_lua_free_string(ptr))
			{
				Console.WriteLine("LibMAME ERROR: string buffer wasn't freed");
			}

			return ret;
		}

		private void UpdateGameName()
		{
			_gameName = MameGetString(MAMELuaCommand.GetGameName);
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

		private void MAMEPeriodicCallback()
		{
			if (_exiting)
			{
				LibMAME.mame_lua_execute(MAMELuaCommand.Exit);
				_exiting = false;
			}

			for (; _memAccess;)
			{
				_mamePeriodicComplete.Set();
				_memoryAccessComplete.WaitOne();

				if (!_frameDone && !_paused || _exiting) // FrameAdvance() has been requested
				{
					_memAccess = false;
					return;
				}
			}

			//int MAMEFrame = LibMAME.mame_lua_get_int(MAMELuaCommand.GetFrameNumber);

			if (!_paused)
			{
				SendInput();
				LibMAME.mame_lua_execute(MAMELuaCommand.Step);
				_frameDone = false;
				_paused = true;
			}
			else if (!_frameDone)
			{
				UpdateVideo();
				_frameDone = true;
				_mameFrameComplete.Set();
			}
		}

		private void MAMESoundCallback()
		{
			int bytesPerSample = 2;
			IntPtr ptr = LibMAME.mame_lua_get_string(MAMELuaCommand.GetSamples, out var lengthInBytes);

			if (ptr == IntPtr.Zero)
			{
				Console.WriteLine("LibMAME ERROR: audio buffer pointer is null");
				return;
			}

			int numSamples = lengthInBytes / bytesPerSample;

			unsafe
			{
				short* pSample = (short*)ptr.ToPointer();
				for (int i = 0; i < numSamples; i++)
				{
					_audioSamples.Enqueue(*(pSample + i));
				}
			}

			if (!LibMAME.mame_lua_free_string(ptr))
			{
				Console.WriteLine("LibMAME ERROR: audio buffer wasn't freed");
			}
		}

		private void MAMEBootCallback()
		{
			LibMAME.mame_lua_execute(MAMELuaCommand.Pause);

			CheckVersions();
			GetInputFields();
			GetROMsInfo();
			UpdateVideo();
			UpdateAspect();
			UpdateFramerate();
			UpdateGameName();
			InitMemoryDomains();

			int length = LibMAME.mame_lua_get_int("return string.len(manager.machine:buffer_save())");
			_mameSaveBuffer = new byte[length];
			_hawkSaveBuffer = new byte[length + 4 + 4 + 4 + 1];

			_mameStartupComplete.Set();
		}
		
		private void MAMELogCallback(LibMAME.OutputChannel channel, int size, string data)
		{
			if (data.Contains("NOT FOUND"))
			{
				_loadFailure = data;
			}

			if (data.Contains("Fatal error"))
			{
				_mameStartupComplete.Set();
				_loadFailure += data;
			}

			// mame sends osd_output_channel casted to int, we implicitly cast it back
			if (!data.Contains("pause = "))
			{
				Console.WriteLine(
					$"[MAME { channel.ToString() }] " +
					$"{ data.Replace('\n', ' ') }");
			}
		}

		private void GetROMsInfo()
		{
			string ROMsInfo = MameGetString(MAMELuaCommand.GetROMsInfo);
			string[] ROMs = ROMsInfo.Split(';');

			foreach (string ROM in ROMs)
			{
				if (ROM != string.Empty)
				{
					string[] substrings = ROM.Split(',');
					string name = substrings[0];
					string hashdata = substrings[1].Replace("R", " CRC:").Replace("S", " SHA:");
					string flags = substrings[2];

					_romHashes.Add(name, hashdata);
				}
			}
		}

		private class MAMELuaCommand
		{
			// commands
			public const string Step = "emu.step()";
			public const string Pause = "emu.pause()";
			public const string Unpause = "emu.unpause()";
			public const string Exit = "manager.machine:exit()";

			// getters
			public const string GetVersion = "return emu.app_version()";
			public const string GetGameName = "return manager.machine.system.description";
			public const string GetPixels = "return manager.machine.video:snapshot_pixels()";
			public const string GetSamples = "return manager.machine.sound:get_samples()";
			public const string GetWidth = "return (select(1, manager.machine.video:snapshot_size()))";
			public const string GetHeight = "return (select(2, manager.machine.video:snapshot_size()))";
			public const string GetMainCPUName = "return manager.machine.devices[\":maincpu\"].shortname";

			// memory space
			public const string GetSpace = "return manager.machine.devices[\":maincpu\"].spaces[\"program\"]";
			public const string GetSpaceMapCount = "return #manager.machine.devices[\":maincpu\"].spaces[\"program\"].map.entries";
			public const string SpaceMap = "manager.machine.devices[\":maincpu\"].spaces[\"program\"].map.entries";
			public const string GetSpaceAddressMask = "return manager.machine.devices[\":maincpu\"].spaces[\"program\"].address_mask";
			public const string GetSpaceAddressShift = "return manager.machine.devices[\":maincpu\"].spaces[\"program\"].shift";
			public const string GetSpaceDataWidth = "return manager.machine.devices[\":maincpu\"].spaces[\"program\"].data_width";
			public const string GetSpaceEndianness = "return manager.machine.devices[\":maincpu\"].spaces[\"program\"].endianness";

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
			public const string GetInputFields =
				"final = {} " +
				"for tag, _ in pairs(manager.machine.ioport.ports) do " +
					"for name, field in pairs(manager.machine.ioport.ports[tag].fields) do " +
						"if field.type_class ~= \"dipswitch\" then " +
							"table.insert(final, string.format(\"%s,%s;\", tag, name)) " +
						"end " +
					"end " +
				"end " +
				"table.sort(final) " +
				"return table.concat(final)";
			public const string GetROMsInfo =
				"final = {} " +
				"for __, r in pairs(manager.machine.devices[\":\"].roms) do " +
					"if (r:hashdata() ~= \"\") then " +
						"table.insert(final, string.format(\"%s,%s,%s;\", r:name(), r:hashdata(), r:flags())) " +
					"end " +
				"end " +
				"table.sort(final) " +
				"return table.concat(final)";
		}
	}
}
