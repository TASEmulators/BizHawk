using SlimDX;
using SlimDX.DirectInput;

namespace BizHawk.MultiClient
{
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
}
