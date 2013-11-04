using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class CheatList : IEnumerable<Cheat>
	{
		private List<Cheat> _cheatList = new List<Cheat>();
		private string _currentFileName = String.Empty;
		private bool _changes;
		private string _defaultFileName = String.Empty;

		public IEnumerator<Cheat> GetEnumerator()
		{
			return _cheatList.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public Cheat this[int index]
		{
			get { return _cheatList[index]; }
		}

		public void Pulse()
		{
			foreach(var cheat in _cheatList)
			{
				cheat.Pulse();
			}
		}

		/// <summary>
		/// Looks for a .cht file that matches the ROM loaded based on the default filename for a given ROM
		/// </summary>
		/// <returns></returns>
		public bool AttemptToLoadCheatFile()
		{
			var file = new FileInfo(_defaultFileName);

			if (file.Exists)
			{
				return Load(file.FullName, false);
			}
			else
			{
				return false;
			}
		}

		public void FlagChanges()
		{
			_changes = true;
		}

		public int Count
		{
			get { return _cheatList.Count; }
		}

		public int CheatCount
		{
			get { return _cheatList.Count(x => !x.IsSeparator); }
		}

		public int ActiveCount
		{
			get { return _cheatList.Count(x => x.Enabled); }
		}

		public void NewList(string defaultFileName)
		{
			_defaultFileName = defaultFileName;
			_cheatList.Clear();
			_currentFileName = String.Empty;
			_changes = false;
		}

		public void Update()
		{
			_cheatList.ForEach(x => x.Pulse());
		}

		public void Add(Cheat c)
		{
			if (_cheatList.Any(x => x.Domain == c.Domain && x.Address == c.Address))
			{
				_cheatList.FirstOrDefault(x => x.Domain == c.Domain && x.Address == c.Address).Enable();
			}
			else
			{
				_cheatList.Add(c);
			}

			_changes = true;
		}

		public void Insert(int index, Cheat c)
		{
			if (_cheatList.Any(x => x.Domain == c.Domain && x.Address == c.Address))
			{
				_cheatList.FirstOrDefault(x => x.Domain == c.Domain && x.Address == c.Address).Enable();
			}
			else
			{
				_cheatList.Insert(index, c);
			}

			_changes = true;
		}

		public void Remove(Cheat c)
		{
			_changes = true;
			_cheatList.Remove(c);
		}

		public void Remove(Watch w)
		{
			
			var cheat = _cheatList.FirstOrDefault(x => x.Domain == w.Domain && x.Address == w.Address);
			if (cheat != null)
			{
				_changes = true;
				_cheatList.Remove(cheat);
			}
		}

		public void RemoveRange(IEnumerable<Cheat> cheats)
		{
			_changes = true;
			foreach (var cheat in cheats)
			{
				_cheatList.Remove(cheat);
			}
		}

		public bool Changes
		{
			get { return _changes; }
		}

		public void Clear()
		{
			_changes = true;
			_cheatList.Clear();
		}

		public void DisableAll()
		{
			_changes = true;
			_cheatList.ForEach(x => x.Disable());
		}

		public void EnableAll()
		{
			_changes = true;
			_cheatList.ForEach(x => x.Enable());
		}

		public bool IsActive(MemoryDomain domain, int address)
		{
			foreach (var cheat in _cheatList)
			{
				if (cheat.IsSeparator)
				{
					continue;
				}
				else if (cheat.Domain == domain && cheat.Contains(address) && cheat.Enabled)
				{
					return true;
				}
			}

			return false;
		}

		public void SaveOnClose()
		{
			if (Global.Config.CheatsAutoSaveOnClose)
			{
				if (_changes && _cheatList.Any())
				{
					if (String.IsNullOrWhiteSpace(_currentFileName))
					{
						_currentFileName = _defaultFileName;
					}

					SaveFile(_currentFileName);
				}
				else if (!_cheatList.Any() && !String.IsNullOrWhiteSpace(_currentFileName))
				{
					new FileInfo(_currentFileName).Delete();
				}
			}
		}

		public bool Save()
		{
			if (String.IsNullOrWhiteSpace(_currentFileName))
			{
				_currentFileName = _defaultFileName;
			}

			return SaveFile(_currentFileName);
		}

		public bool SaveFile(string path)
		{
			try
			{
				FileInfo file = new FileInfo(path);
				if (file.Directory != null && !file.Directory.Exists)
				{
					file.Directory.Create();
				}

				using (StreamWriter sw = new StreamWriter(path))
				{
					StringBuilder sb = new StringBuilder();

					foreach (var cheat in _cheatList)
					{
						if (cheat.IsSeparator)
						{
							sb.AppendLine("----");
						}
						else
						{
							//Set to hex for saving 
							cheat.SetType(Watch.DisplayType.Hex);

							sb
								.Append(cheat.AddressStr).Append('\t')
								.Append(cheat.ValueStr).Append('\t')
								.Append(cheat.Compare.HasValue ? cheat.Compare.Value.ToString() : "N").Append('\t')
								.Append(cheat.Domain != null ? cheat.Domain.Name : String.Empty).Append('\t')
								.Append(cheat.Enabled ? '1' : '0').Append('\t')
								.Append(cheat.Name).Append('\t')
								.Append(cheat.SizeAsChar).Append('\t')
								.Append(cheat.TypeAsChar).Append('\t')
								.Append((cheat.BigEndian ?? false) ? '1' : '0').Append('\t')
								.AppendLine();
						}
					}

					sw.WriteLine(sb.ToString());
				}

				_changes = false;
				_currentFileName = path;
				Global.Config.RecentCheats.Add(_currentFileName);
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
				_currentFileName = path;
			}

			using (StreamReader sr = file.OpenText())
			{
				if (append)
				{
					_changes = true; 
				}
				else
				{
					Clear();
					_changes = false;
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
							Watch.WatchSize size = Watch.WatchSize.Byte;
							Watch.DisplayType type = Watch.DisplayType.Hex;
							bool BIGENDIAN = false;


							if (s.Length < 6) continue;
							//NewCheat c = new NewCheat(
							string[] vals = s.Split('\t');
							int ADDR = Int32.Parse(vals[0], NumberStyles.HexNumber);
							int value = Int32.Parse(vals[1], NumberStyles.HexNumber);

							if (vals[2] == "N")
							{
								compare = null;
							}
							else
							{
								compare = Int32.Parse(vals[2], NumberStyles.HexNumber);
							}
							MemoryDomain domain = DomainByName(vals[3]);
							bool ENABLED = vals[4] == "1";
							string name = vals[5];

							//For backwards compatibility, don't assume these values exist
							if (vals.Length > 6)
							{
								size = Watch.SizeFromChar(vals[6][0]);
								type = Watch.DisplayTypeFromChar(vals[7][0]);
								BIGENDIAN = vals[8] == "1";
							}

							Watch w = Watch.GenerateWatch(
								domain,
								ADDR,
								size,
								type,
								name,
								BIGENDIAN
							);

							Cheat c = new Cheat(w, value, compare, !Global.Config.DisableCheatsOnLoad && ENABLED);
							_cheatList.Add(c);
						}
					}
					catch
					{
						continue;
					}
				}
			}

			return true;
		}

		public string CurrentFileName
		{
			get { return _currentFileName; }
		}

		public void Sort(string column, bool reverse)
		{
			switch (column)
			{
				case NAME:
					if (reverse)
					{
						_cheatList = _cheatList
							.OrderByDescending(x => x.Name)
							.ThenBy(x => x.Address ?? 0)
							.ToList();
					}
					else
					{
						_cheatList = _cheatList
							.OrderBy(x => x.Name)
							.ThenBy(x => x.Address ?? 0)
							.ToList();
					}
					break;
				case ADDRESS:
					if (reverse)
					{
						_cheatList = _cheatList
							.OrderByDescending(x => x.Address ?? 0)
							.ThenBy(x => x.Name)
							.ToList();
					}
					else
					{
						_cheatList = _cheatList
							.OrderBy(x => x.Address ?? 0)
							.ThenBy(x => x.Name)
							.ToList();
					}
					break;
				case VALUE:
					if (reverse)
					{
						_cheatList = _cheatList
							.OrderByDescending(x => x.Value ?? 0)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address ?? 0)
							.ToList();
					}
					else
					{
						_cheatList = _cheatList
							.OrderBy(x => x.Value ?? 0)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address ?? 0)
							.ToList();
					}
					break;
				case COMPARE:
					if (reverse)
					{
						_cheatList = _cheatList
							.OrderByDescending(x => x.Compare ?? 0)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address ?? 0)
							.ToList();
					}
					else
					{
						_cheatList = _cheatList
							.OrderBy(x => x.Compare ?? 0)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address ?? 0)
							.ToList();
					}
					break;
				case ON:
					if (reverse)
					{
						_cheatList = _cheatList
							.OrderByDescending(x => x.Enabled)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address ?? 0)
							.ToList();
					}
					else
					{
						_cheatList = _cheatList
							.OrderBy(x => x.Enabled)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address ?? 0)
							.ToList();
					}
					break;
				case DOMAIN:
					if (reverse)
					{
						_cheatList = _cheatList
							.OrderByDescending(x => x.Domain)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address ?? 0)
							.ToList();
					}
					else
					{
						_cheatList = _cheatList
							.OrderBy(x => x.Domain)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address ?? 0)
							.ToList();
					}
					break;
				case SIZE:
					if (reverse)
					{
						_cheatList = _cheatList
							.OrderByDescending(x => ((int)x.Size))
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address ?? 0)
							.ToList();
					}
					else
					{
						_cheatList = _cheatList
							.OrderBy(x => ((int)x.Size))
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address ?? 0)
							.ToList();
					}
					break;
				case ENDIAN:
					if (reverse)
					{
						_cheatList = _cheatList
							.OrderByDescending(x => x.BigEndian)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address ?? 0)
							.ToList();
					}
					else
					{
						_cheatList = _cheatList
							.OrderBy(x => x.BigEndian)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address ?? 0)
							.ToList();
					}
					break;
				case TYPE:
					if (reverse)
					{
						_cheatList = _cheatList
							.OrderByDescending(x => x.Type)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address ?? 0)
							.ToList();
					}
					else
					{
						_cheatList = _cheatList
							.OrderBy(x => x.Type)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address ?? 0)
							.ToList();
					}
					break;
			}
		}

		#region Privates

		private static MemoryDomain DomainByName(string name)
		{
			//Attempts to find the memory domain by name, if it fails, it defaults to index 0
			foreach (MemoryDomain domain in Global.Emulator.MemoryDomains)
			{
				if (domain.Name == name)
				{
					return domain;
				}
			}

			return Global.Emulator.MainMemory;
		}

		#endregion

		public const string NAME = "NamesColumn";
		public const string ADDRESS = "AddressColumn";
		public const string VALUE = "ValueColumn";
		public const string COMPARE = "CompareColumn";
		public const string ON = "OnColumn";
		public const string DOMAIN = "DomainColumn";
		public const string SIZE = "SizeColumn";
		public const string ENDIAN = "EndianColumn";
		public const string TYPE = "DisplayTypeColumn";
	}
}
