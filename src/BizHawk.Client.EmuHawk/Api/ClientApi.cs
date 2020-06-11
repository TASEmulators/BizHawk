using System;
using System.Drawing;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class ClientApi
	{
		/// <inheritdoc cref="IEmuClient.DoFrameAdvance"/>
		public static SystemInfo RunningSystem => GlobalWin.ClientApi.RunningSystem;

		/// <inheritdoc cref="IEmuClient.BeforeQuickLoad"/>
		public static event BeforeQuickLoadEventHandler BeforeQuickLoad
		{
			add => GlobalWin.ClientApi.BeforeQuickLoad += value;
			remove => GlobalWin.ClientApi.BeforeQuickLoad -= value;
		}

		/// <inheritdoc cref="IEmuClient.BeforeQuickSave"/>
		public static event BeforeQuickSaveEventHandler BeforeQuickSave
		{
			add => GlobalWin.ClientApi.BeforeQuickSave += value;
			remove => GlobalWin.ClientApi.BeforeQuickSave -= value;
		}

		/// <inheritdoc cref="IEmuClient.RomLoaded"/>
		public static event EventHandler RomLoaded
		{
			add => GlobalWin.ClientApi.RomLoaded += value;
			remove => GlobalWin.ClientApi.RomLoaded -= value;
		}

		/// <inheritdoc cref="IEmuClient.StateLoaded"/>
		public static event StateLoadedEventHandler StateLoaded
		{
			add => GlobalWin.ClientApi.StateLoaded += value;
			remove => GlobalWin.ClientApi.StateLoaded -= value;
		}

		/// <inheritdoc cref="IEmuClient.StateSaved"/>
		public static event StateSavedEventHandler StateSaved
		{
			add => GlobalWin.ClientApi.StateSaved += value;
			remove => GlobalWin.ClientApi.StateSaved -= value;
		}

		/// <inheritdoc cref="IEmuClient.BorderHeight"/>
		public static int BorderHeight() => GlobalWin.ClientApi.BorderHeight();

		/// <inheritdoc cref="IEmuClient.BorderWidth"/>
		public static int BorderWidth() => GlobalWin.ClientApi.BorderWidth();

		/// <inheritdoc cref="IEmuClient.BufferHeight"/>
		public static int BufferHeight() => GlobalWin.ClientApi.BufferHeight();

		/// <inheritdoc cref="IEmuClient.BufferWidth"/>
		public static int BufferWidth() => GlobalWin.ClientApi.BufferWidth();

		/// <inheritdoc cref="IEmuClient.ClearAutohold"/>
		public static void ClearAutohold() => GlobalWin.ClientApi.ClearAutohold();

		/// <inheritdoc cref="IEmuClient.CloseEmulator"/>
		public static void CloseEmulator() => GlobalWin.ClientApi.CloseEmulator();

		/// <inheritdoc cref="IEmuClient.CloseEmulatorWithCode"/>
		public static void CloseEmulatorWithCode(int exitCode) => GlobalWin.ClientApi.CloseEmulatorWithCode(exitCode);

		/// <inheritdoc cref="IEmuClient.CloseRom"/>
		public static void CloseRom() => GlobalWin.ClientApi.CloseRom();

		/// <inheritdoc cref="IEmuClient.DisplayMessages"/>
		public static void DisplayMessages(bool value) => GlobalWin.ClientApi.DisplayMessages(value);

		/// <inheritdoc cref="IEmuClient.DoFrameAdvance"/>
		public static void DoFrameAdvance() => GlobalWin.ClientApi.DoFrameAdvance();

		/// <inheritdoc cref="IEmuClient.DoFrameAdvanceAndUnpause"/>
		public static void DoFrameAdvanceAndUnpause() => GlobalWin.ClientApi.DoFrameAdvanceAndUnpause();

		/// <inheritdoc cref="IEmuClient.EnableRewind"/>
		public static void EnableRewind(bool enabled) => GlobalWin.ClientApi.EnableRewind(enabled);

		/// <inheritdoc cref="IEmuClient.FrameSkip"/>
		public static void FrameSkip(int numFrames) => GlobalWin.ClientApi.FrameSkip(numFrames);

		/// <inheritdoc cref="IEmuClient.GetInput"/>
		public static Joypad GetInput(int player) => GlobalWin.ClientApi.GetInput(player);

		/// <inheritdoc cref="IEmuClient.GetSoundOn"/>
		public static bool GetSoundOn() => GlobalWin.ClientApi.GetSoundOn();

		/// <inheritdoc cref="IEmuClient.GetTargetScanlineIntensity"/>
		public static int GetTargetScanlineIntensity() => GlobalWin.ClientApi.GetTargetScanlineIntensity();

		/// <inheritdoc cref="IEmuClient.GetWindowSize"/>
		public static int GetWindowSize() => GlobalWin.ClientApi.GetWindowSize();

		/// <inheritdoc cref="IEmuClient.InvisibleEmulation"/>
		public static void InvisibleEmulation(bool invisible) => GlobalWin.ClientApi.InvisibleEmulation(invisible);

		/// <inheritdoc cref="IEmuClient.IsPaused"/>
		public static bool IsPaused() => GlobalWin.ClientApi.IsPaused();

		/// <inheritdoc cref="IEmuClient.IsSeeking"/>
		public static bool IsSeeking() => GlobalWin.ClientApi.IsSeeking();

		/// <inheritdoc cref="IEmuClient.IsTurbo"/>
		public static bool IsTurbo() => GlobalWin.ClientApi.IsTurbo();

		/// <inheritdoc cref="IEmuClient.LoadState"/>
		public static void LoadState(string name) => GlobalWin.ClientApi.LoadState(name);

		/// <inheritdoc cref="IEmuClient.OnBeforeQuickLoad"/>
		public static void OnBeforeQuickLoad(object sender, string quickSaveSlotName, out bool eventHandled) => GlobalWin.ClientApi.OnBeforeQuickLoad(sender, quickSaveSlotName, out eventHandled);

		/// <inheritdoc cref="IEmuClient.OnBeforeQuickSave"/>
		public static void OnBeforeQuickSave(object sender, string quickSaveSlotName, out bool eventHandled) => GlobalWin.ClientApi.OnBeforeQuickSave(sender, quickSaveSlotName, out eventHandled);

		/// <inheritdoc cref="IEmuClient.OnRomLoaded"/>
		public static void OnRomLoaded(IEmulator emu) => GlobalWin.ClientApi.OnRomLoaded(emu);

		/// <inheritdoc cref="IEmuClient.OnStateLoaded"/>
		public static void OnStateLoaded(object sender, string stateName) => GlobalWin.ClientApi.OnStateLoaded(sender, stateName);

		/// <inheritdoc cref="IEmuClient.OnStateSaved"/>
		public static void OnStateSaved(object sender, string stateName) => GlobalWin.ClientApi.OnStateSaved(sender, stateName);

		/// <inheritdoc cref="IEmuClient.OpenRom"/>
		public static void OpenRom(string path) => GlobalWin.ClientApi.OpenRom(path);

		/// <inheritdoc cref="IEmuClient.Pause"/>
		public static void Pause() => GlobalWin.ClientApi.Pause();

		/// <inheritdoc cref="IEmuClient.PauseAv"/>
		public static void PauseAv() => GlobalWin.ClientApi.PauseAv();

		/// <inheritdoc cref="IEmuClient.RebootCore"/>
		public static void RebootCore() => GlobalWin.ClientApi.RebootCore();

		/// <inheritdoc cref="IEmuClient.SaveRam"/>
		public static void SaveRam() => GlobalWin.ClientApi.SaveRam();

		/// <inheritdoc cref="IEmuClient.SaveState"/>
		public static void SaveState(string name) => GlobalWin.ClientApi.SaveState(name);

		/// <inheritdoc cref="IEmuClient.ScreenHeight"/>
		public static int ScreenHeight() => GlobalWin.ClientApi.ScreenHeight();

		/// <inheritdoc cref="IEmuClient.Screenshot"/>
		public static void Screenshot(string path = null) => GlobalWin.ClientApi.Screenshot(path);

		/// <inheritdoc cref="IEmuClient.ScreenshotToClipboard"/>
		public static void ScreenshotToClipboard() => GlobalWin.ClientApi.ScreenshotToClipboard();

		/// <inheritdoc cref="IEmuClient.ScreenWidth"/>
		public static int ScreenWidth() => GlobalWin.ClientApi.ScreenWidth();

		/// <inheritdoc cref="IEmuClient.SeekFrame"/>
		public static void SeekFrame(int frame) => GlobalWin.ClientApi.SeekFrame(frame);

		/// <inheritdoc cref="IEmuClient.SetExtraPadding"/>
		public static void SetExtraPadding(int left, int top = 0, int right = 0, int bottom = 0) => GlobalWin.ClientApi.SetExtraPadding(left, top, right, bottom);

		/// <inheritdoc cref="IEmuClient.SetGameExtraPadding"/>
		public static void SetGameExtraPadding(int left, int top = 0, int right = 0, int bottom = 0) => GlobalWin.ClientApi.SetGameExtraPadding(left, top, right, bottom);

		/// <inheritdoc cref="IEmuClient.SetInput"/>
		public static void SetInput(int player, Joypad joypad) => GlobalWin.ClientApi.SetInput(player, joypad);

		/// <inheritdoc cref="IEmuClient.SetScreenshotOSD"/>
		public static void SetScreenshotOSD(bool value) => GlobalWin.ClientApi.SetScreenshotOSD(value);

		/// <inheritdoc cref="IEmuClient.SetSoundOn"/>
		public static void SetSoundOn(bool enable) => GlobalWin.ClientApi.SetSoundOn(enable);

		/// <inheritdoc cref="IEmuClient.SetTargetScanlineIntensity"/>
		public static void SetTargetScanlineIntensity(int val) => GlobalWin.ClientApi.SetTargetScanlineIntensity(val);

		/// <inheritdoc cref="IEmuClient.SetWindowSize"/>
		public static void SetWindowSize(int size) => GlobalWin.ClientApi.SetWindowSize(size);

		/// <inheritdoc cref="IEmuClient.SpeedMode"/>
		public static void SpeedMode(int percent) => GlobalWin.ClientApi.SpeedMode(percent);

		/// <inheritdoc cref="IEmuClient.TogglePause"/>
		public static void TogglePause() => GlobalWin.ClientApi.TogglePause();

		/// <inheritdoc cref="IEmuClient.TransformPoint"/>
		public static Point TransformPoint(Point point) => GlobalWin.ClientApi.TransformPoint(point);

		/// <inheritdoc cref="IEmuClient.Unpause"/>
		public static void Unpause() => GlobalWin.ClientApi.Unpause();

		/// <inheritdoc cref="IEmuClient.UnpauseAv"/>
		public static void UnpauseAv() => GlobalWin.ClientApi.UnpauseAv();

		/// <inheritdoc cref="IEmuClient.UnpauseEmulation"/>
		public static void UnpauseEmulation() => GlobalWin.ClientApi.UnpauseEmulation();

		/// <inheritdoc cref="IEmuClient.UpdateEmulatorAndVP"/>
		public static void UpdateEmulatorAndVP(IEmulator emu = null) => GlobalWin.ClientApi.UpdateEmulatorAndVP(emu);

		/// <inheritdoc cref="IEmuClient.Xpos"/>
		public static int Xpos() => GlobalWin.ClientApi.Xpos();

		/// <inheritdoc cref="IEmuClient.Ypos"/>
		public static int Ypos() => GlobalWin.ClientApi.Ypos();
	}
}
