using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Sony.PSX;

namespace BizHawk.Client.EmuHawk
{
	[SchemaAttributes("PSX")]
	public class PSXSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			var psx = ((Octoshock)core);
			var settings = (Octoshock.SyncSettings)psx.GetSyncSettings();

			var fioConfig = settings.FIOConfig.ToLogical();
			for (int i = 0; i < 2; i++)
			{
				int pnum = i + 1;
				if (fioConfig.DevicesPlayer[i] == OctoshockDll.ePeripheralType.DualAnalog || fioConfig.DevicesPlayer[i] == OctoshockDll.ePeripheralType.DualShock)
					yield return DualShockController(pnum);
				if (fioConfig.DevicesPlayer[i] == OctoshockDll.ePeripheralType.Pad)
					yield return GamePadController(pnum);
			}

			yield return ConsoleButtons(psx);
		}

		public static PadSchema DualShockController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(500, 290),
				DisplayName = "DualShock Player" + controller,
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(32, 50),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(32, 71),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(11, 62),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(53, 62),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " L1",
						DisplayName = "L1",
						Location = new Point(3, 32),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " R1",
						DisplayName = "R1",
						Location = new Point(191, 32),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " L2",
						DisplayName = "L2",
						Location = new Point(3, 10),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " R2",
						DisplayName = "R2",
						Location = new Point(191, 10),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " L3",
						DisplayName = "L3",
						Location = new Point(72, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " R3",
						DisplayName = "R3",
						Location = new Point(130, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Square",
						DisplayName = "",
						Icon = Properties.Resources.Square,
						Location = new Point(148, 62),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Triangle",
						DisplayName = "",
						Icon = Properties.Resources.Triangle,
						Location = new Point(169, 50),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Circle",
						DisplayName = "",
						Icon = Properties.Resources.Circle,
						Location = new Point(190, 62),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Cross",
						DisplayName = "",
						Icon = Properties.Resources.Cross,
						Location = new Point(169, 71),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Start",
						DisplayName = "S",
						Location = new Point(112, 62),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Select",
						DisplayName = "s",
						Location = new Point(90, 62),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " LStick X",
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
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " RStick X",
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

		public static PadSchema GamePadController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(240, 115),
				DisplayName = "Gamepad Player" + controller,
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(37, 55),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(37, 76),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(16, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(58, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " L1",
						DisplayName = "L1",
						Location = new Point(8, 37),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " R1",
						DisplayName = "R1",
						Location = new Point(196, 37),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " L2",
						DisplayName = "L2",
						Location = new Point(8, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " R2",
						DisplayName = "R2",
						Location = new Point(196, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Square",
						DisplayName = "",
						Icon = Properties.Resources.Square,
						Location = new Point(153, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Triangle",
						DisplayName = "",
						Icon = Properties.Resources.Triangle,
						Location = new Point(174, 55),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Circle",
						DisplayName = "",
						Icon = Properties.Resources.Circle,
						Location = new Point(195, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Cross",
						DisplayName = "",
						Icon = Properties.Resources.Cross,
						Location = new Point(174, 76),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Start",
						DisplayName = "S",
						Location = new Point(112, 67),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Select",
						DisplayName = "s",
						Location = new Point(90, 67),
						Type = PadSchema.PadInputType.Boolean
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
					new PadSchema.ButtonScema
					{
						Name = "Reset",
						DisplayName = "Reset",
						Location = new Point(10, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "Disc Select", //not really, but shuts up a warning
						Type = PadSchema.PadInputType.DiscManager,
						Location = new Point(10,54),
						TargetSize = new Size(300,300),
						OwnerEmulator = psx,
						SecondaryNames = new [] { "Open", "Close", "Disc Select" }
					}
				}
			};
		}
	}
}
