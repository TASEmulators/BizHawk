#nullable enable

using BizHawk.Common;

namespace BizHawk.Bizware.Input
{
	internal static class KeyMouseInputFactory
	{
		public static IKeyMouseInput CreateKeyMouseInput() => OSTailoredCode.CurrentOS switch
		{
			OSTailoredCode.DistinctOS.Linux => new X11KeyMouseInput(),
			// macOS EmuHawk runs under XQuartz (Mono WinForms is X11), so read the keyboard/mouse
			// through X11 like Linux. The Quartz path (CGEventSourceKeyState) needs Accessibility
			// permission and Mac HID keycodes, and doesn't integrate with the XQuartz window.
			OSTailoredCode.DistinctOS.macOS => new X11KeyMouseInput(),
			OSTailoredCode.DistinctOS.Windows => new RawKeyMouseInput(),
			_ => throw new InvalidOperationException("Unknown OS"),
		};
	}
}
