using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Sega.Saturn;

namespace BizHawk.Client.EmuHawk
{
	[SchemaAttributes("SAT")]
	public class SaturnSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			var ss = ((Saturnus)core).GetSyncSettings();

			int totalPorts = (ss.Port1Multitap ? 6 : 1) + (ss.Port2Multitap ? 6 : 1);

			var padSchemas = new SaturnusControllerDeck.Device[]
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
				case SaturnusControllerDeck.Device.Mission:
				case SaturnusControllerDeck.Device.DualMission:
				case SaturnusControllerDeck.Device.Wheel:
				case SaturnusControllerDeck.Device.Keyboard:
				case SaturnusControllerDeck.Device.Mouse:
					System.Windows.Forms.MessageBox.Show("This peripheral is not supported yet");
					return null;
			}
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(212, 90),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(34, 17),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(34, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(22, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(44, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Start",
						DisplayName = "S",
						Location = new Point(78, 52),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " A",
						DisplayName = "A",
						Location = new Point(110, 63),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " B",
						DisplayName = "B",
						Location = new Point(134, 53),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " C",
						DisplayName = "C",
						Location = new Point(158, 43),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " X",
						DisplayName = "X",
						Location = new Point(110, 40),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Y",
						DisplayName = "Y",
						Location = new Point(134, 30),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " Z",
						DisplayName = "Z",
						Location = new Point(158, 20),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " L",
						DisplayName = "L",
						Location = new Point(2, 10),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "P" + controller + " R",
						DisplayName = "R",
						Location = new Point(184, 10),
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
