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
			var scanCode = key switch
			{
				DInputKey.D0 => 0x000BU,
				DInputKey.D1 => 0x0002U,
				DInputKey.D2 => 0x0003U,
				DInputKey.D3 => 0x0004U,
				DInputKey.D4 => 0x0005U,
				DInputKey.D5 => 0x0006U,
				DInputKey.D6 => 0x0007U,
				DInputKey.D7 => 0x0008U,
				DInputKey.D8 => 0x0009U,
				DInputKey.D9 => 0x000AU,
				DInputKey.A => 0x001EU,
				DInputKey.B => 0x0030U,
				DInputKey.C => 0x002EU,
				DInputKey.D => 0x0020U,
				DInputKey.E => 0x0012U,
				DInputKey.F => 0x0021U,
				DInputKey.G => 0x0022U,
				DInputKey.H => 0x0023U,
				DInputKey.I => 0x0017U,
				DInputKey.J => 0x0024U,
				DInputKey.K => 0x0025U,
				DInputKey.L => 0x0026U,
				DInputKey.M => 0x0032U,
				DInputKey.N => 0x0031U,
				DInputKey.O => 0x0018U,
				DInputKey.P => 0x0019U,
				DInputKey.Q => 0x0010U,
				DInputKey.R => 0x0013U,
				DInputKey.S => 0x001FU,
				DInputKey.T => 0x0014U,
				DInputKey.U => 0x0016U,
				DInputKey.V => 0x002FU,
				DInputKey.W => 0x0011U,
				DInputKey.X => 0x002DU,
				DInputKey.Y => 0x0015U,
				DInputKey.Z => 0x002CU,
//				DInputKey.AbntC1 => 0x73U,
//				DInputKey.AbntC2 => 0x7EU,
				DInputKey.Apostrophe => 0x0028U,
				DInputKey.Applications => 0xE05DU,
//				DInputKey.AT => 0x91U,
//				DInputKey.AX => 0x96U,
				DInputKey.Backspace => 0x000EU,
				DInputKey.Backslash => 0x002BU,
//				DInputKey.Calculator => 0xA1U,
				DInputKey.CapsLock => 0x003AU,
//				DInputKey.Colon => 0x92U,
				DInputKey.Comma => 0x0033U,
				DInputKey.Convert => 0x0079U,
				DInputKey.Delete => 0x0053U,
				DInputKey.DownArrow => 0x0050U,
				DInputKey.End => 0x004FU,
				DInputKey.Equals => 0x000DU,
				DInputKey.Escape => 0x0001U,
				DInputKey.F1 => 0x003BU,
				DInputKey.F2 => 0x003CU,
				DInputKey.F3 => 0x003DU,
				DInputKey.F4 => 0x003EU,
				DInputKey.F5 => 0x003FU,
				DInputKey.F6 => 0x0040U,
				DInputKey.F7 => 0x0041U,
				DInputKey.F8 => 0x0042U,
				DInputKey.F9 => 0x0043U,
				DInputKey.F10 => 0x0044U,
				DInputKey.F11 => 0x0057U,
				DInputKey.F12 => 0x0058U,
				DInputKey.F13 => 0x0064U,
				DInputKey.F14 => 0x0065U,
				DInputKey.F15 => 0x0066U,
				DInputKey.Grave => 0x0029U,
				DInputKey.Home => 0x0047U,
				DInputKey.Insert => 0x0052U,
//				DInputKey.Kana => 0x70U,
//				DInputKey.Kanji => 0x94U,
				DInputKey.LeftBracket => 0x001AU,
				DInputKey.LeftControl => 0x001DU,
				DInputKey.LeftArrow => 0x004BU,
				DInputKey.LeftAlt => 0x0038U,
				DInputKey.LeftShift => 0x002AU,
				DInputKey.LeftWindowsKey => 0xE05BU,
				DInputKey.Mail => 0xE06CU,
				DInputKey.MediaSelect => 0xE06DU,
				DInputKey.MediaStop => 0xE024U,
				DInputKey.Minus => 0x000CU,
				DInputKey.Mute => 0xE020U,
//				DInputKey.MyComputer => 0xEBU,
				DInputKey.NextTrack => 0xE019U,
//				DInputKey.NoConvert => 0x7BU,
				DInputKey.NumberLock => 0x0045U,
//				DInputKey.NumberPad0 => 0x52U,
//				DInputKey.NumberPad1 => 0x4FU,
//				DInputKey.NumberPad2 => 0x50U,
//				DInputKey.NumberPad3 => 0x51U,
//				DInputKey.NumberPad4 => 0x4BU,
//				DInputKey.NumberPad5 => 0x4CU,
//				DInputKey.NumberPad6 => 0x4DU,
//				DInputKey.NumberPad7 => 0x47U,
//				DInputKey.NumberPad8 => 0x48U,
//				DInputKey.NumberPad9 => 0x49U,
//				DInputKey.NumberPadComma => 0xB3U,
//				DInputKey.NumberPadEnter => 0x9CU,
//				DInputKey.NumberPadEquals => 0x8DU,
				DInputKey.NumberPadMinus => 0x004AU,
//				DInputKey.NumberPadPeriod => 0x53U,
				DInputKey.NumberPadPlus => 0x004EU,
				DInputKey.NumberPadSlash => 0xE035U,
				DInputKey.NumberPadStar => 0x0037U,
				DInputKey.Oem102 => 0x0056U,
				DInputKey.PageDown => 0x0051U,
				DInputKey.PageUp => 0x0049U,
				DInputKey.Pause => 0xE11DU,
				DInputKey.Period => 0x0034U,
				DInputKey.PlayPause => 0xE022U,
//				DInputKey.Power => 0xDEU,
				DInputKey.PreviousTrack => 0xE010U,
				DInputKey.RightBracket => 0x001BU,
				DInputKey.RightControl => 0xE01DU,
				DInputKey.Return => 0x001CU,
				DInputKey.RightArrow => 0x004DU,
				DInputKey.RightAlt => 0xE038U,
				DInputKey.RightShift => 0x0036U,
				DInputKey.RightWindowsKey => 0xE05CU,
				DInputKey.ScrollLock => 0x0046U,
				DInputKey.Semicolon => 0x0027U,
				DInputKey.Slash => 0x0035U,
				DInputKey.Sleep => 0xE05FU,
				DInputKey.Space => 0x0039U,
//				DInputKey.Stop => 0x95U,
				DInputKey.PrintScreen => 0x0054U,
				DInputKey.Tab => 0x000FU,
//				DInputKey.Underline => 0x93U,
//				DInputKey.Unlabeled => 0x97U,
				DInputKey.UpArrow => 0x0048U,
				DInputKey.VolumeDown => 0xE02EU,
				DInputKey.VolumeUp => 0xE030U,
//				DInputKey.Wake => 0x00E3U,
				DInputKey.WebBack => 0xE06AU,
				DInputKey.WebFavorites => 0xE066U,
				DInputKey.WebForward => 0xE069U,
				DInputKey.WebHome => 0xE032U,
				DInputKey.WebRefresh => 0xE067U,
				DInputKey.WebSearch => 0xE065U,
				DInputKey.WebStop => 0xE068U,
//				DInputKey.Yen => 0x7DU,
				_ => 0U
			};
			if (scanCode == 0U) return DInputKey.Unknown;
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
//				WinFormsKey. => DInputKey.AbntC1,
//				WinFormsKey. => DInputKey.AbntC2,
				WinFormsKey.OemQuotes => DInputKey.Apostrophe,
				WinFormsKey.Apps => DInputKey.Applications,
