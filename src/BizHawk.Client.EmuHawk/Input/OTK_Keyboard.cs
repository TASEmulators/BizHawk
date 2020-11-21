using System.Collections.Generic;

using BizHawk.Client.Common;

using OpenTK.Input;

namespace BizHawk.Client.EmuHawk
{
	public static class OTK_Keyboard
	{
		private static readonly Key[] KeyList = {
			Key.Unknown,

			// A-Z
			Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G, Key.H, Key.I, Key.J, Key.K, Key.L, Key.M, Key.N, Key.O, Key.P, Key.Q, Key.R, Key.S, Key.T, Key.U, Key.V, Key.W, Key.X, Key.Y, Key.Z,
			// 0-9
			Key.Number1, Key.Number2, Key.Number3, Key.Number4, Key.Number5, Key.Number6, Key.Number7, Key.Number8, Key.Number9, Key.Number0,
			// misc. printables (ASCII order)
			Key.Space, Key.Quote, Key.Comma, Key.Minus, Key.Period, Key.Slash, Key.Semicolon, Key.Plus, Key.BracketLeft, Key.BackSlash, Key.BracketRight, Key.Tilde,
			// misc. (alphabetically)
			Key.BackSpace, Key.CapsLock, Key.Delete, Key.Down, Key.End, Key.Enter, Key.Escape, Key.Home, Key.Insert, Key.Left, Key.Menu, Key.NonUSBackSlash, Key.NumLock, Key.PageDown, Key.PageUp, Key.Pause, Key.PrintScreen, Key.Right, Key.ScrollLock, Key.Tab, Key.Up,
			// modifier
			Key.WinLeft, Key.WinRight, Key.ControlLeft, Key.ControlRight, Key.AltLeft, Key.AltRight, Key.ShiftLeft, Key.ShiftRight,

			// function
			Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12,
			// keypad (alphabetically)
			Key.Keypad0, Key.Keypad1, Key.Keypad2, Key.Keypad3, Key.Keypad4, Key.Keypad5, Key.Keypad6, Key.Keypad7, Key.Keypad8, Key.Keypad9, Key.KeypadAdd, Key.KeypadDecimal, Key.KeypadDivide, Key.KeypadEnter, Key.KeypadMultiply, Key.KeypadSubtract
		};

		private static readonly IReadOnlyList<DistinctKey> KeyEnumMap = new List<DistinctKey>
		{
			DistinctKey.Unknown, // Unknown
			DistinctKey.LeftShift,
			DistinctKey.RightShift,
			DistinctKey.LeftCtrl,
			DistinctKey.RightCtrl,
			DistinctKey.LeftAlt,
			DistinctKey.RightAlt,
			DistinctKey.LWin,
			DistinctKey.RWin,
			DistinctKey.Apps,
			DistinctKey.F1,
			DistinctKey.F2,
			DistinctKey.F3,
			DistinctKey.F4,
			DistinctKey.F5,
			DistinctKey.F6,
			DistinctKey.F7,
			DistinctKey.F8,
			DistinctKey.F9,
			DistinctKey.F10,
			DistinctKey.F11,
			DistinctKey.F12,
			DistinctKey.F13,
			DistinctKey.F14,
			DistinctKey.F15,
			DistinctKey.F16,
			DistinctKey.F17,
			DistinctKey.F18,
			DistinctKey.F19,
			DistinctKey.F20,
			DistinctKey.F21,
			DistinctKey.F22,
			DistinctKey.F23,
			DistinctKey.F24,
			DistinctKey.Unknown, // F25
			DistinctKey.Unknown, // F26
			DistinctKey.Unknown, // F27
			DistinctKey.Unknown, // F28
			DistinctKey.Unknown, // F29
			DistinctKey.Unknown, // F30
			DistinctKey.Unknown, // F31
			DistinctKey.Unknown, // F32
			DistinctKey.Unknown, // F33
			DistinctKey.Unknown, // F34
			DistinctKey.Unknown, // F35
			DistinctKey.Up,
			DistinctKey.Down,
			DistinctKey.Left,
			DistinctKey.Right,
			DistinctKey.Return,
			DistinctKey.Escape,
			DistinctKey.Space,
			DistinctKey.Tab,
			DistinctKey.Back,
			DistinctKey.Insert,
			DistinctKey.Delete,
			DistinctKey.PageUp,
			DistinctKey.PageDown,
			DistinctKey.Home,
			DistinctKey.End,
			DistinctKey.CapsLock,
			DistinctKey.Scroll, // ScrollLock; my Scroll Lock key is only recognised by OpenTK as Pause tho --yoshi
			DistinctKey.PrintScreen,
			DistinctKey.Pause,
			DistinctKey.NumLock,
			DistinctKey.Clear,
			DistinctKey.Sleep,
			DistinctKey.NumPad0,
			DistinctKey.NumPad1,
			DistinctKey.NumPad2,
			DistinctKey.NumPad3,
			DistinctKey.NumPad4,
			DistinctKey.NumPad5,
			DistinctKey.NumPad6,
			DistinctKey.NumPad7,
			DistinctKey.NumPad8,
			DistinctKey.NumPad9,
			DistinctKey.Divide,
			DistinctKey.Multiply,
			DistinctKey.Subtract,
			DistinctKey.Add,
			DistinctKey.Decimal,
			DistinctKey.NumPadEnter,
			DistinctKey.A,
			DistinctKey.B,
			DistinctKey.C,
			DistinctKey.D,
			DistinctKey.E,
			DistinctKey.F,
			DistinctKey.G,
			DistinctKey.H,
			DistinctKey.I,
			DistinctKey.J,
			DistinctKey.K,
			DistinctKey.L,
			DistinctKey.M,
			DistinctKey.N,
			DistinctKey.O,
			DistinctKey.P,
			DistinctKey.Q,
			DistinctKey.R,
			DistinctKey.S,
			DistinctKey.T,
			DistinctKey.U,
			DistinctKey.V,
			DistinctKey.W,
			DistinctKey.X,
			DistinctKey.Y,
			DistinctKey.Z,
			DistinctKey.D0,
			DistinctKey.D1,
			DistinctKey.D2,
			DistinctKey.D3,
			DistinctKey.D4,
			DistinctKey.D5,
			DistinctKey.D6,
			DistinctKey.D7,
			DistinctKey.D8,
			DistinctKey.D9,
			DistinctKey.OemTilde,
			DistinctKey.OemMinus,
			DistinctKey.OemPlus,
			DistinctKey.OemOpenBrackets,
			DistinctKey.OemCloseBrackets,
			DistinctKey.OemSemicolon,
			DistinctKey.OemQuotes,
			DistinctKey.OemComma,
			DistinctKey.OemPeriod,
			DistinctKey.OemQuestion, // Slash
			DistinctKey.OemPipe, // BackSlash
			DistinctKey.OemBackslash, // NonUSBackSlash
		};

