using System.Drawing;

using BizHawk.Client.Common.cheats;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMainFormForApi
	{
		/// <remarks>only referenced from <see cref="ClientLuaLibrary"/></remarks>
		CheatCollection CheatList { get; }

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		Point DesktopLocation { get; }

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		bool EmulatorPaused { get; }

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		bool InvisibleEmulateNextFrame { get; set; }

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		bool IsSeeking { get; }

		/// <remarks>referenced from <see cref="EmuClientApi"/> and <c>LuaConsole</c></remarks>
		bool IsTurboing { get; }

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		bool IsRewinding { get; }

		/// <remarks>only referenced from <see cref="CommApi"/></remarks>
		(HttpCommunication HTTP, MemoryMappedFiles MMF, SocketServer Sockets) NetworkingHelpers { get; }

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		bool PauseAvi { get; set; }

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void ClearHolds();

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void ClickSpeedItem(int num);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void CloseEmulator(int? exitCode = null);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void CloseRom(bool clearSram = false);

		/// <remarks>only referenced from <see cref="ClientLuaLibrary"/></remarks>
		IDecodeResult DecodeCheatForAPI(string code, out MemoryDomain/*?*/ domain);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void EnableRewind(bool enabled);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		bool FlushSaveRAM(bool autosave = false);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void FrameAdvance(bool discardApiHawkSurfaces = true);

		/// <param name="forceWindowResize">Override <see cref="Common.Config.ResizeWithFramebuffer"/></param>
		/// <remarks>referenced from <see cref="EmuClientApi"/> and <c>LuaConsole</c></remarks>
		void FrameBufferResized(bool forceWindowResize = false);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void FrameSkipMessage();

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		int GetApproxFramerate();

		/// <summary>
		/// essentially <c>return MainForm.StartNewMovie(MovieSession.Get(filename), record: false);</c>,
		/// but also ensures a rom is loaded, and defers to TAStudio
		/// </summary>
		/// <param name="archive">unused</param>
		/// <remarks>only referenced from <see cref="MovieApi"/></remarks>
		bool LoadMovie(string filename, string archive = null);

		/// <remarks>only referenced from <see cref="SaveStateApi"/></remarks>
		bool LoadQuickSave(int slot, bool suppressOSD = false);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		bool LoadRom(string path, LoadRomArgs args);

		/// <remarks>referenced from <see cref="EmuClientApi"/> and <see cref="SaveStateApi"/></remarks>
		bool LoadState(string path, string userFriendlyStateName, bool suppressOSD = false);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void PauseEmulator();

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		bool RebootCore();

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void Render();

		/// <remarks>only referenced from <see cref="MovieApi"/></remarks>
		bool RestartMovie();

		/// <remarks>only referenced from <see cref="SaveStateApi"/></remarks>
		void SaveQuickSave(int slot, bool suppressOSD = false, bool fromLua = false);

		/// <remarks>referenced from <see cref="EmuClientApi"/> and <see cref="SaveStateApi"/></remarks>
		void SaveState(string path, string userFriendlyStateName, bool fromLua = false, bool suppressOSD = false);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void SeekFrameAdvance();

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void StepRunLoop_Throttle();

		/// <remarks>only referenced from <see cref="MovieApi"/></remarks>
		void StopMovie(bool saveChanges = true);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void TakeScreenshot();

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void TakeScreenshot(string path);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void TakeScreenshotToClipboard();

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void TogglePause();

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void ToggleSound();

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		bool UnpauseEmulator();

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		event BeforeQuickLoadEventHandler QuicksaveLoad;

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		event BeforeQuickSaveEventHandler QuicksaveSave;

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		event EventHandler RomLoaded;

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		event StateLoadedEventHandler SavestateLoaded;

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		event StateSavedEventHandler SavestateSaved;
	}
}
