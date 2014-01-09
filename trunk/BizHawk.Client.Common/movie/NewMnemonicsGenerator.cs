using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	// Used with the version 2 movie implementation (TasMovie.cs)
	/*
	public class NewMnemonicsGenerator
	{
		public MnemonicLookupTable MnemonicLookup { get; private set; }
		public IController Source { get; set; }

		public List<string> ActivePlayers { get; set; }

		public NewMnemonicsGenerator(IController source)
		{
			MnemonicLookup = new MnemonicLookupTable();
			Source = source;
			ActivePlayers = MnemonicLookup[Global.Emulator.SystemId].Select(x => x.Name).ToList();
		}

		public bool IsEmpty
		{
			get
			{
				return ActiveCollections
					.SelectMany(mc => mc)
					.All(kvp => !this.Source.IsPressed(kvp.Key));
			}
		}

		public string EmptyMnemonic
		{
			get
			{
				var sb = new StringBuilder();

				sb.Append('|');
				foreach (var mc in ActiveCollections)
				{
					foreach (var kvp in mc)
					{
						sb.Append('.');
					}

					sb.Append('|');
				}

				return sb.ToString();
			}
		}

		public Dictionary<string, bool> ParseMnemonicString(string mnemonic)
		{
			// TODo: clean me up
			var buttons = new Dictionary<string, bool>(); 
			var collections = AllCollections.SelectMany(mc => mc).ToList();
			var availMnemonics = AvailableMnemonics;
			List<char> mnemonicList = mnemonic.Replace("|", "").ToCharArray().ToList();

			for (int i = 0; i < collections.Count; i++)
			{
				if (availMnemonics.ContainsKey(collections[i].Key))
				{
					buttons.Add(collections[i].Key, mnemonicList[i] != '.');
				}
			}

			return buttons;
		}

		public string GenerateMnemonicString(Dictionary<string, bool> buttons)
		{
			var collections = MnemonicLookup[Global.Emulator.SystemId].Where(x => ActivePlayers.Contains(x.Name));
			var sb = new StringBuilder();

			sb.Append('|');
			foreach (var mc in collections)
			{
				foreach (var kvp in mc.Where(kvp => buttons.ContainsKey(kvp.Key)))
				{
					sb.Append(buttons[kvp.Key] ? kvp.Value : '.');
				}

				sb.Append('|');
			}
			return sb.ToString();
		}

		public string MnemonicString
		{
			get
			{
				var sb = new StringBuilder();
				sb.Append('|');
				foreach (var mc in ActiveCollections)
				{
					foreach (var kvp in mc)
					{
						sb.Append(Source.IsPressed(kvp.Key) ? kvp.Value : '.');
					}

					sb.Append('|');
				}

				return sb.ToString();
			}
		}

		public IEnumerable<char> Mnemonics
		{
			get
			{
				var mnemonics = new List<char>();
				foreach (var mc in ActiveCollections)
				{
					mnemonics.AddRange(mc.Select(x => x.Value));
				}

				return mnemonics;
			}
		}

		public Dictionary<string, char> AvailableMnemonics
		{
			get
			{
				return ActiveCollections
					.SelectMany(mc => mc)
					.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
			}
		}

		public Dictionary<string, bool> GetBoolButtons()
		{
			return ActiveCollections
				.SelectMany(mc => mc)
				.ToDictionary(kvp => kvp.Key, kvp => this.Source.IsPressed(kvp.Key));
		}

		private IEnumerable<MnemonicCollection> ActiveCollections
		{
			get
			{
				return MnemonicLookup[Global.Emulator.SystemId].Where(x => ActivePlayers.Contains(x.Name));
			}
		}

		private IEnumerable<MnemonicCollection> AllCollections
		{
			get
			{
				return MnemonicLookup[Global.Emulator.SystemId];
			}
		}
	}
	*/
}
