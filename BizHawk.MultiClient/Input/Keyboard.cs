using System.Collections.Generic;
using System.Text;
using SlimDX;
using SlimDX.DirectInput;

namespace BizHawk.MultiClient
{
	public static class KeyInput
	{
		private static DirectInput dinput;
		private static Keyboard keyboard;
		private static KeyboardState state = new KeyboardState();
		private static List<Key> unpressedKeys = new List<Key>();

		public static void Initialize()
		{
			if (dinput == null)
				dinput = new DirectInput();

			if (keyboard == null || keyboard.Disposed)
				keyboard = new Keyboard(dinput);
			if (Global.Config.AcceptBackgroundInput)
				keyboard.SetCooperativeLevel(Global.MainForm.Handle, CooperativeLevel.Background | CooperativeLevel.Nonexclusive);
			else
				keyboard.SetCooperativeLevel(Global.MainForm.Handle, CooperativeLevel.Foreground | CooperativeLevel.Nonexclusive);
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

			unpressedKeys.RemoveAll(key => state.IsReleased(key));
		}

		public static KeyboardState State { get { return state; } }

		public static void Unpress(Key key)
		{
			if (unpressedKeys.Contains(key))
				return;
			unpressedKeys.Add(key);
		}

		public static bool IsPressed(Key key)
		{
			if (unpressedKeys.Contains(key))
				return false;
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

		public static string GetPressedKey()
		{
			if (state.PressedKeys.Count == 0)
				return null;

		    Key? key = null;
            for (int i=0; i<state.PressedKeys.Count; i++)
            {
                if (state.PressedKeys[i].IsModifier())
                    continue;
                key = state.PressedKeys[i];
                break;
            }

            if (key == null)
                return null;

		    string keystr = GetModifierKeys() ?? "";
		    return keystr + key;
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

        public static string GetModifierKeys()
        {
            StringBuilder sb = new StringBuilder(16);
            if (ShiftModifier) sb.Append("Shift+");
            if (CtrlModifier) sb.Append("Ctrl+");
            if (AltModifier) sb.Append("Alt+");
            if (sb.Length == 0) return null;
            return sb.ToString();
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