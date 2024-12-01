#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;

using static BizHawk.Common.XlibImports;

// a lot of this code is taken from OpenTK

namespace BizHawk.Bizware.Input
{
	internal sealed class X11KeyInput : IKeyInput
	{
		private IntPtr Display;
		private readonly bool[] LastKeyState = new bool[256];
		private readonly object LockObj = new();
		private readonly DistinctKey[] KeyEnumMap = new DistinctKey[256];

		public X11KeyInput()
		{
			if (OSTailoredCode.CurrentOS != OSTailoredCode.DistinctOS.Linux)
			{
				throw new NotSupportedException("X11 is Linux only");
			}

			Display = XOpenDisplay(null);

			if (Display == IntPtr.Zero)
			{
				// There doesn't seem to be a convention for what exception type to throw in these situations. Can't use NRE. Well...
//				_ = Unsafe.AsRef<X11.Display>()!; // hmm
				// InvalidOperationException doesn't match. Exception it is. --yoshi
				throw new Exception("Could not open XDisplay");
			}

			using (new XLock(Display))
			{
				// check if we can use XKb
				int major = 1, minor = 0;
				var supportsXkb = XkbQueryExtension(Display, out _, out _, out _, ref major, ref minor);

				if (supportsXkb)
				{
					// we generally want this behavior
					XkbSetDetectableAutoRepeat(Display, true, out _);
				}

				CreateKeyMap(supportsXkb);
			}
		}

		public void Dispose()
		{
			lock (LockObj)
			{
				if (Display != IntPtr.Zero)
				{
					_ = XCloseDisplay(Display);
					Display = IntPtr.Zero;
				}
			}
		}

		public unsafe IEnumerable<KeyEvent> Update(bool handleAltKbLayouts)
		{
			lock (LockObj)
			{
				// Can't update without a display connection
				if (Display == IntPtr.Zero)
				{
					return Enumerable.Empty<KeyEvent>();
				}

				var keys = stackalloc byte[32];

				using (new XLock(Display))
				{
					// this apparently always returns 1 no matter what?
					_ = XQueryKeymap(Display, keys);
				}

				var keyEvents = new List<KeyEvent>();
				for (var keycode = 0; keycode < 256; keycode++)
				{
					var key = KeyEnumMap[keycode];
					if (key != DistinctKey.Unknown)
					{
						var keystate = (keys[keycode >> 3] >> (keycode & 0x07) & 0x01) != 0;
						if (LastKeyState[keycode] != keystate)
						{
							keyEvents.Add(new(key, pressed: keystate));
							LastKeyState[keycode] = keystate;
						}
					}
				}

				return keyEvents;
			}
		}

