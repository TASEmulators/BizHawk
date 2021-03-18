using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using BizHawk.Client.Common;

using OpenTK.Input;

namespace BizHawk.Bizware.OpenTK3
{
	public static class OTK_Keyboard
	{
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

		private static KeyboardState _kbState;

		public static void Initialize ()
		{
			_kbState = Keyboard.GetState();
		}

		public static IEnumerable<KeyEvent> Update()
		{
			KeyboardState newState;
			try
			{
				newState = Keyboard.GetState();
			}
			catch
			{
				// OpenTK's keyboard class isn't thread safe.
				// In rare cases (sometimes it takes up to 10 minutes to occur) it will
				// be updating the keyboard state when we call GetState() and choke.
				// Until I fix OpenTK, it's fine to just swallow it because input continues working.
				if (Debugger.IsAttached) Console.WriteLine("OpenTK Keyboard thread is angry.");
				return Enumerable.Empty<KeyEvent>();
			}

			var lastState = _kbState;
			_kbState = newState;
			if (lastState == _kbState) return Enumerable.Empty<KeyEvent>();

			var eventList = new List<KeyEvent>();
			for (var i = 1; i < 131; i++)
			{
				var key = (Key) i;
				if (lastState.IsKeyUp(key) && _kbState.IsKeyDown(key))
				{
					eventList.Add(new KeyEvent(KeyEnumMap[i], pressed: true));
				}
				else if (lastState.IsKeyDown(key) && _kbState.IsKeyUp(key))
				{
					eventList.Add(new KeyEvent(KeyEnumMap[i], pressed: false));
				}
			}
			return eventList;
		}
	}
}
