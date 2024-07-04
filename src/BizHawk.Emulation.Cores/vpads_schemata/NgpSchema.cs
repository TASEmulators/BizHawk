using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.NGP)]
	public class NgpSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			yield return Controller();
			yield return ConsoleButtons();
		}

		private static PadSchema Controller()
		{
			return new PadSchema
			{
				Size = new Size(174, 79),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, 1),
					ButtonSchema.Down(14, 56, 1),
					ButtonSchema.Left(2, 34, 1),
					ButtonSchema.Right(24, 34, 1),
					new ButtonSchema(74, 34, 1, "B"),
					new ButtonSchema(98, 34, 1, "A"),
					new ButtonSchema(146, 12, 1, "Option", "O")
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				Size = new Size(150, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Reset"),
					new ButtonSchema(58, 15, "Power")
				}
			};
		}
	}
}
