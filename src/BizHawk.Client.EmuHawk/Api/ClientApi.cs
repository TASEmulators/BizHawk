using System;
using System.Drawing;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class ClientApi
	{
		public static IEmuClientApi EmuClient { get; set; }

		/// <inheritdoc cref="IEmuClientApi.BeforeQuickLoad"/>
		public static event BeforeQuickLoadEventHandler BeforeQuickLoad
		{
			add => EmuClient.BeforeQuickLoad += value;
			remove => EmuClient.BeforeQuickLoad -= value;
		}

		/// <inheritdoc cref="IEmuClientApi.BeforeQuickSave"/>
		public static event BeforeQuickSaveEventHandler BeforeQuickSave
		{
			add => EmuClient.BeforeQuickSave += value;
			remove => EmuClient.BeforeQuickSave -= value;
		}

		/// <inheritdoc cref="IEmuClientApi.RomLoaded"/>
		public static event EventHandler RomLoaded
		{
			add => EmuClient.RomLoaded += value;
			remove => EmuClient.RomLoaded -= value;
		}

		/// <inheritdoc cref="IEmuClientApi.StateLoaded"/>
		public static event StateLoadedEventHandler StateLoaded
		{
			add => EmuClient.StateLoaded += value;
			remove => EmuClient.StateLoaded -= value;
		}

		/// <inheritdoc cref="IEmuClientApi.StateSaved"/>
		public static event StateSavedEventHandler StateSaved
		{
			add => EmuClient.StateSaved += value;
			remove => EmuClient.StateSaved -= value;
		}

		/// <inheritdoc cref="IEmuClientApi.BorderHeight"/>
		public static int BorderHeight() => EmuClient.BorderHeight();

		/// <inheritdoc cref="IEmuClientApi.BorderWidth"/>
		public static int BorderWidth() => EmuClient.BorderWidth();

		/// <inheritdoc cref="IEmuClientApi.BufferHeight"/>
		public static int BufferHeight() => EmuClient.BufferHeight();

		/// <inheritdoc cref="IEmuClientApi.BufferWidth"/>
		public static int BufferWidth() => EmuClient.BufferWidth();

		/// <inheritdoc cref="IEmuClientApi.ClearAutohold"/>
		public static void ClearAutohold() => EmuClient.ClearAutohold();

		/// <inheritdoc cref="IEmuClientApi.CloseEmulator"/>
		public static void CloseEmulator() => EmuClient.CloseEmulator();

		/// <inheritdoc cref="IEmuClientApi.CloseEmulator"/>
		public static void CloseEmulatorWithCode(int exitCode) => EmuClient.CloseEmulator(exitCode);

		/// <inheritdoc cref="IEmuClientApi.CloseRom"/>
		public static void CloseRom() => EmuClient.CloseRom();

		/// <inheritdoc cref="IEmuClientApi.DisplayMessages"/>
		public static void DisplayMessages(bool value) => EmuClient.DisplayMessages(value);

		/// <inheritdoc cref="IEmuClientApi.DoFrameAdvance"/>
		public static void DoFrameAdvance() => EmuClient.DoFrameAdvance();

		/// <inheritdoc cref="IEmuClientApi.DoFrameAdvanceAndUnpause"/>
		public static void DoFrameAdvanceAndUnpause() => EmuClient.DoFrameAdvanceAndUnpause();

		/// <inheritdoc cref="IEmuClientApi.EnableRewind"/>
		public static void EnableRewind(bool enabled) => EmuClient.EnableRewind(enabled);

		/// <inheritdoc cref="IEmuClientApi.FrameSkip"/>
		public static void FrameSkip(int numFrames) => EmuClient.FrameSkip(numFrames);

		/// <inheritdoc cref="IEmuClientApi.GetInput"/>
		public static Joypad GetInput(int player) => EmuClient.GetInput(player);

		/// <inheritdoc cref="IEmuClientApi.GetSoundOn"/>
		public static bool GetSoundOn() => EmuClient.GetSoundOn();

		/// <inheritdoc cref="IEmuClientApi.GetTargetScanlineIntensity"/>
		public static int GetTargetScanlineIntensity() => EmuClient.GetTargetScanlineIntensity();

		/// <inheritdoc cref="IEmuClientApi.GetWindowSize"/>
		public static int GetWindowSize() => EmuClient.GetWindowSize();

		/// <inheritdoc cref="IEmuClientApi.InvisibleEmulation"/>
		public static void InvisibleEmulation(bool invisible) => EmuClient.InvisibleEmulation(invisible);

		/// <inheritdoc cref="IEmuClientApi.IsPaused"/>
		public static bool IsPaused() => EmuClient.IsPaused();

		/// <inheritdoc cref="IEmuClientApi.IsSeeking"/>
		public static bool IsSeeking() => EmuClient.IsSeeking();

		/// <inheritdoc cref="IEmuClientApi.IsTurbo"/>
		public static bool IsTurbo() => EmuClient.IsTurbo();

		/// <inheritdoc cref="IEmuClientApi.LoadState"/>
		public static void LoadState(string name) => EmuClient.LoadState(name);

		/// <inheritdoc cref="IEmuClientApi.OnBeforeQuickLoad"/>
		public static void OnBeforeQuickLoad(object sender, string quickSaveSlotName, out bool eventHandled) => EmuClient.OnBeforeQuickLoad(sender, quickSaveSlotName, out eventHandled);

		/// <inheritdoc cref="IEmuClientApi.OnBeforeQuickSave"/>
		public static void OnBeforeQuickSave(object sender, string quickSaveSlotName, out bool eventHandled) => EmuClient.OnBeforeQuickSave(sender, quickSaveSlotName, out eventHandled);

		/// <inheritdoc cref="IEmuClientApi.OnRomLoaded"/>
		public static void OnRomLoaded(IEmulator emu) => EmuClient.OnRomLoaded(emu);

		/// <inheritdoc cref="IEmuClientApi.OnStateLoaded"/>
		public static void OnStateLoaded(object sender, string stateName) => EmuClient.OnStateLoaded(sender, stateName);

		/// <inheritdoc cref="IEmuClientApi.OnStateSaved"/>
		public static void OnStateSaved(object sender, string stateName) => EmuClient.OnStateSaved(sender, stateName);

		/// <inheritdoc cref="IEmuClientApi.OpenRom"/>
		public static void OpenRom(string path) => EmuClient.OpenRom(path);

		/// <inheritdoc cref="IEmuClientApi.Pause"/>
		public static void Pause() => EmuClient.Pause();

		/// <inheritdoc cref="IEmuClientApi.PauseAv"/>
		public static void PauseAv() => EmuClient.PauseAv();

		/// <inheritdoc cref="IEmuClientApi.RebootCore"/>
		public static void RebootCore() => EmuClient.RebootCore();

		/// <inheritdoc cref="IEmuClientApi.SaveRam"/>
		public static void SaveRam() => EmuClient.SaveRam();

		/// <inheritdoc cref="IEmuClientApi.SaveState"/>
		public static void SaveState(string name) => EmuClient.SaveState(name);

		/// <inheritdoc cref="IEmuClientApi.ScreenHeight"/>
		public static int ScreenHeight() => EmuClient.ScreenHeight();

		/// <inheritdoc cref="IEmuClientApi.Screenshot"/>
		public static void Screenshot(string path = null) => EmuClient.Screenshot(path);

		/// <inheritdoc cref="IEmuClientApi.ScreenshotToClipboard"/>
		public static void ScreenshotToClipboard() => EmuClient.ScreenshotToClipboard();

		/// <inheritdoc cref="IEmuClientApi.ScreenWidth"/>
		public static int ScreenWidth() => EmuClient.ScreenWidth();

		/// <inheritdoc cref="IEmuClientApi.SeekFrame"/>
		public static void SeekFrame(int frame) => EmuClient.SeekFrame(frame);

		/// <inheritdoc cref="IEmuClientApi.SetClientExtraPadding"/>
		public static void SetExtraPadding(int left, int top = 0, int right = 0, int bottom = 0) => EmuClient.SetClientExtraPadding(left, top, right, bottom);

		/// <inheritdoc cref="IEmuClientApi.SetGameExtraPadding"/>
		public static void SetGameExtraPadding(int left, int top = 0, int right = 0, int bottom = 0) => EmuClient.SetGameExtraPadding(left, top, right, bottom);

		/// <inheritdoc cref="IEmuClientApi.SetInput"/>
		public static void SetInput(int player, Joypad joypad) => EmuClient.SetInput(player, joypad);

		/// <inheritdoc cref="IEmuClientApi.SetScreenshotOSD"/>
		public static void SetScreenshotOSD(bool value) => EmuClient.SetScreenshotOSD(value);

		/// <inheritdoc cref="IEmuClientApi.SetSoundOn"/>
		public static void SetSoundOn(bool enable) => EmuClient.SetSoundOn(enable);

		/// <inheritdoc cref="IEmuClientApi.SetTargetScanlineIntensity"/>
		public static void SetTargetScanlineIntensity(int val) => EmuClient.SetTargetScanlineIntensity(val);

		/// <inheritdoc cref="IEmuClientApi.SetWindowSize"/>
		public static void SetWindowSize(int size) => EmuClient.SetWindowSize(size);

		/// <inheritdoc cref="IEmuClientApi.SpeedMode"/>
		public static void SpeedMode(int percent) => EmuClient.SpeedMode(percent);

		/// <inheritdoc cref="IEmuClientApi.TogglePause"/>
		public static void TogglePause() => EmuClient.TogglePause();

		/// <inheritdoc cref="IEmuClientApi.TransformPoint"/>
		public static Point TransformPoint(Point point) => EmuClient.TransformPoint(point);

		/// <inheritdoc cref="IEmuClientApi.Unpause"/>
		public static void Unpause() => EmuClient.Unpause();

		/// <inheritdoc cref="IEmuClientApi.UnpauseAv"/>
		public static void UnpauseAv() => EmuClient.UnpauseAv();

		/// <inheritdoc cref="Unpause"/>
		public static void UnpauseEmulation() => Unpause();

		/// <inheritdoc cref="IEmuClientApi.UpdateEmulatorAndVP"/>
		public static void UpdateEmulatorAndVP(IEmulator emu = null) => EmuClient.UpdateEmulatorAndVP(emu);

		/// <inheritdoc cref="IEmuClientApi.Xpos"/>
		public static int Xpos() => EmuClient.Xpos();

		/// <inheritdoc cref="IEmuClientApi.Ypos"/>
		public static int Ypos() => EmuClient.Ypos();
	}
}
