using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BizHawk.Common.NumberExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;


namespace BizHawk.Client.Common
{
	/// <summary>
	/// This class hold a collection <see cref="Watch"/>
	/// Different memory domain can be mixed
	/// </summary>
	public sealed partial class WatchList
		: IList<Watch>
	{
		#region Fields

		public const string ADDRESS = "AddressColumn";
		public const string VALUE = "ValueColumn";
		public const string PREV = "PrevColumn";
		public const string CHANGES = "ChangesColumn";
		public const string DIFF = "DiffColumn";
		public const string DOMAIN = "DomainColumn";
		public const string NOTES = "NotesColumn";

		private static readonly WatchDomainComparer domainComparer = new WatchDomainComparer();
		private static readonly WatchAddressComparer addressComparer = new WatchAddressComparer();

		private static IMemoryDomains _memoryDomains;

		private List<Watch> _watchList = new List<Watch>(0);
		private MemoryDomain _domain;
		private string _currentFilename = string.Empty;
		private string _systemid;

		#endregion

		#region cTor(s)

		/// <summary>
		/// Initialize a new instance of <see cref="WatchList"/> that will
		/// contains a set of <see cref="Watch"/>
		/// </summary>
		/// <param name="core">All available memomry domains</param>
		/// <param name="domain">Domain you want to watch</param>
		/// <param name="systemid">System identifier (NES, SNES, ...)</param>
		[Obsolete("Use the constructor with two parameters instead")]
		public WatchList(IMemoryDomains core, MemoryDomain domain, string systemid)
		{
			if (_memoryDomains == null)
			{
				_memoryDomains = core;
			}
			_domain = domain;
			_systemid = systemid;
		}

		/// <summary>
		/// Initialize a new instance of <see cref="WatchList"/> that will
		/// contains a set of <see cref="Watch"/>
		/// </summary>
		/// <param name="core">All available memomry domains</param>
		/// <param name="domain">Domain you want to watch</param>
		/// <param name="systemid">System identifier (NES, SNES, ...)</param>
		public WatchList(IMemoryDomains core, string systemid)
		{
			if (_memoryDomains == null)
			{
				_memoryDomains = core;
			}
			//TODO: Remove this after tests
			_domain = core.MainMemory;
			_systemid = systemid;
		}

		#endregion

		#region Methods

		[Obsolete("Use the method with single parameter instead")]
		public void RefreshDomains(IMemoryDomains core, MemoryDomain domain)
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

		public void RefreshDomains(IMemoryDomains core)
		{
			_memoryDomains = core;
			Parallel.ForEach<Watch>(_watchList, watch =>
			{
				watch.Domain = core[watch.Domain.Name];
				watch.ResetPrevious();
				watch.Update();
				watch.ClearChangeCount();
			}
			);
		}

		#endregion

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
			get
			{
				return _watchList.Count;
			}
		}

		public int WatchCount
		{
			get
			{
				return _watchList.Count<Watch>(watch => !watch.IsSeparator);
			}
		}

		[Obsolete("Use count property instead")]
		public int ItemCount
		{
			get
			{
				return Count;
			}
		}

		[Obsolete("Use domain from individual watch instead")]
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

		IEnumerator IEnumerable.GetEnumerator()
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
						_watchList.Sort(addressComparer);
						_watchList.Reverse();
					}
					else
					{
						_watchList.Sort();
					}

					break;
				case VALUE:
					if (reverse)
					{
						_watchList = _watchList
							.OrderByDescending(x => x.Value)
							.ThenBy(x => x.Address)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ThenBy(x => x.BigEndian)
							.ToList();
					}
					else
					{
						_watchList = _watchList
							.OrderBy(x => x.Value)
							.ThenBy(x => x.Address)
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
							.ThenBy(x => x.Address)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}
					else
					{
						_watchList = _watchList
							.OrderBy(x => x.PreviousStr)
							.ThenBy(x => x.Address)
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
							.ThenBy(x => x.Address)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}
					else
					{
						_watchList = _watchList
							.OrderBy(x => x.Diff)
							.ThenBy(x => x.Address)
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
							.ThenBy(x => x.Address)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}
					else
					{
						_watchList = _watchList
							.OrderBy(x => x.ChangeCount)
							.ThenBy(x => x.Address)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}

					break;
				case DOMAIN:
					if (reverse)
					{
						_watchList.Sort(domainComparer);
						_watchList.Reverse();
					}
					else
					{
						_watchList.Sort(domainComparer);
					}

					break;
				case NOTES:
					if (reverse)
					{
						_watchList = _watchList
							.OrderByDescending(x => x.Notes)
							.ThenBy(x => x.Address)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}
					else
					{
						_watchList = _watchList
							.OrderBy(x => x.Notes)
							.ThenBy(x => x.Address)
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
			Parallel.ForEach<Watch>(_watchList, watch =>
			{
				watch.Update();
			});
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
					sb.AppendLine(watch.ToString());
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
							bigEndian,
							notes));
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
