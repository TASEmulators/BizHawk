using System.Collections.Generic;
using System.Drawing;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Computers.TIC80;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.TIC80)]
	public class TIC80Schema : IVirtualPadSchema
	{
		public virtual IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			var inputsActive = ((TIC80)core).InputsActive;
			if (inputsActive[0]) yield return StandardController(1);
			if (inputsActive[1]) yield return StandardController(2);
			if (inputsActive[2]) yield return StandardController(3);
			if (inputsActive[3]) yield return StandardController(4);
			if (inputsActive[4]) yield return Mouse();
			// todo: keyboard
			yield return ConsoleButtons();
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
					new ButtonSchema(100, 45, controller, "B"),
					new ButtonSchema(124, 45, controller, "A"),
					new ButtonSchema(100, 23, controller, "Y"),
					new ButtonSchema(124, 23, controller, "X")
				}
			};
		}

		private static PadSchema Mouse()
		{
			var posRange = new AxisSpec((-128).RangeTo(127), 0);
			var scrollRange = new AxisSpec((-32).RangeTo(31), 0);
			return new PadSchema
			{
				Size = new Size(375, 395),
				Buttons = new PadSchemaControl[]
				{
					new AnalogSchema(6, 14, "Mouse Position X")
					{
						Spec = posRange,
						SecondarySpec = posRange,
					},
					new AnalogSchema(6, 220, "Mouse Scroll X")
					{
						Spec = scrollRange,
						SecondarySpec = scrollRange,
					},
					new ButtonSchema(275, 15, "Mouse Left Click")
					{
						DisplayName = "Left",
					},
					new ButtonSchema(275, 45, "Mouse Middle Click")
					{
						DisplayName = "Middle",
					},
					new ButtonSchema(275, 75, "Mouse Right Click")
					{
						DisplayName = "Right",
					},
				}
			};
		}

		private static PadSchema ConsoleButtons()
		{
			return new ConsoleSchema
			{
				Size = new Size(75, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15, "Reset")
				}
			};
		}
	}
}
