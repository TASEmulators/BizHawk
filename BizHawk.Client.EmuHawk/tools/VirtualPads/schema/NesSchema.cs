using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SubNESHawk;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;

namespace BizHawk.Client.EmuHawk
{
	[Schema("NES")]
	// ReSharper disable once UnusedMember.Global
	public class NesSchema : IVirtualPadSchema
	{
		/// <exception cref="Exception">found <c>ControllerSNES</c></exception>
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core)
		{
			if (core is NES || core is SubNESHawk)
			{
				NES.NESSyncSettings ss = null;
				bool isFds = false;
				int fdsButtonCount = 0;
				if (core is NES nesHawk)
				{
					ss = nesHawk.GetSyncSettings();
					isFds = nesHawk.IsFDS;
					fdsButtonCount = nesHawk.ControllerDefinition.BoolButtons.Count(b => b.StartsWith("FDS Insert "));
				}
				else if (core is SubNESHawk subNesHawk)
				{
					ss = subNesHawk.GetSyncSettings();
					isFds = subNesHawk.IsFds;
					fdsButtonCount = subNesHawk.ControllerDefinition.BoolButtons.Count(b => b.StartsWith("FDS Insert "));
				}

				if (ss.Controls.Famicom)
				{
					yield return StandardController(1);
					yield return Famicom2ndController();

					switch (ss.Controls.FamicomExpPort)
					{
						default:
						case "UnpluggedFam":
							break;
						case "Zapper":
							yield return Zapper(3);
							break;
						case "ArkanoidFam":
							yield return ArkanoidPaddle(3);
							break;
						case "Famicom4P":
							yield return StandardController(3);
							yield return StandardController(4);
							break;
						case "FamilyBasicKeyboard":
							yield return FamicomFamilyKeyboard(3);
							break;
						case "OekaKids":
							yield return OekaKidsTablet(3);
							break;
					}
				}
				else
				{
					var currentControllerNo = 1;
					switch (ss.Controls.NesLeftPort)
					{
						default:
						case "UnpluggedNES":
							break;
						case "ControllerNES":
							yield return StandardController(1);
							currentControllerNo++;
							break;
						case "Zapper":
							yield return Zapper(1);
							currentControllerNo++;
							break;
						case "ArkanoidNES":
							yield return ArkanoidPaddle(1);
							currentControllerNo++;
							break;
						case "FourScore":
							yield return StandardController(1);
							yield return StandardController(2);
							currentControllerNo += 2;
							break;
						case "PowerPad":
							yield return PowerPad(1);
							currentControllerNo++;
							break;
						case "ControllerSNES":
							throw new Exception("TODO");
					}

					switch (ss.Controls.NesRightPort)
					{
						default:
						case "UnpluggedNES":
							break;
						case "ControllerNES":
							yield return StandardController(currentControllerNo);
							break;
						case "Zapper":
							yield return Zapper(currentControllerNo);
							break;
						case "ArkanoidNES":
							yield return ArkanoidPaddle(currentControllerNo);
							break;
						case "FourScore":
							yield return StandardController(currentControllerNo);
							yield return StandardController(currentControllerNo + 1);
							currentControllerNo += 2;
							break;
						case "PowerPad":
							yield return PowerPad(currentControllerNo);
							break;
						case "ControllerSNES":
							throw new Exception("TODO");
					}

					if (currentControllerNo == 0)
					{
						yield return null;
					}
				}

				if (isFds)
				{
					yield return FdsConsoleButtons(fdsButtonCount);
				}
				else
				{
					yield return NesConsoleButtons();
				}
			}
			else
				// Quicknes Can support none, one or two controllers.
			{
				var ss = ((QuickNES)core).GetSyncSettings();
				if (ss.LeftPortConnected && ss.RightPortConnected)
				{
					// Set both controllers
					yield return StandardController(1);
					yield return StandardController(2);
				}
				else if (ss.LeftPortConnected && !ss.RightPortConnected)
				{
					yield return StandardController(1);
				}
				else if (!ss.LeftPortConnected && ss.RightPortConnected)
				{
					yield return StandardController(1);
				}

				yield return NesConsoleButtons();
			}
		}

