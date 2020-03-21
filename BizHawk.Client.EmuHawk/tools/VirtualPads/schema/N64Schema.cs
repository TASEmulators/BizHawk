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
					ButtonSchema.Up($"P{controller}  DPad U", 24, 230),
					ButtonSchema.Down($"P{controller} DPad D", 24, 251),
					ButtonSchema.Left($"P{controller} DPad L", 3, 242),
					ButtonSchema.Right($"P{controller} DPad R", 45, 242),
					new ButtonSchema
					{
						Name = $"P{controller} L",
						DisplayName = "L",
						Location = new Point(3, 185)
					},
					new ButtonSchema
					{
						Name = $"P{controller} R",
						DisplayName = "R",
						Location = new Point(191, 185)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Z",
						DisplayName = "Z",
						Location = new Point(81, 269)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "S",
						Location = new Point(81, 246)
					},
					new ButtonSchema
					{
						Name = $"P{controller} B",
						DisplayName = "B",
						Location = new Point(127, 246)
					},
					new ButtonSchema
					{
						Name = $"P{controller} A",
						DisplayName = "A",
						Location = new Point(138, 269)
					},
					new ButtonSchema
					{
						Name = $"P{controller} C Up",
						Icon = Properties.Resources.YellowUp,
						Location = new Point(173, 210)
					},
					new ButtonSchema
					{
						Name = $"P{controller} C Down",
						Icon = Properties.Resources.YellowDown,
						Location = new Point(173, 231)
					},
					new ButtonSchema
					{
						Name = $"P{controller} C Left",
						Icon = Properties.Resources.YellowLeft,
						Location = new Point(152, 221)
					},
					new ButtonSchema
					{
						Name = $"P{controller} C Right",
						Icon = Properties.Resources.YellowRight,
						Location = new Point(194, 221)
					},
					new ButtonSchema
					{
						Name = $"P{controller} X Axis",
						AxisRange = controllerDefRanges[0],
						SecondaryAxisRange = controllerDefRanges[1],
						Location = new Point(6, 14),
						Type = PadInputType.AnalogStick
					}
				}
			};
		}
	}
}
