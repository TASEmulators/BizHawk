using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Sony.PSX;

using static BizHawk.Emulation.Common.ControllerDefinition;

namespace BizHawk.Client.EmuHawk
{
	[Schema("PSX")]
	// ReSharper disable once UnusedMember.Global
	public class PsxSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			var psx = (Octoshock)core;
			var settings = psx.GetSyncSettings();

			var fioConfig = settings.FIOConfig.ToLogical();
			for (int i = 0; i < fioConfig.DevicesPlayer.Length; i++)
			{
				if (fioConfig.DevicesPlayer[i] == OctoshockDll.ePeripheralType.None)
				{
					continue;
				}

				int pNum = i + 1;
				if (fioConfig.DevicesPlayer[i] == OctoshockDll.ePeripheralType.DualAnalog || fioConfig.DevicesPlayer[i] == OctoshockDll.ePeripheralType.DualShock)
				{
					yield return DualShockController(pNum);
				}

				if (fioConfig.DevicesPlayer[i] == OctoshockDll.ePeripheralType.Pad)
				{
					yield return GamePadController(pNum);
				}

				if (fioConfig.DevicesPlayer[i] == OctoshockDll.ePeripheralType.NegCon)
				{
					yield return NeGcon(pNum);
				}
			}

			yield return ConsoleButtons(psx);
		}

		private static PadSchema DualShockController(int controller)
		{
			var stickRanges = CreateAxisRangePair(0, 128, 255, AxisPairOrientation.RightAndDown);
			return new PadSchema
			{
				Size = new Size(500, 290),
				DisplayName = $"DualShock Player{controller}",
				Buttons = new PadSchemaControl[]
				{
					ButtonSchema.Up(32, 50, controller),
					ButtonSchema.Down(32, 71, controller),
					ButtonSchema.Left(11, 62, controller),
					ButtonSchema.Right(53, 62, controller),
					new ButtonSchema(3, 32, controller, "L1"),
					new ButtonSchema(191, 32, controller, "R1"),
					new ButtonSchema(3, 10, controller, "L2"),
					new ButtonSchema(191, 10, controller, "R2"),
					new ButtonSchema(72, 90, controller, "L3"),
					new ButtonSchema(130, 90, controller, "R3"),
					new ButtonSchema(148, 62, controller, "Square")
					{
						Icon = Properties.Resources.Square
					},
					new ButtonSchema(169, 50, controller, "Triangle")
					{
						Icon = Properties.Resources.Triangle
					},
					new ButtonSchema(190, 62, controller, "Circle")
					{
						Icon = Properties.Resources.Circle
					},
					new ButtonSchema(169, 71, controller, "Cross")
					{
						Icon = Properties.Resources.Cross
					},
					new ButtonSchema(112, 62, controller, "Start")
					{
						DisplayName = "S"
					},
					new ButtonSchema(90, 62, controller, "Select")
					{
						DisplayName = "s"
					},
					new AnalogSchema(3, 120, $"P{controller} LStick X")
					{
						AxisRange = stickRanges[0],
						SecondaryAxisRange = stickRanges[1]
					},
					new AnalogSchema(260, 120, $"P{controller} RStick X")
					{
						AxisRange = stickRanges[0],
						SecondaryAxisRange = stickRanges[1]
					}
				}
			};
		}

		private static PadSchema GamePadController(int controller)
		{
			return new PadSchema
			{
				Size = new Size(240, 115),
				DisplayName = $"Gamepad Player{controller}",
				Buttons = new[]
				{
					ButtonSchema.Up(37, 55, controller),
					ButtonSchema.Down(37, 76, controller),
					ButtonSchema.Left(16, 67, controller),
					ButtonSchema.Right(58, 67, controller),
					new ButtonSchema(8, 37, controller, "L1"),
					new ButtonSchema(196, 37, controller, "R1"),
					new ButtonSchema(8, 15, controller, "L2"),
					new ButtonSchema(196, 15, controller, "R2"),
					new ButtonSchema(153, 67, controller, "Square")
					{
						Icon = Properties.Resources.Square
					},
					new ButtonSchema(174, 55, controller, "Triangle")
					{
						Icon = Properties.Resources.Triangle
					},
					new ButtonSchema(195, 67, controller, "Circle")
					{
						Icon = Properties.Resources.Circle
					},
					new ButtonSchema(174, 76, controller, "Cross")
					{
						Icon = Properties.Resources.Cross
					},
					new ButtonSchema(112, 67, controller, "Start")
					{
						DisplayName = "S"
					},
					new ButtonSchema(90, 67, controller, "Select")
					{
						DisplayName = "s"
					}
				}
			};
		}

		private static PadSchema NeGcon(int controller)
		{
			return new PadSchema
			{
				Size = new Size(343, 195),
				DisplayName = $"NeGcon Player{controller}",
				Buttons = new PadSchemaControl[]
				{
					ButtonSchema.Up(36, 83, controller),
					ButtonSchema.Down(36, 104, controller),
					ButtonSchema.Left(15, 95, controller),
					ButtonSchema.Right(57, 95, controller),
					new ButtonSchema(78, 118, controller, "Start") { DisplayName = "S" },
					new ButtonSchema(278, 38, controller, "B"),
					new ButtonSchema(308, 55, controller, "A"),
					new ButtonSchema(308, 15, controller, "R"),
					new SingleAxisSchema(5, 15, controller, "L")
					{
						TargetSize = new Size(128, 55),
						MinValue = 0,
						MaxValue = 255
					},
					new SingleAxisSchema(125, 15, controller, "Twist", isVertical: true)
					{
						TargetSize = new Size(64, 178),
						MinValue = 0,
						MaxValue = 255
					},
					new SingleAxisSchema(180, 60, controller, "2")
					{
						DisplayName = "II",
						TargetSize = new Size(128, 55),
						MinValue = 0,
						MaxValue = 255
					},
					new SingleAxisSchema(220, 120, controller, "1")
					{
						DisplayName = "I",
						TargetSize = new Size(128, 55),
						MinValue = 0,
						MaxValue = 255
					}
				}
			};
		}

		private static PadSchema ConsoleButtons(Octoshock psx)
		{
			return new ConsoleSchema
			{
				Size = new Size(310, 400),
				Buttons = new PadSchemaControl[]
				{
					new ButtonSchema(10, 15, "Reset"),
					new DiscManagerSchema(10, 54, new Size(300, 300), psx, new[] { "Open", "Close", "Disc Select" })
				}
			};
		}
	}
}
