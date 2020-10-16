#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;

using SlimDX;
using SlimDX.DirectInput;

using static BizHawk.Common.Win32Imports;

using DInputKey = SlimDX.DirectInput.Key;
using OpenTKKey = OpenTK.Input.Key;
using WinFormsKey = System.Windows.Forms.Keys;

namespace BizHawk.Bizware.DirectX
{
	internal static class KeyInput
	{
		private static DirectInput? _directInput;

		private static Keyboard? _keyboard;

		private static readonly object _lockObj = new object();

		public static void Initialize(IntPtr mainFormHandle)
		{
			lock (_lockObj)
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
			lock (_lockObj)
			{
				_keyboard?.Dispose();
				_keyboard = null;
				_directInput?.Dispose();
				_directInput = null;
			}
		}

		public static IEnumerable<KeyEvent> Update(Config config)
		{
			OpenTKKey Mapped(DInputKey k) => KeyEnumMap[config.HandleAlternateKeyboardLayouts ? MapToRealKeyViaScanCode(k) : k];

			lock (_lockObj)
			{
				if (_keyboard == null || _keyboard.Acquire().IsFailure || _keyboard.Poll().IsFailure) return Enumerable.Empty<KeyEvent>();

				var eventList = new List<KeyEvent>();
				while (true)
				{
					var events = _keyboard.GetBufferedData();
					if (Result.Last.IsFailure || events.Count == 0) return eventList;

					foreach (var e in events)
					{
						foreach (var k in e.PressedKeys) eventList.Add(new KeyEvent { Key = Mapped(k), Pressed = true });
						foreach (var k in e.ReleasedKeys) eventList.Add(new KeyEvent { Key = Mapped(k), Pressed = false });
					}
				}
			}
		}

		private static WinFormsKey MapWin32VirtualScanCodeToVirtualKey(uint scanCode)
		{
			const uint MAPVK_VSC_TO_VK_EX = 0x03;
			return (WinFormsKey) MapVirtualKey(scanCode, MAPVK_VSC_TO_VK_EX);
		}

