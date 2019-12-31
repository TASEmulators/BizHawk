using System.Collections.Generic;
using OpenTK.Input;

namespace BizHawk.Client.EmuHawk
{
	public static class OTK_Keyboard
	{
		private static readonly Dictionary<Key, SlimDX.DirectInput.Key> KeyEnumMap = new Dictionary<Key, SlimDX.DirectInput.Key>
		{
			// A-Z
			{Key.A, SlimDX.DirectInput.Key.A}, {Key.B, SlimDX.DirectInput.Key.B}, {Key.C, SlimDX.DirectInput.Key.C}, {Key.D, SlimDX.DirectInput.Key.D}, {Key.E, SlimDX.DirectInput.Key.E}, {Key.F, SlimDX.DirectInput.Key.F}, {Key.G, SlimDX.DirectInput.Key.G}, {Key.H, SlimDX.DirectInput.Key.H}, {Key.I, SlimDX.DirectInput.Key.I}, {Key.J, SlimDX.DirectInput.Key.J}, {Key.K, SlimDX.DirectInput.Key.K}, {Key.L, SlimDX.DirectInput.Key.L}, {Key.M, SlimDX.DirectInput.Key.M}, {Key.N, SlimDX.DirectInput.Key.N}, {Key.O, SlimDX.DirectInput.Key.O}, {Key.P, SlimDX.DirectInput.Key.P}, {Key.Q, SlimDX.DirectInput.Key.Q}, {Key.R, SlimDX.DirectInput.Key.R}, {Key.S, SlimDX.DirectInput.Key.S}, {Key.T, SlimDX.DirectInput.Key.T}, {Key.U, SlimDX.DirectInput.Key.U}, {Key.V, SlimDX.DirectInput.Key.V}, {Key.W, SlimDX.DirectInput.Key.W}, {Key.X, SlimDX.DirectInput.Key.X}, {Key.Y, SlimDX.DirectInput.Key.Y}, {Key.Z, SlimDX.DirectInput.Key.Z},
			// 0-9
			{Key.Number1, SlimDX.DirectInput.Key.D1}, {Key.Number2, SlimDX.DirectInput.Key.D2}, {Key.Number3, SlimDX.DirectInput.Key.D3}, {Key.Number4, SlimDX.DirectInput.Key.D4}, {Key.Number5, SlimDX.DirectInput.Key.D5}, {Key.Number6, SlimDX.DirectInput.Key.D6}, {Key.Number7, SlimDX.DirectInput.Key.D7}, {Key.Number8, SlimDX.DirectInput.Key.D8}, {Key.Number9, SlimDX.DirectInput.Key.D9}, {Key.Number0, SlimDX.DirectInput.Key.D0},
			// misc. printables (ASCII order)
			{Key.Space, SlimDX.DirectInput.Key.Space}, {Key.Quote, SlimDX.DirectInput.Key.Apostrophe}, {Key.Comma, SlimDX.DirectInput.Key.Comma}, {Key.Minus, SlimDX.DirectInput.Key.Minus}, {Key.Period, SlimDX.DirectInput.Key.Period}, {Key.Slash, SlimDX.DirectInput.Key.Slash}, {Key.Semicolon, SlimDX.DirectInput.Key.Semicolon}, {Key.Plus, SlimDX.DirectInput.Key.Equals}, {Key.BracketLeft, SlimDX.DirectInput.Key.LeftBracket}, {Key.BackSlash, SlimDX.DirectInput.Key.Backslash}, {Key.BracketRight, SlimDX.DirectInput.Key.RightBracket}, {Key.Tilde, SlimDX.DirectInput.Key.Grave},
			// misc. (alphabetically)
			{Key.BackSpace, SlimDX.DirectInput.Key.Backspace}, {Key.CapsLock, SlimDX.DirectInput.Key.CapsLock}, {Key.Delete, SlimDX.DirectInput.Key.Delete}, {Key.Down, SlimDX.DirectInput.Key.DownArrow}, {Key.End, SlimDX.DirectInput.Key.End}, {Key.Enter, SlimDX.DirectInput.Key.Return}, {Key.Escape, SlimDX.DirectInput.Key.Escape}, {Key.Home, SlimDX.DirectInput.Key.Home}, {Key.Insert, SlimDX.DirectInput.Key.Insert}, {Key.Left, SlimDX.DirectInput.Key.LeftArrow}, {Key.PageDown, SlimDX.DirectInput.Key.PageDown}, {Key.PageUp, SlimDX.DirectInput.Key.PageUp}, {Key.Pause, SlimDX.DirectInput.Key.Pause}, {Key.Right, SlimDX.DirectInput.Key.RightArrow}, {Key.ScrollLock, SlimDX.DirectInput.Key.ScrollLock}, {Key.Tab, SlimDX.DirectInput.Key.Tab}, {Key.Up, SlimDX.DirectInput.Key.UpArrow},
			// modifier
			{Key.WinLeft, SlimDX.DirectInput.Key.LeftWindowsKey}, {Key.WinRight, SlimDX.DirectInput.Key.RightWindowsKey}, {Key.ControlLeft, SlimDX.DirectInput.Key.LeftControl}, {Key.ControlRight, SlimDX.DirectInput.Key.RightControl}, {Key.AltLeft, SlimDX.DirectInput.Key.LeftAlt}, {Key.AltRight, SlimDX.DirectInput.Key.RightAlt}, {Key.ShiftLeft, SlimDX.DirectInput.Key.LeftShift}, {Key.ShiftRight, SlimDX.DirectInput.Key.RightShift},

			// function
			{Key.F1, SlimDX.DirectInput.Key.F1}, {Key.F2, SlimDX.DirectInput.Key.F2}, {Key.F3, SlimDX.DirectInput.Key.F3}, {Key.F4, SlimDX.DirectInput.Key.F4}, {Key.F5, SlimDX.DirectInput.Key.F5}, {Key.F6, SlimDX.DirectInput.Key.F6}, {Key.F7, SlimDX.DirectInput.Key.F7}, {Key.F8, SlimDX.DirectInput.Key.F8}, {Key.F9, SlimDX.DirectInput.Key.F9}, {Key.F10, SlimDX.DirectInput.Key.F10}, {Key.F11, SlimDX.DirectInput.Key.F11}, {Key.F12, SlimDX.DirectInput.Key.F12},
			// keypad (alphabetically)
			{Key.Keypad0, SlimDX.DirectInput.Key.NumberPad0}, {Key.Keypad1, SlimDX.DirectInput.Key.NumberPad1}, {Key.Keypad2, SlimDX.DirectInput.Key.NumberPad2}, {Key.Keypad3, SlimDX.DirectInput.Key.NumberPad3}, {Key.Keypad4, SlimDX.DirectInput.Key.NumberPad4}, {Key.Keypad5, SlimDX.DirectInput.Key.NumberPad5}, {Key.Keypad6, SlimDX.DirectInput.Key.NumberPad6}, {Key.Keypad7, SlimDX.DirectInput.Key.NumberPad7}, {Key.Keypad8, SlimDX.DirectInput.Key.NumberPad8}, {Key.Keypad9, SlimDX.DirectInput.Key.NumberPad9}, {Key.KeypadAdd, SlimDX.DirectInput.Key.NumberPadPlus}, {Key.KeypadDecimal, SlimDX.DirectInput.Key.NumberPadPeriod}, {Key.KeypadDivide, SlimDX.DirectInput.Key.NumberPadSlash}, {Key.KeypadEnter, SlimDX.DirectInput.Key.NumberPadEnter}, {Key.KeypadMultiply, SlimDX.DirectInput.Key.NumberPadStar}, {Key.KeypadSubtract, SlimDX.DirectInput.Key.NumberPadMinus}
		};

