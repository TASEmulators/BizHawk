using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Client.EmuHawk
{
	[Schema("PCECD")]
	// ReSharper disable once UnusedMember.Global
	public class PceCdSchema : PceSchema { }

	[Schema("PCE")]
	public class PceSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			return core switch
			{
				PCEngine pce => PceHawkSchemas(pce),
				NymaCore hyper => NymaSchemas(hyper),
				_ => Enumerable.Empty<PadSchema>()
			};
		}

		private static IEnumerable<PadSchema> PceHawkSchemas(PCEngine core)
		{
			var ss = core.GetSyncSettings();

			var padSchemas = new[]
				{
					ss.Port1,
					ss.Port2,
					ss.Port3,
					ss.Port4,
					ss.Port5
				}
				.Where(p => p != PceControllerType.Unplugged)
				.Select((p, i) => PceHawkGenerateSchemaForPort(p, i + 1))
				.Where(s => s != null);

			return padSchemas;
		}

		private static PadSchema PceHawkGenerateSchemaForPort(PceControllerType type, int controller)
		{
			switch (type)
			{
				default:
					MessageBox.Show($"{type} is not supported yet");
					return null;
				case PceControllerType.Unplugged:
					return null;
				case PceControllerType.GamePad:
					return StandardHawkController(controller);
			}
		}

		private static PadSchema StandardHawkController(int controller)
		{
			return new PadSchema
			{
				Size = new Size(174, 90),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, controller),
					ButtonSchema.Down(14, 56, controller),
					ButtonSchema.Left(2, 34, controller),
					ButtonSchema.Right(24, 34, controller),
					new ButtonSchema(122, 34, controller, "B2")
					{
						DisplayName = "II"
					},
					new ButtonSchema(146, 34, controller, "B1")
					{
						DisplayName = "I"
					},
					new ButtonSchema(52, 34, controller, "Select")
					{
						DisplayName = "s"
					},
					new ButtonSchema(74, 34, controller, "Run")
					{
						DisplayName = "R"
					}
				}
			};
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
				Size = new Size(174, 90),
				Buttons = new[]
				{
					ButtonSchema.NymaUp(14, 12, controller),
					ButtonSchema.NymaDown(14, 56, controller),
					ButtonSchema.NymaLeft(2, 34, controller),
					ButtonSchema.NymaRight(24, 34, controller),
					new ButtonSchema(122, 34, controller, "I"),
					new ButtonSchema(146, 34, controller, "II"),
					new ButtonSchema(52, 34, controller, "SELECT") { DisplayName = "s" },
					new ButtonSchema(74, 34, controller, "RUN") { DisplayName = "R" }
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
