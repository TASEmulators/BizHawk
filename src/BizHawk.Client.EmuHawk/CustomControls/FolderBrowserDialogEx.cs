using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

using static Windows.Win32.Win32Imports;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Component wrapping access to the Browse For Folder common dialog box.
	/// Call the ShowDialog() method to bring the dialog box up.
	/// </summary>
	/// <remarks>
	/// I believe this code is from http://support.microsoft.com/kb/306285<br/>
	/// The license is assumed to be effectively public domain.<br/>
	/// I saw a version of it with at least one bug fixed at https://github.com/slavat/MailSystem.NET/blob/master/Queuing%20System/ActiveQLibrary/CustomControl/FolderBrowser.cs<br/>
	/// --zeromus
	/// </remarks>
	public sealed class FolderBrowserEx : Component
	{
		private const uint BrowseOptions = BIF_NEWDIALOGSTYLE | BIF_EDITBOX | BIF_DONTGOBELOWDOMAIN | BIF_RETURNONLYFSDIRS;

		public string Description = "Please select a folder below:";

		public string SelectedPath;

		/// <summary>Shows the folder browser dialog box with the specified owner window.</summary>
		public unsafe DialogResult ShowDialog(IWin32Window owner = null)
		{
			const int startLocation = 0; // = Desktop CSIDL
			int Callback(HWND hwnd, uint uMsg, LPARAM lParam, LPARAM lpData)
			{
				if (uMsg == BFFM_INITIALIZED)
				{
					var str = Marshal.StringToHGlobalUni(SelectedPath);
					try
					{
						WmImports.SendMessageW(hwnd, BFFM_SETSELECTIONW, new WPARAM(1), new LPARAM(str));
					}
					finally
					{
						Marshal.FreeHGlobal(str);
					}
				}

				return 0;
			}

			HWND hWndOwner = new(owner?.Handle ?? WmImports.GetActiveWindow());
			ITEMIDLIST* pidlRoot = null;
			_ = SHGetSpecialFolderLocation(hWndOwner, startLocation, &pidlRoot);
			if (pidlRoot is null) return DialogResult.Cancel;

			ITEMIDLIST* pidlRet = null;
			PWSTR pszDisplayName = new(null);
			try
			{
				var browseOptions = BrowseOptions;
				if (Application.OleRequired() is ApartmentState.MTA) browseOptions &= ~BIF_NEWDIALOGSTYLE;
				const int BUF_SIZE_BYTES = (int) Win32Imports.MAX_PATH * sizeof(char);
				pszDisplayName = new(Marshal.AllocCoTaskMem(BUF_SIZE_BYTES));
				fixed (char* lpszTitle = Description)
				{
					var bi = new BROWSEINFOW
					{
						hwndOwner = hWndOwner,
						pidlRoot = pidlRoot,
						pszDisplayName = pszDisplayName,
						lpszTitle = lpszTitle,
						ulFlags = browseOptions,
						lpfn = Callback,
					};
					pidlRet = SHBrowseForFolderW(in bi);
				}
				if (pidlRet is null) return DialogResult.Cancel; // user clicked Cancel

				var path = new char[Win32Imports.MAX_PATH];
				if (!SHGetPathFromIDListW(*pidlRet, path)) return DialogResult.Cancel;

				SelectedPath = new string(path).TrimEnd('\0');
			}
			finally
			{
				Marshal.FreeCoTaskMem(unchecked((IntPtr) pidlRoot));
				Marshal.FreeCoTaskMem(unchecked((IntPtr) pidlRet));
				Marshal.FreeCoTaskMem(unchecked((IntPtr) pszDisplayName.Value));
			}

			return DialogResult.OK;
		}
	}
}
