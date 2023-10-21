#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;

using static BizHawk.Common.RawInputImports;
using static BizHawk.Common.Win32Imports;

using RawKey = Vortice.DirectInput.Key;

namespace BizHawk.Bizware.Input
{
	/// <summary>
	/// Note: Only 1 window per device class (i.e. keyboards) is actually allowed to RAWINPUT (last one to call RegisterRawInputDevices)
	/// So only one instance can actually be used at the same time
	/// </summary>
	internal sealed class RawKeyInput : IKeyInput
	{
		private const int WM_CLOSE = 0x0010;
		private const int WM_INPUT = 0x00FF;

		private IntPtr RawInputWindow;
		private bool HandleAltKbLayouts;
		private List<KeyEvent> KeyEvents = new();
		private readonly object LockObj = new();

		private static readonly WNDPROC _wndProc = WndProc;

		private static readonly Lazy<IntPtr> _rawInputWindowAtom = new(() =>
		{
			var wc = default(WNDCLASS);
			wc.lpfnWndProc = _wndProc;
			wc.hInstance = GetModuleHandle(null);
			wc.lpszClassName = "RawKeyInputClass";

			var atom = RegisterClass(ref wc);
			if (atom == IntPtr.Zero)
			{
				throw new InvalidOperationException("Failed to register RAWINPUT window class");
			}

			return atom;
		});

		private static unsafe IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
		{
			var ud = GetWindowLongPtr(hWnd, GWLP_USERDATA);
			if (ud == IntPtr.Zero)
			{
				return DefWindowProc(hWnd, uMsg, wParam, lParam);
			}

			if (uMsg != WM_INPUT)
			{
				if (uMsg == WM_CLOSE)
				{
					SetWindowLongPtr(hWnd, GWLP_USERDATA, IntPtr.Zero);
					GCHandle.FromIntPtr(ud).Free();
				}

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
					var handle = GCHandle.FromIntPtr(ud);
					var rawKeyInput = (RawKeyInput)handle.Target;

					lock (rawKeyInput.LockObj)
					{
						// TODO: Make a separate enum map for RAWINPUT / VKeys and don't rely on DKeyInput's maps 
						var rawKey = rawKeyInput.HandleAltKbLayouts
							? DKeyInput.VKeyToDKeyMap.GetValueOrDefault(input->data.keyboard.VKey, RawKey.Unknown)
							: (RawKey)(input->data.keyboard.MakeCode |
								(input->data.keyboard.Flags >= RAWKEYBOARD.RIM_KEY.E0 ? 0x80 : 0));

						if (DKeyInput.KeyEnumMap.TryGetValue(rawKey, out var key) && key != DistinctKey.Unknown)
						{
							rawKeyInput.KeyEvents.Add(new(key, input->data.keyboard.Flags is RAWKEYBOARD.RIM_KEY.MAKE or RAWKEYBOARD.RIM_KEY.E0));
						}
					}
				}

				return DefRawInputProc(input, 0, Marshal.SizeOf<RAWINPUTHEADER>());
			}
		}

		private static IntPtr CreateRawInputWindow()
		{
			const int WS_CHILD = 0x40000000;
			var window = CreateWindowEx(
					dwExStyle: 0,
					lpClassName: _rawInputWindowAtom.Value,
					lpWindowName: "RawKeyInput",
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

			var rid = default(RAWINPUTDEVICE);
			rid.usUsagePage = RAWINPUTDEVICE.HidUsagePage.GENERIC;
			rid.usUsage = RAWINPUTDEVICE.HidUsageId.GENERIC_KEYBOARD;
			rid.dwFlags = RAWINPUTDEVICE.RIDEV.INPUTSINK;
			rid.hwndTarget = window;

			if (!RegisterRawInputDevices(ref rid, 1, Marshal.SizeOf<RAWINPUTDEVICE>()))
			{
				DestroyWindow(window);
				throw new InvalidOperationException("Failed to register RAWINPUTDEVICE");
			}

			return window;
		}

		public RawKeyInput()
		{
			if (OSTailoredCode.IsUnixHost)
			{
				throw new NotSupportedException("RAWINPUT is Windows only");
			}

			// we can't use a window created on this thread, as Update is called on a different thread
			// but we can still test window creation
			var testWindow = CreateRawInputWindow(); // this will throw if window creation or rawinput registering fails
			DestroyWindow(testWindow);
		}

		public void Dispose()
		{
			lock (LockObj)
			{
				if (RawInputWindow != IntPtr.Zero)
				{
					// Can't use DestroyWindow, that's only allowed in the thread that created the window!
					PostMessage(RawInputWindow, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
					RawInputWindow = IntPtr.Zero;
				}
			}
		}

		public IEnumerable<KeyEvent> Update(bool handleAltKbLayouts)
		{
			lock (LockObj)
			{
				if (RawInputWindow == IntPtr.Zero)
				{
					RawInputWindow = CreateRawInputWindow();
					var handle = GCHandle.Alloc(this, GCHandleType.Normal);
					SetWindowLongPtr(RawInputWindow, GWLP_USERDATA, GCHandle.ToIntPtr(handle));
				}

				HandleAltKbLayouts = handleAltKbLayouts;

				while (PeekMessage(out var msg, RawInputWindow, 0, 0, PM_REMOVE))
				{
					TranslateMessage(ref msg);
					DispatchMessage(ref msg);
				}

				var ret = KeyEvents;
				KeyEvents = new();
				return ret;
			}
		}
	}
}
