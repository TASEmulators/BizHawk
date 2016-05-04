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
		private static readonly WatchValueComparer valueComparer = new WatchValueComparer();
		private static readonly WatchPreviousValueComparer previousValueComparer = new WatchPreviousValueComparer();
		private static readonly WatchValueDifferenceComparer valueDifferenceComparer = new WatchValueDifferenceComparer();
		private static readonly WatchChangeCountComparer changeCountComparer = new WatchChangeCountComparer();
		private static readonly WatchNoteComparer noteComparer = new WatchNoteComparer();

		private IMemoryDomains _memoryDomains;

		private List<Watch> _watchList = new List<Watch>(0);
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
		public WatchList(IMemoryDomains core, string systemid)
		{
			_memoryDomains = core;
			_systemid = systemid;
		}

		#endregion

		#region Methods

		#region ICollection<Watch>

		/// <summary>
		/// Adds a <see cref="Watch"/> into the current collection
		/// </summary>
		/// <param name="watch"><see cref="Watch"/> to add</param>
		public void Add(Watch watch)
		{
			_watchList.Add(watch);
			Changes = true;
		}

		/// <summary>
		/// Removes all item from the current collection
		/// Clear also the file name
		/// </summary>
		public void Clear()
		{
			_watchList.Clear();
			Changes = false;
			_currentFilename = string.Empty;
		}

		/// <summary>
		/// Determines if the current <see cref="WatchList"/> contains the
		/// specified <see cref="Watch"/>
		/// </summary>
		/// <param name="watch">The object to</param>
		/// <returns></returns>
		public bool Contains(Watch watch)
		{
			return _watchList.Contains(watch);
		}

		/// <summary>
		/// Copies the elements of the current <see cref="WatchList"/>
		/// into an <see cref="Array"/> starting at a particular <see cref="Array"/> index
		/// </summary>
		/// <param name="array">The one-dimension <see cref="Array"/> that will serve as destination to copy</param>
		/// <param name="arrayIndex">Zero-based index where the copy should starts</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="ArgumentException"></exception>
		public void CopyTo(Watch[] array, int arrayIndex)
		{
			_watchList.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Removes the first of specified <see cref="Watch"/>
		/// </summary>
		/// <param name="watch"><see cref="Watch"/> to remove</param>
		/// <returns>True if <see cref="Watch"/> successfully removed; otherwise, false</returns>
		public bool Remove(Watch watch)
		{
			bool result = _watchList.Remove(watch);
			if (result)
			{
				Changes = true;
			}

			return result;
		}

		#endregion

		#region IList<Watch>

		/// <summary>
		/// Determines the zero-base position of the specified <see cref="Watch"/>
		/// into the <see cref="WatchList"/>
		/// </summary>
		/// <param name="watch"><see cref="Watch"/> to look for</param>
		/// <returns>Zero-base position if <see cref="Watch"/> has been found; otherwise -1</returns>
		public int IndexOf(Watch watch)
		{
			return _watchList.IndexOf(watch);
		}

		/// <summary>
		/// Insert a <see cref="Watch"/> at the specified index
		/// </summary>
		/// <param name="index">The zero-base index where the <see cref="Watch"/> should be inserted</param>
		/// <param name="watch"><see cref="Watch"/> to insert</param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public void Insert(int index, Watch watch)
		{
			_watchList.Insert(index, watch);
		}

		/// <summary>
		/// Removes item at the specified index
		/// </summary>
		/// <param name="index">Zero-based index of the <see cref="Watch"/> to remove</param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public void RemoveAt(int index)
		{
			_watchList.RemoveAt(index);
			Changes = true;
		}

		#endregion IList<Watch>

		#region IEnumerable<Watch>

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An <see cref="IEnumerator{T}"/> for the current collection</returns>
		public IEnumerator<Watch> GetEnumerator()
		{
			return _watchList.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An <see cref="IEnumerator"/> for the current collection</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion IEnumerable<Watch>		

		/// <summary>
		/// Add an existing collection of <see cref="Watch"/> into the current one
		/// <see cref="Watch"/> equality will be checked to avoid doubles
		/// </summary>
		/// <param name="watches"><see cref="IEnumerable{Watch}"/> of watch to merge</param>
		public void AddRange(IEnumerable<Watch> watches)
		{
			Parallel.ForEach<Watch>(watches, watch =>
			{
				if (!_watchList.Contains(watch))
				{
					_watchList.Add(watch);
				}
			});
			Changes = true;
		}

		/// <summary>
		/// Clears change count of all <see cref="Watch"/> in the collection
		/// </summary>
		public void ClearChangeCounts()
		{
			Parallel.ForEach<Watch>(_watchList, watch => watch.ClearChangeCount());
		}

		/// <summary>
		/// Sort the current list based on one of the constant
		/// </summary>
		/// <param name="column">Value that specify sorting base</param>
		/// <param name="reverse">Value that define the ordering. Ascending (true) or desceding (false)</param>
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
						_watchList.Sort(valueComparer);
						_watchList.Reverse();
					}
					else
					{
						_watchList.Sort(valueComparer);
					}

					break;

				case PREV:
					if (reverse)
					{
						_watchList.Sort(previousValueComparer);
						_watchList.Reverse();
					}
					else
					{
						_watchList.Sort(previousValueComparer);
					}

					break;

				case DIFF:
					if (reverse)
					{
						_watchList.Sort(valueDifferenceComparer);
						_watchList.Reverse();
					}
					else
					{
						_watchList.Sort(valueDifferenceComparer);
					}
					break;

				case CHANGES:
					if (reverse)
					{
						_watchList.Sort(changeCountComparer);
						_watchList.Reverse();
					}
					else
					{
						_watchList.Sort(changeCountComparer);
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
						_watchList.Sort(noteComparer);
						_watchList.Reverse();
					}
					else
					{
						_watchList.Sort(noteComparer);
					}

					break;
			}
		}

		/// <summary>
		/// Sets WatchList's domain list to a new one
		/// <see cref="Watch"/> domain will also be refreshed
		/// </summary>
		/// <param name="core">New domains</param>
		public void RefreshDomains(IMemoryDomains core)
		{
			_memoryDomains = core;
			Parallel.ForEach<Watch>(_watchList, watch =>
			{
				if (watch.IsSeparator)
					return;
				watch.Domain = core[watch.Domain.Name];
				watch.ResetPrevious();
				watch.Update();
				watch.ClearChangeCount();
			}
			);
		}

		/// <summary>
		/// Updates all <see cref="Watch"/> ine the current collection
		/// </summary>
		public void UpdateValues()
		{
			Parallel.ForEach<Watch>(_watchList, watch =>
			{
				watch.Update();
			});
		}

		#endregion

		#region Propeties

		#region ICollection<Watch>

		/// <summary>
		/// Gets the number of elements contained in this <see cref="WatchList"/>
		/// </summary>
		public int Count
		{
			get
			{
				return _watchList.Count;
			}
		}

		/// <summary>
		/// <see cref="WatchList"/> is alsways read-write
		/// so this value will be always false
		/// </summary>
		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		#endregion ICollection<Watch>

		#region IList<Watch>

		/// <summary>
		/// Gets or sets element at the specified index
		/// </summary>
		/// <param name="index">The zero based index of the element you want to get or set</param>
		/// <returns><see cref="Watch"/> at the specified index</returns>
		public Watch this[int index]
		{
			get
			{
				return _watchList[index];
			}
			set
			{
				_watchList[index] = value;
			}
		}

		#endregion IList<Watch>

		/// <summary>
		/// Gets a value indicating if collection has changed or not
		/// </summary>
		public bool Changes { get; set; }

		/// <summary>
		/// Gets or sets current <see cref="WatchList"/>'s filename
		/// </summary>
		public string CurrentFileName
		{
			get
			{
				return _currentFilename;
			}
			set
			{
				_currentFilename = value;
			}
		}

		/// <summary>
		/// Gets the number of <see cref="Watch"/> that are not <see cref="SeparatorWatch"/>
		/// </summary>
		public int WatchCount
		{
			get
			{
				return _watchList.Count<Watch>(watch => !watch.IsSeparator);
			}
		}

		#endregion

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
				sb.Append("SystemID ").AppendLine(_systemid);

				foreach (var watch in _watchList)
				{
					sb.AppendLine(watch.ToString());
				}

				sw.WriteLine(sb.ToString());
			}

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
			var file = new FileInfo(path);
			if (file.Exists == false)
			{
				return false;
			}

			var isBizHawkWatch = true; // Hack to support .wch files from other emulators
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

					if (isBizHawkWatch)
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
				}

				_currentFilename = path;
			}

			if (!append)
			{
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
