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
	public class EmuClientApi : IEmuClient
	{
		private List<Joypad> _allJoyPads;

		private IEmulator Emulator { get; set; }

		private readonly IReadOnlyCollection<JoypadButton> JoypadButtonsArray = Enum.GetValues(typeof(JoypadButton)).Cast<JoypadButton>().ToList(); //TODO can the return of GetValues be cast to JoypadButton[]? --yoshi

		private readonly JoypadStringToEnumConverter JoypadConverter = new JoypadStringToEnumConverter();

		public SystemInfo RunningSystem
		{
			get
			{
				switch (GlobalWin.Emulator.SystemId)
				{
					case "PCE" when GlobalWin.Emulator is PCEngine pceHawk:
						return pceHawk.Type switch
						{
							NecSystemType.TurboGrafx => SystemInfo.PCE,
							NecSystemType.TurboCD => SystemInfo.PCECD,
							NecSystemType.SuperGrafx => SystemInfo.SGX,
							_ => throw new ArgumentOutOfRangeException()
						};
					case "PCE":
						return SystemInfo.PCE; // not always accurate, but anyone wanting accuracy has probably figured out how to use IEmu.GetSystemId()
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

		internal readonly BizHawkSystemIdToEnumConverter SystemIdConverter = new BizHawkSystemIdToEnumConverter();

		private IVideoProvider VideoProvider { get; set; }

		public event BeforeQuickLoadEventHandler BeforeQuickLoad;

		public event BeforeQuickSaveEventHandler BeforeQuickSave;

		public event EventHandler RomLoaded;

		public event StateLoadedEventHandler StateLoaded;

		public event StateSavedEventHandler StateSaved;

		public int BorderHeight() => GlobalWin.DisplayManager.TransformPoint(new Point(0, 0)).Y;

		public int BorderWidth() => GlobalWin.DisplayManager.TransformPoint(new Point(0, 0)).X;

		public int BufferHeight() => VideoProvider.BufferHeight;

		public int BufferWidth() => VideoProvider.BufferWidth;

		public void ClearAutohold() => GlobalWin.MainForm.ClearHolds();

		public void CloseEmulator() => GlobalWin.MainForm.CloseEmulator();

		public void CloseEmulatorWithCode(int exitCode) => GlobalWin.MainForm.CloseEmulator(exitCode);

		public void CloseRom() => GlobalWin.MainForm.CloseRom();

		public void DisplayMessages(bool value) => GlobalWin.Config.DisplayMessages = value;

		public void DoFrameAdvance()
		{
			GlobalWin.MainForm.FrameAdvance();
			GlobalWin.MainForm.StepRunLoop_Throttle();
			GlobalWin.MainForm.Render();
		}

		public void DoFrameAdvanceAndUnpause()
		{
			DoFrameAdvance();
			UnpauseEmulation();
		}

		public void EnableRewind(bool enabled) => GlobalWin.MainForm.EnableRewind(enabled);

		public void FrameSkip(int numFrames)
		{
			if (numFrames > 0)
			{
				GlobalWin.Config.FrameSkip = numFrames;
				GlobalWin.MainForm.FrameSkipMessage();
			}
			else
			{
				Console.WriteLine("Invalid frame skip value");
			}
		}

		private void GetAllInputs()
		{
			var joypadAdapter = GlobalWin.InputManager.AutofireStickyXorAdapter;

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

		public bool GetSoundOn() => GlobalWin.Config.SoundEnabled;

		public int GetTargetScanlineIntensity() => GlobalWin.Config.TargetScanlineFilterIntensity;

		public int GetWindowSize() => GlobalWin.Config.TargetZoomFactors[Emulator.SystemId];

		public void InvisibleEmulation(bool invisible) => GlobalWin.MainForm.InvisibleEmulation = invisible;

		public bool IsPaused() => GlobalWin.MainForm.EmulatorPaused;

		public bool IsSeeking() => GlobalWin.MainForm.IsSeeking;

		public bool IsTurbo() => GlobalWin.MainForm.IsTurboing;

		public void LoadState(string name) => GlobalWin.MainForm.LoadState(Path.Combine(GlobalWin.Config.PathEntries.SaveStateAbsolutePath(GlobalWin.Game.System), $"{name}.State"), name, suppressOSD: false);

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
			Emulator = emu;
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

		public void OpenRom(string path) => GlobalWin.MainForm.LoadRom(path, new MainForm.LoadRomArgs { OpenAdvanced = OpenAdvancedSerializer.ParseWithLegacy(path) });

		public void Pause() => GlobalWin.MainForm.PauseEmulator();

		public void PauseAv() => GlobalWin.MainForm.PauseAvi = true;

		public void RebootCore() => GlobalWin.MainForm.RebootCore();

		public void SaveRam() => GlobalWin.MainForm.FlushSaveRAM();

		public void SaveState(string name) => GlobalWin.MainForm.SaveState(Path.Combine(GlobalWin.Config.PathEntries.SaveStateAbsolutePath(GlobalWin.Game.System), $"{name}.State"), name, fromLua: false);

		public int ScreenHeight() => GlobalWin.MainForm.PresentationPanel.NativeSize.Height;

		public void Screenshot(string path)
		{
			if (path == null) GlobalWin.MainForm.TakeScreenshot();
			else GlobalWin.MainForm.TakeScreenshot(path);
		}

		public void ScreenshotToClipboard() => GlobalWin.MainForm.TakeScreenshotToClipboard();

		public int ScreenWidth() => GlobalWin.MainForm.PresentationPanel.NativeSize.Width;

		public void SeekFrame(int frame)
		{
			var wasPaused = GlobalWin.MainForm.EmulatorPaused;
			while (Emulator.Frame != frame) GlobalWin.MainForm.SeekFrameAdvance();
			if (!wasPaused) GlobalWin.MainForm.UnpauseEmulator();
		}

		public void SetExtraPadding(int left, int top, int right, int bottom)
		{
			GlobalWin.DisplayManager.ClientExtraPadding = new Padding(left, top, right, bottom);
			GlobalWin.MainForm.FrameBufferResized();
		}

		public void SetGameExtraPadding(int left, int top, int right, int bottom)
		{
			GlobalWin.DisplayManager.GameExtraPadding = new Padding(left, top, right, bottom);
			GlobalWin.MainForm.FrameBufferResized();
		}

		public void SetInput(int player, Joypad joypad)
		{
			if (!1.RangeTo(RunningSystem.MaxControllers).Contains(player)) throw new IndexOutOfRangeException($"{RunningSystem.DisplayName} does not support {player} controller(s)");

			if (joypad.Inputs == 0)
			{
				GlobalWin.InputManager.AutofireStickyXorAdapter.ClearStickies();
			}
			else
			{
				foreach (var button in JoypadButtonsArray.Where(button => joypad.Inputs.HasFlag(button)))
				{
					GlobalWin.InputManager.AutofireStickyXorAdapter.SetSticky(
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
					GlobalWin.InputManager.AutofireStickyXorAdapter.SetAxis($"P{i} X Axis", _allJoyPads[i - 1].AnalogX);
					GlobalWin.InputManager.AutofireStickyXorAdapter.SetAxis($"P{i} Y Axis", _allJoyPads[i - 1].AnalogY);
				}
			}
#endif
		}

		public void SetScreenshotOSD(bool value) => GlobalWin.Config.ScreenshotCaptureOsd = value;

		public void SetSoundOn(bool enable)
		{
			if (enable != GlobalWin.Config.SoundEnabled) GlobalWin.MainForm.ToggleSound();
		}

		public void SetTargetScanlineIntensity(int val) => GlobalWin.Config.TargetScanlineFilterIntensity = val;

		public void SetWindowSize(int size)
		{
			if (size == 1 || size == 2 || size == 3 || size == 4 || size == 5 || size == 10)
			{
				GlobalWin.Config.TargetZoomFactors[Emulator.SystemId] = size;
				GlobalWin.MainForm.FrameBufferResized();
				GlobalWin.OSD.AddMessage($"Window size set to {size}x");
			}
			else
			{
				Console.WriteLine("Invalid window size");
			}
		}

		public void SpeedMode(int percent)
		{
			if (percent.StrictlyBoundedBy(0.RangeTo(6400))) GlobalWin.MainForm.ClickSpeedItem(percent);
			else Console.WriteLine("Invalid speed value");
		}

		public void TogglePause() => GlobalWin.MainForm.TogglePause();

		public Point TransformPoint(Point point) => GlobalWin.DisplayManager.TransformPoint(point);

		public void Unpause() => GlobalWin.MainForm.UnpauseEmulator();

		public void UnpauseAv() => GlobalWin.MainForm.PauseAvi = false;

		public void UnpauseEmulation() => GlobalWin.MainForm.UnpauseEmulator();

		public void UpdateEmulatorAndVP(IEmulator emu)
		{
			Emulator = emu;
			VideoProvider = emu.AsVideoProviderOrDefault();
		}

		public int Xpos() => GlobalWin.MainForm.DesktopLocation.X;

		public int Ypos() => GlobalWin.MainForm.DesktopLocation.Y;
	}
}
