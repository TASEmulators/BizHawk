using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[SchemaAttributes("Coleco")]
	public class ColecoSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			yield return StandardController(1);
			yield return StandardController(2);
		}

		public static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(128, 200),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(50, 11),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(50, 32),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(29, 22),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(71, 22),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " L",
						DisplayName = "L",
						Location = new Point(3, 42),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " R",
						DisplayName = "R",
						Location = new Point(100, 42),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Key 1",
						DisplayName = "1",
						Location = new Point(27, 85),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Key 2",
						DisplayName = "2",
						Location = new Point(50, 85),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Key 3",
						DisplayName = "3",
						Location = new Point(73, 85),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Key 4",
						DisplayName = "4",
						Location = new Point(27, 108),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Key 5",
						DisplayName = "5",
						Location = new Point(50, 108),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Key 6",
						DisplayName = "6",
						Location = new Point(73, 108),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Key 7",
						DisplayName = "7",
						Location = new Point(27, 131),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Key 8",
						DisplayName = "8",
						Location = new Point(50, 131),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Key 9",
						DisplayName = "9",
						Location = new Point(73, 131),
						Type = PadSchema.PadInputType.Boolean
					},

					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Star",
						DisplayName = "*",
						Location = new Point(27, 154),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Key 0",
						DisplayName = "0",
						Location = new Point(50, 154),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Pound",
						DisplayName = "#",
						Location = new Point(73, 154),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}
	}
}
