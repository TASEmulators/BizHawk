using System.Drawing;

namespace BizHawk.Client.Common
{
	public interface IEmuClientApi : IDisposable, IExternalApi
	{
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

		void CloseEmulator(int? exitCode = null);

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

		/// <returns>the (host) framerate, approximated from frame durations</returns>
		int GetApproxFramerate();

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
		/// <returns><see langword="true"/> iff succeeded</returns>
		bool LoadState(string name);

		bool OpenRom(string path);

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

		void SetScreenshotOSD(bool value);

		void SetSoundOn(bool enable);

		void SetTargetScanlineIntensity(int val);

		void SetWindowSize(int size);

		void SpeedMode(int percent);

		void TogglePause();

		Point TransformPoint(Point point);

		void Unpause();

		void UnpauseAv();

		int Xpos();

		int Ypos();
	}
}
