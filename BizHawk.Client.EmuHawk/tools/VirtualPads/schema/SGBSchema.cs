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
					ButtonSchema.Up(14, 12, $"P{controllerNum} Up"),
					ButtonSchema.Down(14, 56, $"P{controllerNum} Down"),
					ButtonSchema.Left(2, 34, $"P{controllerNum} Left"),
					ButtonSchema.Right(24, 34, $"P{controllerNum} Right"),
					new ButtonSchema(122, 34)
					{
						Name = $"P{controllerNum} B",
						DisplayName = "B"
					},
					new ButtonSchema(146, 34)
					{
						Name = $"P{controllerNum} A",
						DisplayName = "A"
					},
					new ButtonSchema(52, 34)
					{
						Name = $"P{controllerNum} Select",
						DisplayName = "s"
					},
					new ButtonSchema(74, 34)
					{
						Name = $"P{controllerNum} Start",
						DisplayName = "S"
					}
				}
			};
		}
	}
}
