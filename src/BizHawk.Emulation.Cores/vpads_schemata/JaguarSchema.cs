using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Atari.Jaguar;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.Jaguar)]
	public class JaguarSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			var ss = ((VirtualJaguar)core).GetSyncSettings();
			if (ss.P1Active) yield return StandardController(1);
			if (ss.P2Active) yield return StandardController(2);
			yield return ConsoleButtons();
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				Size = new Size(184, 200),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, controller),
					ButtonSchema.Down(14, 56, controller),
					ButtonSchema.Left(2, 34, controller),
					ButtonSchema.Right(24, 34, controller),
					new ButtonSchema(106, 62, controller, "C"),
					new ButtonSchema(130, 62, controller, "B"),
					new ButtonSchema(154, 62, controller, "A"),
					new ButtonSchema(130, 12, controller, "Option"),
					new ButtonSchema(130, 37, controller, "Pause"),
					new ButtonSchema(52, 96, controller, "1"),
					new ButtonSchema(76, 96, controller, "2"),
					new ButtonSchema(100, 96, controller, "3"),
					new ButtonSchema(52, 120, controller, "4"),
					new ButtonSchema(76, 120, controller, "5"),
					new ButtonSchema(100, 120, controller, "6"),
					new ButtonSchema(52, 144, controller, "7"),
					new ButtonSchema(76, 144, controller, "8"),
					new ButtonSchema(100, 144, controller, "9"),
					new ButtonSchema(52, 168, controller, "Asterisk") { DisplayName = "*" },
					new ButtonSchema(76, 168, controller, "0"),
					new ButtonSchema(100, 168, controller, "Pound") { DisplayName = "#" },
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				Size = new Size(70, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Power"),
				}
			};
		}
	}
}
