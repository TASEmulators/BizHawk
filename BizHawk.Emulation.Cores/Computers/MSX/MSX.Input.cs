using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	public partial class MSX
	{

		public static readonly ControllerDefinition GGController = new ControllerDefinition
		{
			Name = "GG Controller",
			BoolButtons =
				{
					"Reset",
					"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 B1", "P1 B2", "P1 Start"
				}
		};
	}
}