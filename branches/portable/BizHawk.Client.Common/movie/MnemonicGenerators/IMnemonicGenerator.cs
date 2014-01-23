using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMnemonicGenerator
	{
		IController Source { get; set; }

		string Name { get; }

		/// <summary>
		/// Gets or sets the prefix that will be prepended to all button names
		/// Example: "P1" would combine with "Up" to make "P1 Up"
		/// </summary>
		string ControllerPrefix { get; set; }

		void Add(string key, char value);

		char this[string key] { get; }
		bool IsEmpty { get; }
		string MnemonicString { get; }

		/// <summary>
		/// Gets a string that represents an empty or default mnemonic
		/// </summary>
		string EmptyMnemonicString { get; }

		// Analog TODO: this assumes the Generator is boolean
		/// <summary>
		/// Parses a segment of a full mnemonic string (the content between pipes)
		/// Note: this assume the pipes are not being passed in!
		/// </summary>
		IDictionary<string, bool> ParseMnemonicSegment(string mnemonicSegment);

		Dictionary<string, char> AvailableMnemonics { get; }
	}
}
