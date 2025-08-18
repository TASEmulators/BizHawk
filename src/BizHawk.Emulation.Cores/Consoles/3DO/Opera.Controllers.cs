using BizHawk.Common.CollectionExtensions;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Panasonic3DO
{
	public partial class Opera
	{
		private static ControllerDefinition CreateControllerDefinition(SyncSettings settings, bool isMultiDisc)
		{
			var controller = new ControllerDefinition("3DO Controller");
			setPortControllers(1, settings.Controller1Type, controller);
			setPortControllers(2, settings.Controller2Type, controller);

			// If this is multi-disc, add a cd swap option
			if (isMultiDisc)
			{
				controller.BoolButtons.Add("Next Disc");
				controller.BoolButtons.Add("Prev Disc");
			}

			// Adding Reset button
			controller.BoolButtons.Add("Reset");

			return controller.MakeImmutable();
		}

		public const int MOUSE_MIN_POS_X = -64;
		public const int MOUSE_MAX_POS_X = 64;
		public const int MOUSE_MIN_POS_Y = -64;
		public const int MOUSE_MAX_POS_Y = 64;

		public const int POINTER_MIN_POS = -32768;
		public const int POINTER_MAX_POS = 32767;
		private static void setPortControllers(int port, ControllerType type, ControllerDefinition controller)
		{
			switch (type)
			{
				case ControllerType.Gamepad:
					foreach (var button in JoystickButtonCollection) controller.BoolButtons.Add($"P{port} {button}");
					break;
				case ControllerType.Mouse:
					controller.BoolButtons.AddRange([
						$"P{port} {Inputs.MouseLeftButton}",
						$"P{port} {Inputs.MouseMiddleButton}",
						$"P{port} {Inputs.MouseRightButton}",
						$"P{port} {Inputs.MouseFourthButton}",
					]);
					controller.AddAxis($"P{port} {Inputs.MouseX}", MOUSE_MIN_POS_X.RangeTo(MOUSE_MAX_POS_X), (MOUSE_MIN_POS_X + MOUSE_MAX_POS_X) / 2)
						.AddAxis($"P{port} {Inputs.MouseY}", MOUSE_MIN_POS_Y.RangeTo(MOUSE_MAX_POS_Y), (MOUSE_MIN_POS_Y + MOUSE_MAX_POS_Y) / 2);
					break;
				case ControllerType.FlightStick:
					foreach (var button in FlightStickButtonCollection) controller.BoolButtons.Add($"P{port} {button}");
					controller.AddAxis($"P{port} {Inputs.FlighStickHorizontalAxis}", MOUSE_MIN_POS_X.RangeTo(MOUSE_MAX_POS_X), (MOUSE_MIN_POS_X + MOUSE_MAX_POS_X) / 2)
						.AddAxis($"P{port} {Inputs.FlighStickVerticalAxis}", MOUSE_MIN_POS_Y.RangeTo(MOUSE_MAX_POS_Y), (MOUSE_MIN_POS_Y + MOUSE_MAX_POS_Y) / 2)
						.AddAxis($"P{port} {Inputs.FlighStickAltitudeAxis}", MOUSE_MIN_POS_Y.RangeTo(MOUSE_MAX_POS_Y), (MOUSE_MIN_POS_Y + MOUSE_MAX_POS_Y) / 2);
					break;
				case ControllerType.LightGun:
					foreach (var button in LightGunButtonCollection) controller.BoolButtons.Add($"P{port} {button}");
					controller.AddAxis($"P{port} {Inputs.LightGunScreenX}", (POINTER_MIN_POS).RangeTo(POINTER_MAX_POS), 0)
						.AddAxis($"P{port} {Inputs.LightGunScreenY}", (POINTER_MIN_POS).RangeTo(POINTER_MAX_POS), 0);
					break;
				case ControllerType.ArcadeLightGun:
					foreach (var button in ArcadeLightGunButtonCollection) controller.BoolButtons.Add($"P{port} {button}");
					controller.AddAxis($"P{port} {Inputs.LightGunScreenX}", (POINTER_MIN_POS).RangeTo(POINTER_MAX_POS), 0)
						.AddAxis($"P{port} {Inputs.LightGunScreenY}", (POINTER_MIN_POS).RangeTo(POINTER_MAX_POS), 0);
					break;
				case ControllerType.OrbatakTrackball:
					foreach (var button in OrbatakTrackballCollection) controller.BoolButtons.Add($"P{port} {button}");
					controller.AddAxis($"P{port} {Inputs.TrackballPosX}", MOUSE_MIN_POS_X.RangeTo(MOUSE_MAX_POS_X), (MOUSE_MIN_POS_X + MOUSE_MAX_POS_X) / 2)
						.AddAxis($"P{port} {Inputs.TrackballPosY}", MOUSE_MIN_POS_Y.RangeTo(MOUSE_MAX_POS_Y), (MOUSE_MIN_POS_Y + MOUSE_MAX_POS_Y) / 2);
					break;
			}
		}

		private static string[] JoystickButtonCollection = [
			JoystickButtons.Up,
			JoystickButtons.Down,
			JoystickButtons.Left,
			JoystickButtons.Right,
			JoystickButtons.ButtonX,
			JoystickButtons.ButtonP,
			JoystickButtons.ButtonA,
			JoystickButtons.ButtonB,
			JoystickButtons.ButtonC,
			JoystickButtons.ButtonL,
			JoystickButtons.ButtonR,
		];

		private static string[] FlightStickButtonCollection = [
			FlightStickButtons.Up,
			FlightStickButtons.Down,
			FlightStickButtons.Left,
			FlightStickButtons.Right,
			FlightStickButtons.Fire,
			FlightStickButtons.ButtonA,
			FlightStickButtons.ButtonB,
			FlightStickButtons.ButtonX,
			FlightStickButtons.ButtonP,
			FlightStickButtons.LeftTrigger,
			FlightStickButtons.RightTrigger,
		];

		private static string[] LightGunButtonCollection = [
			LightGunButtons.Trigger,
			LightGunButtons.Select,
			LightGunButtons.Reload,
			LightGunButtons.IsOffScreen,
		];

		private static string[] ArcadeLightGunButtonCollection = [
			ArcadeLightGunButtons.Trigger,
			ArcadeLightGunButtons.Select,
			ArcadeLightGunButtons.Start,
			ArcadeLightGunButtons.Reload,
			ArcadeLightGunButtons.AuxA,
			ArcadeLightGunButtons.IsOffScreen,
		];

		private static string[] OrbatakTrackballCollection = [
			OrbatakTrackballButtons.StartP1,
			OrbatakTrackballButtons.StartP2,
			OrbatakTrackballButtons.CoinP1,
			OrbatakTrackballButtons.CoinP2,
			OrbatakTrackballButtons.Service,
		];

		private static class JoystickButtons
		{
			public const string Up = "Up";
			public const string Down = "Down";
			public const string Left = "Left";
			public const string Right = "Right";
			public const string ButtonX = "X";
			public const string ButtonP = "P";
			public const string ButtonA = "A";
			public const string ButtonB = "B";
			public const string ButtonC = "C";
			public const string ButtonL = "L";
			public const string ButtonR = "R";
		}
		private static class FlightStickButtons
		{
			public const string Up = "Up";
			public const string Down = "Down";
			public const string Left = "Left";
			public const string Right = "Right";
			public const string Fire = "Fire";
			public const string ButtonA = "A";
			public const string ButtonB = "B";
			public const string ButtonC = "C";
			public const string LeftTrigger = "LT";
			public const string RightTrigger = "RT";
			public const string ButtonP = "P";
			public const string ButtonX = "X";
		}

		private static class LightGunButtons
		{
			public const string Trigger = "Trigger";
			public const string Select = "Select";
			public const string Reload = "Reload";
			public const string IsOffScreen = "Is Off-Screen";
		}
		private static class ArcadeLightGunButtons
		{
			public const string Trigger = "Trigger";
			public const string Select = "Select";
			public const string Start = "Start";
			public const string Reload = "Reload";
			public const string AuxA = "Aux A";
			public const string IsOffScreen = "Is Off-Screen";
		}

		private static class OrbatakTrackballButtons
		{
			public const string StartP1 = "Start P1";
			public const string StartP2 = "Start P2";
			public const string CoinP1 = "Coin P1";
			public const string CoinP2 = "Coin P2";
			public const string Service = "Service";
		}

		private static class Inputs
		{
			public const string Joystick = "Joystick";
			public const string MouseLeftButton = "Left Button";
			public const string MouseRightButton = "Right Button";
			public const string MouseMiddleButton = "Middle Button";
			public const string MouseFourthButton = "Fourth Button";
			public const string MouseX = "Mouse X";
			public const string MouseY = "Mouse Y";
			public const string FlighStickHorizontalAxis = "Flight Stick Horizontal Axis";
			public const string FlighStickVerticalAxis = "Flight Stick Vertical Axis";
			public const string FlighStickAltitudeAxis = "Flight Stick Altitude Axis";
			public const string LightGunScreenX = "Light Gun Screen X";
			public const string LightGunScreenY = "Light Gun Screen Y";
			public const string TrackballPosX = "Trackball X";
			public const string TrackballPosY = "Trackball Y";
		}
	}
}
