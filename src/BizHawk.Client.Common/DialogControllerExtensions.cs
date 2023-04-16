#nullable enable

using System;

namespace BizHawk.Client.Common
{
	public static class DialogControllerExtensions
	{
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
	}
}
