#nullable disable

using System;
using System.Runtime.InteropServices;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnusedMember.Global

namespace BizHawk.Common
{
	public static class CommctrlImports
	{
		public const int LVM_GETHEADER = 4127;
		public const int HDM_GETITEM = 4619;
		public const int HDM_SETITEM = 4620;

		[StructLayout(LayoutKind.Sequential)]
		public struct HDITEM
		{
			public Mask mask;
			public int cxy;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string pszText;
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

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, ref HDITEM lParam);
	}
}