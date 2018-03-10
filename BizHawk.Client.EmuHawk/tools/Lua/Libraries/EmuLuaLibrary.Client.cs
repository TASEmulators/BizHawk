using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using NLua;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	[Description("A library for manipulating the EmuHawk client UI")]
	public sealed class EmuHawkLuaLibrary : LuaLibraryBase
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[RequiredService]
		private IVideoProvider VideoProvider { get; set; }

		private readonly Dictionary<int, string> _filterMappings = new Dictionary<int, string>
			{
				{ 0, "None" },
				{ 1, "x2SAI" },
				{ 2, "SuperX2SAI" },
				{ 3, "SuperEagle" },
				{ 4, "Scanlines" },
			};

		public EmuHawkLuaLibrary(Lua lua)
			: base(lua) { }

		public EmuHawkLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "client";

		[LuaMethod("exit", "Closes the emulator")]
		public void CloseEmulator()
		{
			GlobalWin.MainForm.CloseEmulator();
		}

		[LuaMethod("exitCode", "Closes the emulator and returns the provided code")]
		public void CloseEmulatorWithCode(int exitCode)
		{
			GlobalWin.MainForm.CloseEmulator(exitCode);
		}

		[LuaMethod("borderheight", "Gets the current height in pixels of the letter/pillarbox area (top side only) around the emu display surface, excluding the gameExtraPadding you've set. This function (the whole lot of them) should be renamed or refactored since the padding areas have got more complex.")]
		public static int BorderHeight()
		{
			var point = new System.Drawing.Point(0, 0);
			return GlobalWin.DisplayManager.TransformPoint(point).Y;
		}

		[LuaMethod("borderwidth", "Gets the current width in pixels of the letter/pillarbox area (left side only) around the emu display surface, excluding the gameExtraPadding you've set. This function (the whole lot of them) should be renamed or refactored since the padding areas have got more complex.")]
		public static int BorderWidth()
		{
			var point = new System.Drawing.Point(0, 0);
			return GlobalWin.DisplayManager.TransformPoint(point).X;
		}

		[LuaMethod("bufferheight", "Gets the visible height of the emu display surface (the core video output). This excludes the gameExtraPadding you've set.")]
		public int BufferHeight()
		{
			return VideoProvider.BufferHeight;
		}

		[LuaMethod("bufferwidth", "Gets the visible width of the emu display surface (the core video output). This excludes the gameExtraPadding you've set.")]
		public int BufferWidth()
		{
			return VideoProvider.BufferWidth;
		}

		[LuaMethod("clearautohold", "Clears all autohold keys")]
		public void ClearAutohold()
		{
			GlobalWin.MainForm.ClearHolds();
		}

		[LuaMethod("closerom", "Closes the loaded Rom")]
		public static void CloseRom()
		{
			GlobalWin.MainForm.CloseRom();
		}

		[LuaMethod("enablerewind", "Sets whether or not the rewind feature is enabled")]
		public void EnableRewind(bool enabled)
		{
			GlobalWin.MainForm.EnableRewind(enabled);
		}

		[LuaMethod("frameskip", "Sets the frame skip value of the client UI")]
		public void FrameSkip(int numFrames)
		{
			if (numFrames > 0)
			{
				Global.Config.FrameSkip = numFrames;
				GlobalWin.MainForm.FrameSkipMessage();
			}
			else
			{
				Log("Invalid frame skip value");
			}
		}

		[LuaMethod("gettargetscanlineintensity", "Gets the current scanline intensity setting, used for the scanline display filter")]
		public static int GetTargetScanlineIntensity()
		{
			return Global.Config.TargetScanlineFilterIntensity;
		}

		[LuaMethod("getwindowsize", "Gets the main window's size Possible values are 1, 2, 3, 4, 5, and 10")]
		public int GetWindowSize()
		{
			return Global.Config.TargetZoomFactors[Emulator.SystemId];
		}

		[LuaMethod("SetGameExtraPadding", "Sets the extra padding added to the 'emu' surface so that you can draw HUD elements in predictable placements")]
		public static void SetGameExtraPadding(int left, int top, int right, int bottom)
		{
			GlobalWin.DisplayManager.GameExtraPadding = new System.Windows.Forms.Padding(left, top, right, bottom);
			GlobalWin.MainForm.FrameBufferResized();
		}

		[LuaMethod("SetSoundOn", "Sets the state of the Sound On toggle")]
		public static void SetSoundOn(bool enable)
		{
			Global.Config.SoundEnabled = enable;
		}

		[LuaMethod("GetSoundOn", "Gets the state of the Sound On toggle")]
		public static bool GetSoundOn()
		{
			return Global.Config.SoundEnabled;
		}

		[LuaMethod("SetClientExtraPadding", "Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements")]
		public static void SetClientExtraPadding(int left, int top, int right, int bottom)
		{
			GlobalWin.DisplayManager.ClientExtraPadding = new System.Windows.Forms.Padding(left, top, right, bottom);
			GlobalWin.MainForm.FrameBufferResized();
		}

		[LuaMethod("ispaused", "Returns true if emulator is paused, otherwise, false")]
		public static bool IsPaused()
		{
			return GlobalWin.MainForm.EmulatorPaused;
		}

		[LuaMethod("isturbo", "Returns true if emulator is in turbo mode, otherwise, false")]
		public static bool IsTurbo()
		{
			return GlobalWin.MainForm.IsTurboing;
		}

		[LuaMethod("isseeking", "Returns true if emulator is seeking, otherwise, false")]
		public static bool IsSeeking()
		{
			return GlobalWin.MainForm.IsSeeking;
		}

		[LuaMethod("opencheats", "opens the Cheats dialog")]
		public static void OpenCheats()
		{
			GlobalWin.Tools.Load<Cheats>();
		}

		[LuaMethod("openhexeditor", "opens the Hex Editor dialog")]
		public static void OpenHexEditor()
		{
			GlobalWin.Tools.Load<HexEditor>();
		}

		[LuaMethod("openramwatch", "opens the RAM Watch dialog")]
		public static void OpenRamWatch()
		{
			GlobalWin.Tools.LoadRamWatch(loadDialog: true);
		}

		[LuaMethod("openramsearch", "opens the RAM Search dialog")]
		public static void OpenRamSearch()
		{
			GlobalWin.Tools.Load<RamSearch>();
		}

		[LuaMethod("openrom", "opens the Open ROM dialog")]
		public static void OpenRom(string path)
		{
			var ioa = OpenAdvancedSerializer.ParseWithLegacy(path);
			GlobalWin.MainForm.LoadRom(path, new MainForm.LoadRomArgs { OpenAdvanced = ioa });
		}

		[LuaMethod("opentasstudio", "opens the TAStudio dialog")]
		public static void OpenTasStudio()
		{
			GlobalWin.Tools.Load<TAStudio>();
		}

		[LuaMethod("opentoolbox", "opens the Toolbox Dialog")]
		public static void OpenToolBox()
		{
			GlobalWin.Tools.Load<ToolBox>();
		}

		[LuaMethod("opentracelogger", "opens the tracelogger if it is available for the given core")]
		public static void OpenTraceLogger()
		{
			GlobalWin.Tools.Load<TraceLogger>();
		}

		[LuaMethod("pause", "Pauses the emulator")]
		public static void Pause()
		{
			GlobalWin.MainForm.PauseEmulator();
		}

		[LuaMethod("pause_av", "If currently capturing Audio/Video, this will suspend the record. Frames will not be captured into the AV until client.unpause_av() is called")]
		public static void PauseAv()
		{
			GlobalWin.MainForm.PauseAvi = true;
		}

		[LuaMethod("reboot_core", "Reboots the currently loaded core")]
		public static void RebootCore()
		{
			((LuaConsole)GlobalWin.Tools.Get<LuaConsole>()).LuaImp.IsRebootingCore = true;
			GlobalWin.MainForm.RebootCore();
			((LuaConsole)GlobalWin.Tools.Get<LuaConsole>()).LuaImp.IsRebootingCore = false;
		}

		[LuaMethod("screenheight", "Gets the current height in pixels of the emulator's drawing area")]
		public static int ScreenHeight()
		{
			return GlobalWin.MainForm.PresentationPanel.NativeSize.Height;
		}

		[LuaMethod("screenshot", "if a parameter is passed it will function as the Screenshot As menu item of EmuHawk, else it will function as the Screenshot menu item")]
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

		[LuaMethod("screenshottoclipboard", "Performs the same function as EmuHawk's Screenshot To Clipboard menu item")]
		public static void ScreenshotToClipboard()
		{
			GlobalWin.MainForm.TakeScreenshotToClipboard();
		}

		[LuaMethod("settargetscanlineintensity", "Sets the current scanline intensity setting, used for the scanline display filter")]
		public static void SetTargetScanlineIntensity(int val)
		{
			Global.Config.TargetScanlineFilterIntensity = val;
		}

		[LuaMethod("setscreenshotosd", "Sets the screenshot Capture OSD property of the client")]
		public static void SetScreenshotOSD(bool value)
		{
			Global.Config.Screenshot_CaptureOSD = value;
		}

		[LuaMethod("screenwidth", "Gets the current width in pixels of the emulator's drawing area")]
		public static int ScreenWidth()
		{
			return GlobalWin.MainForm.PresentationPanel.NativeSize.Width;
		}

		[LuaMethod("setwindowsize", "Sets the main window's size to the give value. Accepted values are 1, 2, 3, 4, 5, and 10")]
		public void SetWindowSize(int size)
		{
			if (size == 1 || size == 2 || size == 3 || size == 4 || size == 5 || size == 10)
			{
				Global.Config.TargetZoomFactors[Emulator.SystemId] = size;
				GlobalWin.MainForm.FrameBufferResized();
				GlobalWin.OSD.AddMessage("Window size set to " + size + "x");
			}
			else
			{
				Log("Invalid window size");
			}
		}

		[LuaMethod("speedmode", "Sets the speed of the emulator (in terms of percent)")]
		public void SpeedMode(int percent)
		{
			if (percent > 0 && percent < 6400)
			{
				GlobalWin.MainForm.ClickSpeedItem(percent);
			}
			else
			{
				Log("Invalid speed value");
			}
		}

		[LuaMethod("togglepause", "Toggles the current pause state")]
		public static void TogglePause()
		{
			GlobalWin.MainForm.TogglePause();
		}
		
		[LuaMethod("transformPointX", "Transforms an x-coordinate in emulator space to an x-coordinate in client space")]
		public static int TransformPointX(int x)
		{
			var point = new System.Drawing.Point(x, 0);
			return GlobalWin.DisplayManager.TransformPoint(point).X;
		}

		[LuaMethod("transformPointY", "Transforms an y-coordinate in emulator space to an y-coordinate in client space")]
		public static int TransformPointY(int y)
		{
			var point = new System.Drawing.Point(0, y);
			return GlobalWin.DisplayManager.TransformPoint(point).Y;
		}

		[LuaMethod("unpause", "Unpauses the emulator")]
		public static void Unpause()
		{
			GlobalWin.MainForm.UnpauseEmulator();
		}

		[LuaMethod("unpause_av", "If currently capturing Audio/Video this resumes capturing")]
		public static void UnpauseAv()
		{
			GlobalWin.MainForm.PauseAvi = false;
		}

		[LuaMethod("xpos", "Returns the x value of the screen position where the client currently sits")]
		public static int Xpos()
		{
			return GlobalWin.MainForm.DesktopLocation.X;
		}

		[LuaMethod("ypos", "Returns the y value of the screen position where the client currently sits")]
		public static int Ypos()
		{
			return GlobalWin.MainForm.DesktopLocation.Y;
		}

		[LuaMethod("getavailabletools", "Returns a list of the tools currently open")]
		public LuaTable GetAvailableTools()
		{
			var t = Lua.NewTable();
			var tools = GlobalWin.Tools.AvailableTools.ToList();
			for (int i = 0; i < tools.Count; i++)
			{
				t[i] = tools[i].Name.ToLower();
			}

			return t;
		}

		[LuaMethod("gettool", "Returns an object that represents a tool of the given name (not case sensitive). If the tool is not open, it will be loaded if available. Use gettools to get a list of names")]
		public LuaTable GetTool(string name)
		{
			var toolType  = ReflectionUtil.GetTypeByName(name)
				.FirstOrDefault(x => typeof(IToolForm).IsAssignableFrom(x) && !x.IsInterface);

			if (toolType != null)
			{
				GlobalWin.Tools.Load(toolType);
			}

			var selectedTool = GlobalWin.Tools.AvailableTools
				.FirstOrDefault(tool => tool.GetType().Name.ToLower() == name.ToLower());

			if (selectedTool != null)
			{
				return LuaHelper.ToLuaTable(Lua, selectedTool);
			}

			return null;
		}

		[LuaMethod("createinstance", "returns a default instance of the given type of object if it exists (not case sensitive). Note: This will only work on objects which have a parameterless constructor.  If no suitable type is found, or the type does not have a parameterless constructor, then nil is returned")]
		public LuaTable CreateInstance(string name)
		{
			var possibleTypes = ReflectionUtil.GetTypeByName(name);

			if (possibleTypes.Any())
			{
				var instance = Activator.CreateInstance(possibleTypes.First());
				return LuaHelper.ToLuaTable(Lua, instance);
			}

			return null;
		}

		[LuaMethod("displaymessages", "sets whether or not on screen messages will display")]
		public void DisplayMessages(bool value)
		{
			Global.Config.DisplayMessages = value;
		}

		[LuaMethod("saveram", "flushes save ram to disk")]
		public void SaveRam()
		{
			GlobalWin.MainForm.FlushSaveRAM();
		}
	}
}
