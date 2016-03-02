using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service allows the core to dump a cpu trace to the client
	/// If available the Trace Logger tool will be available on the client
	/// </summary>
	public interface ITraceable : IEmulatorService
	{
		bool Enabled { get; set; }

		/// <summary>
		/// The header that would be used by a trace logger
		/// </summary>
		string Header { get; set; }

		/// <summary>
		/// The current log of cpu instructions
		/// </summary>
		IEnumerable<TraceInfo> Contents { get; }

		/// <summary>
		/// Takes the current log of cpu instructions, when doing so, it will clear the contents from the buffer
		/// </summary>
		IEnumerable<TraceInfo> TakeContents();

		void Put(TraceInfo content);
	}

	public class TraceInfo
	{
		public string Disassembly { get; set; }
		public string RegisterInfo { get; set; }
	}
}
