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
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(23, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(23, 36),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(44, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button",
						DisplayName = "B",
						Location = new Point(124, 24),
						Type = PadSchema.PadInputType.Boolean
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
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button 1",
						DisplayName = "B1",
						Location = new Point(5, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button 2",
						DisplayName = "B2",
						Location = new Point(5, 48),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Paddle X 1",
						DisplayName = "Paddle X 1",
						Location = new Point(55, 17),
						Type = PadSchema.PadInputType.FloatSingle,
						TargetSize = new Size(128, 69),
						MaxValue = 127,
						MinValue = -127
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Paddle X 2",
						DisplayName = "Paddle X 2",
						Location = new Point(193, 17),
						Type = PadSchema.PadInputType.FloatSingle,
						TargetSize = new Size(128, 69),
						MaxValue = 127,
						MinValue = -127
					},
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
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(23, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(23, 36),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(44, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button",
						DisplayName = "B",
						Location = new Point(132, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button 1",
						DisplayName = "B1",
						Location = new Point(68, 36),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button 2",
						DisplayName = "B2",
						Location = new Point(100, 36),
						Type = PadSchema.PadInputType.Boolean
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
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button",
						DisplayName = "B1",
						Location = new Point(5, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Wheel X 1",
						DisplayName = "Wheel X 1",
						Location = new Point(55, 17),
						Type = PadSchema.PadInputType.FloatSingle,
						TargetSize = new Size(128, 69),
						MaxValue = 127,
						MinValue = -127
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Wheel X 2",
						DisplayName = "Wheel X 2",
						Location = new Point(193, 17),
						Type = PadSchema.PadInputType.FloatSingle,
						TargetSize = new Size(128, 69),
						MaxValue = 127,
						MinValue = -127
					},
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
					new PadSchema.ButtonSchema
					{
						Name = "Select",
						DisplayName = "Select",
						Location = new Point(10, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Reset",
						DisplayName = "Reset",
						Location = new Point(60, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Power",
						DisplayName = "Power",
						Location = new Point(108, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Toggle Left Difficulty",
						DisplayName = "Left Difficulty",
						Location = new Point(10, 40),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Toggle Right Difficulty",
						DisplayName = "Right Difficulty",
						Location = new Point(92, 40),
						Type = PadSchema.PadInputType.Boolean
					},
				}
			};
		}
	}
}
