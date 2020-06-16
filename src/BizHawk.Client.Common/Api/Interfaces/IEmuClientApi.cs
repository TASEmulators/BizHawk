using System;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IEmuClientApi : IExternalApi
	{
		SystemInfo RunningSystem { get; }

		/// <summary>
		/// Occurs before a quickload is done (just after user has pressed the shortcut button
		/// or has click on the item menu)
		/// </summary>
		event BeforeQuickLoadEventHandler BeforeQuickLoad;

		/// <summary>
		/// Occurs before a quicksave is done (just after user has pressed the shortcut button
		/// or has click on the item menu)
		/// </summary>
		event BeforeQuickSaveEventHandler BeforeQuickSave;

		/// <summary>
		/// Occurs when a ROM is successfully loaded
		/// </summary>
		event EventHandler RomLoaded;

		/// <summary>
		/// Occurs when a savestate is successfully loaded
		/// </summary>
		event StateLoadedEventHandler StateLoaded;

		/// <summary>
		/// Occurs when a savestate is successfully saved
		/// </summary>
		event StateSavedEventHandler StateSaved;

		int BorderHeight();

		int BorderWidth();

		int BufferHeight();

		int BufferWidth();

		void ClearAutohold();

		void CloseEmulator();

		void CloseEmulatorWithCode(int exitCode);

		void CloseRom();

		void DisplayMessages(bool value);

		/// <summary>
		/// THE FrameAdvance stuff
		/// </summary>
		void DoFrameAdvance();

		/// <summary>
		/// THE FrameAdvance stuff
		/// Auto unpause emulation
		/// </summary>
		void DoFrameAdvanceAndUnpause();

		void EnableRewind(bool enabled);

		void FrameSkip(int numFrames);

		/// <summary>
		/// Gets a <see cref="Joypad"/> for specified player
		/// </summary>
		/// <param name="player">Player (one based) you want current inputs</param>
		/// <returns>A <see cref="Joypad"/> populated with current inputs</returns>
		/// <exception cref="IndexOutOfRangeException">Raised when you specify a player less than 1 or greater than maximum allows (see SystemInfo class to get this information)</exception>
		Joypad GetInput(int player);

		bool GetSoundOn();

		int GetTargetScanlineIntensity();

		int GetWindowSize();

		/// <summary>
		/// Use with <see cref="SeekFrame(int)"/> for CamHack.
		/// Refer to <c>MainForm.InvisibleEmulation</c> for the workflow details.
		/// </summary>
		void InvisibleEmulation(bool invisible);

		bool IsPaused();

		bool IsSeeking();

		bool IsTurbo();

		/// <summary>
		/// Load a savestate specified by its name
		/// </summary>
		/// <param name="name">Savestate friendly name</param>
		void LoadState(string name);

		/// <summary>
		/// Raised before a quickload is done (just after pressing shortcut button)
		/// </summary>
		/// <param name="sender">Object who raised the event</param>
		/// <param name="quickSaveSlotName">Slot used for quickload</param>
		/// <param name="eventHandled">A boolean that can be set if users want to handle save themselves; if so, BizHawk won't do anything</param>
		void OnBeforeQuickLoad(object sender, string quickSaveSlotName, out bool eventHandled);

		/// <summary>
		/// Raised before a quicksave is done (just after pressing shortcut button)
		/// </summary>
		/// <param name="sender">Object who raised the event</param>
		/// <param name="quickSaveSlotName">Slot used for quicksave</param>
		/// <param name="eventHandled">A boolean that can be set if users want to handle save themselves; if so, BizHawk won't do anything</param>
		void OnBeforeQuickSave(object sender, string quickSaveSlotName, out bool eventHandled);

		/// <summary>
		/// Raise when a rom is successfully Loaded
		/// </summary>
		void OnRomLoaded(IEmulator emu);

		/// <summary>
		/// Raise when a state is loaded
		/// </summary>
		/// <param name="sender">Object who raised the event</param>
		/// <param name="stateName">User friendly name for saved state</param>
		void OnStateLoaded(object sender, string stateName);

		/// <summary>
		/// Raise when a state is saved
		/// </summary>
		/// <param name="sender">Object who raised the event</param>
		/// <param name="stateName">User friendly name for saved state</param>
		void OnStateSaved(object sender, string stateName);

		void OpenRom(string path);

		void Pause();

		void PauseAv();

		void RebootCore();

		void SaveRam();

		/// <summary>
		/// Save a state with specified name
		/// </summary>
		/// <param name="name">Savestate friendly name</param>
		void SaveState(string name);

		int ScreenHeight();

		void Screenshot(string path = null);

		void ScreenshotToClipboard();

		int ScreenWidth();

		/// <summary>
		/// Use with <see cref="InvisibleEmulation(bool)"/> for CamHack.
		/// Refer to <c>MainForm.InvisibleEmulation</c> for the workflow details.
		/// </summary>
		void SeekFrame(int frame);

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		/// <param name="right">Right padding</param>
		/// <param name="bottom">Bottom padding</param>
		void SetClientExtraPadding(int left, int top = 0, int right = 0, int bottom = 0);

		/// <summary>
		/// Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements
		/// </summary>
		/// <param name="left">Left padding</param>
		/// <param name="top">Top padding</param>
		/// <param name="right">Right padding</param>
		/// <param name="bottom">Bottom padding</param>
		void SetGameExtraPadding(int left, int top = 0, int right = 0, int bottom = 0);

		/// <summary>
		/// Set inputs in specified <see cref="Joypad"/> to specified player
		/// </summary>
		/// <param name="player">Player (one based) whom inputs must be set</param>
		/// <param name="joypad"><see cref="Joypad"/> with inputs</param>
		/// <exception cref="IndexOutOfRangeException">Raised when you specify a player less than 1 or greater than maximum allows (see SystemInfo class to get this information)</exception>
		/// <remarks>Still have some strange behaviour with multiple inputs; so this feature is still in beta</remarks>
		void SetInput(int player, Joypad joypad);

		void SetScreenshotOSD(bool value);

		void SetSoundOn(bool enable);

		void SetTargetScanlineIntensity(int val);

		void SetWindowSize(int size);

		void SpeedMode(int percent);

		void TogglePause();

		Point TransformPoint(Point point);

		void Unpause();

		void UnpauseAv();

		void UpdateEmulatorAndVP(IEmulator emu = null);

		int Xpos();

		int Ypos();
	}
}
