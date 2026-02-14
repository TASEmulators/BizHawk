using BizHawk.Bizware.Graphics;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMainFormForTools : IDialogController
	{
		/// <remarks>referenced by 3 or more tools</remarks>
		CheatCollection CheatList { get; }

		/// <remarks>referenced by 3 or more tools</remarks>
		string CurrentlyOpenRom { get; }

		/// <remarks>referenced from HexEditor and RetroAchievements</remarks>
		LoadRomArgs CurrentlyOpenRomArgs { get; }

		/// <remarks>only referenced from TAStudio</remarks>
		bool EmulatorPaused { get; }

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

		/// <remarks>only referenced from TAStudio</remarks>
		bool IsFastForwarding { get; }

		/// <remarks>referenced from PlayMovie and TAStudio</remarks>
		int? PauseOnFrame { get; set; }

		/// <remarks>only referenced from PlaybackBox</remarks>
		bool PressRewind { get; set; }

		/// <remarks>referenced from BookmarksBranchesBox and VideoWriterChooserForm</remarks>
		BitmapBuffer CaptureOSD();

		/// <remarks>only referenced from TAStudio</remarks>
		void DisableRewind();

		/// <remarks>only referenced from TAStudio</remarks>
		void EnableRewind(bool enabled);

		/// <remarks>only referenced from TAStudio</remarks>
		bool EnsureCoreIsAccurate();

		/// <remarks>only referenced from TAStudio</remarks>
		void FrameAdvance(bool discardApiHawkSurfaces = true);

		/// <remarks>only referenced from LuaConsole</remarks>
		/// <param name="forceWindowResize">Override <see cref="Common.Config.ResizeWithFramebuffer"/></param>
		void FrameBufferResized(bool forceWindowResize = false);

		/// <remarks>only referenced from BasicBot</remarks>
		bool LoadQuickSave(int slot, bool suppressOSD = false);

		/// <remarks>referenced from MultiDiskBundler and RetroAchievements</remarks>
		bool LoadRom(string path, LoadRomArgs args);

		/// <remarks>only referenced from BookmarksBranchesBox</remarks>
		BitmapBuffer MakeScreenshotImage();

		/// <remarks>referenced from ToolFormBase</remarks>
		void MaybePauseFromMenuOpened();

		/// <remarks>referenced from ToolFormBase</remarks>
		void MaybeUnpauseFromMenuClosed();

		/// <remarks>referenced by 3 or more tools</remarks>
		void PauseEmulator();

		/// <remarks>only referenced from TAStudio</remarks>
		bool BlockFrameAdvance { get; set; }

		/// <remarks>referenced from PlaybackBox and TAStudio</remarks>
		void SetMainformMovieInfo();

		/// <remarks>referenced by 3 or more tools</remarks>
		bool StartNewMovie(IMovie movie, bool newMovie);

		/// <remarks>only referenced from BasicBot</remarks>
		void Throttle();

		/// <remarks>only referenced from TAStudio</remarks>
		void TogglePause();

		/// <remarks>referenced by 3 or more tools</remarks>
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