		private static PadSchema NesConsoleButtons()
		{
			return new PadSchema
			{
				DisplayName = "Console",
				IsConsole = true,
				DefaultSize = new Size(150, 50),
				Buttons = new[]
				{
					new ButtonSchema(10, 15) { Name = "Reset" },
					new ButtonSchema(58, 15) { Name = "Power" }
				}
			};
		}

		private static PadSchema FdsConsoleButtons(int diskSize)
		{
			var buttons = new List<ButtonSchema>
			{
				new ButtonSchema(10, 15) { Name = "Reset" },
				new ButtonSchema(58, 15) { Name = "Power" },
				new ButtonSchema(108, 15)
				{
					Name = "FDS Eject",
					DisplayName = "Eject"
				}
			};

			for (var i = 0; i < diskSize; i++)
			{
				buttons.Add(new ButtonSchema(10 + (i * 58), 50)
				{
					Name = $"FDS Insert {i}",
					DisplayName = $"Insert {i}"
				});
			}

			var width = 20 + (diskSize * 58);
			if (width < 160)
			{
				width = 160;
			}

			return new PadSchema
			{
				DisplayName = "Console",
				IsConsole = true,
				DefaultSize = new Size(width, 100),
				Buttons = buttons
			};
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
					ButtonSchema.Up(23, 15, $"P{controller} Up"),
					ButtonSchema.Down(23, 36, $"P{controller} Down"),
					ButtonSchema.Left(2, 24, $"P{controller} Left"),
					ButtonSchema.Right(44, 24, $"P{controller} Right"),
					new ButtonSchema(124, 24)
					{
						Name = $"P{controller} B",
						DisplayName = "B"
					},
					new ButtonSchema(147, 24)
					{
						Name = $"P{controller} A",
						DisplayName = "A"
					},
					new ButtonSchema(72, 24)
					{
						Name = $"P{controller} Select",
						DisplayName = "s"
					},
					new ButtonSchema(93, 24)
					{
						Name = $"P{controller} Start",
						DisplayName = "S"
					}
				}
			};
		}

		private static PadSchema Famicom2ndController()
		{
			var controller = 2;
			return new PadSchema
			{
				DisplayName = "Player 2",
				IsConsole = false,
				DefaultSize = new Size(174, 74),
				MaxSize = new Size(174, 74),
				Buttons = new[]
				{
					ButtonSchema.Up(23, 15, $"P{controller} Up"),
					ButtonSchema.Down(23, 36, $"P{controller} Down"),
					ButtonSchema.Left(2, 24, $"P{controller} Left"),
					ButtonSchema.Right(44, 24, $"P{controller} Right"),
					new ButtonSchema(124, 24)
					{
						Name = $"P{controller} B",
						DisplayName = "B"
					},
					new ButtonSchema(147, 24)
					{
						Name = $"P{controller} A",
						DisplayName = "A"
					},
					new ButtonSchema(72, 24)
					{
						Name = $"P{controller} Microphone",
						DisplayName = "Mic"
					}
				}
			};
		}

		private static PadSchema Zapper(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Zapper",
				IsConsole = false,
				DefaultSize = new Size(356, 290),
				MaxSize = new Size(356, 290),
				Buttons = new[]
				{
					new ButtonSchema(14, 17)
					{
						Name = $"P{controller} Zapper X",
						Type = PadInputType.TargetedPair,
						TargetSize = new Size(256, 240),
						SecondaryNames = new[]
						{
							$"P{controller} Zapper Y"
						}
					},
					new ButtonSchema(284, 17)
					{
						Name = $"P{controller} Fire",
						DisplayName = "Fire"
					}
				}
			};
		}

		private static PadSchema ArkanoidPaddle(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Arkanoid Paddle",
				IsConsole = false,
				DefaultSize = new Size(380, 110),
				MaxSize = new Size(380, 110),
				Buttons = new[]
				{
					new ButtonSchema(14, 17)
					{
						Name = $"P{controller} Paddle",
						DisplayName = "Arkanoid Paddle",
						Type = PadInputType.FloatSingle,
						TargetSize = new Size(380, 69),
						MaxValue = 160
					},
					new ButtonSchema(14, 85)
					{
						Name = $"P{controller} Fire",
						DisplayName = "Fire"
					}
				}
			};
		}

		private static PadSchema PowerPad(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Power Pad",
				IsConsole = false,
				DefaultSize = new Size(154, 114),
				Buttons = new[]
				{
					new ButtonSchema(14, 17)
					{
						Name = $"P{controller} PP1",
						DisplayName = "1  "
					},
					new ButtonSchema(45, 17)
					{
						Name = $"P{controller} PP2",
						DisplayName = "2  "
					},
					new ButtonSchema(76, 17)
					{
						Name = $"P{controller} PP3",
						DisplayName = "3  "
					},
					new ButtonSchema(107, 17)
					{
						Name = $"P{controller} PP4",
						DisplayName = "4  "
					},
					new ButtonSchema(14, 48)
					{
						Name = $"P{controller} PP5",
						DisplayName = "5  "
					},
					new ButtonSchema(45, 48)
					{
						Name = $"P{controller} PP6",
						DisplayName = "6  "
					},
					new ButtonSchema(76, 48)
					{
						Name = $"P{controller} PP7",
						DisplayName = "7  "
					},
					new ButtonSchema(107, 48)
					{
						Name = $"P{controller} PP8",
						DisplayName = "8  "
					},

					new ButtonSchema(14, 79)
					{
						Name = $"P{controller} PP9",
						DisplayName = "9  "
					},
					new ButtonSchema(45, 79)
					{
						Name = $"P{controller} PP10",
						DisplayName = "10"
					},
					new ButtonSchema(76, 79)
					{
						Name = $"P{controller} PP11",
						DisplayName = "11"
					},
					new ButtonSchema(107, 79)
					{
						Name = $"P{controller} PP12",
						DisplayName = "12"
					}
				}
			};
		}

		private static PadSchema OekaKidsTablet(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Tablet",
				IsConsole = false,
				DefaultSize = new Size(356, 290),
				MaxSize = new Size(356, 290),
				Buttons = new[]
				{
					new ButtonSchema(14, 17)
					{
						Name = $"P{controller} Pen X",
						Type = PadInputType.TargetedPair,
						TargetSize = new Size(256, 240),
						SecondaryNames = new[]
						{
							$"P{controller} Pen Y"
						}
					},
					new ButtonSchema(284, 17)
					{
						Name = $"P{controller} Click",
						DisplayName = "Click"
					},
					new ButtonSchema(284, 48)
					{
						Name = $"P{controller} Touch",
						DisplayName = "Touch"
					}
				}
			};
		}

		private static PadSchema FamicomFamilyKeyboard(int controller)
		{
			return new PadSchema
			{
				DisplayName = "Family Basic Keyboard",
				IsConsole = false,
				DefaultSize = new Size(560, 180),
				Buttons = new[]
				{
					new ButtonSchema(23, 15)
					{
						Name = $"P{controller} F1",
						DisplayName = "    F1    "
					},
					new ButtonSchema(76, 15)
					{
						Name = $"P{controller} F2",
						DisplayName = "    F2    "
					},
					new ButtonSchema(129, 15)
					{
						Name = $"P{controller} F3",
						DisplayName = "    F3    "
					},
					new ButtonSchema(182, 15)
					{
						Name = $"P{controller} F4",
						DisplayName = "    F4    "
					},
					new ButtonSchema(235, 15)
					{
						Name = $"P{controller} F5",
						DisplayName = "    F5    "
					},
					new ButtonSchema(288, 15)
					{
						Name = $"P{controller} F6",
						DisplayName = "    F6    "
					},
					new ButtonSchema(341, 15)
					{
						Name = $"P{controller} F7",
						DisplayName = "    F7    "
					},
					new ButtonSchema(394, 15)
					{
						Name = $"P{controller} F8",
						DisplayName = "    F8    "
					},
					new ButtonSchema(36, 38)
					{
						Name = $"P{controller} 1",
						DisplayName = "1"
					},
					new ButtonSchema(60, 38)
					{
						Name = $"P{controller} 2",
						DisplayName = "2"
					},
					new ButtonSchema(84, 38)
					{
						Name = $"P{controller} 3",
						DisplayName = "3"
					},
					new ButtonSchema(108, 38)
					{
						Name = $"P{controller} 4",
						DisplayName = "4"
					},
					new ButtonSchema(132, 38)
					{
						Name = $"P{controller} 5",
						DisplayName = "5"
					},
					new ButtonSchema(156, 38)
					{
						Name = $"P{controller} 6",
						DisplayName = "6"
					},
					new ButtonSchema(180, 38)
					{
						Name = $"P{controller} 7",
						DisplayName = "7"
					},
					new ButtonSchema(204, 38)
					{
						Name = $"P{controller} 8",
						DisplayName = "8"
					},
					new ButtonSchema(228, 38)
					{
						Name = $"P{controller} 9",
						DisplayName = "9"
					},
					new ButtonSchema(252, 38)
					{
						Name = $"P{controller} 0",
						DisplayName = "0"
					},
					new ButtonSchema(276, 38)
					{
						Name = $"P{controller} Minus",
						DisplayName = "-"
					},
					new ButtonSchema(296, 38)
					{
						Name = $"P{controller} Caret",
						DisplayName = "^"
					},
					new ButtonSchema(320, 38)
					{
						Name = $"P{controller} Yen",
						DisplayName = "¥"
					},
					new ButtonSchema(344, 38)
					{
						Name = $"P{controller} Stop",
						DisplayName = "STOP"
					},
					new ButtonSchema(15, 61)
					{
						Name = $"P{controller} Escape",
						DisplayName = "ESC"
					},
					new ButtonSchema(54, 61)
					{
						Name = $"P{controller} Q",
						DisplayName = "Q"
					},
					new ButtonSchema(80, 61)
					{
						Name = $"P{controller} W",
						DisplayName = "W"
					},
					new ButtonSchema(108, 61)
					{
						Name = $"P{controller} E",
						DisplayName = "E"
					},
					new ButtonSchema(132, 61)
					{
						Name = $"P{controller} R",
						DisplayName = "R"
					},
					new ButtonSchema(158, 61)
					{
						Name = $"P{controller} T",
						DisplayName = "T"
					},
					new ButtonSchema(182, 61)
					{
						Name = $"P{controller} Y",
						DisplayName = "Y"
					},
					new ButtonSchema(206, 61)
					{
						Name = $"P{controller} U",
						DisplayName = "U"
					},
					new ButtonSchema(232, 61)
					{
						Name = $"P{controller} I",
						DisplayName = "I"
					},
					new ButtonSchema(252, 61)
					{
						Name = $"P{controller} O",
						DisplayName = "O"
					},
					new ButtonSchema(278, 61)
					{
						Name = $"P{controller} P",
						DisplayName = "P"
					},
					new ButtonSchema(302, 61)
					{
						Name = $"P{controller} At",
						DisplayName = "@"
					},
					new ButtonSchema(330, 61)
					{
						Name = $"P{controller} Left Bracket",
						DisplayName = "["
					},
					new ButtonSchema(350, 61)
					{
						Name = $"P{controller} Return",
						DisplayName = "RETURN"
					},
					new ButtonSchema(30, 84)
					{
						Name = $"P{controller} Control",
						DisplayName = "CTR"
					},
					new ButtonSchema(70, 84)
					{
						Name = $"P{controller} A",
						DisplayName = "A"
					},
					new ButtonSchema(94, 84)
					{
						Name = $"P{controller} S",
						DisplayName = "S"
					},
					new ButtonSchema(118, 84)
					{
						Name = $"P{controller} D",
						DisplayName = "D"
					},
					new ButtonSchema(144, 84)
					{
						Name = $"P{controller} F",
						DisplayName = "F"
					},
					new ButtonSchema(168, 84)
					{
						Name = $"P{controller} G",
						DisplayName = "G"
					},
					new ButtonSchema(194, 84)
					{
						Name = $"P{controller} H",
						DisplayName = "H"
					},
					new ButtonSchema(220, 84)
					{
						Name = $"P{controller} J",
						DisplayName = "J"
					},
					new ButtonSchema(242, 84)
					{
						Name = $"P{controller} K",
						DisplayName = "K"
					},
					new ButtonSchema(266, 84)
					{
						Name = $"P{controller} L",
						DisplayName = "L"
					},
					new ButtonSchema(290, 84)
					{
						Name = $"P{controller} Semicolon",
						DisplayName = ";"
					},
					new ButtonSchema(311, 84)
					{
						Name = $"P{controller} Colon",
						DisplayName = ":",
						Location = new Point(311, 84)
					},
					new ButtonSchema(332, 84)
					{
						Name = $"P{controller} Right Bracket",
						DisplayName = "]"
					},
					new ButtonSchema(352, 84)
					{
						Name = $"P{controller} カナ",
						DisplayName = "カナ"
					},
					new ButtonSchema(10, 107)
					{
						Name = $"P{controller} Left Shift",
						DisplayName = "SHIFT"
					},
					new ButtonSchema(58, 107)
					{
						Name = $"P{controller} Z",
						DisplayName = "Z"
					},
					new ButtonSchema(82, 107)
					{
						Name = $"P{controller} X",
						DisplayName = "X"
					},
					new ButtonSchema(106, 107)
					{
						Name = $"P{controller} C",
						DisplayName = "C"
					},
					new ButtonSchema(130, 107)
					{
						Name = $"P{controller} V",
						DisplayName = "V"
					},
					new ButtonSchema(154, 107)
					{
						Name = $"P{controller} B",
						DisplayName = "B"
					},
					new ButtonSchema(178, 107)
					{
						Name = $"P{controller} N",
						DisplayName = "N"
					},
					new ButtonSchema(203, 107)
					{
						Name = $"P{controller} M",
						DisplayName = "M"
					},
					new ButtonSchema(229, 107)
					{
						Name = $"P{controller} Comma",
						DisplayName = ","
					},
					new ButtonSchema(249, 107)
					{
						Name = $"P{controller} Period",
						DisplayName = "."
					},
					new ButtonSchema(270, 107)
					{
						Name = $"P{controller} Slash",
						DisplayName = "/"
					},
					new ButtonSchema(292, 107)
					{
						Name = $"P{controller} Underscore",
						DisplayName = "_"
					},
					new ButtonSchema(316, 107)
					{
						Name = $"P{controller} Right Shift",
						DisplayName = "SHIFT"
					},
					new ButtonSchema(82, 130)
					{
						Name = $"P{controller} Graph",
						DisplayName = "GRPH"
					},
					new ButtonSchema(130, 130)
					{
						Name = $"P{controller} Space",
						DisplayName = "                  SPACE                  "
					},
					new ButtonSchema(420, 46)
					{
						Name = $"P{controller} Clear/Home",
						DisplayName = " CLR\nHOME"
					},
					new ButtonSchema(470, 46)
					{
						Name = $"P{controller} Insert",
						DisplayName = "\nINS"
					},
					new ButtonSchema(506, 46)
					{
						Name = $"P{controller} Delete",
						DisplayName = "\nDEL"
					},
					new ButtonSchema(468, 86)
					{
						Name = $"P{controller} Up",
						DisplayName = "  ↑  "
					},
					new ButtonSchema(468, 134)
					{
						Name = $"P{controller} Down",
						DisplayName = "  ↓  "
					},
					new ButtonSchema(446, 110)
					{
						Name = $"P{controller} Left",
						DisplayName = "  ←  "
					},
					new ButtonSchema(488, 110)
					{
						Name = $"P{controller} Right",
						DisplayName = "  ➝  "
					}
				}
			};
		}
	}
}
