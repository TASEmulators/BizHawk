using System;
using System.Collections.Generic;

using BizHawk.Client.Common;

using SlimDX;
using SlimDX.DirectInput;

namespace BizHawk.Bizware.DirectX
{
	internal static class KeyInput
	{
		private static readonly object SyncObj = new object();
		private static readonly List<KeyEvent> EventList = new List<KeyEvent>();
		private static DirectInput _directInput;
		private static Keyboard _keyboard;

		public static void Initialize(IntPtr mainFormHandle)
		{
			lock (SyncObj)
			{
				Cleanup();

				_directInput = new DirectInput();

				_keyboard = new Keyboard(_directInput);
				_keyboard.SetCooperativeLevel(mainFormHandle, CooperativeLevel.Background | CooperativeLevel.Nonexclusive);
				_keyboard.Properties.BufferSize = 8;
			}
		}

		public static void Cleanup()
		{
			lock (SyncObj)
			{
				if (_keyboard != null)
				{
					_keyboard.Dispose();
					_keyboard = null;
				}

				if (_directInput != null)
				{
					_directInput.Dispose();
					_directInput = null;
				}
			}
		}

		internal static readonly Dictionary<Key, OpenTK.Input.Key> KeyEnumMap = new Dictionary<Key, OpenTK.Input.Key>
		{
			// A-Z
			{Key.A, OpenTK.Input.Key.A}, {Key.B, OpenTK.Input.Key.B}, {Key.C, OpenTK.Input.Key.C}, {Key.D, OpenTK.Input.Key.D}, {Key.E, OpenTK.Input.Key.E}, {Key.F, OpenTK.Input.Key.F}, {Key.G, OpenTK.Input.Key.G}, {Key.H, OpenTK.Input.Key.H}, {Key.I, OpenTK.Input.Key.I}, {Key.J, OpenTK.Input.Key.J}, {Key.K, OpenTK.Input.Key.K}, {Key.L, OpenTK.Input.Key.L}, {Key.M, OpenTK.Input.Key.M}, {Key.N, OpenTK.Input.Key.N}, {Key.O, OpenTK.Input.Key.O}, {Key.P, OpenTK.Input.Key.P}, {Key.Q, OpenTK.Input.Key.Q}, {Key.R, OpenTK.Input.Key.R}, {Key.S, OpenTK.Input.Key.S}, {Key.T, OpenTK.Input.Key.T}, {Key.U, OpenTK.Input.Key.U}, {Key.V, OpenTK.Input.Key.V}, {Key.W, OpenTK.Input.Key.W}, {Key.X, OpenTK.Input.Key.X}, {Key.Y, OpenTK.Input.Key.Y}, {Key.Z, OpenTK.Input.Key.Z},
			// 0-9
			{Key.D1, OpenTK.Input.Key.Number1}, {Key.D2, OpenTK.Input.Key.Number2}, {Key.D3, OpenTK.Input.Key.Number3}, {Key.D4, OpenTK.Input.Key.Number4}, {Key.D5, OpenTK.Input.Key.Number5}, {Key.D6, OpenTK.Input.Key.Number6}, {Key.D7, OpenTK.Input.Key.Number7}, {Key.D8, OpenTK.Input.Key.Number8}, {Key.D9, OpenTK.Input.Key.Number9}, {Key.D0, OpenTK.Input.Key.Number0},
			// misc. printables (ASCII order)
			{Key.Space, OpenTK.Input.Key.Space}, {Key.Apostrophe, OpenTK.Input.Key.Quote}, {Key.Comma, OpenTK.Input.Key.Comma}, {Key.Minus, OpenTK.Input.Key.Minus}, {Key.Period, OpenTK.Input.Key.Period}, {Key.Slash, OpenTK.Input.Key.Slash}, {Key.Semicolon, OpenTK.Input.Key.Semicolon}, {Key.Equals, OpenTK.Input.Key.Plus}, {Key.LeftBracket, OpenTK.Input.Key.BracketLeft}, {Key.Backslash, OpenTK.Input.Key.BackSlash}, {Key.RightBracket, OpenTK.Input.Key.BracketRight}, {Key.Grave, OpenTK.Input.Key.Tilde},
			// misc. (alphabetically)
			{Key.Backspace, OpenTK.Input.Key.BackSpace}, {Key.CapsLock, OpenTK.Input.Key.CapsLock}, {Key.Delete, OpenTK.Input.Key.Delete}, {Key.DownArrow, OpenTK.Input.Key.Down}, {Key.End, OpenTK.Input.Key.End}, {Key.Return, OpenTK.Input.Key.Enter}, {Key.Escape, OpenTK.Input.Key.Escape}, {Key.Home, OpenTK.Input.Key.Home}, {Key.Insert, OpenTK.Input.Key.Insert}, {Key.LeftArrow, OpenTK.Input.Key.Left}, {Key.Oem102, OpenTK.Input.Key.NonUSBackSlash}, {Key.NumberLock, OpenTK.Input.Key.NumLock}, {Key.PageDown, OpenTK.Input.Key.PageDown}, {Key.PageUp, OpenTK.Input.Key.PageUp}, {Key.Pause, OpenTK.Input.Key.Pause}, {Key.PrintScreen, OpenTK.Input.Key.PrintScreen}, {Key.RightArrow, OpenTK.Input.Key.Right}, {Key.ScrollLock, OpenTK.Input.Key.ScrollLock}, {Key.Tab, OpenTK.Input.Key.Tab}, {Key.UpArrow, OpenTK.Input.Key.Up},
			// modifier
			{Key.LeftWindowsKey, OpenTK.Input.Key.WinLeft}, {Key.RightWindowsKey, OpenTK.Input.Key.WinRight}, {Key.LeftControl, OpenTK.Input.Key.ControlLeft}, {Key.RightControl, OpenTK.Input.Key.ControlRight}, {Key.LeftAlt, OpenTK.Input.Key.AltLeft}, {Key.RightAlt, OpenTK.Input.Key.AltRight}, {Key.LeftShift, OpenTK.Input.Key.ShiftLeft}, {Key.RightShift, OpenTK.Input.Key.ShiftRight},

			// function
			{Key.F1, OpenTK.Input.Key.F1}, {Key.F2, OpenTK.Input.Key.F2}, {Key.F3, OpenTK.Input.Key.F3}, {Key.F4, OpenTK.Input.Key.F4}, {Key.F5, OpenTK.Input.Key.F5}, {Key.F6, OpenTK.Input.Key.F6}, {Key.F7, OpenTK.Input.Key.F7}, {Key.F8, OpenTK.Input.Key.F8}, {Key.F9, OpenTK.Input.Key.F9}, {Key.F10, OpenTK.Input.Key.F10}, {Key.F11, OpenTK.Input.Key.F11}, {Key.F12, OpenTK.Input.Key.F12},
			// keypad (alphabetically)
			{Key.NumberPad0, OpenTK.Input.Key.Keypad0}, {Key.NumberPad1, OpenTK.Input.Key.Keypad1}, {Key.NumberPad2, OpenTK.Input.Key.Keypad2}, {Key.NumberPad3, OpenTK.Input.Key.Keypad3}, {Key.NumberPad4, OpenTK.Input.Key.Keypad4}, {Key.NumberPad5, OpenTK.Input.Key.Keypad5}, {Key.NumberPad6, OpenTK.Input.Key.Keypad6}, {Key.NumberPad7, OpenTK.Input.Key.Keypad7}, {Key.NumberPad8, OpenTK.Input.Key.Keypad8}, {Key.NumberPad9, OpenTK.Input.Key.Keypad9}, {Key.NumberPadPlus, OpenTK.Input.Key.KeypadAdd}, {Key.NumberPadPeriod, OpenTK.Input.Key.KeypadDecimal}, {Key.NumberPadSlash, OpenTK.Input.Key.KeypadDivide}, {Key.NumberPadEnter, OpenTK.Input.Key.KeypadEnter}, {Key.NumberPadStar, OpenTK.Input.Key.KeypadMultiply}, {Key.NumberPadMinus, OpenTK.Input.Key.KeypadSubtract}
		};

		public static IEnumerable<KeyEvent> Update(Config config)
		{
			OpenTK.Input.Key Mapped(Key k) => KeyEnumMap[config.HandleAlternateKeyboardLayouts ? KeyboardMapping.Handle(k) : k];

			lock (SyncObj)
			{
				EventList.Clear();

				if (_keyboard == null || _keyboard.Acquire().IsFailure || _keyboard.Poll().IsFailure)
					return EventList;

				for (; ; )
				{
					var events = _keyboard.GetBufferedData();
					if (Result.Last.IsFailure || events.Count == 0)
						break;
					foreach (var e in events)
					{
						foreach (var k in e.PressedKeys)
							EventList.Add(new KeyEvent { Key = Mapped(k), Pressed = true });
						foreach (var k in e.ReleasedKeys)
							EventList.Add(new KeyEvent { Key = Mapped(k), Pressed = false });
					}
				}

				return EventList;
			}
		}
	}
}
