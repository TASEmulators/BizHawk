using System;
using System.Collections.Generic;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class EmuHawkLuaLibrary : LuaLibraryBase
	{
		private readonly Dictionary<int, string> _filterMappings = new Dictionary<int, string>
			{
				{ 0, "None" },
				{ 1, "x2SAI" },
				{ 2, "SuperX2SAI" },
				{ 3, "SuperEagle" },
				{ 4, "Scanlines" },
			};

		public EmuHawkLuaLibrary(Action<string> logOutputCallback)
			: this()
		{
			LogOutputCallback = logOutputCallback;
		}

		public EmuHawkLuaLibrary() { }

		public override string Name { get { return "client"; } }
		public Action<string> LogOutputCallback { get; set; }

		private void Log(string message)
		{
			if (LogOutputCallback != null)
			{
				LogOutputCallback(message);
			}
		}

		[LuaMethodAttributes(
			"clearautohold",
			"Clears all autohold keys"
		)]
		public void ClearAutohold()
		{
			GlobalWin.MainForm.ClearHolds();
		}

		[LuaMethodAttributes(
			"closerom",
			"Closes the loaded Rom"
		)]
		public static void CloseRom()
		{
			GlobalWin.MainForm.CloseRom();
		}

		[LuaMethodAttributes(
			"enablerewind",
			"Sets whether or not the rewind feature is enabled"
		)]
		public static void EnableRewind(bool enabled)
		{
			if (enabled)
			{
				Global.Rewinder.RewindActive = true;
				GlobalWin.OSD.AddMessage("Rewind enabled");
			}
			else
			{
				Global.Rewinder.RewindActive = false;
				GlobalWin.OSD.AddMessage("Rewind suspended");
			}
		}

		[LuaMethodAttributes(
			"frameskip",
			"Sets the frame skip value of the client UI"
		)]
		public void FrameSkip(int numFrames)
		{
			var frames = LuaInt(numFrames);
			if (frames > 0)
			{
				Global.Config.FrameSkip = frames;
				GlobalWin.MainForm.FrameSkipMessage();
			}
			else
			{
				ConsoleLuaLibrary.Log("Invalid frame skip value");
			}
		}

		[LuaMethodAttributes(
			"getdisplayfilter",
			"Gets the current display filter setting, possible values: 'None', 'x2SAI', 'SuperX2SAI', 'SuperEagle', 'Scanlines'"
		)]
		public string GetDisplayFilter()
		{
			return _filterMappings[Global.Config.TargetDisplayFilter];
		}

		[LuaMethodAttributes(
			"gettargetscanlineintensity",
			"TODO"
		)]
		public static int GetTargetScanlineIntensity()
		{
			return Global.Config.TargetScanlineFilterIntensity;
		}

		[LuaMethodAttributes(
			"getwindowsize",
			"TODO"
		)]
		public static int GetWindowSize()
		{
			return Global.Config.TargetZoomFactor;
		}

		[LuaMethodAttributes(
			"ispaused",
			"TODO"
		)]
		public static bool IsPaused()
		{
			return GlobalWin.MainForm.EmulatorPaused;
		}

		[LuaMethodAttributes(
			"opencheats",
			"TODO"
		)]
		public static void OpenCheats()
		{
			GlobalWin.Tools.Load<Cheats>();
		}

		[LuaMethodAttributes(
			"openhexeditor",
			"TODO"
		)]
		public static void OpenHexEditor()
		{
			GlobalWin.Tools.Load<HexEditor>();
		}

		[LuaMethodAttributes(
			"openramwatch",
			"TODO"
		)]
		public static void OpenRamWatch()
		{
			GlobalWin.Tools.LoadRamWatch(loadDialog: true);
		}

		[LuaMethodAttributes(
			"openramsearch",
			"TODO"
		)]
		public static void OpenRamSearch()
		{
			GlobalWin.Tools.Load<RamSearch>();
		}

		[LuaMethodAttributes(
			"openrom",
			"TODO"
		)]
		public static void OpenRom(string path)
		{
			GlobalWin.MainForm.LoadRom(path);
		}

		[LuaMethodAttributes(
			"opentasstudio",
			"TODO"
		)]
		public static void OpenTasStudio()
		{
			GlobalWin.Tools.Load<TAStudio>();
		}

		[LuaMethodAttributes(
			"opentoolbox",
			"TODO"
		)]
		public static void OpenToolBox()
		{
			GlobalWin.Tools.Load<ToolBox>();
		}

		[LuaMethodAttributes(
			"opentracelogger",
			"TODO"
		)]
		public static void OpenTraceLogger()
		{
			GlobalWin.Tools.LoadTraceLogger();
		}

		[LuaMethodAttributes(
			"paint",
			"TODO"
		)]
		public static void Paint()
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
		}

		[LuaMethodAttributes(
			"pause",
			"TODO"
		)]
		public static void Pause()
		{
			GlobalWin.MainForm.PauseEmulator();
		}

		[LuaMethodAttributes(
			"pause_av",
			"TODO"
		)]
		public static void PauseAv()
		{
			GlobalWin.MainForm.PauseAVI = true;
		}

		[LuaMethodAttributes(
			"reboot_core",
			"TODO"
		)]
		public static void RebootCore()
		{
			GlobalWin.MainForm.RebootCore();
		}

		[LuaMethodAttributes(
			"screenheight",
			"TODO"
		)]
		public static int ScreenHeight()
		{
			return GlobalWin.RenderPanel.NativeSize.Height;
		}

		[LuaMethodAttributes(
			"screenshot",
			"TODO"
		)]
		public static void Screenshot(string path = null)
		{
			if (path == null)
			{
				GlobalWin.MainForm.TakeScreenshot();
			}
			else
			{
				GlobalWin.MainForm.TakeScreenshot(path);
			}
		}

		[LuaMethodAttributes(
			"screenshottoclipboard",
			"TODO"
		)]
		public static void ScreenshotToClipboard()
		{
			GlobalWin.MainForm.TakeScreenshotToClipboard();
		}

		[LuaMethodAttributes(
			"setdisplayfilter",
			"TODO"
		)]
		public void SetDisplayFilter(string filter)
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

		[LuaMethodAttributes(
			"settargetscanlineintensity",
			"TODO"
		)]
		public static void SetTargetScanlineIntensity(int val)
		{
			Global.Config.TargetScanlineFilterIntensity = val;
		}

		[LuaMethodAttributes(
			"setscreenshotosd",
			"TODO"
		)]
		public static void SetScreenshotOSD(bool value)
		{
			Global.Config.Screenshot_CaptureOSD = value;
		}

		[LuaMethodAttributes(
			"screenwidth",
			"TODO"
		)]
		public static int ScreenWidth()
		{
			return GlobalWin.RenderPanel.NativeSize.Width;
		}

		[LuaMethodAttributes(
			"setwindowsize",
			"TODO"
		)]
		public void SetWindowSize(int size)
		{
			var s = LuaInt(size);
			if (s == 1 || s == 2 || s == 3 || s == 4 || s == 5 || s == 10)
			{
				Global.Config.TargetZoomFactor = s;
				GlobalWin.MainForm.FrameBufferResized();
				GlobalWin.OSD.AddMessage("Window size set to " + s + "x");
			}
			else
			{
				Log("Invalid window size");
			}
		}

		[LuaMethodAttributes(
			"speedmode",
			"TODO"
		)]
		public void SpeedMode(int percent)
		{
			var speed = LuaInt(percent);
			if (speed > 0 && speed < 6400)
			{
				GlobalWin.MainForm.ClickSpeedItem(speed);
			}
			else
			{
				Log("Invalid speed value");
			}
		}

		[LuaMethodAttributes(
			"togglepause",
			"TODO"
		)]
		public static void TogglePause()
		{
			GlobalWin.MainForm.TogglePause();
		}

		[LuaMethodAttributes(
			"unpause",
			"TODO"
		)]
		public static void Unpause()
		{
			GlobalWin.MainForm.UnpauseEmulator();
		}

		[LuaMethodAttributes(
			"unpause_av",
			"TODO"
		)]
		public static void UnpauseAv()
		{
			GlobalWin.MainForm.PauseAVI = false;
		}

		[LuaMethodAttributes(
			"xpos",
			"TODO"
		)]
		public static int Xpos()
		{
			return GlobalWin.MainForm.DesktopLocation.X;
		}

		[LuaMethodAttributes(
			"ypos",
			"TODO"
		)]
		public static int Ypos()
		{
			return GlobalWin.MainForm.DesktopLocation.Y;
		}
	}
}
