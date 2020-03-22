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

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				Size = new Size(200, 90),
				Buttons = new[]
				{
					ButtonSchema.Up(34, 17, controller),
					ButtonSchema.Down(34, 61, controller),
					ButtonSchema.Left(22, 39, controller),
					ButtonSchema.Right(44, 39, controller),
					new ButtonSchema(2, 10, controller, "L"),
					new ButtonSchema(174, 10, controller, "R"),
					new ButtonSchema(70, 39, controller, "Select") { DisplayName = "s" },
					new ButtonSchema(92, 39, controller, "Start") { DisplayName = "S" },
					new ButtonSchema(121, 39, controller, "Y"),
					new ButtonSchema(145, 52, controller, "B"),
					new ButtonSchema(122, 15, controller, "X"),
					new ButtonSchema(146, 25, controller, "A")
				}
			};
		}

		private static PadSchema Mouse(int controller)
		{
			var controllerDefRanges = new SnesMouseController().Definition.FloatRanges;
			return new PadSchema
			{
				Size = new Size(345, 225),
				Buttons = new[]
				{
					new AnalogSchema(6, 14, $"P{controller} Mouse X")
					{
						AxisRange = controllerDefRanges[0],
						SecondaryAxisRange = controllerDefRanges[1]
					},
					new ButtonSchema(275, 15, controller, "Mouse Left")
					{
						DisplayName = "Left"
					},
					new ButtonSchema(275, 45, controller, "Mouse Right")
					{
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
				Size = new Size(356, 290),
				Buttons = new[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} Scope X")
					{
						TargetSize = new Size(256, 240)
					},
					new ButtonSchema(284, 17, controller, "Trigger"),
					new ButtonSchema(284, 47, controller, "Cursor"),
					new ButtonSchema(284, 77, controller, "Turbo"),
					new ButtonSchema(284, 107, controller, "Pause")
				}
			};
		}

		private static PadSchema Justifier(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Justifier",
				Size = new Size(356, 290),
				Buttons = new[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} Justifier X")
					{
						TargetSize = new Size(256, 240)
					},
					new ButtonSchema(284, 17, controller, "Trigger"),
					new ButtonSchema(284, 47, controller, "Start")
				}
			};
		}

		private static PadSchema Payload(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Payload",
				Size = new Size(460, 85),
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
				yield return new ButtonSchema(
					startX + (i * buttonSpacingX),
					startY,
					controller,
					$"B{i}")
				{
					DisplayName = i.ToString()
				};
			}

			for (int i = 0; i < 16; i++)
			{
				yield return new ButtonSchema(
					startX + (i * buttonSpacingX),
					startY + buttonSpacingY,
					controller,
					$"B{i + 16}")
				{
					DisplayName = (i + 16).ToString()
				};
			}
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				Size = new Size(150, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Reset"),
					new ButtonSchema(58, 15, "Power")
				}
			};
		}
	}
}
