using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	[Core(
		name: "MAME",
		author: "MAMEDev",
		isPorted: true,
		portedVersion: "0.209",
		portedUrl: "https://github.com/mamedev/mame.git",
		singleInstance: false)]
	public partial class MAME : IEmulator, IVideoProvider, ISoundProvider
	{
		public MAME(CoreComm comm, string dir, string file)
		{
			ServiceProvider = new BasicServiceProvider(this);

			CoreComm = comm;
			gameDirectory = dir;
			gameFilename = file;

			MAMEThread = new Thread(ExecuteMAMEThread);
			AsyncLaunchMAME();
		}

		public CoreComm CoreComm { get; private set; }
		public IEmulatorServiceProvider ServiceProvider { get; private set; }
		public ControllerDefinition ControllerDefinition => MAMEController;
		public string SystemId => "MAME";
		public int[] GetVideoBuffer() => frameBuffer;
		public bool DeterministicEmulation => true;
		public bool CanProvideAsync => false;
		public SyncSoundMode SyncMode => SyncSoundMode.Sync;
		public int BackgroundColor => unchecked((int)0xFF0000FF);
		public int Frame { get; private set; }
		public int VirtualWidth { get; private set; } = 320;
		public int VirtualHeight { get; private set; } = 240;
		public int BufferWidth { get; private set; } = 320;
		public int BufferHeight { get; private set; } = 240;
		public int VsyncNumerator { get; private set; } = 60;
		public int VsyncDenominator { get; private set; } = 1;

		private Thread MAMEThread;
		private ManualResetEvent MAMEStartupComplete = new ManualResetEvent(false);
		private ManualResetEvent MAMEFrameComplete = new ManualResetEvent(false);
		private ManualResetEvent MAMECommandComplete = new ManualResetEvent(false);
		private bool frameDone = false;
		private bool commandDone = false;
		private int[] frameBuffer = new int[0];
		private short[] audioBuffer = new short[0];
		private int numSamples = 0;
		private string gameDirectory;
		private string gameFilename;

		private void AsyncLaunchMAME()
		{
			MAMEThread.Start();
			MAMEStartupComplete.WaitOne();
			//UpdateVideo();
		}

		private void ExecuteMAMEThread()
		{
			LibMAME.mame_set_frame_callback(MAMEFrameCallback);
			LibMAME.mame_set_periodic_callback(MAMEPeriodicCallback);
			LibMAME.mame_set_boot_callback(MAMEBootCallback);
			LibMAME.mame_set_log_callback(MAMELogCallback);

			string[] args = MakeCommandline(gameDirectory, gameFilename);
			LibMAME.mame_launch(args.Length, args);
		}

		// https://docs.mamedev.org/commandline/commandline-index.html
		private string[] MakeCommandline(string directory, string rom)
		{
			return new string[] {
				 "mame"                           // dummy, internally discarded by index, so has to go first
				, rom                             // no dash for rom names (internally called "unadorned" option)
			//	, "-window"                       // forbid fullscreen
			//	, "-nokeepaspect"                 // forbid window stretching
			//	, "-nomaximize"                   // forbid windowed fullscreen
				, "-noreadconfig"                 // forbid reading any config files
				, "-norewind"                     // forbid rewind savestates (captured upon frame advance)
				, "-skip_gameinfo"                // forbid this blocking screen that requires user input
				, "-rompath",          directory  // mame doesn't load roms from full paths, only from dirs to scan
				, "-volume",           "-32"
				, "-output",           "console"
				, "-samplerate",       "44100"
				, "-video",            "none"     // forbid mame window altogether
			//	, "-sound",            "none"     // "no sound" mode forces low samplerate, useless
				, "-keyboardprovider", "none"
				, "-mouseprovider",    "none"
				, "-lightgunprovider", "none"
				, "-joystickprovider", "none"
			};
		}

		private void UpdateFramerate()
		{
			VsyncNumerator = 1000000000;
			UInt64 ok = (UInt64)LibMAME.mame_lua_get_double(MAMELuaCommand.GetRefresh);
			VsyncDenominator = (int)(ok / 1000000000);
		}

		private void UpdateAspect()
		{
			double x = LibMAME.mame_lua_get_double(MAMELuaCommand.GetBoundX);
			double y = LibMAME.mame_lua_get_double(MAMELuaCommand.GetBoundY);
			double ratio = x / y;
			if (ratio <= 1)
			{
				//taller. expand height.
				VirtualWidth = BufferWidth;
				VirtualHeight = (int)(BufferWidth / ratio);
			}
			else
			{
				//wider. expand width.
				VirtualWidth = (int)(BufferHeight * ratio);
				VirtualHeight = BufferHeight;
			}
		}

		private void UpdateVideo()
		{
			int lengthInBytes;

			//int frame = LibMAME.mame_lua_get_int(MAMELuaCommand.GetFrameNumber);
			BufferWidth = LibMAME.mame_lua_get_int(MAMELuaCommand.GetWidth);
			BufferHeight = LibMAME.mame_lua_get_int(MAMELuaCommand.GetHeight);
			int expectedSize = BufferWidth * BufferHeight;
			int bytesPerPixel = 4;

			IntPtr ptr = LibMAME.mame_lua_get_string(MAMELuaCommand.GetPixels, out lengthInBytes);

			if (ptr == null)
			{
				Console.WriteLine("LibMAME ERROR: framebuffer pointer is null");
				return;
			}

			if (expectedSize * bytesPerPixel != lengthInBytes)
			{
				Console.WriteLine(
					"LibMAME ERROR: framebuffer has wrong size\n" +
					$"width:    {BufferWidth} pixels\n" +
					$"height:   {BufferHeight} pixels\n" +
					$"expected: {expectedSize * bytesPerPixel} bytes\n" +
					$"received: {lengthInBytes} bytes\n");
				return;
			}

			frameBuffer = new int[expectedSize];
			Marshal.Copy(ptr, frameBuffer, 0, expectedSize);
		//	string see = Marshal.PtrToStringAnsi(ptr, lengthInBytes);
			bool test = LibMAME.mame_lua_free_string(ptr);
		}

		private void UpdateAudio()
		{
			if (Frame == 0)
			{
				return;
			}
			int lengthInBytes;
			IntPtr ptr = LibMAME.mame_lua_get_string(MAMELuaCommand.GetSamples, out lengthInBytes);
			numSamples = lengthInBytes / 4;
			audioBuffer = new short[numSamples * 2];
			Marshal.Copy(ptr, audioBuffer, 0, numSamples * 2);
			bool test = LibMAME.mame_lua_free_string(ptr);
		}

		public bool FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			LibMAME.mame_lua_execute(MAMELuaCommand.Unpause);
			FrameWait();
			Frame++;
			CommandWait();
			return true;
		}

		private void MAMEFrameCallback()
		{
			LibMAME.mame_lua_execute(MAMELuaCommand.Step);
			UpdateFramerate();
			UpdateVideo();
			UpdateAspect();
			UpdateAudio();
			frameDone = true;
			MAMEFrameComplete.Set();
		}
		
		private void MAMEPeriodicCallback()
		{
			commandDone = true;
			MAMECommandComplete.Set();
		}
		
		private void MAMEBootCallback()
		{
			MAMEStartupComplete.Set();
		}
		
		// mame sends osd_output_channel casted to int, we implicitly cast is back
		private void MAMELogCallback(LibMAME.OutputChannel channel, int size, string data)
		{
			Console.WriteLine($"[MAME { channel.ToString() }] { data.Replace('\n', ' ') }");
		}

		private void FrameWait()
		{
			for (frameDone = false; frameDone == false;)
			{
				MAMEFrameComplete.WaitOne();
			}
		}

		private void CommandWait()
		{
			for (commandDone = false; commandDone == false;)
			{
				MAMECommandComplete.WaitOne();
			}
		}

		public static readonly ControllerDefinition MAMEController = new ControllerDefinition
		{
			Name = "MAME Controller",
			BoolButtons =
			{
				"Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "L", "R", "Power"
			}
		};

		public void ResetCounters()
		{
			Frame = 0;
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~MAME() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}

		#endregion

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = audioBuffer;
			nsamp = numSamples;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		public void DiscardSamples()
		{
			numSamples = 0;
		}

		private class MAMELuaCommand
		{
			public const string Step = "emu.step()";
			public const string Pause = "emu.pause()";
			public const string Unpause = "emu.unpause()";
			public const string GetPixels = "return manager:machine():video():pixels()";
			public const string GetSamples = "return manager:machine():sound():samples()";
			public const string GetWidth = "local w,h = manager:machine():video():size() return w";
			public const string GetHeight = "local w,h = manager:machine():video():size() return h";
			public const string GetFrameNumber = "for k,v in pairs(manager:machine().screens) do return v:frame_number() end";
			public const string GetRefresh = "for k,v in pairs(manager:machine().screens) do return v:refresh_attoseconds() end";
			public const string GetBoundX = "local x0,x1,y0,y1 = manager:machine():render():ui_target():view_bounds() return x1-x0";
			public const string GetBoundY = "local x0,x1,y0,y1 = manager:machine():render():ui_target():view_bounds() return y1-y0";
		}

	}
}
