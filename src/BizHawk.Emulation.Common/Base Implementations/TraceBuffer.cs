namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A generic implementation of ITraceable that can be used by any core
	/// </summary>
	/// <seealso cref="ITraceable" />
	public class TraceBuffer : ITraceable
	{
		private const string DEFAULT_HEADER = "Instructions";

		public string Header { get; }

		public ITraceSink? Sink { get; set; }

		public TraceBuffer(string header = DEFAULT_HEADER) => Header = header;
	}
}
