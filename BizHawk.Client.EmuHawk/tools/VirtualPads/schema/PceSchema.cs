using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	[SchemaAttributes("PCE")]
	public class PceSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas()
		{
			yield return StandardController(1);
			yield return StandardController(2);
		}

		public static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(174, 74),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(14, 2),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(14, 46),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(24, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " B",
						DisplayName = "II",
						Location = new Point(122, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " A",
						DisplayName = "I",
						Location = new Point(146, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Select",
						DisplayName = "s",
						Location = new Point(52, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Start",
						DisplayName = "R",
						Location = new Point(74, 24),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}
	}
}