//				WinFormsKey. => DInputKey.AT,
//				WinFormsKey. => DInputKey.AX,
				WinFormsKey.Back => DInputKey.Backspace,
				WinFormsKey.OemPipe => DInputKey.Backslash,
//				WinFormsKey. => DInputKey.Calculator,
				WinFormsKey.Capital => DInputKey.CapsLock,
//				WinFormsKey. => DInputKey.Colon,
				WinFormsKey.Oemcomma => DInputKey.Comma,
				WinFormsKey.IMEConvert => DInputKey.Convert,
				WinFormsKey.Delete => DInputKey.Delete,
				WinFormsKey.Down => DInputKey.DownArrow,
				WinFormsKey.End => DInputKey.End,
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
				WinFormsKey.Home => DInputKey.Home,
				WinFormsKey.Insert => DInputKey.Insert,
//				WinFormsKey.KanaMode => DInputKey.Kana,
//				WinFormsKey.KanjiMode => DInputKey.Kanji,
				WinFormsKey.OemOpenBrackets => DInputKey.LeftBracket,
				WinFormsKey.LControlKey => DInputKey.LeftControl,
				WinFormsKey.Left => DInputKey.LeftArrow,
				WinFormsKey.LMenu => DInputKey.LeftAlt,
				WinFormsKey.LShiftKey => DInputKey.LeftShift,
				WinFormsKey.LWin => DInputKey.LeftWindowsKey,
				WinFormsKey.LaunchMail => DInputKey.Mail,
				WinFormsKey.SelectMedia => DInputKey.MediaSelect,
				WinFormsKey.MediaStop => DInputKey.MediaStop,
				WinFormsKey.OemMinus => DInputKey.Minus,
				WinFormsKey.VolumeMute => DInputKey.Mute,
