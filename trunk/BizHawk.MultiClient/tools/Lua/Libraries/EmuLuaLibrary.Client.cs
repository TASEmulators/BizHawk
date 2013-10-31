using System;
using BizHawk.Client.Common;

namespace BizHawk.MultiClient
{
	public class MultiClientLuaLibrary : LuaLibraryBase
	{
		public MultiClientLuaLibrary(Action<string> logOutputCallback)
			: this()
		{
			LogOutputCallback = logOutputCallback;
		}

		public MultiClientLuaLibrary() : base() { }

		public override string Name { get { return "client"; } }
		public override string[] Functions
		{
			get
			{
				return new []
				{
					"closerom",
					"getwindowsize",
					"opencheats",
					"openhexeditor",
					"openramwatch",
					"openramsearch",
					"openrom",
					"opentasstudio",
					"opentoolbox",
					"opentracelogger",
					"pause_av",
					"reboot_core",
					"screenheight",
					"screenshot",
					"screenshottoclipboard",
					"screenwidth",
					"setscreenshotosd",
					"setwindowsize",
					"unpause_av",
					"xpos",
					"ypos",
				};
			}
		}

		public Action<string> LogOutputCallback = null;

		public static void client_closerom()
		{
			GlobalWinF.MainForm.CloseROM();
		}

		public static int client_getwindowsize()
		{
			return Global.Config.TargetZoomFactor;
		}

		public static void client_opencheats()
		{
			GlobalWinF.MainForm.LoadCheatsWindow();
		}

		public static void client_openhexeditor()
		{
			GlobalWinF.MainForm.LoadHexEditor();
		}

		public static void client_openramwatch()
		{
			GlobalWinF.MainForm.LoadRamWatch(true);
		}

		public static void client_openramsearch()
		{
			GlobalWinF.MainForm.LoadRamSearch();
		}

		public static void client_openrom(object lua_input)
		{
			GlobalWinF.MainForm.LoadRom(lua_input.ToString());
		}

		public static void client_opentasstudio()
		{
			GlobalWinF.MainForm.LoadTAStudio();
		}

		public static void client_opentoolbox()
		{
			GlobalWinF.MainForm.LoadToolBox();
		}

		public static void client_opentracelogger()
		{
			GlobalWinF.MainForm.LoadTraceLogger();
		}

		public static void client_pause_av()
		{
			GlobalWinF.MainForm.PauseAVI = true;
		}

		public static void client_reboot_core()
		{
			GlobalWinF.MainForm.RebootCore();
		}

		public static int client_screenheight()
		{
			return GlobalWinF.RenderPanel.NativeSize.Height;
		}

		public static void client_screenshot(object path = null)
		{
			if (path == null)
			{
				GlobalWinF.MainForm.TakeScreenshot();
			}
			else
			{
				GlobalWinF.MainForm.TakeScreenshot(path.ToString());
			}
		}

		public static void client_screenshottoclipboard()
		{
			GlobalWinF.MainForm.TakeScreenshotToClipboard();
		}

		public static void client_setscreenshotosd(bool value)
		{
			Global.Config.Screenshot_CaptureOSD = value;
		}

		public static int client_screenwidth()
		{
			return GlobalWinF.RenderPanel.NativeSize.Width;
		}

		public void client_setwindowsize(object window_size)
		{
			try
			{
				string temp = window_size.ToString();
				int size = Convert.ToInt32(temp);
				if (size == 1 || size == 2 || size == 3 || size == 4 || size == 5 || size == 10)
				{
					Global.Config.TargetZoomFactor = size;
					GlobalWinF.MainForm.FrameBufferResized();
					GlobalWinF.OSD.AddMessage("Window size set to " + size.ToString() + "x");
				}
				else
				{
					if (LogOutputCallback != null)
					{
						LogOutputCallback("Invalid window size");
					}
				}
			}
			catch
			{
				LogOutputCallback("Invalid window size");
			}

		}

		public static void client_unpause_av()
		{
			GlobalWinF.MainForm.PauseAVI = false;
		}

		public static int client_xpos()
		{
			return GlobalWinF.MainForm.DesktopLocation.X;
		}

		public static int client_ypos()
		{
			return GlobalWinF.MainForm.DesktopLocation.Y;
		}
	}
}
