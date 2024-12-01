using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.SAT)]
	public class SaturnSchema : IVirtualPadSchema
	{
		private static readonly AxisSpec AxisRange = new AxisSpec(0.RangeTo(0xffff), 0x8000);

		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			var nyma = (NymaCore)core;
			foreach (var result in nyma.ActualPortData
				.Where(r => r.Port.ShortName != "builtin"))
			{
				var num = int.Parse(result.Port.ShortName.Last().ToString());
				var device = result.Device.ShortName;
				var schema = GenerateSchemaForPort(device, num, showMessageBox);
				if (schema != null)
				{
					yield return schema;
				}
			}

			yield return ConsoleButtons(nyma.ControllerDefinition.Axes["Disk Index"]);
		}

		private static PadSchema GenerateSchemaForPort(string device, int controllerNum, Action<string> showMessageBox)
		{
			switch (device)
			{
				default:
					showMessageBox($"This peripheral `{device}` is not supported yet");
					return null;
				case "none":
					return null;
				case "gamepad":
					return StandardController(controllerNum);
				case "3dpad":
					return ThreeDeeController(controllerNum);
				case "mouse":
					return Mouse(controllerNum);
				case "wheel":
					return Wheel(controllerNum);
				case "mission":
					return MissionControl(controllerNum);
				case "dmission":
					return DualMissionControl(controllerNum);
				case "gun":
					return LightGun(controllerNum);
			}
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				Size = new Size(250, 100),
				Buttons = new[]
				{
					ButtonSchema.Up(34, 17, controller),
					ButtonSchema.Down(34, 61, controller),
					ButtonSchema.Left(22, 39, controller),
					ButtonSchema.Right(44, 39, controller),
					new ButtonSchema(78, 52, controller, "Start", "S"),
					new ButtonSchema(110, 63, controller, "A"),
					new ButtonSchema(134, 53, controller, "B"),
					new ButtonSchema(158, 43, controller, "C"),
					new ButtonSchema(110, 40, controller, "X"),
					new ButtonSchema(134, 30, controller, "Y"),
					new ButtonSchema(158, 20, controller, "Z"),
					new ButtonSchema(2, 10, controller, "L"),
					new ButtonSchema(184, 10, controller, "R")
				}
			};
		}

		private static PadSchema ThreeDeeController(int controller)
		{
			return new PadSchema
			{
				Size = new Size(458, 285),
				Buttons = new PadSchemaControl[]
				{
					ButtonSchema.Up(290, 77, $"P{controller} D-Pad Up"),
					ButtonSchema.Down(290, 121, $"P{controller} D-Pad Down"),
					ButtonSchema.Left(278, 99, $"P{controller} D-Pad Left"),
					ButtonSchema.Right(300, 99, $"P{controller} D-Pad Right"),
					new ButtonSchema(334, 112, controller, "Start", "S"),
					new ButtonSchema(366, 123, controller, "A"),
					new ButtonSchema(390, 113, controller, "B"),
					new ButtonSchema(414, 103, controller, "C"),
					new ButtonSchema(366, 100, controller, "X"),
					new ButtonSchema(390, 90, controller, "Y"),
					new ButtonSchema(414, 80, controller, "Z"),
					new AnalogSchema(6, 74, $"P{controller} Analog Left / Right")
					{
						SecondaryName = $"P{controller} Analog Up / Down",
						Spec = AxisRange,
						SecondarySpec = AxisRange
					},
					new SingleAxisSchema(8, 12, controller, "L")
					{
						DisplayName = "L",
						TargetSize = new Size(128, 55),
						MinValue = AxisRange.Min,
						MaxValue = AxisRange.Max
					},
					new SingleAxisSchema(328, 12, controller, "R")
					{
						DisplayName = "R",
						TargetSize = new Size(128, 55),
						MinValue = AxisRange.Min,
						MaxValue = AxisRange.Max
					}
				}
			};
		}

		private static PadSchema Mouse(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Mouse",
				Size = new Size(375, 320),
				Buttons = new PadSchemaControl[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} Motion Left / Right", AxisRange.Max, AxisRange.Max) //TODO (0xFFFF, 0xFFFF) matches previous behaviour - what was intended here?
					{
						SecondaryName = $"P{controller} Motion Up / Down",
						TargetSize = new Size(256, 256)
					},
					new ButtonSchema(300, 17, controller, "Left Button", "Left"),
					new ButtonSchema(300, 47, controller, "Middle Button", "Middle"),
					new ButtonSchema(300, 77, controller, "Right Button", "Right"),
					new ButtonSchema(300, 107, controller, "Start")
				}
			};
		}

		private static PadSchema Wheel(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Wheel",
				Size = new Size(325, 130),
				Buttons = new PadSchemaControl[]
				{
					new SingleAxisSchema(45, 70, controller, "Analog Left / Right")
					{
						TargetSize = new Size(256, 45),
						MinValue = 0,
						MaxValue = 65535,
						DisplayName = "Wheel"
					},
					new ButtonSchema(15, 12, controller, "Z"),
					new ButtonSchema(42, 22, controller, "Y"),
					new ButtonSchema(69, 32, controller, "X"),
					new ButtonSchema(145, 32, controller, "Start"),
					new ButtonSchema(231, 32, controller, "A"),
					new ButtonSchema(258, 22, controller, "B"),
					new ButtonSchema(285, 12, controller, "C"),
					ButtonSchema.Left(122, 32, $"P{controller} L Gear Shift"),
					ButtonSchema.Right(185, 32, $"P{controller} R Gear Shift")
				}
			};
		}

		private static PadSchema MissionControl(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Mission",
				Size = new Size(455, 230),
				Buttons = new PadSchemaControl[]
				{
					new ButtonSchema(45, 15, controller, "Start"),
					new ButtonSchema(5, 58, controller, "L"),
					new ButtonSchema(105, 58, controller, "R"),
					new ButtonSchema(30, 43, controller, "X"),
					new ButtonSchema(55, 43, controller, "Y"),
					new ButtonSchema(80, 43, controller, "Z"),
					new ButtonSchema(30, 70, controller, "A"),
					new ButtonSchema(55, 70, controller, "B"),
					new ButtonSchema(80, 70, controller, "C"),
					new AnalogSchema(195, 13, $"P{controller} Stick Left / Right")
					{
						SecondaryName = $"P{controller} Stick Fore / Back",
						Spec = AxisRange,
						SecondarySpec = AxisRange
					},
					new SingleAxisSchema(135, 13, controller, "Throttle Down / Up", isVertical: true)
					{
						DisplayName = "Throttle",
						TargetSize = new Size(64, 178),
						MinValue = AxisRange.Min,
						MaxValue = AxisRange.Max
					}
				}
			};
		}

		private static PadSchema DualMissionControl(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Dual Mission",
				Size = new Size(850, 230),
				Buttons = new PadSchemaControl[]
				{
					new AnalogSchema(68, 13, $"P{controller} L Stick Left / Right")
					{
						SecondaryName = $"P{controller} L Stick Fore / Back",
						Spec = AxisRange,
						SecondarySpec = AxisRange
					},
					new SingleAxisSchema(8, 13, controller, "L Throttle Down / Up", isVertical: true)
					{
						DisplayName = "Throttle",
						TargetSize = new Size(64, 178),
						MinValue = AxisRange.Min,
						MaxValue = AxisRange.Max
					},
					
					new ButtonSchema(400, 15, controller, "Start"),
					new ButtonSchema(360, 58, controller, "L"),
					new ButtonSchema(460, 58, controller, "R"),
					new ButtonSchema(385, 43, controller, "X"),
					new ButtonSchema(410, 43, controller, "Y"),
					new ButtonSchema(435, 43, controller, "Z"),
					new ButtonSchema(385, 70, controller, "A"),
					new ButtonSchema(410, 70, controller, "B"),
					new ButtonSchema(435, 70, controller, "C"),

					new AnalogSchema(570, 13, $"P{controller} R Stick Left / Right")
					{
						SecondaryName = $"P{controller} R Stick Fore / Back",
						Spec = AxisRange,
						SecondarySpec = AxisRange
					},
					new SingleAxisSchema(510, 13, controller, "R Throttle Down / Up", isVertical: true)
					{
						DisplayName = "Throttle",
						TargetSize = new Size(64, 178),
						MinValue = AxisRange.Min,
						MaxValue = AxisRange.Max
					}
				}
			};
		}

		private static PadSchema LightGun(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Light Gun",
				Size = new Size(375, 320),
				Buttons = new PadSchemaControl[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} X Axis", AxisRange.Max, AxisRange.Max) //TODO (0xFFFF, 0xFFFF) matches previous behaviour - what was intended here?
					{
						SecondaryName = $"P{controller} Y Axis",
						TargetSize = new Size(256, 256)
					},
					new ButtonSchema(300, 17, controller, "Trigger"),
					new ButtonSchema(300, 57, controller, "Start"),
					new ButtonSchema(300, 290, controller, "Offscreen Shot", "Offscreen")
				}
			};
		}

		private static PadSchema ConsoleButtons(AxisSpec diskRange)
		{
			return new ConsoleSchema
			{
				Size = new Size(327, 100),
				Buttons = new PadSchemaControl[]
				{
					new ButtonSchema(10, 15, "Reset"),
					new ButtonSchema(58, 15, "Power"),
					new ButtonSchema(108, 15, "Open Tray"),
					new ButtonSchema(175, 15, "Close Tray"),
					new SingleAxisSchema(10, 35, "Disk Index")
					{
						MinValue = diskRange.Min,
						MaxValue = diskRange.Max,
						TargetSize = new Size(310, 60)
					},
					new ButtonSchema(242, 15, "P13 Smpc Reset")
					{
						DisplayName = "Smpc Reset"
					}
				}
			};
		}
	}
}
