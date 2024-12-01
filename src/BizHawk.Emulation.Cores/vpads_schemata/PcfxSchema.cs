using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.PCFX)]
	public class PcfxSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			var nyma = (NymaCore) core;
			return NymaSchemas(nyma, showMessageBox);
		}

		private static IEnumerable<PadSchema> NymaSchemas(NymaCore nyma, Action<string> showMessageBox)
		{
			foreach (NymaCore.PortResult result in nyma.ActualPortData)
			{
				var num = int.Parse(result.Port.ShortName.Last().ToString());
				var device = result.Device.ShortName;
				if (device == "gamepad")
				{
					yield return StandardController(num);
				}
				else if (device == "mouse")
				{
					yield return Mouse(num);
				}
				else if (device != "none")
				{
					showMessageBox($"Controller type {device} not supported yet.");
				}
			}

			yield return ConsoleButtons();
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				Size = new Size(230, 100),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, controller),
					ButtonSchema.Down(14, 56, controller),
					ButtonSchema.Left(2, 34, controller),
					ButtonSchema.Right(24, 34, controller),
					new ButtonSchema(72, 17, controller, "Mode 1: Set A", "1A"),
					new ButtonSchema(72, 40, controller, "Mode 2: Set A", "2A"),
					new ButtonSchema(102, 17, controller, "Mode 1: Set B", "1B"),
					new ButtonSchema(102, 40, controller, "Mode 2: Set B", "2B"),
					new ButtonSchema(140, 63, controller, "IV"),
					new ButtonSchema(166, 53, controller, "V"),
					new ButtonSchema(192, 43, controller, "VI"),
					new ButtonSchema(140, 40, controller, "I"),
					new ButtonSchema(166, 30, controller, "II"),
					new ButtonSchema(192, 20, controller, "III"),
					new ButtonSchema(77, 63, controller, "Select", "s"),
					new ButtonSchema(101, 63, controller, "Run", "R")
				}
			};
		}

		private static PadSchema Mouse(int controller)
		{
			var range = new AxisSpec((-127).RangeTo(127), 0);
			return new PadSchema
			{
				Size = new Size(345, 225),
				Buttons = new PadSchemaControl[]
				{
					new AnalogSchema(6, 14, $"P{controller} Motion Left / Right")
					{
						SecondaryName = $"P{controller} Motion Up / Down",
						Spec = range,
						SecondarySpec = range
					},
					new ButtonSchema(275, 15, controller, "Left Button")
					{
						DisplayName = "Left"
					},
					new ButtonSchema(275, 45, controller, "Right Button")
					{
						DisplayName = "Right"
					}
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				Size = new Size(150, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Reset"),
					new ButtonSchema(58, 15, "Power")
				}
			};
		}
	}
}
