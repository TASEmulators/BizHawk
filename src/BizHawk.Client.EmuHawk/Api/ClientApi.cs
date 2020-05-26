using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
		private static IEmulator Emulator { get; set; }

		private static IVideoProvider VideoProvider { get; set; }

		private static readonly IReadOnlyCollection<JoypadButton> JoypadButtonsArray = Enum.GetValues(typeof(JoypadButton)).Cast<JoypadButton>().ToList(); //TODO can the return of GetValues be cast to JoypadButton[]? --yoshi

		internal static readonly BizHawkSystemIdToEnumConverter SystemIdConverter = new BizHawkSystemIdToEnumConverter();

		private static readonly JoypadStringToEnumConverter JoypadConverter = new JoypadStringToEnumConverter();

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

		public static void UpdateEmulatorAndVP(IEmulator emu = null)
		{
			Emulator = emu;
			VideoProvider = emu.AsVideoProviderOrDefault();
		}

		/// <summary>
		/// THE FrameAdvance stuff
		/// </summary>
		public static void DoFrameAdvance()
		{
			GlobalWin.MainForm.FrameAdvance();
			GlobalWin.MainForm.StepRunLoop_Throttle();
			GlobalWin.MainForm.Render();
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
		/// Use with <see cref="InvisibleEmulation(bool)"/> for CamHack.
		/// Refer to <see cref="MainForm.InvisibleEmulation"/> for the workflow details.
		/// </summary>
		public static void SeekFrame(int frame)
		{
			var wasPaused = GlobalWin.MainForm.EmulatorPaused;
			while (Emulator.Frame != frame) GlobalWin.MainForm.SeekFrameAdvance();
			if (!wasPaused) GlobalWin.MainForm.UnpauseEmulator();
		}

		/// <summary>
		/// Use with <see cref="SeekFrame(int)"/> for CamHack.
		/// Refer to <see cref="MainForm.InvisibleEmulation"/> for the workflow details.
		/// </summary>
		public static void InvisibleEmulation(bool invisible) => GlobalWin.MainForm.InvisibleEmulation = invisible;

		/// <summary>
		/// Gets a <see cref="Joypad"/> for specified player
		/// </summary>
		/// <param name="player">Player (one based) you want current inputs</param>
		/// <returns>A <see cref="Joypad"/> populated with current inputs</returns>
		/// <exception cref="IndexOutOfRangeException">Raised when you specify a player less than 1 or greater than maximum allows (see SystemInfo class to get this information)</exception>
		public static Joypad GetInput(int player)
		{
			if (!1.RangeTo(RunningSystem.MaxControllers).Contains(player)) throw new IndexOutOfRangeException($"{RunningSystem.DisplayName} does not support {player} controller(s)");
			GetAllInputs();
			return _allJoyPads[player - 1];
		}

		/// <summary>
		/// Load a savestate specified by its name
		/// </summary>
		/// <param name="name">Savestate friendly name</param>
		public static void LoadState(string name) => GlobalWin.MainForm.LoadState(Path.Combine(Global.Config.PathEntries.SaveStateAbsolutePath(Global.Game.System), $"{name}.State"), name, suppressOSD: false);

		/// <summary>
		/// Raised before a quickload is done (just after pressing shortcut button)
		/// </summary>
		/// <param name="sender">Object who raised the event</param>
		/// <param name="quickSaveSlotName">Slot used for quickload</param>
		/// <param name="eventHandled">A boolean that can be set if users want to handle save themselves; if so, BizHawk won't do anything</param>
		public static void OnBeforeQuickLoad(object sender, string quickSaveSlotName, out bool eventHandled)
		{
			if (BeforeQuickLoad == null)
			{
				eventHandled = false;
				return;
			}
			var e = new BeforeQuickLoadEventArgs(quickSaveSlotName);
			BeforeQuickLoad(sender, e);
			eventHandled = e.Handled;
		}


		/// <summary>
		/// Raised before a quicksave is done (just after pressing shortcut button)
		/// </summary>
		/// <param name="sender">Object who raised the event</param>
		/// <param name="quickSaveSlotName">Slot used for quicksave</param>
		/// <param name="eventHandled">A boolean that can be set if users want to handle save themselves; if so, BizHawk won't do anything</param>
		public static void OnBeforeQuickSave(object sender, string quickSaveSlotName, out bool eventHandled)
		{
			if (BeforeQuickSave == null)
			{
				eventHandled = false;
				return;
			}
			var e = new BeforeQuickSaveEventArgs(quickSaveSlotName);
			BeforeQuickSave(sender, e);
			eventHandled = e.Handled;
		}


		/// <summary>
		/// Raise when a state is loaded
		/// </summary>
		/// <param name="sender">Object who raised the event</param>
		/// <param name="stateName">User friendly name for saved state</param>
		public static void OnStateLoaded(object sender, string stateName) => StateLoaded?.Invoke(sender, new StateLoadedEventArgs(stateName));

		/// <summary>
		/// Raise when a state is saved
		/// </summary>
		/// <param name="sender">Object who raised the event</param>
		/// <param name="stateName">User friendly name for saved state</param>
		public static void OnStateSaved(object sender, string stateName) => StateSaved?.Invoke(sender, new StateSavedEventArgs(stateName));

		/// <summary>
		/// Raise when a rom is successfully Loaded
		/// </summary>
		public static void OnRomLoaded(IEmulator emu)
		{
			Emulator = emu;
			VideoProvider = emu.AsVideoProviderOrDefault();
			RomLoaded?.Invoke(null, EventArgs.Empty);

			// TODO: Don't crash
			// _allJoyPads = new List<Joypad>(RunningSystem.MaxControllers);
			// for (var i = 1; i <= RunningSystem.MaxControllers; i++) _allJoyPads.Add(new Joypad(RunningSystem, i));
		}

		/// <summary>
		/// Save a state with specified name
		/// </summary>
		/// <param name="name">Savestate friendly name</param>
		public static void SaveState(string name) => GlobalWin.MainForm.SaveState(Path.Combine(Global.Config.PathEntries.SaveStateAbsolutePath(Global.Game.System), $"{name}.State"), name, fromLua: false);

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		/// <param name="right">Right padding</param>
		/// <param name="bottom">Bottom padding</param>
		public static void SetGameExtraPadding(int left, int top = 0, int right = 0, int bottom = 0)
		{
			GlobalWin.DisplayManager.GameExtraPadding = new Padding(left, top, right, bottom);
			GlobalWin.MainForm.FrameBufferResized();
		}

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		/// <param name="right">Right padding</param>
		/// <param name="bottom">Bottom padding</param>
		public static void SetExtraPadding(int left, int top = 0, int right = 0, int bottom = 0)
		{
			GlobalWin.DisplayManager.ClientExtraPadding = new Padding(left, top, right, bottom);
			GlobalWin.MainForm.FrameBufferResized();
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
			if (!1.RangeTo(RunningSystem.MaxControllers).Contains(player)) throw new IndexOutOfRangeException($"{RunningSystem.DisplayName} does not support {player} controller(s)");

			if (joypad.Inputs == 0)
			{
				Global.InputManager.AutofireStickyXorAdapter.ClearStickies();
			}
			else
			{
				foreach (var button in JoypadButtonsArray.Where(button => joypad.Inputs.HasFlag(button)))
				{
					Global.InputManager.AutofireStickyXorAdapter.SetSticky(
						RunningSystem == SystemInfo.GB
							? $"{JoypadConverter.ConvertBack(button, RunningSystem)}"
							: $"P{player} {JoypadConverter.ConvertBack(button, RunningSystem)}",
						isSticky: true
					);
				}
			}

#if false // Using this breaks joypad usage (even in UI); have to figure out why
			if ((RunningSystem.AvailableButtons & JoypadButton.AnalogStick) == JoypadButton.AnalogStick)
			{
				for (var i = 1; i <= RunningSystem.MaxControllers; i++)
				{
					Global.InputManager.AutofireStickyXorAdapter.SetAxis($"P{i} X Axis", _allJoyPads[i - 1].AnalogX);
					Global.InputManager.AutofireStickyXorAdapter.SetAxis($"P{i} Y Axis", _allJoyPads[i - 1].AnalogY);
				}
			}
#endif
		}

		/// <summary>
		/// Resume the emulation
		/// </summary>
		public static void UnpauseEmulation() => GlobalWin.MainForm.UnpauseEmulator();

		/// <summary>
		/// Gets all current inputs for each joypad and store
		/// them in <see cref="Joypad"/> class collection
		/// </summary>
		private static void GetAllInputs()
		{
			var joypadAdapter = Global.InputManager.AutofireStickyXorAdapter;

			var pressedButtons = joypadAdapter.Definition.BoolButtons.Where(b => joypadAdapter.IsPressed(b));

			foreach (var j in _allJoyPads) j.ClearInputs();

			Parallel.ForEach(pressedButtons, button =>
			{
				if (RunningSystem == SystemInfo.GB) _allJoyPads[0].AddInput(JoypadConverter.Convert(button));
				else if (int.TryParse(button.Substring(1, 2), out var player)) _allJoyPads[player - 1].AddInput(JoypadConverter.Convert(button.Substring(3)));
			});

			if ((RunningSystem.AvailableButtons & JoypadButton.AnalogStick) == JoypadButton.AnalogStick)
			{
				for (var i = 1; i <= RunningSystem.MaxControllers; i++)
				{
					_allJoyPads[i - 1].AnalogX = joypadAdapter.AxisValue($"P{i} X Axis");
					_allJoyPads[i - 1].AnalogY = joypadAdapter.AxisValue($"P{i} Y Axis");
				}
			}
		}

		public static void CloseEmulator() => GlobalWin.MainForm.CloseEmulator();

		public static void CloseEmulatorWithCode(int exitCode) => GlobalWin.MainForm.CloseEmulator(exitCode);

		public static int BorderHeight() => GlobalWin.DisplayManager.TransformPoint(new Point(0, 0)).Y;

		public static int BorderWidth() => GlobalWin.DisplayManager.TransformPoint(new Point(0, 0)).X;

		public static int BufferHeight() => VideoProvider.BufferHeight;

		public static int BufferWidth() => VideoProvider.BufferWidth;

		public static void ClearAutohold() => GlobalWin.MainForm.ClearHolds();

		public static void CloseRom() => GlobalWin.MainForm.CloseRom();

		public static void DisplayMessages(bool value) => Global.Config.DisplayMessages = value;

		public static void EnableRewind(bool enabled) => GlobalWin.MainForm.EnableRewind(enabled);

		public static void FrameSkip(int numFrames)
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

		public static int GetTargetScanlineIntensity() => Global.Config.TargetScanlineFilterIntensity;

		public static int GetWindowSize() => Global.Config.TargetZoomFactors[Emulator.SystemId];

		public static void SetSoundOn(bool enable)
		{
			if (enable != Global.Config.SoundEnabled) GlobalWin.MainForm.ToggleSound();
		}

		public static bool GetSoundOn() => Global.Config.SoundEnabled;

		public static bool IsPaused() => GlobalWin.MainForm.EmulatorPaused;

		public static bool IsTurbo() => GlobalWin.MainForm.IsTurboing;

		public static bool IsSeeking() => GlobalWin.MainForm.IsSeeking;

		public static void OpenRom(string path) => GlobalWin.MainForm.LoadRom(path, new MainForm.LoadRomArgs { OpenAdvanced = OpenAdvancedSerializer.ParseWithLegacy(path) });

		public static void Pause() => GlobalWin.MainForm.PauseEmulator();

		public static void PauseAv() => GlobalWin.MainForm.PauseAvi = true;

		public static void RebootCore() => GlobalWin.MainForm.RebootCore();

		public static void SaveRam() => GlobalWin.MainForm.FlushSaveRAM();

		public static int ScreenHeight() => GlobalWin.MainForm.PresentationPanel.NativeSize.Height;

		public static void Screenshot(string path = null)
		{
			if (path == null) GlobalWin.MainForm.TakeScreenshot();
			else GlobalWin.MainForm.TakeScreenshot(path);
		}

		public static void ScreenshotToClipboard() => GlobalWin.MainForm.TakeScreenshotToClipboard();

		public static void SetTargetScanlineIntensity(int val) => Global.Config.TargetScanlineFilterIntensity = val;

		public static void SetScreenshotOSD(bool value) => Global.Config.ScreenshotCaptureOsd = value;

		public static int ScreenWidth() => GlobalWin.MainForm.PresentationPanel.NativeSize.Width;

		public static void SetWindowSize(int size)
		{
			if (size == 1 || size == 2 || size == 3 || size == 4 || size == 5 || size == 10)
			{
				Global.Config.TargetZoomFactors[Emulator.SystemId] = size;
				GlobalWin.MainForm.FrameBufferResized();
				GlobalWin.OSD.AddMessage($"Window size set to {size}x");
			}
			else
			{
				Console.WriteLine("Invalid window size");
			}
		}

		public static void SpeedMode(int percent)
		{
			if (percent.StrictlyBoundedBy(0.RangeTo(6400))) GlobalWin.MainForm.ClickSpeedItem(percent);
			else Console.WriteLine("Invalid speed value");
		}

		public static void TogglePause() => GlobalWin.MainForm.TogglePause();

		public static Point TransformPoint(Point point) => GlobalWin.DisplayManager.TransformPoint(point);

		public static void Unpause() => GlobalWin.MainForm.UnpauseEmulator();

		public static void UnpauseAv() => GlobalWin.MainForm.PauseAvi = false;

		public static int Xpos() => GlobalWin.MainForm.DesktopLocation.X;

		public static int Ypos() => GlobalWin.MainForm.DesktopLocation.Y;

		/// <summary>
		/// Gets current emulated system
		/// </summary>
		public static SystemInfo RunningSystem
		{
			get
			{
				switch (GlobalWin.Emulator.SystemId)
				{
					case "PCE":
						return ((PCEngine) GlobalWin.Emulator).Type switch
						{
							NecSystemType.TurboGrafx => SystemInfo.PCE,
							NecSystemType.TurboCD => SystemInfo.PCECD,
							NecSystemType.SuperGrafx => SystemInfo.SGX,
							_ => throw new ArgumentOutOfRangeException()
						};
					case "SMS":
						var sms = (SMS) GlobalWin.Emulator;
						return sms.IsSG1000
							? SystemInfo.SG
							: sms.IsGameGear
								? SystemInfo.GG
								: SystemInfo.SMS;
					case "GB":
						if (GlobalWin.Emulator is Gameboy gb) return gb.IsCGBMode() ? SystemInfo.GBC : SystemInfo.GB;
						return SystemInfo.DualGB;
					default:
						return SystemInfo.FindByCoreSystem(SystemIdConverter.Convert(GlobalWin.Emulator.SystemId));
				}
			}
		}
	}
}
