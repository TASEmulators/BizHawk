using System.Collections.Generic;
using SlimDX;
using SlimDX.DirectInput;

namespace BizHawk.Client.EmuHawk
{
	public static class KeyInput
	{
		private static readonly object SyncObj = new object();
		private static readonly List<KeyEvent> EventList = new List<KeyEvent>();
		private static DirectInput _directInput;
		private static Keyboard _keyboard;

		public static void Initialize()
		{
			lock (SyncObj)
			{
				Cleanup();

				_directInput = new DirectInput();

				_keyboard = new Keyboard(_directInput);
				_keyboard.SetCooperativeLevel(GlobalWin.MainForm.Handle, CooperativeLevel.Background | CooperativeLevel.Nonexclusive);
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

		public static IEnumerable<KeyEvent> Update()
		{
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
							EventList.Add(new KeyEvent { Key = KeyboardMapping.Handle(k), Pressed = true });
						foreach (var k in e.ReleasedKeys)
							EventList.Add(new KeyEvent { Key = KeyboardMapping.Handle(k), Pressed = false });
					}
				}

				return EventList;
			}
		}

		public struct KeyEvent
		{
			public Key Key;
			public bool Pressed;
		}
	}
}
