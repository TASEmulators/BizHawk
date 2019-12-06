using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	[Core(
		name: "MAME",
		author: "MAMEDev",
		isPorted: true,
		portedVersion: "0.214",
		portedUrl: "https://github.com/mamedev/mame.git",
		singleInstance: false)]
	public partial class MAME : IEmulator, IVideoProvider, ISoundProvider
	{
		public MAME(CoreComm comm, string dir, string file, out string gamename)
		{
			ServiceProvider = new BasicServiceProvider(this);

			CoreComm = comm;
			gameDirectory = dir;
			gameFilename = file;
			MAMEThread = new Thread(ExecuteMAMEThread);

			AsyncLaunchMAME();

			gamename = gameName;
		}

		#region Properties

		public CoreComm CoreComm { get; private set; }
		public IEmulatorServiceProvider ServiceProvider { get; private set; }
		public ControllerDefinition ControllerDefinition => MAMEController;
		public string SystemId => "MAME";
		public int[] GetVideoBuffer() => frameBuffer;
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

		#endregion

		#region Fields

		private Thread MAMEThread;
		private ManualResetEvent MAMEStartupComplete = new ManualResetEvent(false);
		private ManualResetEvent MAMEFrameComplete = new ManualResetEvent(false);
		private SortedDictionary<string, string> fieldsPorts = new SortedDictionary<string, string>();
		private IController Controller = NullController.Instance;
		private int[] frameBuffer = new int[0];
		private Queue<short> audioSamples = new Queue<short>();
		private decimal dAudioSamples = 0;
		private int sampleRate = 44100;
		private bool paused = true;
		private bool exiting = false;
		private bool frameDone = true;
		private int numSamples = 0;
		private string gameDirectory;
		private string gameFilename;
		private string gameName = "Arcade";
		private LibMAME.PeriodicCallbackDelegate periodicCallback;
		private LibMAME.SoundCallbackDelegate soundCallback;
		private LibMAME.BootCallbackDelegate bootCallback;
		private LibMAME.LogCallbackDelegate logCallback;

		#endregion

		#region IEmulator

		public bool FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			if (exiting)
			{
				return false;
			}

			Controller = controller;
			paused = false;
			frameDone = false;

			for (; frameDone == false;)
			{
				MAMEFrameComplete.WaitOne();
			}

			Frame++;

			return true;
		}

		public void ResetCounters()
		{
			Frame = 0;
		}

		public void Dispose()
		{
			exiting = true;
			MAMEThread.Join();
		}

		#endregion

		#region ISoundProvider

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
			decimal dSamplesPerFrame = (decimal)sampleRate * VsyncDenominator / VsyncNumerator;

			if (audioSamples.Any())
			{
				dAudioSamples -= dSamplesPerFrame;
				int remainder = (int)Math.Round(dAudioSamples - Math.Truncate(dAudioSamples)) ^ 1;
				nsamp = (int)Math.Round(dSamplesPerFrame) + remainder;
			}
			else
			{
				nsamp = (int)Math.Round(dSamplesPerFrame);
			}

			samples = new short[nsamp * 2];

			for (int i = 0; i < nsamp * 2; i++)
			{
				if (audioSamples.Any())
				{
					samples[i] = audioSamples.Dequeue();
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
			audioSamples.Clear();
		}

		#endregion

		#region Launchers

		private void AsyncLaunchMAME()
		{
			MAMEThread.Start();
			MAMEStartupComplete.WaitOne();
		}

		private void ExecuteMAMEThread()
		{
			// dodge GC
			periodicCallback = MAMEPeriodicCallback;
			soundCallback = MAMESoundCallback;
			bootCallback = MAMEBootCallback;
			logCallback = MAMELogCallback;

			LibMAME.mame_set_periodic_callback(periodicCallback);
			LibMAME.mame_set_sound_callback(soundCallback);
			LibMAME.mame_set_boot_callback(bootCallback);
			LibMAME.mame_set_log_callback(logCallback);

			// https://docs.mamedev.org/commandline/commandline-index.html
			string[] args = new string[] {
				 "mame"                                // dummy, internally discarded by index, so has to go first
				, gameFilename                         // no dash for rom names
				, "-noreadconfig"                      // forbid reading any config files
				, "-norewind"                          // forbid rewind savestates (captured upon frame advance)
				, "-skip_gameinfo"                     // forbid this blocking screen that requires user input
				, "-nothrottle"                        // forbid throttling to "real" speed of the device
				, "-update_in_pause"                   // ^ including frame-advancing
				, "-rompath",            gameDirectory // mame doesn't load roms from full paths, only from dirs to scan
				, "-volume",                     "-32" // lowest attenuation means mame osd remains silent
				, "-output",                 "console" // print everyting to hawk console
				, "-samplerate", sampleRate.ToString() // match hawk samplerate
				, "-video",                     "none" // forbid mame window altogether
				, "-keyboardprovider",          "none"
				, "-mouseprovider",             "none"
				, "-lightgunprovider",          "none"
				, "-joystickprovider",          "none"
			};

			LibMAME.mame_launch(args.Length, args);
		}

		#endregion

		#region Updaters

		private void UpdateFramerate()
		{
			VsyncNumerator = 1000000000;
			UInt64 refresh = (UInt64)LibMAME.mame_lua_get_double(MAMELuaCommand.GetRefresh);
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
			int lengthInBytes;
			IntPtr ptr = LibMAME.mame_lua_get_string(MAMELuaCommand.GetPixels, out lengthInBytes);

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

			frameBuffer = new int[expectedSize];
			Marshal.Copy(ptr, frameBuffer, 0, expectedSize);

			if (!LibMAME.mame_lua_free_string(ptr))
			{
				Console.WriteLine("LibMAME ERROR: frame buffer wasn't freed");
			}
		}

		private void UpdateInput()
		{
			foreach (var fieldPort in fieldsPorts)
			{
				LibMAME.mame_lua_execute(
					"manager:machine():ioport()" +
					$".ports  [\"{ fieldPort.Value }\"]" +
					$".fields [\"{ fieldPort.Key   }\"]" +
					$":set_value({ (Controller.IsPressed(fieldPort.Key) ? 1 : 0) })");
			}
		}

		private void Update()
		{
			UpdateFramerate();
			UpdateVideo();
			UpdateAspect();
			UpdateInput();
		}

		private void CheckVersions()
		{
			int lengthInBytes;
			IntPtr ptr = LibMAME.mame_lua_get_string(MAMELuaCommand.GetVersion, out lengthInBytes);
			string MAMEVersion = Marshal.PtrToStringAnsi(ptr, lengthInBytes);

			if (!LibMAME.mame_lua_free_string(ptr))
			{
				Console.WriteLine("LibMAME ERROR: string buffer wasn't freed");
			}

			string version = this.Attributes().PortedVersion;
			Debug.Assert(version == MAMEVersion,
				"MAME versions desync!\n\n" +
				$"MAME is { MAMEVersion }\n" +
				$"MAMEHawk is { version }");
		}

		private void UpdateGameName()
		{
			int lengthInBytes;
			IntPtr ptr = LibMAME.mame_lua_get_string(MAMELuaCommand.GetGameName, out lengthInBytes);
			gameName = Marshal.PtrToStringAnsi(ptr, lengthInBytes);

			if (!LibMAME.mame_lua_free_string(ptr))
			{
				Console.WriteLine("LibMAME ERROR: string buffer wasn't freed");
			}
		}

		#endregion

		#region Callbacks

		/*
		 * FrameAdvance() and MAME
		 * 
		 * MAME fires the periodic callback on every video and debugger update,
		 * which happens every VBlank and also repeatedly at certain time
		 * intervals while paused. Since MAME's luaengine runs in a separate
		 * thread, it's only safe to update everything we need per frame during
		 * this callback, when it's explicitly waiting for further lua commands.
		 * 
		 * If we disable throttling and pass -update_in_pause, there will be no
		 * delay between video updates. This allows to run at full speed while
		 * frame-stepping.
		 * 
		 * MAME only captures new frame data once per VBlank, while unpaused.
		 * But it doesn't have an exclusive VBlank callback we could attach to.
		 * It has a LUA_ON_FRAME_DONE callback, but that fires even more
		 * frequently and updates all sorts of other non-video stuff, and we
		 * need none of that here.
		 * 
		 * So we filter out all the calls that happen while paused (non-VBlank
		 * updates). Then, when Hawk asks us to advance a frame, we virtually
		 * unpause and declare the new frame unfinished. This informs MAME that
		 * it should advance one frame internally. Hawk starts waiting for the
		 * MAME thread to complete the request.
		 * 
		 * After MAME's done advancing, it fires the periodic callback again.
		 * That's when we update everything and declare the new frame finished,
		 * filtering out any further updates again. Then we allow Hawk to
		 * complete frame-advancing.
		 */
		private void MAMEPeriodicCallback()
		{
			if (exiting)
			{
				LibMAME.mame_lua_execute(MAMELuaCommand.Exit);
				exiting = false;
			}

			int MAMEFrame = LibMAME.mame_lua_get_int(MAMELuaCommand.GetFrameNumber);

			if (!paused)
			{
				LibMAME.mame_lua_execute(MAMELuaCommand.Step);
				frameDone = false;
				paused = true;
			}
			else if (!frameDone)
			{
				Update();
				frameDone = true;
				MAMEFrameComplete.Set();
			}
		}

		private void MAMESoundCallback()
		{
			int bytesPerSample = 2;
			int lengthInBytes;
			IntPtr ptr = LibMAME.mame_lua_get_string(MAMELuaCommand.GetSamples, out lengthInBytes);

			if (ptr == IntPtr.Zero)
			{
				Console.WriteLine("LibMAME ERROR: audio buffer pointer is null");
				return;
			}

			numSamples = lengthInBytes / bytesPerSample;

			unsafe
			{
				short* pSample = (short*)ptr.ToPointer();
				for (int i = 0; i < numSamples; i++)
				{
					audioSamples.Enqueue(*(pSample + i));
					dAudioSamples++;
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
			Update();
			UpdateGameName();
			MAMEStartupComplete.Set();
		}
		
		private void MAMELogCallback(LibMAME.OutputChannel channel, int size, string data)
		{
			// mame sends osd_output_channel casted to int, we implicitly cast is back
			if (!data.Contains("pause = "))
			{
				Console.WriteLine(
					$"[MAME { channel.ToString() }] " +
					$"{ data.Replace('\n', ' ') }");
			}
		}

		#endregion

		#region Input

		public static ControllerDefinition MAMEController = new ControllerDefinition
		{
			Name = "MAME Controller",
			BoolButtons = new List<string>()
		};

		private void GetInputFields()
		{
			int lengthInBytes;

			IntPtr ptr = LibMAME.mame_lua_get_string(MAMELuaCommand.GetInputFields, out lengthInBytes);

			if (ptr == IntPtr.Zero)
			{
				Console.WriteLine("LibMAME ERROR: string buffer pointer is null");
				return;
			}

			string inputFields = Marshal.PtrToStringAnsi(ptr, lengthInBytes);
			string[] portFields = inputFields.Split(';');
			MAMEController.BoolButtons.Clear();

			foreach (string portField in portFields)
			{
				if (portField != string.Empty)
				{
					string[] substrings = portField.Split(',');
					string tag = substrings.First();
					string field = substrings.Last();

					fieldsPorts.Add(field, tag);
					MAMEController.BoolButtons.Add(field);
				}
			}

			if (!LibMAME.mame_lua_free_string(ptr))
			{
				Console.WriteLine("LibMAME ERROR: string buffer wasn't freed");
			}
		}

		#endregion

		#region Lua Commands

		private class MAMELuaCommand
		{
			public const string Step = "emu.step()";
			public const string Pause = "emu.pause()";
			public const string Unpause = "emu.unpause()";
			public const string Exit = "manager:machine():exit()";
			public const string GetVersion = "return emu.app_version()";
			public const string GetGameName = "return manager:machine():system().description";
			public const string GetPixels = "return manager:machine():video():pixels()";
			public const string GetSamples = "return manager:machine():sound():samples()";
			public const string GetFrameNumber = "return select(2, next(manager:machine().screens)):frame_number()";
			public const string GetRefresh = "return select(2, next(manager:machine().screens)):refresh_attoseconds()";
			public const string GetWidth = "return (select(1, manager:machine():video():size()))";
			public const string GetHeight = "return (select(2, manager:machine():video():size()))";
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

		#endregion
	}
}