		private static DInputKey MapToRealKeyViaScanCode(DInputKey key)
		{
			if (key == DInputKey.Unknown) return DInputKey.Unknown;
			var scanCode = key switch
			{
				DInputKey.D0 => 0x0BU,
				DInputKey.D1 => 0x02U,
				DInputKey.D2 => 0x03U,
				DInputKey.D3 => 0x04U,
				DInputKey.D4 => 0x05U,
				DInputKey.D5 => 0x06U,
				DInputKey.D6 => 0x07U,
				DInputKey.D7 => 0x08U,
				DInputKey.D8 => 0x09U,
				DInputKey.D9 => 0x0AU,
				DInputKey.A => 0x1EU,
				DInputKey.B => 0x30U,
				DInputKey.C => 0x2EU,
				DInputKey.D => 0x20U,
				DInputKey.E => 0x12U,
				DInputKey.F => 0x21U,
				DInputKey.G => 0x22U,
				DInputKey.H => 0x23U,
				DInputKey.I => 0x17U,
				DInputKey.J => 0x24U,
				DInputKey.K => 0x25U,
				DInputKey.L => 0x26U,
				DInputKey.M => 0x32U,
				DInputKey.N => 0x31U,
				DInputKey.O => 0x18U,
				DInputKey.P => 0x19U,
				DInputKey.Q => 0x10U,
				DInputKey.R => 0x13U,
				DInputKey.S => 0x1FU,
				DInputKey.T => 0x14U,
				DInputKey.U => 0x16U,
				DInputKey.V => 0x2FU,
				DInputKey.W => 0x11U,
				DInputKey.X => 0x2DU,
				DInputKey.Y => 0x15U,
				DInputKey.Z => 0x2CU,
				DInputKey.AbntC1 => 0x73U,
				DInputKey.AbntC2 => 0x7EU,
				DInputKey.Apostrophe => 0x28U,
				DInputKey.Applications => 0xDDU,
				DInputKey.AT => 0x91U,
				DInputKey.AX => 0x96U,
				DInputKey.Backspace => 0x0EU,
				DInputKey.Backslash => 0x2BU,
				DInputKey.Calculator => 0xA1U,
				DInputKey.CapsLock => 0x3AU,
				DInputKey.Colon => 0x92U,
				DInputKey.Comma => 0x33U,
				DInputKey.Convert => 0x79U,
				DInputKey.Delete => 0xD3U,
				DInputKey.DownArrow => 0xD0U,
				DInputKey.End => 0xCFU,
				DInputKey.Equals => 0x0DU,
				DInputKey.Escape => 0x01U,
				DInputKey.F1 => 0x3BU,
				DInputKey.F2 => 0x3CU,
				DInputKey.F3 => 0x3DU,
				DInputKey.F4 => 0x3EU,
				DInputKey.F5 => 0x3FU,
				DInputKey.F6 => 0x40U,
				DInputKey.F7 => 0x41U,
				DInputKey.F8 => 0x42U,
				DInputKey.F9 => 0x43U,
				DInputKey.F10 => 0x44U,
				DInputKey.F11 => 0x57U,
				DInputKey.F12 => 0x58U,
				DInputKey.F13 => 0x64U,
				DInputKey.F14 => 0x65U,
				DInputKey.F15 => 0x66U,
				DInputKey.Grave => 0x29U,
				DInputKey.Home => 0xC7U,
				DInputKey.Insert => 0xD2U,
				DInputKey.Kana => 0x70U,
				DInputKey.Kanji => 0x94U,
				DInputKey.LeftBracket => 0x1AU,
				DInputKey.LeftControl => 0x1DU,
				DInputKey.LeftArrow => 0xCBU,
				DInputKey.LeftAlt => 0x38U,
				DInputKey.LeftShift => 0x2AU,
				DInputKey.LeftWindowsKey => 0xDBU,
				DInputKey.Mail => 0xECU,
				DInputKey.MediaSelect => 0xEDU,
				DInputKey.MediaStop => 0xA4U,
				DInputKey.Minus => 0x0CU,
				DInputKey.Mute => 0xA0U,
				DInputKey.MyComputer => 0xEBU,
				DInputKey.NextTrack => 0x99U,
				DInputKey.NoConvert => 0x7BU,
				DInputKey.NumberLock => 0x45U,
				DInputKey.NumberPad0 => 0x52U,
				DInputKey.NumberPad1 => 0x4FU,
				DInputKey.NumberPad2 => 0x50U,
				DInputKey.NumberPad3 => 0x51U,
				DInputKey.NumberPad4 => 0x4BU,
				DInputKey.NumberPad5 => 0x4CU,
				DInputKey.NumberPad6 => 0x4DU,
				DInputKey.NumberPad7 => 0x47U,
				DInputKey.NumberPad8 => 0x48U,
				DInputKey.NumberPad9 => 0x49U,
				DInputKey.NumberPadComma => 0xB3U,
				DInputKey.NumberPadEnter => 0x9CU,
				DInputKey.NumberPadEquals => 0x8DU,
				DInputKey.NumberPadMinus => 0x4AU,
				DInputKey.NumberPadPeriod => 0x53U,
				DInputKey.NumberPadPlus => 0x4EU,
				DInputKey.NumberPadSlash => 0xB5U,
				DInputKey.NumberPadStar => 0x37U,
				DInputKey.Oem102 => 0x56U,
				DInputKey.PageDown => 0xD1U,
				DInputKey.PageUp => 0xC9U,
				DInputKey.Pause => 0xC5U,
				DInputKey.Period => 0x34U,
				DInputKey.PlayPause => 0xA2U,
				DInputKey.Power => 0xDEU,
				DInputKey.PreviousTrack => 0x90U,
				DInputKey.RightBracket => 0x1BU,
				DInputKey.RightControl => 0x9DU,
				DInputKey.Return => 0x1CU,
				DInputKey.RightArrow => 0xCDU,
				DInputKey.RightAlt => 0xB8U,
				DInputKey.RightShift => 0x36U,
				DInputKey.RightWindowsKey => 0xDCU,
				DInputKey.ScrollLock => 0x46U,
				DInputKey.Semicolon => 0x27U,
				DInputKey.Slash => 0x35U,
				DInputKey.Sleep => 0xDFU,
				DInputKey.Space => 0x39U,
				DInputKey.Stop => 0x95U,
				DInputKey.PrintScreen => 0xB7U,
				DInputKey.Tab => 0x0FU,
				DInputKey.Underline => 0x93U,
				DInputKey.Unlabeled => 0x97U,
				DInputKey.UpArrow => 0xC8U,
				DInputKey.VolumeDown => 0xAEU,
				DInputKey.VolumeUp => 0xB0U,
				DInputKey.Wake => 0xE3U,
				DInputKey.WebBack => 0xEAU,
				DInputKey.WebFavorites => 0xE6U,
				DInputKey.WebForward => 0xE9U,
				DInputKey.WebHome => 0xB2U,
				DInputKey.WebRefresh => 0xE7U,
				DInputKey.WebSearch => 0xE5U,
				DInputKey.WebStop => 0xE8U,
				DInputKey.Yen => 0x7DU,
				_ => throw new Exception("this should never be hit, every enum member has been accounted for")
			};
			return MapWin32VirtualScanCodeToVirtualKey(scanCode) switch
			{
				WinFormsKey.D0 => DInputKey.D0,
				WinFormsKey.D1 => DInputKey.D1,
				WinFormsKey.D2 => DInputKey.D2,
				WinFormsKey.D3 => DInputKey.D3,
				WinFormsKey.D4 => DInputKey.D4,
				WinFormsKey.D5 => DInputKey.D5,
				WinFormsKey.D6 => DInputKey.D6,
				WinFormsKey.D7 => DInputKey.D7,
				WinFormsKey.D8 => DInputKey.D8,
				WinFormsKey.D9 => DInputKey.D9,
				WinFormsKey.A => DInputKey.A,
				WinFormsKey.B => DInputKey.B,
				WinFormsKey.C => DInputKey.C,
				WinFormsKey.D => DInputKey.D,
				WinFormsKey.E => DInputKey.E,
				WinFormsKey.F => DInputKey.F,
				WinFormsKey.G => DInputKey.G,
				WinFormsKey.H => DInputKey.H,
				WinFormsKey.I => DInputKey.I,
				WinFormsKey.J => DInputKey.J,
				WinFormsKey.K => DInputKey.K,
				WinFormsKey.L => DInputKey.L,
				WinFormsKey.M => DInputKey.M,
				WinFormsKey.N => DInputKey.N,
				WinFormsKey.O => DInputKey.O,
				WinFormsKey.P => DInputKey.P,
				WinFormsKey.Q => DInputKey.Q,
				WinFormsKey.R => DInputKey.R,
				WinFormsKey.S => DInputKey.S,
				WinFormsKey.T => DInputKey.T,
				WinFormsKey.U => DInputKey.U,
				WinFormsKey.V => DInputKey.V,
				WinFormsKey.W => DInputKey.W,
				WinFormsKey.X => DInputKey.X,
				WinFormsKey.Y => DInputKey.Y,
				WinFormsKey.Z => DInputKey.Z,
				WinFormsKey.OemQuotes => DInputKey.Apostrophe,
				WinFormsKey.Back => DInputKey.Backspace,
				WinFormsKey.OemPipe => DInputKey.Backslash,
				WinFormsKey.Capital => DInputKey.CapsLock,
				WinFormsKey.Oemcomma => DInputKey.Comma,
				WinFormsKey.Oemplus => DInputKey.Equals,
				WinFormsKey.Escape => DInputKey.Escape,
				WinFormsKey.F1 => DInputKey.F1,
				WinFormsKey.F2 => DInputKey.F2,
				WinFormsKey.F3 => DInputKey.F3,
				WinFormsKey.F4 => DInputKey.F4,
				WinFormsKey.F5 => DInputKey.F5,
				WinFormsKey.F6 => DInputKey.F6,
				WinFormsKey.F7 => DInputKey.F7,
				WinFormsKey.F8 => DInputKey.F8,
				WinFormsKey.F9 => DInputKey.F9,
				WinFormsKey.F10 => DInputKey.F10,
				WinFormsKey.F11 => DInputKey.F11,
				WinFormsKey.F12 => DInputKey.F12,
				WinFormsKey.F13 => DInputKey.F13,
				WinFormsKey.F14 => DInputKey.F14,
				WinFormsKey.F15 => DInputKey.F15,
				WinFormsKey.Oemtilde => DInputKey.Grave,
				WinFormsKey.OemOpenBrackets => DInputKey.LeftBracket,
				WinFormsKey.LControlKey => DInputKey.LeftControl,
				WinFormsKey.LMenu => DInputKey.LeftAlt,
				WinFormsKey.LShiftKey => DInputKey.LeftShift,
				WinFormsKey.OemMinus => DInputKey.Minus,
				WinFormsKey.NumLock => DInputKey.NumberLock,
				WinFormsKey.Subtract => DInputKey.NumberPadMinus,
				WinFormsKey.Add => DInputKey.NumberPadPlus,
				WinFormsKey.Multiply => DInputKey.NumberPadStar,
				WinFormsKey.OemBackslash => DInputKey.Oem102,
				WinFormsKey.OemPeriod => DInputKey.Period,
				WinFormsKey.OemCloseBrackets => DInputKey.RightBracket,
				WinFormsKey.Return => DInputKey.Return,
				WinFormsKey.RShiftKey => DInputKey.RightShift,
				WinFormsKey.Scroll => DInputKey.ScrollLock,
				WinFormsKey.OemSemicolon => DInputKey.Semicolon,
				WinFormsKey.OemQuestion => DInputKey.Slash,
				WinFormsKey.Space => DInputKey.Space,
				WinFormsKey.Tab => DInputKey.Tab,
				_ => DInputKey.Unknown
			};
		}

