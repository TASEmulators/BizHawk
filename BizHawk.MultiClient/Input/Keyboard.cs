using System.Collections.Generic;
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
			return false;
		}

		public static Key? GetPressedKey()
		{
			// TODO uhh this will return the same key over and over though.
			if (state.PressedKeys.Count == 0)
				return null;
			if (state.PressedKeys[0] == Key.NumberPad8)
				return Key.UpArrow;
			if (state.PressedKeys[0] == Key.NumberPad2)
				return Key.DownArrow;
			if (state.PressedKeys[0] == Key.NumberPad4)
				return Key.LeftArrow;
			if (state.PressedKeys[0] == Key.NumberPad6)
				return Key.RightArrow;
			return state.PressedKeys[0];
		}
	}
}