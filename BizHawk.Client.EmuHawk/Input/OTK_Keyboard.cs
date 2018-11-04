using System.Collections.Generic;
using System.Text;
using OpenTK.Input;

namespace BizHawk.Client.EmuHawk
{
	public static class OTK_Keyboard
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
}
