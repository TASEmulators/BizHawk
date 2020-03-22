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
				IsConsole = false,
				DefaultSize = new Size(174, 74),
				MaxSize = new Size(174, 74),
				Buttons = new[]
				{
					ButtonSchema.Up(23, 15, $"P{controller} Up"),
					ButtonSchema.Down(23, 36, $"P{controller} Down"),
					ButtonSchema.Left(2, 24, $"P{controller} Left"),
					ButtonSchema.Right(44, 24, $"P{controller} Right"), 
					new ButtonSchema(124, 24)
					{
						Name = $"P{controller} Button",
						DisplayName = "B"
					}
				}
			};
		}

		private static PadSchema PaddleController(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"Player {controller}",
				IsConsole = false,
				DefaultSize = new Size(334, 94),
				MaxSize = new Size(334, 94),
				Buttons = new[]
				{
					new ButtonSchema(5, 24)
					{
						Name = $"P{controller} Button 1",
						DisplayName = "B1"
					},
					new ButtonSchema(5, 48)
					{
						Name = $"P{controller} Button 2",
						DisplayName = "B2"
					},
					new ButtonSchema(55, 17)
					{
						Name = $"P{controller} Paddle X 1",
						DisplayName = "Paddle X 1",
						Type = PadInputType.FloatSingle,
						TargetSize = new Size(128, 69),
						MaxValue = 127,
						MinValue = -127
					},
					new ButtonSchema(193, 17)
					{
						Name = $"P{controller} Paddle X 2",
						DisplayName = "Paddle X 2",
						Type = PadInputType.FloatSingle,
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
				IsConsole = false,
				DefaultSize = new Size(174, 74),
				MaxSize = new Size(174, 74),
				Buttons = new[]
				{
					ButtonSchema.Up(23, 15, $"P{controller} Up"),
					ButtonSchema.Down(23, 36, $"P{controller} Down"),
					ButtonSchema.Left(2, 24, $"P{controller} Left"),
					ButtonSchema.Right(44, 24, $"P{controller} Right"),
					new ButtonSchema(132, 24)
					{
						Name = $"P{controller} Button",
						DisplayName = "B"
					},
					new ButtonSchema(68, 36)
					{
						Name = $"P{controller} Button 1",
						DisplayName = "B1"
					},
					new ButtonSchema(100, 36)
					{
						Name = $"P{controller} Button 2",
						DisplayName = "B2"
					}
				}
			};
		}

		private static PadSchema DrivingController(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"Player {controller}",
				IsConsole = false,
				DefaultSize = new Size(334, 94),
				MaxSize = new Size(334, 94),
				Buttons = new[]
				{
					new ButtonSchema(5, 24)
					{
						Name = $"P{controller} Button",
						DisplayName = "B1"
					},
					new ButtonSchema(55, 17)
					{
						Name = $"P{controller} Wheel X 1",
						DisplayName = "Wheel X 1",
						Type = PadInputType.FloatSingle,
						TargetSize = new Size(128, 69),
						MaxValue = 127,
						MinValue = -127
					},
					new ButtonSchema(193, 17)
					{
						Name = $"P{controller} Wheel X 2",
						DisplayName = "Wheel X 2",
						Type = PadInputType.FloatSingle,
						TargetSize = new Size(128, 69),
						MaxValue = 127,
						MinValue = -127
					}
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new PadSchema
			{
				DisplayName = "Console",
				IsConsole = true,
				DefaultSize = new Size(185, 75),
				Buttons = new[]
				{
					new ButtonSchema(10, 15) { Name = "Select" },
					new ButtonSchema(60, 15) { Name = "Reset" },
					new ButtonSchema(108, 15) { Name = "Power" },
					new ButtonSchema(10, 40)
					{
						Name = "Toggle Left Difficulty",
						DisplayName = "Left Difficulty"
					},
					new ButtonSchema(92, 40)
					{
						Name = "Toggle Right Difficulty",
						DisplayName = "Right Difficulty"
					}
				}
			};
		}
	}
}
