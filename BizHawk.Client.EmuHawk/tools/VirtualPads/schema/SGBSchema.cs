using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[Schema("SGB")]
	// ReSharper disable once UnusedMember.Global
	public class SgbSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			yield return StandardController(1);
			yield return StandardController(2);
			yield return StandardController(3);
			yield return StandardController(4);
		}

		private static PadSchema StandardController(int controllerNum)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(174, 79),
				Buttons = new[]
				{
					ButtonSchema.Up($"P{controllerNum} Up", 14, 12),
					ButtonSchema.Down($"P{controllerNum} Down", 14, 56),
					ButtonSchema.Left($"P{controllerNum} Left", 2, 34),
					ButtonSchema.Right($"P{controllerNum} Right", 24, 34),
					new ButtonSchema
					{
						Name = $"P{controllerNum} B",
						DisplayName = "B",
						Location = new Point(122, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controllerNum} A",
						DisplayName = "A",
						Location = new Point(146, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controllerNum} Select",
						DisplayName = "s",
						Location = new Point(52, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controllerNum} Start",
						DisplayName = "S",
						Location = new Point(74, 34)
					}
				}
			};
		}
	}
}
