using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using BizHawk.Common;

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
		/// <remarks>is this supposed to be public? we're obviously not using it at callsites at the moment --yoshi</remarks>
		private Win32Imports.BROWSEINFO.FLAGS publicOptions = Win32Imports.BROWSEINFO.FLAGS.RestrictToFilesystem | Win32Imports.BROWSEINFO.FLAGS.RestrictToDomain;

		public string Description = "Please select a folder below:";

		public string SelectedPath;

		/// <summary>Shows the folder browser dialog box with the specified owner window.</summary>
		public DialogResult ShowDialog(IWin32Window owner = null)
		{
			const Win32Imports.BROWSEINFO.FLAGS privateOptions = Win32Imports.BROWSEINFO.FLAGS.NewDialogStyle | Win32Imports.BROWSEINFO.FLAGS.ShowTextBox;
			const int startLocation = 0; // = Desktop CSIDL
			int Callback(IntPtr hwnd, uint uMsg, IntPtr lParam, IntPtr lpData)
			{
				if (uMsg == 1)
				{
					var str = Marshal.StringToHGlobalUni(SelectedPath);
					Win32Imports.SendMessage(hwnd, 0x400 + 103, (IntPtr) 1, str);
					Marshal.FreeHGlobal(str);
				}
				return 0;
			}

			var hWndOwner = owner?.Handle ?? Win32Imports.GetActiveWindow();
			Win32Imports.SHGetSpecialFolderLocation(hWndOwner, startLocation, out var pidlRoot);
			if (pidlRoot == IntPtr.Zero) return DialogResult.Cancel;
			var mergedOptions = publicOptions | privateOptions;
			if ((mergedOptions & Win32Imports.BROWSEINFO.FLAGS.NewDialogStyle) != 0 && ApartmentState.MTA == Application.OleRequired())
			{
				mergedOptions &= ~Win32Imports.BROWSEINFO.FLAGS.NewDialogStyle;
			}

			IntPtr pidlRet = default;
			try
			{
				var buffer = Marshal.AllocHGlobal(Win32Imports.MAX_PATH);
				var bi = new Win32Imports.BROWSEINFO
				{
					hwndOwner = hWndOwner,
					pidlRoot = pidlRoot,
					pszDisplayName = buffer,
					lpszTitle = Description,
					ulFlags = mergedOptions,
					lpfn = Callback
				};
				pidlRet = Win32Imports.SHBrowseForFolder(ref bi);
				Marshal.FreeHGlobal(buffer);
				if (pidlRet == IntPtr.Zero) return DialogResult.Cancel; // user clicked Cancel
				var sb = new StringBuilder(Win32Imports.MAX_PATH);
				if (Win32Imports.SHGetPathFromIDList(pidlRet, sb) == 0) return DialogResult.Cancel;
				SelectedPath = sb.ToString();
			}
			finally
			{
				Win32Imports.SHGetMalloc(out var malloc);
				malloc.Free(pidlRoot);
				if (pidlRet != IntPtr.Zero) malloc.Free(pidlRet);
			}
			return DialogResult.OK;
		}
	}
}
