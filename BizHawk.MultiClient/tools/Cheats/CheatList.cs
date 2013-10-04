using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public class NewCheatList : IEnumerable<NewCheat>
	{
		private List<NewCheat> _cheatList = new List<NewCheat>();
		private string _currentFileName = String.Empty;
		private bool _changes = false;

		public NewCheatList() { }

		public IEnumerator<NewCheat> GetEnumerator()
		{
			return _cheatList.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public NewCheat this[int index]
		{
			get { return _cheatList[index]; }
		}

		public int Count
		{
			get { return _cheatList.Count; }
		}

		public int CheatCount
		{
			get { return _cheatList.Count(x => !x.IsSeparator); }
		}

		public int ActiveCheatCount
		{
			get { return _cheatList.Count(x => x.Enabled); }
		}

		public void Update()
		{
			_cheatList.ForEach(x => x.Pulse());
		}

		public void Add(NewCheat c)
		{
			_changes = true;
			_cheatList.Add(c);
		}

		public void Remove(NewCheat c)
		{
			_changes = true;
			_cheatList.Remove(c);
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

		public void Freeze(MemoryDomain domain, int address, Watch.WatchSize size, int value, bool? bigendian = null)
		{
			var exists = _cheatList.Any(x => x.Domain == domain && x.Address == address && x.Size == size);
			if (!exists)
			{
				bool endian = false;
				if (bigendian.HasValue)
				{
					endian = bigendian.Value;
				}
				else
				{
					switch (domain.Endian)
					{
						default:
						case Endian.Unknown:
						case Endian.Little:
							bigendian = false;
							break;
						case Endian.Big:
							bigendian = true;
							break;
					}
				}

				Watch w = Watch.GenerateWatch(domain, address, size, Watch.DisplayType.Unsigned, String.Empty, endian);
				_cheatList.Add(new NewCheat(w, compare: null, enabled: true));
			}
		}

		public bool Save()
		{
			if (!String.IsNullOrWhiteSpace(_currentFileName))
			{
				return SaveFile();
			}
			else
			{
				return SaveAs();
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
				if (!append)
				{
					Clear();
				}

				string s;
				while ((s = sr.ReadLine()) != null)
				{
					try
					{
						int ADDR, VALUE, COMPARE;
						MemoryDomain DOMAIN;
						bool ENABLED;
						string NAME;

						if (s.Length < 6) continue;
						//NewCheat c = new NewCheat(
						string[] vals = s.Split('\t');
						ADDR = Int32.Parse(vals[0], NumberStyles.HexNumber);
						VALUE = Int32.Parse(vals[1], NumberStyles.HexNumber);
						COMPARE = Int32.Parse(vals[2], NumberStyles.HexNumber);
						DOMAIN = ToolHelpers.DomainByName(vals[3]);
						ENABLED = vals[4] == "1";
						NAME = vals[5];

						Watch w = Watch.GenerateWatch(
							DOMAIN,
							ADDR,
							Watch.WatchSize.Byte,
							Watch.DisplayType.Hex,
							NAME,
							false
						);

						NewCheat c = new NewCheat(w, COMPARE, ENABLED);
						_cheatList.Add(c);
					}
					catch
					{
						continue;
					}
				}
			}

			throw new NotImplementedException();
		}

		public string CurrentFileName
		{
			get { return _currentFileName; }
		}

		#region privates

		private bool SaveFile()
		{
			throw new NotImplementedException();
		}

		private bool SaveAs()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region File Handling

		public static FileInfo GetFileFromUser(string currentFile)
		{
			var ofd = new OpenFileDialog();
			if (!String.IsNullOrWhiteSpace(currentFile))
			{
				ofd.FileName = Path.GetFileNameWithoutExtension(currentFile);
			}
			ofd.InitialDirectory = PathManager.MakeAbsolutePath(
				Global.Config.PathEntries[Global.Emulator.SystemId, "Cheats"].Path,
				Global.Emulator.SystemId);
			ofd.Filter = "Cheat Files (*.cht)|*.cht|All Files|*.*";
			ofd.RestoreDirectory = true;

			Global.Sound.StopSound();
			var result = ofd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return null;
			var file = new FileInfo(ofd.FileName);
			return file;
		}

		#endregion
	}
}