//				WinFormsKey. => DInputKey.MyComputer,
				WinFormsKey.MediaNextTrack => DInputKey.NextTrack,
//				WinFormsKey.IMENonconvert => DInputKey.NoConvert,
				WinFormsKey.NumLock => DInputKey.NumberLock,
//				WinFormsKey.NumPad0 => DInputKey.NumberPad0,
//				WinFormsKey.NumPad1 => DInputKey.NumberPad1,
//				WinFormsKey.NumPad2 => DInputKey.NumberPad2,
//				WinFormsKey.NumPad3 => DInputKey.NumberPad3,
//				WinFormsKey.NumPad4 => DInputKey.NumberPad4,
//				WinFormsKey.NumPad5 => DInputKey.NumberPad5,
//				WinFormsKey.NumPad6 => DInputKey.NumberPad6,
//				WinFormsKey.NumPad7 => DInputKey.NumberPad7,
//				WinFormsKey.NumPad8 => DInputKey.NumberPad8,
//				WinFormsKey.NumPad9 => DInputKey.NumberPad9,
//				WinFormsKey. => DInputKey.NumberPadComma,
//				WinFormsKey. => DInputKey.NumberPadEnter,
//				WinFormsKey. => DInputKey.NumberPadEquals,
				WinFormsKey.Subtract => DInputKey.NumberPadMinus,
//				WinFormsKey.Decimal => DInputKey.NumberPadPeriod,
				WinFormsKey.Add => DInputKey.NumberPadPlus,
				WinFormsKey.Divide => DInputKey.NumberPadSlash,
				WinFormsKey.Multiply => DInputKey.NumberPadStar,
				WinFormsKey.OemBackslash => DInputKey.Oem102,
				WinFormsKey.Next => DInputKey.PageDown,
				WinFormsKey.Prior => DInputKey.PageUp,
				WinFormsKey.Pause => DInputKey.Pause,
				WinFormsKey.OemPeriod => DInputKey.Period,
				WinFormsKey.MediaPlayPause => DInputKey.PlayPause,
//				WinFormsKey. => DInputKey.Power,
				WinFormsKey.MediaPreviousTrack => DInputKey.PreviousTrack,
				WinFormsKey.OemCloseBrackets => DInputKey.RightBracket,
				WinFormsKey.RControlKey => DInputKey.RightControl,
				WinFormsKey.Return => DInputKey.Return,
				WinFormsKey.Right => DInputKey.RightArrow,
				WinFormsKey.RMenu => DInputKey.RightAlt,
				WinFormsKey.RShiftKey => DInputKey.RightShift,
				WinFormsKey.RWin => DInputKey.RightWindowsKey,
				WinFormsKey.Scroll => DInputKey.ScrollLock,
				WinFormsKey.OemSemicolon => DInputKey.Semicolon,
				WinFormsKey.OemQuestion => DInputKey.Slash,
				WinFormsKey.Sleep => DInputKey.Sleep,
				WinFormsKey.Space => DInputKey.Space,
