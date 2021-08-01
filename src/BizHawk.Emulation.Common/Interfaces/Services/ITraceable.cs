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
		ITraceSink? Sink { get; set; }
	}

	public readonly struct TraceInfo
	{
		public readonly string Disassembly;

		public readonly string RegisterInfo;

		public TraceInfo(string disassembly, string registerInfo)
		{
			Disassembly = disassembly;
			RegisterInfo = registerInfo;
		}
	}
}
