using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public class MnemonicCollection : NamedDictionary<string, char>
	{
		public MnemonicCollection(string name)
			: base(name)
		{
			
		}
	}

	public class CoreMnemonicCollection : List<MnemonicCollection>
	{
		private readonly List<string> _systemIds;

		public CoreMnemonicCollection(string systemId)
		{
			_systemIds = new List<string>
			{
				systemId
			};
		}

		public CoreMnemonicCollection(string[] systemIds)
		{
			_systemIds = systemIds.ToList();
		}

		public MnemonicCollection this[string name]
		{
			get
			{
				return this.FirstOrDefault(x => x.Name == name);
			}
		}

		public IEnumerable<string> SystemIds
		{
			get { return _systemIds; }
		}
	}

	public class MnemonicLookupTable
	{
		private List<CoreMnemonicCollection> _list;

		public CoreMnemonicCollection this[string systemId]
		{
			get
			{
				return _list.FirstOrDefault(core => core.SystemIds.Contains(systemId));
			}
		}

		public MnemonicLookupTable()
		{
			_list = new List<CoreMnemonicCollection>
			{
				new CoreMnemonicCollection(new []{ "NES", "FDS" })
				{
					new MnemonicCollection("Console")
					{
						{ "Reset", 'r' },
						{ "Power", 'P' },
						{ "FDS Eject", 'E' },
						{ "FDS Insert 0", '0' },
						{ "FDS Insert 1", '1' },
						{ "VS Coin 1", 'c' },
						{ "VS Coin 2", 'C' }
					},
					new MnemonicCollection("Player 1")
					{
						{ "P1 Up", 'U' },
						{ "P1 Down", 'D' },
						{ "P1 Left", 'L' },
						{ "P1 Right", 'R' },
						{ "P1 Select", 's' },
						{ "P1 Start", 'S' },
						{ "P1 B", 'B' },
						{ "P1 A", 'A' }
					},
					new MnemonicCollection("Player 2")
					{
						{ "P2 Up", 'U' },
						{ "P2 Down", 'D' },
						{ "P2 Left", 'L' },
						{ "P2 Right", 'R' },
						{ "P2 Select", 's' },
						{ "P2 Start", 'S' },
						{ "P2 B", 'B' },
						{ "P2 A", 'A' }
					},
					new MnemonicCollection("Player 3")
					{
						{ "P3 Up", 'U' },
						{ "P3 Down", 'D' },
						{ "P3 Left", 'L' },
						{ "P3 Right", 'R' },
						{ "P3 Select", 's' },
						{ "P3 Start", 'S' },
						{ "P3 B", 'B' },
						{ "P3 A", 'A' }
					},
					new MnemonicCollection("Player 4")
					{
						{ "P4 Up", 'U' },
						{ "P4 Down", 'D' },
						{ "P4 Left", 'L' },
						{ "P4 Right", 'R' },
						{ "P4 Select", 's' },
						{ "P4 Start", 'S' },
						{ "P4 B", 'B' },
						{ "P4 A", 'A' }
					}
				},
				new CoreMnemonicCollection(new []{ "SNES", "SGB" })
				{
					new MnemonicCollection("Console")
					{
						{ "Reset", 'r' },
						{ "Power", 'P' },
					},
					new MnemonicCollection("Player 1")
					{
						{ "P1 Up", 'U' },
						{ "P1 Down", 'D' },
						{ "P1 Left", 'L' },
						{ "P1 Right", 'R' },
						{ "P1 Select", 's' },
						{ "P1 Start", 'S' },
						{ "P1 B", 'B' },
						{ "P1 A", 'A' },
						{ "P1 X", 'X' },
						{ "P1 Y", 'Y'},
						{ "P1 L", 'L'},
						{ "P1 R", 'R'}
					},
					new MnemonicCollection("Player 2")
					{
						{ "P2 Up", 'U' },
						{ "P2 Down", 'D' },
						{ "P2 Left", 'L' },
						{ "P2 Right", 'R' },
						{ "P2 Select", 's' },
						{ "P2 Start", 'S' },
						{ "P2 B", 'B' },
						{ "P2 A", 'A' },
						{ "P2 X", 'X' },
						{ "P2 Y", 'Y'},
						{ "P2 L", 'L'},
						{ "P2 R", 'R'}
						
					},
					new MnemonicCollection("Player 3")
					{
						{ "P3 Up", 'U' },
						{ "P3 Down", 'D' },
						{ "P3 Left", 'L' },
						{ "P3 Right", 'R' },
						{ "P3 Select", 's' },
						{ "P3 Start", 'S' },
						{ "P3 B", 'B' },
						{ "P3 A", 'A' },
						{ "P3 X", 'X' },
						{ "P3 Y", 'Y'},
						{ "P3 L", 'L'},
						{ "P3 R", 'R'}
					},
					new MnemonicCollection("Player 4")
					{
						{ "P4 Up", 'U' },
						{ "P4 Down", 'D' },
						{ "P4 Left", 'L' },
						{ "P4 Right", 'R' },
						{ "P4 Select", 's' },
						{ "P4 Start", 'S' },
						{ "P4 B", 'B' },
						{ "P4 A", 'A' },
						{ "P4 X", 'X' },
						{ "P4 Y", 'Y'},
						{ "P4 L", 'L'},
						{ "P4 R", 'R'}
					}
				}
			};
		}
	}
}
