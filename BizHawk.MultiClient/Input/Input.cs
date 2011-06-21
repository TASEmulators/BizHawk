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
			string[] controls = control.Split('+');
			for (int i = 0; i < controls.Length; i++)
			{
				if (IsPressedSingle(controls[i]) == false)
					return false;
			}
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
			//for (int j = 0; j < GamePad.Devices.Count; j++)
			return null;
		}
	}
}
