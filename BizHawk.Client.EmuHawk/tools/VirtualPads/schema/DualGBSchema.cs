using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[Schema("DGB")]
	// ReSharper disable once UnusedMember.Global
	public class DualGbSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			yield return StandardController(1);
			yield return StandardController(2);
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				IsConsole = false,
				DefaultSize = new Size(174, 79),
				Buttons = new[]
				{
					ButtonSchema.Up($"P{controller} Up", 14, 12),
					ButtonSchema.Down($"P{controller} Down", 14, 56),
					ButtonSchema.Left($"P{controller} Left", 2, 34),
					ButtonSchema.Right($"P{controller} Right", 24, 34),
					new ButtonSchema(122, 34)
					{
						Name = $"P{controller} B",
						DisplayName = "B"
					},
					new ButtonSchema(146, 34)
					{
						Name = $"P{controller} A",
						DisplayName = "A"
					},
					new ButtonSchema(52, 34)
					{
						Name = $"P{controller} Select",
						DisplayName = "s"
					},
					new ButtonSchema(74, 34)
					{
						Name = $"P{controller} Start",
						DisplayName = "S"
					}
				}
			};
		}
	}
}
