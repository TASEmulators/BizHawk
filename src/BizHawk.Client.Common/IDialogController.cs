#nullable enable

namespace BizHawk.Client.Common
{
	public interface IDialogController
	{
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
