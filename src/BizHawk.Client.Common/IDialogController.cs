#nullable enable

using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IDialogController
	{
		void AddOnScreenMessage(string message, int? duration = null);

		/// <summary>Creates and shows a <c>System.Windows.Forms.OpenFileDialog</c> or equivalent</summary>
		/// <param name="dialogParent">parent window</param>
		/// <param name="discardCWDChange"><c>OpenFileDialog.RestoreDirectory</c> (renamed for clarity without inverting value; isn't this useless when specifying <paramref name="initDir"/>? keeping it for backcompat)</param>
		/// <param name="filterStr"><c>OpenFileDialog.Filter</c> (call <c>ToString</c> on a <see cref="FilesystemFilter"/>/<see cref="FilesystemFilterSet"/>)</param>
		/// <param name="filterIndex"><c>OpenFileDialog.FilterIndex</c>; initially selected entry in <paramref name="filterStr"/></param>
		/// <param name="initDir"><c>OpenFileDialog.InitialDirectory</c>; initial browse location</param>
		/// <param name="initFileName"><c>OpenFileDialog.FileName</c>; pre-selected file (overrides <paramref name="initDir"/>?)</param>
		/// <param name="maySelectMultiple"><c>OpenFileDialog.Multiselect</c></param>
		/// <param name="windowTitle"><c>OpenFileDialog.Title</c></param>
		/// <returns>filenames of selected files, or <see langword="null"/> iff cancelled</returns>
		IReadOnlyList<string>? ShowFileMultiOpenDialog(
			IDialogParent dialogParent,
			string? filterStr,
			ref int filterIndex,
			string initDir,
			bool discardCWDChange = false,
			string? initFileName = null,
			bool maySelectMultiple = false,
			string? windowTitle = null);

		/// <summary>Creates and shows a <c>System.Windows.Forms.SaveFileDialog</c> or equivalent</summary>
		/// <param name="dialogParent">parent window</param>
		/// <param name="discardCWDChange"><c>SaveFileDialog.RestoreDirectory</c> (renamed for clarity without inverting value; isn't this useless when specifying <paramref name="initDir"/>? keeping it for backcompat)</param>
		/// <param name="fileExt"><c>SaveFileDialog.DefaultExt</c>; used only when the user's chosen filename doesn't have an extension (omit leading '.')</param>
		/// <param name="filterStr"><c>SaveFileDialog.Filter</c> (call <c>ToString</c> on a <see cref="FilesystemFilter"/>/<see cref="FilesystemFilterSet"/>)</param>
		/// <param name="initDir"><c>SaveFileDialog.InitialDirectory</c>; initial browse location</param>
		/// <param name="initFileName"><c>SaveFileDialog.FileName</c>; pre-selected file (overrides <paramref name="initDir"/>?)</param>
		/// <param name="muteOverwriteWarning"><c>SaveFileDialog.OverwritePrompt</c> (renamed for clarity with inverted value)</param>
		/// <returns>filename of selected destination, or <see langword="null"/> iff cancelled</returns>
		string? ShowFileSaveDialog(
			IDialogParent dialogParent,
			bool discardCWDChange,
			string? fileExt,
			string? filterStr,
			string initDir,
			string? initFileName,
			bool muteOverwriteWarning);

		/// <summary>
		/// Creates and shows a <c>System.Windows.Forms.MessageBox</c> or equivalent with the given <paramref name="text"/>,
		/// and with the given <paramref name="owner"/>, <paramref name="caption"/>, and <paramref name="icon"/> if they're specified.
		/// </summary>
		void ShowMessageBox(
			IDialogParent? owner,
			string text,
			string? caption = null,
			EMsgBoxIcon? icon = null);

		/// <summary>
		/// Creates and shows a <c>System.Windows.Forms.MessageBox</c> or equivalent with the given <paramref name="text"/>,
		/// and with the given <paramref name="owner"/>, <paramref name="caption"/>, and <paramref name="icon"/> if they're specified.
		/// </summary>
		/// <returns><see langword="true"/> iff "Yes"/"OK" was chosen</returns>
		bool ShowMessageBox2(
			IDialogParent? owner,
			string text,
			string? caption = null,
			EMsgBoxIcon? icon = null,
			bool useOKCancel = false);

		/// <summary>
		/// Creates and shows a <c>System.Windows.Forms.MessageBox</c> or equivalent with the given <paramref name="text"/>,
		/// and with the given <paramref name="owner"/>, <paramref name="caption"/>, and <paramref name="icon"/> if they're specified.
		/// </summary>
		/// <returns><see langword="true"/> if "Yes" was chosen, <see langword="false"/> if "No" was chosen, or <see langword="null"/> if "Cancel" was chosen</returns>
		bool? ShowMessageBox3(
			IDialogParent? owner,
			string text,
			string? caption = null,
			EMsgBoxIcon? icon = null);

		void StartSound();

		void StopSound();
	}
}
