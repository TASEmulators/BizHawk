using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sony.PSP
{
	public partial class PPSSPP
	{
		private static ControllerDefinition CreateControllerDefinition(SyncSettings settings, bool isMultiDisc)
		{
			var controller = new ControllerDefinition("PSP Controller");

			foreach (var button in JoystickButtonCollection) controller.BoolButtons.Add($"P1 {button}");
			foreach (var axis in JoystickAxisCollection) controller.AddAxis($"P1 {axis}", (-32768).RangeTo(32767), 0);

			// If this is multi-disc, add a cd swap option
			if (isMultiDisc)
			{
				controller.BoolButtons.Add("Next Disc");
				controller.BoolButtons.Add("Prev Disc");
			}

			return controller.MakeImmutable();
		}


		private static string[] JoystickButtonCollection = [
			JoystickButtons.Up,
			JoystickButtons.Down,
			JoystickButtons.Left,
			JoystickButtons.Right,
			JoystickButtons.Start,
			JoystickButtons.Select,
			JoystickButtons.ButtonSquare,
			JoystickButtons.ButtonTriangle,
			JoystickButtons.ButtonCircle,
			JoystickButtons.ButtonCross,
			JoystickButtons.ButtonLTrigger,
			JoystickButtons.ButtonRTrigger,
		];

		private static string[] JoystickAxisCollection = [
			JoystickAxes.RightAnalogX,
			JoystickAxes.RightAnalogY,
			JoystickAxes.LeftAnalogX,
			JoystickAxes.LeftAnalogY,
		];

		private static class JoystickButtons
		{
			public const string Up = "Up";
			public const string Down = "Down";
			public const string Left = "Left";
			public const string Right = "Right";
			public const string Start = "Start";
			public const string Select = "Select";
			public const string ButtonSquare = "Square";
			public const string ButtonTriangle = "Triangle";
			public const string ButtonCircle = "Circle";
			public const string ButtonCross = "Cross";
			public const string ButtonLTrigger = "L Trigger";
			public const string ButtonRTrigger = "R Trigger";
		}

		private static class JoystickAxes
		{
			public const string RightAnalogX = "Right Analog X";
			public const string RightAnalogY = "Right Analog Y";
			public const string LeftAnalogX = "Left Analog X";
			public const string LeftAnalogY = "Left Analog Y";
		}
	}
}
