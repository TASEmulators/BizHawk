using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class Bk2LogEntryGenerator : ILogEntryGenerator
	{
		private IController _source;

		public IMovieController MovieControllerAdapter
		{
			get { return new Bk2ControllerAdapter(); }
		}

		public void SetSource(IController source)
		{
			_source = source;
		}

		public string GenerateInputDisplay()
		{
			return GenerateLogEntry()
				.Replace(".", " ")
				.Replace("|", "")
				.Replace(" 000, 000", "         ");
		}

		public bool IsEmpty
		{
			get
			{
				return EmptyEntry == GenerateLogEntry();
			}
		}

		public string EmptyEntry
		{
			get
			{
				return CreateLogEntry(createEmpty: true);
			}
		}

		public string GenerateLogEntry()
		{
			return CreateLogEntry();
		}

		private string CreateLogEntry(bool createEmpty = false)
		{
			var sb = new StringBuilder();
			sb.Append('|');

			foreach (var button in _source.Type.BoolButtons)
			{
				if (createEmpty)
				{
					sb.Append('.');
				}
				else
				{
					sb.Append(_source.IsPressed(button) ? Mnemonics[button] : '.');
				}
			}

			if (_source.Type.FloatControls.Any())
			{
				foreach (var floatBtn in _source.Type.FloatControls)
				{
					if (createEmpty)
					{
						sb.Append(" 000,");
					}
					else
					{
						var val = (int)_source.GetFloat(floatBtn);
						sb.Append(' ').Append(val).Append(',');
					}
				}

				sb.Remove(sb.Length - 1, 1);
			}

			sb.Append('|');
			return sb.ToString();
		}

		private readonly Bk2MnemonicsLookup Mnemonics = new Bk2MnemonicsLookup();

		public class Bk2MnemonicsLookup
		{
			public char this[string button]
			{
				get
				{
					if (SystemOverrides.ContainsKey(Global.Emulator.SystemId) && SystemOverrides[Global.Emulator.SystemId].ContainsKey(button))
					{
						return SystemOverrides[Global.Emulator.SystemId][button];
					}
					else if (BaseMnemonicLookupTable.ContainsKey(button))
					{
						return BaseMnemonicLookupTable[button];
					}

					return '!';
				}
			}

			private readonly Dictionary<string, char> BaseMnemonicLookupTable = new Dictionary<string, char>
			{
				{ "P1 Up", 'U' },
				{ "P1 Down", 'D' },
				{ "P1 Left", 'L' },
				{ "P1 Right", 'R' },
				{ "P1 B", 'B' },
				{ "P1 A", 'A' },
				{ "P1 Select", 's' },
				{ "P1 Start", 'S' },

				{ "P2 Up", 'U' },
				{ "P2 Down", 'D' },
				{ "P2 Left", 'L' },
				{ "P2 Right", 'R' },
				{ "P2 B", 'B' },
				{ "P2 A", 'A' },
				{ "P2 Select", 's' },
				{ "P2 Start", 'S' },
			};

			private readonly Dictionary<string, Dictionary<string, char>> SystemOverrides = new Dictionary<string, Dictionary<string, char>>
			{
				{
					"NES",
					new Dictionary<string, char>
					{
						{ "P1 Up", 'Q' }
					}
				}
			};
		}
	}
}
