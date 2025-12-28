using BizHawk.Bizware.Graphics;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public interface IMainFormForTools : IDialogController
	{
		/// <remarks>referenced by 3 or more tools</remarks>
		CheatCollection CheatList { get; }

		/// <remarks>referenced by 3 or more tools</remarks>
		string CurrentlyOpenRom { get; }

		/// <remarks>referenced from <see cref="HexEditor"/> and RetroAchievements</remarks>
		LoadRomArgs CurrentlyOpenRomArgs { get; }

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		bool EmulatorPaused { get; }

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		bool GameIsClosing { get; }

		/// <remarks>only referenced from <see cref="PlaybackBox"/></remarks>
		bool HoldFrameAdvance { get; set; }

		/// <remarks>only referenced from <see cref="BasicBot"/></remarks>
		bool InvisibleEmulation { get; set; }

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		bool IsFastForwarding { get; }

		/// <remarks>referenced from <see cref="PlayMovie"/> and <see cref="TAStudio"/></remarks>
		int? PauseOnFrame { get; set; }

		/// <remarks>only referenced from <see cref="PlaybackBox"/></remarks>
		bool PressRewind { get; set; }

		/// <remarks>referenced from <see cref="BookmarksBranchesBox"/> and <see cref="VideoWriterChooserForm"/></remarks>
		BitmapBuffer CaptureOSD();

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		void DisableRewind();

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		void EnableRewind(bool enabled);

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		bool EnsureCoreIsAccurate();

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		void FrameAdvance(bool discardApiHawkSurfaces = true);

		/// <remarks>only referenced from <see cref="BasicBot"/></remarks>
		bool LoadQuickSave(int slot, bool suppressOSD = false);

		/// <remarks>referenced from <see cref="MultiDiskBundler"/> and RetroAchievements</remarks>
		bool LoadRom(string path, LoadRomArgs args);

		/// <remarks>only referenced from <see cref="BookmarksBranchesBox"/></remarks>
		BitmapBuffer MakeScreenshotImage();

		/// <remarks>referenced from <see cref="ToolFormBase"/></remarks>
		void MaybePauseFromMenuOpened();

		/// <remarks>referenced from <see cref="ToolFormBase"/></remarks>
		void MaybeUnpauseFromMenuClosed();

		/// <remarks>referenced by 3 or more tools</remarks>
		void PauseEmulator();

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		bool BlockFrameAdvance { get; set; }

		/// <remarks>referenced from <see cref="PlaybackBox"/> and <see cref="TAStudio"/></remarks>
		void SetMainformMovieInfo();

		/// <remarks>referenced by 3 or more tools</remarks>
		bool StartNewMovie(IMovie movie, bool newMovie);

		/// <remarks>only referenced from <see cref="BasicBot"/></remarks>
		void Throttle();

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		void TogglePause();

		/// <remarks>referenced by 3 or more tools</remarks>
		bool UnpauseEmulator();

		/// <remarks>only referenced from <see cref="BasicBot"/></remarks>
		void Unthrottle();

		/// <remarks>only referenced from <see cref="LogWindow"/></remarks>
		void UpdateDumpInfo(RomStatus? newStatus = null);

		/// <remarks>only referenced from <see cref="BookmarksBranchesBox"/></remarks>
		void UpdateStatusSlots();

		/// <remarks>referenced from <see cref="TAStudio"/> and RetroAchievements</remarks>
		void UpdateWindowTitle();
	}
}
