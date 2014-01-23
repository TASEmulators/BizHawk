using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// A console specific collection of Mnemonic generators
	/// This handles includes all the "business" logic specific to the console
	/// </summary>
	public interface IMnemonicPorts
	{
		/// <summary>
		/// Gets the total number of available controller ports (this does not include the console controls
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Gets or sets the Source controller to read the input state from
		/// </summary>
		IController Source { get; set; }

		/// <summary>
		/// Gets or sets the given port with an IMnemonicGenerator implementation
		/// Ports are zero based
		/// Set will throw an InvalidOperationException if a particular implementation is not allowed, this is platform specific logic such as NES doesn't allow a zapper in port 0, etc
		/// Both will throw an ArgumentOutOfRangeException exception if portNum is not less than Count
		/// </summary>
		IMnemonicGenerator this[int portNum] { get; set; }

		/// <summary>
		/// Gets an IMnemonicGenerator implementation that represents the buttons and controls on the console itself (Reset, Power, etc)
		/// </summary>
		IMnemonicGenerator ConsoleControls { get; }

		Dictionary<string, bool> ParseMnemonicString(string mnemonicStr);

		// Analog TODO: this assume the generators are boolean
		Dictionary<string, bool> GetBoolButtons();

		// TODO: this shouldn't be required, refactor MovieRecord
		string GenerateMnemonicString(Dictionary<string, bool> buttons);

		string EmptyMnemonic { get; }

		Dictionary<string, char> AvailableMnemonics { get; }
	}
}
