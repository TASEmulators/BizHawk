using System.Runtime.InteropServices;
using System.Text;
using System;
using System.Windows.Forms;
using System.ComponentModel;

//I believe this code is from http://support.microsoft.com/kb/306285
//The license is assumed to be effectively public domain.
//I saw a version of it with at least one bug fixed at https://github.com/slavat/MailSystem.NET/blob/master/Queuing%20System/ActiveQLibrary/CustomControl/FolderBrowser.cs

namespace BizHawk.Client.EmuHawk
{

	/// <summary>
	/// Component wrapping access to the Browse For Folder common dialog box.
	/// Call the ShowDialog() method to bring the dialog box up.
	/// </summary>
	public sealed class FolderBrowserEx : Component
	{
		private const int MAX_PATH = 260;

		// Root node of the tree view.
		private FolderID startLocation = FolderID.Desktop;

		// Browse info options.
		private int publicOptions = (int) Win32API.Shell32.BffStyles.RestrictToFilesystem |
		                            (int) Win32API.Shell32.BffStyles.RestrictToDomain;

		private const int privateOptions = (int) (Win32API.Shell32.BffStyles.NewDialogStyle | Win32API.Shell32.BffStyles.ShowTextBox);

		// Description text to show.
		public string Description = "Please select a folder below:";

		/// <summary>
		/// Enum of CSIDLs identifying standard shell folders.
		/// </summary>
		public enum FolderID
		{
			Desktop = 0x0000,
			Printers = 0x0004,
			MyDocuments = 0x0005,
			Favorites = 0x0006,
			Recent = 0x0008,
			SendTo = 0x0009,
			StartMenu = 0x000b,
			MyComputer = 0x0011,
			NetworkNeighborhood = 0x0012,
			Templates = 0x0015,
			MyPictures = 0x0027,
			NetAndDialUpConnections = 0x0031,
		}

		/// <summary>
		/// Helper function that returns the IMalloc interface used by the shell.
		/// </summary>
		private static Win32API.IMalloc GetShMalloc()
		{
			Win32API.Shell32.SHGetMalloc(out var malloc);
			return malloc;
		}

		/// <summary>
		/// Shows the folder browser dialog box.
		/// </summary>
		public DialogResult ShowDialog()
		{
			return ShowDialog(null);
		}

		private int Callback(IntPtr hwnd, uint uMsg, IntPtr lParam, IntPtr lpData)
		{
			switch (uMsg)
			{
				case 1:
					IntPtr str = Marshal.StringToHGlobalUni(SelectedPath);
					Win32.SendMessage(hwnd, 0x400 + 103, (IntPtr)1, str);
					Marshal.FreeHGlobal(str);
					break;

			}

			return 0;
		}


		/// <summary>
		/// Shows the folder browser dialog box with the specified owner window.
		/// </summary>
		public DialogResult ShowDialog(IWin32Window owner)
		{
			IntPtr pidlRoot = IntPtr.Zero;

			// Get/find an owner HWND for this dialog.

			var hWndOwner = owner?.Handle ?? Win32API.GetActiveWindow();

			// Get the IDL for the specific startLocation.
			Win32API.Shell32.SHGetSpecialFolderLocation(hWndOwner, (int) startLocation, out pidlRoot);

			if (pidlRoot == IntPtr.Zero)
			{
				return DialogResult.Cancel;
			}

			int mergedOptions = publicOptions | privateOptions;

			if ((mergedOptions & (int) Win32API.Shell32.BffStyles.NewDialogStyle) != 0)
			{
				if (System.Threading.ApartmentState.MTA == Application.OleRequired())
				{
					mergedOptions = mergedOptions & (~(int) Win32API.Shell32.BffStyles.NewDialogStyle);
				}
			}

			IntPtr pidlRet = IntPtr.Zero;

			try
			{
				// Construct a BROWSEINFO.
				Win32API.Shell32.BROWSEINFO bi = new Win32API.Shell32.BROWSEINFO();
				IntPtr buffer = Marshal.AllocHGlobal(MAX_PATH);

				bi.pidlRoot = pidlRoot;
				bi.hwndOwner = hWndOwner;
				bi.pszDisplayName = buffer;
				bi.lpszTitle = Description;
				bi.ulFlags = mergedOptions;
				bi.lpfn = Callback;
				// The rest of the fields are initialized to zero by the constructor.
				// bi.lParam = IntPtr.Zero;    bi.iImage = 0;

				// Show the dialog.
				pidlRet = Win32API.Shell32.SHBrowseForFolder(ref bi);

				// Free the buffer you've allocated on the global heap.
				Marshal.FreeHGlobal(buffer);

				if (pidlRet == IntPtr.Zero)
				{
					// User clicked Cancel.
					return DialogResult.Cancel;
				}

				// Then retrieve the path from the IDList.
				var sb = new StringBuilder(MAX_PATH);
				if (0 == Win32API.Shell32.SHGetPathFromIDList(pidlRet, sb))
				{
					return DialogResult.Cancel;
				}

				// Convert to a string.
				SelectedPath = sb.ToString();
			}
			finally
			{
				Win32API.IMalloc malloc = GetShMalloc();
				malloc.Free(pidlRoot);

				if (pidlRet != IntPtr.Zero)
				{
					malloc.Free(pidlRet);
				}
			}

			return DialogResult.OK;
		}

