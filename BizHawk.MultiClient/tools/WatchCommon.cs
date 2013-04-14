using System;
using System.Collections.Generic;
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
			using (StreamWriter sw = new StreamWriter(path))
			{
				StringBuilder str = new StringBuilder();
				str.Append("Domain ");
				str.Append(domain_name);
				str.Append('\n');
				str.Append("SystemID ");
				str.Append(Global.Emulator.SystemId);
				str.Append('\n');

				foreach (Watch t in watchList)
				{
					str.Append(string.Format("{0:X4}", t.Address));
					str.Append('\t');
					str.Append(t.TypeChar);
					str.Append('\t');
					str.Append(t.SignedChar);
					str.Append('\t');

					if (t.BigEndian)
					{
						str.Append("1\t");
					}
					else
					{
						str.Append("0\t");
					}
					str.Append(t.Domain.Name);
					str.Append('\t');
					str.Append(t.Notes);
					str.Append('\n');
				}

				sw.WriteLine(str.ToString());
			}
			return true;
		}

		public static bool LoadWatchFile(string path, bool append, List<Watch> watchList, out string domain)
		{
			domain = "";
			var file = new FileInfo(path);
			if (file.Exists == false) return false;
			bool isBizHawkWatch = true; //Hack to support .wch files from other emulators
			bool isOldBizHawkWatch = false;
			using (StreamReader sr = file.OpenText())
			{
				string s;

				if (append == false)
				{
					watchList.Clear();
				}

				while ((s = sr.ReadLine()) != null)
				{
					//.wch files from other emulators start with a number representing the number of watch, that line can be discarded here
					//Any properly formatted line couldn't possibly be this short anyway, this also takes care of any garbage lines that might be in a file
					if (s.Length < 5)
					{
						isBizHawkWatch = false;
						continue;
					}

					if (s.Length >= 6 && s.Substring(0, 6) == "Domain")
					{
						domain = s.Substring(7, s.Length - 7);
						isBizHawkWatch = true;
					}

					if (s.Length >= 8 && s.Substring(0, 8) == "SystemID")
						continue;

					int z = StringHelpers.HowMany(s, '\t');
					int y;
					if (z == 5)
					{
						//If 5, then this is a post 1.0.5 .wch file
						if (isBizHawkWatch)
						{
							//Do nothing here
						}
						else
						{
							y = s.IndexOf('\t') + 1;
							s = s.Substring(y, s.Length - y);   //5 digit value representing the watch position number
						}
					}
					else if (z == 4)
					{
						isOldBizHawkWatch = true;
					}
					else //4 is 1.0.5 and earlier
					{
						continue;   //If not 4, something is wrong with this line, ignore it
					}
					Watch w = new Watch();

					string temp = s.Substring(0, s.IndexOf('\t'));
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
					{
						w.BigEndian = false;
					}
					else
					{
						w.BigEndian = true;
					}

					if (isBizHawkWatch && !isOldBizHawkWatch)
					{
						y = s.IndexOf('\t') + 1;
						s = s.Substring(y, s.Length - y);   //Domain
						temp = s.Substring(0, s.IndexOf('\t'));
						w.Domain = Global.Emulator.MemoryDomains[GetDomainPos(temp)];
					}

					y = s.IndexOf('\t') + 1;
					w.Notes = s.Substring(y, s.Length - y);   //User notes

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
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.WatchPath);
			}
			else
			{
				sfd.FileName = "NULL";
				sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.WatchPath);
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
