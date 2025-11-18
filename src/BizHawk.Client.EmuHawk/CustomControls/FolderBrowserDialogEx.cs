using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using BizHawk.Common;

using static BizHawk.Common.Shell32Imports;

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
		private const BROWSEINFOW.FLAGS BrowseOptions = BROWSEINFOW.FLAGS.RestrictToFilesystem | BROWSEINFOW.FLAGS.RestrictToDomain |
			BROWSEINFOW.FLAGS.NewDialogStyle | BROWSEINFOW.FLAGS.ShowTextBox;

		public string Description = "Please select a folder below:";

		public string SelectedPath;

		/// <summary>Shows the folder browser dialog box with the specified owner window.</summary>
		public DialogResult ShowDialog(IWin32Window owner = null)
		{
			const int startLocation = 0; // = Desktop CSIDL
			int Callback(IntPtr hwnd, uint uMsg, IntPtr lParam, IntPtr lpData)
			{
				if (uMsg == BFFM_INITIALIZED)
				{
					var str = Marshal.StringToHGlobalUni(SelectedPath);
					try
					{
						WmImports.SendMessageW(hwnd, BFFM_SETSELECTIONW, new(1), str);
					}
					finally
					{
						Marshal.FreeHGlobal(str);
					}
				}

				return 0;
			}

			var hWndOwner = owner?.Handle ?? WmImports.GetActiveWindow();
			_ = SHGetSpecialFolderLocation(hWndOwner, startLocation, out var pidlRoot);
			if (pidlRoot == IntPtr.Zero)
			{
				return DialogResult.Cancel;
			}

			var pidlRet = IntPtr.Zero;
			var pszDisplayName = IntPtr.Zero;
			try
			{
				var browseOptions = BrowseOptions;
				if (ApartmentState.MTA == Application.OleRequired())
				{
					browseOptions &= ~BROWSEINFOW.FLAGS.NewDialogStyle;
				}

				const int BUF_SIZE_BYTES = (int) Win32Imports.MAX_PATH * sizeof(char);
				pszDisplayName = Marshal.AllocCoTaskMem(BUF_SIZE_BYTES);
				var bi = new BROWSEINFOW
				{
					hwndOwner = hWndOwner,
					pidlRoot = pidlRoot,
					pszDisplayName = pszDisplayName,
					lpszTitle = Description,
					ulFlags = browseOptions,
					lpfn = Callback,
				};

				pidlRet = SHBrowseForFolderW(ref bi);
				if (pidlRet == IntPtr.Zero)
				{
					return DialogResult.Cancel; // user clicked Cancel
				}

				var path = new char[Win32Imports.MAX_PATH];
				if (SHGetPathFromIDListW(pidlRet, path) == 0)
				{
					return DialogResult.Cancel;
				}

				SelectedPath = new string(path).TrimEnd('\0');
			}
			finally
			{
				Marshal.FreeCoTaskMem(pidlRoot);
				Marshal.FreeCoTaskMem(pidlRet);
				Marshal.FreeCoTaskMem(pszDisplayName);
			}

			return DialogResult.OK;
		}
	}
}
