using System.Collections.Generic;
using System.Drawing;

namespace BizHawk.Client.EmuHawk
{
	[SchemaAttributes("C64")]
	public class C64Schema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas()
		{
			yield return StandardController(1);
			yield return StandardController(2);
			yield return Keyboard();
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Player " + controller,
				IsConsole = false,
				DefaultSize = new Size(174, 74),
				MaxSize = new Size(174, 74),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(23, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(23, 36),
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
						Location = new Point(44, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " B",
						DisplayName = "B",
						Location = new Point(124, 24),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}

		private static PadSchema Keyboard()
		{
			return new PadSchema
			{
				DisplayName = "Keyboard",
				IsConsole = false,
				DefaultSize = new Size(400, 250),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "Key 1",
						DisplayName = "1",
						Location = new Point(12, 12),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}
	}
}
