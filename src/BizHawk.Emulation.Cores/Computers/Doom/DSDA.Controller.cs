using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA
	{
		public static ControllerDefinition CreateControllerDefinition(DoomSyncSettings settings)
		{
			var controller = new ControllerDefinition($"{settings.InputFormat} Input Format");
			var longtics = settings.TurningResolution == TurningResolution.Longtics;
			int playersPresent = Convert.ToInt32(settings.Player1Present)
				| Convert.ToInt32(settings.Player2Present) << 1
				| Convert.ToInt32(settings.Player3Present) << 2
				| Convert.ToInt32(settings.Player4Present) << 3;

			for (int i = 0; i < 4; i++)
			{
				if ((playersPresent & (1 << i)) is not 0)
				{
					var port = i + 1;

					controller
						.AddAxis($"P{port} Run Speed", (-50).RangeTo(50), 0)
						.AddAxis($"P{port} Strafing Speed", (-50).RangeTo(50), 0)
						.AddAxis($"P{port} Turning Speed", (-128).RangeTo(127), 0);

					// editing a short in tastudio would be a nightmare, so we split it:
					// high byte represents shorttics mode and whole angle values
					// low byte is fractional part only available with longtics
					if (longtics)
					{
						controller.AddAxis($"P{port} Turning Speed Frac.", (-255).RangeTo(255), 0);
					}

					controller
						.AddAxis($"P{port} Weapon Select", (0).RangeTo(7), 0)
						.AddAxis($"P{port} Mouse Running", (-128).RangeTo(127), 0)
						// current max raw mouse delta is 180
						.AddAxis($"P{port} Mouse Turning", (longtics ? -180 : -128).RangeTo(longtics ? 180 : 127), 0);

					if (settings.InputFormat is not ControllerTypes.Doom)
					{
						controller
							.AddAxis($"P{port} Fly / Look", (-7).RangeTo(7), 0)
							.AddAxis($"P{port} Use Artifact", (0).RangeTo(10), 0);
					}

					controller.BoolButtons.AddRange([
						$"P{port} Fire",
						$"P{port} Use",
						$"P{port} Forward",
						$"P{port} Backward",
						$"P{port} Turn Left",
						$"P{port} Turn Right",
						$"P{port} Strafe Left",
						$"P{port} Strafe Right",
						$"P{port} Run",
						$"P{port} Strafe",
						$"P{port} Weapon Select 1",
						$"P{port} Weapon Select 2",
						$"P{port} Weapon Select 3",
						$"P{port} Weapon Select 4",
					]);

					if (settings.InputFormat is ControllerTypes.Hexen)
					{
						controller.BoolButtons.AddRange([
							$"P{port} Jump",
							$"P{port} End Player",
						]);
					}
					else
					{
						controller.BoolButtons.AddRange([
							$"P{port} Weapon Select 5",
							$"P{port} Weapon Select 6",
							$"P{port} Weapon Select 7",
						]);
					}
				}
			}

			controller.BoolButtons.AddRange([
				"Change Gamma",
				"Automap Toggle",
				"Automap +",
				"Automap -",
				"Automap Full/Zoom",
				"Automap Follow",
				"Automap Up",
				"Automap Down",
				"Automap Right",
				"Automap Left",
				"Automap Grid",
				"Automap Mark",
				"Automap Clear Marks"
			]);

			return controller.MakeImmutable();
		}
	}
}
