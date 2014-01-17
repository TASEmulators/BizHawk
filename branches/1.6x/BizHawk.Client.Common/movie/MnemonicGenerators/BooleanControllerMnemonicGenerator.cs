using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class BooleanControllerMnemonicGenerator : IMnemonicGenerator
	{
		private readonly NamedDictionary<string, char> _controllerMnemonics;

		public BooleanControllerMnemonicGenerator(string name, IEnumerable<KeyValuePair<string, char>> mnemonics)
		{
			_controllerMnemonics = new NamedDictionary<string, char>(name);
			foreach (var kvp in mnemonics)
			{
				_controllerMnemonics.Add(kvp.Key, kvp.Value);
			}
		}

		public void Add(string key, char value)
		{
			_controllerMnemonics.Add(key, value);
		}

		public Dictionary<string, char> AvailableMnemonics
		{
			get
			{
				return _controllerMnemonics.ToDictionary(kvp => ControllerPrefix + " " + kvp.Key, kvp => kvp.Value);
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

			for (var i = 0; i < mnemonicSegment.Length; i++)
			{
				buttons.Add(keys[i], mnemonicSegment[i] != '.');
			}
			
			return buttons;
		}
	}
}
