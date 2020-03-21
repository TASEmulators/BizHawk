using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Nintendo.SNES9X;

namespace BizHawk.Client.EmuHawk
{
	[Schema("SNES")]
	// ReSharper disable once UnusedMember.Global
	public class SnesSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			if (core is LibsnesCore bsnes)
			{
				return GetBsnesPadSchemas(bsnes);
			}

			return GetSnes9xPadSchemas((Snes9x)core);
		}
		private IEnumerable<PadSchema> GetSnes9xPadSchemas(Snes9x core)
		{
			// Only standard controller is supported on the left port
			yield return StandardController(1);

			Snes9x.SyncSettings syncSettings = core.GetSyncSettings();
			LibSnes9x.RightPortDevice rightPort = syncSettings.RightPort;

			switch (rightPort)
			{
				default:
				case LibSnes9x.RightPortDevice.Joypad:
					yield return StandardController(2);
					break;
				case LibSnes9x.RightPortDevice.Justifier:
					yield return Justifier(2);
					break;
				case LibSnes9x.RightPortDevice.Mouse:
					yield return Mouse(2);
					break;
				case LibSnes9x.RightPortDevice.Multitap:
					yield return StandardController(2);
					yield return StandardController(3);
					yield return StandardController(4);
					yield return StandardController(5);
					break;
				case LibSnes9x.RightPortDevice.SuperScope:
					yield return SuperScope(2);
					break;
			}

			yield return ConsoleButtons();
		}

		private IEnumerable<PadSchema> GetBsnesPadSchemas(LibsnesCore core)
		{
			var syncSettings = core.GetSyncSettings();

			var ports = new[]
			{
				syncSettings.LeftPort,
				syncSettings.RightPort
			};

			int offset = 0;
			for (int i = 0; i < 2; i++)
			{
				int playerNum = i + offset + 1;
				switch (ports[i])
				{
					default:
					case LibsnesControllerDeck.ControllerType.Unplugged:
						offset -= 1;
						break;
					case LibsnesControllerDeck.ControllerType.Gamepad:
						yield return StandardController(playerNum);
						break;
					case LibsnesControllerDeck.ControllerType.Multitap:
						for (int j = 0; j < 4; j++)
						{
							yield return StandardController(playerNum + j);
						}

						offset += 3;
						break;
					case LibsnesControllerDeck.ControllerType.Mouse:
						yield return Mouse(playerNum);
						break;
					case LibsnesControllerDeck.ControllerType.SuperScope:
						yield return SuperScope(playerNum);
						break;
					case LibsnesControllerDeck.ControllerType.Justifier:
						for (int j = 0; j < 2; j++)
						{
							yield return Justifier(playerNum);
							offset += j;
						}

						break;
					case LibsnesControllerDeck.ControllerType.Payload:
						yield return Payload(playerNum);
						break;
				}
			}

			yield return ConsoleButtons();
		}

		private static PadSchema StandardController(int controller, int rowOffset = 0)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(200, 90),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Up",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(34, 17),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Down",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(34, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left",
						Icon = Properties.Resources.Back,
						Location = new Point(22, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right",
						Icon = Properties.Resources.Forward,
						Location = new Point(44, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} L",
						DisplayName = "L",
						Location = new Point(2, 10),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} R",
						DisplayName = "R",
						Location = new Point(174, 10),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Select",
						DisplayName = "s",
						Location = new Point(70, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "S",
						Location = new Point(92, 39),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Y",
						DisplayName = "Y",
						Location = new Point(121, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} B",
						DisplayName = "B",
						Location = new Point(145, 52),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} X",
						DisplayName = "X",
						Location = new Point(122, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} A",
						DisplayName = "A",
						Location = new Point(146, 25),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema Mouse(int controller)
		{
			var controllerDefRanges = new SnesMouseController().Definition.FloatRanges;
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(345, 225),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Mouse X",
						AxisRange = controllerDefRanges[0],
						SecondaryAxisRange = controllerDefRanges[1],
						Location = new Point(6, 14),
						Type = PadSchema.PadInputType.AnalogStick
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Mouse Left",
						DisplayName = "Left",
						Location = new Point(275, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Mouse Right",
						DisplayName = "Right",
						Location = new Point(275, 45),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema SuperScope(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Super Scope",
				IsConsole = false,
				DefaultSize = new Size(356, 290),
				MaxSize = new Size(356, 290),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Scope X",
						Location = new Point(14, 17),
						Type = PadSchema.PadInputType.TargetedPair,
						TargetSize = new Size(256, 240),
						SecondaryNames = new[]
						{
							$"P{controller} Scope Y",
						}
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Trigger",
						DisplayName = "Trigger",
						Location = new Point(284, 17),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Cursor",
						DisplayName = "Cursor",
						Location = new Point(284, 47),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Turbo",
						DisplayName = "Turbo",
						Location = new Point(284, 77),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Pause",
						DisplayName = "Pause",
						Location = new Point(284, 107),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema Justifier(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Justifier",
				IsConsole = false,
				DefaultSize = new Size(356, 290),
				MaxSize = new Size(356, 290),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Justifier X",
						Location = new Point(14, 17),
						Type = PadSchema.PadInputType.TargetedPair,
						TargetSize = new Size(256, 240),
						SecondaryNames = new[]
						{
							$"P{controller} Justifier Y",
						}
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Trigger",
						DisplayName = "Trigger",
						Location = new Point(284, 17),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "Start",
						Location = new Point(284, 47),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema Payload(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Payload",
				IsConsole = false,
				DefaultSize = new Size(460, 85),
				Buttons = PayLoadButtons(controller)
			};
		}

		private static IEnumerable<PadSchema.ButtonSchema> PayLoadButtons(int controller)
		{
			int startX = 5;
			int startY = 15;
			int buttonSpacingX = 28;
			int buttonSpacingY = 30;

			for (int i = 0; i < 16; i++)
			{
				yield return new PadSchema.ButtonSchema
				{
					Name = $"P{controller} B{i}",
					DisplayName = i.ToString(),
					Location = new Point(startX + (i * buttonSpacingX), startY),
					Type = PadSchema.PadInputType.Boolean
				};
			}

			for (int i = 0; i < 16; i++)
			{
				yield return new PadSchema.ButtonSchema
				{
					Name = $"P{controller} B{i + 16}",
					DisplayName = (i + 16).ToString(),
					Location = new Point(startX + (i * buttonSpacingX), startY + buttonSpacingY),
					Type = PadSchema.PadInputType.Boolean
				};
			}
		}

		private static PadSchema ConsoleButtons()
		{
			return new PadSchema
			{
				DisplayName = "Console",
				IsConsole = true,
				DefaultSize = new Size(150, 50),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = "Reset",
						Location = new Point(10, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Power",
						Location = new Point(58, 15),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}
	}
}
