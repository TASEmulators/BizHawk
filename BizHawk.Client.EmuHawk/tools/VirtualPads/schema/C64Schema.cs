using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[Schema("C64")]
	// ReSharper disable once UnusedMember.Global
	public class C64Schema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			yield return StandardController(1);
			yield return StandardController(2);
			yield return Keyboard();
		}

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"Player {controller}",
				IsConsole = false,
				DefaultSize = new Size(174, 74),
				MaxSize = new Size(174, 74),
				Buttons = new[]
				{
					ButtonSchema.Up( 23, 15, $"P{controller} Up"),
					ButtonSchema.Down(23, 36, $"P{controller} Down"),
					ButtonSchema.Left(2, 24, $"P{controller} Left"),
					ButtonSchema.Right(44, 24, $"P{controller} Right"), 
					new ButtonSchema(124, 24)
					{
						Name = $"P{controller} Button",
						DisplayName = "B"
					}
				}
			};
		}

		private static PadSchema Keyboard()
		{
			return new PadSchema
			{
				DisplayName = "Keyboard",
				IsConsole = false,
				DefaultSize = new Size(500, 150),
				Buttons = new[]
				{
					new ButtonSchema(16, 18)
					{
						Name = "Key Left Arrow",
						DisplayName = "←"
					},
					new ButtonSchema(46, 18)
					{
						Name = "Key 1",
						DisplayName = "1"
					},
					new ButtonSchema(70, 18)
					{
						Name = "Key 2",
						DisplayName = "2"
					},
					new ButtonSchema(94, 18)
					{
						Name = "Key 3",
						DisplayName = "3"
					},
					new ButtonSchema(118, 18)
					{
						Name = "Key 4",
						DisplayName = "4"
					},
					new ButtonSchema(142, 18)
					{
						Name = "Key 5",
						DisplayName = "5"
					},
					new ButtonSchema(166, 18)
					{
						Name = "Key 6",
						DisplayName = "6"
					},
					new ButtonSchema(190, 18)
					{
						Name = "Key 7",
						DisplayName = "7"
					},
					new ButtonSchema(214, 18)
					{
						Name = "Key 8",
						DisplayName = "8"
					},
					new ButtonSchema(238, 18)
					{
						Name = "Key 9",
						DisplayName = "9"
					},
					new ButtonSchema(262, 18)
					{
						Name = "Key 0",
						DisplayName = "0"
					},
					new ButtonSchema(286, 18)
					{
						Name = "Key Plus",
						DisplayName = "+"
					},
					new ButtonSchema(310, 18)
					{
						Name = "Key Minus",
						DisplayName = "-"
					},
					new ButtonSchema(330, 18)
					{
						Name = "Key Pound",
						DisplayName = "£"
					},
					new ButtonSchema(354, 18)
					{
						Name = "Key Clear/Home",
						DisplayName = "C/H"
					},
					new ButtonSchema(392, 18)
					{
						Name = "Key Insert/Delete",
						DisplayName = "I/D"
					},
					new ButtonSchema(450, 18)
					{
						Name = "Key F1",
						DisplayName = "F 1"
					},
					new ButtonSchema(450, 42)
					{
						Name = "Key F3",
						DisplayName = "F 3"
					},
					new ButtonSchema(450, 66)
					{
						Name = "Key F5",
						DisplayName = "F 5"
					},
					new ButtonSchema(450, 90)
					{
						Name = "Key F7",
						DisplayName = "F 7"
					},
					new ButtonSchema(16, 42)
					{
						Name = "Key Control",
						DisplayName = "CTRL"
					},
					new ButtonSchema(62, 42)
					{
						Name = "Key Q",
						DisplayName = "Q"
					},
					new ButtonSchema(88, 42)
					{
						Name = "Key W",
						DisplayName = "W"
					},
					new ButtonSchema(116, 42)
					{
						Name = "Key E",
						DisplayName = "E"
					},
					new ButtonSchema(140, 42)
					{
						Name = "Key R",
						DisplayName = "R"
					},
					new ButtonSchema(166, 42)
					{
						Name = "Key T",
						DisplayName = "T"
					},
					new ButtonSchema(190, 42)
					{
						Name = "Key Y",
						DisplayName = "Y"
					},
					new ButtonSchema(214, 42)
					{
						Name = "Key U",
						DisplayName = "U"
					},
					new ButtonSchema(240, 42)
					{
						Name = "Key I",
						DisplayName = "I"
					},
					new ButtonSchema(260, 42)
					{
						Name = "Key O",
						DisplayName = "O"
					},
					new ButtonSchema(286, 42)
					{
						Name = "Key P",
						DisplayName = "P"
					},
					new ButtonSchema(310, 42)
					{
						Name = "Key At",
						DisplayName = "@"
					},
					new ButtonSchema(338, 42)
					{
						Name = "Key Asterisk",
						DisplayName = "*"
					},
					new ButtonSchema(360, 42)
					{
						Name = "Key Up Arrow",
						DisplayName = "↑"
					},
					new ButtonSchema(390, 42)
					{
						Name = "Key Restore",
						DisplayName = "RST"
					},
					new ButtonSchema(12, 66)
					{
						Name = "Key Run/Stop",
						DisplayName = "R/S"
					},
					new ButtonSchema(50, 66)
					{
						Name = "Key Lck",
						DisplayName = "Lck"
					},
					new ButtonSchema(86, 66)
					{
						Name = "Key A",
						DisplayName = "A"
					},
					new ButtonSchema(110, 66)
					{
						Name = "Key S",
						DisplayName = "S"
					},
					new ButtonSchema(134, 66)
					{
						Name = "Key D",
						DisplayName = "D"
					},
					new ButtonSchema(160, 66)
					{
						Name = "Key F",
						DisplayName = "F"
					},
					new ButtonSchema(184, 66)
					{
						Name = "Key G",
						DisplayName = "G"
					},
					new ButtonSchema(210, 66)
					{
						Name = "Key H",
						DisplayName = "H"
					},
					new ButtonSchema(236, 66)
					{
						Name = "Key J",
						DisplayName = "J"
					},
					new ButtonSchema(258, 66)
					{
						Name = "Key K",
						DisplayName = "K"
					},
					new ButtonSchema(282, 66)
					{
						Name = "Key L",
						DisplayName = "L"
					},
					new ButtonSchema(306, 66)
					{
						Name = "Key Colon",
						DisplayName = ":"
					},
					new ButtonSchema(326, 66)
					{
						Name = "Key Semicolon",
						DisplayName = ";"
					},
					new ButtonSchema(346, 66)
					{
						Name = "Key Equal",
						DisplayName = "="
					},
					new ButtonSchema(370, 66)
					{
						Name = "Key Return",
						DisplayName = "Return"
					},
					new ButtonSchema(8, 90)
					{
						Name = "Key Commodore",
						DisplayName = "C64"
					},
					new ButtonSchema(44, 90)
					{
						Name = "Key Left Shift",
						DisplayName = "Shift"
					},
					new ButtonSchema(82, 90)
					{
						Name = "Key Z",
						DisplayName = "Z"
					},
					new ButtonSchema(106, 90)
					{
						Name = "Key X",
						DisplayName = "X"
					},
					new ButtonSchema(130, 90)
					{
						Name = "Key C",
						DisplayName = "C"
					},
					new ButtonSchema(154, 90)
					{
						Name = "Key V",
						DisplayName = "V"
					},
					new ButtonSchema(178, 90)
					{
						Name = "Key B",
						DisplayName = "B"
					},
					new ButtonSchema(202, 90)
					{
						Name = "Key N",
						DisplayName = "N"
					},
					new ButtonSchema(226, 90)
					{
						Name = "Key M",
						DisplayName = "M"
					},
					new ButtonSchema(252, 90)
					{
						Name = "Key Comma",
						DisplayName = ","
					},
					new ButtonSchema(272, 90)
					{
						Name = "Key Period",
						DisplayName = "."
					},
					new ButtonSchema(292, 90)
					{
						Name = "Key Slash",
						DisplayName = "/"
					},
					new ButtonSchema(314, 90)
					{
						Name = "Key Right Shift",
						DisplayName = "Shift"
					},
					new ButtonSchema(352, 90)
					{
						Name = "Key Cursor Up/Down",
						DisplayName = "Csr U"
					},
					new ButtonSchema(396, 90)
					{
						Name = "Key Cursor Left/Right",
						DisplayName = "Csr L"
					},
					new ButtonSchema(120, 114)
					{
						Name = "Key Space",
						DisplayName = "                          Space                          "
					}
				}
			};
		}
	}
}
