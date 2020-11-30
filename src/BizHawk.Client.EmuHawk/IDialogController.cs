#nullable enable

using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public interface IDialogController
	{
		DialogResult ShowDialogAsChild(Form dialog);

		void StartSound();

		void StopSound();
	}
}
