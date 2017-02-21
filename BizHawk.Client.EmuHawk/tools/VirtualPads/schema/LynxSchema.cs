using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[SchemaAttributes("Lynx")]
	public class LYNXSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			yield return StandardController();
			//yield return ConsoleButtons();
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
						Location = new Point(14, 12),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(14, 56),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(24, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "B",
						DisplayName = "B",
						Location = new Point(130, 62),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "A",
						DisplayName = "A",
						Location = new Point(154, 62),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "Option 1",
						DisplayName = "1",
						Location = new Point(100, 12),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "Option 2",
						DisplayName = "2",
						Location = new Point(100, 62),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "Pause",
						DisplayName = "Pause",
						Location = new Point(100, 37),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}
	}
}
