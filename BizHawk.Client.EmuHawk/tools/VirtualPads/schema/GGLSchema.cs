using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[Schema("GGL")]
	// ReSharper disable once UnusedMember.Global
	public class GGLSchema : IVirtualPadSchema
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
				DefaultSize = new Size(174, 90),
				Buttons = new[]
				{
					ButtonSchema.Up($"P{controller} Up", 14, 12),
					ButtonSchema.Down($"P{controller} Down", 14, 56),
					ButtonSchema.Left($"P{controller} Left", 2, 34),
					ButtonSchema.Right($"P{controller} Right", 24, 34),
					new ButtonSchema
					{
						Name = $"P{controller} Start",
						DisplayName = "S",
						Location = new Point(134, 12)
					},
					new ButtonSchema
					{
						Name = $"P{controller} B1",
						DisplayName = "1",
						Location = new Point(122, 34)
					},
					new ButtonSchema
					{
						Name = $"P{controller} B2",
						DisplayName = "2",
						Location = new Point(146, 34)
					}
				}
			};
		}
	}
}
