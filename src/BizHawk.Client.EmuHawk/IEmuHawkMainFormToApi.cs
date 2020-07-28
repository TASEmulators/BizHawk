using System.Drawing;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public interface IMainFormForApi
	{
		/// <remarks>only referenced from <see cref="ClientLuaLibrary"/></remarks>
		CheatCollection CheatList { get; }

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		Point DesktopLocation { get; }

		/// <remarks>only referenced from <see cref="ClientLuaLibrary"/></remarks>
		IEmulator Emulator { get; }

		bool EmulatorPaused { get; }

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		bool InvisibleEmulation { set; }

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		bool IsSeeking { get; }

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		bool IsTurboing { get; }

		/// <remarks>only referenced from <see cref="InputApi"/></remarks>
		long MouseWheelTracker { get; }

		/// <remarks>only referenced from <see cref="CommApi"/></remarks>
		(HttpCommunication HTTP, MemoryMappedFiles MMF, SocketServer Sockets, WebSocketClient WebSocketClient) NetworkingHelpers { get; }

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		bool PauseAvi { set; }

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		PresentationPanel PresentationPanel { get; }

		void AddOnScreenMessage(string message);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void ClearHolds();

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void ClickSpeedItem(int num);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void CloseEmulator(int? exitCode = null);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void CloseRom(bool clearSram = false);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void EnableRewind(bool enabled);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		bool FlushSaveRAM(bool autosave = false);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void FrameAdvance();

		void FrameBufferResized();

		void FrameSkipMessage();

		/// <remarks>only referenced from <see cref="SaveStateApi"/></remarks>
		void LoadQuickSave(string quickSlotName, bool suppressOSD = false);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		bool LoadRom(string path, LoadRomArgs args);

		void LoadState(string combine, string name, bool suppressOSD = false);

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void PauseEmulator();

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void RebootCore();

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void Render();

		/// <remarks>only referenced from <see cref="SaveStateApi"/></remarks>
		void SaveQuickSave(string quickSlotName, bool fromLua = false, bool suppressOSD = false);

		void SaveState(string path, string userFriendlyStateName, bool fromLua = false, bool suppressOSD = false);

		void SeekFrameAdvance();

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		void StepRunLoop_Throttle();

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

		void UnpauseEmulator();
	}
}
