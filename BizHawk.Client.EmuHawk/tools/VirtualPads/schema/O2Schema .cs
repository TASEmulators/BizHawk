using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.O2Hawk;

namespace BizHawk.Client.EmuHawk
{
	[Schema("O2")]
	public class O2Schema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			var O2SyncSettings = ((O2Hawk)core).GetSyncSettings().Clone();
			// var port1 = O2SyncSettings.Port1;
			// var port2 = O2SyncSettings.Port2;

			// if (port1 == "O2 Controller")
			// {
				yield return StandardController(1);
			// }

			// if (port2 == "O2 Controller")
			// {
				yield return StandardController(2);
			// }
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(200, 100),
				Buttons = new[]
				{
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Up",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(14, 12),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Down",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(14, 56),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right",
						Icon = Properties.Resources.Forward,
						Location = new Point(24, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button 1",
						DisplayName = "1",
						Location = new Point(74, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button 2",
						DisplayName = "2",
						Location = new Point(98, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button 3",
						DisplayName = "3",
						Location = new Point(122, 34),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button 4",
						DisplayName = "4",
						Location = new Point(146, 34),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}
	}
}
