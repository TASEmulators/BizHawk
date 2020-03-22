using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[Schema("AppleII")]
	// ReSharper disable once UnusedMember.Global
	public class AppleIISchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			yield return Keyboard();
			yield return DiskSelection();
		}

		private static PadSchema Keyboard()
		{
			return new PadSchema
			{
				DisplayName = "Keyboard",
				IsConsole = false,
				DefaultSize = new Size(390, 150),
				Buttons = new[]
				{
					/************************** Row 1 **************************/
					new ButtonSchema(10, 18) { Name = "Escape",  DisplayName = "Esc"
					},
					new ButtonSchema(46, 18) { Name = "1" },
					new ButtonSchema(70, 18) { Name = "2" },
					new ButtonSchema(94, 18) { Name = "3" },
					new ButtonSchema(118, 18) { Name = "4" },
					new ButtonSchema(142, 18) { Name = "5" },
					new ButtonSchema(166, 18) { Name = "6" },
					new ButtonSchema(190, 18) { Name = "7" },
					new ButtonSchema(214, 18) { Name = "8" },
					new ButtonSchema(238, 18) { Name = "9" },
					new ButtonSchema(262, 18) { Name = "0" },
					new ButtonSchema(286, 18) { Name = "-" },
					new ButtonSchema(307, 18) { Name = "=" },
					new ButtonSchema(331, 18) { Name = "Delete" },
					
					/************************** Row 2 **************************/
					new ButtonSchema(10, 42) { Name = "Tab",  DisplayName = " Tab " },
					new ButtonSchema(52, 42) { Name = "Q" },
					new ButtonSchema(78, 42) { Name = "W" },
					new ButtonSchema(106, 42) { Name = "E" },
					new ButtonSchema(130, 42) { Name = "R" },
					new ButtonSchema(156, 42) { Name = "T" },
					new ButtonSchema(180, 42) { Name = "Y" },
					new ButtonSchema(204, 42) { Name = "U" },
					new ButtonSchema(230, 42) { Name = "I" },
					new ButtonSchema(250, 42) { Name = "O" },
					new ButtonSchema(276, 42) { Name = "P" },
					new ButtonSchema(302, 42) { Name = "[" },
					new ButtonSchema(325, 42) { Name = "]" },
					new ButtonSchema(349, 42) { Name = "\\",  DisplayName = " \\ " },

					/************************** Row 3 **************************/
					new ButtonSchema(10, 66) { Name = "Control",  DisplayName = " Control " },
					new ButtonSchema(66, 66) { Name = "A" },
					new ButtonSchema(90, 66) { Name = "S" },
					new ButtonSchema(114, 66) { Name = "D" },
					new ButtonSchema(140, 66) { Name = "F" },
					new ButtonSchema(164, 66) { Name = "G" },
					new ButtonSchema(190, 66) { Name = "H" },
					new ButtonSchema(216, 66) { Name = "J" },
					new ButtonSchema(238, 66) { Name = "K" },
					new ButtonSchema(262, 66) { Name = "L" },
					new ButtonSchema(286, 66) { Name = ";" },
					new ButtonSchema(307, 66) { Name = "'" },
					new ButtonSchema(328, 66) { Name = "Return" },

					/************************** Row 4 **************************/
					new ButtonSchema(10, 90) { Name = "Shift", DisplayName = "     Shift     " },
					new ButtonSchema(80, 90) { Name = "Z" },
					new ButtonSchema(106, 90) { Name = "X" },
					new ButtonSchema(130, 90) { Name = "C" },
					new ButtonSchema(154, 90) { Name = "V" },
					new ButtonSchema(178, 90) { Name = "B" },
					new ButtonSchema(202, 90) { Name = "N" },
					new ButtonSchema(226, 90) { Name = "M" },
					new ButtonSchema(252, 90) { Name = "," },
					new ButtonSchema(272, 90) { Name = "." },
					new ButtonSchema(292, 90) { Name = "/" },
					new ButtonSchema(315, 90) { Name = "Shift", DisplayName = "    Shift    " },

					/************************** Row 5 **************************/

					new ButtonSchema(10, 114)
					{
						Name = "Caps Lock",
						DisplayName = "Caps"
					},
					new ButtonSchema(52, 114)
					{
						Name = "`",
						DisplayName = "~"
					},
					new ButtonSchema(96, 114)
					{
						Name = "White Apple",
						DisplayName = "<"
					},
					new ButtonSchema(120, 114)
					{
						Name = "Space",
						DisplayName = "                Space                "
					},
					new ButtonSchema(265, 114)
					{
						Name = "Black Apple",
						DisplayName = ">"
					},
					ButtonSchema.Left("Left", 289, 114),
					ButtonSchema.Right("Right", 311, 114),
					ButtonSchema.Down("Down", 333, 114),
					ButtonSchema.Up("Up", 355, 114), 
				}
			};
		}

		private static PadSchema DiskSelection()
		{
			return new PadSchema
			{
				DisplayName = "Disk Selection",
				IsConsole = false,
				DefaultSize = new Size(120, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 18)
					{
						Name = "Next Disk",
						DisplayName = "Next"
					},
					new ButtonSchema(50, 18)
					{
						Name = "Previous Disk",
						DisplayName = "Previous"
					}
				}
			};
		}
	}
}
