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
				ss.Port5,
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
				IsConsole = false,
				DefaultSize = new Size(174, 90),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(14, 12),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(14, 56),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(24, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} B2",
						DisplayName = "II",
						Location = new Point(122, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} B1",
						DisplayName = "I",
						Location = new Point(146, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Select",
						DisplayName = "s",
						Location = new Point(52, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Run",
						DisplayName = "R",
						Location = new Point(74, 34),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}
	}
}
