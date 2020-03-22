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
				DefaultSize = new Size(174, 79),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, $"P{controller} Up"),
					ButtonSchema.Down(14, 56, $"P{controller} Down"),
					ButtonSchema.Left(2, 34, $"P{controller} Left"),
					ButtonSchema.Right(24, 34, $"P{controller} Right"),
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
