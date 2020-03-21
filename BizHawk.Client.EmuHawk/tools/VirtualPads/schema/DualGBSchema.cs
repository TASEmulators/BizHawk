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
					new ButtonSchema
					{
						Name = $"P{controller} B",
						DisplayName = "B",
						Location = new Point(122, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} A",
						DisplayName = "A",
						Location = new Point(146, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Select",
						DisplayName = "s",
						Location = new Point(52, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "S",
						Location = new Point(74, 34)
					}
				}
			};
		}
	}
}