		private unsafe void CreateKeyMap(bool supportsXkb)
		{
			for (var i = 0; i < KeyEnumMap.Length; i++)
			{
				KeyEnumMap[i] = DistinctKey.Unknown;
			}

			if (supportsXkb)
			{
				var keyboard = XkbAllocKeyboard(Display);
				if (keyboard != null)
				{
					_ = XkbGetNames(Display, 0x3FF, keyboard);
					var names = Marshal.PtrToStructure<XkbNamesRec>(keyboard->names);

					for (int i = keyboard->min_key_code; i <= keyboard->max_key_code; i++)
					{
						var name = new string(names.keys[i].name, 0, 4);
						var key = name switch
						{
							"TLDE" => DistinctKey.OemTilde,
							"AE01" => DistinctKey.D1,
							"AE02" => DistinctKey.D2,
							"AE03" => DistinctKey.D3,
							"AE04" => DistinctKey.D4,
							"AE05" => DistinctKey.D5,
							"AE06" => DistinctKey.D6,
							"AE07" => DistinctKey.D7,
							"AE08" => DistinctKey.D8,
							"AE09" => DistinctKey.D9,
							"AE10" => DistinctKey.D0,
							"AE11" => DistinctKey.OemMinus,
							"AE12" => DistinctKey.OemPlus,
							"AD01" => DistinctKey.Q,
							"AD02" => DistinctKey.W,
							"AD03" => DistinctKey.E,
							"AD04" => DistinctKey.R,
							"AD05" => DistinctKey.T,
							"AD06" => DistinctKey.Y,
							"AD07" => DistinctKey.U,
							"AD08" => DistinctKey.I,
							"AD09" => DistinctKey.O,
							"AD10" => DistinctKey.P,
							"AD11" => DistinctKey.OemOpenBrackets,
							"AD12" => DistinctKey.OemCloseBrackets,
							"AC01" => DistinctKey.A,
							"AC02" => DistinctKey.S,
							"AC03" => DistinctKey.D,
							"AC04" => DistinctKey.F,
							"AC05" => DistinctKey.G,
							"AC06" => DistinctKey.H,
							"AC07" => DistinctKey.J,
							"AC08" => DistinctKey.K,
							"AC09" => DistinctKey.L,
							"AC10" => DistinctKey.OemSemicolon,
							"AC11" => DistinctKey.OemQuotes,
							"AB01" => DistinctKey.Z,
							"AB02" => DistinctKey.X,
							"AB03" => DistinctKey.C,
							"AB04" => DistinctKey.V,
							"AB05" => DistinctKey.B,
							"AB06" => DistinctKey.N,
							"AB07" => DistinctKey.M,
							"AB08" => DistinctKey.OemComma,
							"AB09" => DistinctKey.OemPeriod,
							"AB10" => DistinctKey.OemQuestion,
							"BKSL" => DistinctKey.OemPipe,
							_ => DistinctKey.Unknown,
						};

						KeyEnumMap[i] = key;
					}

					XkbFreeKeyboard(keyboard, 0, true);
				}
			}

			for (var i = 0; i < KeyEnumMap.Length; i++)
			{
				if (KeyEnumMap[i] == DistinctKey.Unknown)
				{
					if (supportsXkb)
					{
						var keysym = XkbKeycodeToKeysym(Display, i, 0, 1);
						var key = keysym switch
						{
							Keysym.KP_0 => DistinctKey.NumPad0,
							Keysym.KP_1 => DistinctKey.NumPad1,
							Keysym.KP_2 => DistinctKey.NumPad2,
							Keysym.KP_3 => DistinctKey.NumPad3,
							Keysym.KP_4 => DistinctKey.NumPad4,
							Keysym.KP_5 => DistinctKey.NumPad5,
							Keysym.KP_6 => DistinctKey.NumPad6,
							Keysym.KP_7 => DistinctKey.NumPad7,
							Keysym.KP_8 => DistinctKey.NumPad8,
							Keysym.KP_9 => DistinctKey.NumPad9,
							Keysym.KP_Separator => DistinctKey.Separator,
							Keysym.KP_Decimal => DistinctKey.Decimal,
							Keysym.KP_Enter => DistinctKey.NumPadEnter,
							_ => DistinctKey.Unknown,
						};

						if (key == DistinctKey.Unknown)
						{
							keysym = XkbKeycodeToKeysym(Display, i, 0, 0);
							key = KeysymEnumMap.GetValueOrDefault(keysym, DistinctKey.Unknown);
						}

						KeyEnumMap[i] = key;
					}
					else
					{
						var e = new XKeyEvent
						{
							display = Display,
							keycode = i,
						};

						var keysym = XLookupKeysym(ref e, 0);
						var key = KeysymEnumMap.GetValueOrDefault(keysym, DistinctKey.Unknown);

						if (key == DistinctKey.Unknown)
						{
							keysym = XLookupKeysym(ref e, 1);
							key = KeysymEnumMap.GetValueOrDefault(keysym, DistinctKey.Unknown);
						}

						KeyEnumMap[i] = key;
					}
				}
			}
		}

