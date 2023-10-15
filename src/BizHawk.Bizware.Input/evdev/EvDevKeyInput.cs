#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

using static BizHawk.Common.EvDevImports;
using static BizHawk.Common.FildesImports;

namespace BizHawk.Bizware.Input
{
	internal static class EvDevKeyInput
	{
		private struct EvDevKeyboard
		{
			public uint DriverVersion;
			public ushort IdBus;
			public ushort IdVendor;
			public ushort IdProduct;
			public ushort IdVersion;
			public string Name;
			public int Fd;
			public string Path;

			public override string ToString()
			{
				var verMajor = DriverVersion >> 16;
				var verMinor = DriverVersion >> 8 & 0xFF;
				var varRev = DriverVersion & 0xFF;
				return $"{Name} ({verMajor}.{verMinor}.{varRev} {IdBus:X4}/{IdVendor:X4}/{IdProduct:X4}/{IdVersion:X4})";
			}
		}

		private static bool _isInit;
		private static FileSystemWatcher? _fileSystemWatcher;
		private static readonly Dictionary<string, EvDevKeyboard> _keyboards = new();

		private static readonly object _lockObj = new();

		private static List<int> DecodeBits(Span<byte> bits)
		{
			var result = new List<int>(bits.Length * 8);
			for (var i = 0; i < bits.Length; i++)
			{
				var b = bits[i];
				var bitPos = i * 8;
				for (var j = 0; j < 8; j++)
				{
					if (b.Bit(j))
					{
						result.Add(bitPos + j);
					}
				}
			}

			return result;
		}

		private static unsafe void MaybeAddKeyboard(string path)
		{
			if (_keyboards.ContainsKey(path))
			{
				// already have this, ignore
				return;
			}

			var fd = open(path, OpenFlags.O_RDONLY | OpenFlags.O_NONBLOCK | OpenFlags.O_CLOEXEC);
			if (fd == -1)
			{
				return;
			}

			var version = 0u;
			var id = stackalloc ushort[4];
			var str = stackalloc byte[256];
			new Span<ushort>(id, 4).Clear();
			new Span<byte>(str, 256).Clear();

			// if any of these fail, the device was either removed or garbage
			if (ioctl(fd, new(EVIOCGVERSION), &version) == -1 ||
				ioctl(fd, new(EVIOCGID), id) == -1 ||
				ioctl(fd, new(EVIOCGNAME(256)), str) == -1)
			{
				_ = close(fd);
				return;
			}

			str[255] = 0; // not trusting this remains nul terminated
			var name = Marshal.PtrToStringAnsi(new(str))!;

			const int eventBitBufferSize = (int)EvDevEventType.EV_MAX / 8 + 1;
			var eventBits = stackalloc byte[eventBitBufferSize];
			new Span<byte>(eventBits, eventBitBufferSize).Clear();
			if (ioctl(fd, new(EVIOCGBIT(EvDevEventType.EV_SYN, (int)EvDevEventType.EV_MAX)), eventBits) == -1)
			{
				_ = close(fd);
				return;
			}

			var supportedEvents = DecodeBits(new(eventBits, eventBitBufferSize));
			if (!supportedEvents.Contains((int)EvDevEventType.EV_KEY))
			{
				// we only care about keyboards
				_ = close(fd);
				return;
			}

			const int keyBitBufferSize = (int)EvDevKeyCode.KEY_MAX / 8 + 1;
			var keyBits = stackalloc byte[keyBitBufferSize];
			new Span<byte>(keyBits, keyBitBufferSize).Clear();
			if (ioctl(fd, new(EVIOCGBIT(EvDevEventType.EV_KEY, (int)EvDevKeyCode.KEY_MAX)), keyBits) == -1)
			{
				_ = close(fd);
				return;
			}

			var supportedKeys = DecodeBits(new(keyBits, keyBitBufferSize));
			if (supportedKeys.Count == 0)
			{
				// probably garbage
				return;
			}

			if (name.IndexOf("keyboard", StringComparison.InvariantCultureIgnoreCase) == -1)
			{
				// probably not be a keyboard
				// TODO: do some better heuristics here (maybe check if supportedKeys has A-Z?)
				return;
			}

			var keyboard = new EvDevKeyboard
			{
				DriverVersion = version,
				IdBus = id[0],
				IdProduct = id[1],
				IdVendor = id[2],
				IdVersion = id[3],
				Name = name,
				Fd = fd,
				Path = path,
			};

			Console.WriteLine($"Added keyboard {keyboard}");
			_keyboards.Add(path, keyboard);
		}

