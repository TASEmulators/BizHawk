using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	// Lot's of todos here
	public static class MnemonicGeneratorFactory
	{
		public static IMnemonicPorts Generate()
		{
			switch (Global.Emulator.SystemId)
			{
				default:
				case "NES":
					return new NesMnemonicGenerator(Global.MovieOutputHardpoint, false, false);
			}
		}
	}
}
