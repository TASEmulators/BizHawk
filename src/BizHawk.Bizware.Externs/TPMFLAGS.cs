using Windows.Win32.Foundation;

namespace Windows.Win32.UI.WindowsAndMessaging
{
	/// <summary><see href="https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-trackpopupmenuex#parameters"/></summary>
	/// <seealso cref="Win32Imports.TrackPopupMenuEx(HMENU, TPMFLAGS, int, int, HWND, TPMPARAMS*)"/>
	[Flags]
	public enum TPMFLAGS : uint
	{
		CENTERALIGN = 0x0004U,
		LEFTALIGN = 0x0000U,
		RIGHTALIGN = 0x0008U,

		BOTTOMALIGN = 0x0020U,
		TOPALIGN = 0x0000U,
		VCENTERALIGN = 0x0010U,

		NONOTIFY = 0x0080U,
		RETURNCMD = 0x0100U,

		LEFTBUTTON = 0x0000U,
		RIGHTBUTTON = 0x0002U,

		HORNEGANIMATION = 0x0800U,
		HORPOSANIMATION = 0x0400U,
		NOANIMATION = 0x4000U,
		VERNEGANIMATION = 0x2000U,
		VERPOSANIMATION = 0x1000U,

		RECURSE = 0x0001U, // value missing from official docs, but confirmed by https://github.com/microsoft/windows-rs/blob/bb15076311bf185400ecd244d47596b8415450fa/crates/libs/sys/src/Windows/Win32/UI/WindowsAndMessaging/mod.rs#L3461

		HORIZONTAL = 0x0000U,
		VERTICAL = 0x0040U,

		LAYOUTRTL = 0x8000U, // value also missing from official docs, but confirmed by https://github.com/microsoft/windows-rs/blob/bb15076311bf185400ecd244d47596b8415450fa/crates/libs/sys/src/Windows/Win32/UI/WindowsAndMessaging/mod.rs#L3456
	}
}
