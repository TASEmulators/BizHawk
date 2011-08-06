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
					cheatList.Clear();  //Wipe existing list and read from file

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
	}
}