		private static readonly List<KeyEvent> EventList = new List<KeyEvent>();
		private static KeyboardState _kbState;

		public static void Initialize ()
		{
			_kbState = Keyboard.GetState();
		}

		public static IEnumerable<KeyEvent> Update()
		{
			EventList.Clear();
			var lastState = _kbState;
			try
			{
				_kbState = Keyboard.GetState();
				foreach (var key in KeyList)
				{
					if (lastState.IsKeyUp(key) && _kbState.IsKeyDown(key))
						EventList.Add(new KeyEvent(KeyEnumMap[(int) key], pressed: true));
					else if (lastState.IsKeyDown(key) && _kbState.IsKeyUp(key))
						EventList.Add(new KeyEvent(KeyEnumMap[(int) key], pressed: false));
				}
			}
			catch
			{
				// OpenTK's keyboard class isn't thread safe.
				// In rare cases (sometimes it takes up to 10 minutes to occur) it will
				// be updating the keyboard state when we call GetState() and choke.
				// Until I fix OpenTK, it's fine to just swallow it because input continues working.
				if (System.Diagnostics.Debugger.IsAttached)
				{
					System.Console.WriteLine("OpenTK Keyboard thread is angry.");
				}
			}
			return EventList;
		}

		public static bool IsPressed(Key key)
		{
			return _kbState.IsKeyDown(key);
		}

		public static bool ShiftModifier => IsPressed(Key.ShiftLeft) || IsPressed(Key.ShiftRight);

		public static bool CtrlModifier => IsPressed(Key.ControlLeft) || IsPressed(Key.ControlRight);

		public static bool AltModifier => IsPressed(Key.AltLeft) || IsPressed(Key.AltRight);

		public static Input.ModifierKey GetModifierKeysAsKeys()
		{
			Input.ModifierKey ret = Input.ModifierKey.None;
			if (ShiftModifier)
				ret |= Input.ModifierKey.Shift;
			if (CtrlModifier)
				ret |= Input.ModifierKey.Control;
			if (AltModifier)
				ret |= Input.ModifierKey.Alt;
			return ret;
		}
	}

	internal static class KeyExtensions
	{
		public static bool IsModifier (this Key key)
		{
			switch (key)
			{
				case Key.ShiftLeft:
				case Key.ShiftRight:
				case Key.ControlLeft:
				case Key.ControlRight:
				case Key.AltLeft:
				case Key.AltRight:
					return true;
				default:
					return false;
			}
		}
	}
}
