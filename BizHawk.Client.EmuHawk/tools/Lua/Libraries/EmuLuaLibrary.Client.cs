using System;
﻿using System.Drawing;
using System.ComponentModel;
using System.Linq;

using NLua;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using System.Threading;
using System.Diagnostics;

using BizHawk.Common;
using BizHawk.Client.ApiHawk;

// ReSharper disable StringLiteralTypo
// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.EmuHawk
{
	[Description("A library for manipulating the EmuHawk client UI")]
	public sealed class EmuHawkLuaLibrary : DelegatingLuaLibraryEmu
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[RequiredService]
		private IVideoProvider VideoProvider { get; set; }

		public MainForm MainForm { get; set; }

		public EmuHawkLuaLibrary(Lua lua)
			: base(lua) { }

		public EmuHawkLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "client";

		[LuaMethodExample("client.exit( );")]
		[LuaMethod("exit", "Closes the emulator")]
		public void CloseEmulator()
		{
			MainForm.CloseEmulator();
		}

		[LuaMethodExample("client.exitCode( 0 );")]
		[LuaMethod("exitCode", "Closes the emulator and returns the provided code")]
		public void CloseEmulatorWithCode(int exitCode)
		{
			MainForm.CloseEmulator(exitCode);
		}

		[LuaMethodExample("local inclibor = client.borderheight( );")]
		[LuaMethod("borderheight", "Gets the current height in pixels of the letter/pillarbox area (top side only) around the emu display surface, excluding the gameExtraPadding you've set. This function (the whole lot of them) should be renamed or refactored since the padding areas have got more complex.")]
		public static int BorderHeight()
		{
			var point = new System.Drawing.Point(0, 0);
			return GlobalWin.DisplayManager.TransformPoint(point).Y;
		}

		[LuaMethodExample("local inclibor = client.borderwidth( );")]
		[LuaMethod("borderwidth", "Gets the current width in pixels of the letter/pillarbox area (left side only) around the emu display surface, excluding the gameExtraPadding you've set. This function (the whole lot of them) should be renamed or refactored since the padding areas have got more complex.")]
		public static int BorderWidth()
		{
			var point = new System.Drawing.Point(0, 0);
			return GlobalWin.DisplayManager.TransformPoint(point).X;
		}

		[LuaMethodExample("local inclibuf = client.bufferheight( );")]
		[LuaMethod("bufferheight", "Gets the visible height of the emu display surface (the core video output). This excludes the gameExtraPadding you've set.")]
		public int BufferHeight()
		{
			return VideoProvider.BufferHeight;
		}

		[LuaMethodExample("local inclibuf = client.bufferwidth( );")]
		[LuaMethod("bufferwidth", "Gets the visible width of the emu display surface (the core video output). This excludes the gameExtraPadding you've set.")]
		public int BufferWidth()
		{
			return VideoProvider.BufferWidth;
		}

		[LuaMethodExample("client.clearautohold( );")]
		[LuaMethod("clearautohold", "Clears all autohold keys")]
		public void ClearAutohold()
		{
			MainForm.ClearHolds();
		}

		[LuaMethodExample("client.closerom( );")]
		[LuaMethod("closerom", "Closes the loaded Rom")]
		public void CloseRom()
		{
			MainForm.CloseRom();
		}

		[LuaMethodExample("client.enablerewind( true );")]
		[LuaMethod("enablerewind", "Sets whether or not the rewind feature is enabled")]
		public void EnableRewind(bool enabled)
		{
			MainForm.EnableRewind(enabled);
		}

		[LuaMethodExample("client.frameskip( 8 );")]
		[LuaMethod("frameskip", "Sets the frame skip value of the client UI (use 0 to disable)")]
		public void FrameSkip(int numFrames)
		{
			if (numFrames >= 0)
			{
				Global.Config.FrameSkip = numFrames;
				MainForm.FrameSkipMessage();
			}
			else
			{
				Log("Invalid frame skip value");
			}
		}

		/// <summary>
		/// Use with <see cref="SeekFrame(int)"/> for CamHack.
		/// Refer to <see cref="MainForm.InvisibleEmulation"/> for the workflow details.
		/// </summary>
		[LuaMethodExample("client.invisibleemulation( true );")]
		[LuaMethod("invisibleemulation", "Disables and enables emulator updates")]
		public void InvisibleEmulation(bool invisible)
		{
			MainForm.InvisibleEmulation = invisible;
		}

		/// <summary>
		/// Use with <see cref="InvisibleEmulation(bool)"/> for CamHack.
		/// Refer to <see cref="MainForm.InvisibleEmulation"/> for the workflow details.
		/// </summary>
		[LuaMethodExample("client.seekframe( 100 );")]
		[LuaMethod("seekframe", "Makes the emulator seek to the frame specified")]
		public void SeekFrame(int frame)
		{
			bool wasPaused = MainForm.EmulatorPaused;

			// can't re-enter lua while doing this
			MainForm.SuppressLua = true;
			while (Emulator.Frame != frame)
			{
				MainForm.SeekFrameAdvance();
			}

			MainForm.SuppressLua = false;

			if (!wasPaused)
			{
				MainForm.UnpauseEmulator();
			}
		}

		[LuaMethodExample("local incliget = client.gettargetscanlineintensity( );")]
		[LuaMethod("gettargetscanlineintensity", "Gets the current scanline intensity setting, used for the scanline display filter")]
		public static int GetTargetScanlineIntensity()
		{
			return Global.Config.TargetScanlineFilterIntensity;
		}

		[LuaMethodExample("local incliget = client.getwindowsize( );")]
		[LuaMethod("getwindowsize", "Gets the main window's size Possible values are 1, 2, 3, 4, 5, and 10")]
		public int GetWindowSize()
		{
			return Global.Config.TargetZoomFactors[Emulator.SystemId];
		}

		[LuaMethodExample("client.SetGameExtraPadding( 5, 10, 15, 20 );")]
		[LuaMethod("SetGameExtraPadding", "Sets the extra padding added to the 'emu' surface so that you can draw HUD elements in predictable placements")]
		public void SetGameExtraPadding(int left, int top, int right, int bottom)
		{
			GlobalWin.DisplayManager.GameExtraPadding = new System.Windows.Forms.Padding(left, top, right, bottom);
			MainForm.FrameBufferResized();
		}

		[LuaMethodExample("client.SetSoundOn( true );")]
		[LuaMethod("SetSoundOn", "Sets the state of the Sound On toggle")]
		public static void SetSoundOn(bool enable) => ClientApi.SetSoundOn(enable);

		[LuaMethodExample("if ( client.GetSoundOn( ) ) then\r\n\tconsole.log( \"Gets the state of the Sound On toggle\" );\r\nend;")]
		[LuaMethod("GetSoundOn", "Gets the state of the Sound On toggle")]
		public static bool GetSoundOn() => ClientApi.GetSoundOn();

		[LuaMethodExample("client.SetClientExtraPadding( 5, 10, 15, 20 );")]
		[LuaMethod("SetClientExtraPadding", "Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements")]
		public void SetClientExtraPadding(int left, int top, int right, int bottom)
		{
			GlobalWin.DisplayManager.ClientExtraPadding = new System.Windows.Forms.Padding(left, top, right, bottom);
			MainForm.FrameBufferResized();
		}

		[LuaMethodExample("if ( client.ispaused( ) ) then\r\n\tconsole.log( \"Returns true if emulator is paused, otherwise, false\" );\r\nend;")]
		[LuaMethod("ispaused", "Returns true if emulator is paused, otherwise, false")]
		public bool IsPaused()
		{
			return MainForm.EmulatorPaused;
		}

		[LuaMethodExample("if ( client.client.isturbo( ) ) then\r\n\tconsole.log( \"Returns true if emulator is in turbo mode, otherwise, false\" );\r\nend;")]
		[LuaMethod("isturbo", "Returns true if emulator is in turbo mode, otherwise, false")]
		public bool IsTurbo()
		{
			return MainForm.IsTurboing;
		}

		[LuaMethodExample("if ( client.isseeking( ) ) then\r\n\tconsole.log( \"Returns true if emulator is seeking, otherwise, false\" );\r\nend;")]
		[LuaMethod("isseeking", "Returns true if emulator is seeking, otherwise, false")]
		public bool IsSeeking()
		{
			return MainForm.IsSeeking;
		}

		[LuaMethodExample("client.opencheats( );")]
		[LuaMethod("opencheats", "opens the Cheats dialog")]
		public void OpenCheats() => APIs.Tool.OpenCheats();

		[LuaMethodExample("client.openhexeditor( );")]
		[LuaMethod("openhexeditor", "opens the Hex Editor dialog")]
		public void OpenHexEditor() => APIs.Tool.OpenHexEditor();

		[LuaMethodExample("client.openramwatch( );")]
		[LuaMethod("openramwatch", "opens the RAM Watch dialog")]
		public void OpenRamWatch() => APIs.Tool.OpenRamWatch();

		[LuaMethodExample("client.openramsearch( );")]
		[LuaMethod("openramsearch", "opens the RAM Search dialog")]
		public void OpenRamSearch() => APIs.Tool.OpenRamSearch();

		[LuaMethodExample("client.openrom( \"C:\\\" );")]
		[LuaMethod("openrom", "opens the Open ROM dialog")]
		public void OpenRom(string path)
		{
			var ioa = OpenAdvancedSerializer.ParseWithLegacy(path);
			MainForm.LoadRom(path, new MainForm.LoadRomArgs { OpenAdvanced = ioa });
		}

		[LuaMethodExample("client.opentasstudio( );")]
		[LuaMethod("opentasstudio", "opens the TAStudio dialog")]
		public void OpenTasStudio() => APIs.Tool.OpenTasStudio();

		[LuaMethodExample("client.opentoolbox( );")]
		[LuaMethod("opentoolbox", "opens the Toolbox Dialog")]
		public void OpenToolBox() => APIs.Tool.OpenToolBox();

		[LuaMethodExample("client.opentracelogger( );")]
		[LuaMethod("opentracelogger", "opens the tracelogger if it is available for the given core")]
		public void OpenTraceLogger() => APIs.Tool.OpenTraceLogger();

		[LuaMethodExample("client.pause( );")]
		[LuaMethod("pause", "Pauses the emulator")]
		public void Pause()
		{
			MainForm.PauseEmulator();
		}

		[LuaMethodExample("client.pause_av( );")]
		[LuaMethod("pause_av", "If currently capturing Audio/Video, this will suspend the record. Frames will not be captured into the AV until client.unpause_av() is called")]
		public void PauseAv()
		{
			MainForm.PauseAvi = true;
		}

		[LuaMethodExample("client.reboot_core( );")]
		[LuaMethod("reboot_core", "Reboots the currently loaded core")]
		public void RebootCore()
		{
			((LuaConsole)GlobalWin.Tools.Get<LuaConsole>()).LuaImp.IsRebootingCore = true;
			MainForm.RebootCore();
			((LuaConsole)GlobalWin.Tools.Get<LuaConsole>()).LuaImp.IsRebootingCore = false;
		}

		[LuaMethodExample("local incliscr = client.screenheight( );")]
		[LuaMethod("screenheight", "Gets the current height in pixels of the emulator's drawing area")]
		public int ScreenHeight()
		{
			return MainForm.PresentationPanel.NativeSize.Height;
		}

		[LuaMethodExample("client.screenshot( \"C:\\\" );")]
		[LuaMethod("screenshot", "if a parameter is passed it will function as the Screenshot As menu item of EmuHawk, else it will function as the Screenshot menu item")]
		public void Screenshot(string path = null)
		{
			if (path == null)
			{
				MainForm.TakeScreenshot();
			}
			else
			{
				MainForm.TakeScreenshot(path);
			}
		}

		[LuaMethodExample("client.screenshottoclipboard( );")]
		[LuaMethod("screenshottoclipboard", "Performs the same function as EmuHawk's Screenshot To Clipboard menu item")]
		public void ScreenshotToClipboard()
		{
			MainForm.TakeScreenshotToClipboard();
		}

		[LuaMethodExample("client.settargetscanlineintensity( -1000 );")]
		[LuaMethod("settargetscanlineintensity", "Sets the current scanline intensity setting, used for the scanline display filter")]
		public static void SetTargetScanlineIntensity(int val)
		{
			Global.Config.TargetScanlineFilterIntensity = val;
		}

		[LuaMethodExample("client.setscreenshotosd( true );")]
		[LuaMethod("setscreenshotosd", "Sets the screenshot Capture OSD property of the client")]
		public static void SetScreenshotOSD(bool value)
		{
			Global.Config.ScreenshotCaptureOsd = value;
		}

		[LuaMethodExample("local incliscr = client.screenwidth( );")]
		[LuaMethod("screenwidth", "Gets the current width in pixels of the emulator's drawing area")]
		public int ScreenWidth()
		{
			return MainForm.PresentationPanel.NativeSize.Width;
		}

		[LuaMethodExample("client.setwindowsize( 100 );")]
		[LuaMethod("setwindowsize", "Sets the main window's size to the give value. Accepted values are 1, 2, 3, 4, 5, and 10")]
		public void SetWindowSize(int size)
		{
			if (size == 1 || size == 2 || size == 3 || size == 4 || size == 5 || size == 10)
			{
				Global.Config.TargetZoomFactors[Emulator.SystemId] = size;
				MainForm.FrameBufferResized();
				MainForm.AddOnScreenMessage($"Window size set to {size}x");
			}
			else
			{
				Log("Invalid window size");
			}
		}

		[LuaMethodExample("client.speedmode( 75 );")]
		[LuaMethod("speedmode", "Sets the speed of the emulator (in terms of percent)")]
		public void SpeedMode(int percent)
		{
			if (percent.StrictlyBoundedBy(0.RangeTo(6400)))
			{
				MainForm.ClickSpeedItem(percent);
			}
			else
			{
				Log("Invalid speed value");
			}
		}

		[LuaMethodExample("local curSpeed = client.getconfig().SpeedPercent")]
		[LuaMethod("getconfig", "gets the current config settings object")]
		public object GetConfig()
		{
			return Global.Config;
		}

		[LuaMethodExample("client.togglepause( );")]
		[LuaMethod("togglepause", "Toggles the current pause state")]
		public void TogglePause()
		{
			MainForm.TogglePause();
		}

		[LuaMethodExample("local newY = client.transform_point( 32, 100 ).y;")]
		[LuaMethod("transformPoint", "Transforms a point (x, y) in emulator space to a point in client space")]
		public LuaTable TransformPoint(int x, int y) {
			var transformed = ClientApi.TransformPoint(new Point(x, y));
			var table = Lua.NewTable();
			table["x"] = transformed.X;
			table["y"] = transformed.Y;
			return table;
		}

		[LuaMethodExample("client.unpause( );")]
		[LuaMethod("unpause", "Unpauses the emulator")]
		public void Unpause()
		{
			MainForm.UnpauseEmulator();
		}

		[LuaMethodExample("client.unpause_av( );")]
		[LuaMethod("unpause_av", "If currently capturing Audio/Video this resumes capturing")]
		public void UnpauseAv()
		{
			MainForm.PauseAvi = false;
		}

		[LuaMethodExample("local inclixpo = client.xpos( );")]
		[LuaMethod("xpos", "Returns the x value of the screen position where the client currently sits")]
		public int Xpos()
		{
			return MainForm.DesktopLocation.X;
		}

		[LuaMethodExample("local incliypo = client.ypos( );")]
		[LuaMethod("ypos", "Returns the y value of the screen position where the client currently sits")]
		public int Ypos()
		{
			return MainForm.DesktopLocation.Y;
		}

		[LuaMethodExample("local incbhver = client.getversion( );")]
		[LuaMethod("getversion", "Returns the current stable BizHawk version")]
		public static string GetVersion()
		{
			return VersionInfo.MainVersion;
		}

		[LuaMethodExample("local nlcliget = client.getavailabletools( );")]
		[LuaMethod("getavailabletools", "Returns a list of the tools currently open")]
		public LuaTable GetAvailableTools() => GlobalWin.Tools.AvailableTools.Select(tool => tool.Name.ToLower()).EnumerateToLuaTable(Lua);

		[LuaMethodExample("local nlcliget = client.gettool( \"Tool name\" );")]
		[LuaMethod("gettool", "Returns an object that represents a tool of the given name (not case sensitive). If the tool is not open, it will be loaded if available. Use gettools to get a list of names")]
		public LuaTable GetTool(string name)
		{
			var selectedTool = APIs.Tool.GetTool(name);
			return selectedTool == null ? null : Lua.TableFromObject(selectedTool);
		}

		[LuaMethodExample("local nlclicre = client.createinstance( \"objectname\" );")]
		[LuaMethod("createinstance", "returns a default instance of the given type of object if it exists (not case sensitive). Note: This will only work on objects which have a parameterless constructor.  If no suitable type is found, or the type does not have a parameterless constructor, then nil is returned")]
		public LuaTable CreateInstance(string name)
		{
			var instance = APIs.Tool.GetTool(name);
			return instance == null ? null : Lua.TableFromObject(instance);
		}

		[LuaMethodExample("client.displaymessages( true );")]
		[LuaMethod("displaymessages", "sets whether or not on screen messages will display")]
		public void DisplayMessages(bool value)
		{
			Global.Config.DisplayMessages = value;
		}

		[LuaMethodExample("client.saveram( );")]
		[LuaMethod("saveram", "flushes save ram to disk")]
		public void SaveRam()
		{
			MainForm.FlushSaveRAM();
		}

		[LuaMethodExample("client.sleep( 50 );")]
		[LuaMethod("sleep", "sleeps for n milliseconds")]
		public void Sleep(int millis)
		{
			Thread.Sleep(millis);
		}

		[LuaMethodExample("client.exactsleep( 50 );")]
		[LuaMethod("exactsleep", "sleeps exactly for n milliseconds")]
		public void ExactSleep(int millis)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			while (millis - stopwatch.ElapsedMilliseconds > 100)
			{
				Thread.Sleep(50);
			}
			while (true)
			{
				if (stopwatch.ElapsedMilliseconds >= millis)
				{
					break;
				}
			}
		}
	}
}
