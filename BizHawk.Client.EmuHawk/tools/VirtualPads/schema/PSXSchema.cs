using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Sony.PSX;

namespace BizHawk.Client.EmuHawk
{
	[Schema("PSX")]
	// ReSharper disable once UnusedMember.Global
	public class PSXSchema : IVirtualPadSchema
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
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(500, 290),
				DisplayName = $"DualShock Player{controller}",
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(32, 50),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(32, 71),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(11, 62),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(53, 62),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} L1",
						DisplayName = "L1",
						Location = new Point(3, 32),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} R1",
						DisplayName = "R1",
						Location = new Point(191, 32),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} L2",
						DisplayName = "L2",
						Location = new Point(3, 10),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} R2",
						DisplayName = "R2",
						Location = new Point(191, 10),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} L3",
						DisplayName = "L3",
						Location = new Point(72, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} R3",
						DisplayName = "R3",
						Location = new Point(130, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Square",
						DisplayName = "",
						Icon = Properties.Resources.Square,
						Location = new Point(148, 62),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Triangle",
						DisplayName = "",
						Icon = Properties.Resources.Triangle,
						Location = new Point(169, 50),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Circle",
						DisplayName = "",
						Icon = Properties.Resources.Circle,
						Location = new Point(190, 62),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Cross",
						DisplayName = "",
						Icon = Properties.Resources.Cross,
						Location = new Point(169, 71),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "S",
						Location = new Point(112, 62),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Select",
						DisplayName = "s",
						Location = new Point(90, 62),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} LStick X",
						MinValue = 0,
						MidValue = 128,
						MaxValue = 255,
						MinValueSec = 0,
						MidValueSec = 128,
						MaxValueSec = 255,
						DisplayName = "",
						Location = new Point(3, 120),
						Type = PadSchema.PadInputType.AnalogStick
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} RStick X",
						MinValue = 0,
						MidValue = 128,
						MaxValue = 255,
						MinValueSec = 0,
						MidValueSec = 128,
						MaxValueSec = 255,
						DisplayName = "",
						Location = new Point(260, 120),
						Type = PadSchema.PadInputType.AnalogStick
					}
				}
			};
		}

		private static PadSchema GamePadController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(240, 115),
				DisplayName = $"Gamepad Player{controller}",
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(37, 55),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(37, 76),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(16, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(58, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} L1",
						DisplayName = "L1",
						Location = new Point(8, 37),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} R1",
						DisplayName = "R1",
						Location = new Point(196, 37),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} L2",
						DisplayName = "L2",
						Location = new Point(8, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} R2",
						DisplayName = "R2",
						Location = new Point(196, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Square",
						DisplayName = "",
						Icon = Properties.Resources.Square,
						Location = new Point(153, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Triangle",
						DisplayName = "",
						Icon = Properties.Resources.Triangle,
						Location = new Point(174, 55),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Circle",
						DisplayName = "",
						Icon = Properties.Resources.Circle,
						Location = new Point(195, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Cross",
						DisplayName = "",
						Icon = Properties.Resources.Cross,
						Location = new Point(174, 76),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "S",
						Location = new Point(112, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Select",
						DisplayName = "s",
						Location = new Point(90, 67),
						Type = PadSchema.PadInputType.Boolean
					},
				}
			};
		}

		private static PadSchema NeGcon(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(343, 195),
				DisplayName = $"NeGcon Player{controller}",
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(36, 83),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(36, 104),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(15, 95),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(57, 95),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "S",
						Location = new Point(78, 118),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} B",
						DisplayName = "B",
						Location = new Point(278, 38),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} A",
						DisplayName = "A",
						Location = new Point(308, 55),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} R",
						DisplayName = "R",
						Location = new Point(308, 15),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} L",
						DisplayName = "L",
						Location = new Point(5, 15),
						Type = PadSchema.PadInputType.FloatSingle,
						TargetSize = new Size(128, 55),
						MinValue = 0,
						MaxValue = 255,
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Twist",
						DisplayName = "Twist",
						Location = new Point(125, 15),
						Type = PadSchema.PadInputType.FloatSingle,
						TargetSize = new Size(64, 178),
						MinValue = 0,
						MaxValue = 255,
						Orientation = Orientation.Vertical
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} 2",
						DisplayName = "II",
						Location = new Point(180, 60),
						Type = PadSchema.PadInputType.FloatSingle,
						TargetSize = new Size(128, 55),
						MinValue = 0,
						MaxValue = 255,
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} 1",
						DisplayName = "I",
						Location = new Point(220, 120),
						Type = PadSchema.PadInputType.FloatSingle,
						TargetSize = new Size(128, 55),
						MinValue = 0,
						MaxValue = 255,
					},
				}
			};
		}

		private static PadSchema ConsoleButtons(Octoshock psx)
		{
			return new PadSchema
			{
				DisplayName = "Console",
				IsConsole = true,
				DefaultSize = new Size(310, 400),
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
						Name = "Disc Select", // not really, but shuts up a warning
						Type = PadSchema.PadInputType.DiscManager,
						Location = new Point(10, 54),
						TargetSize = new Size(300, 300),
						OwnerEmulator = psx,
						SecondaryNames = new[] { "Open", "Close", "Disc Select" }
					}
				}
			};
		}
	}
}