		private static void MaybeRemoveKeyboard(string path)
		{
			if (_keyboards.TryGetValue(path, out var keyboard))
			{
				_ = close(keyboard.Fd);
				Console.WriteLine($"Removed keyboard {keyboard}");
				_keyboards.Remove(path);
			}
		}

		private static void OnWatcherEvent(object _, FileSystemEventArgs e)
		{
			lock (_lockObj)
			{
				if (!_isInit)
				{
					return;
				}

				// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
				switch (e.ChangeType)
				{
					case WatcherChangeTypes.Created:
					case WatcherChangeTypes.Changed:
						MaybeAddKeyboard(e.FullPath);
						break;
					case WatcherChangeTypes.Deleted:
						MaybeRemoveKeyboard(e.FullPath);
						break;
					default:
						Console.WriteLine($"Unexpected watcher event {e.ChangeType}");
						break;
				}
			}
		}

		public static void Initialize()
		{
			if (OSTailoredCode.CurrentOS != OSTailoredCode.DistinctOS.Linux)
			{
				// also supported in BSDs, I think OSTailoredCode.CurrentOS is Linux for BSDs?
				throw new NotSupportedException("evdev is Linux only");
			}

			lock (_lockObj)
			{
				Deinitialize();

				_fileSystemWatcher = new("/dev/input/", "event*")
				{
					NotifyFilter = NotifyFilters.FileName | NotifyFilters.Attributes,
				};

				_fileSystemWatcher.Created += OnWatcherEvent;
				_fileSystemWatcher.Changed += OnWatcherEvent;
				_fileSystemWatcher.Deleted += OnWatcherEvent;
				_fileSystemWatcher.EnableRaisingEvents = true;

				var evFns = Directory.GetFiles("/dev/input/", "event*");
				foreach (var fn in evFns)
				{
					MaybeAddKeyboard(fn);
				}

				_isInit = true;
			}
		}

		public static void Deinitialize()
		{
			lock (_lockObj)
			{
				_fileSystemWatcher?.Dispose();
				_fileSystemWatcher = null;

				foreach (var keyboard in _keyboards.Values)
				{
					_ = close(keyboard.Fd);
				}

				_keyboards.Clear();
				_isInit = false;
			}
		}

		public static IEnumerable<KeyEvent> Update()
		{
			lock (_lockObj)
			{
				if (!_isInit)
				{
					return Enumerable.Empty<KeyEvent>();
				}

				var kbEvent = default(EvDevKeyboardEvent);
				var kbEventSize = (IntPtr)Marshal.SizeOf<EvDevKeyboardEvent>();
				var kbsToClose = new List<string>();
				Span<byte> keyChanges = stackalloc byte[(int)EvDevKeyCode.KEY_CNT];
				keyChanges.Clear();

				foreach (var keyboard in _keyboards.Values)
				{
					while (true)
					{
						unsafe
						{
							var res = read(keyboard.Fd, &kbEvent, kbEventSize);
							if (res == (IntPtr)(-1))
							{
								// this is actually errno, despite the Win32 name
								var errno = Marshal.GetLastWin32Error();

								const int EAGAIN = 11;
								const int ENODEV = 19;

								// EAGAIN means there's no more events left to read (generally expected)
								if (errno == EAGAIN)
								{
									break;
								}

								// ENODEV means the device is gone
								if (errno == ENODEV)
								{
									kbsToClose.Add(keyboard.Path);
									break;
								}

								Debug.WriteLine($"Unexpected error reading keyboards: {errno}");
								break;
							}

							if (res != kbEventSize)
							{
								Debug.WriteLine("Unexpected incomplete read");
								break;
							}
						}

						if (kbEvent.type != EvDevEventType.EV_KEY)
						{
							// don't care for non-EV_KEY events
							continue;
						}

						if (kbEvent.code > EvDevKeyCode.KEY_MAX)
						{
							Debug.WriteLine($"Unexpected event code {kbEvent.code}");
							continue;
						}

						switch (kbEvent.value)
						{
							case EvDevKeyValue.KeyUp:
								keyChanges[(int)kbEvent.code] = 1;
								break;
							case EvDevKeyValue.KeyDown:
							case EvDevKeyValue.KeyRepeat:
								keyChanges[(int)kbEvent.code] = 2;
								break;
							default:
								Debug.WriteLine($"Unexpected event value {kbEvent.value}");
								break;
						}
					}
				}

				foreach (var path in kbsToClose)
				{
					MaybeRemoveKeyboard(path);
				}

				var keyEvents = new List<KeyEvent>();
				for (var i = 0; i < (int)EvDevKeyCode.KEY_CNT; i++)
				{
					if (keyChanges[i] != 0 &&
						KeyEnumMap.TryGetValue((EvDevKeyCode)i, out var key))
					{
						keyEvents.Add(new(key, keyChanges[i] == 1));
					}
				}

				return keyEvents;
			}
		}

