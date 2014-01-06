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

		/// <summary>
		/// The
		/// </summary>
		string ControllerPrefix { get; set; }
		char this[string key] { get; }
		bool IsEmpty { get; }
		string MnemonicString { get; }

		/// <summary>
		/// Returns a string that represents an empty or default mnemonic 
		/// </summary>
		string EmptyMnemonicString { get; }

		/// <summary>
		/// Parses a segment of a full mnemonic string (the content between pipes)
		/// Note: this assume the pipes are not being passed in!
		/// </summary>
		IDictionary<string, bool> ParseMnemonicSegment(string mnemonicSegment);
	}

	public interface IMnemonicGeneratorCollection
	{
		IEnumerable<IMnemonicGenerator> Generators { get; }
	}

	public class BooleanControllerMnemonicGenerator : IMnemonicGenerator
	{
		private NamedDictionary<string, char> _controllerMnemonics;

		public BooleanControllerMnemonicGenerator(string name)
		{
			_controllerMnemonics = new NamedDictionary<string, char>(name);
		}

		public IController Source { get; set; }
		public string ControllerPrefix { get; set; }

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
