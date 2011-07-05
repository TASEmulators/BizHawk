using System;
using SlimDX.DirectInput;

namespace BizHawk.MultiClient
{
	public static class Input
	{
		public static void Initialize()
		{
			KeyInput.Initialize();
			GamePad.Initialize();
		}

		public static void Update()
		{
			KeyInput.Update();
			GamePad.UpdateAll();
		}

		public static bool IsPressed(string control)
		{
            // Check joystick first, its easier
            if (control.StartsWith("J1 ")) return GetGamePad(0, control.Substring(3));
            if (control.StartsWith("J2 ")) return GetGamePad(1, control.Substring(3));
            if (control.StartsWith("J3 ")) return GetGamePad(2, control.Substring(3));
            if (control.StartsWith("J4 ")) return GetGamePad(3, control.Substring(3));

            // Keyboard time. 
            // Keyboard bindings are significantly less free-form than they were previously. 
            // They are no longer just a list of keys which must be pressed simultaneously.
            // Bindings are assumed to be in the form of 0, 1, 2, or 3 modifiers (Ctrl, Alt, Shift),
            // plus one non-modifier key, which is at the end.
            // It is not possible to bind to two non-modifier keys together as a chorded hotkey.


		    int lastCombinerPosition = control.LastIndexOf('+');
            if (lastCombinerPosition < 0)
            {
                // No modifiers in this key binding.

                // Verify that no modifiers are currently pressed.
                if (KeyInput.CtrlModifier || KeyInput.ShiftModifier || KeyInput.AltModifier)
                    return false;

                Key k = (Key) Enum.Parse(typeof (Key), control, true);
                return KeyInput.IsPressed(k);
            }

            // 1 or more modifiers present in binding. First, lets identify the non-modifier key and check if it's pressed.
		    string nonModifierString = control.Substring(lastCombinerPosition+1);
            Key nonModifierKey = (Key)Enum.Parse(typeof(Key), nonModifierString, true);
            if (KeyInput.IsPressed(nonModifierKey) == false)
                return false; // non-modifier key isn't pressed anyway, exit out

            // non-modifier key IS pressed, now we need to ensure the modifiers match exactly
            if (control.Contains("Ctrl+") ^ KeyInput.CtrlModifier) return false;
            if (control.Contains("Shift+") ^ KeyInput.ShiftModifier) return false;
            if (control.Contains("Alt+") ^ KeyInput.AltModifier) return false;

            // You have passed all my tests, you may consider yourself pressed.
            // Man, I'm winded.
		    return true;
		}

		private static bool IsPressedSingle(string control)
		{
			if (string.IsNullOrEmpty(control))
				return false;

			if (control.StartsWith("J1 ")) return GetGamePad(0, control.Substring(3));
			if (control.StartsWith("J2 ")) return GetGamePad(1, control.Substring(3));
			if (control.StartsWith("J3 ")) return GetGamePad(2, control.Substring(3));
			if (control.StartsWith("J4 ")) return GetGamePad(3, control.Substring(3));

			if (control.Contains("RightShift"))
				control = control.Replace("RightShift", "LeftShift");
			if (control.Contains("RightControl"))
				control = control.Replace("RightControl", "LeftControl");
			if (control.Contains("RightAlt"))
				control = control.Replace("RightAlt", "LeftAlt");
			if (control.Contains("Ctrl"))
				control = control.Replace("Ctrl", "LeftControl");
			
			if (control.Contains("Shift") && control != "LeftShift")
				control = control.Replace("Shift", "LeftShift");
			if (control.Contains("Control") && control.Trim() != "LeftControl")
				control = control.Replace("Control", "LeftControl");
			if (control.Contains("Ctrl") && control.Trim() != "LeftControl")
				control = control.Replace("Control", "LeftControl");
			if (control.Contains("Alt") && control != "LeftAlt")
				control = control.Replace("Alt", "LeftAlt");

			Key k = (Key)Enum.Parse(typeof(Key), control, true);
			return KeyInput.IsPressed(k);
		}

		private static bool GetGamePad(int index, string control)
		{
			if (index >= GamePad.Devices.Count)
				return false;

			if (control == "Up") return GamePad.Devices[index].Up;
			if (control == "Down") return GamePad.Devices[index].Down;
			if (control == "Left") return GamePad.Devices[index].Left;
			if (control == "Right") return GamePad.Devices[index].Right;

			if (control.StartsWith("B"))
			{
				int buttonIndex = int.Parse(control.Substring(1)) - 1;
				if (buttonIndex >= GamePad.Devices[index].Buttons.Length)
					return false;
				return GamePad.Devices[index].Buttons[buttonIndex];
			}

			return false;
		}

		public static string GetPressedKey()
		{
            // Poll Joystick input
			for (int j = 0; j < GamePad.Devices.Count; j++)
			{
                if (GamePad.Devices[j].Up)    return "J" + (j+1) + " Up";
                if (GamePad.Devices[j].Down)  return "J" + (j+1) + " Down";
                if (GamePad.Devices[j].Left)  return "J" + (j+1) + " Left";
                if (GamePad.Devices[j].Right) return "J" + (j+1) + " Right";

			    var buttons = GamePad.Devices[j].Buttons;
                for (int b=0; b<buttons.Length; b++)
                {
                    if (buttons[b])
                        return "J" + (j+1) + " B" + (b+1);
                }
			}

		    return KeyInput.GetPressedKey();
		}
	}
}
