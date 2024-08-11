using System.Collections.Generic;
using System.Drawing;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.N3DS)]
	public class N3DSSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			yield return Controller();
			yield return Console();
		}

		private static PadSchema Controller()
		{
			static AxisSpec MakeSpec()
				=> new((-128).RangeTo(127), 0);

			return new PadSchema
			{
				Size = new Size(770, 435),
				Buttons =
				[
					new AnalogSchema(6, 14, "Circle Pad X")
					{
						Spec = MakeSpec(),
						SecondarySpec = MakeSpec()
					},

					new AnalogSchema(6, 224, "C-Stick X")
					{
						Spec = MakeSpec(),
						SecondarySpec = MakeSpec()
					},

					ButtonSchema.Up(294, 129),
					ButtonSchema.Down(294, 172),
					ButtonSchema.Left(282, 150),
					ButtonSchema.Right(304, 150),
					new ButtonSchema(292, 10, "L"),
					new ButtonSchema(317, 10, "ZL"),
					new ButtonSchema(710, 10, "R"),
					new ButtonSchema(675, 10, "ZR"),
					new ButtonSchema(685, 179, "Start"),
					new ButtonSchema(685, 201, "Select"),
					new ButtonSchema(685, 100, "Y"),
					new ButtonSchema(709, 113, "B"),
					new ButtonSchema(685, 76, "X"),
					new ButtonSchema(710, 86, "A"),

					// Screen
					new TargetedPairSchema(352, 35, "Touch X")
					{
						TargetSize = new Size(320, 240)
					},
					new ButtonSchema(352, 10, "Touch")
				]
			};
		}

		private static PadSchema Console()
		{
			return new ConsoleSchema
			{
				Size = new Size(240, 200),
				Buttons =
				[
					new ButtonSchema(8, 18, "Tilt"),
					new ButtonSchema(43, 18, "Reset"),

					new SingleAxisSchema(8, 58, "Tilt X")
					{
						TargetSize = new Size(226, 69),
						MinValue = 0,
						MaxValue = 320
					},

					new SingleAxisSchema(8, 128, "Tilt Y")
					{
						TargetSize = new Size(226, 69),
						MinValue = 0,
						MaxValue = 240
					}
				]
			};
		}
	}
}
