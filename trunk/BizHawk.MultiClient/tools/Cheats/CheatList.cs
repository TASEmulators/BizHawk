using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public class CheatList : IEnumerable<Cheat>
	{
		private List<Cheat> _cheatList = new List<Cheat>();
		private string _currentFileName = String.Empty;
		private bool _changes = false;

		public CheatList() { }

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
			var file = new FileInfo(GenerateDefaultFilename());

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

		public void NewList()
		{
			_cheatList.Clear();
			_currentFileName = String.Empty;
			_changes = false;
			Global.MainForm.UpdateCheatStatus();
		}

		public void Update()
		{
			_cheatList.ForEach(x => x.Pulse());
		}

		public void Add(Cheat c)
		{
			_changes = true;
			_cheatList.Add(c);
			Global.MainForm.UpdateCheatStatus();
		}

		public void Insert(int index, Cheat c)
		{
			_changes = true;
			_cheatList.Insert(index, c);
			Global.MainForm.UpdateCheatStatus();
		}

		public void Remove(Cheat c)
		{
			_changes = true;
			_cheatList.Remove(c);
			Global.MainForm.UpdateCheatStatus();
		}

		public void RemoveRange(IEnumerable<Cheat> cheats)
		{
			_changes = true;
			foreach (var cheat in cheats)
			{
				_cheatList.Remove(cheat);
			}
			Global.MainForm.UpdateCheatStatus();
		}

		public bool Changes
		{
			get { return _changes; }
		}

		public void Clear()
		{
			_changes = true;
			_cheatList.Clear();
			Global.MainForm.UpdateCheatStatus();
		}

		public void DisableAll()
		{
			_changes = true;
			_cheatList.ForEach(x => x.Disable());
			Global.MainForm.UpdateCheatStatus();
		}

		public void EnableAll()
		{
			_changes = true;
			_cheatList.ForEach(x => x.Enable());
			Global.MainForm.UpdateCheatStatus();
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
						_currentFileName = GenerateDefaultFilename();
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
				_currentFileName = GenerateDefaultFilename();
			}

			return SaveFile(_currentFileName);
		}

		public bool SaveAs()
		{
			var file = GetSaveFileFromUser();
			if (file != null)
			{
				return SaveFile(file.FullName);
			}
			else
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
							int ADDR, VALUE;
							int? COMPARE;
							MemoryDomain DOMAIN;
							bool ENABLED;
							string NAME;
							Watch.WatchSize SIZE = Watch.WatchSize.Byte;
							Watch.DisplayType TYPE = Watch.DisplayType.Hex;
							bool BIGENDIAN = false;


							if (s.Length < 6) continue;
							//NewCheat c = new NewCheat(
							string[] vals = s.Split('\t');
							ADDR = Int32.Parse(vals[0], NumberStyles.HexNumber);
							VALUE = Int32.Parse(vals[1], NumberStyles.HexNumber);

							if (vals[2] == "N")
							{
								COMPARE = null;
							}
							else
							{
								COMPARE = Int32.Parse(vals[2], NumberStyles.HexNumber);
							}
							DOMAIN = ToolHelpers.DomainByName(vals[3]);
							ENABLED = vals[4] == "1";
							NAME = vals[5];

							//For backwards compatibility, don't assume these values exist
							if (vals.Length > 6)
							{
								SIZE = Watch.SizeFromChar(vals[6][0]);
								TYPE = Watch.DisplayTypeFromChar(vals[7][0]);
								BIGENDIAN = vals[8] == "1";
							}

							Watch w = Watch.GenerateWatch(
								DOMAIN,
								ADDR,
								SIZE,
								TYPE,
								NAME,
								BIGENDIAN
							);

							Cheat c = new Cheat(w, VALUE, COMPARE, Global.Config.DisableCheatsOnLoad ? false : ENABLED);
							_cheatList.Add(c);
						}
					}
					catch
					{
						continue;
					}
				}
			}

			Global.MainForm.UpdateCheatStatus();
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
				case NewCheatForm.NAME:
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
				case NewCheatForm.ADDRESS:
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
				case NewCheatForm.VALUE:
					if (reverse)
					{
						_cheatList = _cheatList
							.OrderByDescending(x => x.Value ?? 0)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address.Value)
							.ToList();
					}
					else
					{
						_cheatList = _cheatList
							.OrderBy(x => x.Value ?? 0)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address.Value)
							.ToList();
					}
					break;
				case NewCheatForm.COMPARE:
					if (reverse)
					{
						_cheatList = _cheatList
							.OrderByDescending(x => x.Compare ?? 0)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address.Value)
							.ToList();
					}
					else
					{
						_cheatList = _cheatList
							.OrderBy(x => x.Compare ?? 0)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address.Value)
							.ToList();
					}
					break;
				case NewCheatForm.ON:
					if (reverse)
					{
						_cheatList = _cheatList
							.OrderByDescending(x => x.Enabled)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address.Value)
							.ToList();
					}
					else
					{
						_cheatList = _cheatList
							.OrderBy(x => x.Enabled)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address.Value)
							.ToList();
					}
					break;
				case NewCheatForm.DOMAIN:
					if (reverse)
					{
						_cheatList = _cheatList
							.OrderByDescending(x => x.Domain)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address.Value)
							.ToList();
					}
					else
					{
						_cheatList = _cheatList
							.OrderBy(x => x.Domain)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address.Value)
							.ToList();
					}
					break;
				case NewCheatForm.SIZE:
					if (reverse)
					{
						_cheatList = _cheatList
							.OrderByDescending(x => ((int)x.Size))
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address.Value)
							.ToList();
					}
					else
					{
						_cheatList = _cheatList
							.OrderBy(x => ((int)x.Size))
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address.Value)
							.ToList();
					}
					break;
				case NewCheatForm.ENDIAN:
					if (reverse)
					{
						_cheatList = _cheatList
							.OrderByDescending(x => x.BigEndian)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address.Value)
							.ToList();
					}
					else
					{
						_cheatList = _cheatList
							.OrderBy(x => x.BigEndian)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address.Value)
							.ToList();
					}
					break;
				case NewCheatForm.TYPE:
					if (reverse)
					{
						_cheatList = _cheatList
							.OrderByDescending(x => x.Type)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address.Value)
							.ToList();
					}
					else
					{
						_cheatList = _cheatList
							.OrderBy(x => x.Type)
							.ThenBy(x => x.Name)
							.ThenBy(x => x.Address.Value)
							.ToList();
					}
					break;
			}
		}

		#region privates

		private string GenerateDefaultFilename()
		{
			PathEntry pathEntry = Global.Config.PathEntries[Global.Emulator.SystemId, "Cheats"];
			if (pathEntry == null)
			{
				pathEntry = Global.Config.PathEntries[Global.Emulator.SystemId, "Base"];
			}
			string path = PathManager.MakeAbsolutePath(pathEntry.Path, Global.Emulator.SystemId);

			var f = new FileInfo(path);
			if (f.Directory != null && f.Directory.Exists == false)
			{
				f.Directory.Create();
			}

			return Path.Combine(path, PathManager.FilesystemSafeName(Global.Game) + ".cht");
		}

		private bool SaveFile(string path)
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
							Watch.DisplayType type = cheat.Type;
							cheat.SetType(Watch.DisplayType.Hex);

							sb
								.Append(cheat.AddressStr).Append('\t')
								.Append(cheat.ValueStr).Append('\t')
								.Append(cheat.Compare.HasValue ? cheat.Compare.Value : 'N').Append('\t')
								.Append(cheat.Domain != null ? cheat.Domain.Name : String.Empty).Append('\t')
								.Append(cheat.Enabled ? '1' : '0').Append('\t')
								.Append(cheat.Name)
								.Append(cheat.SizeAsChar).Append('\t')
								.Append(cheat.SizeAsChar).Append('\t')
								.Append(cheat.BigEndian.Value ? '1' : '0').Append('\t')
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

		#endregion

		#region File Handling

		public static FileInfo GetFileFromUser(string currentFile)
		{
			var ofd = new OpenFileDialog();
			if (!String.IsNullOrWhiteSpace(currentFile))
			{
				ofd.FileName = Path.GetFileNameWithoutExtension(currentFile);
			}
			ofd.InitialDirectory = PathManager.GetCheatsPath(Global.Game);
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

		private FileInfo GetSaveFileFromUser()
		{
			var sfd = new SaveFileDialog();
			if (!String.IsNullOrWhiteSpace(_currentFileName))
			{
				sfd.FileName = Path.GetFileNameWithoutExtension(Global.CheatList_Legacy.CurrentCheatFile);
			}
			else if (!(Global.Emulator is NullEmulator))
			{
				sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
			}
			sfd.InitialDirectory = PathManager.GetCheatsPath(Global.Game);
			sfd.Filter = "Cheat Files (*.cht)|*.cht|All Files|*.*";
			sfd.RestoreDirectory = true;
			Global.Sound.StopSound();
			var result = sfd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
			{
				return null;
			}

			var file = new FileInfo(sfd.FileName);
			Global.Config.LastRomPath = file.DirectoryName;
			return file;
		}

		#endregion
	}
}
