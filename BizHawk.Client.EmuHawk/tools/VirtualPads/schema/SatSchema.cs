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
				IsConsole = false,
				DefaultSize = new Size(500, 500),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(34, 17),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(34, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(22, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(44, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "S",
						Location = new Point(78, 52),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} A",
						DisplayName = "A",
						Location = new Point(110, 63),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} B",
						DisplayName = "B",
						Location = new Point(134, 53),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} C",
						DisplayName = "C",
						Location = new Point(158, 43),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} X",
						DisplayName = "X",
						Location = new Point(110, 40),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Y",
						DisplayName = "Y",
						Location = new Point(134, 30),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Z",
						DisplayName = "Z",
						Location = new Point(158, 20),
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
						Location = new Point(184, 10),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema ThreeDeeController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(458, 285),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(290, 77),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(290, 121),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(278, 99),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(300, 99),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "S",
						Location = new Point(334, 112),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} A",
						DisplayName = "A",
						Location = new Point(366, 123),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} B",
						DisplayName = "B",
						Location = new Point(390, 113),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} C",
						DisplayName = "C",
						Location = new Point(414, 103),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} X",
						DisplayName = "X",
						Location = new Point(366, 100),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Y",
						DisplayName = "Y",
						Location = new Point(390, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Z",
						DisplayName = "Z",
						Location = new Point(414, 80),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Stick Horizontal",
						SecondaryNames = new[] { $"P{controller} Stick Vertical" },
						MinValue = 0,
						MidValue = 127,
						MaxValue = 255,
						MinValueSec = 0,
						MidValueSec = 127,
						MaxValueSec = 255,
						DisplayName = "",
						Location = new Point(6, 74),
						Type = PadSchema.PadInputType.AnalogStick
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left Shoulder",
						DisplayName = "L",
						Location = new Point(8, 12),
						Type = PadSchema.PadInputType.FloatSingle,
						TargetSize = new Size(128, 55),
						MinValue = 0,
						MaxValue = 255,
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right Shoulder",
						DisplayName = "L",
						Location = new Point(328, 12),
						Type = PadSchema.PadInputType.FloatSingle,
						TargetSize = new Size(128, 55),
						MinValue = 0,
						MaxValue = 255,
					}
				}
			};
		}

		private static PadSchema Mouse(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Mouse",
				IsConsole = false,
				DefaultSize = new Size(375, 320),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} X",
						SecondaryNames = new[] { $"P{controller} Y" },
						Location = new Point(14, 17),
						Type = PadSchema.PadInputType.TargetedPair,
						TargetSize = new Size(256, 256)
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Mouse Left",
						DisplayName = "Left",
						Location = new Point(300, 17),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Mouse Center",
						DisplayName = "Center",
						Location = new Point(300, 47),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Mouse Right",
						DisplayName = "Right",
						Location = new Point(300, 77),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "Start",
						Location = new Point(300, 107),
						Type = PadSchema.PadInputType.Boolean
					}
				},
			};
		}

		private static PadSchema Wheel(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Wheel",
				IsConsole = false,
				DefaultSize = new Size(325, 100),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Wheel",
						DisplayName = "Wheel",
						Location = new Point(8, 12),
						Type = PadSchema.PadInputType.FloatSingle,
						TargetSize = new Size(128, 55),
						MinValue = 0,
						MaxValue = 255,
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(150, 20),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(150, 43),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} A",
						DisplayName = "A",
						Location = new Point(180, 63),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} B",
						DisplayName = "B",
						Location = new Point(204, 53),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} C",
						DisplayName = "C",
						Location = new Point(228, 43),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} X",
						DisplayName = "X",
						Location = new Point(180, 40),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Y",
						DisplayName = "Y",
						Location = new Point(204, 30),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Z",
						DisplayName = "Z",
						Location = new Point(228, 20),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "Start",
						Location = new Point(268, 20),
						Type = PadSchema.PadInputType.Boolean
					},
				},

			};
		}

		private static PadSchema MissionControl(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Mission",
				IsConsole = false,
				DefaultSize = new Size(445, 230),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "Start",
						Location = new Point(45, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} L",
						DisplayName = "L",
						Location = new Point(5, 58),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} R",
						DisplayName = "R",
						Location = new Point(105, 58),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} X",
						DisplayName = "X",
						Location = new Point(30, 43),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Y",
						DisplayName = "Y",
						Location = new Point(55, 43),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Z",
						DisplayName = "Z",
						Location = new Point(80, 43),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} A",
						DisplayName = "A",
						Location = new Point(30, 70),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} B",
						DisplayName = "B",
						Location = new Point(55, 70),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} C",
						DisplayName = "C",
						Location = new Point(80, 70),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Stick Horizontal",
						SecondaryNames = new[] { $"P{controller} Stick Vertical" },
						MinValue = 0,
						MidValue = 127,
						MaxValue = 255,
						MinValueSec = 0,
						MidValueSec = 127,
						MaxValueSec = 255,
						DisplayName = "",
						Location = new Point(185, 13),
						Type = PadSchema.PadInputType.AnalogStick
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Throttle",
						DisplayName = "Throttle",
						Location = new Point(135, 13),
						Type = PadSchema.PadInputType.FloatSingle,
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
			return new PadSchema
			{
				DisplayName = "Dual Mission",
				IsConsole = false,
				DefaultSize = new Size(680, 230),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left Stick Horizontal",
						SecondaryNames = new[] { $"P{controller} Left Stick Vertical" },
						MinValue = 0,
						MidValue = 127,
						MaxValue = 255,
						MinValueSec = 0,
						MidValueSec = 127,
						MaxValueSec = 255,
						DisplayName = "",
						Location = new Point(58, 13),
						Type = PadSchema.PadInputType.AnalogStick
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left Throttle",
						DisplayName = "Throttle",
						Location = new Point(8, 13),
						Type = PadSchema.PadInputType.FloatSingle,
						TargetSize = new Size(64, 178),
						MinValue = 0,
						MaxValue = 255,
						Orientation = Orientation.Vertical
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right Stick Horizontal",
						SecondaryNames = new[] { $"P{controller} Right Stick Vertical" },
						MinValue = 0,
						MidValue = 127,
						MaxValue = 255,
						MinValueSec = 0,
						MidValueSec = 127,
						MaxValueSec = 255,
						DisplayName = "",
						Location = new Point(400, 13),
						Type = PadSchema.PadInputType.AnalogStick
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right Throttle",
						DisplayName = "Throttle",
						Location = new Point(350, 13),
						Type = PadSchema.PadInputType.FloatSingle,
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
						DisplayName = "Reset",
						Location = new Point(10, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Power",
						DisplayName = "Power",
						Location = new Point(58, 15),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}
	}
}
