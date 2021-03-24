#nullable enable

namespace BizHawk.Client.Common
{
	/// <remarks>In a WinForms app, inheritors must also inherit <c>System.Windows.Forms.IWin32Window</c>.</remarks>
	public interface IDialogParent
	{
		IDialogController DialogController { get; }
	}
}
