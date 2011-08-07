using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public class CheatList
	{
		public List<Cheat> cheatList = new List<Cheat>();
		public string currentCheatFile = "";
		public bool changes = false;

		public bool LoadCheatFile(string path, bool append)
		{
			int y;
			var file = new FileInfo(path);
			if (file.Exists == false) return false;

			using (StreamReader sr = file.OpenText())
			{
				if (!append) currentCheatFile = path;

				string s = "";
				string temp = "";

				if (append == false)
				{
					Clear();	//Wipe existing list and read from file
				}

				while ((s = sr.ReadLine()) != null)
				{
					if (s.Length < 6) continue;
					Cheat c = new Cheat();
					temp = s.Substring(0, s.IndexOf('\t'));   //Address
					c.address = int.Parse(temp, NumberStyles.HexNumber);

					y = s.IndexOf('\t') + 1;
					s = s.Substring(y, s.Length - y);   //Value
					temp = s.Substring(0, 2);
					c.value = byte.Parse(temp, NumberStyles.HexNumber);

					y = s.IndexOf('\t') + 1;
					s = s.Substring(y, s.Length - y); //Memory Domain
					temp = s.Substring(0, s.IndexOf('\t'));
					c.domain = SetDomain(temp);

					y = s.IndexOf('\t') + 1;
					s = s.Substring(y, s.Length - y); //Enabled
					y = int.Parse(s[0].ToString());

					try
					{
						if (y == 0)
							c.Disable();
						else
							c.Enable();
					}
					catch
					{
						NotSupportedError();
					}

					y = s.IndexOf('\t') + 1;
					s = s.Substring(y, s.Length - y); //Name
					c.name = s;

					cheatList.Add(c);
				}

				Global.Config.RecentCheats.Add(file.FullName);
				changes = false;


			}

			if (Global.Config.DisableCheatsOnLoad)
			{
				for (int x = 0; x < cheatList.Count; x++)
					cheatList[x].Disable();
			}
			return true; //TODO
		}

		public void NotSupportedError()
		{
			MessageBox.Show("Unable to enable cheat for this platform, cheats are not supported for " + Global.Emulator.SystemId, "Cheat error",
							MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private MemoryDomain SetDomain(string name)
		{
			//Attempts to find the memory domain by name, if it fails, it defaults to index 0
			for (int x = 0; x < Global.Emulator.MemoryDomains.Count; x++)
			{
				if (Global.Emulator.MemoryDomains[x].Name == name)
					return Global.Emulator.MemoryDomains[x];
			}
			return Global.Emulator.MemoryDomains[0];
		}

		public bool IsActiveCheat(MemoryDomain d, int address)
		{
			for (int x = 0; x < cheatList.Count; x++)
			{
				if (cheatList[x].address == address && cheatList[x].domain.Name == d.Name)
				{
					return true;
				}
			}
			return false;
		}

		public string GetCheatsPath()
		{
			string path;
			switch (Global.Emulator.SystemId)
			{
				case "NES":
					path = PathManager.MakeAbsolutePath(Global.Config.PathNESCheats, "NES");
					break;
				case "SMS":
					path = PathManager.MakeAbsolutePath(Global.Config.PathSMSCheats, "SMS");
					break;
				case "SG":
					path = PathManager.MakeAbsolutePath(Global.Config.PathSGCheats, "SG");
					break;
				case "GG":
					path = PathManager.MakeAbsolutePath(Global.Config.PathGGCheats, "GG");
					break;
				case "GEN":
					path = PathManager.MakeAbsolutePath(Global.Config.PathGenesisCheats, "GEN");
					break;
				case "SFX":
				case "PCE":
					path = PathManager.MakeAbsolutePath(Global.Config.PathPCECheats, "PCE");
					break;
				case "GB":
					path = PathManager.MakeAbsolutePath(Global.Config.PathGBCheats, "GB");
					break;
				case "TI83":
					path = PathManager.MakeAbsolutePath(Global.Config.PathTI83Cheats, "TI83");
					break;
				default:
					path = PathManager.GetBasePathAbsolute();
					break;
			}
			var f = new FileInfo(path);
			if (f.Directory.Exists == false)
				f.Directory.Create();
			return path;
		}

		public bool SaveCheatFile(string path)
		{
			var file = new FileInfo(path);
			if (!file.Directory.Exists)
				file.Directory.Create();

			using (StreamWriter sw = new StreamWriter(path))
			{
				string str = "";

				for (int x = 0; x < cheatList.Count; x++)
				{
					str += FormatAddress(cheatList[x].address) + "\t";
					str += String.Format("{0:X2}", cheatList[x].value) + "\t";
					str += cheatList[x].domain.Name + "\t";
					if (cheatList[x].IsEnabled())
						str += "1\t";
					else
						str += "0\t";
					str += cheatList[x].name + "\n";
				}

				sw.WriteLine(str);
			}
			changes = false;
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
				if (changes && cheatList.Count > 0)
				{
					if (currentCheatFile.Length == 0)
						currentCheatFile = MakeDefaultFilename();

					SaveCheatFile(Global.CheatList.currentCheatFile);
				}
			}
		}

		public string MakeDefaultFilename()
		{
			return Path.Combine(Global.CheatList.GetCheatsPath(), PathManager.FilesystemSafeName(Global.Game) + ".cht");
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
			string CheatFile = MakeDefaultFilename();

			var file = new FileInfo(CheatFile);
			if (file.Exists == false)
				return false;
			else
			{
				LoadCheatFile(CheatFile, false);
				return true;
			}
		}

		public void Clear()
		{
			cheatList.Clear();
			MemoryPulse.Clear();
		}
	}
}
