using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using BizHawk.Common.CollectionExtensions;
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

		private List<Cheat> _cheatList = new List<Cheat>();
		private string _defaultFileName = "";
		private bool _changes;

		public delegate void CheatListEventHandler(object sender, CheatListEventArgs e);
		public event CheatListEventHandler Changed;

		public int Count => _cheatList.Count;

		public int CheatCount => _cheatList.Count(c => !c.IsSeparator);

		public int ActiveCount => _cheatList.Count(c => c.Enabled);

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

		public Cheat this[MemoryDomain domain, long address] =>
			_cheatList.FirstOrDefault(cheat => cheat.Domain == domain && cheat.Address == address);


		public IEnumerator<Cheat> GetEnumerator() => _cheatList.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public void Pulse()
		{
			_cheatList.ForEach(cheat => cheat.Pulse());
		}

		/// <summary>
		/// Looks for a .cht file that matches the ROM loaded based on the default filename for a given ROM
		/// </summary>
		public bool AttemptToLoadCheatFile()
		{
			var file = new FileInfo(_defaultFileName);
			return file.Exists && Load(file.FullName, false);
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
			if (cheat is null)
			{
				throw new ArgumentNullException($"{nameof(cheat)} can not be null");
			}

			if (cheat.IsSeparator)
			{
				_cheatList.Add(cheat);
			}
			else
			{
				cheat.Changed += CheatChanged;
				if (Contains(cheat))
				{
					_cheatList.Remove(Global.CheatList.FirstOrDefault(c => c.Domain == cheat.Domain && c.Address == cheat.Address));
				}

				_cheatList.Add(cheat);
			}

			Changes = true;
		}

		public void AddRange(IEnumerable<Cheat> cheats)
		{
			_cheatList.AddRange(
				cheats.Where(c => !_cheatList.Contains(c)));
			Changes = true;
		}

		public void Insert(int index, Cheat cheat)
		{
			cheat.Changed += CheatChanged;
			if (_cheatList.Any(c => c.Domain == cheat.Domain && c.Address == cheat.Address))
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

		public bool Remove(Watch watch)
		{
			var cheat = _cheatList.FirstOrDefault(c => c == watch);
			if (cheat != (Cheat)null)
			{
				_cheatList.Remove(cheat);
				Changes = true;
				return true;
			}
			
			return false;
		}

		public bool Contains(Cheat cheat)
		{
			return _cheatList.Any(c => c == cheat);
		}

		public bool Contains(MemoryDomain domain, long address)
		{
			return _cheatList.Any(c => c.Domain == domain && c.Address == address);
		}

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

		public void RemoveAll()
		{
			_cheatList.Clear();
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

		public void EnableAll()
		{
			_cheatList.ForEach(c => c.Enable(false));
			Changes = true;
		}

		public bool IsActive(MemoryDomain domain, long address)
		{
			return _cheatList.Any(cheat => 
					!cheat.IsSeparator &&
					cheat.Enabled &&
					cheat.Domain == domain
					&& cheat.Contains(address));
		}

		/// <summary>
		/// Returns the value of a given byte in a cheat, If the cheat is a single byte this will be the same indexing the cheat,
		/// But if the cheat is multi-byte, this will return just the cheat value for that specific byte
		/// </summary>
		/// <returns>Returns null if address is not a part of a cheat, else returns the value of that specific byte only</returns>
		public byte? GetByteValue(MemoryDomain domain, long addr)
		{
			var activeCheat = _cheatList.FirstOrDefault(cheat => cheat.Contains(addr));
			if (activeCheat == (Cheat)null)
			{
				return null;
			}

			return activeCheat.GetByteVal(addr);
		}

		/// <summary>
		/// Returns the value of a given cheat, or a partial value of a multi-byte cheat
		/// Note that address + size MUST NOT exceed the range of the cheat or undefined behavior will occur
		/// </summary>
		/// <param name="domain">The <seealso cref="MemoryDomain"/> to apply cheats to</param>
		/// <param name="addr">The starting address for which you will get the number of bytes</param>
		/// <param name="size">The number of bytes of the cheat to return</param>
		/// <returns>The value, or null if it can't resolve the address with a given cheat</returns>
		public int? GetCheatValue(MemoryDomain domain, long addr, WatchSize size)
		{
			var activeCheat = _cheatList.FirstOrDefault(cheat => cheat.Contains(addr));
			if (activeCheat == (Cheat)null)
			{
				return null;
			}

			switch (activeCheat.Size)
			{
				default:
				case WatchSize.Byte:
					return activeCheat.Value;
				case WatchSize.Word:
					return size == WatchSize.Byte
						? GetByteValue(domain, addr)
						: activeCheat.Value;
				case WatchSize.DWord:
					if (size == WatchSize.Byte)
					{
						return GetByteValue(domain, addr);
					}
					else if (size == WatchSize.Word)
					{
						if (activeCheat.Address == addr)
						{
							return (activeCheat.Value.Value >> 16) & 0xFFFF;
						}

						return activeCheat.Value.Value & 0xFFFF;
					}

					return activeCheat.Value;
			}
		}

		public void SaveOnClose()
		{
			if (Global.Config.CheatsAutoSaveOnClose)
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
					new FileInfo(CurrentFileName).Delete();
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

				using (var sw = new StreamWriter(path))
				{
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

							cheat.SetType(DisplayType.Hex);

							sb
								.Append(cheat.AddressStr).Append('\t')
								.Append(cheat.ValueStr).Append('\t')
								.Append(cheat.Compare?.ToString() ?? "N").Append('\t')
								.Append(cheat.Domain != null ? cheat.Domain.Name : "").Append('\t')
								.Append(cheat.Enabled ? '1' : '0').Append('\t')
								.Append(cheat.Name).Append('\t')
								.Append(cheat.SizeAsChar).Append('\t')
								.Append(cheat.TypeAsChar).Append('\t')
								.Append((cheat.BigEndian ?? false) ? '1' : '0').Append('\t')
								.Append(cheat.ComparisonType).Append('\t')
								.AppendLine();

							cheat.SetType(tempCheatType);

						}
					}

					sw.WriteLine(sb.ToString());
				}

				CurrentFileName = path;
				Global.Config.RecentCheats.Add(CurrentFileName);
				Changes = false;
				return true;
			}
			catch
			{
				return false;
			}
		}

		public bool Load(string path, bool append)
		{
			var file = new FileInfo(path);
			if (file.Exists == false)
			{
				return false;
			}

			if (!append)
			{
				CurrentFileName = path;
			}

			using (var sr = file.OpenText())
			{
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
							var type = DisplayType.Hex;
							var bigEndian = false;
							Cheat.CompareType comparisonType = Cheat.CompareType.None;

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

							var domain = Global.Emulator.AsMemoryDomains()[vals[3]];
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

							Add(new Cheat(watch, value, compare, !Global.Config.DisableCheatsOnLoad && enabled, comparisonType));
						}
					}
					catch
					{
						continue;
					}
				}
			}

			Changes = false;
			return true;
		}

		public void Sort(string column, bool reverse)
		{
			switch (column)
			{
				case NameColumn:
					_cheatList = _cheatList
						.OrderBy(c => c.Name, reverse)
						.ThenBy(c => c.Address ?? 0)
						.ToList();
					break;
				case AddressColumn:
					_cheatList = _cheatList
						.OrderBy(c => c.Address ?? 0, reverse)
						.ThenBy(c => c.Name)
						.ToList();
					break;
				case ValueColumn:
					_cheatList = _cheatList
						.OrderBy(c => c.Value ?? 0, reverse)
						.ThenBy(c => c.Name)
						.ThenBy(c => c.Address ?? 0)
						.ToList();
					break;
				case CompareColumn:
					_cheatList = _cheatList
						.OrderBy(c => c.Compare ?? 0, reverse)
						.ThenBy(c => c.Name)
						.ThenBy(c => c.Address ?? 0)
						.ToList();
					break;
				case OnColumn:
					_cheatList = _cheatList
						.OrderBy(c => c.Enabled, reverse)
						.ThenBy(c => c.Name)
						.ThenBy(c => c.Address ?? 0)
						.ToList();
					break;
				case DomainColumn:
					_cheatList = _cheatList
						.OrderBy(c => c.Domain, reverse)
						.ThenBy(c => c.Name)
						.ThenBy(c => c.Address ?? 0)
						.ToList();
					break;
				case SizeColumn:
					_cheatList = _cheatList
						.OrderBy(c => (int)c.Size, reverse)
						.ThenBy(c => c.Name)
						.ThenBy(c => c.Address ?? 0)
						.ToList();
					break;
				case EndianColumn:
					_cheatList = _cheatList
						.OrderBy(c => c.BigEndian, reverse)
						.ThenBy(c => c.Name)
						.ThenBy(c => c.Address ?? 0)
						.ToList();
					break;
				case TypeColumn:
					_cheatList = _cheatList
						.OrderBy(c => c.Type, reverse)
						.ThenBy(c => c.Name)
						.ThenBy(c => c.Address ?? 0)
						.ToList();
					break;
				case ComparisonType:
					_cheatList = _cheatList
						.OrderBy(c => c.ComparisonType, reverse)
						.ThenBy(c => c.Name)
						.ThenBy(c => c.Address ?? 0)
						.ToList();
					break;
			}
		}

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
