using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.AppleII)]
	public class AppleIISchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			yield return Keyboard();
			yield return DiskSelection();
		}

		private static PadSchema Keyboard()
		{
			return new PadSchema
			{
				DisplayName = "Keyboard",
				Size = new Size(390, 150),
				Buttons = new[]
				{
					/************************** Row 1 **************************/
					new ButtonSchema(10, 18, "Escape") { DisplayName = "Esc" },
					new ButtonSchema(46, 18, "1"),
					new ButtonSchema(70, 18, "2"),
					new ButtonSchema(94, 18, "3"),
					new ButtonSchema(118, 18, "4"),
					new ButtonSchema(142, 18, "5"),
					new ButtonSchema(166, 18, "6"),
					new ButtonSchema(190, 18, "7"),
					new ButtonSchema(214, 18, "8"),
					new ButtonSchema(238, 18, "9"),
					new ButtonSchema(262, 18, "0"),
					new ButtonSchema(286, 18, "-"),
					new ButtonSchema(307, 18, "="),
					new ButtonSchema(331, 18, "Delete"),
					
					/************************** Row 2 **************************/
					new ButtonSchema(10, 42, "Tab") { DisplayName = " Tab " },
					new ButtonSchema(52, 42, "Q"),
					new ButtonSchema(78, 42, "W"),
					new ButtonSchema(106, 42, "E"),
					new ButtonSchema(130, 42, "R"),
					new ButtonSchema(156, 42, "T"),
					new ButtonSchema(180, 42, "Y"),
					new ButtonSchema(204, 42, "U"),
					new ButtonSchema(230, 42, "I"),
					new ButtonSchema(250, 42, "O"),
					new ButtonSchema(276, 42, "P"),
					new ButtonSchema(302, 42, "["),
					new ButtonSchema(325, 42, "]"),
					new ButtonSchema(349, 42, "\\") { DisplayName = " \\ " },

					/************************** Row 3 **************************/
					new ButtonSchema(10, 66, "Control") { DisplayName = " Control " },
					new ButtonSchema(66, 66, "A"),
					new ButtonSchema(90, 66, "S"),
					new ButtonSchema(114, 66, "D"),
					new ButtonSchema(140, 66, "F"),
					new ButtonSchema(164, 66, "G"),
					new ButtonSchema(190, 66, "H"),
					new ButtonSchema(216, 66, "J"),
					new ButtonSchema(238, 66, "K"),
					new ButtonSchema(262, 66, "L"),
					new ButtonSchema(286, 66, ";"),
					new ButtonSchema(307, 66, "'"),
					new ButtonSchema(328, 66, "Return"),

					/************************** Row 4 **************************/
					new ButtonSchema(10, 90, "Shift") { DisplayName = "     Shift     " },
					new ButtonSchema(80, 90, "Z"),
					new ButtonSchema(106, 90, "X"),
					new ButtonSchema(130, 90, "C"),
					new ButtonSchema(154, 90, "V"),
					new ButtonSchema(178, 90, "B"),
					new ButtonSchema(202, 90, "N"),
					new ButtonSchema(226, 90, "M"),
					new ButtonSchema(252, 90, ","),
					new ButtonSchema(272, 90, "."),
					new ButtonSchema(292, 90, "/"),
					new ButtonSchema(315, 90, "Shift") { DisplayName = "    Shift    " },

					/************************** Row 5 **************************/

					new ButtonSchema(10, 114, "Caps Lock") { DisplayName = "Caps" },
					new ButtonSchema(52, 114, "`") { DisplayName = "~" },
					new ButtonSchema(96, 114, "White Apple") { DisplayName = "<" },
					new ButtonSchema(120, 114, "Space")
					{
						DisplayName = "                Space                "
					},
					new ButtonSchema(265, 114, "Black Apple") { DisplayName = ">" },
					ButtonSchema.Left(289, 114),
					ButtonSchema.Right(311, 114),
					ButtonSchema.Down(333, 114),
					ButtonSchema.Up(355, 114)
				}
			};
		}

		private static PadSchema DiskSelection()
		{
			return new PadSchema
			{
				DisplayName = "Disk Selection",
				Size = new Size(120, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 18, "Next Disk") { DisplayName = "Next" },
					new ButtonSchema(50, 18, "Previous Disk") { DisplayName = "Previous" }
				}
			};
		}
	}
}
