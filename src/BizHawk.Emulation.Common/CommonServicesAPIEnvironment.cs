#nullable enable

using System;

using BizHawk.API.Base;

namespace BizHawk.Emulation.Common
{
	public class CommonServicesAPIEnvironment : APIEnvironment
	{
		[OptionalService]
		public IBoardInfo? BoardInfo { get; set; }

		[OptionalService]
		public IDebuggable? DebuggableCore { get; set; }

		[OptionalService]
		public IDisassemblable? DisassemblableCore { get; set; }

		[OptionalService]
		public IEmulator? EmuCore { get; set; }

		[OptionalService]
		public IInputPollable? InputPollableCore { get; set; }

		[OptionalService]
		public IMemoryDomains? MemoryDomains { get; set; }

		[OptionalService]
		public IRegionable? RegionableCore { get; set; }

		[OptionalService]
		public IStatable? StatableCore { get; set; }

		public CommonServicesAPIEnvironment(
			Action<string> logCallback,
			HistoricAPIEnvironment last,
			out HistoricAPIEnvironment keep
		) : base(
			logCallback,
			last,
			out keep
		) {}
	}
}
