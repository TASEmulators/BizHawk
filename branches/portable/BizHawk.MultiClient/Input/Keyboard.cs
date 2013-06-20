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
		private static OpenTK.Input.KeyboardState _kbState;
		
		public static void Initialize ()
		{
			_kbState = OpenTK.Input.Keyboard.GetState();
		}
		
		public static void Update ()
		{
			try
			{
				_kbState = OpenTK.Input.Keyboard.GetState();
			}
			catch
			{
				//OpenTK's keyboard class isn't thread safe.
				//In rare cases (sometimes it takes up to 10 minutes to occur) it will 
				//be updating the keyboard state when we call GetState() and choke.
				//Until I fix OpenTK, it's fine to just swallow it because input continues working.
				if(System.Diagnostics.Debugger.IsAttached)
				{
					System.Console.WriteLine("OpenTK Keyboard thread is angry.");
				}
			}
		}
		
		public static bool IsPressed (Key key)
		{
			return _kbState.IsKeyDown(key);
		}
		
		public static bool ShiftModifier {
			get {
				return IsPressed(Key.ShiftLeft) || IsPressed(Key.ShiftRight);
			}
		}

		public static bool CtrlModifier {
			get {
				return IsPressed(Key.ControlLeft) || IsPressed(Key.ControlRight);
			}
		}
		
		public static bool AltModifier {
			get {
				return IsPressed(Key.AltLeft) || IsPressed(Key.AltRight);
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
