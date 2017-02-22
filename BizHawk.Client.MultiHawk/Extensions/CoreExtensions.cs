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
			var attributes = core.Attributes();

			var str = (!attributes.Released ? "(Experimental) " : string.Empty) +
				attributes.CoreName;

			if (core is LibsnesCore)
			{
				str += " (" + ((LibsnesCore)core).CurrentProfile + ")";
			}

			return str;
		}
	}
}
