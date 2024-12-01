using System.Collections.Generic;
using System.Drawing;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.O2)]
	public class O2Schema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			yield return StandardController(1);
			yield return StandardController(2);
			yield return KeyboardButtons();
			yield return ConsoleButtons();
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				Size = new Size(100, 100),
				Buttons = new[]
				{
					ButtonSchema.Up(14, 12, controller),
					ButtonSchema.Down(14, 56, controller),
					ButtonSchema.Left(2, 34, controller),
					ButtonSchema.Right(24, 34, controller),
					new ButtonSchema(74, 34, controller, "F")
				}
			};
		}

		private static PadSchema KeyboardButtons()
		{
			return new PadSchema
			{
				Size = new Size(275, 200),
				Buttons = new[]
				{
					new ButtonSchema(8, 14, "0"),
					new ButtonSchema(33, 14, "1"),
					new ButtonSchema(58, 14, "2"),
					new ButtonSchema(83, 14, "3"),
					new ButtonSchema(108, 14, "4"),
					new ButtonSchema(133, 14, "5"),
					new ButtonSchema(158, 14, "6"),
					new ButtonSchema(183, 14, "7"),
					new ButtonSchema(208, 14, "8"),
					new ButtonSchema(233, 14, "9"),

					new ButtonSchema(8, 44, "+"),
					new ButtonSchema(33, 44, "-"),
					new ButtonSchema(58, 44, "*") { DisplayName = "x"},
					new ButtonSchema(83, 44, "/") { DisplayName = "÷" },
					new ButtonSchema(108, 44, "="),
					new ButtonSchema(133, 44, "YES") { DisplayName = "y" },
					new ButtonSchema(158, 44, "NO") { DisplayName = "n" },
					new ButtonSchema(183, 44, "CLR") { DisplayName = "cl" },
					new ButtonSchema(216, 44, "ENT") { DisplayName = "enter" },

					new ButtonSchema(8, 74, "Q"),
					new ButtonSchema(33, 74, "W"),
					new ButtonSchema(58, 74, "E"),
					new ButtonSchema(83, 74, "R"),
					new ButtonSchema(108, 74, "T"),
					new ButtonSchema(133, 74, "YES") { DisplayName = "Y" },
					new ButtonSchema(158, 74, "U"),
					new ButtonSchema(183, 74, "I"),
					new ButtonSchema(208, 74, "O"),
					new ButtonSchema(233, 74, "P"),

					new ButtonSchema(20, 104, "A"),
					new ButtonSchema(45, 104, "S"),
					new ButtonSchema(70, 104, "D"),
					new ButtonSchema(95, 104, "F"),
					new ButtonSchema(120, 104, "G"),
					new ButtonSchema(145, 104, "H"),
					new ButtonSchema(170, 104, "J"),
					new ButtonSchema(195, 104, "K"),
					new ButtonSchema(220, 104, "L"),

					
					new ButtonSchema(33, 134, "Z"),
					new ButtonSchema(58, 134, "X"),
					new ButtonSchema(83, 134, "C"),
					new ButtonSchema(108, 134, "V"),
					new ButtonSchema(133, 134, "B"),
					new ButtonSchema(158, 134, "NO") { DisplayName = "N" },
					new ButtonSchema(183, 134, "M"),
					new ButtonSchema(208, 134, "PERIOD") { DisplayName = "." },
					new ButtonSchema(233, 134, "?"),

					new ButtonSchema(95, 164, "SPC") { DisplayName = "  SPACE  " }
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