		private static readonly List<KeyInput.KeyEvent> EventList = new List<KeyInput.KeyEvent>();
		private static KeyboardState _kbState;

		public static void Initialize ()
		{
			_kbState = Keyboard.GetState();
		}

		public static IEnumerable<KeyInput.KeyEvent> Update ()
		{
			EventList.Clear();
			var lastState = _kbState;
			try
			{
				_kbState = Keyboard.GetState();
				foreach (KeyValuePair<Key, SlimDX.DirectInput.Key> entry in KeyEnumMap)
				{
					if (lastState.IsKeyUp(entry.Key) && _kbState.IsKeyDown(entry.Key))
						EventList.Add(new KeyInput.KeyEvent { Key = entry.Value, Pressed = true });
					else if (lastState.IsKeyDown(entry.Key) && _kbState.IsKeyUp(entry.Key))
						EventList.Add(new KeyInput.KeyEvent { Key = entry.Value, Pressed = false });
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
			if (key == Key.ShiftLeft)
				return true;
			if (key == Key.ShiftRight)
				return true;
			if (key == Key.ControlLeft)
				return true;
			if (key == Key.ControlRight)
				return true;
			if (key == Key.AltLeft)
				return true;
			if (key == Key.AltRight)
				return true;
			return false;
		}
	}
}
