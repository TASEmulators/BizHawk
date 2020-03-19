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
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Up",
						DisplayName = "",
						Icon = Properties.Resources.BlueUp,
						Location = new Point(23, 15),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Down",
						DisplayName = "",
						Icon = Properties.Resources.BlueDown,
						Location = new Point(23, 36),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Left",
						DisplayName = "",
						Icon = Properties.Resources.Back,
						Location = new Point(2, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Right",
						DisplayName = "",
						Icon = Properties.Resources.Forward,
						Location = new Point(44, 24),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = $"P{controller} Button",
						DisplayName = "B",
						Location = new Point(124, 24),
						Type = PadSchema.PadInputType.Boolean
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
					new PadSchema.ButtonSchema
					{
						Name = "Key Left Arrow",
						DisplayName = "←",
						Location = new Point(16, 18),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key 1",
						DisplayName = "1",
						Location = new Point(46, 18),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key 2",
						DisplayName = "2",
						Location = new Point(70, 18),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key 3",
						DisplayName = "3",
						Location = new Point(94, 18),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key 4",
						DisplayName = "4",
						Location = new Point(118, 18),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key 5",
						DisplayName = "5",
						Location = new Point(142, 18),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key 6",
						DisplayName = "6",
						Location = new Point(166, 18),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key 7",
						DisplayName = "7",
						Location = new Point(190, 18),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key 8",
						DisplayName = "8",
						Location = new Point(214, 18),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key 9",
						DisplayName = "9",
						Location = new Point(238, 18),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key 0",
						DisplayName = "0",
						Location = new Point(262, 18),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Plus",
						DisplayName = "+",
						Location = new Point(286, 18),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Minus",
						DisplayName = "-",
						Location = new Point(310, 18),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Pound",
						DisplayName = "£",
						Location = new Point(330, 18),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Clear/Home",
						DisplayName = "C/H",
						Location = new Point(354, 18),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Insert/Delete",
						DisplayName = "I/D",
						Location = new Point(392, 18),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key F1",
						DisplayName = "F 1",
						Location = new Point(450, 18),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key F3",
						DisplayName = "F 3",
						Location = new Point(450, 42),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key F5",
						DisplayName = "F 5",
						Location = new Point(450, 66),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key F7",
						DisplayName = "F 7",
						Location = new Point(450, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Control",
						DisplayName = "CTRL",
						Location = new Point(16, 42),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Q",
						DisplayName = "Q",
						Location = new Point(62, 42),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key W",
						DisplayName = "W",
						Location = new Point(88, 42),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key E",
						DisplayName = "E",
						Location = new Point(116, 42),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key R",
						DisplayName = "R",
						Location = new Point(140, 42),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key T",
						DisplayName = "T",
						Location = new Point(166, 42),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Y",
						DisplayName = "Y",
						Location = new Point(190, 42),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key U",
						DisplayName = "U",
						Location = new Point(214, 42),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key I",
						DisplayName = "I",
						Location = new Point(240, 42),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key O",
						DisplayName = "O",
						Location = new Point(260, 42),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key P",
						DisplayName = "P",
						Location = new Point(286, 42),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key At",
						DisplayName = "@",
						Location = new Point(310, 42),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Asterisk",
						DisplayName = "*",
						Location = new Point(338, 42),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Up Arrow",
						DisplayName = "↑",
						Location = new Point(360, 42),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Restore",
						DisplayName = "RST",
						Location = new Point(390, 42),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Run/Stop",
						DisplayName = "R/S",
						Location = new Point(12, 66),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Lck",
						DisplayName = "Lck",
						Location = new Point(50, 66),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key A",
						DisplayName = "A",
						Location = new Point(86, 66),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key S",
						DisplayName = "S",
						Location = new Point(110, 66),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key D",
						DisplayName = "D",
						Location = new Point(134, 66),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key F",
						DisplayName = "F",
						Location = new Point(160, 66),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key G",
						DisplayName = "G",
						Location = new Point(184, 66),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key H",
						DisplayName = "H",
						Location = new Point(210, 66),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key J",
						DisplayName = "J",
						Location = new Point(236, 66),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key K",
						DisplayName = "K",
						Location = new Point(258, 66),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key L",
						DisplayName = "L",
						Location = new Point(282, 66),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Colon",
						DisplayName = ":",
						Location = new Point(306, 66),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Semicolon",
						DisplayName = ";",
						Location = new Point(326, 66),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Equal",
						DisplayName = "=",
						Location = new Point(346, 66),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Return",
						DisplayName = "Return",
						Location = new Point(370, 66),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Commodore",
						DisplayName = "C64",
						Location = new Point(8, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Left Shift",
						DisplayName = "Shift",
						Location = new Point(44, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Z",
						DisplayName = "Z",
						Location = new Point(82, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key X",
						DisplayName = "X",
						Location = new Point(106, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key C",
						DisplayName = "C",
						Location = new Point(130, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key V",
						DisplayName = "V",
						Location = new Point(154, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key B",
						DisplayName = "B",
						Location = new Point(178, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key N",
						DisplayName = "N",
						Location = new Point(202, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key M",
						DisplayName = "M",
						Location = new Point(226, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Comma",
						DisplayName = ",",
						Location = new Point(252, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Period",
						DisplayName = ".",
						Location = new Point(272, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Slash",
						DisplayName = "/",
						Location = new Point(292, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Right Shift",
						DisplayName = "Shift",
						Location = new Point(314, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Cursor Up/Down",
						DisplayName = "Csr U",
						Location = new Point(352, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Cursor Left/Right",
						DisplayName = "Csr L",
						Location = new Point(396, 90),
						Type = PadSchema.PadInputType.Boolean
					},
					new PadSchema.ButtonSchema
					{
						Name = "Key Space",
						DisplayName = "                          Space                          ",
						Location = new Point(120, 114),
						Type = PadSchema.PadInputType.Boolean
					}
				}
			};
		}
	}
}
