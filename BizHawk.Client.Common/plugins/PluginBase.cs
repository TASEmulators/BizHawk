using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public abstract class PluginBase : IPlugin
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[RequiredService]
		private IMemoryDomains Domains { get; set; }

		[OptionalService]
		private IInputPollable InputPollableCore { get; set; }

		[OptionalService]
		private IDebuggable DebuggableCore { get; set; }
	}
}
