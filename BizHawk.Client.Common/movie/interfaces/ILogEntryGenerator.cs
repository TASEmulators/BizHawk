using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface ILogEntryGenerator
	{
		/// <summary>
		/// Sets the controller source used to generate an input log entry
		/// </summary>
		/// <param name="source"></param>
		void SetSource(IController source);

		/// <summary>
		/// Generates an input log entry for the current state of Source
		/// </summary>
		/// <returns></returns>
		string GenerateLogEntry();

		/// <summary>
		/// Generates a display friendly verion of the input log entry
		/// </summary>
		/// <returns></returns>
		string GenerateInputDisplay();

		/// <summary>
		/// Returns whether or not the current controller state is "empty"
		/// </summary>
		bool IsEmpty { get; }

		/// <summary>
		/// Returns an input log entry that is considered empty. (booleans will be false, floats will be 0)
		/// </summary>
		string EmptyEntry { get; }

		/// <summary>
		/// Returns a movie controller adapter in the same state 
		/// </summary>
		IMovieController MovieControllerAdapter { get; }
	}
}
