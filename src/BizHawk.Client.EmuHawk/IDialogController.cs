#nullable enable

using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public interface IDialogController
	{
		/// <summary>
		/// Creates a <see cref="MessageBox"/> with the given <paramref name="text"/>,
		/// and with the given <paramref name="owner"/>, <paramref name="caption"/>, <paramref name="buttons"/>, and <paramref name="icon"/> if they're specified.
		/// </summary>
		DialogResult ShowMessageBox(
			IDialogParent? owner,
			string text,
			string? caption = null,
			MessageBoxButtons? buttons = null,
			MessageBoxIcon? icon = null);

		void StartSound();

		void StopSound();
	}
}
