using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Windows.Win32
{
	public static partial class Win32Imports
	{
		/// <summary><see href="https://learn.microsoft.com/windows/win32/api/winbase/nf-winbase-makeintatom"/></summary>
		/// <remarks><see href="https://github.com/microsoft/CsWin32/issues/443#issuecomment-964670905"/></remarks>
		public static unsafe PCWSTR MAKEINTATOM(uint i)
			=> new(unchecked((char*) i));

		/// <inheritdoc cref="SendMessageW(HWND, uint, WPARAM, LPARAM)"/>
		public static unsafe LRESULT SendMessageW<T>(HWND hWnd, uint Msg, IntPtr wParam, ref T lParam)
			where T : unmanaged
		{
			fixed (void* ptr = &lParam)
			{
				return SendMessageW(
					hWnd,
					Msg,
					new(unchecked((UIntPtr) wParam.ToPointer())),
					new(unchecked((IntPtr) ptr)));
			}
		}

		/// <summary>getter</summary>
		/// <seealso cref="SystemParametersInfoW(SYSTEM_PARAMETERS_INFO_ACTION, uint, void*, SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS)"/>
		public static unsafe BOOL SystemParametersInfoW(
			SYSTEM_PARAMETERS_INFO_ACTION uiAction,
			uint uiParam,
			out nint pvParam)
		{
			const SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS NONE = 0;
			fixed (void* ptr = &pvParam) return SystemParametersInfoW(uiAction, uiParam, ptr, NONE);
		}

		/// <summary>setter</summary>
		/// <seealso cref="SystemParametersInfoW(SYSTEM_PARAMETERS_INFO_ACTION, uint, void*, SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS)"/>
		public static unsafe BOOL SystemParametersInfoW(
			SYSTEM_PARAMETERS_INFO_ACTION uiAction,
			uint uiParam,
			SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS fWinIni)
		{
			nint pvParam = default;
			return SystemParametersInfoW(uiAction, uiParam, &pvParam, fWinIni);
		}

		/// <inheritdoc cref="TrackPopupMenuEx(HMENU, uint, int, int, HWND, TPMPARAMS*)"/>
		public static unsafe BOOL TrackPopupMenuEx(
			HMENU hMenu,
			TPMFLAGS uFlags,
			int x,
			int y,
			HWND hwnd,
			TPMPARAMS* lptpm)
				=> TrackPopupMenuEx(hMenu, uFlags: unchecked((uint) uFlags), x: x, y: y, hwnd, lptpm);
	}
}
