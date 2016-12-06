using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[SchemaAttributes("GBA")]
	public class GBASchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			yield return StandardController();
			yield return ConsoleButtons();
		}

		public static PadSchema StandardController()
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(194, 90),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(29, 17),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(29, 61),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(17, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(39, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "B",
						DisplayName = "B",
						Location = new Point(130, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "A",
						DisplayName = "A",
						Location = new Point(154, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "Select",
						DisplayName = "s",
						Location = new Point(64, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "Start",
						DisplayName = "S",
						Location = new Point(86, 39),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "L",
						DisplayName = "L",
						Location = new Point(2, 12),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "R",
						DisplayName = "R",
						Location = new Point(166, 12),
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
				DefaultSize = new Size(75, 50),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "Power",
						DisplayName = "Power",
						Location = new Point(10, 15),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}
	}
}
