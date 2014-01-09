using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMnemonicGenerator
	{
		IController Source { get; set;  }

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

	/// <summary>
	/// A console specific collection of Mnemonic generators
	/// This handles includes all the "business" logic specific to the console
	/// </summary>
	public interface IMnemonicPorts
	{
		/// <summary>
		/// Total number of available controller ports (this does not include the console controls
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Source controller to read input state from
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

	public class BooleanControllerMnemonicGenerator : IMnemonicGenerator
	{
		private NamedDictionary<string, char> _controllerMnemonics;

		public BooleanControllerMnemonicGenerator(string name, IDictionary<string, char> mnemonics)
		{
			_controllerMnemonics = new NamedDictionary<string, char>(name);
		}

		public void Add(string key, char value)
		{
			_controllerMnemonics.Add(key, value);
		}

		public Dictionary<string, char> AvailableMnemonics
		{
			get
			{
				return _controllerMnemonics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
			}
		}

		public IController Source { get; set; }
		public string ControllerPrefix { get; set; }
		public string Name
		{
			get { return _controllerMnemonics.Name; }
		}

		public char this[string key]
		{
			get
			{
				return _controllerMnemonics[ControllerPrefix + " " + key];
			}
		}

		public bool IsEmpty
		{
			get
			{
				return _controllerMnemonics.All(kvp => !this.Source.IsPressed(kvp.Key));
			}
		}

		public string MnemonicString
		{
			get
			{
				var sb = new StringBuilder(_controllerMnemonics.Count);
				foreach (var kvp in _controllerMnemonics)
				{
					sb.Append(Source.IsPressed(kvp.Key) ? kvp.Value : '.');
				}

				return sb.ToString();
			}
		}

		public string EmptyMnemonicString
		{
			get
			{
				var sb = new StringBuilder(_controllerMnemonics.Count);
				foreach (var kvp in _controllerMnemonics)
				{
					sb.Append('.');
				}

				return sb.ToString();
			}
		}

		public IDictionary<string, bool> ParseMnemonicSegment(string mnemonicSegment)
		{
			var buttons = new Dictionary<string, bool>();
			var keys = _controllerMnemonics.Select(kvp => kvp.Key).ToList();

			for (int i = 0; i < mnemonicSegment.Length; i++)
			{
				buttons.Add(keys[i], mnemonicSegment[i] != '.');
			}
			
			return buttons;
		}
	}
}
