#nullable enable

using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class DialogControllerWinFormsExtensions
	{
		public static IWin32Window AsWinFormsHandle(this IDialogParent dialogParent) => (IWin32Window) dialogParent;

		public static DialogResult ShowDialogAsChild(this IDialogParent dialogParent, CommonDialog dialog)
			=> dialog.ShowDialog(dialogParent.AsWinFormsHandle());

		public static DialogResult ShowDialogAsChild(this IDialogParent dialogParent, Form dialog)
			=> dialog.ShowDialog(dialogParent.AsWinFormsHandle());

		public static DialogResult ShowDialogWithTempMute(this IDialogParent dialogParent, CommonDialog dialog)
			=> dialogParent.DialogController.DoWithTempMute(() => dialog.ShowDialog(dialogParent.AsWinFormsHandle()));

		public static DialogResult ShowDialogWithTempMute(this IDialogParent dialogParent, Form dialog)
			=> dialogParent.DialogController.DoWithTempMute(() => dialog.ShowDialog(dialogParent.AsWinFormsHandle()));

		public static DialogResult ShowDialogWithTempMute(this IDialogParent dialogParent, FolderBrowserEx dialog)
			=> dialogParent.DialogController.DoWithTempMute(() => dialog.ShowDialog(dialogParent.AsWinFormsHandle()));

		public static DialogResult ShowMessageBox(
			this IDialogController mainForm,
			IDialogParent? owner,
			string text,
			string? caption,
			MessageBoxButtons buttons,
			EMsgBoxIcon? icon)
				=> MessageBox.Show(
					owner?.AsWinFormsHandle(),
					text,
					caption ?? string.Empty,
					buttons,
					icon switch
					{
						null => MessageBoxIcon.None,
						EMsgBoxIcon.None => MessageBoxIcon.None,
						EMsgBoxIcon.Error => MessageBoxIcon.Error,
						EMsgBoxIcon.Question => MessageBoxIcon.Question,
						EMsgBoxIcon.Warning => MessageBoxIcon.Warning,
						EMsgBoxIcon.Info => MessageBoxIcon.Information,
						_ => throw new InvalidOperationException()
					});
	}
}
