

/*
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
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	[Core(
		name: "MAME",
		author: "MAMEDev",
		isPorted: true,
		isReleased: false,
		portedVersion: "0.220",
		portedUrl: "https://github.com/mamedev/mame.git",
		singleInstance: false)]
	public partial class MAME : IEmulator, IVideoProvider, ISoundProvider, ISettable<object, MAME.SyncSettings>, IStatable, IInputPollable
	{
		public MAME(string dir, string file, object syncSettings, out string gamename)
		{
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





		public IEmulatorServiceProvider ServiceProvider { get; }
		public ControllerDefinition ControllerDefinition => MAMEController;
		public string SystemId => "MAME";
		public int[] GetVideoBuffer() => _frameBuffer;
		public bool DeterministicEmulation => true;
		public bool CanProvideAsync => false;
		public SyncSoundMode SyncMode => SyncSoundMode.Sync;
		public int BackgroundColor => 0;
		public int Frame { get; private set; }
		public int VirtualWidth { get; private set; } = 320;
		public int VirtualHeight { get; private set; } = 240;
		public int BufferWidth { get; private set; } = 320;
		public int BufferHeight { get; private set; } = 240;
		public int VsyncNumerator { get; private set; } = 60;
		public int VsyncDenominator { get; private set; } = 1;
		public int LagCount { get; set; } = 0;
		public bool IsLagFrame { get; set; } = false;

		[FeatureNotImplemented]
		public IInputCallbackSystem InputCallbacks => throw new NotImplementedException();





		private SyncSettings _syncSettings;
		private Thread _mameThread;
		private ManualResetEvent _mameStartupComplete = new ManualResetEvent(false);
		private ManualResetEvent _mameFrameComplete = new ManualResetEvent(false);
		private ManualResetEvent _memoryAccessComplete = new ManualResetEvent(false);
		private AutoResetEvent _mamePeriodicComplete = new AutoResetEvent(false);
		private SortedDictionary<string, string> _fieldsPorts = new SortedDictionary<string, string>();
		private IController _controller = NullController.Instance;
		private IMemoryDomains _memoryDomains;
		private byte[] _mameSaveBuffer;
		private byte[] _hawkSaveBuffer;
		private int _systemBusAddressShift = 0;
		private bool _memAccess = false;
		private int[] _frameBuffer = new int[0];
		private Queue<short> _audioSamples = new Queue<short>();
		private decimal _dAudioSamples = 0;
		private int _sampleRate = 44100;
		private int _numSamples = 0;
		private bool _paused = true;
		private bool _exiting = false;
		private bool _frameDone = true;
		private string _gameDirectory;
		private string _gameFilename;
		private string _gameName = "Arcade";
		private string _loadFailure = "";
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
				, "-joystick_contradictory"             // L+R/U+D on digital joystick
				, "-nonvram_save"                       // prevent dumping non-volatile ram to disk
				, "-artpath",          "mame\\artwork"  // path to load artowrk from
				, "-diff_directory",      "mame\\diff"  // hdd diffs, whenever stuff is written back to an image
				, "-cfg_directory",                 ""  // send invalid path to prevent cfg handling
				, "-volume",                     "-32"  // lowest attenuation means mame osd remains silent
				, "-output",                 "console"  // print everything to hawk console
				, "-samplerate", _sampleRate.ToString() // match hawk samplerate
				, "-video",                     "none"  // forbid mame window altogether
				, "-keyboardprovider",          "none"
				, "-mouseprovider",             "none"
				, "-lightgunprovider",          "none"
				, "-joystickprovider",          "none"
			};

			LibMAME.mame_launch(args.Length, args);
		}





		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{
			if (_exiting)
			{
				return false;
			}

			_controller = controller;
			_paused = false;
			_frameDone = false;

			if (_memAccess)
			{
				_mamePeriodicComplete.WaitOne();
			}

			for (; _frameDone == false;)
			{
				_mameFrameComplete.WaitOne();
			}

			Frame++;

			if (IsLagFrame)
			{
				LagCount++;
			}

			return true;
		}

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public void Dispose()
		{
			_exiting = true;
			_mameThread.Join();
			_mameSaveBuffer = new byte[0];
			_hawkSaveBuffer = new byte[0];
		}





		public void SaveStateBinary(BinaryWriter writer)
		{
			writer.Write(_mameSaveBuffer.Length);

			LibMAME.SaveError err = LibMAME.mame_save_buffer(_mameSaveBuffer, out int length);

			if (length != _mameSaveBuffer.Length)
			{
				throw new InvalidOperationException("Savestate buffer size mismatch!");
			}

			if (err != LibMAME.SaveError.NONE)
			{
				throw new InvalidOperationException("MAME LOADSTATE ERROR: " + err.ToString());
			}

			writer.Write(_mameSaveBuffer);
			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int length = reader.ReadInt32();

			if (length != _mameSaveBuffer.Length)
			{
				throw new InvalidOperationException("Savestate buffer size mismatch!");
			}

			reader.Read(_mameSaveBuffer, 0, _mameSaveBuffer.Length);
			LibMAME.SaveError err = LibMAME.mame_load_buffer(_mameSaveBuffer, _mameSaveBuffer.Length);

			if (err != LibMAME.SaveError.NONE)
			{
				throw new InvalidOperationException("MAME SAVESTATE ERROR: " + err.ToString());
			}

			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream(_hawkSaveBuffer);
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();

			if (ms.Position != _hawkSaveBuffer.Length)
			{
				throw new InvalidOperationException();
			}

			ms.Close();
			return _hawkSaveBuffer;
		}





		public object GetSettings() => null;
		public PutSettingsDirtyBits PutSettings(object o) => PutSettingsDirtyBits.None;

		public SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSyncSettings(SyncSettings o)
		{
			bool ret = SyncSettings.NeedsReboot(o, _syncSettings);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public class SyncSettings
		{
			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}

			public SyncSettings Clone()
			{
				return (SyncSettings)MemberwiseClone();
			}

			public ExpandoObject ExpandoSettings { get; set; }
		}





		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		/*
		 * GetSamplesSync() and MAME
		 * 
		 * MAME generates samples 50 times per second, regardless of the VBlank
		 * rate of the emulated machine. It then uses complicated logic to
		 * output the required amount of audio to the OS driver and to the AVI,
		 * where it's meant to tie flashed samples to video frame duration.
		 * 
		 * I'm doing my own logic here for now. I grab MAME's audio buffer
		 * whenever it's filled (MAMESoundCallback()) and enqueue it.
		 * 
		 * Whenever Hawk wants new audio, I dequeue it, but with a little quirk.
		 * Since sample count per frame may not align with frame duration, I
		 * subtract the entire decimal fraction of "required" samples from total
		 * samples. I check if the fractional reminder of total samples is > 0.5
		 * by rounding it. I invert it to see what number I should add to the
		 * integer representation of "required" samples, to compensate for
		 * misalignment between fractional and integral "required" samples.
		 * 
		 * TODO: Figure out how MAME does this and maybe use their method instead.
		 */
		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			decimal dSamplesPerFrame = (decimal)_sampleRate * VsyncDenominator / VsyncNumerator;

			if (_audioSamples.Any())
			{
				_dAudioSamples -= dSamplesPerFrame;
				int remainder = (int)Math.Round(_dAudioSamples - Math.Truncate(_dAudioSamples)) ^ 1;
				nsamp = (int)Math.Round(dSamplesPerFrame) + remainder;
			}
			else
			{
				nsamp = (int)Math.Round(dSamplesPerFrame);
			}

			samples = new short[nsamp * 2];

			for (int i = 0; i < nsamp * 2; i++)
			{
				if (_audioSamples.Any())
				{
					samples[i] = _audioSamples.Dequeue();
				}
				else
				{
					samples[i] = 0;
				}
			}
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		public void DiscardSamples()
		{
			_audioSamples.Clear();
		}





		private byte _peek(long addr, int firstOffset, long size)
		{
			if (addr < 0 || addr >= size)
			{
				throw new ArgumentOutOfRangeException();
			}

			if (!_memAccess)
			{
				_memAccess = true;
				_mamePeriodicComplete.WaitOne();
			}

			addr += firstOffset;

			var val = (byte)LibMAME.mame_lua_get_int($"{ MAMELuaCommand.GetSpace }:read_u8({ addr << _systemBusAddressShift })");

			_memoryAccessComplete.Set();

			return val;
		}

		private void _poke(long addr, byte val, int firstOffset, long size)
		{
			if (addr < 0 || addr >= size)
			{
				throw new ArgumentOutOfRangeException();
			}

			if (!_memAccess)
			{
				_memAccess = true;
				_mamePeriodicComplete.WaitOne();
			}

			addr += firstOffset;

			LibMAME.mame_lua_execute($"{ MAMELuaCommand.GetSpace }:write_u8({ addr << _systemBusAddressShift }, { val })");

			_memoryAccessComplete.Set();
		}

		private void InitMemoryDomains()
		{
			var domains = new List<MemoryDomain>();

			_systemBusAddressShift = LibMAME.mame_lua_get_int(MAMELuaCommand.GetSpaceAddressShift);
			var dataWidth = LibMAME.mame_lua_get_int(MAMELuaCommand.GetSpaceDataWidth) >> 3; // mame returns in bits
			var size = (long)LibMAME.mame_lua_get_double(MAMELuaCommand.GetSpaceAddressMask) + dataWidth;
			var endianString = MameGetString(MAMELuaCommand.GetSpaceEndianness);
			var deviceName = MameGetString(MAMELuaCommand.GetMainCPUName);
			//var addrSize = (size * 2).ToString();

			MemoryDomain.Endian endian = MemoryDomain.Endian.Unknown;

			if (endianString == "little")
			{
				endian = MemoryDomain.Endian.Little;
			}
			else if (endianString == "big")
			{
				endian = MemoryDomain.Endian.Big;
			}

			var mapCount = LibMAME.mame_lua_get_int(MAMELuaCommand.GetSpaceMapCount);

			for (int i = 1; i <= mapCount; i++)
			{
				var read = MameGetString($"return { MAMELuaCommand.SpaceMap }[{ i }].readtype");
				var write = MameGetString($"return { MAMELuaCommand.SpaceMap }[{ i }].writetype");

				if (read == "ram" && write == "ram" || read == "rom")
				{
					var firstOffset = LibMAME.mame_lua_get_int($"return { MAMELuaCommand.SpaceMap }[{ i }].offset");
					var lastOffset = LibMAME.mame_lua_get_int($"return { MAMELuaCommand.SpaceMap }[{ i }].endoff");
					var name = $"{ deviceName } : { read } : 0x{ firstOffset:X}-0x{ lastOffset:X}";

					domains.Add(new MemoryDomainDelegate(name, lastOffset - firstOffset + 1, endian,
						delegate (long addr)
						{
							return _peek(addr, firstOffset, size);
						},
						read == "rom" ? (Action<long, byte>)null : delegate (long addr, byte val)
						{
							_poke(addr, val, firstOffset, size);
						},
						dataWidth));
				}
			}

			domains.Add(new MemoryDomainDelegate(deviceName + " : System Bus", size, endian,
				delegate (long addr)
				{
					return _peek(addr, 0, size);
				},
				null, dataWidth));

			_memoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
		}





		private void UpdateFramerate()
		{
			VsyncNumerator = 1000000000;
			long refresh = (long)LibMAME.mame_lua_get_double(MAMELuaCommand.GetRefresh);
			VsyncDenominator = (int)(refresh / 1000000000);
		}

		private void UpdateAspect()
		{
			int x = (int)LibMAME.mame_lua_get_double(MAMELuaCommand.GetBoundX);
			int y = (int)LibMAME.mame_lua_get_double(MAMELuaCommand.GetBoundY);
			VirtualHeight = BufferWidth > BufferHeight * x / y
				? BufferWidth * y / x
				: BufferHeight;
			VirtualWidth = VirtualHeight * x / y;
		}

		private void UpdateVideo()
		{
			BufferWidth = LibMAME.mame_lua_get_int(MAMELuaCommand.GetWidth);
			BufferHeight = LibMAME.mame_lua_get_int(MAMELuaCommand.GetHeight);
			int expectedSize = BufferWidth * BufferHeight;
			int bytesPerPixel = 4;
			IntPtr ptr = LibMAME.mame_lua_get_string(MAMELuaCommand.GetPixels, out var lengthInBytes);

			if (ptr == IntPtr.Zero)
			{
				Console.WriteLine("LibMAME ERROR: frame buffer pointer is null");
				return;
			}

			if (expectedSize * bytesPerPixel != lengthInBytes)
			{
				Console.WriteLine(
					"LibMAME ERROR: frame buffer has wrong size\n" +
					$"width:    { BufferWidth                  } pixels\n" +
					$"height:   { BufferHeight                 } pixels\n" +
					$"expected: { expectedSize * bytesPerPixel } bytes\n" +
					$"received: { lengthInBytes                } bytes\n");
				return;
			}

			_frameBuffer = new int[expectedSize];
			Marshal.Copy(ptr, _frameBuffer, 0, expectedSize);

			if (!LibMAME.mame_lua_free_string(ptr))
			{
				Console.WriteLine("LibMAME ERROR: frame buffer wasn't freed");
			}
		}

		private void UpdateGameName()
		{
			_gameName = MameGetString(MAMELuaCommand.GetGameName);
		}

		private void CheckVersions()
		{
			var mameVersion = MameGetString(MAMELuaCommand.GetVersion);
			var version = this.Attributes().PortedVersion;
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

			_numSamples = lengthInBytes / bytesPerSample;

			unsafe
			{
				short* pSample = (short*)ptr.ToPointer();
				for (int i = 0; i < _numSamples; i++)
				{
					_audioSamples.Enqueue(*(pSample + i));
					_dAudioSamples++;
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
			UpdateVideo();
			UpdateAspect();
			UpdateFramerate();
			UpdateGameName();
			InitMemoryDomains();

			int length = LibMAME.mame_lua_get_int("return string.len(manager:machine():buffer_save())");
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





		public static ControllerDefinition MAMEController = new ControllerDefinition
		{
			Name = "MAME Controller",
			BoolButtons = new List<string>()
		};

		private void GetInputFields()
		{
			string inputFields = MameGetString(MAMELuaCommand.GetInputFields);
			string[] portFields = inputFields.Split(';');
			MAMEController.BoolButtons.Clear();
			_fieldsPorts.Clear();

			foreach (string portField in portFields)
			{
				if (portField != string.Empty)
				{
					string[] substrings = portField.Split(',');
					string tag = substrings.First();
					string field = substrings.Last();

					_fieldsPorts.Add(field, tag);
					MAMEController.BoolButtons.Add(field);
				}
			}
		}

		private void SendInput()
		{
			foreach (var fieldPort in _fieldsPorts)
			{
				LibMAME.mame_lua_execute(
					"manager:machine():ioport()" +
					$".ports  [\"{ fieldPort.Value }\"]" +
					$".fields [\"{ fieldPort.Key   }\"]" +
					$":set_value({ (_controller.IsPressed(fieldPort.Key) ? 1 : 0) })");
			}
		}





		private class MAMELuaCommand
		{
			// commands
			public const string Step = "emu.step()";
			public const string Pause = "emu.pause()";
			public const string Unpause = "emu.unpause()";
			public const string Exit = "manager:machine():exit()";

			// getters
			public const string GetVersion = "return emu.app_version()";
			public const string GetGameName = "return manager:machine():system().description";
			public const string GetPixels = "return manager:machine():video():pixels()";
			public const string GetSamples = "return manager:machine():sound():samples()";
			public const string GetFrameNumber = "return select(2, next(manager:machine().screens)):frame_number()";
			public const string GetRefresh = "return select(2, next(manager:machine().screens)):refresh_attoseconds()";
			public const string GetWidth = "return (select(1, manager:machine():video():size()))";
			public const string GetHeight = "return (select(2, manager:machine():video():size()))";
			public const string GetMainCPUName = "return manager:machine().devices[\":maincpu\"]:shortname()";

			// memory space
			public const string GetSpace = "return manager:machine().devices[\":maincpu\"].spaces[\"program\"]";
			public const string GetSpaceMapCount = "return #manager:machine().devices[\":maincpu\"].spaces[\"program\"].map";
			public const string SpaceMap = "manager:machine().devices[\":maincpu\"].spaces[\"program\"].map";
			public const string GetSpaceAddressMask = "return manager:machine().devices[\":maincpu\"].spaces[\"program\"].address_mask";
			public const string GetSpaceAddressShift = "return manager:machine().devices[\":maincpu\"].spaces[\"program\"].shift";
			public const string GetSpaceDataWidth = "return manager:machine().devices[\":maincpu\"].spaces[\"program\"].data_width";
			public const string GetSpaceEndianness = "return manager:machine().devices[\":maincpu\"].spaces[\"program\"].endianness";
			public const string GetSpaceBuffer =
				"local space = manager:machine().devices[\":maincpu\"].spaces[\"program\"]" +
				"local address_shift = space.shift " +
				"local data_width = space.data_width " +
				"local bit_step " +
				"if     address_shift == 0 then bit_step = data_width " +
				"elseif address_shift >  0 then bit_step = data_width << address_shift " +
				"elseif address_shift <  0 then bit_step = 8 " +
				"end " +
				"return space:read_range(0, 0xfffffff, space.data_width, math.floor(bit_step / 8))";

			// complex stuff
			public const string GetBoundX =
				"local x0,x1,y0,y1 = manager:machine():render():ui_target():view_bounds() " +
				"return x1-x0";
			public const string GetBoundY =
				"local x0,x1,y0,y1 = manager:machine():render():ui_target():view_bounds() " +
				"return y1-y0";
			public const string GetInputFields =
				"final = {} " +
				"for tag, _ in pairs(manager:machine():ioport().ports) do " +
					"for name, field in pairs(manager:machine():ioport().ports[tag].fields) do " +
						"if field.type_class ~= \"dipswitch\" then " +
							"table.insert(final, string.format(\"%s,%s;\", tag, name)) " +
						"end " +
					"end " +
				"end " +
				"table.sort(final) " +
				"return table.concat(final)";
		}


	}
}
