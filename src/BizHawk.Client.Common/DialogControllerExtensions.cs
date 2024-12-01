#nullable enable

using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public static class DialogControllerExtensions
	{
		public static void AddOnScreenMessage(this IDialogParent dialogParent, string message, int? duration = null)
			=> dialogParent.DialogController.AddOnScreenMessage(message, duration);

		public static void DoWithTempMute(this IDialogController dialogController, Action action)
		{
			dialogController.StopSound();
			action();
			dialogController.StartSound();
		}

		public static T DoWithTempMute<T>(this IDialogController dialogController, Func<T> action)
		{
			dialogController.StopSound();
			var ret = action();
			dialogController.StartSound();
			return ret;
		}

		/// <summary>
		/// Creates and shows a <c>System.Windows.Forms.MessageBox</c> or equivalent with the receiver (<paramref name="dialogParent"/>) as its parent, with the given <paramref name="text"/>,
		/// and with the given <paramref name="caption"/> and <paramref name="icon"/> if they're specified.
		/// </summary>
		public static void ModalMessageBox(
			this IDialogParent dialogParent,
			string text,
			string? caption = null,
			EMsgBoxIcon? icon = null)
				=> dialogParent.DialogController.ShowMessageBox(owner: dialogParent, text: text, caption: caption, icon: icon);

		/// <summary>
		/// Creates and shows a <c>System.Windows.Forms.MessageBox</c> or equivalent with the receiver (<paramref name="dialogParent"/>) as its parent, with the given <paramref name="text"/>,
		/// and with the given <paramref name="caption"/> and <paramref name="icon"/> if they're specified.
		/// </summary>
		/// <returns><see langword="true"/> iff "Yes"/"OK" was chosen</returns>
		public static bool ModalMessageBox2(
			this IDialogParent dialogParent,
			string text,
			string? caption = null,
			EMsgBoxIcon? icon = null,
			bool useOKCancel = false)
				=> dialogParent.DialogController.ShowMessageBox2(owner: dialogParent, text: text, caption: caption, icon: icon, useOKCancel: useOKCancel);

		/// <summary>
		/// Creates and shows a <c>System.Windows.Forms.MessageBox</c> or equivalent with the receiver (<paramref name="dialogParent"/>) as its parent, with the given <paramref name="text"/>,
		/// and with the given <paramref name="caption"/> and <paramref name="icon"/> if they're specified.
		/// </summary>
		/// <returns><see langword="true"/> if "Yes" was chosen, <see langword="false"/> if "No" was chosen, or <see langword="null"/> if "Cancel" was chosen</returns>
		public static bool? ModalMessageBox3(
			this IDialogParent dialogParent,
			string text,
			string? caption = null,
			EMsgBoxIcon? icon = null)
				=> dialogParent.DialogController.ShowMessageBox3(owner: dialogParent, text: text, caption: caption, icon: icon);

		/// <summary>Creates and shows a <c>System.Windows.Forms.OpenFileDialog</c> or equivalent with the receiver (<paramref name="dialogParent"/>) as its parent</summary>
		/// <param name="discardCWDChange"><c>OpenFileDialog.RestoreDirectory</c> (isn't this useless when specifying <paramref name="initDir"/>? keeping it for backcompat)</param>
		/// <param name="filter"><c>OpenFileDialog.Filter</c></param>
		/// <param name="initDir"><c>OpenFileDialog.InitialDirectory</c>; initial browse location</param>
		/// <param name="initFileName"><c>OpenFileDialog.FileName</c>; pre-selected file (overrides <paramref name="initDir"/>?)</param>
		/// <returns>filename of selected file, or <see langword="null"/> iff cancelled</returns>
		public static string? ShowFileOpenDialog(
			this IDialogParent dialogParent,
			string initDir,
			bool discardCWDChange = false,
			FilesystemFilterSet? filter = null,
			string? initFileName = null)
				=> dialogParent.ShowFileMultiOpenDialog(
					discardCWDChange: discardCWDChange,
					filterStr: filter?.ToString(),
					initDir: initDir,
					initFileName: initFileName)?[0];

		/// <summary>Creates and shows a <c>System.Windows.Forms.OpenFileDialog</c> or equivalent with the receiver (<paramref name="dialogParent"/>) as its parent</summary>
		/// <param name="filter"><c>OpenFileDialog.Filter</c></param>
		/// <param name="filterIndex"><c>OpenFileDialog.FilterIndex</c>; initially selected entry in <paramref name="filter"/></param>
		/// <param name="initDir"><c>OpenFileDialog.InitialDirectory</c>; initial browse location</param>
		/// <param name="windowTitle"><c>OpenFileDialog.Title</c></param>
		/// <returns>filename of selected file, or <see langword="null"/> iff cancelled</returns>
		/// <remarks>only used from MainForm, but don't move it there</remarks>
		public static string? ShowFileOpenDialog(
			this IDialogParent dialogParent,
			FilesystemFilterSet? filter,
			ref int filterIndex,
			string initDir,
			string? windowTitle = null)
				=> dialogParent.DialogController.ShowFileMultiOpenDialog(
					dialogParent: dialogParent,
					filterStr: filter?.ToString(),
					filterIndex: ref filterIndex,
					initDir: initDir,
					windowTitle: windowTitle)?[0];

		/// <summary>Creates and shows a <c>System.Windows.Forms.OpenFileDialog</c> or equivalent with the receiver (<paramref name="dialogParent"/>) as its parent</summary>
		/// <param name="filterStr"><c>OpenFileDialog.Filter</c></param>
		/// <param name="initDir"><c>OpenFileDialog.InitialDirectory</c>; initial browse location</param>
		/// <param name="initFileName"><c>OpenFileDialog.FileName</c>; pre-selected file (overrides <paramref name="initDir"/>?)</param>
		/// <returns>filename of selected file, or <see langword="null"/> iff cancelled</returns>
		/// <remarks>only used from Lua, but don't move it there</remarks>
		public static string? ShowFileOpenDialog(
			this IDialogParent dialogParent,
			string? filterStr,
			string initDir,
			string? initFileName = null)
				=> dialogParent.ShowFileMultiOpenDialog(
					filterStr: filterStr,
					initDir: initDir,
					initFileName: initFileName)?[0];

		/// <summary>Creates and shows a <c>System.Windows.Forms.SaveFileDialog</c> or equivalent with the receiver (<paramref name="dialogParent"/>) as its parent</summary>
		/// <param name="discardCWDChange"><c>SaveFileDialog.RestoreDirectory</c> (renamed for clarity without inverting value; isn't this useless when specifying <paramref name="initDir"/>? keeping it for backcompat)</param>
		/// <param name="fileExt"><c>SaveFileDialog.DefaultExt</c>; used only when the user's chosen filename doesn't have an extension (omit leading '.')</param>
		/// <param name="filter"><c>SaveFileDialog.Filter</c></param>
		/// <param name="initDir"><c>SaveFileDialog.InitialDirectory</c>; initial browse location</param>
		/// <param name="initFileName"><c>SaveFileDialog.FileName</c>; pre-selected file (overrides <paramref name="initDir"/>?)</param>
		/// <param name="muteOverwriteWarning"><c>SaveFileDialog.OverwritePrompt</c> (renamed for clarity with inverted value)</param>
		/// <returns>filename of selected destination, or <see langword="null"/> iff cancelled</returns>
		public static string? ShowFileSaveDialog(
			this IDialogParent dialogParent,
			string initDir,
			bool discardCWDChange = false,
			string? fileExt = null,
			FilesystemFilterSet? filter = null,
			string? initFileName = null,
			bool muteOverwriteWarning = false)
				=> dialogParent.DialogController.ShowFileSaveDialog(
					dialogParent: dialogParent,
					discardCWDChange: discardCWDChange,
					fileExt: fileExt,
					filterStr: filter?.ToString(),
					initDir: initDir,
					initFileName: initFileName,
					muteOverwriteWarning: muteOverwriteWarning);

		/// <summary>
		/// Creates and shows a <c>System.Windows.Forms.MessageBox</c> or equivalent without a parent, with the given <paramref name="text"/>,
		/// and with the given <paramref name="caption"/> and <paramref name="icon"/> if they're specified.
		/// </summary>
		public static void ShowMessageBox(
			this IDialogController dialogController,
			string text,
			string? caption = null,
			EMsgBoxIcon? icon = null)
				=> dialogController.ShowMessageBox(owner: null, text: text, caption: caption, icon: icon);

		/// <summary>
		/// Creates and shows a <c>System.Windows.Forms.MessageBox</c> or equivalent without a parent, with the given <paramref name="text"/>,
		/// and with the given <paramref name="caption"/> and <paramref name="icon"/> if they're specified.
		/// </summary>
		/// <returns><see langword="true"/> iff "Yes"/"OK" was chosen</returns>
		public static bool ShowMessageBox2(
			this IDialogController dialogController,
			string text,
			string? caption = null,
			EMsgBoxIcon? icon = null,
			bool useOKCancel = false)
				=> dialogController.ShowMessageBox2(owner: null, text: text, caption: caption, icon: icon, useOKCancel: useOKCancel);

		/// <summary>
		/// Creates and shows a <c>System.Windows.Forms.MessageBox</c> or equivalent without a parent, with the given <paramref name="text"/>,
		/// and with the given <paramref name="caption"/> and <paramref name="icon"/> if they're specified.
		/// </summary>
		/// <returns><see langword="true"/> if "Yes" was chosen, <see langword="false"/> if "No" was chosen, or <see langword="null"/> if "Cancel" was chosen</returns>
		public static bool? ShowMessageBox3(
			this IDialogController dialogController,
			string text,
			string? caption = null,
			EMsgBoxIcon? icon = null)
				=> dialogController.ShowMessageBox3(owner: null, text: text, caption: caption, icon: icon);

		/// <summary>Creates and shows a <c>System.Windows.Forms.OpenFileDialog</c> or equivalent with the receiver (<paramref name="dialogParent"/>) as its parent</summary>
		/// <param name="discardCWDChange"><c>OpenFileDialog.RestoreDirectory</c> (renamed for clarity without inverting value; isn't this useless when specifying <paramref name="initDir"/>? keeping it for backcompat)</param>
		/// <param name="filterStr"><c>OpenFileDialog.Filter</c> (call <c>ToString</c> on a <see cref="FilesystemFilter"/>/<see cref="FilesystemFilterSet"/>)</param>
		/// <param name="initDir"><c>OpenFileDialog.InitialDirectory</c>; initial browse location</param>
		/// <param name="initFileName"><c>OpenFileDialog.FileName</c>; pre-selected file (overrides <paramref name="initDir"/>?)</param>
		/// <param name="maySelectMultiple"><c>OpenFileDialog.Multiselect</c></param>
		/// <returns>filenames of selected files, or <see langword="null"/> iff cancelled</returns>
		private static IReadOnlyList<string>? ShowFileMultiOpenDialog(
			this IDialogParent dialogParent,
			string? filterStr,
			string initDir,
			bool discardCWDChange = false,
			string? initFileName = null,
			bool maySelectMultiple = false)
		{
			var filterIndex = 1; // you'd think the default would be 0, but it's not
			return dialogParent.DialogController.ShowFileMultiOpenDialog(
				dialogParent: dialogParent,
				discardCWDChange: discardCWDChange,
				filterStr: filterStr,
				filterIndex: ref filterIndex,
				initDir: initDir,
				initFileName: initFileName,
				maySelectMultiple: maySelectMultiple);
		}

		/// <summary>Creates and shows a <c>System.Windows.Forms.OpenFileDialog</c> or equivalent with the receiver (<paramref name="dialogParent"/>) as its parent</summary>
		/// <param name="discardCWDChange"><c>OpenFileDialog.RestoreDirectory</c> (isn't this useless when specifying <paramref name="initDir"/>? keeping it for backcompat)</param>
		/// <param name="filter"><c>OpenFileDialog.Filter</c></param>
		/// <param name="initDir"><c>OpenFileDialog.InitialDirectory</c>; initial browse location</param>
		/// <returns>filenames of selected files, or <see langword="null"/> iff cancelled</returns>
		public static IReadOnlyList<string>? ShowFileMultiOpenDialog(
			this IDialogParent dialogParent,
			string initDir,
			bool discardCWDChange = false,
			FilesystemFilterSet? filter = null)
				=> dialogParent.ShowFileMultiOpenDialog(
					discardCWDChange: discardCWDChange,
					filterStr: filter?.ToString(),
					initDir: initDir,
					initFileName: null,
					maySelectMultiple: true);
	}
}