		private static readonly IReadOnlyDictionary<Keysym, DistinctKey> KeysymEnumMap = new Dictionary<Keysym, DistinctKey>
		{
			[Keysym.Escape] = DistinctKey.Escape,
			[Keysym.Return] = DistinctKey.Return,
			[Keysym.space] = DistinctKey.Space,
			[Keysym.BackSpace] = DistinctKey.Back,
			[Keysym.Shift_L] = DistinctKey.LeftShift,
			[Keysym.Shift_R] = DistinctKey.RightShift,
			[Keysym.Alt_L] = DistinctKey.LeftAlt,
			[Keysym.Alt_R] = DistinctKey.RightAlt,
			[Keysym.Control_L] = DistinctKey.LeftCtrl,
			[Keysym.Control_R] = DistinctKey.RightCtrl,
			[Keysym.Super_L] = DistinctKey.LWin,
			[Keysym.Super_R] = DistinctKey.RWin,
			[Keysym.Meta_L] = DistinctKey.LWin,
			[Keysym.Meta_R] = DistinctKey.RWin,
			[Keysym.ISO_Level3_Shift] = DistinctKey.RightAlt,
			[Keysym.Menu] = DistinctKey.Apps,
			[Keysym.Tab] = DistinctKey.Tab,
			[Keysym.minus] = DistinctKey.OemMinus,
			[Keysym.plus] = DistinctKey.OemPlus,
			[Keysym.equal] = DistinctKey.OemPlus,
			[Keysym.Caps_Lock] = DistinctKey.CapsLock,
			[Keysym.Num_Lock] = DistinctKey.NumLock,
			[Keysym.F1] = DistinctKey.F1,
			[Keysym.F2] = DistinctKey.F2,
			[Keysym.F3] = DistinctKey.F3,
			[Keysym.F4] = DistinctKey.F4,
			[Keysym.F5] = DistinctKey.F5,
			[Keysym.F6] = DistinctKey.F6,
			[Keysym.F7] = DistinctKey.F7,
			[Keysym.F8] = DistinctKey.F8,
			[Keysym.F9] = DistinctKey.F9,
			[Keysym.F10] = DistinctKey.F10,
			[Keysym.F11] = DistinctKey.F11,
			[Keysym.F12] = DistinctKey.F12,
			[Keysym.F13] = DistinctKey.F13,
			[Keysym.F14] = DistinctKey.F14,
			[Keysym.F15] = DistinctKey.F15,
			[Keysym.F16] = DistinctKey.F16,
			[Keysym.F17] = DistinctKey.F17,
			[Keysym.F18] = DistinctKey.F18,
			[Keysym.F19] = DistinctKey.F19,
			[Keysym.F20] = DistinctKey.F20,
			[Keysym.F21] = DistinctKey.F21,
			[Keysym.F22] = DistinctKey.F22,
			[Keysym.F23] = DistinctKey.F23,
			[Keysym.F24] = DistinctKey.F24,
			[Keysym.A] = DistinctKey.A,
			[Keysym.a] = DistinctKey.A,
			[Keysym.B] = DistinctKey.B,
			[Keysym.b] = DistinctKey.B,
			[Keysym.C] = DistinctKey.C,
			[Keysym.c] = DistinctKey.C,
			[Keysym.D] = DistinctKey.D,
			[Keysym.d] = DistinctKey.D,
			[Keysym.E] = DistinctKey.E,
			[Keysym.e] = DistinctKey.E,
			[Keysym.F] = DistinctKey.F,
			[Keysym.f] = DistinctKey.F,
			[Keysym.G] = DistinctKey.G,
			[Keysym.g] = DistinctKey.G,
			[Keysym.H] = DistinctKey.H,
			[Keysym.h] = DistinctKey.H,
			[Keysym.I] = DistinctKey.I,
			[Keysym.i] = DistinctKey.I,
			[Keysym.J] = DistinctKey.J,
			[Keysym.j] = DistinctKey.J,
			[Keysym.K] = DistinctKey.K,
			[Keysym.k] = DistinctKey.K,
			[Keysym.L] = DistinctKey.L,
			[Keysym.l] = DistinctKey.L,
			[Keysym.M] = DistinctKey.M,
			[Keysym.m] = DistinctKey.M,
			[Keysym.N] = DistinctKey.N,
			[Keysym.n] = DistinctKey.N,
			[Keysym.O] = DistinctKey.O,
			[Keysym.o] = DistinctKey.O,
			[Keysym.P] = DistinctKey.P,
			[Keysym.p] = DistinctKey.P,
			[Keysym.Q] = DistinctKey.Q,
			[Keysym.q] = DistinctKey.Q,
			[Keysym.R] = DistinctKey.R,
			[Keysym.r] = DistinctKey.R,
			[Keysym.S] = DistinctKey.S,
			[Keysym.s] = DistinctKey.S,
			[Keysym.T] = DistinctKey.T,
			[Keysym.t] = DistinctKey.T,
			[Keysym.U] = DistinctKey.U,
			[Keysym.u] = DistinctKey.U,
			[Keysym.V] = DistinctKey.V,
			[Keysym.v] = DistinctKey.V,
			[Keysym.W] = DistinctKey.W,
			[Keysym.w] = DistinctKey.W,
			[Keysym.X] = DistinctKey.X,
			[Keysym.x] = DistinctKey.X,
			[Keysym.Y] = DistinctKey.Y,
			[Keysym.y] = DistinctKey.Y,
			[Keysym.Z] = DistinctKey.Z,
			[Keysym.z] = DistinctKey.Z,
			[Keysym.Number0] = DistinctKey.D0,
			[Keysym.Number1] = DistinctKey.D1,
			[Keysym.Number2] = DistinctKey.D2,
			[Keysym.Number3] = DistinctKey.D3,
			[Keysym.Number4] = DistinctKey.D4,
			[Keysym.Number5] = DistinctKey.D5,
			[Keysym.Number6] = DistinctKey.D6,
			[Keysym.Number7] = DistinctKey.D7,
			[Keysym.Number8] = DistinctKey.D8,
			[Keysym.Number9] = DistinctKey.D9,
			[Keysym.KP_0] = DistinctKey.NumPad0,
			[Keysym.KP_1] = DistinctKey.NumPad1,
			[Keysym.KP_2] = DistinctKey.NumPad2,
			[Keysym.KP_3] = DistinctKey.NumPad3,
			[Keysym.KP_4] = DistinctKey.NumPad4,
			[Keysym.KP_5] = DistinctKey.NumPad5,
			[Keysym.KP_6] = DistinctKey.NumPad6,
			[Keysym.KP_7] = DistinctKey.NumPad7,
			[Keysym.KP_8] = DistinctKey.NumPad8,
			[Keysym.KP_9] = DistinctKey.NumPad9,
			[Keysym.Pause] = DistinctKey.Pause,
			[Keysym.Break] = DistinctKey.Pause,
			[Keysym.Scroll_Lock] = DistinctKey.Scroll,
			[Keysym.Insert] = DistinctKey.Insert,
			[Keysym.Print] = DistinctKey.PrintScreen,
			[Keysym.Sys_Req] = DistinctKey.PrintScreen,
			[Keysym.backslash] = DistinctKey.OemBackslash,
			[Keysym.bar] = DistinctKey.OemBackslash,
			[Keysym.braceleft] = DistinctKey.OemOpenBrackets,
			[Keysym.bracketleft] = DistinctKey.OemOpenBrackets,
			[Keysym.braceright] = DistinctKey.OemCloseBrackets,
			[Keysym.bracketright] = DistinctKey.OemCloseBrackets,
			[Keysym.colon] = DistinctKey.OemSemicolon,
			[Keysym.semicolon] = DistinctKey.OemSemicolon,
			[Keysym.quoteright] = DistinctKey.OemQuotes,
			[Keysym.quotedbl] = DistinctKey.OemQuotes,
			[Keysym.quoteleft] = DistinctKey.OemTilde,
			[Keysym.asciitilde] = DistinctKey.OemTilde,
			[Keysym.comma] = DistinctKey.OemComma,
			[Keysym.less] = DistinctKey.OemComma,
			[Keysym.period] = DistinctKey.OemPeriod,
			[Keysym.greater] = DistinctKey.OemPeriod,
			[Keysym.slash] = DistinctKey.OemQuestion,
			[Keysym.question] = DistinctKey.OemQuestion,
			[Keysym.Left] = DistinctKey.Left,
			[Keysym.Down] = DistinctKey.Down,
			[Keysym.Right] = DistinctKey.Right,
			[Keysym.Up] = DistinctKey.Up,
			[Keysym.Delete] = DistinctKey.Delete,
			[Keysym.Home] = DistinctKey.Home,
			[Keysym.End] = DistinctKey.End,
			[Keysym.Page_Up] = DistinctKey.PageUp,
			[Keysym.Page_Down] = DistinctKey.PageDown,
			[Keysym.KP_Add] = DistinctKey.Add,
			[Keysym.KP_Subtract] = DistinctKey.Subtract,
			[Keysym.KP_Multiply] = DistinctKey.Multiply,
			[Keysym.KP_Divide] = DistinctKey.Divide,
			[Keysym.KP_Decimal] = DistinctKey.Decimal,
			[Keysym.KP_Insert] = DistinctKey.NumPad0,
			[Keysym.KP_End] = DistinctKey.NumPad1,
			[Keysym.KP_Down] = DistinctKey.NumPad2,
			[Keysym.KP_Page_Down] = DistinctKey.NumPad3,
			[Keysym.KP_Left] = DistinctKey.NumPad4,
			[Keysym.KP_Right] = DistinctKey.NumPad6,
			[Keysym.KP_Home] = DistinctKey.NumPad7,
			[Keysym.KP_Up] = DistinctKey.NumPad8,
			[Keysym.KP_Page_Up] = DistinctKey.NumPad9,
			[Keysym.KP_Delete] = DistinctKey.Decimal,
			[Keysym.KP_Enter] = DistinctKey.NumPadEnter,
		};
	}
}
