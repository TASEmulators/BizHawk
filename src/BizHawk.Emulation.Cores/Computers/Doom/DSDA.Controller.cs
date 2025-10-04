using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA
	{
		public static ControllerDefinition CreateControllerDefinition(DoomSyncSettings settings)
		{
			var controller = new ControllerDefinition("Doom Controller");
			var longtics = settings.TurningResolution == TurningResolution.Longtics;

			for (int port = 1; port <= 4; port++)
			{
				if (PlayerPresent(settings, port))
				{
					controller
						.AddAxis($"P{port} Run Speed",    (-50).RangeTo(50), 0)
						.AddAxis($"P{port} Strafe Speed", (-50).RangeTo(50), 0)
						.AddAxis($"P{port} Turn Speed",  (-128).RangeTo(127), 0);

					// editing a short in tastudio would be a nightmare, so we split it:
					// high byte represents shorttics mode and whole angle values
					// low byte is fractional part only available with longtics
					if (longtics)
					{
						controller.AddAxis($"P{port} Turn Speed Frac.", (-255).RangeTo(255), 0);
					}

					controller
						.AddAxis($"P{port} Weapon Select", 0.RangeTo(7), 0)
						.AddAxis($"P{port} Mouse Run", (-128).RangeTo(127), 0)
						// current max raw mouse delta is 180
						.AddAxis($"P{port} Mouse Turn", (longtics ? -180 : -128)
							.RangeTo(longtics ? 180 : 127), 0);

					if (settings.InputFormat is not ControllerType.Doom)
					{
						controller
							.AddAxis($"P{port} Look", (-7).RangeTo(8), 0)
							.AddAxis($"P{port} Fly",  (-7).RangeTo(8), 0)
							.AddAxis($"P{port} Use Artifact", 0.RangeTo(10), 0);
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

					if (settings.InputFormat is ControllerType.Hexen)
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

					if (settings.InputFormat is not ControllerType.Doom)
					{
						if (settings.InputFormat is ControllerType.Heretic)
						{
							// TODO
							//controller.BoolButtons.Add($"P{port} Inventory Skip");
						}

						controller.BoolButtons.AddRange([
							$"P{port} Inventory Left",
							$"P{port} Inventory Right",
							$"P{port} Use Artifact",
							$"P{port} Look Up",
							$"P{port} Look Down",
							$"P{port} Look Center",
							$"P{port} Fly Up",
							$"P{port} Fly Down",
							$"P{port} Fly Center",
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

			controller
				.AddAxis($"Camera Mode",           (-1).RangeTo(2),  -1)
				.AddAxis($"Camera Run Speed",    (-128).RangeTo(127), 0)
				.AddAxis($"Camera Strafe Speed", (-128).RangeTo(127), 0)
				.AddAxis($"Camera Turn Speed",   (-128).RangeTo(127), 0)
				.AddAxis($"Camera Fly",          (-128).RangeTo(127), 0);

			controller.BoolButtons.Add("Camera Reset");

			return controller.MakeImmutable();
		}
	}
}
