using System;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMainFormForTools : IDialogController
	{
		CheatCollection CheatList { get; }

		string CurrentlyOpenRom { get; }

		/// <remarks>only referenced from HexEditor</remarks>
		LoadRomArgs CurrentlyOpenRomArgs { get; }

		/// <remarks>only referenced from TAStudio</remarks>
		bool EmulatorPaused { get; }

		/// <remarks>only referenced from TAStudio</remarks>
		FirmwareManager FirmwareManager { get; }

		/// <remarks>only referenced from TAStudio</remarks>
		bool GameIsClosing { get; }

		/// <remarks>only referenced from PlaybackBox</remarks>
		bool HoldFrameAdvance { get; set; }

		/// <remarks>only referenced from BasicBot</remarks>
		bool InvisibleEmulation { get; set; }

		/// <remarks>only referenced from TAStudio</remarks>
		bool IsSeeking { get; }

		/// <remarks>only referenced from LuaConsole</remarks>
		bool IsTurboing { get; }

		int? PauseOnFrame { get; set; }

		/// <remarks>only referenced from PlaybackBox</remarks>
		bool PressRewind { get; set; }

		/// <remarks>only referenced from GenericDebugger</remarks>
		event Action<bool> OnPauseChanged;

		BitmapBuffer CaptureOSD();

		/// <remarks>only referenced from TAStudio</remarks>
		void DisableRewind();

		/// <remarks>only referenced from TAStudio</remarks>
		void EnableRewind(bool enabled);

		/// <remarks>only referenced from TAStudio</remarks>
		bool EnsureCoreIsAccurate();

		/// <remarks>only referenced from TAStudio</remarks>
		void FrameAdvance();

		/// <remarks>only referenced from LuaConsole</remarks>
		void FrameBufferResized();

		/// <remarks>only referenced from BasicBot</remarks>
		bool LoadQuickSave(int slot, bool suppressOSD = false);

		/// <remarks>only referenced from MultiDiskBundler</remarks>
		bool LoadRom(string path, LoadRomArgs args);

		/// <remarks>only referenced from BookmarksBranchesBox</remarks>
		BitmapBuffer MakeScreenshotImage();

		void MaybePauseFromMenuOpened();

		void MaybeUnpauseFromMenuClosed();

		void PauseEmulator();

		bool BlockFrameAdvance { get; set; }

		/// <remarks>only referenced from TAStudio</remarks>
		void RelinquishControl(IControlMainform master);

		/// <remarks>only referenced from TAStudio</remarks>
		void SeekFrameAdvance();

		void SetMainformMovieInfo();

		bool StartNewMovie(IMovie movie, bool record);

		/// <remarks>only referenced from TAStudio</remarks>
		void TakeBackControl();

		/// <remarks>only referenced from BasicBot</remarks>
		void Throttle();

		/// <remarks>only referenced from TAStudio</remarks>
		void TogglePause();

		void UnpauseEmulator();

		/// <remarks>only referenced from BasicBot</remarks>
		void Unthrottle();

		/// <remarks>only referenced from LogWindow</remarks>
		void UpdateDumpInfo(RomStatus? newStatus = null);

		/// <remarks>only referenced from BookmarksBranchesBox</remarks>
		void UpdateStatusSlots();

		/// <remarks>only referenced from TAStudio</remarks>
		void UpdateWindowTitle();
	}
}
