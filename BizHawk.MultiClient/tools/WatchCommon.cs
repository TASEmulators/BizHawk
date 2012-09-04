using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Globalization;

namespace BizHawk.MultiClient
{
	public static class WatchCommon
	{
		public static bool SaveWchFile(string path, string domain_name, List<Watch> watchList)
		{
			var file = new FileInfo(path);

			using (StreamWriter sw = new StreamWriter(path))
			{
				StringBuilder str = new StringBuilder();
				str.Append("Domain " + domain_name + "\n");
				str.Append("SystemID " + Global.Emulator.SystemId + "\n");

				for (int x = 0; x < watchList.Count; x++)
				{
					str.Append(string.Format("{0:X4}", watchList[x].Address) + "\t");
					str.Append(watchList[x].TypeChar.ToString() + "\t");
					str.Append(watchList[x].SignedChar.ToString() + "\t");

					if (watchList[x].BigEndian == true)
					{
						str.Append("1\t");
					}
					else
					{
						str.Append("0\t");
					}

					str.Append(watchList[x].Notes + "\n");
				}

				sw.WriteLine(str.ToString());
			}
			return true;
		}

		public static bool LoadWatchFile(string path, bool append, List<Watch> watchList, out string domain)
		{
			domain = "";
			int y, z;
			var file = new FileInfo(path);
			if (file.Exists == false) return false;

			using (StreamReader sr = file.OpenText())
			{
				int count = 0;
				string s = "";
				string temp = "";

				if (append == false)
					watchList.Clear();  //Wipe existing list and read from file

				while ((s = sr.ReadLine()) != null)
				{
					//parse each line and add to watchList

					//.wch files from other emulators start with a number representing the number of watch, that line can be discarded here
					//Any properly formatted line couldn't possibly be this short anyway, this also takes care of any garbage lines that might be in a file
					if (s.Length < 5) continue;

					if (s.Length >= 6 && s.Substring(0, 6) == "Domain")
						domain = s.Substring(7, s.Length - 7);

					if (s.Length >= 8 && s.Substring(0, 8) == "SystemID")
						continue;

					z = StringHelpers.HowMany(s, '\t');
					if (z == 5)
					{
						//If 5, then this is a .wch file format made from another emulator, the first column (watch position) is not needed here
						y = s.IndexOf('\t') + 1;
						s = s.Substring(y, s.Length - y);   //5 digit value representing the watch position number
					}
					else if (z != 4)
						continue;   //If not 4, something is wrong with this line, ignore it
					count++;
					Watch w = new Watch();

					temp = s.Substring(0, s.IndexOf('\t'));
					try
					{
						w.Address = int.Parse(temp, NumberStyles.HexNumber);
					}
					catch
					{
						continue;
					}

					y = s.IndexOf('\t') + 1;
					s = s.Substring(y, s.Length - y);   //Type
					if (!w.SetTypeByChar(s[0]))
						continue;

					y = s.IndexOf('\t') + 1;
					s = s.Substring(y, s.Length - y);   //Signed
					if (!w.SetSignedByChar(s[0]))
						continue;

					y = s.IndexOf('\t') + 1;
					s = s.Substring(y, s.Length - y);   //Endian
					try {
						y = Int16.Parse(s[0].ToString());
					}
					catch
					{
						continue;
					}
					if (y == 0)
						w.BigEndian = false;
					else
						w.BigEndian = true;

					w.Notes = s.Substring(2, s.Length - 2);   //User notes

					watchList.Add(w);
				}
			}

			return true;
		}

		public static int GetDomainPos(string name)
		{
			//Attempts to find the memory domain by name, if it fails, it defaults to index 0
			for (int x = 0; x < Global.Emulator.MemoryDomains.Count; x++)
			{
				if (Global.Emulator.MemoryDomains[x].Name == name)
					return x;
			}
			return 0;
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
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.WatchPath, "");
			}
			else
			{
				sfd.FileName = "NULL";
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.WatchPath, "");
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
	}
}