		internal static readonly Dictionary<DInputKey, OpenTKKey> KeyEnumMap = new Dictionary<DInputKey, OpenTKKey>
		{
			// A-Z
			{DInputKey.A, OpenTKKey.A}, {DInputKey.B, OpenTKKey.B}, {DInputKey.C, OpenTKKey.C}, {DInputKey.D, OpenTKKey.D}, {DInputKey.E, OpenTKKey.E}, {DInputKey.F, OpenTKKey.F}, {DInputKey.G, OpenTKKey.G}, {DInputKey.H, OpenTKKey.H}, {DInputKey.I, OpenTKKey.I}, {DInputKey.J, OpenTKKey.J}, {DInputKey.K, OpenTKKey.K}, {DInputKey.L, OpenTKKey.L}, {DInputKey.M, OpenTKKey.M}, {DInputKey.N, OpenTKKey.N}, {DInputKey.O, OpenTKKey.O}, {DInputKey.P, OpenTKKey.P}, {DInputKey.Q, OpenTKKey.Q}, {DInputKey.R, OpenTKKey.R}, {DInputKey.S, OpenTKKey.S}, {DInputKey.T, OpenTKKey.T}, {DInputKey.U, OpenTKKey.U}, {DInputKey.V, OpenTKKey.V}, {DInputKey.W, OpenTKKey.W}, {DInputKey.X, OpenTKKey.X}, {DInputKey.Y, OpenTKKey.Y}, {DInputKey.Z, OpenTKKey.Z},
			// 0-9
			{DInputKey.D1, OpenTKKey.Number1}, {DInputKey.D2, OpenTKKey.Number2}, {DInputKey.D3, OpenTKKey.Number3}, {DInputKey.D4, OpenTKKey.Number4}, {DInputKey.D5, OpenTKKey.Number5}, {DInputKey.D6, OpenTKKey.Number6}, {DInputKey.D7, OpenTKKey.Number7}, {DInputKey.D8, OpenTKKey.Number8}, {DInputKey.D9, OpenTKKey.Number9}, {DInputKey.D0, OpenTKKey.Number0},
			// misc. printables (ASCII order)
			{DInputKey.Space, OpenTKKey.Space}, {DInputKey.Apostrophe, OpenTKKey.Quote}, {DInputKey.Comma, OpenTKKey.Comma}, {DInputKey.Minus, OpenTKKey.Minus}, {DInputKey.Period, OpenTKKey.Period}, {DInputKey.Slash, OpenTKKey.Slash}, {DInputKey.Semicolon, OpenTKKey.Semicolon}, {DInputKey.Equals, OpenTKKey.Plus}, {DInputKey.LeftBracket, OpenTKKey.BracketLeft}, {DInputKey.Backslash, OpenTKKey.BackSlash}, {DInputKey.RightBracket, OpenTKKey.BracketRight}, {DInputKey.Grave, OpenTKKey.Tilde},
			// misc. (alphabetically)
			{DInputKey.Backspace, OpenTKKey.BackSpace}, {DInputKey.CapsLock, OpenTKKey.CapsLock}, {DInputKey.Delete, OpenTKKey.Delete}, {DInputKey.DownArrow, OpenTKKey.Down}, {DInputKey.End, OpenTKKey.End}, {DInputKey.Return, OpenTKKey.Enter}, {DInputKey.Escape, OpenTKKey.Escape}, {DInputKey.Home, OpenTKKey.Home}, {DInputKey.Insert, OpenTKKey.Insert}, {DInputKey.LeftArrow, OpenTKKey.Left}, {DInputKey.Oem102, OpenTKKey.NonUSBackSlash}, {DInputKey.NumberLock, OpenTKKey.NumLock}, {DInputKey.PageDown, OpenTKKey.PageDown}, {DInputKey.PageUp, OpenTKKey.PageUp}, {DInputKey.Pause, OpenTKKey.Pause}, {DInputKey.PrintScreen, OpenTKKey.PrintScreen}, {DInputKey.RightArrow, OpenTKKey.Right}, {DInputKey.ScrollLock, OpenTKKey.ScrollLock}, {DInputKey.Tab, OpenTKKey.Tab}, {DInputKey.UpArrow, OpenTKKey.Up},
			// modifier
			{DInputKey.LeftWindowsKey, OpenTKKey.WinLeft}, {DInputKey.RightWindowsKey, OpenTKKey.WinRight}, {DInputKey.LeftControl, OpenTKKey.ControlLeft}, {DInputKey.RightControl, OpenTKKey.ControlRight}, {DInputKey.LeftAlt, OpenTKKey.AltLeft}, {DInputKey.RightAlt, OpenTKKey.AltRight}, {DInputKey.LeftShift, OpenTKKey.ShiftLeft}, {DInputKey.RightShift, OpenTKKey.ShiftRight},

			// function
			{DInputKey.F1, OpenTKKey.F1}, {DInputKey.F2, OpenTKKey.F2}, {DInputKey.F3, OpenTKKey.F3}, {DInputKey.F4, OpenTKKey.F4}, {DInputKey.F5, OpenTKKey.F5}, {DInputKey.F6, OpenTKKey.F6}, {DInputKey.F7, OpenTKKey.F7}, {DInputKey.F8, OpenTKKey.F8}, {DInputKey.F9, OpenTKKey.F9}, {DInputKey.F10, OpenTKKey.F10}, {DInputKey.F11, OpenTKKey.F11}, {DInputKey.F12, OpenTKKey.F12},
			// keypad (alphabetically)
			{DInputKey.NumberPad0, OpenTKKey.Keypad0}, {DInputKey.NumberPad1, OpenTKKey.Keypad1}, {DInputKey.NumberPad2, OpenTKKey.Keypad2}, {DInputKey.NumberPad3, OpenTKKey.Keypad3}, {DInputKey.NumberPad4, OpenTKKey.Keypad4}, {DInputKey.NumberPad5, OpenTKKey.Keypad5}, {DInputKey.NumberPad6, OpenTKKey.Keypad6}, {DInputKey.NumberPad7, OpenTKKey.Keypad7}, {DInputKey.NumberPad8, OpenTKKey.Keypad8}, {DInputKey.NumberPad9, OpenTKKey.Keypad9}, {DInputKey.NumberPadPlus, OpenTKKey.KeypadAdd}, {DInputKey.NumberPadPeriod, OpenTKKey.KeypadDecimal}, {DInputKey.NumberPadSlash, OpenTKKey.KeypadDivide}, {DInputKey.NumberPadEnter, OpenTKKey.KeypadEnter}, {DInputKey.NumberPadStar, OpenTKKey.KeypadMultiply}, {DInputKey.NumberPadMinus, OpenTKKey.KeypadSubtract}
		};
	}
}
