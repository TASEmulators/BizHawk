#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Client.Common;
using BizHawk.Common;

using static BizHawk.Common.RawInputImports;
using static BizHawk.Common.WmImports;

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
			var wc = default(WNDCLASSW);
			wc.lpfnWndProc = _wndProc;
			wc.hInstance = LoaderApiImports.GetModuleHandleW(null);
			wc.lpszClassName = "RawKeyInputClass";

			var atom = RegisterClassW(ref wc);
			if (atom == IntPtr.Zero)
			{
				throw new InvalidOperationException("Failed to register RAWINPUT window class");
			}

			return atom;
		});

		private static unsafe IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
		{
			var ud = GetWindowLongPtrW(hWnd, GWLP_USERDATA);
			if (ud == IntPtr.Zero)
			{
				return DefWindowProcW(hWnd, uMsg, wParam, lParam);
			}

			if (uMsg != WM_INPUT)
			{
				if (uMsg == WM_CLOSE)
				{
					SetWindowLongPtrW(hWnd, GWLP_USERDATA, IntPtr.Zero);
					GCHandle.FromIntPtr(ud).Free();
				}

				return DefWindowProcW(hWnd, uMsg, wParam, lParam);
			}

			if (GetRawInputData(lParam, RID.INPUT, IntPtr.Zero,
					out var size, Marshal.SizeOf<RAWINPUTHEADER>()) == -1)
			{
				return DefWindowProcW(hWnd, uMsg, wParam, lParam);
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
					return DefWindowProcW(hWnd, uMsg, wParam, lParam);
				}

				if (input->header.dwType == RAWINPUTHEADER.RIM_TYPE.KEYBOARD &&
					(input->data.keyboard.Flags & ~(RAWKEYBOARD.RIM_KEY.E0 | RAWKEYBOARD.RIM_KEY.BREAK)) == 0)
				{
					var handle = GCHandle.FromIntPtr(ud);
					var rawKeyInput = (RawKeyInput)handle.Target;

					lock (rawKeyInput.LockObj)
					{
						// TODO: Make a separate enum map for RAWINPUT / VKeys and don't rely on DKeyInput's maps 
						var rawKey = (RawKey)(input->data.keyboard.MakeCode |
							((input->data.keyboard.Flags & RAWKEYBOARD.RIM_KEY.E0) != 0 ? 0x80 : 0));

						// kind of a dumb hack, the Pause key is apparently special here
						// keyboards would send scancode 0x1D with an E1 prefix, followed by 0x45 (which is NumLock!)
						// this "NumLock" press will set the VKey to 255 (invalid VKey), so we can use that to know if this is actually a Pause press
						// (note that DIK_PAUSE is just 0x45 with an E0 prefix, although this is likely just a conversion DirectInput does internally)
						if (rawKey == RawKey.NumberLock && input->data.keyboard.VKey == 0xFF)
						{
							rawKey = RawKey.Pause;
						}

						if (rawKeyInput.HandleAltKbLayouts)
						{
							rawKey = DKeyInput.MapToRealKeyViaScanCode(rawKey);
						}

						if (DKeyInput.KeyEnumMap.TryGetValue(rawKey, out var key) && key != DistinctKey.Unknown)
						{
							rawKeyInput.KeyEvents.Add(new(key, (input->data.keyboard.Flags & RAWKEYBOARD.RIM_KEY.BREAK) == RAWKEYBOARD.RIM_KEY.MAKE));
						}
					}
				}

				return DefRawInputProc(input, 0, Marshal.SizeOf<RAWINPUTHEADER>());
			}
		}

		private static IntPtr CreateRawInputWindow()
		{
			const int WS_CHILD = 0x40000000;
			var window = CreateWindowExW(
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
					hInstance: LoaderApiImports.GetModuleHandleW(null),
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
					PostMessageW(RawInputWindow, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
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
					SetWindowLongPtrW(RawInputWindow, GWLP_USERDATA, GCHandle.ToIntPtr(handle));
				}

				HandleAltKbLayouts = handleAltKbLayouts;

				while (PeekMessageW(out var msg, RawInputWindow, 0, 0, PM_REMOVE))
				{
					TranslateMessage(ref msg);
					DispatchMessageW(ref msg);
				}

				var ret = KeyEvents;
				KeyEvents = new();
				return ret;
			}
		}
	}
}
