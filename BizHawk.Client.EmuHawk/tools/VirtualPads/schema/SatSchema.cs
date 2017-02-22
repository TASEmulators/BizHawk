using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[SchemaAttributes("SAT")]
	public class SaturnSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			yield return StandardController(1);
			yield return StandardController(2);
			yield return ConsoleButtons();
		}

		public static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(212, 90),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(34, 17),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(34, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(22, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(44, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Start",
						DisplayName = "S",
						Location = new Point(78, 52),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " A",
						DisplayName = "A",
						Location = new Point(110, 63),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " B",
						DisplayName = "B",
						Location = new Point(134, 53),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " C",
						DisplayName = "C",
						Location = new Point(158, 43),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " X",
						DisplayName = "X",
						Location = new Point(110, 40),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Y",
						DisplayName = "Y",
						Location = new Point(134, 30),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Z",
						DisplayName = "Z",
						Location = new Point(158, 20),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " L",
						DisplayName = "L",
						Location = new Point(2, 10),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
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
					new PadSchema.ButtonScema
					{
						Name = "Reset",
						DisplayName = "Reset",
						Location = new Point(10, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
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
