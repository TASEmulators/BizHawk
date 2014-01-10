using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMnemonicGenerator
	{
		IController Source { get; set; }

		string Name { get; }

		/// <summary>
		/// Will be prepended to all button names
		/// Example: "P1"
		/// </summary>
		string ControllerPrefix { get; set; }

		void Add(string key, char value);

		char this[string key] { get; }
		bool IsEmpty { get; }
		string MnemonicString { get; }

		/// <summary>
		/// Returns a string that represents an empty or default mnemonic 
		/// </summary>
		string EmptyMnemonicString { get; }

		// Analog TODO: this assumes the Generator is boolean
		/// <summary>
		/// Parses a segment of a full mnemonic string (the content between pipes)
		/// Note: this assume the pipes are not being passed in!
		/// </summary>
		IDictionary<string, bool> ParseMnemonicSegment(string mnemonicSegment);

		// Analog Support TODO: this assume the Generator is boolean
		//Dictionary<string, bool> GetBoolButtons();

		Dictionary<string, char> AvailableMnemonics { get; }
	}
}
