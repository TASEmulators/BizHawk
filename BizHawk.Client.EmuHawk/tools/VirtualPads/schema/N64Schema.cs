using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64;

namespace BizHawk.Client.EmuHawk
{
	[Schema("N64")]
	// ReSharper disable once UnusedMember.Global
	public class N64Schema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			var ss = ((N64)core).GetSyncSettings();
			for (var i = 0; i < 4; i++)
			{
				if (ss.Controllers[i].IsConnected)
				{
					yield return StandardController(i + 1);
				}
			}
		}

		private static PadSchema StandardController(int controller)
		{
			var controllerDefRanges = N64Input.N64ControllerDefinition.FloatRanges;
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(275, 316),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} DPad U",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(24, 230),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} DPad D",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(24, 251),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} DPad L",
						Icon = Properties.Resources.Back,
						Location = new Point(3, 242),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} DPad R",
						Icon = Properties.Resources.Forward,
						Location = new Point(45, 242),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} L",
						DisplayName = "L",
						Location = new Point(3, 185),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} R",
						DisplayName = "R",
						Location = new Point(191, 185),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Z",
						DisplayName = "Z",
						Location = new Point(81, 269),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "S",
						Location = new Point(81, 246),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} B",
						DisplayName = "B",
						Location = new Point(127, 246),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} A",
						DisplayName = "A",
						Location = new Point(138, 269),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} C Up",
						Icon = Properties.Resources.YellowUp,
						Location = new Point(173, 210),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} C Down",
						Icon = Properties.Resources.YellowDown,
						Location = new Point(173, 231),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} C Left",
						Icon = Properties.Resources.YellowLeft,
						Location = new Point(152, 221),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} C Right",
						Icon = Properties.Resources.YellowRight,
						Location = new Point(194, 221),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} X Axis",
						AxisRange = controllerDefRanges[0],
						SecondaryAxisRange = controllerDefRanges[1],
						Location = new Point(6, 14),
						Type = PadSchema.PadInputType.AnalogStick
					}
				}
			};
		}
	}
}
