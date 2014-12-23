namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Allows the cpu to dump trace info to a trace stream
	/// </summary>
	public interface ITraceable : IEmulatorService
	{
		// TODO: would it be faster (considering both disk and screen output) to keep the data as a List<string> directly?

		bool Enabled { get; set; }

		/// <summary>
		/// The header that would be used by a trace logger
		/// </summary>
		string Header { get; set; }

		/// <summary>
		/// The current log of cpu instructions
		/// </summary>
		string Contents { get; }

		/// <summary>
		/// Takes the current log of cpu instructions, when doing so, it will clear the contents from the buffer
		/// </summary>
		string TakeContents();

		/// <summary>
		/// Adds contents to the log of cpu instructions
		/// </summary>
		void Put(string content);
	}
}
