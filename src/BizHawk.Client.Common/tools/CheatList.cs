using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class CheatCollection : ICollection<Cheat>
	{
		private const string NameColumn = "NamesColumn";
		private const string AddressColumn = "AddressColumn";
		private const string ValueColumn = "ValueColumn";
		private const string CompareColumn = "CompareColumn";
		private const string OnColumn = "OnColumn";
		private const string DomainColumn = "DomainColumn";
		private const string SizeColumn = "SizeColumn";
		private const string EndianColumn = "EndianColumn";
		private const string TypeColumn = "DisplayTypeColumn";
		private const string ComparisonType = "ComparisonTypeColumn";

		private readonly IDialogParent _dialogParent;

		private readonly ICheatConfig _config;
		private List<Cheat> _cheatList = new List<Cheat>();
		private string _defaultFileName = "";
		private bool _changes;

		public CheatCollection(IDialogParent dialogParent, ICheatConfig config)
		{
			_dialogParent = dialogParent;
			_config = config;
		}

		public delegate void CheatListEventHandler(object sender, CheatListEventArgs e);
		public event CheatListEventHandler Changed;

		public int Count => _cheatList.Count;

		public int CheatCount => _cheatList.Count(c => !c.IsSeparator);

		public int ActiveCount => _cheatList.Count(c => c.Enabled);

		public bool AnyActive
			=> _cheatList.Exists(static c => c.Enabled);

		public bool Changes
		{
			get => _changes;
			set
			{
				_changes = value;
				if (value)
				{
					CheatChanged(Cheat.Separator); // Pass a dummy, no cheat invoked this change
				}
			}
		}

		public string CurrentFileName { get; private set; } = "";

		public bool IsReadOnly => false;

		public Cheat this[int index] => _cheatList[index];

		public Cheat this[MemoryDomain domain, long address]
			=> _cheatList.Find(cheat => cheat.Domain == domain && cheat.Address == address);

		public IEnumerator<Cheat> GetEnumerator() => _cheatList.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public void Pulse()
		{
			_cheatList.ForEach(cheat => cheat.Pulse());
		}

		/// <summary>
		/// Looks for a .cht file that matches the ROM loaded based on the default filename for a given ROM
		/// </summary>
		public bool AttemptToLoadCheatFile(IMemoryDomains domains)
		{
			var file = new FileInfo(_defaultFileName);
			return file.Exists && Load(domains, file.FullName, false);
		}

		public void NewList(string defaultFileName, bool autosave = false)
		{
			_defaultFileName = defaultFileName;

			if (_cheatList.Any() && _changes && autosave)
			{
				if (string.IsNullOrEmpty(CurrentFileName))
				{
					CurrentFileName = _defaultFileName;
				}

				Save();
			}

			_cheatList.Clear();
			CurrentFileName = "";
			Changes = false;
		}

		/// <exception cref="ArgumentNullException"><paramref name="cheat"/> is null</exception>
		public void Add(Cheat cheat)
		{
			if (cheat is null) throw new ArgumentNullException(paramName: nameof(cheat));

			if (cheat.IsSeparator)
			{
				_cheatList.Add(cheat);
			}
			else
			{
				cheat.Changed += CheatChanged;
				if (Contains(cheat))
				{
					_cheatList.Remove(this.FirstOrDefault(c => c.Domain == cheat.Domain && c.Address == cheat.Address));
				}

				_cheatList.Add(cheat);
			}

			Changes = true;
		}

		public void AddRange(IEnumerable<Cheat> cheats)
		{
			var toAdd = cheats.Where(c => !_cheatList.Contains(c)).ToList();
			if (toAdd.Count is 0) return;
			const int WARN_WHEN_ADDING_MORE_THAN = 200;
			if (toAdd.Count > WARN_WHEN_ADDING_MORE_THAN && !_dialogParent.ModalMessageBox2($"Adding {toAdd.Count} freezes/cheats at once is probably a bad idea. Do it anyway?")) return;
			_cheatList.AddRange(toAdd);
			Changes = true;
		}

		public void Insert(int index, Cheat cheat)
		{
			cheat.Changed += CheatChanged;
			if (_cheatList.Exists(c => c.Domain == cheat.Domain && c.Address == cheat.Address))
			{
				_cheatList.First(c => c.Domain == cheat.Domain && c.Address == cheat.Address).Enable();
			}
			else
			{
				_cheatList.Insert(index, cheat);
			}

			Changes = true;
		}

		public bool Exchange(Cheat oldCheat, Cheat newCheat)
		{
			int index = _cheatList.IndexOf(oldCheat);
			if (index == -1)
			{
				return false;
			}

			_cheatList[index] = newCheat;
			Changes = true;

			return true;
		}

		public bool Remove(Cheat cheat)
		{
			var result = _cheatList.Remove(cheat);
			if (result)
			{
				Changes = true;
				return true;
			}
			
			return false;
		}

		public bool Contains(Cheat cheat)
			=> _cheatList.Exists(c => c == cheat);

		public void CopyTo(Cheat[] array, int arrayIndex)
		{
			_cheatList.CopyTo(array, arrayIndex);
		}

		public void RemoveRange(IEnumerable<Cheat> cheats)
		{
			foreach (var cheat in cheats.ToList()) // enumerate passed IEnumerable because it may depend on the value of _cheatList
			{
				_cheatList.Remove(cheat);
			}

			Changes = true;
		}

		public void RemoveRange(IEnumerable<Watch> watches)
		{
			_cheatList.RemoveAll(cheat => watches.Any(w => w == cheat));
			Changes = true;
		}

		public void Clear()
		{
			_cheatList.Clear();
			Changes = true;
		}

		public void DisableAll()
		{
			_cheatList.ForEach(c => c.Disable(false));
			Changes = true;
		}

		public bool IsActive(MemoryDomain domain, long address)
			=> _cheatList.Exists(cheat => !cheat.IsSeparator && cheat.Enabled && cheat.Domain == domain && cheat.Contains(address));

		public void SaveOnClose()
		{
			if (_config.AutoSaveOnClose)
			{
				if (Changes && _cheatList.Any())
				{
					if (string.IsNullOrWhiteSpace(CurrentFileName))
					{
						CurrentFileName = _defaultFileName;
					}

					SaveFile(CurrentFileName);
				}
				else if (!_cheatList.Any() && !string.IsNullOrWhiteSpace(CurrentFileName))
				{
					File.Delete(CurrentFileName);
					_config.Recent.Remove(CurrentFileName);
				}
			}
		}

		public bool Save()
		{
			if (string.IsNullOrWhiteSpace(CurrentFileName))
			{
				CurrentFileName = _defaultFileName;
			}

			return SaveFile(CurrentFileName);
		}

		public bool SaveFile(string path)
		{
			try
			{
				var file = new FileInfo(path);
				if (file.Directory != null && !file.Directory.Exists)
				{
					file.Directory.Create();
				}

				var sb = new StringBuilder();

				foreach (var cheat in _cheatList)
				{
					if (cheat.IsSeparator)
					{
						sb.AppendLine("----");
					}
					else
					{
						// Set to hex for saving
						var tempCheatType = cheat.Type;

						cheat.SetType(WatchDisplayType.Hex);

						sb
							.Append(cheat.AddressStr).Append('\t')
							.Append(cheat.ValueStr).Append('\t')
							.Append(cheat.Compare is null ? "N" : cheat.CompareStr).Append('\t')
							.Append(cheat.Domain != null ? cheat.Domain.Name : "").Append('\t')
							.Append(cheat.Enabled ? '1' : '0').Append('\t')
							.Append(cheat.Name).Append('\t')
							.Append(cheat.SizeAsChar).Append('\t')
							.Append(cheat.TypeAsChar).Append('\t')
							.Append(cheat.BigEndian is true ? '1' : '0').Append('\t')
							.Append(cheat.ComparisonType).Append('\t')
							.AppendLine();

						cheat.SetType(tempCheatType);
					}
				}

				File.WriteAllText(path, sb.ToString());

				CurrentFileName = path;
				_config.Recent.Add(CurrentFileName);
				Changes = false;
				return true;
			}
			catch
			{
				return false;
			}
		}

		public bool Load(IMemoryDomains domains, string path, bool append)
		{
			var file = new FileInfo(path);
			if (!file.Exists)
			{
				return false;
			}

			if (!append)
			{
				CurrentFileName = path;
			}

			using var sr = file.OpenText();
			
			if (!append)
			{
				Clear();
			}

			string s;
			while ((s = sr.ReadLine()) != null)
			{
				try
				{
					if (s == "----")
					{
						_cheatList.Add(Cheat.Separator);
					}
					else
					{
						int? compare;
						var size = WatchSize.Byte;
						var type = WatchDisplayType.Hex;
						var bigEndian = false;
						var comparisonType = Cheat.CompareType.None;

						if (s.Length < 6)
						{
							continue;
						}

						var vals = s.Split('\t');
						var address = int.Parse(vals[0], NumberStyles.HexNumber);
						var value = int.Parse(vals[1], NumberStyles.HexNumber);

						if (vals[2] == "N")
						{
							compare = null;
						}
						else
						{
							compare = int.Parse(vals[2], NumberStyles.HexNumber);
						}

						var domain = domains[vals[3]];
						var enabled = vals[4] == "1";
						var name = vals[5];

						// For backwards compatibility, don't assume these values exist
						if (vals.Length > 6)
						{
							size = Watch.SizeFromChar(vals[6][0]);
							type = Watch.DisplayTypeFromChar(vals[7][0]);
							bigEndian = vals[8] == "1";
						}
						
						// For backwards compatibility, don't assume these values exist
						if (vals.Length > 9)
						{
							if (!Enum.TryParse(vals[9], out comparisonType))
							{
								continue; // Not sure if this is the best answer, could just resort to ==
							}
						}

						var watch = Watch.GenerateWatch(
							domain,
							address,
							size,
							type,
							bigEndian,
							name);

						Add(new Cheat(watch, value, compare, !_config.DisableOnLoad && enabled, comparisonType));
					}
				}
				catch
				{
					// Ignore and continue
				}
			}

			_config.Recent.Add(CurrentFileName);
			Changes = false;
			return true;
		}

		public void UpdateDomains(IMemoryDomains domains)
		{
			for (int i = _cheatList.Count - 1; i >= 0; i--)
			{
				var cheat = _cheatList[i];
				var newDomain = domains[cheat.Domain.Name];
				if (newDomain is not null)
				{
					cheat.Domain = newDomain;
				}
				else
				{
					_cheatList.RemoveAt(i);
					Changes = true;
				}
			}
		}

		private static readonly RigidMultiPredicateSort<Cheat> ColumnSorts
			= new RigidMultiPredicateSort<Cheat>(new Dictionary<string, Func<Cheat, IComparable>>
			{
				[NameColumn] = c => c.Name,
				[AddressColumn] = c => c.Address ?? 0L,
				[ValueColumn] = c => c.Value ?? 0,
				[CompareColumn] = c => c.Compare ?? 0,
				[OnColumn] = c => c.Enabled,
				[DomainColumn] = c => c.Domain.Name,
				[SizeColumn] = c => (int) c.Size,
				[EndianColumn] = c => c.BigEndian,
				[TypeColumn] = c => c.Type,
				[ComparisonType] = c => c.ComparisonType
			});

		public void Sort(string column, bool reverse) => _cheatList = ColumnSorts.AppliedTo(_cheatList, column, firstIsDesc: reverse);

		public void SetDefaultFileName(string defaultFileName)
		{
			_defaultFileName = defaultFileName;
		}

		private void CheatChanged(object sender)
		{
			Changed?.Invoke(this, new CheatListEventArgs(sender as Cheat));
			_changes = true;
		}

		public class CheatListEventArgs : EventArgs
		{
			public CheatListEventArgs(Cheat c)
			{
				Cheat = c;
			}

			public Cheat Cheat { get; }
		}
	}
}
