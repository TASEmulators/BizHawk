#nullable enable

using BizHawk.Common;

namespace BizHawk.Bizware.Input
{
	internal static class KeyInputFactory
	{
		public static IKeyInput CreateKeyInput() => OSTailoredCode.CurrentOS switch
		{
			OSTailoredCode.DistinctOS.Linux => new X11KeyInput(),
			OSTailoredCode.DistinctOS.macOS => new QuartzKeyInput(),
			OSTailoredCode.DistinctOS.Windows => new RawKeyInput(),
			_ => throw new InvalidOperationException("Unknown OS"),
		};
	}
}
