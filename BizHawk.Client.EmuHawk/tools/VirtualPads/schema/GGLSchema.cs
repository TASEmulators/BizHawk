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
				DefaultSize = new Size(174, 90),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, $"P{controller} Up"),
					ButtonSchema.Down(14, 56, $"P{controller} Down"),
					ButtonSchema.Left(2, 34, $"P{controller} Left"),
					ButtonSchema.Right(24, 34, $"P{controller} Right"),
					new ButtonSchema(134, 12)
					{
						Name = $"P{controller} Start",
						DisplayName = "S"
					},
					new ButtonSchema(122, 34)
					{
						Name = $"P{controller} B1",
						DisplayName = "1"
					},
					new ButtonSchema(146, 34)
					{
						Name = $"P{controller} B2",
						DisplayName = "2"
					}
				}
			};
		}
	}
}
