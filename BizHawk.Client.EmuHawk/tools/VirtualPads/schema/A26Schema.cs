using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Atari.Atari2600;

namespace BizHawk.Client.EmuHawk
{
	[Schema("A26")]
	// ReSharper disable once UnusedMember.Global
	public class A26Schema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			var ss = ((Atari2600)core).GetSyncSettings().Clone();

			var port1 = PadSchemaFromSetting(ss.Port1, 1);
			if (port1 != null)
			{
				yield return port1;
			}

			var port2 = PadSchemaFromSetting(ss.Port2, 2);
			if (port2 != null)
			{
				yield return port2;
			}

			yield return ConsoleButtons();
		}

		private static PadSchema PadSchemaFromSetting(Atari2600ControllerTypes type, int controller)
		{
			return type switch
			{
				Atari2600ControllerTypes.Unplugged => null,
				Atari2600ControllerTypes.Joystick => StandardController(controller),
				Atari2600ControllerTypes.Paddle => PaddleController(controller),
				Atari2600ControllerTypes.BoostGrip => BoostGripController(controller),
				Atari2600ControllerTypes.Driving => DrivingController(controller),
				_ => null
			};
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"Player {controller}",
				DefaultSize = new Size(174, 74),
				MaxSize = new Size(174, 74),
				Buttons = new[]
				{
					ButtonSchema.Up(23, 15, controller),
					ButtonSchema.Down(23, 36, controller),
					ButtonSchema.Left(2, 24, controller),
					ButtonSchema.Right(44, 24, controller),
					new ButtonSchema(124, 24, controller, "Button") { DisplayName = "B" }
				}
			};
		}

		private static PadSchema PaddleController(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"Player {controller}",
				DefaultSize = new Size(334, 94),
				MaxSize = new Size(334, 94),
				Buttons = new[]
				{
					new ButtonSchema(5, 24, controller, "Button 1")
					{
						DisplayName = "B1"
					},
					new ButtonSchema(5, 48, controller, "Button 2")
					{
						DisplayName = "B2"
					},
					new SingleFloatSchema(55, 17, controller, "Paddle X 1")
					{
						TargetSize = new Size(128, 69),
						MaxValue = 127,
						MinValue = -127
					},
					new SingleFloatSchema(193, 17, controller, "Paddle X 2")
					{
						TargetSize = new Size(128, 69),
						MaxValue = 127,
						MinValue = -127
					}
				}
			};
		}

		private static PadSchema BoostGripController(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"Player {controller}",
				DefaultSize = new Size(174, 74),
				MaxSize = new Size(174, 74),
				Buttons = new[]
				{
					ButtonSchema.Up(23, 15, controller),
					ButtonSchema.Down(23, 36, controller),
					ButtonSchema.Left(2, 24, controller),
					ButtonSchema.Right(44, 24, controller),
					new ButtonSchema(132, 24, controller, "Button") { DisplayName = "B" },
					new ButtonSchema(68, 36, "Button 1") { DisplayName = "B1" },
					new ButtonSchema(100, 36, "Button 2") { DisplayName = "B2" }
				}
			};
		}

		private static PadSchema DrivingController(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"Player {controller}",
				DefaultSize = new Size(334, 94),
				MaxSize = new Size(334, 94),
				Buttons = new[]
				{
					new ButtonSchema(5, 24, controller, "Button")
					{
						DisplayName = "B1"
					},
					new SingleFloatSchema(55, 17, controller, "Wheel X 1")
					{
						TargetSize = new Size(128, 69),
						MaxValue = 127,
						MinValue = -127
					},
					new SingleFloatSchema(193, 17, controller, "Wheel X 2")
					{
						TargetSize = new Size(128, 69),
						MaxValue = 127,
						MinValue = -127
					}
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				DefaultSize = new Size(185, 75),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Select"),
					new ButtonSchema(60, 15, "Reset"),
					new ButtonSchema(108, 15, "Power"),
					new ButtonSchema(10, 40, "Toggle Left Difficulty")
					{
						DisplayName = "Left Difficulty"
					},
					new ButtonSchema(92, 40, "Toggle Right Difficulty")
					{
						DisplayName = "Right Difficulty"
					}
				}
			};
		}
	}
}
