using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	public partial class MSX
	{

		public static readonly ControllerDefinition MSXController = new ControllerDefinition
		{
			Name = "MSX Controller",
			BoolButtons =
				{
					"Reset",
					"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 B1", "P1 B2",
					"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 B1", "P2 B2",
					"7", "6", "5", "4", "3", "2", "1", "0",
					";", "[", "@", "$", "^", "-", "9", "8",
					"B", "A",      "/", ".", ",", "]", ":",
					"J", "I", "H", "G", "F", "E", "D", "C",
					"R", "Q", "P", "O", "N", "M", "L", "K",
					"Z", "Y", "X", "W", "V", "U", "T", "S",
					"F3", "F2", "F1", "KANA", "CAP", "GRAPH", "CTRL", "SHIFT",
					"RET", "SEL", "BACK", "STOP", "TAB", "ESC", "F5", "F4",
					"RIGHT", "DOWN", "UP", "LEFT", "DEL", "INS", "HOME", "SPACE"
				}
		};
	}
}