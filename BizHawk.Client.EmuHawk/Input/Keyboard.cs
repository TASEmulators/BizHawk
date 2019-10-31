using System.Collections.Generic;
using SlimDX;
using SlimDX.DirectInput;

namespace BizHawk.Client.EmuHawk
{
	public static class KeyInput
	{
		private static readonly object _syncObj = new object();
		private static readonly List<KeyEvent> _eventList = new List<KeyEvent>();
		private static DirectInput _dinput;
		private static Keyboard _keyboard;

		public static void Initialize()
		{
			lock (_syncObj)
			{
				Cleanup();

				_dinput = new DirectInput();

				_keyboard = new Keyboard(_dinput);
				_keyboard.SetCooperativeLevel(GlobalWin.MainForm.Handle, CooperativeLevel.Background | CooperativeLevel.Nonexclusive);
				_keyboard.Properties.BufferSize = 8;
			}
		}

		public static void Cleanup()
		{
			lock (_syncObj)
			{
				if (_keyboard != null)
				{
					_keyboard.Dispose();
					_keyboard = null;
				}

				if (_dinput != null)
				{
					_dinput.Dispose();
					_dinput = null;
				}
			}
		}

		public static IEnumerable<KeyEvent> Update()
		{
			lock (_syncObj)
			{
				_eventList.Clear();

				if (_keyboard == null || _keyboard.Acquire().IsFailure || _keyboard.Poll().IsFailure)
					return _eventList;

				for (; ; )
				{
					var events = _keyboard.GetBufferedData();
					if (Result.Last.IsFailure || events.Count == 0)
						break;
					foreach (var e in events)
					{
						foreach (var k in e.PressedKeys)
							_eventList.Add(new KeyEvent { Key = KeyboardMapping.Handle(k), Pressed = true });
						foreach (var k in e.ReleasedKeys)
							_eventList.Add(new KeyEvent { Key = KeyboardMapping.Handle(k), Pressed = false });
					}
				}

				return _eventList;
			}
		}

		public struct KeyEvent
		{
			public Key Key;
			public bool Pressed;
		}
	}
}
