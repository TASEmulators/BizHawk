using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public class WatchList : IEnumerable<Watch>
	{
		private string _currentFilename = "";

		public enum WatchPrevDef { LastSearch, Original, LastFrame, LastChange };

		private List<Watch> _watchList = new List<Watch>();
		private MemoryDomain _domain;

		public WatchList(MemoryDomain domain)
		{
			_domain = domain;
		}

		public IEnumerator<Watch> GetEnumerator()
		{
			return _watchList.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public int Count
		{
			get { return _watchList.Count; }
		}

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

		public int WatchCount
		{
			get
			{
				return _watchList.Count(w => !w.IsSeparator);
			}
		}

		public int ItemCount
		{
			get
			{
				return _watchList.Count;
			}
		}

		public void OrderWatches(string column, bool reverse)
		{
			switch (column)
			{
				case RamWatch.ADDRESS:
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
				case RamWatch.VALUE:
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
				case RamWatch.PREV: //Note: these only work if all entries are detailed objects!
					if (reverse)
					{
						_watchList = _watchList
							.OrderByDescending(x => (x as IWatchDetails).PreviousStr)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}
					else
					{
						_watchList = _watchList
							.OrderBy(x => (x as IWatchDetails).PreviousStr)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}
					break;
				case RamWatch.DIFF:
					if (reverse)
					{
						_watchList = _watchList
							.OrderByDescending(x => (x as IWatchDetails).Diff)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}
					else
					{
						_watchList = _watchList
							.OrderBy(x => (x as IWatchDetails).Diff)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}
					break;
				case RamWatch.CHANGES:
					if (reverse)
					{
						_watchList = _watchList
							.OrderByDescending(x => (x as IWatchDetails).ChangeCount)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}
					else
					{
						_watchList = _watchList
							.OrderBy(x => (x as IWatchDetails).ChangeCount)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}
					break;
				case RamWatch.DOMAIN:
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
				case RamWatch.NOTES:
					if (reverse)
					{
						_watchList = _watchList
							.OrderByDescending(x => (x as IWatchDetails).Notes)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}
					else
					{
						_watchList = _watchList
							.OrderBy(x => (x as IWatchDetails).Notes)
							.ThenBy(x => x.Address ?? 0)
							.ThenBy(x => x.Size)
							.ThenBy(x => x.Type)
							.ToList();
					}
					break;
			}
		}

		public string AddressFormatStr
		{
			get
			{
				if (_domain != null)
				{
					return "{0:X" + IntHelpers.GetNumDigits(_domain.Size - 1).ToString() + "}";
				}
				else
				{
					return "";
				}
			}
		}

		public void Clear()
		{
			_watchList.Clear();
			Changes = false;
			_currentFilename = "";
		}

		public MemoryDomain Domain { get { return _domain; } set { _domain = value; } }

		public void UpdateValues()
		{
			var detailedWatches = _watchList.OfType<IWatchDetails>().ToList();
			foreach (var watch in detailedWatches)
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

		public void Remove(Watch watch)
		{
			_watchList.Remove(watch);
			Changes = true;
		}

		public void Insert(int index, Watch watch)
		{
			_watchList.Insert(index, watch);
		}

		public void ClearChangeCounts()
		{
			var detailedWatches = _watchList.OfType<IWatchDetails>().ToList();
			foreach (var watch in detailedWatches)
			{
				watch.ClearChangeCount();
			}
		}

		#region File handling logic - probably needs to be its own class

		public string CurrentFileName { get { return _currentFilename; } set { _currentFilename = value; } }
		public bool Changes { get; set; }

		public bool Save()
		{
			bool result;
			if (!String.IsNullOrWhiteSpace(CurrentFileName))
			{
				result = SaveFile();
			}
			else
			{
				result = SaveAs();
			}

			if (result)
			{
				Changes = false;
			}

			return result;
		}

		public bool Load(string path, bool details, bool append)
		{
			bool result = LoadFile(path, details, append);

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
			if (!String.IsNullOrWhiteSpace(CurrentFileName))
			{
				LoadFile(CurrentFileName, true, false);
				Changes = false;
			}
		}

		private bool SaveFile()
		{
			if (String.IsNullOrWhiteSpace(CurrentFileName))
			{
				return false;
			}

			using (StreamWriter sw = new StreamWriter(CurrentFileName))
			{
				StringBuilder sb = new StringBuilder();
				sb
					.Append("Domain ").AppendLine(_domain.Name)
					.Append("SystemID ").AppendLine(Global.Emulator.SystemId);

				foreach (Watch w in _watchList)
				{
					sb
						.Append(String.Format(AddressFormatStr, w.Address)).Append('\t')
						.Append(w.SizeAsChar).Append('\t')
						.Append(w.TypeAsChar).Append('\t')
						.Append(w.BigEndian ? '1' : '0').Append('\t')
						.Append(w.Domain.Name).Append('\t')
						.Append(w is IWatchDetails ? (w as IWatchDetails).Notes : String.Empty)
						.AppendLine();
				}

				sw.WriteLine(sb.ToString());
			}

			return true;
		}

		public bool SaveAs()
		{
			var file = WatchCommon.GetSaveFileFromUser(CurrentFileName);
			if (file != null)
			{
				CurrentFileName = file.FullName;
				return SaveFile();
			}
			else
			{
				return false;
			}
		}

		private bool LoadFile(string path, bool details, bool append)
		{
			string domain = "";
			var file = new FileInfo(path);
			if (file.Exists == false) return false;
			bool isBizHawkWatch = true; //Hack to support .wch files from other emulators
			bool isOldBizHawkWatch = false;
			using (StreamReader sr = file.OpenText())
			{
				string line;

				if (append == false)
				{
					Clear();
				}

				while ((line = sr.ReadLine()) != null)
				{
					//.wch files from other emulators start with a number representing the number of watch, that line can be discarded here
					//Any properly formatted line couldn't possibly be this short anyway, this also takes care of any garbage lines that might be in a file
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

					int numColumns = StringHelpers.HowMany(line, '\t');
					int startIndex;
					if (numColumns == 5)
					{
						//If 5, then this is a post 1.0.5 .wch file
						if (isBizHawkWatch)
						{
							//Do nothing here
						}
						else
						{
							startIndex = line.IndexOf('\t') + 1;
							line = line.Substring(startIndex, line.Length - startIndex);   //5 digit value representing the watch position number
						}
					}
					else if (numColumns == 4)
					{
						isOldBizHawkWatch = true;
					}
					else //4 is 1.0.5 and earlier
					{
						continue;   //If not 4, something is wrong with this line, ignore it
					}



					//Temporary, rename if kept
					int addr;
					bool bigEndian;
					MemoryDomain memDomain = Global.Emulator.MainMemory;

					string temp = line.Substring(0, line.IndexOf('\t'));
					try
					{
						addr = Int32.Parse(temp, NumberStyles.HexNumber);
					}
					catch
					{
						continue;
					}

					startIndex = line.IndexOf('\t') + 1;
					line = line.Substring(startIndex, line.Length - startIndex);   //Type
					Watch.WatchSize size = Watch.SizeFromChar(line[0]);


					startIndex = line.IndexOf('\t') + 1;
					line = line.Substring(startIndex, line.Length - startIndex);   //Signed
					Watch.DisplayType type = Watch.DisplayTypeFromChar(line[0]);

					startIndex = line.IndexOf('\t') + 1;
					line = line.Substring(startIndex, line.Length - startIndex);   //Endian
					try
					{
						startIndex = Int16.Parse(line[0].ToString());
					}
					catch
					{
						continue;
					}
					if (startIndex == 0)
					{
						bigEndian = false;
					}
					else
					{
						bigEndian = true;
					}

					if (isBizHawkWatch && !isOldBizHawkWatch)
					{
						startIndex = line.IndexOf('\t') + 1;
						line = line.Substring(startIndex, line.Length - startIndex);   //Domain
						temp = line.Substring(0, line.IndexOf('\t'));
						memDomain = Global.Emulator.MemoryDomains[GetDomainPos(temp)];
					}

					startIndex = line.IndexOf('\t') + 1;
					string notes = line.Substring(startIndex, line.Length - startIndex);

					Watch w = Watch.GenerateWatch(memDomain, addr, size, details);
					w.BigEndian = bigEndian;
					w.Type = type;
					if (w is IWatchDetails)
					{
						(w as IWatchDetails).Notes = notes;
					}

					_watchList.Add(w);
					_domain = Global.Emulator.MemoryDomains[GetDomainPos(domain)];
				}
			}

			return true;
		}

		private static int GetDomainPos(string name)
		{
			//Attempts to find the memory domain by name, if it fails, it defaults to index 0
			for (int x = 0; x < Global.Emulator.MemoryDomains.Count; x++)
			{
				if (Global.Emulator.MemoryDomains[x].Name == name)
					return x;
			}
			return 0;
		}

		public static FileInfo GetFileFromUser(string currentFile)
		{
			var ofd = new OpenFileDialog();
			if (currentFile.Length > 0)
				ofd.FileName = Path.GetFileNameWithoutExtension(currentFile);
			ofd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.WatchPath, null);
			ofd.Filter = "Watch Files (*.wch)|*.wch|All Files|*.*";
			ofd.RestoreDirectory = true;

			Global.Sound.StopSound();
			var result = ofd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return null;
			var file = new FileInfo(ofd.FileName);
			return file;
		}

		public static FileInfo GetSaveFileFromUser(string currentFile)
		{
			var sfd = new SaveFileDialog();
			if (currentFile.Length > 0)
			{
				sfd.FileName = Path.GetFileNameWithoutExtension(currentFile);
				sfd.InitialDirectory = Path.GetDirectoryName(currentFile);
			}
			else if (!(Global.Emulator is NullEmulator))
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.WatchPath, null);
			}
			else
			{
				sfd.FileName = "NULL";
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.WatchPath, null);
			}
			sfd.Filter = "Watch Files (*.wch)|*.wch|All Files|*.*";
			sfd.RestoreDirectory = true;
			Global.Sound.StopSound();
			var result = sfd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return null;
			var file = new FileInfo(sfd.FileName);
			return file;
		}

		#endregion
	}
}
