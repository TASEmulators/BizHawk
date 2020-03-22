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
					ButtonSchema.Up(24, 230, $"P{controller} DPad U"),
					ButtonSchema.Down(24, 251, $"P{controller} DPad D"),
					ButtonSchema.Left(3, 242, $"P{controller} DPad L"),
					ButtonSchema.Right(45, 242, $"P{controller} DPad R"),
					new ButtonSchema(3, 185)
					{
						Name = $"P{controller} L",
						DisplayName = "L"
					},
					new ButtonSchema(191, 185)
					{
						Name = $"P{controller} R",
						DisplayName = "R"
					},
					new ButtonSchema(81, 269)
					{
						Name = $"P{controller} Z",
						DisplayName = "Z"
					},
					new ButtonSchema(81, 246)
					{
						Name = $"P{controller} Start",
						DisplayName = "S"
					},
					new ButtonSchema(127, 246)
					{
						Name = $"P{controller} B",
						DisplayName = "B"
					},
					new ButtonSchema(138, 269)
					{
						Name = $"P{controller} A",
						DisplayName = "A"
					},
					new ButtonSchema(173, 210)
					{
						Name = $"P{controller} C Up",
						Icon = Properties.Resources.YellowUp
					},
					new ButtonSchema(173, 231)
					{
						Name = $"P{controller} C Down",
						Icon = Properties.Resources.YellowDown
					},
					new ButtonSchema(152, 221)
					{
						Name = $"P{controller} C Left",
						Icon = Properties.Resources.YellowLeft
					},
					new ButtonSchema(194, 221)
					{
						Name = $"P{controller} C Right",
						Icon = Properties.Resources.YellowRight
					},
					new AnalogSchema(6, 14, $"P{controller} X Axis")
					{
						AxisRange = controllerDefRanges[0],
						SecondaryAxisRange = controllerDefRanges[1]
					}
				}
			};
		}
	}
}
