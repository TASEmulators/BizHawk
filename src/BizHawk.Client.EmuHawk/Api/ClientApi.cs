using System;
using System.Drawing;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class ClientApi
	{
		/// <inheritdoc cref="IEmuClientApi.DoFrameAdvance"/>
		public static SystemInfo RunningSystem => GlobalWin.ClientApi.RunningSystem;

		/// <inheritdoc cref="IEmuClientApi.BeforeQuickLoad"/>
		public static event BeforeQuickLoadEventHandler BeforeQuickLoad
		{
			add => GlobalWin.ClientApi.BeforeQuickLoad += value;
			remove => GlobalWin.ClientApi.BeforeQuickLoad -= value;
		}

		/// <inheritdoc cref="IEmuClientApi.BeforeQuickSave"/>
		public static event BeforeQuickSaveEventHandler BeforeQuickSave
		{
			add => GlobalWin.ClientApi.BeforeQuickSave += value;
			remove => GlobalWin.ClientApi.BeforeQuickSave -= value;
		}

		/// <inheritdoc cref="IEmuClientApi.RomLoaded"/>
		public static event EventHandler RomLoaded
		{
			add => GlobalWin.ClientApi.RomLoaded += value;
			remove => GlobalWin.ClientApi.RomLoaded -= value;
		}

		/// <inheritdoc cref="IEmuClientApi.StateLoaded"/>
		public static event StateLoadedEventHandler StateLoaded
		{
			add => GlobalWin.ClientApi.StateLoaded += value;
			remove => GlobalWin.ClientApi.StateLoaded -= value;
		}

		/// <inheritdoc cref="IEmuClientApi.StateSaved"/>
		public static event StateSavedEventHandler StateSaved
		{
			add => GlobalWin.ClientApi.StateSaved += value;
			remove => GlobalWin.ClientApi.StateSaved -= value;
		}

		/// <inheritdoc cref="IEmuClientApi.BorderHeight"/>
		public static int BorderHeight() => GlobalWin.ClientApi.BorderHeight();

		/// <inheritdoc cref="IEmuClientApi.BorderWidth"/>
		public static int BorderWidth() => GlobalWin.ClientApi.BorderWidth();

		/// <inheritdoc cref="IEmuClientApi.BufferHeight"/>
		public static int BufferHeight() => GlobalWin.ClientApi.BufferHeight();

		/// <inheritdoc cref="IEmuClientApi.BufferWidth"/>
		public static int BufferWidth() => GlobalWin.ClientApi.BufferWidth();

		/// <inheritdoc cref="IEmuClientApi.ClearAutohold"/>
		public static void ClearAutohold() => GlobalWin.ClientApi.ClearAutohold();

		/// <inheritdoc cref="IEmuClientApi.CloseEmulator"/>
		public static void CloseEmulator() => GlobalWin.ClientApi.CloseEmulator();

		/// <inheritdoc cref="IEmuClientApi.CloseEmulatorWithCode"/>
		public static void CloseEmulatorWithCode(int exitCode) => GlobalWin.ClientApi.CloseEmulatorWithCode(exitCode);

		/// <inheritdoc cref="IEmuClientApi.CloseRom"/>
		public static void CloseRom() => GlobalWin.ClientApi.CloseRom();

		/// <inheritdoc cref="IEmuClientApi.DisplayMessages"/>
		public static void DisplayMessages(bool value) => GlobalWin.ClientApi.DisplayMessages(value);

		/// <inheritdoc cref="IEmuClientApi.DoFrameAdvance"/>
		public static void DoFrameAdvance() => GlobalWin.ClientApi.DoFrameAdvance();

		/// <inheritdoc cref="IEmuClientApi.DoFrameAdvanceAndUnpause"/>
		public static void DoFrameAdvanceAndUnpause() => GlobalWin.ClientApi.DoFrameAdvanceAndUnpause();

		/// <inheritdoc cref="IEmuClientApi.EnableRewind"/>
		public static void EnableRewind(bool enabled) => GlobalWin.ClientApi.EnableRewind(enabled);

		/// <inheritdoc cref="IEmuClientApi.FrameSkip"/>
		public static void FrameSkip(int numFrames) => GlobalWin.ClientApi.FrameSkip(numFrames);

		/// <inheritdoc cref="IEmuClientApi.GetInput"/>
		public static Joypad GetInput(int player) => GlobalWin.ClientApi.GetInput(player);

		/// <inheritdoc cref="IEmuClientApi.GetSoundOn"/>
		public static bool GetSoundOn() => GlobalWin.ClientApi.GetSoundOn();

		/// <inheritdoc cref="IEmuClientApi.GetTargetScanlineIntensity"/>
		public static int GetTargetScanlineIntensity() => GlobalWin.ClientApi.GetTargetScanlineIntensity();

		/// <inheritdoc cref="IEmuClientApi.GetWindowSize"/>
		public static int GetWindowSize() => GlobalWin.ClientApi.GetWindowSize();

		/// <inheritdoc cref="IEmuClientApi.InvisibleEmulation"/>
		public static void InvisibleEmulation(bool invisible) => GlobalWin.ClientApi.InvisibleEmulation(invisible);

		/// <inheritdoc cref="IEmuClientApi.IsPaused"/>
		public static bool IsPaused() => GlobalWin.ClientApi.IsPaused();

		/// <inheritdoc cref="IEmuClientApi.IsSeeking"/>
		public static bool IsSeeking() => GlobalWin.ClientApi.IsSeeking();

		/// <inheritdoc cref="IEmuClientApi.IsTurbo"/>
		public static bool IsTurbo() => GlobalWin.ClientApi.IsTurbo();

		/// <inheritdoc cref="IEmuClientApi.LoadState"/>
		public static void LoadState(string name) => GlobalWin.ClientApi.LoadState(name);

		/// <inheritdoc cref="IEmuClientApi.OnBeforeQuickLoad"/>
		public static void OnBeforeQuickLoad(object sender, string quickSaveSlotName, out bool eventHandled) => GlobalWin.ClientApi.OnBeforeQuickLoad(sender, quickSaveSlotName, out eventHandled);

		/// <inheritdoc cref="IEmuClientApi.OnBeforeQuickSave"/>
		public static void OnBeforeQuickSave(object sender, string quickSaveSlotName, out bool eventHandled) => GlobalWin.ClientApi.OnBeforeQuickSave(sender, quickSaveSlotName, out eventHandled);

		/// <inheritdoc cref="IEmuClientApi.OnRomLoaded"/>
		public static void OnRomLoaded(IEmulator emu) => GlobalWin.ClientApi.OnRomLoaded(emu);

		/// <inheritdoc cref="IEmuClientApi.OnStateLoaded"/>
		public static void OnStateLoaded(object sender, string stateName) => GlobalWin.ClientApi.OnStateLoaded(sender, stateName);

		/// <inheritdoc cref="IEmuClientApi.OnStateSaved"/>
		public static void OnStateSaved(object sender, string stateName) => GlobalWin.ClientApi.OnStateSaved(sender, stateName);

		/// <inheritdoc cref="IEmuClientApi.OpenRom"/>
		public static void OpenRom(string path) => GlobalWin.ClientApi.OpenRom(path);

		/// <inheritdoc cref="IEmuClientApi.Pause"/>
		public static void Pause() => GlobalWin.ClientApi.Pause();

		/// <inheritdoc cref="IEmuClientApi.PauseAv"/>
		public static void PauseAv() => GlobalWin.ClientApi.PauseAv();

		/// <inheritdoc cref="IEmuClientApi.RebootCore"/>
		public static void RebootCore() => GlobalWin.ClientApi.RebootCore();

		/// <inheritdoc cref="IEmuClientApi.SaveRam"/>
		public static void SaveRam() => GlobalWin.ClientApi.SaveRam();

		/// <inheritdoc cref="IEmuClientApi.SaveState"/>
		public static void SaveState(string name) => GlobalWin.ClientApi.SaveState(name);

		/// <inheritdoc cref="IEmuClientApi.ScreenHeight"/>
		public static int ScreenHeight() => GlobalWin.ClientApi.ScreenHeight();

		/// <inheritdoc cref="IEmuClientApi.Screenshot"/>
		public static void Screenshot(string path = null) => GlobalWin.ClientApi.Screenshot(path);

		/// <inheritdoc cref="IEmuClientApi.ScreenshotToClipboard"/>
		public static void ScreenshotToClipboard() => GlobalWin.ClientApi.ScreenshotToClipboard();

		/// <inheritdoc cref="IEmuClientApi.ScreenWidth"/>
		public static int ScreenWidth() => GlobalWin.ClientApi.ScreenWidth();

		/// <inheritdoc cref="IEmuClientApi.SeekFrame"/>
		public static void SeekFrame(int frame) => GlobalWin.ClientApi.SeekFrame(frame);

		/// <inheritdoc cref="IEmuClientApi.SetClientExtraPadding"/>
		public static void SetExtraPadding(int left, int top = 0, int right = 0, int bottom = 0) => GlobalWin.ClientApi.SetClientExtraPadding(left, top, right, bottom);

		/// <inheritdoc cref="IEmuClientApi.SetGameExtraPadding"/>
		public static void SetGameExtraPadding(int left, int top = 0, int right = 0, int bottom = 0) => GlobalWin.ClientApi.SetGameExtraPadding(left, top, right, bottom);

		/// <inheritdoc cref="IEmuClientApi.SetInput"/>
		public static void SetInput(int player, Joypad joypad) => GlobalWin.ClientApi.SetInput(player, joypad);

		/// <inheritdoc cref="IEmuClientApi.SetScreenshotOSD"/>
		public static void SetScreenshotOSD(bool value) => GlobalWin.ClientApi.SetScreenshotOSD(value);

		/// <inheritdoc cref="IEmuClientApi.SetSoundOn"/>
		public static void SetSoundOn(bool enable) => GlobalWin.ClientApi.SetSoundOn(enable);

		/// <inheritdoc cref="IEmuClientApi.SetTargetScanlineIntensity"/>
		public static void SetTargetScanlineIntensity(int val) => GlobalWin.ClientApi.SetTargetScanlineIntensity(val);

		/// <inheritdoc cref="IEmuClientApi.SetWindowSize"/>
		public static void SetWindowSize(int size) => GlobalWin.ClientApi.SetWindowSize(size);

		/// <inheritdoc cref="IEmuClientApi.SpeedMode"/>
		public static void SpeedMode(int percent) => GlobalWin.ClientApi.SpeedMode(percent);

		/// <inheritdoc cref="IEmuClientApi.TogglePause"/>
		public static void TogglePause() => GlobalWin.ClientApi.TogglePause();

		/// <inheritdoc cref="IEmuClientApi.TransformPoint"/>
		public static Point TransformPoint(Point point) => GlobalWin.ClientApi.TransformPoint(point);

		/// <inheritdoc cref="IEmuClientApi.Unpause"/>
		public static void Unpause() => GlobalWin.ClientApi.Unpause();

		/// <inheritdoc cref="IEmuClientApi.UnpauseAv"/>
		public static void UnpauseAv() => GlobalWin.ClientApi.UnpauseAv();

		/// <inheritdoc cref="Unpause"/>
		public static void UnpauseEmulation() => Unpause();

		/// <inheritdoc cref="IEmuClientApi.UpdateEmulatorAndVP"/>
		public static void UpdateEmulatorAndVP(IEmulator emu = null) => GlobalWin.ClientApi.UpdateEmulatorAndVP(emu);

		/// <inheritdoc cref="IEmuClientApi.Xpos"/>
		public static int Xpos() => GlobalWin.ClientApi.Xpos();

		/// <inheritdoc cref="IEmuClientApi.Ypos"/>
		public static int Ypos() => GlobalWin.ClientApi.Ypos();
	}
}