		private static readonly Dictionary<EvDevKeyCode, DistinctKey> KeyEnumMap = new()
		{
			[EvDevKeyCode.KEY_ESC] = DistinctKey.Escape,
			[EvDevKeyCode.KEY_1] = DistinctKey.D1,
			[EvDevKeyCode.KEY_2] = DistinctKey.D2,
			[EvDevKeyCode.KEY_3] = DistinctKey.D3,
			[EvDevKeyCode.KEY_4] = DistinctKey.D4,
			[EvDevKeyCode.KEY_5] = DistinctKey.D5,
			[EvDevKeyCode.KEY_6] = DistinctKey.D6,
			[EvDevKeyCode.KEY_7] = DistinctKey.D7,
			[EvDevKeyCode.KEY_8] = DistinctKey.D8,
			[EvDevKeyCode.KEY_9] = DistinctKey.D9,
			[EvDevKeyCode.KEY_0] = DistinctKey.D0,
			[EvDevKeyCode.KEY_MINUS] = DistinctKey.OemMinus,
			[EvDevKeyCode.KEY_EQUAL] = DistinctKey.OemPlus,
			[EvDevKeyCode.KEY_BACKSPACE] = DistinctKey.Back,
			[EvDevKeyCode.KEY_TAB] = DistinctKey.Tab,
			[EvDevKeyCode.KEY_Q] = DistinctKey.Q,
			[EvDevKeyCode.KEY_W] = DistinctKey.W,
			[EvDevKeyCode.KEY_E] = DistinctKey.E,
			[EvDevKeyCode.KEY_R] = DistinctKey.R,
			[EvDevKeyCode.KEY_T] = DistinctKey.T,
			[EvDevKeyCode.KEY_Y] = DistinctKey.Y,
			[EvDevKeyCode.KEY_U] = DistinctKey.U,
			[EvDevKeyCode.KEY_I] = DistinctKey.I,
			[EvDevKeyCode.KEY_O] = DistinctKey.O,
			[EvDevKeyCode.KEY_P] = DistinctKey.P,
			[EvDevKeyCode.KEY_LEFTBRACE] = DistinctKey.OemOpenBrackets,
			[EvDevKeyCode.KEY_RIGHTBRACE] = DistinctKey.OemCloseBrackets,
			[EvDevKeyCode.KEY_ENTER] = DistinctKey.Enter,
			[EvDevKeyCode.KEY_LEFTCTRL] = DistinctKey.LeftCtrl,
			[EvDevKeyCode.KEY_A] = DistinctKey.A,
			[EvDevKeyCode.KEY_S] = DistinctKey.S,
			[EvDevKeyCode.KEY_D] = DistinctKey.D,
			[EvDevKeyCode.KEY_F] = DistinctKey.F,
			[EvDevKeyCode.KEY_G] = DistinctKey.G,
			[EvDevKeyCode.KEY_H] = DistinctKey.H,
			[EvDevKeyCode.KEY_J] = DistinctKey.J,
			[EvDevKeyCode.KEY_K] = DistinctKey.K,
			[EvDevKeyCode.KEY_L] = DistinctKey.L,
			[EvDevKeyCode.KEY_SEMICOLON] = DistinctKey.OemSemicolon,
			[EvDevKeyCode.KEY_APOSTROPHE] = DistinctKey.OemQuotes,
			[EvDevKeyCode.KEY_GRAVE] = DistinctKey.OemTilde,
			[EvDevKeyCode.KEY_LEFTSHIFT] = DistinctKey.LeftShift,
			[EvDevKeyCode.KEY_BACKSLASH] = DistinctKey.OemBackslash,
			[EvDevKeyCode.KEY_Z] = DistinctKey.Z,
			[EvDevKeyCode.KEY_X] = DistinctKey.X,
			[EvDevKeyCode.KEY_C] = DistinctKey.C,
			[EvDevKeyCode.KEY_V] = DistinctKey.V,
			[EvDevKeyCode.KEY_B] = DistinctKey.B,
			[EvDevKeyCode.KEY_N] = DistinctKey.N,
			[EvDevKeyCode.KEY_M] = DistinctKey.M,
			[EvDevKeyCode.KEY_COMMA] = DistinctKey.OemComma,
			[EvDevKeyCode.KEY_DOT] = DistinctKey.OemPeriod,
			[EvDevKeyCode.KEY_SLASH] = DistinctKey.OemQuestion,
			[EvDevKeyCode.KEY_RIGHTSHIFT] = DistinctKey.RightShift,
			[EvDevKeyCode.KEY_KPASTERISK] = DistinctKey.Multiply,
			[EvDevKeyCode.KEY_LEFTALT] = DistinctKey.LeftAlt,
			[EvDevKeyCode.KEY_SPACE] = DistinctKey.Space,
			[EvDevKeyCode.KEY_CAPSLOCK] = DistinctKey.CapsLock,
			[EvDevKeyCode.KEY_F1] = DistinctKey.F1,
			[EvDevKeyCode.KEY_F2] = DistinctKey.F2,
			[EvDevKeyCode.KEY_F3] = DistinctKey.F3,
			[EvDevKeyCode.KEY_F4] = DistinctKey.F4,
			[EvDevKeyCode.KEY_F5] = DistinctKey.F5,
			[EvDevKeyCode.KEY_F6] = DistinctKey.F6,
			[EvDevKeyCode.KEY_F7] = DistinctKey.F7,
			[EvDevKeyCode.KEY_F8] = DistinctKey.F8,
			[EvDevKeyCode.KEY_F9] = DistinctKey.F9,
			[EvDevKeyCode.KEY_F10] = DistinctKey.F10,
			[EvDevKeyCode.KEY_NUMLOCK] = DistinctKey.NumLock,
			[EvDevKeyCode.KEY_SCROLLLOCK] = DistinctKey.Scroll,
			[EvDevKeyCode.KEY_KP7] = DistinctKey.NumPad7,
			[EvDevKeyCode.KEY_KP8] = DistinctKey.NumPad8,
			[EvDevKeyCode.KEY_KP9] = DistinctKey.NumPad9,
			[EvDevKeyCode.KEY_KPMINUS] = DistinctKey.Subtract,
			[EvDevKeyCode.KEY_KP4] = DistinctKey.NumPad4,
			[EvDevKeyCode.KEY_KP5] = DistinctKey.NumPad5,
			[EvDevKeyCode.KEY_KP6] = DistinctKey.NumPad6,
			[EvDevKeyCode.KEY_KPPLUS] = DistinctKey.Add,
			[EvDevKeyCode.KEY_KP1] = DistinctKey.NumPad1,
			[EvDevKeyCode.KEY_KP2] = DistinctKey.NumPad2,
			[EvDevKeyCode.KEY_KP3] = DistinctKey.NumPad3,
			[EvDevKeyCode.KEY_KPDOT] = DistinctKey.Decimal,
			[EvDevKeyCode.KEY_102ND] = DistinctKey.Oem102,
			[EvDevKeyCode.KEY_F11] = DistinctKey.F11,
			[EvDevKeyCode.KEY_F12] = DistinctKey.F12,
			[EvDevKeyCode.KEY_KPENTER] = DistinctKey.NumPadEnter,
			[EvDevKeyCode.KEY_RIGHTCTRL] = DistinctKey.RightCtrl,
			[EvDevKeyCode.KEY_KPSLASH] = DistinctKey.Divide,
			[EvDevKeyCode.KEY_RIGHTALT] = DistinctKey.RightAlt,
			[EvDevKeyCode.KEY_LINEFEED] = DistinctKey.LineFeed,
			[EvDevKeyCode.KEY_HOME] = DistinctKey.Home,
			[EvDevKeyCode.KEY_UP] = DistinctKey.Up,
			[EvDevKeyCode.KEY_PAGEUP] = DistinctKey.PageUp,
			[EvDevKeyCode.KEY_LEFT] = DistinctKey.Left,
			[EvDevKeyCode.KEY_RIGHT] = DistinctKey.Right,
			[EvDevKeyCode.KEY_END] = DistinctKey.End,
			[EvDevKeyCode.KEY_DOWN] = DistinctKey.Down,
			[EvDevKeyCode.KEY_PAGEDOWN] = DistinctKey.PageDown,
			[EvDevKeyCode.KEY_INSERT] = DistinctKey.Insert,
			[EvDevKeyCode.KEY_DELETE] = DistinctKey.Delete,
			[EvDevKeyCode.KEY_MUTE] = DistinctKey.VolumeMute,
			[EvDevKeyCode.KEY_VOLUMEDOWN] = DistinctKey.VolumeDown,
			[EvDevKeyCode.KEY_VOLUMEUP] = DistinctKey.VolumeUp,
			[EvDevKeyCode.KEY_LEFTMETA] = DistinctKey.LWin,
			[EvDevKeyCode.KEY_RIGHTMETA] = DistinctKey.RWin,
			[EvDevKeyCode.KEY_STOP] = DistinctKey.BrowserStop,
			[EvDevKeyCode.KEY_HELP] = DistinctKey.Help,
			[EvDevKeyCode.KEY_SLEEP] = DistinctKey.Sleep,
			[EvDevKeyCode.KEY_PROG1] = DistinctKey.LaunchApplication1,
			[EvDevKeyCode.KEY_PROG2] = DistinctKey.LaunchApplication2,
			[EvDevKeyCode.KEY_MAIL] = DistinctKey.LaunchMail,
			[EvDevKeyCode.KEY_BACK] = DistinctKey.BrowserBack,
			[EvDevKeyCode.KEY_FORWARD] = DistinctKey.BrowserForward,
			[EvDevKeyCode.KEY_NEXTSONG] = DistinctKey.MediaNextTrack,
			[EvDevKeyCode.KEY_PLAYPAUSE] = DistinctKey.MediaPlayPause,
			[EvDevKeyCode.KEY_PREVIOUSSONG] = DistinctKey.MediaPreviousTrack,
			[EvDevKeyCode.KEY_STOPCD] = DistinctKey.MediaStop,
			[EvDevKeyCode.KEY_HOMEPAGE] = DistinctKey.BrowserHome,
			[EvDevKeyCode.KEY_REFRESH] = DistinctKey.BrowserRefresh,
			[EvDevKeyCode.KEY_F13] = DistinctKey.F13,
			[EvDevKeyCode.KEY_F14] = DistinctKey.F14,
			[EvDevKeyCode.KEY_F15] = DistinctKey.F15,
			[EvDevKeyCode.KEY_F16] = DistinctKey.F16,
			[EvDevKeyCode.KEY_F17] = DistinctKey.F17,
			[EvDevKeyCode.KEY_F18] = DistinctKey.F18,
			[EvDevKeyCode.KEY_F19] = DistinctKey.F19,
			[EvDevKeyCode.KEY_F20] = DistinctKey.F20,
			[EvDevKeyCode.KEY_F21] = DistinctKey.F21,
			[EvDevKeyCode.KEY_F22] = DistinctKey.F22,
			[EvDevKeyCode.KEY_F23] = DistinctKey.F23,
			[EvDevKeyCode.KEY_F24] = DistinctKey.F24,
			[EvDevKeyCode.KEY_PLAY] = DistinctKey.Play,
			[EvDevKeyCode.KEY_PRINT] = DistinctKey.Print,
			[EvDevKeyCode.KEY_SEARCH] = DistinctKey.BrowserSearch,
			[EvDevKeyCode.KEY_CANCEL] = DistinctKey.Cancel,
			[EvDevKeyCode.KEY_MEDIA] = DistinctKey.SelectMedia,
			[EvDevKeyCode.KEY_SELECT] = DistinctKey.Select,
			[EvDevKeyCode.KEY_FAVORITES] = DistinctKey.BrowserFavorites,
			[EvDevKeyCode.KEY_CLEAR] = DistinctKey.Clear,
		};
	}
}
