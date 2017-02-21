using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Atari.Atari7800;

namespace BizHawk.Client.EmuHawk
{
	[SchemaAttributes("A78")]
	public class A78Schema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			switch ((core as Atari7800).ControlAdapter.ControlType.Name)
			{
				case "Atari 7800 Joystick Controller":
					yield return JoystickController(1);
					yield return JoystickController(2);
					break;
				case "Atari 7800 Paddle Controller":
					yield return PaddleController(1);
					yield return PaddleController(2);
					break;
				case "Atari 7800 Keypad Controller":
					break;
				case "Atari 7800 Driving Controller":
					break;
				case "Atari 7800 Booster Grip Controller":
					break;
				case "Atari 7800 ProLine Joystick Controller":
					yield return ProLineController(1);
					yield return ProLineController(2);
					break;
				case "Atari 7800 Light Gun Controller":
					yield return LightGunController(1);
					yield return LightGunController(2);
					break;
			}

			
			yield return ConsoleButtons();
		}

		private static PadSchema ProLineController(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Player " + controller,
				IsConsole = false,
				DefaultSize = new Size(174, 74),
				MaxSize = new Size(174, 74),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(23, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(23, 36),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(44, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Trigger",
						DisplayName = "1",
						Location = new Point(120, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Trigger 2",
						DisplayName = "2",
						Location = new Point(145, 24),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema JoystickController(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Player " + controller,
				IsConsole = false,
				DefaultSize = new Size(174, 74),
				MaxSize = new Size(174, 74),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(23, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(23, 36),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(44, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Trigger",
						DisplayName = "1",
						Location = new Point(120, 24),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema PaddleController(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Player " + controller,
				IsConsole = false,
				DefaultSize = new Size(250, 74),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Paddle",
						DisplayName = "Paddle",
						Location = new Point(23, 15),
						Type = PadSchema.PadInputType.FloatSingle
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Trigger",
						DisplayName = "1",
						Location = new Point(12, 90),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema LightGunController(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Light Gun",
				IsConsole = false,
				DefaultSize = new Size(356, 290),
				MaxSize = new Size(356, 290),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " VPos",
						Location = new Point(14, 17),
						Type = PadSchema.PadInputType.TargetedPair,
						TargetSize = new Size(256, 240),
						SecondaryNames = new []
						{
							"P" + controller + " HPos",
						}
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Trigger",
						DisplayName = "Trigger",
						Location = new Point(284, 17),
						Type = PadSchema.PadInputType.Boolean
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
				DefaultSize = new Size(215, 50),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "Select",
						DisplayName = "Select",
						Location = new Point(10, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "Reset",
						DisplayName = "Reset",
						Location = new Point(60, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "Power",
						DisplayName = "Power",
						Location = new Point(108, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "Pause",
						DisplayName = "Pause",
						Location = new Point(158, 15),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}
	}
}
