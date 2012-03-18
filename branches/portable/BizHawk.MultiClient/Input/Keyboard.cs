using System.Collections.Generic;
using System.Text;

#if WINDOWS
using SlimDX;
using SlimDX.DirectInput;
#else
using OpenTK.Input;

#endif

namespace BizHawk.MultiClient
{
#if WINDOWS
	public static class KeyInput
	{
		private static DirectInput dinput;
		private static Keyboard keyboard;
		private static KeyboardState state = new KeyboardState();

		public static void Initialize()
		{
			if (dinput == null) 
				dinput = new DirectInput();

			if (keyboard == null || keyboard.Disposed)
				keyboard = new Keyboard(dinput);
			keyboard.SetCooperativeLevel(Global.MainForm.Handle, CooperativeLevel.Background | CooperativeLevel.Nonexclusive);
		}

		public static void Update()
		{
			if (keyboard.Acquire().IsFailure)
				return;
			if (keyboard.Poll().IsFailure)
				return;

			state = keyboard.GetCurrentState();
			if (Result.Last.IsFailure)
				return;
		}

		public static KeyboardState State { get { return state; } }

		
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
#else
	public static class KeyInput
	{
		//Note: Code using KeyboardState is the "correct" implementation.
		//However, Input.Keyboard is broken on Mac OS X 10.7 in the current version of OpenTK
		//The deprecated KeyboardDevice code is being used until that's fixed in OpenTK
		
		//private static OpenTK.Input.KeyboardState _kbState;
		private static KeyboardDevice _oldKeyboard;
		
		public static void Initialize ()
		{
			//_kbState = OpenTK.Input.Keyboard.GetState();
			OpenTK.GameWindow gw = new OpenTK.GameWindow();
			_oldKeyboard = gw.InputDriver.Keyboard[0];
		}
		
		public static void Update ()
		{
			//_kbState = OpenTK.Input.Keyboard.GetState();
		}
		
		//public static KeyboardState State { get { return _kbState; } }
		
		public static bool IsPressed (Key key)
		{
			return _oldKeyboard[key];
			//return _kbState.IsKeyDown(key);
		}
		
		public static bool ShiftModifier {
			/*get {
				if (_kbState.IsKeyDown (Key.ShiftLeft))
					return true;
				if (_kbState.IsKeyDown (Key.ShiftRight))
					return true;
				return false;
			}*/
			get {
				return _oldKeyboard[Key.ShiftLeft] || _oldKeyboard[Key.ShiftRight];
			}
		}
		
		public static bool CtrlModifier {
			/*get {
				if (_kbState.IsKeyDown (Key.ControlLeft))
					return true;
				if (_kbState.IsKeyDown (Key.ControlRight))
					return true;
				return false;
			}*/
			get {
				return _oldKeyboard[Key.ControlLeft] || _oldKeyboard[Key.ControlRight];
			}
		}
		
		public static bool AltModifier {
			/*get {
				if (_kbState.IsKeyDown (Key.AltLeft))
					return true;
				if (_kbState.IsKeyDown (Key.AltRight))
					return true;
				return false;
			}*/
			get {
				return _oldKeyboard[Key.AltLeft] || _oldKeyboard[Key.AltRight];
			}
		}
		
		public static Input.ModifierKey GetModifierKeysAsKeys ()
		{
			Input.ModifierKey ret = Input.ModifierKey.None;
			if (ShiftModifier)
				ret |= Input.ModifierKey.Shift;
			if (CtrlModifier)
				ret |= Input.ModifierKey.Control;
			if (AltModifier)
				ret |= Input.ModifierKey.Alt;
			return ret;
		}
	}
	
	internal static class KeyExtensions
	{
		public static bool IsModifier (this Key key)
		{
			if (key == Key.ShiftLeft)
				return true;
			if (key == Key.ShiftRight)
				return true;
			if (key == Key.ControlLeft)
				return true;
			if (key == Key.ControlRight)
				return true;
			if (key == Key.AltLeft)
				return true;
			if (key == Key.AltRight)
				return true;
			return false;
		}
	}
#endif
}
