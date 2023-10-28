#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Text;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnusedMember.Global

namespace BizHawk.Common
{
	public static class ShlobjImports
	{
		public const int BFFM_INITIALIZED = 1;
		public const int BFFM_SETSELECTIONW = 0x400 + 103;

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		public delegate int BFFCALLBACK(IntPtr hwnd, uint uMsg, IntPtr lParam, IntPtr lpData);

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct BROWSEINFO
		{
			public IntPtr hwndOwner;
			public IntPtr pidlRoot;
			public IntPtr pszDisplayName;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string lpszTitle;
			public FLAGS ulFlags;
			[MarshalAs(UnmanagedType.FunctionPtr)]
			public BFFCALLBACK lpfn;
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

		[Guid("00000002-0000-0000-C000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
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

		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SHBrowseForFolder(ref BROWSEINFO bi);

		[DllImport("shell32.dll")]
		public static extern int SHGetMalloc(out IMalloc ppMalloc);

		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		public static extern int SHGetPathFromIDList(IntPtr pidl, StringBuilder pszPath);

		[DllImport("shell32.dll")]
		public static extern int SHGetSpecialFolderLocation(IntPtr hwndOwner, int nFolder, out IntPtr ppidl);
	}
}
