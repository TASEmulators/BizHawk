using System;
using System.Collections.Generic;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class MultiClientLuaLibrary : LuaLibraryBase
	{
		private Dictionary<int, string> _filterMappings = new Dictionary<int,string>
			{
				{ 0, "None" },
				{ 1, "x2SAI" },
				{ 2, "SuperX2SAI" },
				{ 3, "SuperEagle" },
				{ 4, "Scanlines" },
			};

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
					"enablerewind",
					"frameskip",
					"getdisplayfilter",
					"gettargetscanlineintensity",
					"getwindowsize",
					"ispaused",
					"opencheats",
					"openhexeditor",
					"openramwatch",
					"openramsearch",
					"openrom",
					"opentasstudio",
					"opentoolbox",
					"opentracelogger",
					"paint",
					"pause",
					"pause_av",
					"reboot_core",
					"screenheight",
					"screenshot",
					"screenshottoclipboard",
					"screenwidth",
					"setdisplayfilter",
					"setscreenshotosd",
					"settargetscanlineintensity",
					"setwindowsize",
					"speedmode",
					"togglepause",
					"unpause",
					"unpause_av",
					"xpos",
					"ypos",
				};
			}
		}

		public Action<string> LogOutputCallback = null;

		public static void client_closerom()
		{
			GlobalWin.MainForm.CloseRom();
		}

		public static void client_enablerewind(object boolean)
		{
			string temp = boolean.ToString();
			if (!String.IsNullOrWhiteSpace(temp))
			{
				if (temp == "0" || temp.ToLower() == "false")
				{
					Global.Rewinder.RewindActive = false;
					GlobalWin.OSD.AddMessage("Rewind suspended");
				}
				else
				{
					Global.Rewinder.RewindActive = true;
					GlobalWin.OSD.AddMessage("Rewind enabled");
				}
			}
		}

		public void client_frameskip(object num_frames)
		{
			try
			{
				string temp = num_frames.ToString();
				int frames = Convert.ToInt32(temp);
				if (frames > 0)
				{
					Global.Config.FrameSkip = frames;
					GlobalWin.MainForm.FrameSkipMessage();
				}
				else
				{
					ConsoleLuaLibrary.console_log("Invalid frame skip value");
				}
			}
			catch
			{
				ConsoleLuaLibrary.console_log("Invalid frame skip value");
			}
		}

		public string client_getdisplayfilter()
		{
			return _filterMappings[Global.Config.TargetDisplayFilter];
		}

		private static int client_gettargetscanlineintensity()
		{
			return Global.Config.TargetScanlineFilterIntensity;
		}

		public static int client_getwindowsize()
		{
			return Global.Config.TargetZoomFactor;
		}

		public static bool client_ispaused()
		{
			return GlobalWin.MainForm.EmulatorPaused;
		}

		public static void client_opencheats()
		{
			GlobalWin.Tools.Load<Cheats>();
		}

		public static void client_openhexeditor()
		{
			GlobalWin.Tools.Load<HexEditor>();
		}

		public static void client_openramwatch()
		{
			GlobalWin.Tools.LoadRamWatch(true);
		}

		public static void client_openramsearch()
		{
			GlobalWin.Tools.Load<RamSearch>();
		}

		public static void client_openrom(object lua_input)
		{
			GlobalWin.MainForm.LoadRom(lua_input.ToString());
		}

		public static void client_opentasstudio()
		{
			GlobalWin.Tools.Load<TAStudio>();
		}

		public static void client_opentoolbox()
		{
			GlobalWin.Tools.Load<ToolBox>();
		}

		public static void client_opentracelogger()
		{
			GlobalWin.Tools.LoadTraceLogger();
		}

		public static void client_paint()
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
		}

		public static void client_pause()
		{
			GlobalWin.MainForm.PauseEmulator();
		}

		public static void client_pause_av()
		{
			GlobalWin.MainForm.PauseAVI = true;
		}

		public static void client_reboot_core()
		{
			GlobalWin.MainForm.RebootCore();
		}

		public static int client_screenheight()
		{
			return GlobalWin.RenderPanel.NativeSize.Height;
		}

		public static void client_screenshot(object path = null)
		{
			if (path == null)
			{
				GlobalWin.MainForm.TakeScreenshot();
			}
			else
			{
				GlobalWin.MainForm.TakeScreenshot(path.ToString());
			}
		}

		public static void client_screenshottoclipboard()
		{
			GlobalWin.MainForm.TakeScreenshotToClipboard();
		}

		public void client_setdisplayfilter(string filter)
		{
			foreach (var kvp in _filterMappings)
			{
				if (String.Equals(kvp.Value, filter, StringComparison.CurrentCultureIgnoreCase))
				{
					Global.Config.TargetDisplayFilter = kvp.Key;
					return;
				}
			}
		}

		private static void client_settargetscanlineintensity(int val)
		{
			Global.Config.TargetScanlineFilterIntensity = val;
		}

		public static void client_setscreenshotosd(bool value)
		{
			Global.Config.Screenshot_CaptureOSD = value;
		}

		public static int client_screenwidth()
		{
			return GlobalWin.RenderPanel.NativeSize.Width;
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
					GlobalWin.MainForm.FrameBufferResized();
					GlobalWin.OSD.AddMessage("Window size set to " + size.ToString() + "x");
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

		public void client_speedmode(object percent)
		{
			try
			{
				int speed = Convert.ToInt32(percent.ToString());
				if (speed > 0 && speed < 1600) //arbituarily capping it at 1600%
				{
					GlobalWin.MainForm.ClickSpeedItem(speed);
				}
				else
				{
					ConsoleLuaLibrary.console_log("Invalid speed value");
				}
			}
			catch
			{
				ConsoleLuaLibrary.console_log("Invalid speed value");
			}
		}

		public static void client_togglepause()
		{
			GlobalWin.MainForm.TogglePause();
		}

		public static void client_unpause()
		{
			GlobalWin.MainForm.UnpauseEmulator();
		}

		public static void client_unpause_av()
		{
			GlobalWin.MainForm.PauseAVI = false;
		}

		public static int client_xpos()
		{
			return GlobalWin.MainForm.DesktopLocation.X;
		}

		public static int client_ypos()
		{
			return GlobalWin.MainForm.DesktopLocation.Y;
		}
	}
}
