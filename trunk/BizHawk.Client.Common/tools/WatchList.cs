using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using BizHawk.Common.NumberExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class WatchList : IList<Watch>
	{
		private IMemoryDomains _memoryDomains;
		private List<Watch> _watchList = new List<Watch>();
		private MemoryDomain _domain;
		private string _currentFilename = string.Empty;
		private string _systemid;

		public const string ADDRESS = "AddressColumn";
		public const string VALUE = "ValueColumn";
		public const string PREV = "PrevColumn";
		public const string CHANGES = "ChangesColumn";
		public const string DIFF = "DiffColumn";
		public const string DOMAIN = "DomainColumn";
		public const string NOTES = "NotesColumn";

		public WatchList(IMemoryDomains core, MemoryDomain domain, string systemid)
		{
			_memoryDomains = core;
			_domain = domain;
			_systemid = systemid;
		}

		public void RefreshDomans(IMemoryDomains core, MemoryDomain domain)
		{
			_memoryDomains = core;
			_domain = domain;

			_watchList.ForEach(w =>
			{
				if (w.Domain != null)
				{
					w.Domain = _memoryDomains[w.Domain.Name];
				}
			});
		}
		
		public enum WatchPrevDef { LastSearch, Original, LastFrame, LastChange }

		public string AddressFormatStr // TODO: this is probably compensating for not using the ToHex string extension
		{
			get
			{
				if (_domain != null)
				{
					return "{0:X" + (_domain.Size - 1).NumHexDigits() + "}";
				}

				return string.Empty;
			}
		}

		public int Count
		{
			get { return _watchList.Count; }
		}

		public int WatchCount
		{
			get { return _watchList.Count(w => !w.IsSeparator); }
		}

		public int ItemCount
		{
			get { return _watchList.Count; }
		}

		public MemoryDomain Domain
		{
			get { return _domain; }
			set { _domain = value; }
		}

		public bool IsReadOnly { get { return false; } }

		public string CurrentFileName
		{
			get { return _currentFilename; }
			set { _currentFilename = value; }
		}

		public bool Changes { get; set; }

		public Watch this[int index]
		{
			get { return _watchList[index]; }
			set { _watchList[index] = value; }
		}

		public IEnumerator<Watch> GetEnumerator()
		{
			return _watchList.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void OrderWatches(string column, bool reverse)
		{
			switch (column)
			{
				case ADDRESS:
					if (reverse)
					{
						_watchList = _watchList
							.OrderByDescending(x => x.Address ?? 0)
							.ThenBy(x => x.Domain.Name)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ThenBy(x => x.BigEndian)
							.ToList();
					}
					else
					{
						_watchList = _watchList
							.OrderBy(x => x.Address ?? 0)
							.ThenBy(x => x.Domain.Name)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ThenBy(x => x.BigEndian)
							.ToList();
					}

					break;
				case VALUE:
					if (reverse)
					{
						_watchList = _watchList
							.OrderByDescending(x => x.Value ?? 0)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ThenBy(x => x.BigEndian)
							.ToList();
					}
					else
					{
						_watchList = _watchList
							.OrderBy(x => x.Value ?? 0)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ThenBy(x => x.BigEndian)
							.ToList();
					}

					break;
				case PREV: // Note: these only work if all entries are detailed objects!
					if (reverse)
					{
						_watchList = _watchList
							.OrderByDescending(x => x.PreviousStr)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}
					else
					{
						_watchList = _watchList
							.OrderBy(x => x.PreviousStr)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}

					break;
				case DIFF:
					if (reverse)
					{
						_watchList = _watchList
							.OrderByDescending(x => x.Diff)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}
					else
					{
						_watchList = _watchList
							.OrderBy(x => x.Diff)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}

					break;
				case CHANGES:
					if (reverse)
					{
						_watchList = _watchList
							.OrderByDescending(x => x.ChangeCount)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}
					else
					{
						_watchList = _watchList
							.OrderBy(x => x.ChangeCount)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}

					break;
				case DOMAIN:
					if (reverse)
					{
						_watchList = _watchList
							.OrderByDescending(x => x.Domain)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ThenBy(x => x.BigEndian)
							.ToList();
					}
					else
					{
						_watchList = _watchList
							.OrderBy(x => x.Domain)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ThenBy(x => x.BigEndian)
							.ToList();
					}

					break;
				case NOTES:
					if (reverse)
					{
						_watchList = _watchList
							.OrderByDescending(x => x.Notes)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}
					else
					{
						_watchList = _watchList
							.OrderBy(x => x.Notes)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}

					break;
			}
		}

		public void Clear()
		{
			_watchList.Clear();
			Changes = false;
			_currentFilename = string.Empty;
		}

		public void UpdateValues()
		{
			foreach (var watch in _watchList)
			{
				watch.Update();
			}
		}

		public void Add(Watch watch)
		{
			_watchList.Add(watch);
			Changes = true;
		}

		public void AddRange(IList<Watch> watches)
		{
			_watchList.AddRange(watches);
			Changes = true;
		}

		public bool Remove(Watch watch)
		{
			var result = _watchList.Remove(watch);
			if (result)
			{
				Changes = true;
			}

			return result;
		}

		public void Insert(int index, Watch watch)
		{
			_watchList.Insert(index, watch);
		}

		public void ClearChangeCounts()
		{
			foreach (var watch in _watchList)
			{
				watch.ClearChangeCount();
			}
		}

		public bool Contains(Watch watch)
		{
			return _watchList.Any(w =>
				w.Size == watch.Size &&
				w.Type == watch.Type &&
				w.Domain == watch.Domain &&
				w.Address == watch.Address &&
				w.BigEndian == watch.BigEndian);
		}

		public void CopyTo(Watch[] array, int arrayIndex)
		{
			_watchList.CopyTo(array, arrayIndex);
		}

		public int IndexOf(Watch watch)
		{
			return _watchList.IndexOf(watch);
		}

		public void RemoveAt(int index)
		{
			_watchList.RemoveAt(index);
			Changes = true;
		}

		#region File handling logic - probably needs to be its own class

		public bool Load(string path, bool append)
		{
			var result = LoadFile(path, append);

			if (result)
			{
				if (append)
				{
					Changes = true;
				}
				else
				{
					CurrentFileName = path;
					Changes = false;
				}
			}

			return result;
		}

		public void Reload()
		{
			if (!string.IsNullOrWhiteSpace(CurrentFileName))
			{
				LoadFile(CurrentFileName, append: false);
				Changes = false;
			}
		}

		public bool Save()
		{
			if (string.IsNullOrWhiteSpace(CurrentFileName))
			{
				return false;
			}

			using (var sw = new StreamWriter(CurrentFileName))
			{
				var sb = new StringBuilder();
				sb
					.Append("Domain ").AppendLine(_domain.Name)
					.Append("SystemID ").AppendLine(_systemid);

				foreach (var watch in _watchList)
				{
					sb
						.Append(string.Format(AddressFormatStr, watch.Address ?? 0)).Append('\t')
						.Append(watch.SizeAsChar).Append('\t')
						.Append(watch.TypeAsChar).Append('\t')
						.Append(watch.BigEndian ? '1' : '0').Append('\t')
						.Append(watch.DomainName).Append('\t')
						.Append(watch.Notes)
						.AppendLine();
				}

				sw.WriteLine(sb.ToString());
			}

			Global.Config.RecentWatches.Add(CurrentFileName);
			Changes = false;
			return true;
		}

		public bool SaveAs(FileInfo file)
		{
			if (file != null)
			{
				CurrentFileName = file.FullName;
				return Save();
			}
			
			return false;
		}

		private bool LoadFile(string path, bool append)
		{
			var domain = string.Empty;
			var file = new FileInfo(path);
			if (file.Exists == false)
			{
				return false;
			}

			var isBizHawkWatch = true; // Hack to support .wch files from other emulators
			var isOldBizHawkWatch = false;
			using (var sr = file.OpenText())
			{
				string line;

				if (!append)
				{
					Clear();
				}

				while ((line = sr.ReadLine()) != null)
				{
					// .wch files from other emulators start with a number representing the number of watch, that line can be discarded here
					// Any properly formatted line couldn't possibly be this short anyway, this also takes care of any garbage lines that might be in a file
					if (line.Length < 5)
					{
						isBizHawkWatch = false;
						continue;
					}

					if (line.Length >= 6 && line.Substring(0, 6) == "Domain")
					{
						domain = line.Substring(7, line.Length - 7);
						isBizHawkWatch = true;
					}

					if (line.Length >= 8 && line.Substring(0, 8) == "SystemID")
					{
						continue;
					}

					var numColumns = line.HowMany('\t');
					int startIndex;
					if (numColumns == 5)
					{
						// If 5, then this is a post 1.0.5 .wch file
						if (isBizHawkWatch)
						{
							// Do nothing here
						}
						else
						{
							startIndex = line.IndexOf('\t') + 1;
							line = line.Substring(startIndex, line.Length - startIndex);   // 5 digit value representing the watch position number
						}
					}
					else if (numColumns == 4)
					{
						isOldBizHawkWatch = true; // This supports the legacy .wch format from 1.0.5 and earlier
					}
					else 
					{
						continue;   // If not 4, something is wrong with this line, ignore it
					}

					// Temporary, rename if kept
					int addr;
					var memDomain = _memoryDomains.MainMemory;

					var temp = line.Substring(0, line.IndexOf('\t'));
					try
					{
						addr = int.Parse(temp, NumberStyles.HexNumber);
					}
					catch
					{
						continue;
					}

					startIndex = line.IndexOf('\t') + 1;
					line = line.Substring(startIndex, line.Length - startIndex);   // Type
					var size = Watch.SizeFromChar(line[0]);

					startIndex = line.IndexOf('\t') + 1;
					line = line.Substring(startIndex, line.Length - startIndex);   // Signed
					var type = Watch.DisplayTypeFromChar(line[0]);

					startIndex = line.IndexOf('\t') + 1;
					line = line.Substring(startIndex, line.Length - startIndex);   // Endian
					try
					{
						startIndex = short.Parse(line[0].ToString());
					}
					catch
					{
						continue;
					}

					var bigEndian = startIndex != 0;

					if (isBizHawkWatch && !isOldBizHawkWatch)
					{
						startIndex = line.IndexOf('\t') + 1;
						line = line.Substring(startIndex, line.Length - startIndex);   // Domain
						temp = line.Substring(0, line.IndexOf('\t'));
						memDomain = _memoryDomains[temp] ?? _memoryDomains.MainMemory;
					}

					startIndex = line.IndexOf('\t') + 1;
					var notes = line.Substring(startIndex, line.Length - startIndex);

					_watchList.Add(
						Watch.GenerateWatch(
							memDomain,
							addr,
							size,
							type,
							notes,
							bigEndian));
					_domain = _memoryDomains[domain];
				}

				Domain = _memoryDomains[domain] ?? _memoryDomains.MainMemory;
				_currentFilename = path;
			}

			if (!append)
			{
				Global.Config.RecentWatches.Add(path);
				Changes = false;
			}
			else
			{
				Changes = true;
			}

			return true;
		}

		#endregion
	}
}
