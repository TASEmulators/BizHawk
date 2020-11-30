#nullable enable

using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public interface IDialogParent
	{
		IDialogController DialogController { get; }

		IWin32Window SelfAsHandle { get; }
	}
}
