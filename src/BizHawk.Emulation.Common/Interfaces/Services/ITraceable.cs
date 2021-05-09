namespace BizHawk.Emulation.Common
{
	public interface ITraceSink
	{
		void Put(TraceInfo info);
	}

	/// <summary>
	/// This service allows the core to dump a CPU trace to the client
	/// If available the Trace Logger tool will be available on the client
	/// </summary>
	public interface ITraceable : IEmulatorService
	{
		/// <summary>
		/// Gets the header that would be used by a trace logger
		/// </summary>
		string Header { get; }

		/// <summary>
		/// Sets the sink
		/// that's right, we can only have one sink.
		/// a sink can route to two other sinks if it has to, though
		/// </summary>
		ITraceSink Sink { set; }

		/// <summary>
		/// Gets a value indicating whether tracing is enabled
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
