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
				DefaultSize = new Size(200, 90),
				Buttons = new[]
				{
					ButtonSchema.Up(34, 17, $"P{controller} Up"),
					ButtonSchema.Down(34, 61, $"P{controller} Down"),
					ButtonSchema.Left(22, 39, $"P{controller} Left"),
					ButtonSchema.Right(44, 39, $"P{controller} Right"),
					new ButtonSchema(2, 10)
					{
						Name = $"P{controller} L",
						DisplayName = "L"
					},
					new ButtonSchema(174, 10)
					{
						Name = $"P{controller} R",
						DisplayName = "R"
					},
					new ButtonSchema(70, 39)
					{
						Name = $"P{controller} Select",
						DisplayName = "s"
					},
					new ButtonSchema(92, 39)
					{
						Name = $"P{controller} Start",
						DisplayName = "S"
					},
					new ButtonSchema(121, 39)
					{
						Name = $"P{controller} Y",
						DisplayName = "Y"
					},
					new ButtonSchema(145, 52)
					{
						Name = $"P{controller} B",
						DisplayName = "B"
					},
					new ButtonSchema(122, 15)
					{
						Name = $"P{controller} X",
						DisplayName = "X"
					},
					new ButtonSchema(146, 25)
					{
						Name = $"P{controller} A",
						DisplayName = "A"
					}
				}
			};
		}

		private static PadSchema Mouse(int controller)
		{
			var controllerDefRanges = new SnesMouseController().Definition.FloatRanges;
			return new PadSchema
			{
				DefaultSize = new Size(345, 225),
				Buttons = new[]
				{
					new AnalogSchema(6, 14, $"P{controller} Mouse X")
					{
						AxisRange = controllerDefRanges[0],
						SecondaryAxisRange = controllerDefRanges[1]
					},
					new ButtonSchema(275, 15)
					{
						Name = $"P{controller} Mouse Left",
						DisplayName = "Left"
					},
					new ButtonSchema(275, 45)
					{
						Name = $"P{controller} Mouse Right",
						DisplayName = "Right"
					}
				}
			};
		}

		private static PadSchema SuperScope(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Super Scope",
				DefaultSize = new Size(356, 290),
				MaxSize = new Size(356, 290),
				Buttons = new[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} Scope X")
					{
						TargetSize = new Size(256, 240)
					},
					new ButtonSchema(284, 17)
					{
						Name = $"P{controller} Trigger",
						DisplayName = "Trigger"
					},
					new ButtonSchema(284, 47)
					{
						Name = $"P{controller} Cursor",
						DisplayName = "Cursor"
					},
					new ButtonSchema(284, 77)
					{
						Name = $"P{controller} Turbo",
						DisplayName = "Turbo"
					},
					new ButtonSchema(284, 107)
					{
						Name = $"P{controller} Pause",
						DisplayName = "Pause"
					}
				}
			};
		}

		private static PadSchema Justifier(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Justifier",
				DefaultSize = new Size(356, 290),
				MaxSize = new Size(356, 290),
				Buttons = new[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} Justifier X")
					{
						TargetSize = new Size(256, 240)
					},
					new ButtonSchema(284, 17)
					{
						Name = $"P{controller} Trigger",
						DisplayName = "Trigger"
					},
					new ButtonSchema(284, 47)
					{
						Name = $"P{controller} Start",
						DisplayName = "Start"
					}
				}
			};
		}

		private static PadSchema Payload(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Payload",
				DefaultSize = new Size(460, 85),
				Buttons = PayLoadButtons(controller)
			};
		}

		private static IEnumerable<ButtonSchema> PayLoadButtons(int controller)
		{
			int startX = 5;
			int startY = 15;
			int buttonSpacingX = 28;
			int buttonSpacingY = 30;

			for (int i = 0; i < 16; i++)
			{
				yield return new ButtonSchema(startX + (i * buttonSpacingX), startY)
				{
					Name = $"P{controller} B{i}",
					DisplayName = i.ToString()
				};
			}

			for (int i = 0; i < 16; i++)
			{
				yield return new ButtonSchema(startX + (i * buttonSpacingX), startY + buttonSpacingY)
				{
					Name = $"P{controller} B{i + 16}",
					DisplayName = (i + 16).ToString()
				};
			}
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				DefaultSize = new Size(150, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Reset"),
					new ButtonSchema(58, 15, "Power")
				}
			};
		}
	}
}
