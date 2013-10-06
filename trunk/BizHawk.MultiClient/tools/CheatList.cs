using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public class LegacyCheatList : IEnumerable<LegacyCheat>
	{
		private List<LegacyCheat> cheatList = new List<LegacyCheat>();
		public string CurrentCheatFile = "";
		public bool Changes = false;
		public int Count { get { return cheatList.Count; } }

		public bool LoadCheatFile(string path, bool append)
		{
			var file = new FileInfo(path);
			if (file.Exists == false) return false;

			int cheatcount = 0;
			using (StreamReader sr = file.OpenText())
			{
				if (!append) CurrentCheatFile = path;

				string s;

				if (append == false)
				{
					Clear();	//Wipe existing list and read from file
				}
				while ((s = sr.ReadLine()) != null)
				{
					try
					{
						if (s.Length < 6) continue;
						LegacyCheat c = new LegacyCheat();
						string temp = s.Substring(0, s.IndexOf('\t'));
						c.Address = int.Parse(temp, NumberStyles.HexNumber);

						int y = s.IndexOf('\t') + 1;
						s = s.Substring(y, s.Length - y);   //Value
						temp = s.Substring(0, 2);
						c.Value = byte.Parse(temp, NumberStyles.HexNumber);

						bool comparefailed = false; //adelikat: This is a hack for 1.0.6 to support .cht files made in previous versions before the compare value was implemented
						y = s.IndexOf('\t') + 1;
						s = s.Substring(y, s.Length - y);   //Compare
						temp = s.Substring(0, s.IndexOf('\t'));
						try
						{
							if (temp == "N")
							{
								c.Compare = null;
							}
							else
							{
								c.Compare = byte.Parse(temp, NumberStyles.HexNumber);
							}
						}
						catch
						{
							comparefailed = true;
							c.Domain = SetDomain(temp);
						}

						if (!comparefailed)
						{
							y = s.IndexOf('\t') + 1;
							s = s.Substring(y, s.Length - y); //Memory Domain
							temp = s.Substring(0, s.IndexOf('\t'));
							c.Domain = SetDomain(temp);
						}

						y = s.IndexOf('\t') + 1;
						s = s.Substring(y, s.Length - y); //Enabled
						y = int.Parse(s[0].ToString());

						if (y == 0)
						{
							c.Disable();
						}
						else
						{
							c.Enable();
						}

						y = s.IndexOf('\t') + 1;
						s = s.Substring(y, s.Length - y); //Name
						c.Name = s;

						cheatcount++;
						cheatList.Add(c);
					}
					catch
					{
						continue;
					}
					
				}
			}

			if (Global.Config.DisableCheatsOnLoad)
			{
				foreach (LegacyCheat t in cheatList)
				{
					t.Disable();
				}
			}

			if (cheatcount > 0)
			{
				Changes = false;
				Global.Config.RecentCheats.Add(file.FullName);
				Global.MainForm.UpdateCheatStatus();
				return true;
			}
			else
			{
				return false;
			}
		}

		public void NotSupportedError()
		{
			MessageBox.Show("Unable to enable cheat for this platform, cheats are not supported for " + Global.Emulator.SystemId, "Cheat error",
							MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private MemoryDomain SetDomain(string name)
		{
			//Attempts to find the memory domain by name, if it fails, it defaults to index 0
			foreach (MemoryDomain t in Global.Emulator.MemoryDomains)
			{
				if (t.Name == name)
				{
					return t;
				}
			}
			return Global.Emulator.MemoryDomains[0];
		}

		public IEnumerator<LegacyCheat> GetEnumerator()
		{
			return cheatList.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public bool IsActiveCheat(MemoryDomain d, int address)
		{
			return cheatList.Any(t => t.Address == address && t.Domain.Name == d.Name);
		}

		public void Remove(MemoryDomain d, int address)
		{
			for (int x = 0; x < cheatList.Count; x++)
			{
				if (cheatList[x].Address == address && cheatList[x].Domain.Name == d.Name)
				{
					cheatList.Remove(cheatList[x]);
				}
			}

			Global.OSD.AddMessage("Cheat removed");
		}

		public void DisableAll()
		{
			foreach (LegacyCheat c in cheatList)
			{
				c.Disable();
			}
		}

		public string CheatsPath
		{
			get
			{
				PathEntry pathEntry = Global.Config.PathEntries[Global.Emulator.SystemId, "Cheats"];
				if (pathEntry == null)
				{
					pathEntry = Global.Config.PathEntries[Global.Emulator.SystemId, "Base"];
				}
				string path = pathEntry.Path;
				
				var f = new FileInfo(path);
				if (f.Directory != null && f.Directory.Exists == false)
				{
					f.Directory.Create();
				}

				return path;
			}
		}

		public bool SaveCheatFile(string path)
		{
			FileInfo file = new FileInfo(path);
			if (file.Directory != null && !file.Directory.Exists)
			{
				file.Directory.Create();
			}

			using (StreamWriter sw = new StreamWriter(path))
			{
				string str = "";

				foreach (LegacyCheat t in cheatList)
				{
					str += FormatAddress(t.Address) + "\t";
					str += String.Format("{0:X2}", t.Value) + "\t";
					
					if (t.Compare == null)
					{
						str += "N\t";
					}
					else
					{
						str += String.Format("{0:X2}", t.Compare) + "\t";
					}
					
					str += t.Domain.Name + "\t";
					
					if (t.IsEnabled)
					{
						str += "1\t";
					}
					else
					{
						str += "0\t";
					}
					
					str += t.Name + "\n";
				}

				sw.WriteLine(str);
			}
			Changes = false;
			return true;
		}

		public string FormatAddress(int address)
		{
			return String.Format("{0:X" + GetNumDigits((Global.Emulator.MainMemory.Size - 1)).ToString() + "}", address);
		}


		public void SaveSettings()
		{
			if (Global.Config.CheatsAutoSaveOnClose)
			{
				if (Changes && cheatList.Count > 0)
				{
					if (CurrentCheatFile.Length == 0)
						CurrentCheatFile = DefaultFilename;

					SaveCheatFile(Global.CheatList_Legacy.CurrentCheatFile);
				}
				else if (cheatList.Count == 0 && CurrentCheatFile.Length > 0)
				{
					var file = new FileInfo(CurrentCheatFile);
					file.Delete();
				}
			}
		}

		public string DefaultFilename
		{
			get
			{
				return Path.Combine(Global.CheatList_Legacy.CheatsPath, PathManager.FilesystemSafeName(Global.Game) + ".cht");
			}
		}

		private int GetNumDigits(Int32 i)
		{
			if (i < 0x10000) return 4;
			if (i < 0x1000000) return 6;
			else return 8;
		}

		/// <summary>
		/// Looks for a .cht file that matches the name of the ROM loaded
		/// It is up to the caller to determine which directory it looks
		/// </summary>
		public bool AttemptLoadCheatFile()
		{
			string CheatFile = DefaultFilename;
			var file = new FileInfo(CheatFile);
			
			if (file.Exists == false)
			{
				return false;
			}
			else
			{
				bool loaded = LoadCheatFile(CheatFile, false);
				if (loaded)
				{
					Global.MainForm.UpdateCheatStatus();
				}
				return loaded;
			}
		}

		public void Clear()
		{
			cheatList.Clear();
			MemoryPulse.Clear();
			Global.MainForm.UpdateCheatStatus();
		}

		public void Remove(LegacyCheat c)
		{
			c.DisposeOfCheat();
			cheatList.Remove(c);
			Global.MainForm.UpdateCheatStatus();
		}

		public void RemoveCheat(MemoryDomain domain, int address)
		{
			for (int x = 0; x < cheatList.Count; x++)
			{
				if (cheatList[x].Domain == domain && cheatList[x].Address == address)
				{
					MemoryPulse.Remove(domain, address);
					cheatList.Remove(cheatList[x]);
				}
			}
		}

		public void Add(LegacyCheat c)
		{
			if (c != null)
			{
				cheatList.Add(c);
				Global.MainForm.UpdateCheatStatus();
			}
		}

		public LegacyCheat this[int index]
		{
			get
			{
				return cheatList[index];
			}
		}

		public void Insert(int index, LegacyCheat item)
		{
			cheatList.Insert(index, item);
			Global.MainForm.UpdateCheatStatus();
		}

		public bool HasActiveCheats
		{
			get { return cheatList.Any(t => t.IsEnabled); }
		}

		public int ActiveCheatCount
		{
			get
			{
				if (HasActiveCheats)
				{
					return cheatList.Where(x => x.IsEnabled).Count();
				}
				else
				{
					return 0;
				}
			}
		}
	}
}
