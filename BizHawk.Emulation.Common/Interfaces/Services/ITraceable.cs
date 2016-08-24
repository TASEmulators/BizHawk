using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	public interface ITraceSink
	{
		void Put(TraceInfo info);
	}

	/// <summary>
	/// This service allows the core to dump a cpu trace to the client
	/// If available the Trace Logger tool will be available on the client
	/// </summary>
	public interface ITraceable : IEmulatorService
	{
		//bool Enabled { get; set; }

		/// <summary>
		/// The header that would be used by a trace logger
		/// </summary>
		string Header { get; set; }

		/// <summary>
		/// The current log of cpu instructions
		/// </summary>
		//IEnumerable<TraceInfo> Contents { get; }

		/// <summary>
		/// Takes the current log of cpu instructions, when doing so, it will clear the contents from the buffer
		/// </summary>
		//IEnumerable<TraceInfo> TakeContents();

		//void Put(TraceInfo content);

		//that's right, we can only have one sink.
		//a sink can route to two other sinks if it has to, though
		ITraceSink Sink { get; set; }

		/// <summary>
		/// This is defined as equivalent to Sink != null
		/// It's put here because it's such a common operation to check whether it's enabled, and it's not nice to write Sink != null all over
		/// </summary>
		bool Enabled { get; }

		/// <summary>
		/// This is defined as equivalent to Sink.Put
		/// TBD: could it be defined as equivalent to if(Enabled) Sink.Put()? Probably not, that's just a small amount of wasted work
		/// </summary>
		void Put(TraceInfo info);
	}

	public class TraceInfo
	{
		public string Disassembly { get; set; }
		public string RegisterInfo { get; set; }
	}
}
