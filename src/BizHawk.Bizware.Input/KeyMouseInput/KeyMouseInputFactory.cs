#nullable enable

using BizHawk.Common;

namespace BizHawk.Bizware.Input
{
	internal static class KeyMouseInputFactory
	{
		public static IKeyMouseInput CreateKeyMouseInput() => OSTailoredCode.CurrentOS switch
		{
			OSTailoredCode.DistinctOS.Linux => new X11KeyMouseInput(),
			OSTailoredCode.DistinctOS.macOS => new QuartzKeyMouseInput(),
			OSTailoredCode.DistinctOS.Windows => new RawKeyMouseInput(),
			_ => throw new InvalidOperationException("Unknown OS"),
		};
	}
}
