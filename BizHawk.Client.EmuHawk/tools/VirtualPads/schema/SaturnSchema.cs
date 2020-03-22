using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Sega.Saturn;

namespace BizHawk.Client.EmuHawk
{
	[Schema("SAT")]
	// ReSharper disable once UnusedMember.Global
	public class SaturnSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			var ss = ((Saturnus)core).GetSyncSettings();

			int totalPorts = (ss.Port1Multitap ? 6 : 1) + (ss.Port2Multitap ? 6 : 1);

			var padSchemas = new[]
			{
				ss.Port1,
				ss.Port2,
				ss.Port3,
				ss.Port4,
				ss.Port5,
				ss.Port6,
				ss.Port7,
				ss.Port8,
				ss.Port9,
				ss.Port10,
				ss.Port11,
				ss.Port12
			}.Take(totalPorts)
			.Where(p => p != SaturnusControllerDeck.Device.None)
			.Select((p, i) => GenerateSchemaForPort(p, i + 1))
			.Where(s => s != null)
			.Concat(new[] { ConsoleButtons() });

			return padSchemas;
		}

		private static PadSchema GenerateSchemaForPort(SaturnusControllerDeck.Device device, int controllerNum)
		{
			switch (device)
			{
				default:
				case SaturnusControllerDeck.Device.None:
					return null;
				case SaturnusControllerDeck.Device.Gamepad:
					return StandardController(controllerNum);
				case SaturnusControllerDeck.Device.ThreeDeePad:
					return ThreeDeeController(controllerNum);
				case SaturnusControllerDeck.Device.Mouse:
					return Mouse(controllerNum);
				case SaturnusControllerDeck.Device.Wheel:
					return Wheel(controllerNum);
				case SaturnusControllerDeck.Device.Mission:
					return MissionControl(controllerNum);
				case SaturnusControllerDeck.Device.DualMission:
					return DualMissionControl(controllerNum);
				case SaturnusControllerDeck.Device.Keyboard:
					MessageBox.Show("This peripheral is not supported yet");
					return null;
			}
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				DefaultSize = new Size(250, 100),
				Buttons = new[]
				{
					ButtonSchema.Up(34, 17, $"P{controller} Up"),
					ButtonSchema.Down(34, 61, $"P{controller} Down"),
					ButtonSchema.Left(22, 39, $"P{controller} Left"),
					ButtonSchema.Right(44, 39, $"P{controller} Right"),
					new ButtonSchema(78, 52, controller, "Start")
					{
						DisplayName = "S"
					},
					new ButtonSchema(110, 63, controller, "A")
					{
						DisplayName = "A"
					},
					new ButtonSchema(134, 53, controller, "B")
					{
						DisplayName = "B"
					},
					new ButtonSchema(158, 43, controller, "C")
					{
						DisplayName = "C"
					},
					new ButtonSchema(110, 40, controller, "X")
					{
						DisplayName = "X"
					},
					new ButtonSchema(134, 30, controller, "Y")
					{
						DisplayName = "Y"
					},
					new ButtonSchema(158, 20, controller, "Z")
					{
						DisplayName = "Z"
					},
					new ButtonSchema(2, 10, controller, "L")
					{
						DisplayName = "L"
					},
					new ButtonSchema(184, 10, controller, "R")
					{
						DisplayName = "R"
					}
				}
			};
		}

		private static PadSchema ThreeDeeController(int controller)
		{
			var axisRanges = SaturnusControllerDeck.ThreeDeeAxisRanges;
			return new PadSchema
			{
				DefaultSize = new Size(458, 285),
				Buttons = new[]
				{
					ButtonSchema.Up(290, 77, $"P{controller} Up"),
					ButtonSchema.Down(290, 121, $"P{controller} Down"),
					ButtonSchema.Left(278, 99, $"P{controller} Left"),
					ButtonSchema.Right(300, 99, $"P{controller} Right"),
					new ButtonSchema(334, 112, controller, "Start")
					{
						DisplayName = "S"
					},
					new ButtonSchema(366, 123, controller, "A")
					{
						DisplayName = "A"
					},
					new ButtonSchema(390, 113, controller, "B")
					{
						DisplayName = "B"
					},
					new ButtonSchema(414, 103, controller, "C")
					{
						DisplayName = "C"
					},
					new ButtonSchema(366, 100, controller, "X")
					{
						DisplayName = "X"
					},
					new ButtonSchema(390, 90, controller, "Y")
					{
						DisplayName = "Y"
					},
					new ButtonSchema(414, 80, controller, "Z")
					{
						DisplayName = "Z"
					},
					new AnalogSchema(6, 74)
					{
						Name = $"P{controller} Stick Horizontal",
						SecondaryNames = new[] { $"P{controller} Stick Vertical" },
						AxisRange = axisRanges[0],
						SecondaryAxisRange = axisRanges[1],
					},
					new SingleFloatSchema(8, 12, $"P{controller} Left Shoulder")
					{
						DisplayName = "L",
						TargetSize = new Size(128, 55),
						MinValue = 0,
						MaxValue = 255
					},
					new SingleFloatSchema(328, 12, $"P{controller} Right Shoulder")
					{
						DisplayName = "L",
						TargetSize = new Size(128, 55),
						MinValue = 0,
						MaxValue = 255
					}
				}
			};
		}

		private static PadSchema Mouse(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Mouse",
				DefaultSize = new Size(375, 320),
				Buttons = new[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} X")
					{
						TargetSize = new Size(256, 256)
					},
					new ButtonSchema(300, 17, controller, "Mouse Left")
					{
						DisplayName = "Left"
					},
					new ButtonSchema(300, 47, controller, "Mouse Center")
					{
						DisplayName = "Center"
					},
					new ButtonSchema(300, 77, controller, "Mouse Right")
					{
						DisplayName = "Right"
					},
					new ButtonSchema(300, 107, controller, "Start")
					{
						DisplayName = "Start"
					}
				}
			};
		}

		private static PadSchema Wheel(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Wheel",
				DefaultSize = new Size(325, 100),
				Buttons = new[]
				{
					new SingleFloatSchema(8, 12, $"P{controller} Wheel")
					{
						DisplayName = "Wheel",
						TargetSize = new Size(128, 55),
						MinValue = 0,
						MaxValue = 255
					},
					ButtonSchema.Up(150, 20, $"P{controller} Up"),
					ButtonSchema.Down(150, 43, $"P{controller} Down"),
					new ButtonSchema(180, 63, controller, "A")
					{
						DisplayName = "A"
					},
					new ButtonSchema(204, 53, controller, "B")
					{
						DisplayName = "B"
					},
					new ButtonSchema(228, 43, controller, "C")
					{
						DisplayName = "C"
					},
					new ButtonSchema(180, 40, controller, "X")
					{
						DisplayName = "X"
					},
					new ButtonSchema(204, 30, controller, "Y")
					{
						DisplayName = "Y"
					},
					new ButtonSchema(228, 20, controller, "Z")
					{
						DisplayName = "Z"
					},
					new ButtonSchema(268, 20, controller, "Start")
					{
						DisplayName = "Start"
					}
				}
			};
		}

		private static PadSchema MissionControl(int controller)
		{
			var axisRanges = SaturnusControllerDeck.MissionAxisRanges;
			return new PadSchema
			{
				DisplayName = "Mission",
				DefaultSize = new Size(445, 230),
				Buttons = new[]
				{
					new ButtonSchema(45, 15, controller, "Start")
					{
						DisplayName = "Start"
					},
					new ButtonSchema(5, 58, controller, "L")
					{
						DisplayName = "L"
					},
					new ButtonSchema(105, 58, controller, "R")
					{
						DisplayName = "R"
					},
					new ButtonSchema(30, 43, controller, "X")
					{
						DisplayName = "X"
					},
					new ButtonSchema(55, 43, controller, "Y")
					{
						DisplayName = "Y"
					},
					new ButtonSchema(80, 43, controller, "Z")
					{
						DisplayName = "Z"
					},
					new ButtonSchema(30, 70, controller, "A")
					{
						DisplayName = "A"
					},
					new ButtonSchema(55, 70, controller, "B")
					{
						DisplayName = "B"
					},
					new ButtonSchema(80, 70, controller, "C")
					{
						DisplayName = "C"
					},
					new AnalogSchema(185, 13)
					{
						Name = $"P{controller} Stick Horizontal",
						SecondaryNames = new[] { $"P{controller} Stick Vertical" },
						AxisRange = axisRanges[0],
						SecondaryAxisRange = axisRanges[1]
					},
					new SingleFloatSchema(135, 13, $"P{controller} Throttle")
					{
						DisplayName = "Throttle",
						TargetSize = new Size(64, 178),
						MinValue = 0,
						MaxValue = 255,
						Orientation = Orientation.Vertical
					}
				}
			};
		}

		private static PadSchema DualMissionControl(int controller)
		{
			var axisRanges = SaturnusControllerDeck.DualMissionAxisRanges;
			return new PadSchema
			{
				DisplayName = "Dual Mission",
				DefaultSize = new Size(680, 230),
				Buttons = new ButtonSchema[]
				{
					new AnalogSchema(58, 13)
					{
						Name = $"P{controller} Left Stick Horizontal",
						SecondaryNames = new[] { $"P{controller} Left Stick Vertical" },
						AxisRange = axisRanges[3],
						SecondaryAxisRange = axisRanges[4]
					},
					new SingleFloatSchema(8, 13, $"P{controller} Left Throttle")
					{
						DisplayName = "Throttle",
						TargetSize = new Size(64, 178),
						MinValue = 0,
						MaxValue = 255,
						Orientation = Orientation.Vertical
					},
					new AnalogSchema(400, 13)
					{
						Name = $"P{controller} Right Stick Horizontal",
						SecondaryNames = new[] { $"P{controller} Right Stick Vertical" },
						AxisRange = axisRanges[0],
						SecondaryAxisRange = axisRanges[1]
					},
					new SingleFloatSchema(350, 13, $"P{controller} Right Throttle")
					{
						DisplayName = "Throttle",
						TargetSize = new Size(64, 178),
						MinValue = 0,
						MaxValue = 255,
						Orientation = Orientation.Vertical
					}
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				DefaultSize = new Size(250, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Reset"),
					new ButtonSchema(58, 15, "Power"),
					new ButtonSchema(108, 15, "Previous Disk")
					{
						DisplayName = "Prev Disc"
					},
					new ButtonSchema(175, 15, "Next Disk")
					{
						DisplayName = "Next Disc"
					}
				}
			};
		}
	}
}