//				WinFormsKey. => DInputKey.Stop,
				WinFormsKey.PrintScreen => DInputKey.PrintScreen,
				WinFormsKey.Tab => DInputKey.Tab,
//				WinFormsKey. => DInputKey.Underline,
//				WinFormsKey. => DInputKey.Unlabeled,
				WinFormsKey.Up => DInputKey.UpArrow,
				WinFormsKey.VolumeDown => DInputKey.VolumeDown,
				WinFormsKey.VolumeUp => DInputKey.VolumeUp,
//				WinFormsKey. => DInputKey.Wake,
				WinFormsKey.BrowserBack => DInputKey.WebBack,
				WinFormsKey.BrowserFavorites => DInputKey.WebFavorites,
				WinFormsKey.BrowserForward => DInputKey.WebForward,
				WinFormsKey.BrowserHome => DInputKey.WebHome,
				WinFormsKey.BrowserRefresh => DInputKey.WebRefresh,
				WinFormsKey.BrowserSearch => DInputKey.WebSearch,
				WinFormsKey.BrowserStop => DInputKey.WebStop,
//				WinFormsKey. => DInputKey.Yen,
				_ => DInputKey.Unknown
			};
		}

		internal static readonly Dictionary<DInputKey, OpenTKKey> KeyEnumMap = new Dictionary<DInputKey, OpenTKKey>
		{
			{DInputKey.Unknown, OpenTKKey.Unknown},

			// A-Z
			{DInputKey.A, OpenTKKey.A}, {DInputKey.B, OpenTKKey.B}, {DInputKey.C, OpenTKKey.C}, {DInputKey.D, OpenTKKey.D}, {DInputKey.E, OpenTKKey.E}, {DInputKey.F, OpenTKKey.F}, {DInputKey.G, OpenTKKey.G}, {DInputKey.H, OpenTKKey.H}, {DInputKey.I, OpenTKKey.I}, {DInputKey.J, OpenTKKey.J}, {DInputKey.K, OpenTKKey.K}, {DInputKey.L, OpenTKKey.L}, {DInputKey.M, OpenTKKey.M}, {DInputKey.N, OpenTKKey.N}, {DInputKey.O, OpenTKKey.O}, {DInputKey.P, OpenTKKey.P}, {DInputKey.Q, OpenTKKey.Q}, {DInputKey.R, OpenTKKey.R}, {DInputKey.S, OpenTKKey.S}, {DInputKey.T, OpenTKKey.T}, {DInputKey.U, OpenTKKey.U}, {DInputKey.V, OpenTKKey.V}, {DInputKey.W, OpenTKKey.W}, {DInputKey.X, OpenTKKey.X}, {DInputKey.Y, OpenTKKey.Y}, {DInputKey.Z, OpenTKKey.Z},
			// 0-9
			{DInputKey.D1, OpenTKKey.Number1}, {DInputKey.D2, OpenTKKey.Number2}, {DInputKey.D3, OpenTKKey.Number3}, {DInputKey.D4, OpenTKKey.Number4}, {DInputKey.D5, OpenTKKey.Number5}, {DInputKey.D6, OpenTKKey.Number6}, {DInputKey.D7, OpenTKKey.Number7}, {DInputKey.D8, OpenTKKey.Number8}, {DInputKey.D9, OpenTKKey.Number9}, {DInputKey.D0, OpenTKKey.Number0},
			// misc. printables (ASCII order)
			{DInputKey.Space, OpenTKKey.Space}, {DInputKey.Apostrophe, OpenTKKey.Quote}, {DInputKey.Comma, OpenTKKey.Comma}, {DInputKey.Minus, OpenTKKey.Minus}, {DInputKey.Period, OpenTKKey.Period}, {DInputKey.Slash, OpenTKKey.Slash}, {DInputKey.Semicolon, OpenTKKey.Semicolon}, {DInputKey.Equals, OpenTKKey.Plus}, {DInputKey.LeftBracket, OpenTKKey.BracketLeft}, {DInputKey.Backslash, OpenTKKey.BackSlash}, {DInputKey.RightBracket, OpenTKKey.BracketRight}, {DInputKey.Grave, OpenTKKey.Tilde},
			// misc. (alphabetically)
			{DInputKey.Backspace, OpenTKKey.BackSpace}, {DInputKey.CapsLock, OpenTKKey.CapsLock}, {DInputKey.Delete, OpenTKKey.Delete}, {DInputKey.DownArrow, OpenTKKey.Down}, {DInputKey.End, OpenTKKey.End}, {DInputKey.Return, OpenTKKey.Enter}, {DInputKey.Escape, OpenTKKey.Escape}, {DInputKey.Home, OpenTKKey.Home}, {DInputKey.Insert, OpenTKKey.Insert}, {DInputKey.LeftArrow, OpenTKKey.Left}, {DInputKey.Applications, OpenTKKey.Menu}, {DInputKey.Oem102, OpenTKKey.NonUSBackSlash}, {DInputKey.NumberLock, OpenTKKey.NumLock}, {DInputKey.PageDown, OpenTKKey.PageDown}, {DInputKey.PageUp, OpenTKKey.PageUp}, {DInputKey.Pause, OpenTKKey.Pause}, {DInputKey.PrintScreen, OpenTKKey.PrintScreen}, {DInputKey.RightArrow, OpenTKKey.Right}, {DInputKey.ScrollLock, OpenTKKey.ScrollLock}, {DInputKey.Tab, OpenTKKey.Tab}, {DInputKey.UpArrow, OpenTKKey.Up},
			// modifier
			{DInputKey.LeftWindowsKey, OpenTKKey.WinLeft}, {DInputKey.RightWindowsKey, OpenTKKey.WinRight}, {DInputKey.LeftControl, OpenTKKey.ControlLeft}, {DInputKey.RightControl, OpenTKKey.ControlRight}, {DInputKey.LeftAlt, OpenTKKey.AltLeft}, {DInputKey.RightAlt, OpenTKKey.AltRight}, {DInputKey.LeftShift, OpenTKKey.ShiftLeft}, {DInputKey.RightShift, OpenTKKey.ShiftRight},

			// function
			{DInputKey.F1, OpenTKKey.F1}, {DInputKey.F2, OpenTKKey.F2}, {DInputKey.F3, OpenTKKey.F3}, {DInputKey.F4, OpenTKKey.F4}, {DInputKey.F5, OpenTKKey.F5}, {DInputKey.F6, OpenTKKey.F6}, {DInputKey.F7, OpenTKKey.F7}, {DInputKey.F8, OpenTKKey.F8}, {DInputKey.F9, OpenTKKey.F9}, {DInputKey.F10, OpenTKKey.F10}, {DInputKey.F11, OpenTKKey.F11}, {DInputKey.F12, OpenTKKey.F12},
			// keypad (alphabetically)
			{DInputKey.NumberPad0, OpenTKKey.Keypad0}, {DInputKey.NumberPad1, OpenTKKey.Keypad1}, {DInputKey.NumberPad2, OpenTKKey.Keypad2}, {DInputKey.NumberPad3, OpenTKKey.Keypad3}, {DInputKey.NumberPad4, OpenTKKey.Keypad4}, {DInputKey.NumberPad5, OpenTKKey.Keypad5}, {DInputKey.NumberPad6, OpenTKKey.Keypad6}, {DInputKey.NumberPad7, OpenTKKey.Keypad7}, {DInputKey.NumberPad8, OpenTKKey.Keypad8}, {DInputKey.NumberPad9, OpenTKKey.Keypad9}, {DInputKey.NumberPadPlus, OpenTKKey.KeypadAdd}, {DInputKey.NumberPadPeriod, OpenTKKey.KeypadDecimal}, {DInputKey.NumberPadSlash, OpenTKKey.KeypadDivide}, {DInputKey.NumberPadEnter, OpenTKKey.KeypadEnter}, {DInputKey.NumberPadStar, OpenTKKey.KeypadMultiply}, {DInputKey.NumberPadMinus, OpenTKKey.KeypadSubtract}
		};
	}
}
