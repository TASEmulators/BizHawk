using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BizHawk.Common
{
	public static class Win32Imports
	{
		public const int MAX_PATH = 260;

		public delegate int BFFCALLBACK(IntPtr hwnd, uint uMsg, IntPtr lParam, IntPtr lpData);

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct BROWSEINFO
		{
			public IntPtr hwndOwner;
			public IntPtr pidlRoot;
			public IntPtr pszDisplayName;
			[MarshalAs(UnmanagedType.LPTStr)] public string lpszTitle;
			public FLAGS ulFlags;
			[MarshalAs(UnmanagedType.FunctionPtr)] public BFFCALLBACK lpfn;
			public IntPtr lParam;
			public int iImage;

			[Flags]
			public enum FLAGS
			{
				/// <remarks>BIF_RETURNONLYFSDIRS</remarks>
				RestrictToFilesystem = 0x0001,
				/// <remarks>BIF_DONTGOBELOWDOMAIN</remarks>
				RestrictToDomain = 0x0002,
				/// <remarks>BIF_RETURNFSANCESTORS</remarks>
				RestrictToSubfolders = 0x0008,
				/// <remarks>BIF_EDITBOX</remarks>
				ShowTextBox = 0x0010,
				/// <remarks>BIF_VALIDATE</remarks>
				ValidateSelection = 0x0020,
				/// <remarks>BIF_NEWDIALOGSTYLE</remarks>
				NewDialogStyle = 0x0040,
				/// <remarks>BIF_BROWSEFORCOMPUTER</remarks>
				BrowseForComputer = 0x1000,
				/// <remarks>BIF_BROWSEFORPRINTER</remarks>
				BrowseForPrinter = 0x2000,
				/// <remarks>BIF_BROWSEINCLUDEFILES</remarks>
				BrowseForEverything = 0x4000
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct HDITEM
		{
			public Mask mask;
			public int cxy;
			[MarshalAs(UnmanagedType.LPTStr)] public string pszText;
			public IntPtr hbm;
			public int cchTextMax;
			public Format fmt;
			public IntPtr lParam;

			// _WIN32_IE >= 0x0300
			public int iImage;
			public int iOrder;

			// _WIN32_IE >= 0x0500
			public uint type;
			public IntPtr pvFilter;

			// _WIN32_WINNT >= 0x0600
			public uint state;

			[Flags]
			public enum Mask
			{
				Format = 0x4
			}

			[Flags]
			public enum Format
			{
				SortDown = 0x200,
				SortUp = 0x400
			}
		}

		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("00000002-0000-0000-C000-000000000046")]
		public interface IMalloc
		{
			[PreserveSig] IntPtr Alloc([In] int cb);
			[PreserveSig] IntPtr Realloc([In] IntPtr pv, [In] int cb);
			[PreserveSig] void Free([In] IntPtr pv);
			[PreserveSig] int GetSize([In] IntPtr pv);
			[PreserveSig] int DidAlloc(IntPtr pv);
			[PreserveSig] void HeapMinimize();
		}

		[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint _control87(uint @new, uint mask);

		[DllImport("kernel32.dll", EntryPoint = "DeleteFileW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

		[DllImport("user32.dll")]
		public static extern IntPtr GetActiveWindow();

		[DllImport("kernel32.dll", SetLastError=true)]
		public static extern unsafe uint GetCurrentDirectoryW(uint nBufferLength, byte* pBuffer);

		[DllImport("kernel32", SetLastError = true, EntryPoint = "GetProcAddress")]
		public static extern IntPtr GetProcAddressOrdinal(IntPtr hModule, IntPtr procName);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr GetProcessHeap();

		[DllImport("kernel32.dll", SetLastError = false)]
		public static extern IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, int dwBytes);

		/// <remarks>used in <c>#if false</c> code in <c>AviWriter.CodecToken.DeallocateAVICOMPRESSOPTIONS</c>, don't delete it</remarks>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

		[DllImport("user32")]
		public static extern bool HideCaret(IntPtr hWnd);

		[DllImport("kernel32.dll")]
		public static extern bool IsDebuggerPresent();

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern uint MapVirtualKey(uint uCode, uint uMapType);

		[DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
		public static extern IntPtr MemSet(IntPtr dest, int c, uint count);

		[DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
		public static extern bool PathRelativePathTo([Out] StringBuilder pszPath, [In] string pszFrom, [In] FileAttributes dwAttrFrom, [In] string pszTo, [In] FileAttributes dwAttrTo);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, ref HDITEM lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern unsafe bool SetCurrentDirectoryW(byte* lpPathName);

		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SHBrowseForFolder(ref BROWSEINFO bi);

		[DllImport("shell32.dll")]
		public static extern int SHGetMalloc(out IMalloc ppMalloc);

		[DllImport("shell32.dll")]
		public static extern int SHGetPathFromIDList(IntPtr pidl, StringBuilder Path);

		[DllImport("shell32.dll")]
		public static extern int SHGetSpecialFolderLocation(IntPtr hwndOwner, int nFolder, out IntPtr ppidl);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool SystemParametersInfo(int uAction, int uParam, ref int lpvParam, int flags);

		[DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
		public static extern uint timeBeginPeriod(uint uMilliseconds);
	}
}
