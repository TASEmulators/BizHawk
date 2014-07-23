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
			var ss = (N64SyncSettings)Global.Emulator.GetSyncSettings();
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
						Name = "P" + controller + " X Axis",
						MaxValue = 127,
						DisplayName = "",
						Location = new Point(6, 14),
						Type = PadSchema.PadInputType.AnalogStick
					},
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
						Location = new Point(172, 150),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Z",
						DisplayName = "Z",
						Location = new Point(74, 245),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " Start",
						DisplayName = "S",
						Location = new Point(87, 157),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " B",
						DisplayName = "B",
						Location = new Point(83, 195),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " A",
						DisplayName = "A",
						Location = new Point(113, 206),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " C Up",
						DisplayName = "cU",
						Location = new Point(147, 235),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " C Down",
						DisplayName = "cD",
						Location = new Point(147, 281),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " C Left",
						DisplayName = "cL",
						Location = new Point(129, 258),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonScema
					{
						Name = "P" + controller + " C Right",
						DisplayName = "cR",
						Location = new Point(164, 258),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}
	}
}
