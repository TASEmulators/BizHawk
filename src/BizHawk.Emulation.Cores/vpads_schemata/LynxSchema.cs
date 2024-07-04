using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.Lynx)]
	public class LynxSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			yield return new PadSchema
			{
				Size = new Size(194, 90),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12),
					ButtonSchema.Down(14, 56),
					ButtonSchema.Left(2, 34),
					ButtonSchema.Right(24, 34),
					new ButtonSchema(130, 62, "B"),
					new ButtonSchema(154, 62, "A"),
					new ButtonSchema(100, 12, "Option 1") { DisplayName = "1" },
					new ButtonSchema(100, 62, "Option 2") { DisplayName = "2" },
					new ButtonSchema(100, 37, "Pause")
				}
			};
		}
	}
}
