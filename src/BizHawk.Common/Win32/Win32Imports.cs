#nullable disable

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BizHawk.Common
{
	public static class Win32Imports
	{
		public const int MAX_PATH = 260;
		public const uint PM_REMOVE = 0x0001U;
		public static readonly IntPtr HWND_MESSAGE = new(-3);

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

		[StructLayout(LayoutKind.Sequential)]
		public struct MSG
		{
			public IntPtr hwnd;
			public uint message;
			public IntPtr wParam;
			public IntPtr lParam;
			public uint time;
			public int x;
			public int y;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWINPUTDEVICE
		{
			public HidUsagePage usUsagePage;
			public HidUsageId usUsage;
			public RIDEV dwFlags;
			public IntPtr hwndTarget;

			public enum HidUsagePage : ushort
			{
				GENERIC = 1,
				GAME = 5,
				LED = 8,
				BUTTON = 9,
			}

			public enum HidUsageId : ushort
			{
				GENERIC_POINTER = 1,
				GENERIC_MOUSE = 2,
				GENERIC_JOYSTICK = 4,
				GENERIC_GAMEPAD = 5,
				GENERIC_KEYBOARD = 6,
				GENERIC_KEYPAD = 7,
				GENERIC_MULTI_AXIS_CONTROLLER = 8,
			}

			[Flags]
			public enum RIDEV : int
			{
				REMOVE = 0x00000001,
				EXCLUDE = 0x00000010,
				PAGEONLY = 0x00000020,
				NOLEGACY = PAGEONLY | EXCLUDE,
				INPUTSINK = 0x00000100,
				CAPTUREMOUSE = 0x00000200,
				NOHOTKEYS = CAPTUREMOUSE,
				APPKEYS = 0x00000400,
				EXINPUTSINK = 0x00001000,
				DEVNOTIFY = 0x00002000,
			}
		}

		public enum RID : uint
		{
			HEADER = 0x10000005,
			INPUT = 0x10000003,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWINPUTHEADER
		{
			public RIM_TYPE dwType;
			public uint dwSize;
			public IntPtr hDevice;
			public IntPtr wParam;

			public enum RIM_TYPE : uint
			{
				MOUSE = 0,
				KEYBOARD = 1,
				HID = 2,
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWMOUSE
		{
			public ushort usFlags;
			public uint ulButtons;
			public uint ulRawButtons;
			public int lLastX;
			public int lLastY;
			public uint ulExtraInformation;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWKEYBOARD
		{
			public ushort MakeCode;
			public RIM_KEY Flags;
			public ushort Reserved;
			public ushort VKey;
			public uint Message;
			public uint ExtraInformation;

			public enum RIM_KEY : ushort
			{
				MAKE = 0,
				BREAK = 1,
				E0 = 2,
				E1 = 3,
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWHID
		{
			public uint dwSizeHid;
			public uint dwCount;
			public byte bRawData;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct RAWINPUTDATA
		{
			[FieldOffset(0)]
			public RAWMOUSE mouse;
			[FieldOffset(0)]
			public RAWKEYBOARD keyboard;
			[FieldOffset(0)]
			public RAWHID hid;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWINPUT
		{
			public RAWINPUTHEADER header;
			public RAWINPUTDATA data;
		}

		public delegate IntPtr WNDPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct WNDCLASS
		{
			public uint style;
			public WNDPROC lpfnWndProc;
			public int cbClsExtra;
			public int cbWndExtra;
			public IntPtr hInstance;
			public IntPtr hIcon;
			public IntPtr hCursor;
			public IntPtr hbrBackground;
			public string lpszMenuName;
			public string lpszClassName;
		}

		[Guid("00000002-0000-0000-C000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
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

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr CreateWindowEx(int dwExStyle, IntPtr lpClassName, string lpWindowName,
			int dwStyle, int X, int Y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

		[DllImport("kernel32.dll", EntryPoint = "DeleteFileW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern unsafe IntPtr DefRawInputProc(RAWINPUT* paRawInput, int nInput, int cbSizeHeader);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DestroyWindow(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr hWndChildAfter, string className, string windowTitle);

		[DllImport("user32.dll")]
		public static extern IntPtr GetActiveWindow();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr GetModuleHandle(string lpModuleName);

		[DllImport("kernel32", SetLastError = true, EntryPoint = "GetProcAddress")]
		public static extern IntPtr GetProcAddressOrdinal(IntPtr hModule, IntPtr procName);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr GetProcessHeap();

		[DllImport("user32.dll")]
		public static extern int GetRawInputData(IntPtr hRawInput, RID uiCommand, IntPtr pData, out int bSize, int cbSizeHeader);

		[DllImport("user32.dll")]
		public static extern unsafe int GetRawInputData(IntPtr hRawInput, RID uiCommand, RAWINPUT* pData, ref int bSize, int cbSizeHeader);

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
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr RegisterClass([In] ref WNDCLASS lpWndClass);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, int cbSize);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, ref HDITEM lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

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

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool TranslateMessage([In] ref MSG lpMsg);
	}
}
