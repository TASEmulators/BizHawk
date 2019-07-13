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
	public partial class MAME : IEmulator, IVideoProvider
	{
		public MAME(CoreComm comm, string dir, string file)
		{
			ServiceProvider = new BasicServiceProvider(this);

			CoreComm = comm;
			GameDirectory = dir;
			GameFilename = file;

			MAMEThread = new Thread(ExecuteMAMEThread);
			AsyncLaunchMAME();
		}

		public CoreComm CoreComm { get; private set; }
		public IEmulatorServiceProvider ServiceProvider { get; private set; }
		public ControllerDefinition ControllerDefinition => MAMEController;
		public string SystemId => "MAME";
		public int[] GetVideoBuffer() { return frameBuffer; }
		public int Frame { get; private set; }
		public bool DeterministicEmulation => true;
		public int VirtualWidth => 240;
		public int VirtualHeight => 320;
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int VsyncNumerator => 60;
		public int VsyncDenominator => 1;
		public int BackgroundColor => unchecked((int)0xFF0000FF);

		private Thread MAMEThread;
		private ManualResetEvent MAMEStartupComplete = new ManualResetEvent(false);
		private ManualResetEvent MAMEFrameComplete = new ManualResetEvent(false);
		private ManualResetEvent MAMECommandComplete = new ManualResetEvent(false);
		private bool FrameDone = false;
		private bool CommandDone = false;
		private string GameDirectory;
		private string GameFilename;
		private int[] frameBuffer = new int[0];

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

			string[] args = MakeCommandline(GameDirectory, GameFilename);
			LibMAME.mame_launch(args.Length, args);
		}
		
		// https://docs.mamedev.org/commandline/commandline-index.html
		private string[] MakeCommandline(string directory, string rom)
		{
			return new string[] {
				 "mame"                  // dummy, internally discarded by index, so has to go first
				, rom                    // no dash for rom names (internally called "unadorned" option)
				, "-rompath", directory  // mame doesn't load roms from full paths, only from dirs to scan
				, "-window"              // forbid fullscreen
				, "-nokeepaspect"        // forbid window stretching
				, "-nomaximize"          // forbid windowed fullscreen
				, "-noreadconfig"        // forbid reading any config files
				, "-norewind"            // forbid rewind savestates (captured upon frame advance)
				, "-skip_gameinfo"       // forbid this blocking screen that requires user input
				, "-video", "none"       // forbid mame window altogether
				, "-sound", "none"
				, "-output", "console"
				, "-keyboardprovider", "none"
				, "-mouseprovider",    "none"
				, "-lightgunprovider", "none"
				, "-joystickprovider", "none"
			};
		}

		private void UpdateVideo()
		{
			int lengthInBytes;

			int frame = LibMAME.mame_lua_get_int(MAMELuaCommand.GetFrame);
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
			UpdateVideo();
			FrameDone = true;
			MAMEFrameComplete.Set();
		}
		
		private void MAMEPeriodicCallback()
		{
			CommandDone = true;
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
			for (FrameDone = false; FrameDone == false;)
			{
				MAMEFrameComplete.WaitOne();
			}
		}

		private void CommandWait()
		{
			for (CommandDone = false; CommandDone == false;)
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

		private class MAMELuaCommand
		{
			public const string Step = "emu.step()";
			public const string Pause = "emu.pause()";
			public const string Unpause = "emu.unpause()";
			public const string GetWidth = "return manager:machine():render():ui_target():width()";
			public const string GetHeight = "return manager:machine():render():ui_target():height()";
			public const string GetPixels = "return manager:machine().screens[\":screen\"]:pixels()";
			public const string GetFrame = "return manager:machine().screens[\":screen\"]:frame_number()";
		}

	}
}