		public string SelectedPath;
	}

	internal class Win32API
	{
		// C# representation of the IMalloc interface.
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
		 Guid("00000002-0000-0000-C000-000000000046")]
		public interface IMalloc
		{
			[PreserveSig]
			IntPtr Alloc([In] int cb);

			[PreserveSig]
			IntPtr Realloc([In] IntPtr pv, [In] int cb);

			[PreserveSig]
			void Free([In] IntPtr pv);

			[PreserveSig]
			int GetSize([In] IntPtr pv);

			[PreserveSig]
			int DidAlloc(IntPtr pv);

			[PreserveSig]
			void HeapMinimize();
		}

		[DllImport("User32.DLL")]
		public static extern IntPtr GetActiveWindow();

		public class Shell32
		{
			// Styles used in the BROWSEINFO.ulFlags field.
			[Flags]
			public enum BffStyles
			{
				RestrictToFilesystem = 0x0001, // BIF_RETURNONLYFSDIRS
				RestrictToDomain = 0x0002, // BIF_DONTGOBELOWDOMAIN
				RestrictToSubfolders = 0x0008, // BIF_RETURNFSANCESTORS
				ShowTextBox = 0x0010, // BIF_EDITBOX
				ValidateSelection = 0x0020, // BIF_VALIDATE
				NewDialogStyle = 0x0040, // BIF_NEWDIALOGSTYLE
				BrowseForComputer = 0x1000, // BIF_BROWSEFORCOMPUTER
				BrowseForPrinter = 0x2000, // BIF_BROWSEFORPRINTER
				BrowseForEverything = 0x4000, // BIF_BROWSEINCLUDEFILES
			}

			// Delegate type used in BROWSEINFO.lpfn field.
			public delegate int BFFCALLBACK(IntPtr hwnd, uint uMsg, IntPtr lParam, IntPtr lpData);

			[StructLayout(LayoutKind.Sequential, Pack = 8)]
			public struct BROWSEINFO
			{
				public IntPtr hwndOwner;
				public IntPtr pidlRoot;
				public IntPtr pszDisplayName;
				[MarshalAs(UnmanagedType.LPTStr)] public string lpszTitle;
				public int ulFlags;
				[MarshalAs(UnmanagedType.FunctionPtr)] public BFFCALLBACK lpfn;
				public IntPtr lParam;
				public int iImage;
			}

			[DllImport("Shell32.DLL")]
			public static extern int SHGetMalloc(out IMalloc ppMalloc);

			[DllImport("Shell32.DLL")]
			public static extern int SHGetSpecialFolderLocation(
				IntPtr hwndOwner, int nFolder, out IntPtr ppidl);

			[DllImport("Shell32.DLL")]
			public static extern int SHGetPathFromIDList(
				IntPtr pidl, StringBuilder Path);

			[DllImport("Shell32.DLL", CharSet = CharSet.Auto)]
			public static extern IntPtr SHBrowseForFolder(ref BROWSEINFO bi);
		}
	}
}