#nullable enable

using System.Collections.Generic;

using BizHawk.Client.Common;

using static BizHawk.Common.QuartzImports;

namespace BizHawk.Bizware.Input
{
	internal sealed class QuartzKeyInput : IKeyInput
	{
		private readonly bool[] LastKeyState = new bool[0x7F];

		public void Dispose()
		{
		}

		public IEnumerable<KeyEvent> Update(bool handleAltKbLayouts)
		{
			var keyEvents = new List<KeyEvent>();
			for (var keycode = 0; keycode < 0x7F; keycode++)
			{
				var keystate = CGEventSourceKeyState(
					CGEventSourceStateID.kCGEventSourceStateHIDSystemState, (CGKeyCode)keycode);

				if (LastKeyState[keycode] != keystate)
				{
					if (KeyEnumMap.TryGetValue((CGKeyCode)keycode, out var key))
					{
						keyEvents.Add(new(key, pressed: keystate));
						LastKeyState[keycode] = keystate;
					}
				}
			}

			return keyEvents;
		}

		private static readonly IReadOnlyDictionary<CGKeyCode, DistinctKey> KeyEnumMap = new Dictionary<CGKeyCode, DistinctKey>
		{
			[CGKeyCode.kVK_ANSI_A] = DistinctKey.A,
			[CGKeyCode.kVK_ANSI_S] = DistinctKey.S,
			[CGKeyCode.kVK_ANSI_D] = DistinctKey.D,
			[CGKeyCode.kVK_ANSI_F] = DistinctKey.F,
			[CGKeyCode.kVK_ANSI_H] = DistinctKey.H,
			[CGKeyCode.kVK_ANSI_G] = DistinctKey.G,
			[CGKeyCode.kVK_ANSI_Z] = DistinctKey.Z,
			[CGKeyCode.kVK_ANSI_X] = DistinctKey.X,
			[CGKeyCode.kVK_ANSI_C] = DistinctKey.C,
			[CGKeyCode.kVK_ANSI_V] = DistinctKey.V,
			[CGKeyCode.kVK_ANSI_B] = DistinctKey.B,
			[CGKeyCode.kVK_ANSI_Q] = DistinctKey.Q,
			[CGKeyCode.kVK_ANSI_W] = DistinctKey.W,
			[CGKeyCode.kVK_ANSI_E] = DistinctKey.E,
			[CGKeyCode.kVK_ANSI_R] = DistinctKey.R,
			[CGKeyCode.kVK_ANSI_Y] = DistinctKey.Y,
			[CGKeyCode.kVK_ANSI_T] = DistinctKey.T,
			[CGKeyCode.kVK_ANSI_1] = DistinctKey.D1,
			[CGKeyCode.kVK_ANSI_2] = DistinctKey.D2,
			[CGKeyCode.kVK_ANSI_3] = DistinctKey.D3,
			[CGKeyCode.kVK_ANSI_4] = DistinctKey.D4,
			[CGKeyCode.kVK_ANSI_6] = DistinctKey.D6,
			[CGKeyCode.kVK_ANSI_5] = DistinctKey.D5,
			[CGKeyCode.kVK_ANSI_Equal] = DistinctKey.OemPlus,
			[CGKeyCode.kVK_ANSI_9] = DistinctKey.D9,
			[CGKeyCode.kVK_ANSI_7] = DistinctKey.D7,
			[CGKeyCode.kVK_ANSI_Minus] = DistinctKey.OemMinus,
			[CGKeyCode.kVK_ANSI_8] = DistinctKey.D8,
			[CGKeyCode.kVK_ANSI_0] = DistinctKey.D0,
			[CGKeyCode.kVK_ANSI_RightBracket] = DistinctKey.OemCloseBrackets,
			[CGKeyCode.kVK_ANSI_O] = DistinctKey.O,
			[CGKeyCode.kVK_ANSI_U] = DistinctKey.U,
			[CGKeyCode.kVK_ANSI_LeftBracket] = DistinctKey.OemOpenBrackets,
			[CGKeyCode.kVK_ANSI_I] = DistinctKey.I,
			[CGKeyCode.kVK_ANSI_P] = DistinctKey.P,
			[CGKeyCode.kVK_Return] = DistinctKey.Return,
			[CGKeyCode.kVK_ANSI_L] = DistinctKey.L,
			[CGKeyCode.kVK_ANSI_J] = DistinctKey.J,
			[CGKeyCode.kVK_ANSI_Quote] = DistinctKey.OemQuotes,
			[CGKeyCode.kVK_ANSI_K] = DistinctKey.K,
			[CGKeyCode.kVK_ANSI_Semicolon] = DistinctKey.OemSemicolon,
			[CGKeyCode.kVK_ANSI_Backslash] = DistinctKey.OemBackslash,
			[CGKeyCode.kVK_ANSI_Comma] = DistinctKey.OemComma,
			[CGKeyCode.kVK_ANSI_Slash] = DistinctKey.OemQuestion,
			[CGKeyCode.kVK_ANSI_N] = DistinctKey.N,
			[CGKeyCode.kVK_ANSI_M] = DistinctKey.M,
			[CGKeyCode.kVK_ANSI_Period] = DistinctKey.OemPeriod,
			[CGKeyCode.kVK_Tab] = DistinctKey.Tab,
			[CGKeyCode.kVK_Space] = DistinctKey.Space,
			[CGKeyCode.kVK_ANSI_Grave] = DistinctKey.OemTilde,
			[CGKeyCode.kVK_Delete] = DistinctKey.Delete,
			[CGKeyCode.kVK_Escape] = DistinctKey.Escape,
			[CGKeyCode.kVK_RightCommand] = DistinctKey.RWin,
			[CGKeyCode.kVK_Command] = DistinctKey.LWin,
			[CGKeyCode.kVK_Shift] = DistinctKey.LeftShift,
			[CGKeyCode.kVK_CapsLock] = DistinctKey.CapsLock,
			[CGKeyCode.kVK_Option] = DistinctKey.LeftAlt,
			[CGKeyCode.kVK_Control] = DistinctKey.LeftCtrl,
			[CGKeyCode.kVK_RightShift] = DistinctKey.RightShift,
			[CGKeyCode.kVK_RightOption] = DistinctKey.RightAlt,
			[CGKeyCode.kVK_RightControl] = DistinctKey.RightCtrl,
			// [CGKeyCode.kVK_Function] = DistinctKey.,
			[CGKeyCode.kVK_F17] = DistinctKey.F17,
			[CGKeyCode.kVK_ANSI_KeypadDecimal] = DistinctKey.Decimal,
			[CGKeyCode.kVK_ANSI_KeypadMultiply] = DistinctKey.Multiply,
			[CGKeyCode.kVK_ANSI_KeypadPlus] = DistinctKey.Add,
			[CGKeyCode.kVK_ANSI_KeypadClear] = DistinctKey.Clear,
			[CGKeyCode.kVK_VolumeUp] = DistinctKey.VolumeUp,
			[CGKeyCode.kVK_VolumeDown] = DistinctKey.VolumeDown,
			[CGKeyCode.kVK_Mute] = DistinctKey.VolumeMute,
			[CGKeyCode.kVK_ANSI_KeypadDivide] = DistinctKey.Divide,
			[CGKeyCode.kVK_ANSI_KeypadEnter] = DistinctKey.NumPadEnter,
			[CGKeyCode.kVK_ANSI_KeypadMinus] = DistinctKey.Subtract,
			[CGKeyCode.kVK_F18] = DistinctKey.F18,
			[CGKeyCode.kVK_F19] = DistinctKey.F19,
			[CGKeyCode.kVK_ANSI_KeypadEquals] = DistinctKey.OemPlus,
			[CGKeyCode.kVK_ANSI_Keypad0] = DistinctKey.NumPad0,
			[CGKeyCode.kVK_ANSI_Keypad1] = DistinctKey.NumPad1,
			[CGKeyCode.kVK_ANSI_Keypad2] = DistinctKey.NumPad2,
			[CGKeyCode.kVK_ANSI_Keypad3] = DistinctKey.NumPad3,
			[CGKeyCode.kVK_ANSI_Keypad4] = DistinctKey.NumPad4,
			[CGKeyCode.kVK_ANSI_Keypad5] = DistinctKey.NumPad5,
			[CGKeyCode.kVK_ANSI_Keypad6] = DistinctKey.NumPad6,
			[CGKeyCode.kVK_ANSI_Keypad7] = DistinctKey.NumPad7,
			[CGKeyCode.kVK_F20] = DistinctKey.F20,
			[CGKeyCode.kVK_ANSI_Keypad8] = DistinctKey.NumPad8,
			[CGKeyCode.kVK_ANSI_Keypad9] = DistinctKey.NumPad9,
			[CGKeyCode.kVK_F5] = DistinctKey.F5,
			[CGKeyCode.kVK_F6] = DistinctKey.F6,
			[CGKeyCode.kVK_F7] = DistinctKey.F7,
			[CGKeyCode.kVK_F3] = DistinctKey.F3,
			[CGKeyCode.kVK_F8] = DistinctKey.F8,
			[CGKeyCode.kVK_F9] = DistinctKey.F9,
			[CGKeyCode.kVK_F11] = DistinctKey.F11,
			[CGKeyCode.kVK_F13] = DistinctKey.F13,
			[CGKeyCode.kVK_F16] = DistinctKey.F16,
			[CGKeyCode.kVK_F14] = DistinctKey.F14,
			[CGKeyCode.kVK_F10] = DistinctKey.F10,
			[CGKeyCode.kVK_ContextMenu] = DistinctKey.Apps,
			[CGKeyCode.kVK_F12] = DistinctKey.F12,
			[CGKeyCode.kVK_F15] = DistinctKey.F15,
			[CGKeyCode.kVK_Help] = DistinctKey.Help,
			[CGKeyCode.kVK_Home] = DistinctKey.Home,
			[CGKeyCode.kVK_PageUp] = DistinctKey.PageUp,
			[CGKeyCode.kVK_ForwardDelete] = DistinctKey.Delete,
			[CGKeyCode.kVK_F4] = DistinctKey.F4,
			[CGKeyCode.kVK_End] = DistinctKey.End,
			[CGKeyCode.kVK_F2] = DistinctKey.F2,
			[CGKeyCode.kVK_PageDown] = DistinctKey.PageDown,
			[CGKeyCode.kVK_F1] = DistinctKey.F1,
			[CGKeyCode.kVK_LeftArrow] = DistinctKey.Left,
			[CGKeyCode.kVK_RightArrow] = DistinctKey.Right,
			[CGKeyCode.kVK_DownArrow] = DistinctKey.Down,
			[CGKeyCode.kVK_UpArrow] = DistinctKey.Up,
		};
	}
}
