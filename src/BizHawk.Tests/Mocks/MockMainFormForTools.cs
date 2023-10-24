using System;
using System.Collections.Generic;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Tests.Mocks
{
	internal class MockMainFormForTools : IMainFormForTools
	{
		public EmuClientApi? EmuClient { get ; set; }

		public bool ShowMessageBox2(IDialogParent? owner, string text, string? caption = null, EMsgBoxIcon? icon = null, bool useOKCancel = false) => true;

		public CheatCollection CheatList => throw new NotImplementedException();

		public string CurrentlyOpenRom => throw new NotImplementedException();

		public LoadRomArgs CurrentlyOpenRomArgs => throw new NotImplementedException();

		public bool EmulatorPaused => throw new NotImplementedException();

		public FirmwareManager FirmwareManager => throw new NotImplementedException();

		public bool GameIsClosing => throw new NotImplementedException();

		public bool HoldFrameAdvance { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public bool InvisibleEmulation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public bool IsSeeking => throw new NotImplementedException();

		public bool IsTurboing => throw new NotImplementedException();

		public int? PauseOnFrame { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public bool PressRewind { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public bool BlockFrameAdvance { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public event Action<bool>? OnPauseChanged;

		public void AddOnScreenMessage(string message, int? duration = null) => throw new NotImplementedException();
		public BitmapBuffer CaptureOSD() => throw new NotImplementedException();
		public void DisableRewind() => throw new NotImplementedException();
		public void EnableRewind(bool enabled) => throw new NotImplementedException();
		public bool EnsureCoreIsAccurate() => throw new NotImplementedException();
		public void FrameAdvance() => throw new NotImplementedException();
		public void FrameBufferResized() => throw new NotImplementedException();
		public bool LoadQuickSave(int slot, bool suppressOSD = false) => throw new NotImplementedException();
		public bool LoadRom(string path, LoadRomArgs args) => throw new NotImplementedException();
		public BitmapBuffer MakeScreenshotImage() => throw new NotImplementedException();
		public void MaybePauseFromMenuOpened() => throw new NotImplementedException();
		public void MaybeUnpauseFromMenuClosed() => throw new NotImplementedException();
		public void PauseEmulator() => throw new NotImplementedException();
		public void RelinquishControl(IControlMainform master) => throw new NotImplementedException();
		public void SeekFrameAdvance() => throw new NotImplementedException();
		public void SetMainformMovieInfo() => throw new NotImplementedException();
		public IReadOnlyList<string>? ShowFileMultiOpenDialog(IDialogParent dialogParent, string? filterStr, ref int filterIndex, string initDir, bool discardCWDChange = false, string? initFileName = null, bool maySelectMultiple = false, string? windowTitle = null) => throw new NotImplementedException();
		public string? ShowFileSaveDialog(IDialogParent dialogParent, bool discardCWDChange, string? fileExt, string? filterStr, string initDir, string? initFileName, bool muteOverwriteWarning) => throw new NotImplementedException();
		public void ShowMessageBox(IDialogParent? owner, string text, string? caption = null, EMsgBoxIcon? icon = null) => throw new NotImplementedException();
		public bool? ShowMessageBox3(IDialogParent? owner, string text, string? caption = null, EMsgBoxIcon? icon = null) => throw new NotImplementedException();
		public bool StartNewMovie(IMovie movie, bool record) => throw new NotImplementedException();
		public void StartSound() => throw new NotImplementedException();
		public void StopSound() => throw new NotImplementedException();
		public void TakeBackControl() => throw new NotImplementedException();
		public void Throttle() => throw new NotImplementedException();
		public void TogglePause() => throw new NotImplementedException();
		public void UnpauseEmulator() => throw new NotImplementedException();
		public void Unthrottle() => throw new NotImplementedException();
		public void UpdateDumpInfo(RomStatus? newStatus = null) => throw new NotImplementedException();
		public void UpdateStatusSlots() => throw new NotImplementedException();
		public void UpdateWindowTitle() => throw new NotImplementedException();
	}
}
