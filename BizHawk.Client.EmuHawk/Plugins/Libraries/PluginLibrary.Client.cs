using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class EmuHawkPluginLibrary : PluginLibraryBase
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

		public EmuHawkPluginLibrary() : base()
		{ }

		public void CloseEmulator()
		{
			GlobalWin.MainForm.CloseEmulator();
		}

		public void CloseEmulatorWithCode(int exitCode)
		{
			GlobalWin.MainForm.CloseEmulator(exitCode);
		}

		public static int BorderHeight()
		{
			var point = new System.Drawing.Point(0, 0);
			return GlobalWin.DisplayManager.TransformPoint(point).Y;
		}

		public static int BorderWidth()
		{
			var point = new System.Drawing.Point(0, 0);
			return GlobalWin.DisplayManager.TransformPoint(point).X;
		}

		public int BufferHeight()
		{
			return VideoProvider.BufferHeight;
		}

		public int BufferWidth()
		{
			return VideoProvider.BufferWidth;
		}

		public void ClearAutohold()
		{
			GlobalWin.MainForm.ClearHolds();
		}

		public static void CloseRom()
		{
			GlobalWin.MainForm.CloseRom();
		}

		public void EnableRewind(bool enabled)
		{
			GlobalWin.MainForm.EnableRewind(enabled);
		}

		public void FrameSkip(int numFrames)
		{
			if (numFrames > 0)
			{
				Global.Config.FrameSkip = numFrames;
				GlobalWin.MainForm.FrameSkipMessage();
			}
			else
			{
				Console.WriteLine("Invalid frame skip value");
			}
		}

		public static int GetTargetScanlineIntensity()
		{
			return Global.Config.TargetScanlineFilterIntensity;
		}

		public int GetWindowSize()
		{
			return Global.Config.TargetZoomFactors[Emulator.SystemId];
		}

		public static void SetGameExtraPadding(int left, int top, int right, int bottom)
		{
			GlobalWin.DisplayManager.GameExtraPadding = new System.Windows.Forms.Padding(left, top, right, bottom);
			GlobalWin.MainForm.FrameBufferResized();
		}

		public static void SetSoundOn(bool enable)
		{
			Global.Config.SoundEnabled = enable;
		}

		public static bool GetSoundOn()
		{
			return Global.Config.SoundEnabled;
		}

		public static void SetClientExtraPadding(int left, int top, int right, int bottom)
		{
			GlobalWin.DisplayManager.ClientExtraPadding = new System.Windows.Forms.Padding(left, top, right, bottom);
			GlobalWin.MainForm.FrameBufferResized();
		}

		public static bool IsPaused()
		{
			return GlobalWin.MainForm.EmulatorPaused;
		}

		public static bool IsTurbo()
		{
			return GlobalWin.MainForm.IsTurboing;
		}

		public static bool IsSeeking()
		{
			return GlobalWin.MainForm.IsSeeking;
		}

		public static void OpenCheats()
		{
			GlobalWin.Tools.Load<Cheats>();
		}

		public static void OpenHexEditor()
		{
			GlobalWin.Tools.Load<HexEditor>();
		}

		public static void OpenRamWatch()
		{
			GlobalWin.Tools.LoadRamWatch(loadDialog: true);
		}

		public static void OpenRamSearch()
		{
			GlobalWin.Tools.Load<RamSearch>();
		}

		public static void OpenRom(string path)
		{
			var ioa = OpenAdvancedSerializer.ParseWithLegacy(path);
			GlobalWin.MainForm.LoadRom(path, new MainForm.LoadRomArgs { OpenAdvanced = ioa });
		}

		public static void OpenTasStudio()
		{
			GlobalWin.Tools.Load<TAStudio>();
		}

		public static void OpenToolBox()
		{
			GlobalWin.Tools.Load<ToolBox>();
		}

		public static void OpenTraceLogger()
		{
			GlobalWin.Tools.Load<TraceLogger>();
		}

		public static void Pause()
		{
			GlobalWin.MainForm.PauseEmulator();
		}

		public static void PauseAv()
		{
			GlobalWin.MainForm.PauseAvi = true;
		}

		public static void RebootCore()
		{
			((LuaConsole)GlobalWin.Tools.Get<LuaConsole>()).LuaImp.IsRebootingCore = true;
			GlobalWin.MainForm.RebootCore();
			((LuaConsole)GlobalWin.Tools.Get<LuaConsole>()).LuaImp.IsRebootingCore = false;
		}

		public static int ScreenHeight()
		{
			return GlobalWin.MainForm.PresentationPanel.NativeSize.Height;
		}

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

		public static void ScreenshotToClipboard()
		{
			GlobalWin.MainForm.TakeScreenshotToClipboard();
		}

		public static void SetTargetScanlineIntensity(int val)
		{
			Global.Config.TargetScanlineFilterIntensity = val;
		}

		public static void SetScreenshotOSD(bool value)
		{
			Global.Config.Screenshot_CaptureOSD = value;
		}

		public static int ScreenWidth()
		{
			return GlobalWin.MainForm.PresentationPanel.NativeSize.Width;
		}

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
				Console.WriteLine("Invalid window size");
			}
		}

		public void SpeedMode(int percent)
		{
			if (percent > 0 && percent < 6400)
			{
				GlobalWin.MainForm.ClickSpeedItem(percent);
			}
			else
			{
				Console.WriteLine("Invalid speed value");
			}
		}

		public static void TogglePause()
		{
			GlobalWin.MainForm.TogglePause();
		}

		public static int TransformPointX(int x)
		{
			var point = new System.Drawing.Point(x, 0);
			return GlobalWin.DisplayManager.TransformPoint(point).X;
		}

		public static int TransformPointY(int y)
		{
			var point = new System.Drawing.Point(0, y);
			return GlobalWin.DisplayManager.TransformPoint(point).Y;
		}

		public static void Unpause()
		{
			GlobalWin.MainForm.UnpauseEmulator();
		}

		public static void UnpauseAv()
		{
			GlobalWin.MainForm.PauseAvi = false;
		}

		public static int Xpos()
		{
			return GlobalWin.MainForm.DesktopLocation.X;
		}

		public static int Ypos()
		{
			return GlobalWin.MainForm.DesktopLocation.Y;
		}

		public List<string> GetAvailableTools()
		{
			var tools = GlobalWin.Tools.AvailableTools.ToList();
			var t = new List<string>(tools.Count);
			for (int i = 0; i < tools.Count; i++)
			{
				t[i] = tools[i].Name.ToLower();
			}

			return t;
		}

		public Type GetTool(string name)
		{
			var toolType = ReflectionUtil.GetTypeByName(name)
				.FirstOrDefault(x => typeof(IToolForm).IsAssignableFrom(x) && !x.IsInterface);

			if (toolType != null)
			{
				GlobalWin.Tools.Load(toolType);
			}

			var selectedTool = GlobalWin.Tools.AvailableTools
				.FirstOrDefault(tool => tool.GetType().Name.ToLower() == name.ToLower());

			if (selectedTool != null)
			{
				return selectedTool;
			}

			return null;
		}

		public object CreateInstance(string name)
		{
			var possibleTypes = ReflectionUtil.GetTypeByName(name);

			if (possibleTypes.Any())
			{
				return Activator.CreateInstance(possibleTypes.First());
			}

			return null;
		}

		public void DisplayMessages(bool value)
		{
			Global.Config.DisplayMessages = value;
		}

		public void SaveRam()
		{
			GlobalWin.MainForm.FlushSaveRAM();
		}
	}
}
