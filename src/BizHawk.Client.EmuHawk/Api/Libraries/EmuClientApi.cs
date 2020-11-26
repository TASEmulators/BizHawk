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

namespace BizHawk.Client.EmuHawk
{
	public sealed class EmuClientApi : IEmuClientApi
	{
		private List<Joypad> _allJoyPads;

		private readonly Config _config;

		private readonly DisplayManager _displayManager;

		private readonly InputManager _inputManager;

		private readonly IMainFormForApi _mainForm;

		private readonly Action<string> _logCallback;

		private IEmulator _maybeEmulator;

		public IEmulator Emulator;

		public IGameInfo Game;

		private readonly IReadOnlyCollection<JoypadButton> JoypadButtonsArray = Enum.GetValues(typeof(JoypadButton)).Cast<JoypadButton>().ToList(); //TODO can the return of GetValues be cast to JoypadButton[]? --yoshi

		private readonly JoypadStringToEnumConverter JoypadConverter = new JoypadStringToEnumConverter();

		/// <remarks>future humans: if this is broken, rewrite the caller instead if fixing it</remarks>
		private SystemInfo RunningSystem
		{
			get
			{
				switch (Emulator.SystemId)
				{
					case "PCE" when Emulator is PCEngine pceHawk:
						return pceHawk.Type switch
						{
							NecSystemType.TurboGrafx => SystemInfo.PCE,
							NecSystemType.TurboCD => SystemInfo.PCECD,
							NecSystemType.SuperGrafx => SystemInfo.SGX,
							_ => throw new ArgumentOutOfRangeException()
						};
					case "PCE":
						return SystemInfo.PCE;
					case "SMS":
						var sms = (SMS) Emulator;
						return sms.IsSG1000
							? SystemInfo.SG
							: sms.IsGameGear
								? SystemInfo.GG
								: SystemInfo.SMS;
					case "GB":
						if (Emulator is Gameboy gb) return gb.IsCGBMode() ? SystemInfo.GBC : SystemInfo.GB;
						return SystemInfo.DualGB;
					default:
						return SystemInfo.FindByCoreSystem(SystemIdConverter.Convert(Emulator.SystemId));
				}
			}
		}

		internal static readonly BizHawkSystemIdToEnumConverter SystemIdConverter = new BizHawkSystemIdToEnumConverter();

		private IVideoProvider VideoProvider { get; set; }

		public event BeforeQuickLoadEventHandler BeforeQuickLoad;

		public event BeforeQuickSaveEventHandler BeforeQuickSave;

		public event EventHandler RomLoaded;

		public event StateLoadedEventHandler StateLoaded;

		public event StateSavedEventHandler StateSaved;

		public EmuClientApi(Action<string> logCallback, IMainFormForApi mainForm, DisplayManager displayManager, InputManager inputManager, Config config, IEmulator emulator, IGameInfo game)
		{
			_config = config;
			_displayManager = displayManager;
			Emulator = emulator;
			Game = game;
			_inputManager = inputManager;
			_logCallback = logCallback;
			_mainForm = mainForm;
		}

		public int BorderHeight() => _displayManager.TransformPoint(new Point(0, 0)).Y;

		public int BorderWidth() => _displayManager.TransformPoint(new Point(0, 0)).X;

		public int BufferHeight() => VideoProvider.BufferHeight;

		public int BufferWidth() => VideoProvider.BufferWidth;

		public void ClearAutohold() => _mainForm.ClearHolds();

		public void CloseEmulator(int? exitCode = null) => _mainForm.CloseEmulator(exitCode);

		public void CloseRom() => _mainForm.CloseRom();

		public void DisplayMessages(bool value) => _config.DisplayMessages = value;

		public void DoFrameAdvance()
		{
			_mainForm.FrameAdvance();
			_mainForm.StepRunLoop_Throttle();
			_mainForm.Render();
		}

		public void DoFrameAdvanceAndUnpause()
		{
			DoFrameAdvance();
			Unpause();
		}

		public void EnableRewind(bool enabled) => _mainForm.EnableRewind(enabled);

		public void FrameSkip(int numFrames)
		{
			if (numFrames > 0)
			{
				_config.FrameSkip = numFrames;
				_mainForm.FrameSkipMessage();
			}
			else
			{
				_logCallback("Invalid frame skip value");
			}
		}

