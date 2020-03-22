using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.PCEngine;

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
			var ss = ((PCEngine)core).GetSyncSettings();

			var padSchemas = new[]
			{
				ss.Port1,
				ss.Port2,
				ss.Port3,
				ss.Port4,
				ss.Port5
			}
			.Where(p => p != PceControllerType.Unplugged)
			.Select((p, i) => GenerateSchemaForPort(p, i + 1))
			.Where(s => s != null);

			return padSchemas;
		}

		private static PadSchema GenerateSchemaForPort(PceControllerType type, int controller)
		{
			switch (type)
			{
				default:
					MessageBox.Show($"{type} is not supported yet");
					return null;
				case PceControllerType.Unplugged:
					return null;
				case PceControllerType.GamePad:
					return StandardController(controller);
			}
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				DefaultSize = new Size(174, 90),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, $"P{controller} Up"),
					ButtonSchema.Down(14, 56, $"P{controller} Down"),
					ButtonSchema.Left(2, 34, $"P{controller} Left"),
					ButtonSchema.Right(24, 34, $"P{controller} Right"),
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
	}
}
