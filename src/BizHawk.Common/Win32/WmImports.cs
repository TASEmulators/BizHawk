using System.Runtime.InteropServices;

using Windows.Win32.UI.WindowsAndMessaging;

namespace BizHawk.Common
{
	public static class WmImports1
	{
		[DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		public static extern IntPtr GetWindowLongPtrW(IntPtr hWnd, WINDOW_LONG_PTR_INDEX nIndex);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		public static extern IntPtr SetWindowLongPtrW(IntPtr hWnd, WINDOW_LONG_PTR_INDEX nIndex, IntPtr dwNewLong);
	}
}