		private void GetAllInputs()
		{
			var joypadAdapter = _inputManager.AutofireStickyXorAdapter;

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

		public Joypad GetInput(int player)
		{
			if (!1.RangeTo(RunningSystem.MaxControllers).Contains(player))
				throw new IndexOutOfRangeException($"{RunningSystem.DisplayName} does not support {player} controller(s)");
			GetAllInputs();
			return _allJoyPads[player - 1];
		}

		public bool GetSoundOn() => _config.SoundEnabled;

		public int GetTargetScanlineIntensity() => _config.TargetScanlineFilterIntensity;

		public int GetWindowSize() => _config.TargetZoomFactors[_maybeEmulator.SystemId];

		public void InvisibleEmulation(bool invisible) => _mainForm.InvisibleEmulation = invisible;

		public bool IsPaused() => _mainForm.EmulatorPaused;

		public bool IsSeeking() => _mainForm.IsSeeking;

		public bool IsTurbo() => _mainForm.IsTurboing;

		public void LoadState(string name) => _mainForm.LoadState(Path.Combine(_config.PathEntries.SaveStateAbsolutePath(Game.System), $"{name}.State"), name, suppressOSD: false);

		public void OnBeforeQuickLoad(object sender, string quickSaveSlotName, out bool eventHandled)
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

		public void OnBeforeQuickSave(object sender, string quickSaveSlotName, out bool eventHandled)
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

		public void OnRomLoaded(IEmulator emu)
		{
			_maybeEmulator = emu;
			VideoProvider = emu.AsVideoProviderOrDefault();
			RomLoaded?.Invoke(null, EventArgs.Empty);

			try
			{
				_allJoyPads = new List<Joypad>(RunningSystem.MaxControllers);
				for (var i = 1; i <= RunningSystem.MaxControllers; i++)
					_allJoyPads.Add(new Joypad(RunningSystem, i));
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Apihawk is garbage and may not work this session.");
				Console.Error.WriteLine(e);
			}
		}

		public void OnStateLoaded(object sender, string stateName) => StateLoaded?.Invoke(sender, new StateLoadedEventArgs(stateName));

		public void OnStateSaved(object sender, string stateName) => StateSaved?.Invoke(sender, new StateSavedEventArgs(stateName));

		public void OpenRom(string path) => _mainForm.LoadRom(path, new LoadRomArgs { OpenAdvanced = OpenAdvancedSerializer.ParseWithLegacy(path) });

		public void Pause() => _mainForm.PauseEmulator();

		public void PauseAv() => _mainForm.PauseAvi = true;

		public void RebootCore() => _mainForm.RebootCore();

		public void SaveRam() => _mainForm.FlushSaveRAM();

		public void SaveState(string name) => _mainForm.SaveState(Path.Combine(_config.PathEntries.SaveStateAbsolutePath(Game.System), $"{name}.State"), name, fromLua: false);

		public int ScreenHeight() => _displayManager.GetPanelNativeSize().Height;

		public void Screenshot(string path)
		{
			if (path == null) _mainForm.TakeScreenshot();
			else _mainForm.TakeScreenshot(path);
		}

		public void ScreenshotToClipboard() => _mainForm.TakeScreenshotToClipboard();

		public int ScreenWidth() => _displayManager.GetPanelNativeSize().Width;

		public void SeekFrame(int frame)
		{
			var wasPaused = _mainForm.EmulatorPaused;
			while (_maybeEmulator.Frame != frame) _mainForm.SeekFrameAdvance();
			if (!wasPaused) _mainForm.UnpauseEmulator();
		}

		public void SetClientExtraPadding(int left, int top, int right, int bottom)
		{
			_displayManager.ClientExtraPadding = new Padding(left, top, right, bottom);
			_mainForm.FrameBufferResized();
		}

		public void SetGameExtraPadding(int left, int top, int right, int bottom)
		{
			_displayManager.GameExtraPadding = new Padding(left, top, right, bottom);
			_mainForm.FrameBufferResized();
		}

		public void SetInput(int player, Joypad joypad)
		{
			if (!1.RangeTo(RunningSystem.MaxControllers).Contains(player)) throw new IndexOutOfRangeException($"{RunningSystem.DisplayName} does not support {player} controller(s)");

			if (joypad.Inputs == 0)
			{
				_inputManager.AutofireStickyXorAdapter.ClearStickies();
			}
			else
			{
				foreach (var button in JoypadButtonsArray.Where(button => joypad.Inputs.HasFlag(button)))
				{
					_inputManager.AutofireStickyXorAdapter.SetSticky(
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
					_inputManager.AutofireStickyXorAdapter.SetAxis($"P{i} X Axis", _allJoyPads[i - 1].AnalogX);
					_inputManager.AutofireStickyXorAdapter.SetAxis($"P{i} Y Axis", _allJoyPads[i - 1].AnalogY);
				}
			}
#endif
		}

		public void SetScreenshotOSD(bool value) => _config.ScreenshotCaptureOsd = value;

		public void SetSoundOn(bool enable)
		{
			if (enable != _config.SoundEnabled) _mainForm.ToggleSound();
		}

		public void SetTargetScanlineIntensity(int val) => _config.TargetScanlineFilterIntensity = val;

		public void SetWindowSize(int size)
		{
			if (size == 1 || size == 2 || size == 3 || size == 4 || size == 5 || size == 10)
			{
				_config.TargetZoomFactors[_maybeEmulator.SystemId] = size;
				_mainForm.FrameBufferResized();
				_mainForm.AddOnScreenMessage($"Window size set to {size}x");
			}
			else
			{
				_logCallback("Invalid window size");
			}
		}

		public void SpeedMode(int percent)
		{
			if (percent.StrictlyBoundedBy(0.RangeTo(6400))) _mainForm.ClickSpeedItem(percent);
			else _logCallback("Invalid speed value");
		}

		public void TogglePause() => _mainForm.TogglePause();

		public Point TransformPoint(Point point) => _displayManager.TransformPoint(point);

		public void Unpause() => _mainForm.UnpauseEmulator();

		public void UnpauseAv() => _mainForm.PauseAvi = false;

		public void UpdateEmulatorAndVP(IEmulator emu)
		{
			_maybeEmulator = emu;
			VideoProvider = emu.AsVideoProviderOrDefault();
		}

		public int Xpos() => _mainForm.DesktopLocation.X;

		public int Ypos() => _mainForm.DesktopLocation.Y;
	}
}
