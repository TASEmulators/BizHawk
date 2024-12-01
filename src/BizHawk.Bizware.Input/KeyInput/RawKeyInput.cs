#nullable enable

using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;

using static BizHawk.Common.RawInputImports;
using static BizHawk.Common.WmImports;

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
		private bool _handleAltKbLayouts;
		private List<KeyEvent> _keyEvents = [ ];
		private readonly object _lockObj = new();
		private bool _disposed;

		private IntPtr RawInputBuffer;
		private int RawInputBufferSize;
		private readonly int RawInputBufferDataOffset;

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

			GCHandle handle;
			RawKeyInput rawKeyInput;
			if (uMsg != WM_INPUT)
			{
				if (uMsg == WM_CLOSE)
				{
					SetWindowLongPtrW(hWnd, GWLP_USERDATA, IntPtr.Zero);
					handle = GCHandle.FromIntPtr(ud);
					rawKeyInput = (RawKeyInput)handle.Target;
					Marshal.FreeCoTaskMem(rawKeyInput.RawInputBuffer);
					handle.Free();
				}

				return DefWindowProcW(hWnd, uMsg, wParam, lParam);
			}

			if (GetRawInputData(lParam, RID.INPUT, IntPtr.Zero,
					out var size, sizeof(RAWINPUTHEADER)) == -1)
			{
				return DefWindowProcW(hWnd, uMsg, wParam, lParam);
			}

			// don't think size should ever be this big, but just in case
			// also, make sure to align the buffer to a pointer boundary
			var buffer = size > 1024
				? new IntPtr[(size + sizeof(IntPtr) - 1) / sizeof(IntPtr)]
				: stackalloc IntPtr[(size + sizeof(IntPtr) - 1) / sizeof(IntPtr)];

			handle = GCHandle.FromIntPtr(ud);
			rawKeyInput = (RawKeyInput)handle.Target;

			fixed (IntPtr* p = buffer)
			{
				var input = (RAWINPUT*)p;
				if (GetRawInputData(lParam, RID.INPUT, input,
						ref size, sizeof(RAWINPUTHEADER)) == -1)
				{
					return DefWindowProcW(hWnd, uMsg, wParam, lParam);
				}

				if (input->header.dwType == RAWINPUTHEADER.RIM_TYPE.KEYBOARD)
				{
					rawKeyInput.AddKeyInput(&input->data.keyboard);
				}
			}

			while (true)
			{
				var rawInputBuffer = (RAWINPUT*)rawKeyInput.RawInputBuffer;
				size = rawKeyInput.RawInputBufferSize;
				var count = GetRawInputBuffer(rawInputBuffer, ref size, sizeof(RAWINPUTHEADER));
				if (count == 0)
				{
					break;
				}

				if (count == -1)
				{
					// From testing, it appears this never actually occurs in practice
					// As GetRawInputBuffer will succeed as long as the buffer has room for at least 1 packet
					// As such, initial size is made very large to hopefully accommodate all packets at once
					const int ERROR_INSUFFICIENT_BUFFER = 0x7A;
					if (Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER)
					{
						rawKeyInput.RawInputBufferSize *= 2;
						rawKeyInput.RawInputBuffer = Marshal.ReAllocCoTaskMem(rawKeyInput.RawInputBuffer, rawKeyInput.RawInputBufferSize);
						continue;
					}

					break;
				}

				for (var i = 0u; i < (uint)count; i++)
				{
					if (rawInputBuffer->header.dwType == RAWINPUTHEADER.RIM_TYPE.KEYBOARD)
					{
						var keyboard = (RAWKEYBOARD*)((byte*)&rawInputBuffer->data.keyboard + rawKeyInput.RawInputBufferDataOffset);
						rawKeyInput.AddKeyInput(keyboard);
					}

					var packetSize = rawInputBuffer->header.dwSize;
					var rawInputBufferUnaligned = (nuint)rawInputBuffer + packetSize;
					var pointerAlignment = (nuint)sizeof(nuint) - 1;
					rawInputBuffer = (RAWINPUT*)((rawInputBufferUnaligned + pointerAlignment) & ~pointerAlignment);
				}
			}

			return IntPtr.Zero;
		}

		private unsafe void AddKeyInput(RAWKEYBOARD* keyboard)
		{
			if ((keyboard->Flags & ~(RAWKEYBOARD.RI_KEY.E0 | RAWKEYBOARD.RI_KEY.BREAK)) == 0)
			{
				var rawKey = (RawKey)(keyboard->MakeCode | ((keyboard->Flags & RAWKEYBOARD.RI_KEY.E0) != 0 ? 0x80 : 0));

				// kind of a dumb hack, the Pause key is apparently special here
				// keyboards would send scancode 0x1D with an E1 prefix, followed by 0x45 (which is NumLock!)
				// this "NumLock" press will set the VKey to 255 (invalid VKey), so we can use that to know if this is actually a Pause press
				// (note that DIK_PAUSE is just 0x45 with an E0 prefix, although this is likely just a conversion DirectInput does internally)
				if (rawKey == RawKey.NUMLOCK && keyboard->VKey == VirtualKey.VK_NONE)
				{
					rawKey = RawKey.PAUSE;
				}

				if (_handleAltKbLayouts)
				{
					rawKey = MapToRealKeyViaScanCode(rawKey);
				}

				if (KeyEnumMap.TryGetValue(rawKey, out var key))
				{
					_keyEvents.Add(new(key, (keyboard->Flags & RAWKEYBOARD.RI_KEY.BREAK) == RAWKEYBOARD.RI_KEY.MAKE));
				}
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

			// 32-bit dumb: GetRawInputBuffer packets are directly copied from the kernel
			// so on WOW64 they will have 64-bit headers (i.e. 64 bit handles, not 32 bit)
			if (IntPtr.Size == 4)
			{
				var currentProccess = Win32Imports.GetCurrentProcess();
				if (!Win32Imports.IsWow64Process(currentProccess, out var isWow64))
				{
					throw new InvalidOperationException("Failed to query WOW64 status");
				}

				RawInputBufferDataOffset = isWow64 ? 8 : 0;
			}

			RawInputBufferSize = (Marshal.SizeOf<RAWINPUT>() + RawInputBufferDataOffset) * 16;
			RawInputBuffer = Marshal.AllocCoTaskMem(RawInputBufferSize);
		}

		public void Dispose()
		{
			lock (_lockObj)
			{
				if (RawInputWindow != IntPtr.Zero)
				{
					// Can't use DestroyWindow, that's only allowed in the thread that created the window!
					PostMessageW(RawInputWindow, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
					RawInputWindow = IntPtr.Zero;
				}
				else
				{
					// We'll free RawInputBuffer the raw input window message handler if it's around
					// Otherwise, just do it here
					Marshal.FreeCoTaskMem(RawInputBuffer);
				}

				_disposed = true;
			}
		}

		public IEnumerable<KeyEvent> Update(bool handleAltKbLayouts)
		{
			lock (_lockObj)
			{
				if (_disposed)
				{
					return [ ];
				}

				if (RawInputWindow == IntPtr.Zero)
				{
					RawInputWindow = CreateRawInputWindow();
					var handle = GCHandle.Alloc(this, GCHandleType.Normal);
					SetWindowLongPtrW(RawInputWindow, GWLP_USERDATA, GCHandle.ToIntPtr(handle));
				}

				_handleAltKbLayouts = handleAltKbLayouts;

				while (PeekMessageW(out var msg, RawInputWindow, 0, 0, PM_REMOVE))
				{
					TranslateMessage(ref msg);
					DispatchMessageW(ref msg);
				}

				var ret = _keyEvents;
				_keyEvents = [ ];
				return ret;
			}
		}

		private static readonly RawKey[] _rawKeysNoTranslation =
		[
			RawKey.NUMPAD0,
			RawKey.NUMPAD1,
			RawKey.NUMPAD2,
			RawKey.NUMPAD3,
			RawKey.NUMPAD4,
			RawKey.NUMPAD5,
			RawKey.NUMPAD6,
			RawKey.NUMPAD7,
			RawKey.NUMPAD8,
			RawKey.NUMPAD9,
			RawKey.DECIMAL,
			RawKey.NUMPADENTER,
			RawKey.PAUSE,
			RawKey.POWER,
			RawKey.WAKE,
			RawKey.INTL2,
			RawKey.INTL3,
			RawKey.INTL4,
			RawKey.LANG3,
			RawKey.LANG4
		];

		private static RawKey MapToRealKeyViaScanCode(RawKey key)
		{
			// Some keys are special and don't have a proper translation
			// for these keys just passthrough to our normal handling
			if (Array.IndexOf(_rawKeysNoTranslation, key) != -1)
			{
				return key;
			}

			var scanCode = (uint)key;
			if ((scanCode & 0x80) != 0)
			{
				scanCode &= 0x7F;
				scanCode |= 0xE000;
			}

			const uint MAPVK_VSC_TO_VK_EX = 0x03;
			var virtualKey = (VirtualKey)MapVirtualKeyW(scanCode, MAPVK_VSC_TO_VK_EX);
			return VKeyToRawKeyMap.GetValueOrDefault(virtualKey);
		}

		private static readonly IReadOnlyDictionary<RawKey, DistinctKey> KeyEnumMap = new Dictionary<RawKey, DistinctKey>
		{
			[RawKey.ESCAPE] = DistinctKey.Escape,
			[RawKey._1] = DistinctKey.D1,
			[RawKey._2] = DistinctKey.D2,
			[RawKey._3] = DistinctKey.D3,
			[RawKey._4] = DistinctKey.D4,
			[RawKey._5] = DistinctKey.D5,
			[RawKey._6] = DistinctKey.D6,
			[RawKey._7] = DistinctKey.D7,
			[RawKey._8] = DistinctKey.D8,
			[RawKey._9] = DistinctKey.D9,
			[RawKey._0] = DistinctKey.D0,
			[RawKey.MINUS] = DistinctKey.OemMinus,
			[RawKey.EQUALS] = DistinctKey.OemPlus,
			[RawKey.BACKSPACE] = DistinctKey.Back,
			[RawKey.TAB] = DistinctKey.Tab,
			[RawKey.Q] = DistinctKey.Q,
			[RawKey.W] = DistinctKey.W,
			[RawKey.E] = DistinctKey.E,
			[RawKey.R] = DistinctKey.R,
			[RawKey.T] = DistinctKey.T,
			[RawKey.Y] = DistinctKey.Y,
			[RawKey.U] = DistinctKey.U,
			[RawKey.I] = DistinctKey.I,
			[RawKey.O] = DistinctKey.O,
			[RawKey.P] = DistinctKey.P,
			[RawKey.LEFTBRACKET] = DistinctKey.OemOpenBrackets,
			[RawKey.RIGHTBRACKET] = DistinctKey.OemCloseBrackets,
			[RawKey.ENTER] = DistinctKey.Enter,
			[RawKey.LEFTCONTROL] = DistinctKey.LeftCtrl,
			[RawKey.A] = DistinctKey.A,
			[RawKey.S] = DistinctKey.S,
			[RawKey.D] = DistinctKey.D,
			[RawKey.F] = DistinctKey.F,
			[RawKey.G] = DistinctKey.G,
			[RawKey.H] = DistinctKey.H,
			[RawKey.J] = DistinctKey.J,
			[RawKey.K] = DistinctKey.K,
			[RawKey.L] = DistinctKey.L,
			[RawKey.SEMICOLON] = DistinctKey.OemSemicolon,
			[RawKey.APOSTROPHE] = DistinctKey.OemQuotes,
			[RawKey.GRAVE] = DistinctKey.OemTilde,
			[RawKey.LEFTSHIFT] = DistinctKey.LeftShift,
			[RawKey.BACKSLASH] = DistinctKey.OemPipe,
			[RawKey.Z] = DistinctKey.Z,
			[RawKey.X] = DistinctKey.X,
			[RawKey.C] = DistinctKey.C,
			[RawKey.V] = DistinctKey.V,
			[RawKey.B] = DistinctKey.B,
			[RawKey.N] = DistinctKey.N,
			[RawKey.M] = DistinctKey.M,
			[RawKey.COMMA] = DistinctKey.OemComma,
			[RawKey.PERIOD] = DistinctKey.OemPeriod,
			[RawKey.SLASH] = DistinctKey.OemQuestion,
			[RawKey.RIGHTSHIFT] = DistinctKey.RightShift,
			[RawKey.MULTIPLY] = DistinctKey.Multiply,
			[RawKey.LEFTALT] = DistinctKey.LeftAlt,
			[RawKey.SPACEBAR] = DistinctKey.Space,
			[RawKey.CAPSLOCK] = DistinctKey.CapsLock,
			[RawKey.F1] = DistinctKey.F1,
			[RawKey.F2] = DistinctKey.F2,
			[RawKey.F3] = DistinctKey.F3,
			[RawKey.F4] = DistinctKey.F4,
			[RawKey.F5] = DistinctKey.F5,
			[RawKey.F6] = DistinctKey.F6,
			[RawKey.F7] = DistinctKey.F7,
			[RawKey.F8] = DistinctKey.F8,
			[RawKey.F9] = DistinctKey.F9,
			[RawKey.F10] = DistinctKey.F10,
			[RawKey.NUMLOCK] = DistinctKey.NumLock,
			[RawKey.SCROLLLOCK] = DistinctKey.Scroll,
			[RawKey.NUMPAD7] = DistinctKey.NumPad7,
			[RawKey.NUMPAD8] = DistinctKey.NumPad8,
			[RawKey.NUMPAD9] = DistinctKey.NumPad9,
			[RawKey.SUBSTRACT] = DistinctKey.Subtract,
			[RawKey.NUMPAD4] = DistinctKey.NumPad4,
			[RawKey.NUMPAD5] = DistinctKey.NumPad5,
			[RawKey.NUMPAD6] = DistinctKey.NumPad6,
			[RawKey.ADD] = DistinctKey.Add,
			[RawKey.NUMPAD1] = DistinctKey.NumPad1,
			[RawKey.NUMPAD2] = DistinctKey.NumPad2,
			[RawKey.NUMPAD3] = DistinctKey.NumPad3,
			[RawKey.NUMPAD0] = DistinctKey.NumPad0,
			[RawKey.DECIMAL] = DistinctKey.Decimal,
			[RawKey.EUROPE2] = DistinctKey.Oem102,
			[RawKey.F11] = DistinctKey.F11,
			[RawKey.F12] = DistinctKey.F12,
			[RawKey.NUMPADEQUALS] = DistinctKey.OemPlus,
			[RawKey.INTL6] = DistinctKey.Separator,
			[RawKey.F13] = DistinctKey.F13,
			[RawKey.F14] = DistinctKey.F14,
			[RawKey.F15] = DistinctKey.F15,
			[RawKey.F16] = DistinctKey.F16,
			[RawKey.F17] = DistinctKey.F17,
			[RawKey.F18] = DistinctKey.F18,
			[RawKey.F19] = DistinctKey.F19,
			[RawKey.F20] = DistinctKey.F20,
			[RawKey.F21] = DistinctKey.F21,
			[RawKey.F22] = DistinctKey.F22,
			[RawKey.F23] = DistinctKey.F23,
			[RawKey.INTL2] = DistinctKey.HanjaMode,
			[RawKey.INTL1] = DistinctKey.HangulMode,
			[RawKey.F24] = DistinctKey.F24,
			[RawKey.LANG4] = DistinctKey.DbeHiragana,
			[RawKey.LANG3] = DistinctKey.DbeKatakana,
			[RawKey.SEPARATOR] = DistinctKey.Separator,
			[RawKey.PREVTRACK] = DistinctKey.MediaPreviousTrack,
			[RawKey.NEXTTRACK] = DistinctKey.MediaNextTrack,
			[RawKey.NUMPADENTER] = DistinctKey.NumPadEnter,
			[RawKey.RIGHTCONTROL] = DistinctKey.RightCtrl,
			[RawKey.MUTE] = DistinctKey.VolumeMute,
			[RawKey.CALCULATOR] = DistinctKey.LaunchApplication2,
			[RawKey.PLAYPAUSE] = DistinctKey.MediaPlayPause,
			[RawKey.STOP] = DistinctKey.MediaStop,
			[RawKey.VOLUMEDOWN] = DistinctKey.VolumeDown,
			[RawKey.VOLUMEUP] = DistinctKey.VolumeUp,
			[RawKey.BROWSERHOME] = DistinctKey.BrowserHome,
			[RawKey.DIVIDE] = DistinctKey.Divide,
			[RawKey.PRINTSCREEN] = DistinctKey.PrintScreen,
			[RawKey.RIGHTALT] = DistinctKey.RightAlt,
			[RawKey.PAUSE] = DistinctKey.Pause,
			[RawKey.HOME] = DistinctKey.Home,
			[RawKey.UP] = DistinctKey.Up,
			[RawKey.PAGEUP] = DistinctKey.PageUp,
			[RawKey.LEFT] = DistinctKey.Left,
			[RawKey.RIGHT] = DistinctKey.Right,
			[RawKey.END] = DistinctKey.End,
			[RawKey.DOWN] = DistinctKey.Down,
			[RawKey.PAGEDOWN] = DistinctKey.PageDown,
			[RawKey.INSERT] = DistinctKey.Insert,
			[RawKey.DELETE] = DistinctKey.Delete,
			[RawKey.LEFTGUI] = DistinctKey.LWin,
			[RawKey.RIGHTGUI] = DistinctKey.RWin,
			[RawKey.APPS] = DistinctKey.Apps,
			[RawKey.SLEEP] = DistinctKey.Sleep,
			[RawKey.WAKE] = DistinctKey.Sleep,
			[RawKey.BROWSERSEARCH] = DistinctKey.BrowserSearch,
			[RawKey.BROWSERFAVORITES] = DistinctKey.BrowserFavorites,
			[RawKey.BROWSERREFRESH] = DistinctKey.BrowserRefresh,
			[RawKey.BROWSERSTOP] = DistinctKey.BrowserStop,
			[RawKey.BROWSERFORWARD] = DistinctKey.BrowserForward,
			[RawKey.BROWSERBACK] = DistinctKey.BrowserBack,
			[RawKey.MYCOMPUTER] = DistinctKey.LaunchApplication1,
			[RawKey.MAIL] = DistinctKey.LaunchMail,
			[RawKey.MEDIASELECT] = DistinctKey.SelectMedia,
		};

		private static readonly IReadOnlyDictionary<VirtualKey, RawKey> VKeyToRawKeyMap = new Dictionary<VirtualKey, RawKey>
		{
			[VirtualKey.VK_BACK] = RawKey.BACKSPACE,
			[VirtualKey.VK_TAB] = RawKey.TAB,
			[VirtualKey.VK_CLEAR] = RawKey.NUMLOCK,
			[VirtualKey.VK_RETURN] = RawKey.ENTER,
			[VirtualKey.VK_PAUSE] = RawKey.PAUSE,
			[VirtualKey.VK_CAPITAL] = RawKey.CAPSLOCK,
			[VirtualKey.VK_KANA] = RawKey.INTL2,
			[VirtualKey.VK_ESCAPE] = RawKey.ESCAPE,
			[VirtualKey.VK_SPACE] = RawKey.SPACEBAR,
			[VirtualKey.VK_PRIOR] = RawKey.PAGEUP,
			[VirtualKey.VK_NEXT] = RawKey.PAGEDOWN,
			[VirtualKey.VK_END] = RawKey.END,
			[VirtualKey.VK_HOME] = RawKey.HOME,
			[VirtualKey.VK_LEFT] = RawKey.LEFT,
			[VirtualKey.VK_UP] = RawKey.UP,
			[VirtualKey.VK_RIGHT] = RawKey.RIGHT,
			[VirtualKey.VK_DOWN] = RawKey.DOWN,
			[VirtualKey.VK_PRINT] = RawKey.PRINTSCREEN,
			[VirtualKey.VK_SNAPSHOT] = RawKey.PRINTSCREEN,
			[VirtualKey.VK_INSERT] = RawKey.INSERT,
			[VirtualKey.VK_DELETE] = RawKey.DELETE,
			[VirtualKey.VK_0] = RawKey._0,
			[VirtualKey.VK_1] = RawKey._1,
			[VirtualKey.VK_2] = RawKey._2,
			[VirtualKey.VK_3] = RawKey._3,
			[VirtualKey.VK_4] = RawKey._4,
			[VirtualKey.VK_5] = RawKey._5,
			[VirtualKey.VK_6] = RawKey._6,
			[VirtualKey.VK_7] = RawKey._7,
			[VirtualKey.VK_8] = RawKey._8,
			[VirtualKey.VK_9] = RawKey._9,
			[VirtualKey.VK_A] = RawKey.A,
			[VirtualKey.VK_B] = RawKey.B,
			[VirtualKey.VK_C] = RawKey.C,
			[VirtualKey.VK_D] = RawKey.D,
			[VirtualKey.VK_E] = RawKey.E,
			[VirtualKey.VK_F] = RawKey.F,
			[VirtualKey.VK_G] = RawKey.G,
			[VirtualKey.VK_H] = RawKey.H,
			[VirtualKey.VK_I] = RawKey.I,
			[VirtualKey.VK_J] = RawKey.J,
			[VirtualKey.VK_K] = RawKey.K,
			[VirtualKey.VK_L] = RawKey.L,
			[VirtualKey.VK_M] = RawKey.M,
			[VirtualKey.VK_N] = RawKey.N,
			[VirtualKey.VK_O] = RawKey.O,
			[VirtualKey.VK_P] = RawKey.P,
			[VirtualKey.VK_Q] = RawKey.Q,
			[VirtualKey.VK_R] = RawKey.R,
			[VirtualKey.VK_S] = RawKey.S,
			[VirtualKey.VK_T] = RawKey.T,
			[VirtualKey.VK_U] = RawKey.U,
			[VirtualKey.VK_V] = RawKey.V,
			[VirtualKey.VK_W] = RawKey.W,
			[VirtualKey.VK_X] = RawKey.X,
			[VirtualKey.VK_Y] = RawKey.Y,
			[VirtualKey.VK_Z] = RawKey.Z,
			[VirtualKey.VK_LWIN] = RawKey.LEFTGUI,
			[VirtualKey.VK_RWIN] = RawKey.RIGHTGUI,
			[VirtualKey.VK_APPS] = RawKey.APPS,
			[VirtualKey.VK_SLEEP] = RawKey.SLEEP,
			[VirtualKey.VK_NUMPAD0] = RawKey.NUMPAD0,
			[VirtualKey.VK_NUMPAD1] = RawKey.NUMPAD1,
			[VirtualKey.VK_NUMPAD2] = RawKey.NUMPAD2,
			[VirtualKey.VK_NUMPAD3] = RawKey.NUMPAD3,
			[VirtualKey.VK_NUMPAD4] = RawKey.NUMPAD4,
			[VirtualKey.VK_NUMPAD5] = RawKey.NUMPAD5,
			[VirtualKey.VK_NUMPAD6] = RawKey.NUMPAD6,
			[VirtualKey.VK_NUMPAD7] = RawKey.NUMPAD7,
			[VirtualKey.VK_NUMPAD8] = RawKey.NUMPAD8,
			[VirtualKey.VK_NUMPAD9] = RawKey.NUMPAD9,
			[VirtualKey.VK_MULTIPLY] = RawKey.MULTIPLY,
			[VirtualKey.VK_ADD] = RawKey.ADD,
			[VirtualKey.VK_SEPARATOR] = RawKey.SEPARATOR,
			[VirtualKey.VK_SUBTRACT] = RawKey.SUBSTRACT,
			[VirtualKey.VK_DECIMAL] = RawKey.DECIMAL,
			[VirtualKey.VK_DIVIDE] = RawKey.DIVIDE,
			[VirtualKey.VK_F1] = RawKey.F1,
			[VirtualKey.VK_F2] = RawKey.F2,
			[VirtualKey.VK_F3] = RawKey.F3,
			[VirtualKey.VK_F4] = RawKey.F4,
			[VirtualKey.VK_F5] = RawKey.F5,
			[VirtualKey.VK_F6] = RawKey.F6,
			[VirtualKey.VK_F7] = RawKey.F7,
			[VirtualKey.VK_F8] = RawKey.F8,
			[VirtualKey.VK_F9] = RawKey.F9,
			[VirtualKey.VK_F10] = RawKey.F10,
			[VirtualKey.VK_F11] = RawKey.F11,
			[VirtualKey.VK_F12] = RawKey.F12,
			[VirtualKey.VK_F13] = RawKey.F13,
			[VirtualKey.VK_F14] = RawKey.F14,
			[VirtualKey.VK_F15] = RawKey.F15,
			[VirtualKey.VK_F16] = RawKey.F16,
			[VirtualKey.VK_F17] = RawKey.F17,
			[VirtualKey.VK_F18] = RawKey.F18,
			[VirtualKey.VK_F19] = RawKey.F19,
			[VirtualKey.VK_F20] = RawKey.F20,
			[VirtualKey.VK_F21] = RawKey.F21,
			[VirtualKey.VK_F22] = RawKey.F22,
			[VirtualKey.VK_F23] = RawKey.F23,
			[VirtualKey.VK_F24] = RawKey.F24,
			[VirtualKey.VK_NUMLOCK] = RawKey.NUMLOCK,
			[VirtualKey.VK_SCROLL] = RawKey.SCROLLLOCK,
			[VirtualKey.VK_OEM_FJ_JISHO] = RawKey.NUMPADEQUALS,
			[VirtualKey.VK_LSHIFT] = RawKey.LEFTSHIFT,
			[VirtualKey.VK_RSHIFT] = RawKey.RIGHTSHIFT,
			[VirtualKey.VK_LCONTROL] = RawKey.LEFTCONTROL,
			[VirtualKey.VK_RCONTROL] = RawKey.RIGHTCONTROL,
			[VirtualKey.VK_LMENU] = RawKey.LEFTGUI,
			[VirtualKey.VK_RMENU] = RawKey.RIGHTGUI,
			[VirtualKey.VK_BROWSER_BACK] = RawKey.BROWSERBACK,
			[VirtualKey.VK_BROWSER_FORWARD] = RawKey.BROWSERFORWARD,
			[VirtualKey.VK_BROWSER_REFRESH] = RawKey.BROWSERREFRESH,
			[VirtualKey.VK_BROWSER_STOP] = RawKey.BROWSERSTOP,
			[VirtualKey.VK_BROWSER_SEARCH] = RawKey.BROWSERSEARCH,
			[VirtualKey.VK_BROWSER_FAVORITES] = RawKey.BROWSERFAVORITES,
			[VirtualKey.VK_BROWSER_HOME] = RawKey.BROWSERHOME,
			[VirtualKey.VK_VOLUME_MUTE] = RawKey.MUTE,
			[VirtualKey.VK_VOLUME_DOWN] = RawKey.VOLUMEDOWN,
			[VirtualKey.VK_VOLUME_UP] = RawKey.VOLUMEUP,
			[VirtualKey.VK_MEDIA_NEXT_TRACK] = RawKey.NEXTTRACK,
			[VirtualKey.VK_MEDIA_PREV_TRACK] = RawKey.PREVTRACK,
			[VirtualKey.VK_MEDIA_STOP] = RawKey.STOP,
			[VirtualKey.VK_MEDIA_PLAY_PAUSE] = RawKey.PLAYPAUSE,
			[VirtualKey.VK_LAUNCH_MAIL] = RawKey.MAIL,
			[VirtualKey.VK_LAUNCH_MEDIA_SELECT] = RawKey.MEDIASELECT,
			[VirtualKey.VK_LAUNCH_APP1] = RawKey.MYCOMPUTER,
			[VirtualKey.VK_LAUNCH_APP2] = RawKey.CALCULATOR,
			[VirtualKey.VK_OEM_1] = RawKey.SEMICOLON,
			[VirtualKey.VK_OEM_PLUS] = RawKey.EQUALS,
			[VirtualKey.VK_OEM_COMMA] = RawKey.COMMA,
			[VirtualKey.VK_OEM_MINUS] = RawKey.MINUS,
			[VirtualKey.VK_OEM_PERIOD] = RawKey.PERIOD,
			[VirtualKey.VK_OEM_2] = RawKey.SLASH,
			[VirtualKey.VK_OEM_3] = RawKey.GRAVE,
			[VirtualKey.VK_ABNT_C1] = RawKey.BACKSLASH,
			[VirtualKey.VK_ABNT_C2] = RawKey.SEPARATOR,
			[VirtualKey.VK_OEM_4] = RawKey.LEFTBRACKET,
			[VirtualKey.VK_OEM_5] = RawKey.BACKSLASH,
			[VirtualKey.VK_OEM_6] = RawKey.RIGHTBRACKET,
			[VirtualKey.VK_OEM_7] = RawKey.APOSTROPHE,
			[VirtualKey.VK_OEM_8] = RawKey.RIGHTCONTROL,
			[VirtualKey.VK_OEM_102] = RawKey.EUROPE2,
			[VirtualKey.VK_OEM_ATTN] = RawKey.CAPSLOCK,
			[VirtualKey.VK_OEM_FINISH] = RawKey.RIGHTCONTROL,
			[VirtualKey.VK_OEM_COPY] = RawKey.LEFTALT,
			[VirtualKey.VK_OEM_AUTO] = RawKey.GRAVE,
		};
	}
}
