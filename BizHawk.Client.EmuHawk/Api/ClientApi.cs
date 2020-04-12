using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// This class contains some methods that
	/// interact with BizHawk client
	/// </summary>
	public static class ClientApi
	{
		#region Fields

		private static IEmulator Emulator { get; set; }
		private static IVideoProvider VideoProvider { get; set; }

		private static readonly Assembly ClientAssembly;
		private static readonly object ClientMainFormInstance;
		private static readonly Type MainFormClass;
		private static readonly Array JoypadButtonsArray = Enum.GetValues(typeof(JoypadButton));

		internal static readonly BizHawkSystemIdToEnumConverter SystemIdConverter = new BizHawkSystemIdToEnumConverter();
		internal static readonly JoypadStringToEnumConverter JoypadConverter = new JoypadStringToEnumConverter();

		private static List<Joypad> _allJoyPads;

		/// <summary>
		/// Occurs before a quickload is done (just after user has pressed the shortcut button
		/// or has click on the item menu)
		/// </summary>
		public static event BeforeQuickLoadEventHandler BeforeQuickLoad;
		/// <summary>
		/// Occurs before a quicksave is done (just after user has pressed the shortcut button
		/// or has click on the item menu)
		/// </summary>
		public static event BeforeQuickSaveEventHandler BeforeQuickSave;
		/// <summary>
		/// Occurs when a ROM is successfully loaded
		/// </summary>
		public static event EventHandler RomLoaded;
		/// <summary>
		/// Occurs when a savestate is successfully loaded
		/// </summary>
		public static event StateLoadedEventHandler StateLoaded;
		/// <summary>
		/// Occurs when a savestate is successfully saved
		/// </summary>
		public static event StateSavedEventHandler StateSaved;

		#endregion

		#region cTor(s)

		/// <summary>
		/// Static stuff initialization
		/// </summary>
		static ClientApi()
		{
			ClientAssembly = Assembly.GetEntryAssembly();
			ClientMainFormInstance = ClientAssembly.GetType("BizHawk.Client.EmuHawk.GlobalWin").GetField("MainForm").GetValue(null);
			MainFormClass = ClientAssembly.GetType("BizHawk.Client.EmuHawk.MainForm");
		}

		public static void UpdateEmulatorAndVP(IEmulator emu = null)
		{
			Emulator = emu;
			VideoProvider = emu.AsVideoProviderOrDefault();
		}

		#endregion

		#region Methods

		#region Helpers

		private static void InvokeMainFormMethod(string name, dynamic[] paramList = null)
		{
			List<Type> typeList = new List<Type>();
			MethodInfo method;
			if (paramList != null)
			{
				foreach (var obj in paramList)
				{
					typeList.Add(obj.GetType());
				}
				method = MainFormClass.GetMethod(name, typeList.ToArray());
			}
			else method = MainFormClass.GetMethod(name);

			if(method != null)
				method.Invoke(ClientMainFormInstance, paramList);
		}

		private static object GetMainFormField(string name)
		{
			return MainFormClass.GetField(name);
		}

		private static void SetMainFormField(string name, object value)
		{
			MainFormClass.GetField(name).SetValue(ClientMainFormInstance, value);
		}

		#endregion

		#region Public
		/// <summary>
		/// THE FrameAdvance stuff
		/// </summary>
		public static void DoFrameAdvance()
		{
			InvokeMainFormMethod("FrameAdvance");

			InvokeMainFormMethod("StepRunLoop_Throttle");

			InvokeMainFormMethod("Render");
		}

		/// <summary>
		/// THE FrameAdvance stuff
		/// Auto unpause emulation
		/// </summary>
		public static void DoFrameAdvanceAndUnpause()
		{
			DoFrameAdvance();
			UnpauseEmulation();
		}

		/// <summary>
		/// Gets a <see cref="Joypad"/> for specified player
		/// </summary>
		/// <param name="player">Player (one based) you want current inputs</param>
		/// <returns>A <see cref="Joypad"/> populated with current inputs</returns>
		/// <exception cref="IndexOutOfRangeException">Raised when you specify a player less than 1 or greater than maximum allows (see SystemInfo class to get this information)</exception>
		public static Joypad GetInput(int player)
		{
			if (!1.RangeTo(RunningSystem.MaxControllers).Contains(player))
			{
				throw new IndexOutOfRangeException($"{RunningSystem.DisplayName} does not support {player} controller(s)");
			}
			
			GetAllInputs();
			return _allJoyPads[player - 1];
		}


		/// <summary>
		/// Load a savestate specified by its name
		/// </summary>
		/// <param name="name">Savestate friendly name</param>
		public static void LoadState(string name)
		{
			InvokeMainFormMethod("LoadState", new object[] { Path.Combine(Global.Config.PathEntries.SaveStateAbsolutePath(Global.Game.System), $"{name}.State"), name, false, false });
		}


		/// <summary>
		/// Raised before a quickload is done (just after pressing shortcut button)
		/// </summary>
		/// <param name="sender">Object who raised the event</param>
		/// <param name="quickSaveSlotName">Slot used for quickload</param>
		/// <param name="eventHandled">A boolean that can be set if users want to handle save themselves; if so, BizHawk won't do anything</param>
		public static void OnBeforeQuickLoad(object sender, string quickSaveSlotName, out bool eventHandled)
		{
			eventHandled = false;
			if (BeforeQuickLoad != null)
			{
				var e = new BeforeQuickLoadEventArgs(quickSaveSlotName);
				BeforeQuickLoad(sender, e);
				eventHandled = e.Handled;
			}
		}


		/// <summary>
		/// Raised before a quicksave is done (just after pressing shortcut button)
		/// </summary>
		/// <param name="sender">Object who raised the event</param>
		/// <param name="quickSaveSlotName">Slot used for quicksave</param>
		/// <param name="eventHandled">A boolean that can be set if users want to handle save themselves; if so, BizHawk won't do anything</param>
		public static void OnBeforeQuickSave(object sender, string quickSaveSlotName, out bool eventHandled)
		{
			eventHandled = false;
			if (BeforeQuickSave != null)
			{
				var e = new BeforeQuickSaveEventArgs(quickSaveSlotName);
				BeforeQuickSave(sender, e);
				eventHandled = e.Handled;
			}
		}


		/// <summary>
		/// Raise when a state is loaded
		/// </summary>
		/// <param name="sender">Object who raised the event</param>
		/// <param name="stateName">User friendly name for saved state</param>
		public static void OnStateLoaded(object sender, string stateName)
		{
			StateLoaded?.Invoke(sender, new StateLoadedEventArgs(stateName));
		}

		/// <summary>
		/// Raise when a state is saved
		/// </summary>
		/// <param name="sender">Object who raised the event</param>
		/// <param name="stateName">User friendly name for saved state</param>
		public static void OnStateSaved(object sender, string stateName)
		{
			StateSaved?.Invoke(sender, new StateSavedEventArgs(stateName));
		}

		/// <summary>
		/// Raise when a rom is successfully Loaded
		/// </summary>
		public static void OnRomLoaded(IEmulator emu)
		{
			Emulator = emu;
			VideoProvider = emu.AsVideoProviderOrDefault();
			RomLoaded?.Invoke(null, EventArgs.Empty);

			_allJoyPads = new List<Joypad>(RunningSystem.MaxControllers);
			for (int i = 1; i <= RunningSystem.MaxControllers; i++)
			{
				_allJoyPads.Add(new Joypad(RunningSystem, i));
			}
		}


		/// <summary>
		/// Save a state with specified name
		/// </summary>
		/// <param name="name">Savestate friendly name</param>
		public static void SaveState(string name)
		{
			InvokeMainFormMethod("SaveState", new object[] { Path.Combine(Global.Config.PathEntries.SaveStateAbsolutePath(Global.Game.System), $"{name}.State"), name, false });
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		/// <param name="right">Right padding</param>
		/// <param name="bottom">Bottom padding</param>
		public static void SetGameExtraPadding(int left, int top, int right, int bottom)
		{
			FieldInfo f = ClientAssembly.GetType("BizHawk.Client.EmuHawk.GlobalWin").GetField("DisplayManager");
			object displayManager = f.GetValue(null);
			f = f.FieldType.GetField("GameExtraPadding");
			f.SetValue(displayManager, new Padding(left, top, right, bottom));

			InvokeMainFormMethod("FrameBufferResized");
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		public static void SetGameExtraPadding(int left)
		{
			SetGameExtraPadding(left, 0, 0, 0);
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		public static void SetGameExtraPadding(int left, int top)
		{
			SetGameExtraPadding(left, top, 0, 0);
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		/// <param name="right">Right padding</param>
		public static void SetGameExtraPadding(int left, int top, int right)
		{
			SetGameExtraPadding(left, top, right, 0);
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		/// <param name="right">Right padding</param>
		/// <param name="bottom">Bottom padding</param>
		public static void SetExtraPadding(int left, int top, int right, int bottom)
		{
			FieldInfo f = ClientAssembly.GetType("BizHawk.Client.EmuHawk.GlobalWin").GetField("DisplayManager");
			object displayManager = f.GetValue(null);
			f = f.FieldType.GetField("ClientExtraPadding");
			f.SetValue(displayManager, new Padding(left, top, right, bottom));

			InvokeMainFormMethod("FrameBufferResized");
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		public static void SetExtraPadding(int left)
		{
			SetExtraPadding(left, 0, 0, 0);
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		public static void SetExtraPadding(int left, int top)
		{
			SetExtraPadding(left, top, 0, 0);
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		/// <param name="right">Right padding</param>
		public static void SetExtraPadding(int left, int top, int right)
		{
			SetExtraPadding(left, top, right, 0);
		}

		/// <summary>
		/// Set inputs in specified <see cref="Joypad"/> to specified player
		/// </summary>
		/// <param name="player">Player (one based) whom inputs must be set</param>
		/// <param name="joypad"><see cref="Joypad"/> with inputs</param>
		/// <exception cref="IndexOutOfRangeException">Raised when you specify a player less than 1 or greater than maximum allows (see SystemInfo class to get this information)</exception>
		/// <remarks>Still have some strange behaviour with multiple inputs; so this feature is still in beta</remarks>
		public static void SetInput(int player, Joypad joypad)
		{
			if (!1.RangeTo(RunningSystem.MaxControllers).Contains(player))
			{
				throw new IndexOutOfRangeException($"{RunningSystem.DisplayName} does not support {player} controller(s)");
			}
			else
			{
				if (joypad.Inputs == 0)
				{
					AutoFireStickyXorAdapter joypadAdapter = Global.InputManager.AutofireStickyXorAdapter;
					joypadAdapter.ClearStickies();
				}
				else
				{
					foreach (JoypadButton button in JoypadButtonsArray)
					{
						if (joypad.Inputs.HasFlag(button))
						{
							AutoFireStickyXorAdapter joypadAdapter = Global.InputManager.AutofireStickyXorAdapter;
							joypadAdapter.SetSticky(
								RunningSystem == SystemInfo.GB
									? $"{JoypadConverter.ConvertBack(button, RunningSystem)}"
									: $"P{player} {JoypadConverter.ConvertBack(button, RunningSystem)}", true);
						}
					}
				}

				//Using this break joypad usage (even in UI); have to figure out why
#if false
				if ((RunningSystem.AvailableButtons & JoypadButton.AnalogStick) == JoypadButton.AnalogStick)
				{
					var joypadAdaptor = Global.AutofireStickyXORAdapter;
					for (var i = 1; i <= RunningSystem.MaxControllers; i++)
					{
						joypadAdaptor.SetAxis($"P{i} X Axis", _allJoyPads[i - 1].AnalogX);
						joypadAdaptor.SetAxis($"P{i} Y Axis", _allJoyPads[i - 1].AnalogY);
					}
				}
#endif
			}
		}


		/// <summary>
		/// Resume the emulation
		/// </summary>
		public static void UnpauseEmulation()
		{
			InvokeMainFormMethod("UnpauseEmulator");
		}
		#endregion Public

		/// <summary>
		/// Gets all current inputs for each joypad and store
		/// them in <see cref="Joypad"/> class collection
		/// </summary>
		private static void GetAllInputs()
		{
			var joypadAdapter = Global.InputManager.AutofireStickyXorAdapter;

			var pressedButtons = joypadAdapter.Definition.BoolButtons
				.Where(b => joypadAdapter.IsPressed(b));

			foreach (Joypad j in _allJoyPads)
			{
				j.ClearInputs();
			}

			Parallel.ForEach(pressedButtons, button =>
			{
				if (RunningSystem == SystemInfo.GB)
				{
					_allJoyPads[0].AddInput(JoypadConverter.Convert(button));
				}
				else
				{
					if (int.TryParse(button.Substring(1, 2), out var player))
					{
						_allJoyPads[player - 1].AddInput(JoypadConverter.Convert(button.Substring(3)));
					}
				}
			});

			if ((RunningSystem.AvailableButtons & JoypadButton.AnalogStick) == JoypadButton.AnalogStick)
			{
				for (int i = 1; i <= RunningSystem.MaxControllers; i++)
				{
					_allJoyPads[i - 1].AnalogX = joypadAdapter.AxisValue($"P{i} X Axis");
					_allJoyPads[i - 1].AnalogY = joypadAdapter.AxisValue($"P{i} Y Axis");
				}
			}
		}

		public static void CloseEmulator()
		{
			InvokeMainFormMethod("CloseEmulator");
		}

		public static void CloseEmulatorWithCode(int exitCode)
		{
			InvokeMainFormMethod("CloseEmulator", new object[] {exitCode});
		}

		public static int BorderHeight()
		{
			var point = new System.Drawing.Point(0, 0);
			Type t = ClientAssembly.GetType("BizHawk.Client.EmuHawk.GlobalWin");
			FieldInfo f = t.GetField("DisplayManager");
			object displayManager = f.GetValue(null);
			MethodInfo m = t.GetMethod("TransFormPoint");
			point = (System.Drawing.Point) m.Invoke(displayManager, new object[] { point });
			return point.Y;
		}

		public static int BorderWidth()
		{
			var point = new System.Drawing.Point(0, 0);
			Type t = ClientAssembly.GetType("BizHawk.Client.EmuHawk.GlobalWin");
			FieldInfo f = t.GetField("DisplayManager");
			object displayManager = f.GetValue(null);
			MethodInfo m = t.GetMethod("TransFormPoint");
			point = (System.Drawing.Point)m.Invoke(displayManager, new object[] { point });
			return point.X;
		}

		public static int BufferHeight()
		{
			return VideoProvider.BufferHeight;
		}

		public static int BufferWidth()
		{
			return VideoProvider.BufferWidth;
		}

		public static void ClearAutohold()
		{
			InvokeMainFormMethod("ClearHolds");
		}

		public static void CloseRom()
		{
			InvokeMainFormMethod("CloseRom");
		}

		public static void DisplayMessages(bool value)
		{
			Global.Config.DisplayMessages = value;
		}

		public static void EnableRewind(bool enabled)
		{
			InvokeMainFormMethod("EnableRewind", new object[] {enabled});
		}

		public static void FrameSkip(int numFrames)
		{
			if (numFrames > 0)
			{
				Global.Config.FrameSkip = numFrames;
				InvokeMainFormMethod("FrameSkipMessage");
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

		public static int GetWindowSize()
		{
			return Global.Config.TargetZoomFactors[Emulator.SystemId];
		}

		public static void SetSoundOn(bool enable)
		{
			if (enable != Global.Config.SoundEnabled) InvokeMainFormMethod("ToggleSound");
		}

		public static bool GetSoundOn() => Global.Config.SoundEnabled;

		public static bool IsPaused()
		{
			return (bool) GetMainFormField("EmulatorPaused");
		}

		public static bool IsTurbo()
		{
			return (bool)GetMainFormField("IsTurboing");
		}

		public static bool IsSeeking()
		{
			return (bool)GetMainFormField("IsSeeking");
		}

		public static void OpenRom(string path)
		{
			var ioa = OpenAdvancedSerializer.ParseWithLegacy(path);
			Type t = ClientAssembly.GetType("BizHawk.Client.EmuHawk.GlobalWin.MainForm.LoadRomArgs");
			object o = Activator.CreateInstance(t);
			t.GetField("OpenAdvanced").SetValue(o, ioa);

			InvokeMainFormMethod("LoadRom", new[] {path, o});
		}

		public static void Pause()
		{
			InvokeMainFormMethod("PauseEmulator");
		}

		public static void PauseAv()
		{
			SetMainFormField("PauseAvi", true);
		}

		public static void RebootCore()
		{
			InvokeMainFormMethod("RebootCore");
		}

		public static void SaveRam()
		{
			InvokeMainFormMethod("FlushSaveRAM");
		}

		public static int ScreenHeight()
		{
			Type t = GetMainFormField("PresentationPanel").GetType();
			object o = GetMainFormField("PresentationPanel");
			o = t.GetField("NativeSize").GetValue(o);
			t = t.GetField("NativeSize").GetType();

			return (int) t.GetField("Height").GetValue(o);
		}

		public static void Screenshot(string path = null)
		{
			if (path == null)
			{
				InvokeMainFormMethod("TakeScreenshot");
			}
			else
			{
				InvokeMainFormMethod("TakeScreenshot", new object[] {path});
			}
		}

		public static void ScreenshotToClipboard()
		{
			InvokeMainFormMethod("TakeScreenshotToClipboard");
		}

		public static void SetTargetScanlineIntensity(int val)
		{
			Global.Config.TargetScanlineFilterIntensity = val;
		}

		public static void SetScreenshotOSD(bool value)
		{
			Global.Config.ScreenshotCaptureOsd = value;
		}

		public static int ScreenWidth()
		{
			Type t = GetMainFormField("PresentationPanel").GetType();
			object o = GetMainFormField("PresentationPanel");
			o = t.GetField("NativeSize").GetValue(o);
			t = t.GetField("NativeSize").GetType();

			return (int) t.GetField("Width").GetValue(o);
		}

		public static void SetWindowSize(int size)
		{
			if (size == 1 || size == 2 || size == 3 || size == 4 || size == 5 || size == 10)
			{
				Global.Config.TargetZoomFactors[Emulator.SystemId] = size;
				InvokeMainFormMethod("FrameBufferResized");
				Type t = ClientAssembly.GetType("BizHawk.Client.EmuHawk.GlobalWin");
				FieldInfo f = t.GetField("OSD");
				object osd = f.GetValue(null);
				t = f.GetType();
				MethodInfo m = t.GetMethod("AddMessage");
				m.Invoke(osd, new object[] { $"Window size set to {size}x" });
			}
			else
			{
				Console.WriteLine("Invalid window size");
			}
		}

		public static void SpeedMode(int percent)
		{
			if (percent.StrictlyBoundedBy(0.RangeTo(6400)))
			{
				InvokeMainFormMethod("ClickSpeedItem", new object[] {percent});
			}
			else
			{
				Console.WriteLine("Invalid speed value");
			}
		}

		public static void TogglePause()
		{
			InvokeMainFormMethod("TogglePause");
		}

		public static Point TransformPoint(Point point)
		{
			var globalWinType = ClientAssembly.GetType("BizHawk.Client.EmuHawk.GlobalWin");
			var dispManType = ClientAssembly.GetType("BizHawk.Client.EmuHawk.DisplayManager");
			var dispManInstance = globalWinType.GetField("DisplayManager").GetValue(null);
			var transformed = dispManType.GetMethod("TransformPoint")?.Invoke(dispManInstance, new object[] { point });
			if (transformed is Point p) return p;
			throw new Exception();
		}

		public static void Unpause()
		{
			InvokeMainFormMethod("UnpauseEmulator");
		}

		public static void UnpauseAv()
		{
			SetMainFormField("PauseAvi", false);
		}

		public static int Xpos()
		{
			object o = GetMainFormField("DesktopLocation");
			Type t = MainFormClass.GetField("DesktopLocation").GetType();
			return (int)t.GetField("X").GetValue(o);
		}

		public static int Ypos()
		{
			object o = GetMainFormField("DesktopLocation");
			Type t = MainFormClass.GetField("DesktopLocation").GetType();
			return (int)t.GetField("Y").GetValue(o);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets current emulated system
		/// </summary>
		public static SystemInfo RunningSystem
		{
			get
			{
				switch (Global.Emulator.SystemId)
				{
					case "PCE":
						if (((PCEngine)Global.Emulator).Type == NecSystemType.TurboGrafx)
						{
							return SystemInfo.PCE;
						}
						else if (((PCEngine)Global.Emulator).Type == NecSystemType.SuperGrafx)
						{
							return SystemInfo.SGX;
						}
						else
						{
							return SystemInfo.PCECD;
						}

					case "SMS":
						if (((SMS)Global.Emulator).IsSG1000)
						{
							return SystemInfo.SG;
						}
						else if (((SMS)Global.Emulator).IsGameGear)
						{
							return SystemInfo.GG;
						}
						else
						{
							return SystemInfo.SMS;
						}

					case "GB":
						if (Global.Emulator is Gameboy gb)
						{
							return gb.IsCGBMode()
								? SystemInfo.GBC
								: SystemInfo.GB;
						}
						else
						{
							return SystemInfo.DualGB;
						}

					default:
						return SystemInfo.FindByCoreSystem(SystemIdConverter.Convert(Global.Emulator.SystemId));
				}
			}
		}

		#endregion
	}
}
