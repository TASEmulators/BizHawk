using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface ILogEntryGenerator
	{
		/// <summary>
		/// Sets the controller source used to generate an input log entry
		/// </summary>
		void SetSource(IController source);

		/// <summary>
		/// Generates an input log entry for the current state of Source
		/// </summary>
		string GenerateLogEntry();

		/// <summary>
		/// Generates a human readable key that will specify the names of the
		/// buttons and the order they are in. This is intended to simply be
		/// documentation of the meaning of the mnemonics and not to be used to
		/// enforce the mnemonic values
		/// </summary>
		string GenerateLogKey();

		/// <summary>
		/// Generates a dictionary of button names to their corresponding mnemonic values
		/// </summary>
		Dictionary<string, string> Map();

		/// <summary>
		/// Generates a display friendly version of the input log entry
		/// </summary>
		string GenerateInputDisplay();

		/// <summary>
		/// Gets a value indicating whether or not the current controller state is "empty"
		/// </summary>
		bool IsEmpty { get; }

		/// <summary>
		/// Gets an input log entry that is considered empty. (booleans will be false, floats will be 0)
		/// </summary>
		string EmptyEntry { get; }

		/// <summary>
		/// Gets a movie controller adapter in the same state as the log entry
		/// </summary>
		/// <seealso cref="IMovieController"/>
		IMovieController MovieControllerAdapter { get; }
	}
}
