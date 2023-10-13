#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;

using static BizHawk.Common.RawInputImports;
using static BizHawk.Common.Win32Imports;

using RAWKey = Vortice.DirectInput.Key;

namespace BizHawk.Bizware.Input
{
	internal static class RAWKeyInput
	{
		private static volatile bool _isInit;
		private static IntPtr _rawInputWindowAtom;
		private static IntPtr _rawInputWindow;
		private static bool _handleAlternativeKeyboardLayouts;
		private static List<KeyEvent> _keyEvents = new();

		private static readonly WNDPROC _wndProc = WndProc;
		private static readonly object _lockObj = new();

		private static unsafe IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
		{
			const uint WM_INPUT = 0x00FF;

			if (uMsg != WM_INPUT)
			{
				return DefWindowProc(hWnd, uMsg, wParam, lParam);
			}

			if (GetRawInputData(lParam, RID.INPUT, IntPtr.Zero,
					out var size, Marshal.SizeOf<RAWINPUTHEADER>()) == -1)
			{
				return DefWindowProc(hWnd, uMsg, wParam, lParam);
			}

			// don't think size should ever be this big, but just in case
			var buffer = size > 1024
				? new byte[size]
				: stackalloc byte[size];

			fixed (byte* p = buffer)
			{
				var input = (RAWINPUT*)p;

				if (GetRawInputData(lParam, RID.INPUT, input,
						ref size, Marshal.SizeOf<RAWINPUTHEADER>()) == -1)
				{
					return DefWindowProc(hWnd, uMsg, wParam, lParam);
				}

				if (input->header.dwType == RAWINPUTHEADER.RIM_TYPE.KEYBOARD && input->data.keyboard.Flags <= RAWKEYBOARD.RIM_KEY.E1)
				{
					var rawKey = _handleAlternativeKeyboardLayouts
						? DKeyInput.VKeyToDKeyMap.GetValueOrDefault(input->data.keyboard.VKey, RAWKey.Unknown)
						: (RAWKey)(input->data.keyboard.MakeCode |
							(input->data.keyboard.Flags >= RAWKEYBOARD.RIM_KEY.E0 ? 0x80 : 0));

					if (DKeyInput.KeyEnumMap.TryGetValue(rawKey, out var key) && key != DistinctKey.Unknown)
					{
						_keyEvents.Add(new(key, input->data.keyboard.Flags is RAWKEYBOARD.RIM_KEY.MAKE or RAWKEYBOARD.RIM_KEY.E0));
					}
				}

				return DefRawInputProc(input, 0, Marshal.SizeOf<RAWINPUTHEADER>());
			}
		}

		private static void CreateRawInputWindow()
		{
			const int WS_CHILD = 0x40000000;
			var window = CreateWindowEx(
					dwExStyle: 0,
					lpClassName: _rawInputWindowAtom,
					lpWindowName: "RAWKeyInput",
					dwStyle: WS_CHILD,
					X: 0,
					Y: 0,
					nWidth: 1,
					nHeight: 1,
					hWndParent: HWND_MESSAGE,
					hMenu: IntPtr.Zero,
					hInstance: GetModuleHandle(null),
					lpParam: IntPtr.Zero);

			if (window == IntPtr.Zero)
			{
				throw new InvalidOperationException("Failed to create RAWINPUT window");
			}

			var ri = new RAWINPUTDEVICE[1];
			ri[0].usUsagePage = RAWINPUTDEVICE.HidUsagePage.GENERIC;
			ri[0].usUsage = RAWINPUTDEVICE.HidUsageId.GENERIC_KEYBOARD;
			ri[0].dwFlags = RAWINPUTDEVICE.RIDEV.INPUTSINK;
			ri[0].hwndTarget = window;

			if (!RegisterRawInputDevices(ri, 1, Marshal.SizeOf<RAWINPUTDEVICE>()))
			{
				DestroyWindow(window);
				throw new InvalidOperationException("Failed to register RAWINPUTDEVICE");
			}

			_rawInputWindow = window;
		}

		public static void Initialize()
		{
			if (OSTailoredCode.IsUnixHost)
			{
				throw new NotSupportedException("RAWINPUT is Windows only");
			}

			lock (_lockObj)
			{
				Deinitialize();

				if (_rawInputWindowAtom == IntPtr.Zero)
				{
					var wc = default(WNDCLASS);
					wc.lpfnWndProc = _wndProc;
					wc.hInstance = GetModuleHandle(null);
					wc.lpszClassName = "RAWKeyInputClass";

					_rawInputWindowAtom = RegisterClass(ref wc);
					if (_rawInputWindowAtom == IntPtr.Zero)
					{
						throw new InvalidOperationException("Failed to register RAWINPUT window class");
					}

					// we can't use a window created on this thread, as Update is called on a different thread
					// but we can still test window creation
					CreateRawInputWindow(); // this will throw if window creation or rawinput registering fails
					DestroyWindow(_rawInputWindow);
					_rawInputWindow = IntPtr.Zero;

					_isInit = true;
				}
			}
		}

		public static void Deinitialize()
		{
			lock (_lockObj)
			{
				if (_rawInputWindow != IntPtr.Zero)
				{
					// Can't use DestroyWindow, that's only allowed in the thread that created the window!
					const int WM_CLOSE = 0x0010;
					PostMessage(_rawInputWindow, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
					_rawInputWindow = IntPtr.Zero;
				}

				_keyEvents.Clear();
				_isInit = false;
			}
		}

		public static IEnumerable<KeyEvent> Update(Config config)
		{
			lock (_lockObj)
			{
				if (!_isInit)
				{
					return Enumerable.Empty<KeyEvent>();
				}

				if (_rawInputWindow == IntPtr.Zero)
				{
					CreateRawInputWindow();
				}

				_handleAlternativeKeyboardLayouts = config.HandleAlternateKeyboardLayouts;

				while (PeekMessage(out var msg, _rawInputWindow, 0, 0, PM_REMOVE))
				{
					TranslateMessage(ref msg);
					DispatchMessage(ref msg);
				}

				var ret = _keyEvents;
				_keyEvents = new();
				return ret;
			}
		}
	}
}
