using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Client.Common;

namespace BizHawk.Client.MultiHawk.CoreExtensions
{
	public static class CoreExtensions
	{
		public static string DisplayName(this IEmulator core)
		{
			var attributes = Global.Emulator.Attributes();

			var str = (!attributes.Released ? "(Experimental) " : string.Empty) +
				attributes.CoreName;

			if (Global.Emulator is LibsnesCore)
			{
				str += " (" + ((LibsnesCore)Global.Emulator).CurrentProfile + ")";
			}

			return str;
		}
	}
}
