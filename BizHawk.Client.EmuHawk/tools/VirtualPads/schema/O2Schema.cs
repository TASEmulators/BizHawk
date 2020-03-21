using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.O2Hawk;

namespace BizHawk.Client.EmuHawk
{
	[Schema("O2")]
	// ReSharper disable once UnusedMember.Global
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
				DefaultSize = new Size(100, 100),
				Buttons = new[]
				{
					ButtonSchema.Up($"P{controller} Up", 14, 12),
					ButtonSchema.Down($"P{controller} Down", 14, 56),
					ButtonSchema.Left($"P{controller} Left", 2, 34),
					ButtonSchema.Right($"P{controller} Right", 24, 34),
					new ButtonSchema
					{
						Name = $"P{controller} F",
						DisplayName = "F",
						Location = new Point(74, 34)
					}
				}
			};
		}
	}
}
