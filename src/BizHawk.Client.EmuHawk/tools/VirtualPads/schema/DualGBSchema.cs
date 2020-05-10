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
				Size = new Size(174, 79),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, controller),
					ButtonSchema.Down(14, 56, controller),
					ButtonSchema.Left(2, 34, controller),
					ButtonSchema.Right(24, 34, controller),
					new ButtonSchema(122, 34, controller, "B"),
					new ButtonSchema(146, 34, controller, "A"),
					new ButtonSchema(52, 34, controller, "Select") { DisplayName = "s" },
					new ButtonSchema(74, 34, controller, "Start") { DisplayName = "S" }
				}
			};
		}
	}
}
