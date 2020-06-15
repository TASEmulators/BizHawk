using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Client.EmuHawk
{
	[Schema("PCFX")]
	// ReSharper disable once UnusedMember.Global
	public class PcfxSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			var nyma = (NymaCore) core;
			return NymaSchemas(nyma);
		}

		private static IEnumerable<PadSchema> NymaSchemas(NymaCore nyma)
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
					MessageBox.Show($"Controller type {device} not supported yet.");
				}
			}
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
					new ButtonSchema(72, 17, controller, "MODE 1: Set A") { DisplayName = "1A" },
					new ButtonSchema(72, 40, controller, "MODE 2: Set A") { DisplayName = "2A" },
					new ButtonSchema(102, 17, controller, "MODE 1: Set B") { DisplayName = "1B" },
					new ButtonSchema(102, 40, controller, "MODE 2: Set B") { DisplayName = "2B" },
					new ButtonSchema(140, 63, controller, "IV"),
					new ButtonSchema(166, 53, controller, "V"),
					new ButtonSchema(192, 43, controller, "VI"),
					new ButtonSchema(140, 40, controller, "I"),
					new ButtonSchema(166, 30, controller, "II"),
					new ButtonSchema(192, 20, controller, "III"),
					new ButtonSchema(77, 63, controller, "SELECT") { DisplayName = "s" },
					new ButtonSchema(101, 63, controller, "RUN") { DisplayName = "R" }
				}
			};
		}

		private static PadSchema Mouse(int controller)
		{
			var range = new ControllerDefinition.AxisRange(-127, 0, 127);
			return new PadSchema
			{
				Size = new Size(345, 225),
				Buttons = new PadSchemaControl[]
				{
					new AnalogSchema(6, 14, $"P{controller} Motion Left / Right")
					{
						SecondaryName = $"P{controller} Motion Up / Down",
						AxisRange = range,
						SecondaryAxisRange = range
					},
					new ButtonSchema(275, 15, controller, "Left Button")
					{
						DisplayName = "Left"
					},
					new ButtonSchema(275, 45, controller, "Right Button")
					{
						DisplayName = "Right"
					},
					new ButtonSchema(275, 75, controller, "SELECT")
					{
						DisplayName = "Select"
					},
					new ButtonSchema(275, 105, controller, "RUN")
					{
						DisplayName = "Run"
					}
				}
			};
		}
	}
}
