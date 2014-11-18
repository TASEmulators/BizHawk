using System.Collections.Generic;
using System.Drawing;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.N64;

namespace BizHawk.Client.EmuHawk
{
	[SchemaAttributes("N64")]
	public class N64Schema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas()
		{
			var ss = ((N64)Global.Emulator).GetSyncSettings();
			for (var i = 0; i < 4; i++)
			{
				if (ss.Controllers[i].IsConnected)
				{
					yield return StandardController(i + 1);
				}
			}
		}

		public static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(220, 316),
				Buttons = new[]
				{
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " DPad U",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(24, 195),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " DPad D",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(24, 216),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " DPad L",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(3, 207),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " DPad R",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(45, 207),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " L",
						DisplayName = "L",
						Location = new Point(3, 150),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " R",
						DisplayName = "R",
						Location = new Point(191, 150),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Z",
						DisplayName = "Z",
						Location = new Point(81, 234),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Start",
						DisplayName = "S",
						Location = new Point(81, 211),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " B",
						DisplayName = "B",
						Location = new Point(127, 211),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " A",
						DisplayName = "A",
						Location = new Point(138, 234),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " C Up",
						Icon = Properties.Resources.YellowUp,
						Location = new Point(173, 175),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " C Down",
						Icon = Properties.Resources.YellowDown,
						Location = new Point(173, 196),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " C Left",
						Icon = Properties.Resources.YellowLeft,
						Location = new Point(152, 189),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " C Right",
						Icon = Properties.Resources.YellowRight,
						Location = new Point(194, 189),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " X Axis",
						MaxValue = 127,
						DisplayName = "",
						Location = new Point(6, 14),
						Type = PadSchema.PadInputType.AnalogStick
					}
				}
			};
		}
	}
}
