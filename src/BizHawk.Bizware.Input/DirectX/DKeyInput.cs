#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Common.CollectionExtensions;

using Vortice.DirectInput;

using static BizHawk.Common.Win32Imports;

using DInputKey = Vortice.DirectInput.Key;

namespace BizHawk.Bizware.Input
{
	internal static class DKeyInput
	{
		private static IDirectInput8? _directInput;

		private static IDirectInputDevice8? _keyboard;

		private static readonly object _lockObj = new();

		public static void Initialize(IntPtr mainFormHandle)
		{
			lock (_lockObj)
			{
				Cleanup();

				_directInput = DInput.DirectInput8Create();

				_keyboard = _directInput.CreateDevice(PredefinedDevice.SysKeyboard);
				_keyboard.SetCooperativeLevel(mainFormHandle, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
				_keyboard.SetDataFormat<RawKeyboardState>();
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
			DistinctKey Mapped(DInputKey k) => KeyEnumMap[config.HandleAlternateKeyboardLayouts ? MapToRealKeyViaScanCode(k) : k];

			lock (_lockObj)
			{
				if (_keyboard == null || _keyboard.Acquire().Failure || _keyboard.Poll().Failure) return Enumerable.Empty<KeyEvent>();

				List<KeyEvent> eventList = new List<KeyEvent>();
				while (true)
				{
					try
					{
						var events = _keyboard.GetBufferedKeyboardData();
						if (events.Length == 0) return eventList;
						eventList.AddRange(events.Select(e => new KeyEvent(Mapped(e.Key), e.IsPressed)));
					}
					catch (SharpGen.Runtime.SharpGenException)
					{
						return eventList;
					}
				}
			}
		}

		private static DInputKey MapToRealKeyViaScanCode(DInputKey key)
		{
			const uint MAPVK_VSC_TO_VK_EX = 0x03;
			// DInputKey is a scancode as is
			uint virtualKey = MapVirtualKey((uint) key, MAPVK_VSC_TO_VK_EX);
			return VKeyToDKeyMap.GetValueOrDefault(virtualKey, DInputKey.Unknown);
		}

		private static readonly IReadOnlyDictionary<DInputKey, DistinctKey> KeyEnumMap = new Dictionary<DInputKey, DistinctKey>
		{
			[DInputKey.D0] = DistinctKey.D0,
			[DInputKey.D1] = DistinctKey.D1,
			[DInputKey.D2] = DistinctKey.D2,
			[DInputKey.D3] = DistinctKey.D3,
			[DInputKey.D4] = DistinctKey.D4,
			[DInputKey.D5] = DistinctKey.D5,
			[DInputKey.D6] = DistinctKey.D6,
			[DInputKey.D7] = DistinctKey.D7,
			[DInputKey.D8] = DistinctKey.D8,
			[DInputKey.D9] = DistinctKey.D9,
			[DInputKey.A] = DistinctKey.A,
			[DInputKey.B] = DistinctKey.B,
			[DInputKey.C] = DistinctKey.C,
			[DInputKey.D] = DistinctKey.D,
			[DInputKey.E] = DistinctKey.E,
			[DInputKey.F] = DistinctKey.F,
			[DInputKey.G] = DistinctKey.G,
			[DInputKey.H] = DistinctKey.H,
			[DInputKey.I] = DistinctKey.I,
			[DInputKey.J] = DistinctKey.J,
			[DInputKey.K] = DistinctKey.K,
			[DInputKey.L] = DistinctKey.L,
			[DInputKey.M] = DistinctKey.M,
			[DInputKey.N] = DistinctKey.N,
			[DInputKey.O] = DistinctKey.O,
			[DInputKey.P] = DistinctKey.P,
			[DInputKey.Q] = DistinctKey.Q,
			[DInputKey.R] = DistinctKey.R,
			[DInputKey.S] = DistinctKey.S,
			[DInputKey.T] = DistinctKey.T,
			[DInputKey.U] = DistinctKey.U,
			[DInputKey.V] = DistinctKey.V,
			[DInputKey.W] = DistinctKey.W,
			[DInputKey.X] = DistinctKey.X,
			[DInputKey.Y] = DistinctKey.Y,
			[DInputKey.Z] = DistinctKey.Z,
			[DInputKey.AbntC1] = DistinctKey.AbntC1,
			[DInputKey.AbntC2] = DistinctKey.AbntC2,
			[DInputKey.Apostrophe] = DistinctKey.OemQuotes,
			[DInputKey.Applications] = DistinctKey.Apps,
			[DInputKey.AT] = DistinctKey.Unknown,
			[DInputKey.AX] = DistinctKey.Unknown,
			[DInputKey.Back] = DistinctKey.Back,
			[DInputKey.Backslash] = DistinctKey.OemPipe,
			[DInputKey.Calculator] = DistinctKey.Unknown,
			[DInputKey.CapsLock] = DistinctKey.CapsLock,
			[DInputKey.Colon] = DistinctKey.Unknown,
			[DInputKey.Comma] = DistinctKey.OemComma,
			[DInputKey.Convert] = DistinctKey.ImeConvert,
			[DInputKey.Delete] = DistinctKey.Delete,
			[DInputKey.Down] = DistinctKey.Down,
			[DInputKey.End] = DistinctKey.End,
			[DInputKey.Equals] = DistinctKey.OemPlus,
			[DInputKey.Escape] = DistinctKey.Escape,
			[DInputKey.F1] = DistinctKey.F1,
			[DInputKey.F2] = DistinctKey.F2,
			[DInputKey.F3] = DistinctKey.F3,
			[DInputKey.F4] = DistinctKey.F4,
			[DInputKey.F5] = DistinctKey.F5,
			[DInputKey.F6] = DistinctKey.F6,
			[DInputKey.F7] = DistinctKey.F7,
			[DInputKey.F8] = DistinctKey.F8,
			[DInputKey.F9] = DistinctKey.F9,
			[DInputKey.F10] = DistinctKey.F10,
			[DInputKey.F11] = DistinctKey.F11,
			[DInputKey.F12] = DistinctKey.F12,
			[DInputKey.F13] = DistinctKey.F13,
			[DInputKey.F14] = DistinctKey.F14,
			[DInputKey.F15] = DistinctKey.F15,
			[DInputKey.Grave] = DistinctKey.OemTilde,
			[DInputKey.Home] = DistinctKey.Home,
			[DInputKey.Insert] = DistinctKey.Insert,
			[DInputKey.Kana] = DistinctKey.KanaMode,
			[DInputKey.Kanji] = DistinctKey.KanjiMode,
			[DInputKey.LeftBracket] = DistinctKey.OemOpenBrackets,
			[DInputKey.LeftControl] = DistinctKey.LeftCtrl,
			[DInputKey.Left] = DistinctKey.Left,
			[DInputKey.LeftAlt] = DistinctKey.LeftAlt,
			[DInputKey.LeftShift] = DistinctKey.LeftShift,
			[DInputKey.LeftWindowsKey] = DistinctKey.LWin,
			[DInputKey.Mail] = DistinctKey.LaunchMail,
			[DInputKey.MediaSelect] = DistinctKey.SelectMedia,
			[DInputKey.MediaStop] = DistinctKey.MediaStop,
			[DInputKey.Minus] = DistinctKey.OemMinus,
			[DInputKey.Mute] = DistinctKey.VolumeMute,
			[DInputKey.MyComputer] = DistinctKey.Unknown,
			[DInputKey.NextTrack] = DistinctKey.MediaNextTrack,
			[DInputKey.NoConvert] = DistinctKey.ImeNonConvert,
			[DInputKey.NumberLock] = DistinctKey.NumLock,
			[DInputKey.NumberPad0] = DistinctKey.NumPad0,
			[DInputKey.NumberPad1] = DistinctKey.NumPad1,
			[DInputKey.NumberPad2] = DistinctKey.NumPad2,
			[DInputKey.NumberPad3] = DistinctKey.NumPad3,
			[DInputKey.NumberPad4] = DistinctKey.NumPad4,
			[DInputKey.NumberPad5] = DistinctKey.NumPad5,
			[DInputKey.NumberPad6] = DistinctKey.NumPad6,
			[DInputKey.NumberPad7] = DistinctKey.NumPad7,
			[DInputKey.NumberPad8] = DistinctKey.NumPad8,
			[DInputKey.NumberPad9] = DistinctKey.NumPad9,
			[DInputKey.NumberPadComma] = DistinctKey.Separator,
			[DInputKey.NumberPadEnter] = DistinctKey.NumPadEnter,
			[DInputKey.NumberPadEquals] = DistinctKey.OemPlus,
			[DInputKey.Subtract] = DistinctKey.Subtract,
			[DInputKey.Decimal] = DistinctKey.Decimal,
			[DInputKey.Add] = DistinctKey.Add,
			[DInputKey.Divide] = DistinctKey.Divide,
			[DInputKey.Multiply] = DistinctKey.Multiply,
			[DInputKey.Oem102] = DistinctKey.OemBackslash,
			[DInputKey.PageDown] = DistinctKey.PageDown,
			[DInputKey.PageUp] = DistinctKey.PageUp,
			[DInputKey.Pause] = DistinctKey.Pause,
			[DInputKey.Period] = DistinctKey.OemPeriod,
			[DInputKey.PlayPause] = DistinctKey.MediaPlayPause,
			[DInputKey.Power] = DistinctKey.Unknown,
			[DInputKey.PreviousTrack] = DistinctKey.MediaPreviousTrack,
			[DInputKey.RightBracket] = DistinctKey.OemCloseBrackets,
			[DInputKey.RightControl] = DistinctKey.RightCtrl,
			[DInputKey.Return] = DistinctKey.Return,
			[DInputKey.Right] = DistinctKey.Right,
			[DInputKey.RightAlt] = DistinctKey.RightAlt,
			[DInputKey.RightShift] = DistinctKey.RightShift,
			[DInputKey.RightWindowsKey] = DistinctKey.RWin,
			[DInputKey.ScrollLock] = DistinctKey.Scroll,
			[DInputKey.Semicolon] = DistinctKey.OemSemicolon,
			[DInputKey.Slash] = DistinctKey.OemQuestion,
			[DInputKey.Sleep] = DistinctKey.Sleep,
			[DInputKey.Space] = DistinctKey.Space,
			[DInputKey.Stop] = DistinctKey.MediaStop,
			[DInputKey.PrintScreen] = DistinctKey.PrintScreen,
			[DInputKey.Tab] = DistinctKey.Tab,
			[DInputKey.Underline] = DistinctKey.Unknown,
			[DInputKey.Unlabeled] = DistinctKey.Unknown,
			[DInputKey.Up] = DistinctKey.Up,
			[DInputKey.VolumeDown] = DistinctKey.VolumeDown,
			[DInputKey.VolumeUp] = DistinctKey.VolumeUp,
			[DInputKey.Wake] = DistinctKey.Sleep,
			[DInputKey.WebBack] = DistinctKey.BrowserBack,
			[DInputKey.WebFavorites] = DistinctKey.BrowserFavorites,
			[DInputKey.WebForward] = DistinctKey.BrowserForward,
			[DInputKey.WebHome] = DistinctKey.BrowserHome,
			[DInputKey.WebRefresh] = DistinctKey.BrowserRefresh,
			[DInputKey.WebSearch] = DistinctKey.BrowserSearch,
			[DInputKey.WebStop] = DistinctKey.BrowserStop,
			[DInputKey.Yen] = DistinctKey.Unknown,
			[DInputKey.Unknown] = DistinctKey.Unknown
		};

		private static readonly IReadOnlyDictionary<uint, DInputKey> VKeyToDKeyMap = new Dictionary<uint, DInputKey>
		{
			[0x30] = DInputKey.D0,
			[0x31] = DInputKey.D1,
			[0x32] = DInputKey.D2,
			[0x33] = DInputKey.D3,
			[0x34] = DInputKey.D4,
			[0x35] = DInputKey.D5,
			[0x36] = DInputKey.D6,
			[0x37] = DInputKey.D7,
			[0x38] = DInputKey.D8,
			[0x39] = DInputKey.D9,
			[0x41] = DInputKey.A,
			[0x42] = DInputKey.B,
			[0x43] = DInputKey.C,
			[0x44] = DInputKey.D,
			[0x45] = DInputKey.E,
			[0x46] = DInputKey.F,
			[0x47] = DInputKey.G,
			[0x48] = DInputKey.H,
			[0x49] = DInputKey.I,
			[0x4A] = DInputKey.J,
			[0x4B] = DInputKey.K,
			[0x4C] = DInputKey.L,
			[0x4D] = DInputKey.M,
			[0x4E] = DInputKey.N,
			[0x4F] = DInputKey.O,
			[0x50] = DInputKey.P,
			[0x51] = DInputKey.Q,
			[0x52] = DInputKey.R,
			[0x53] = DInputKey.S,
			[0x54] = DInputKey.T,
			[0x55] = DInputKey.U,
			[0x56] = DInputKey.V,
			[0x57] = DInputKey.W,
			[0x58] = DInputKey.X,
			[0x59] = DInputKey.Y,
			[0x5A] = DInputKey.Z,
			[0xDE] = DInputKey.Apostrophe,
			[0x5D] = DInputKey.Applications,
			[0x08] = DInputKey.Back,
			[0xDC] = DInputKey.Backslash,
			[0x14] = DInputKey.CapsLock,
			[0xBC] = DInputKey.Comma,
			[0x1C] = DInputKey.Convert,
			[0x2E] = DInputKey.Delete,
			[0x28] = DInputKey.Down,
			[0x23] = DInputKey.End,
			[0xBB] = DInputKey.Equals,
			[0x1B] = DInputKey.Escape,
			[0x70] = DInputKey.F1,
			[0x71] = DInputKey.F2,
			[0x72] = DInputKey.F3,
			[0x73] = DInputKey.F4,
			[0x74] = DInputKey.F5,
			[0x75] = DInputKey.F6,
			[0x76] = DInputKey.F7,
			[0x77] = DInputKey.F8,
			[0x78] = DInputKey.F9,
			[0x79] = DInputKey.F10,
			[0x7A] = DInputKey.F11,
			[0x7B] = DInputKey.F12,
			[0x7C] = DInputKey.F13,
			[0x7D] = DInputKey.F14,
			[0x7E] = DInputKey.F15,
			[0xC0] = DInputKey.Grave,
			[0x24] = DInputKey.Home,
			[0x2D] = DInputKey.Insert,
			[0xDB] = DInputKey.LeftBracket,
			[0xA2] = DInputKey.LeftControl,
			[0x25] = DInputKey.Left,
			[0xA4] = DInputKey.LeftAlt,
			[0xA0] = DInputKey.LeftShift,
			[0x5B] = DInputKey.LeftWindowsKey,
			[0xB4] = DInputKey.Mail,
			[0xB5] = DInputKey.MediaSelect,
			[0xB2] = DInputKey.MediaStop,
			[0xBD] = DInputKey.Minus,
			[0xAD] = DInputKey.Mute,
			[0xB0] = DInputKey.NextTrack,
			[0x90] = DInputKey.NumberLock,
			[0x6D] = DInputKey.Subtract,
			[0x6B] = DInputKey.Add,
			[0x6F] = DInputKey.Divide,
			[0x6A] = DInputKey.Multiply,
			[0xE2] = DInputKey.Oem102,
			[0x22] = DInputKey.PageDown,
			[0x21] = DInputKey.PageUp,
			[0x13] = DInputKey.Pause,
			[0xBE] = DInputKey.Period,
			[0xB3] = DInputKey.PlayPause,
			[0xB1] = DInputKey.PreviousTrack,
			[0xDD] = DInputKey.RightBracket,
			[0xA3] = DInputKey.RightControl,
			[0x0D] = DInputKey.Return,
			[0x27] = DInputKey.Right,
			[0xA5] = DInputKey.RightAlt,
			[0xA1] = DInputKey.RightShift,
			[0x5C] = DInputKey.RightWindowsKey,
			[0x91] = DInputKey.ScrollLock,
			[0xBA] = DInputKey.Semicolon,
			[0xBF] = DInputKey.Slash,
			[0x5F] = DInputKey.Sleep,
			[0x20] = DInputKey.Space,
			[0x2C] = DInputKey.PrintScreen,
			[0x09] = DInputKey.Tab,
			[0x26] = DInputKey.Up,
			[0xAE] = DInputKey.VolumeDown,
			[0xAF] = DInputKey.VolumeUp,
			[0xA6] = DInputKey.WebBack,
			[0xAB] = DInputKey.WebFavorites,
			[0xA7] = DInputKey.WebForward,
			[0xAC] = DInputKey.WebHome,
			[0xA8] = DInputKey.WebRefresh,
			[0xAA] = DInputKey.WebSearch,
			[0xA9] = DInputKey.WebStop,
		};
	}
}
