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
					"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 B1", "P2 B2"
				}
		};
	}
}