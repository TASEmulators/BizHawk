using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Consoles._3DO
{
	public partial class Opera
	{ 
		private static ControllerDefinition CreateControllerDefinition(SyncSettings settings)
		{
			var controller = new ControllerDefinition("DOSBox Controller");

			// Adding joystick buttons
			if (settings.Controller1Type == ControllerType.Gamepad)
				foreach (var button in JoystickButtonCollection)
					controller.BoolButtons.Add("P1 " + button);

			if (settings.Controller2Type == ControllerType.Gamepad)
				foreach (var button in JoystickButtonCollection)
					controller.BoolButtons.Add("P2 " + button);

			return controller.MakeImmutable();
		}
		private enum JoystickButtonCodes  
		{
			ButtonB  = 0b0000000000000001,
			ButtonY  = 0b0000000000000010,
			Select   = 0b0000000000000100,
			Start    = 0b0000000000001000,
			Up       = 0b0000000000010000,
			Down     = 0b0000000000100000,
			Left     = 0b0000000001000000,
			Right    = 0b0000000010000000,
			ButtonA  = 0b0000000100000000,
			ButtonX  = 0b0000001000000000,
			ButtonL  = 0b0000010000000000,
			ButtonR  = 0b0000100000000000,
			ButtonL2 = 0b0001000000000000,
			ButtonR2 = 0b0010000000000000,
			ButtonL3 = 0b0100000000000000,
			ButtonR3 = 0b1000000000000000,
		};

		private static string[] JoystickButtonCollection =
		[
			JoystickButtons.Up,
			JoystickButtons.Down,
			JoystickButtons.Left,
			JoystickButtons.Right,
			JoystickButtons.Start,
			JoystickButtons.Select,
			JoystickButtons.ButtonA,
			JoystickButtons.ButtonB,
			JoystickButtons.ButtonX,
			JoystickButtons.ButtonY,
			JoystickButtons.ButtonL,
			JoystickButtons.ButtonR,
			JoystickButtons.ButtonL2,
			JoystickButtons.ButtonR2,
			JoystickButtons.ButtonL3,
			JoystickButtons.ButtonR3,
		];

		private static class JoystickButtons
		{
			public const string Up = "Up";
			public const string Down = "Down";
			public const string Left = "Left";
			public const string Right = "Right";
			public const string Start = "Start";
			public const string Select = "Select";
			public const string ButtonA = "A";
			public const string ButtonB = "B";
			public const string ButtonX = "X";
			public const string ButtonY = "Y";
			public const string ButtonL = "L";
			public const string ButtonR = "R";
			public const string ButtonL2 = "L2";
			public const string ButtonR2 = "R2";
			public const string ButtonL3 = "L3";
			public const string ButtonR3 = "R3";
		}

		private static class Inputs
		{
			public const string Joystick = "Joystick";
		}
	}
}
