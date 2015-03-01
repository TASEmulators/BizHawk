using System.Collections.Generic;
using SlimDX;
using SlimDX.DirectInput;
using System;

namespace BizHawk.Client.MultiHawk
{
	public static class KeyInput
	{
		private static DirectInput dinput;
		private static Keyboard keyboard;
		private static KeyboardState state = new KeyboardState();

		public static void Initialize(IntPtr parent)
		{
			if (dinput == null) 
				dinput = new DirectInput();

			if (keyboard == null || keyboard.Disposed)
				keyboard = new Keyboard(dinput);
			keyboard.SetCooperativeLevel(parent, CooperativeLevel.Background | CooperativeLevel.Nonexclusive);
			keyboard.Properties.BufferSize = 8;
		}

		static List<KeyEvent> EmptyList = new List<KeyEvent>();
		static List<KeyEvent> EventList = new List<KeyEvent>();

		public static IEnumerable<KeyEvent> Update()
		{
			EventList.Clear();

			if (keyboard.Acquire().IsFailure)
				return EmptyList;
			if (keyboard.Poll().IsFailure)
				return EmptyList;

			for (; ; )
			{
				var events = keyboard.GetBufferedData();
				if (Result.Last.IsFailure)
					return EventList;
				if (events.Count == 0)
					break;
				foreach (var e in events)
				{
					foreach (var k in e.PressedKeys)
						EventList.Add(new KeyEvent { Key = k, Pressed = true });
					foreach (var k in e.ReleasedKeys)
					EventList.Add(new KeyEvent { Key = k, Pressed = false });
				}
			}

			return EventList;
		}

		public struct KeyEvent
		{
			public Key Key;
			public bool Pressed;
		}

		
		public static bool IsPressed(Key key)
		{
			if (state.IsPressed(key))
				return true;
			
			if (key == Key.LeftShift && state.IsPressed(Key.RightShift))
				return true;
			if (key == Key.LeftControl && state.IsPressed(Key.RightControl))
				return true;
			if (key == Key.LeftAlt && state.IsPressed(Key.RightAlt))
				return true;

			return false;
		}

	    public static bool ShiftModifier
	    {
	        get
	        {
	            if (state.IsPressed(Key.LeftShift)) return true;
                if (state.IsPressed(Key.RightShift)) return true;
	            return false;
	        }
	    }

        public static bool CtrlModifier
        {
            get
            {
                if (state.IsPressed(Key.LeftControl)) return true;
                if (state.IsPressed(Key.RightControl)) return true;
                return false;
            }
        }

        public static bool AltModifier
        {
            get
            {
                if (state.IsPressed(Key.LeftAlt)) return true;
                if (state.IsPressed(Key.RightAlt)) return true;
                return false;
            }
        }

		public static Input.ModifierKey GetModifierKeysAsKeys()
		{
			Input.ModifierKey ret = Input.ModifierKey.None;
			if (ShiftModifier) ret |= Input.ModifierKey.Shift;
			if (CtrlModifier) ret |= Input.ModifierKey.Control;
			if (AltModifier) ret |= Input.ModifierKey.Alt;
			return ret;
		}

	}

    internal static class KeyExtensions
    {
        public static bool IsModifier(this Key key)
        {
            if (key == Key.LeftShift) return true;
            if (key == Key.RightShift) return true;
            if (key == Key.LeftControl) return true;
            if (key == Key.RightControl) return true;
            if (key == Key.LeftAlt) return true;
            if (key == Key.RightAlt) return true;
            return false;
        }
    }
}
