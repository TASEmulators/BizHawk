#nullable disable

using System.Runtime.InteropServices;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnusedMember.Global

namespace BizHawk.Common
{
	public static class CommctrlImports
	{
		public const int LVM_FIRST = 0x1000;
		public const int LVM_GETHEADER = LVM_FIRST + 31;

		public const int HDM_FIRST = 0x1200;
		public const int HDM_GETITEMW = HDM_FIRST + 11;
		public const int HDM_SETITEMW = HDM_FIRST + 12;

		[StructLayout(LayoutKind.Sequential)]
		public struct HDITEMW
		{
			public Mask mask;
			public int cxy;
			[MarshalAs(UnmanagedType.LPWStr)]
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
			public enum Mask : uint
			{
				Format = 0x4
			}

			[Flags]
			public enum Format : int
			{
				SortDown = 0x200,
				SortUp = 0x400
			}
		}

		[DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		public static extern IntPtr SendMessageW(IntPtr hWnd, uint msg, IntPtr wParam, ref HDITEMW lParam);
	}
}