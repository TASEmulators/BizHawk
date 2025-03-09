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
					controller.BoolButtons.Add("P1 " + Inputs.Joystick + " " + button);

			if (settings.Controller2Type == ControllerType.Gamepad)
				foreach (var button in JoystickButtonCollection)
					controller.BoolButtons.Add("P2 " + Inputs.Joystick + " " + button);

			return controller.MakeImmutable();
		}

		private static string[] JoystickButtonCollection =
		[
			JoystickButtons.Up,
			JoystickButtons.Down,
			JoystickButtons.Left,
			JoystickButtons.Right,
			JoystickButtons.Button1,
			JoystickButtons.Button2
		];

		private static class JoystickButtons
		{
			public const string Up = "Up";
			public const string Down = "Down";
			public const string Left = "Left";
			public const string Right = "Right";
			public const string Button1 = "Button 1";
			public const string Button2 = "Button 2";
		}

		private static class Inputs
		{
			public const string Joystick = "Joystick";
		}
	}
}
